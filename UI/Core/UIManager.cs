using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace WManager
{
    /// <summary>
    /// UI 管理器：负责面板的打开、关闭、页面栈、缓存、预加载、弹窗遮罩、
    /// Toast / Loading 与返回键。
    ///
    /// 常规用法只需要静态门面 <see cref="UI"/>：
    /// <code>
    /// await UI.OpenAsync&lt;MainMenuPanel&gt;();
    /// await UI.PushAsync&lt;BagPanel&gt;();
    /// await UI.PopAsync();
    /// bool ok = await UI.ShowMessageBoxAsync("提示", "确定要退出吗？", true);
    /// </code>
    ///
    /// 面板的资源 Key、层级、缓存策略写在面板类自己的
    /// <see cref="UIPanelInfoAttribute"/> 上，调用侧不需要重复传。
    /// </summary>
    public class UIManager : SingletonBehaviour<UIManager>, IUIService
    {
        /// <summary>一个面板实例的运行时记录</summary>
        private sealed class PanelRecord
        {
            /// <summary>类型声明的元数据（MultiInstance / Persistent / 遮罩策略等固有属性）</summary>
            public UIPanelMeta Meta;

            public UIPanel Panel;

            /// <summary>实际所在层级，可能被调用侧覆盖</summary>
            public UILayer Layer;

            /// <summary>实际缓存策略，可能被调用侧覆盖</summary>
            public bool Cache;

            /// <summary>实际显示模式，可能被调用侧覆盖</summary>
            public UIShowMode ShowMode;

            public UIPopupMask Mask;

            /// <summary>最后一次被打开的时间，缓存超限时按它决定淘汰顺序</summary>
            public float LastUseTime;

            public string Key => Meta.Key;
        }

        private readonly List<PanelRecord> _records = new();
        private readonly Dictionary<UIPanel, PanelRecord> _recordByPanel = new();

        /// <summary>正在加载中的面板，用来合并同一 key 的并发打开请求</summary>
        private readonly Dictionary<string, UniTask<UIPanel>> _loading = new();

        /// <summary>页面栈。用 List 而不是 Stack，是为了支持"返回到某一页"</summary>
        private readonly List<UIPanel> _pageStack = new();

        private IAssetLoader _assetLoader;
        private UIToastService _toastService;
        private UILoadingView _loadingView;
        private int _loadingRefCount;
        private bool _handlingBackKey;

        // ============================================================ 事件

        /// <summary>面板打开完成（过渡动画播完）</summary>
        public event Action<UIPanel> OnPanelOpened;

        /// <summary>面板关闭完成</summary>
        public event Action<UIPanel> OnPanelClosed;

        /// <summary>面板入栈</summary>
        public event Action<UIPanel> OnPanelPushed;

        /// <summary>面板出栈</summary>
        public event Action<UIPanel> OnPanelPopped;

        /// <summary>返回键没有任何面板处理（通常在这里弹"再按一次退出游戏"）</summary>
        public event Action OnBackKeyUnhandled;

        // ============================================================ 生命周期

        protected override void Awake()
        {
            base.Awake();

            // 重复实例已被基类销毁
            if (Instance != this)
                return;

            UIRoot.Create();
        }

        private void Update()
        {
            if (!UISettings.Instance.enableBackKey)
                return;

            if (_handlingBackKey || !WasBackKeyPressed())
                return;

            HandleBackKeyAsync().Forget();
        }

        protected override void OnDestroy()
        {
            OnPanelOpened = null;
            OnPanelClosed = null;
            OnPanelPushed = null;
            OnPanelPopped = null;
            OnBackKeyUnhandled = null;

            base.OnDestroy();
        }

        // ============================================================ 资源加载器

        /// <summary>
        /// 注入自定义资源加载器（Resources / AssetBundle / 自研方案）。
        /// 必须在任何 OpenAsync / PreloadAsync 之前调用。未注入时默认走 AddressablesManager。
        /// </summary>
        public void SetAssetLoader(IAssetLoader loader)
        {
            _assetLoader = loader;
        }

        private IAssetLoader GetAssetLoader()
        {
            return _assetLoader ?? AddressablesManager.Instance;
        }

        // ============================================================ 打开

        /// <summary>
        /// 打开面板。资源 Key、层级、缓存策略默认取自面板类上的
        /// <see cref="UIPanelInfoAttribute"/>，显式传参会覆盖它。
        /// </summary>
        /// <param name="context">面板参数</param>
        /// <param name="key">覆盖资源 Key</param>
        /// <param name="layer">覆盖层级</param>
        /// <param name="cache">覆盖缓存策略</param>
        /// <param name="bringToTop">是否置顶</param>
        /// <param name="showMode">覆盖显示模式</param>
        public async UniTask<T> OpenAsync<T>(
            UIContext context = null,
            string key = null,
            UILayer? layer = null,
            bool? cache = null,
            bool bringToTop = true,
            UIShowMode? showMode = null,
            CancellationToken cancellationToken = default) where T : UIPanel
        {
            var meta = UIPanelMeta.Resolve(typeof(T), key, layer, cache, showMode);
            var panel = await OpenInternalAsync(meta, context, bringToTop, cancellationToken);
            return panel as T;
        }

        /// <summary>打开带强类型参数的面板</summary>
        public UniTask<T> OpenAsync<T, TArgs>(
            TArgs args,
            string key = null,
            UILayer? layer = null,
            bool? cache = null,
            bool bringToTop = true,
            UIShowMode? showMode = null,
            CancellationToken cancellationToken = default) where T : UIPanel<TArgs>
        {
            return OpenAsync<T>(UIContext.Of(args), key, layer, cache, bringToTop, showMode, cancellationToken);
        }

        private async UniTask<UIPanel> OpenInternalAsync(
            UIPanelMeta meta,
            UIContext context,
            bool bringToTop,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(meta.Key))
            {
                Debug.LogError($"[UIManager] 打开 {meta.PanelType.Name} 失败：资源 Key 为空。");
                return null;
            }

            PushInputBlock();

            try
            {
                var record = await AcquireRecordAsync(meta, cancellationToken);

                if (record == null)
                    return null;

                record.LastUseTime = Time.unscaledTime;

                EnsureMask(record);

                if (bringToTop)
                    record.Panel.transform.SetAsLastSibling();

                bool completed = await record.Panel.InternalOpenAsync(context, cancellationToken);

                if (!completed)
                    return record.Panel;

                TrimCache();
                LogVerbose($"打开 {record.Panel.GetType().Name}（key = {record.Key}，layer = {record.Layer}）");
                OnPanelOpened?.Invoke(record.Panel);

                return record.Panel;
            }
            finally
            {
                PopInputBlock();
            }
        }

        /// <summary>拿到可用的面板记录：复用已有的，或者新建一个</summary>
        private async UniTask<PanelRecord> AcquireRecordAsync(UIPanelMeta meta, CancellationToken cancellationToken)
        {
            if (!meta.MultiInstance)
            {
                var existing = FindRecordByKey(meta.Key);

                // 同一 key 的并发打开：等前一次加载完成后复用，避免实例化出两份
                if (existing == null && _loading.TryGetValue(meta.Key, out var pending))
                {
                    await pending;
                    existing = FindRecordByKey(meta.Key);
                }

                if (existing != null)
                {
                    ApplyMeta(existing, meta);
                    return existing;
                }
            }

            return await CreateRecordAsync(meta, cancellationToken);
        }

        private async UniTask<PanelRecord> CreateRecordAsync(UIPanelMeta meta, CancellationToken cancellationToken)
        {
            // Preserve 让同一个加载任务可以被多个调用方 await
            var task = LoadPanelAsync(meta, cancellationToken).Preserve();

            if (!meta.MultiInstance)
                _loading[meta.Key] = task;

            UIPanel panel;

            try
            {
                panel = await task;
            }
            finally
            {
                if (!meta.MultiInstance)
                    _loading.Remove(meta.Key);
            }

            if (panel == null)
                return null;

            var record = new PanelRecord
            {
                Meta = meta,
                Panel = panel,
                Layer = meta.Layer,
                Cache = meta.Cache,
                ShowMode = meta.ShowMode,
                LastUseTime = Time.unscaledTime
            };

            _records.Add(record);
            _recordByPanel[panel] = record;

            return record;
        }

        private async UniTask<UIPanel> LoadPanelAsync(UIPanelMeta meta, CancellationToken cancellationToken)
        {
            var parent = EnsureRoot().GetLayerRoot(meta.Layer);
            var go = await GetAssetLoader().InstantiateAsync(meta.Key, parent, false, cancellationToken);

            if (go == null)
            {
                Debug.LogError(
                    $"[UIManager] 打开 {meta.PanelType.Name} 失败：实例化不出预制体，key = {meta.Key}。" +
                    "请确认该 key 已加入 Addressables，或用 [UIPanelInfo(Key = \"...\")] 指定正确的 key。");
                return null;
            }

            var panel = go.GetComponent(meta.PanelType) as UIPanel;

            if (panel == null)
            {
                Debug.LogError(
                    $"[UIManager] 打开失败：预制体 {meta.Key} 的根节点上没有 {meta.PanelType.Name} 组件。");
                GetAssetLoader().ReleaseInstance(go);
                return null;
            }

            panel.InternalSetup(meta.Key, meta.Layer, meta.ShowMode);
            return panel;
        }

        /// <summary>复用已有面板时，把本次调用显式指定的层级 / 缓存 / 显示模式同步过去</summary>
        private void ApplyMeta(PanelRecord record, UIPanelMeta meta)
        {
            if (record.Layer != meta.Layer)
            {
                var parent = EnsureRoot().GetLayerRoot(meta.Layer);
                record.Panel.transform.SetParent(parent, false);
                record.Panel.Layer = meta.Layer;
                record.Layer = meta.Layer;

                if (record.Mask != null)
                    record.Mask.transform.SetParent(parent, false);
            }

            if (record.ShowMode != meta.ShowMode)
            {
                record.ShowMode = meta.ShowMode;
                record.Panel.ShowMode = meta.ShowMode;
            }

            record.Cache = meta.Cache;
            record.Meta = meta;
        }

        // ============================================================ 页面栈

        /// <summary>
        /// 入栈打开页面：暂停并隐藏当前栈顶，再把新页面压入栈。
        /// 如果目标页面已经在栈里，则改为一路返回到它（不会重复入栈）。
        /// </summary>
        public async UniTask<T> PushAsync<T>(
            UIContext context = null,
            string key = null,
            UILayer? layer = null,
            bool? cache = null,
            CancellationToken cancellationToken = default) where T : UIPanel
        {
            var meta = UIPanelMeta.Resolve(typeof(T), key, layer, cache, UIShowMode.Stack);

            var stacked = FindInStack(meta);
            if (stacked != null)
            {
                await PopToAsync(stacked, cancellationToken);
                return stacked as T;
            }

            var previousTop = PeekPage();

            if (previousTop != null)
            {
                previousTop.InternalPause();
                previousTop.gameObject.SetActive(false);
            }

            var panel = await OpenInternalAsync(meta, context, true, cancellationToken);

            if (panel == null)
            {
                // 打开失败，把上一页恢复回去
                if (previousTop != null)
                {
                    previousTop.gameObject.SetActive(true);
                    previousTop.InternalResume();
                }

                return null;
            }

            _pageStack.Add(panel);
            LogVerbose($"入栈 {panel.GetType().Name}，当前栈深 {_pageStack.Count}");
            OnPanelPushed?.Invoke(panel);

            return panel as T;
        }

        /// <summary>入栈打开带强类型参数的页面</summary>
        public UniTask<T> PushAsync<T, TArgs>(
            TArgs args,
            string key = null,
            UILayer? layer = null,
            bool? cache = null,
            CancellationToken cancellationToken = default) where T : UIPanel<TArgs>
        {
            return PushAsync<T>(UIContext.Of(args), key, layer, cache, cancellationToken);
        }

        /// <summary>返回上一页</summary>
        public async UniTask PopAsync(CancellationToken cancellationToken = default)
        {
            if (_pageStack.Count == 0)
                return;

            var top = _pageStack[_pageStack.Count - 1];
            _pageStack.RemoveAt(_pageStack.Count - 1);

            if (top != null && _recordByPanel.TryGetValue(top, out var record))
                await ClosePanelInternal(record, cancellationToken);

            LogVerbose($"出栈 {(top != null ? top.GetType().Name : "null")}，当前栈深 {_pageStack.Count}");
            OnPanelPopped?.Invoke(top);

            ResumeStackTop();
        }

        /// <summary>一路返回到栈底</summary>
        public async UniTask PopToRootAsync(CancellationToken cancellationToken = default)
        {
            while (_pageStack.Count > 1)
                await PopAsync(cancellationToken);
        }

        /// <summary>一路返回到指定页面</summary>
        public async UniTask PopToAsync(UIPanel target, CancellationToken cancellationToken = default)
        {
            if (target == null || !_pageStack.Contains(target))
                return;

            while (_pageStack.Count > 0 && _pageStack[_pageStack.Count - 1] != target)
                await PopAsync(cancellationToken);

            ResumeStackTop();
        }

        /// <summary>一路返回到指定类型的页面</summary>
        public UniTask PopToAsync<T>(CancellationToken cancellationToken = default) where T : UIPanel
        {
            for (int i = _pageStack.Count - 1; i >= 0; i--)
            {
                if (_pageStack[i] is T)
                    return PopToAsync(_pageStack[i], cancellationToken);
            }

            return UniTask.CompletedTask;
        }

        /// <summary>当前栈顶页面</summary>
        public UIPanel PeekPage()
        {
            for (int i = _pageStack.Count - 1; i >= 0; i--)
            {
                if (_pageStack[i] != null)
                    return _pageStack[i];
            }

            return null;
        }

        /// <summary>页面栈深度</summary>
        public int PageStackCount => _pageStack.Count;

        /// <summary>只读的页面栈快照，栈底在前</summary>
        public IReadOnlyList<UIPanel> PageStack => _pageStack;

        private void ResumeStackTop()
        {
            var top = PeekPage();

            if (top == null)
                return;

            if (!top.gameObject.activeSelf)
                top.gameObject.SetActive(true);

            top.InternalResume();
        }

        private UIPanel FindInStack(UIPanelMeta meta)
        {
            for (int i = _pageStack.Count - 1; i >= 0; i--)
            {
                var panel = _pageStack[i];

                if (panel != null && meta.PanelType.IsInstanceOfType(panel) && panel.PanelKey == meta.Key)
                    return panel;
            }

            return null;
        }

        private void RemoveFromPageStack(UIPanel target)
        {
            if (target == null)
                return;

            for (int i = _pageStack.Count - 1; i >= 0; i--)
            {
                if (_pageStack[i] == target)
                    _pageStack.RemoveAt(i);
            }
        }

        // ============================================================ 关闭

        /// <summary>关闭指定面板实例。多实例面板请优先用这个重载。</summary>
        public async UniTask CloseAsync(UIPanel panel, CancellationToken cancellationToken = default)
        {
            if (panel == null)
                return;

            if (!_recordByPanel.TryGetValue(panel, out var record))
            {
                Debug.LogWarning($"[UIManager] CloseAsync 忽略：{panel.GetType().Name} 不是由 UIManager 打开的。", panel);
                return;
            }

            await ClosePanelInternal(record, cancellationToken);
        }

        /// <summary>按类型关闭最上面的一个实例</summary>
        public UniTask CloseAsync<T>(CancellationToken cancellationToken = default) where T : UIPanel
        {
            var panel = GetPanel<T>();
            return panel == null ? UniTask.CompletedTask : CloseAsync(panel, cancellationToken);
        }

        /// <summary>按资源 Key 关闭最上面的一个实例</summary>
        public UniTask CloseAsync(string key, CancellationToken cancellationToken = default)
        {
            var record = FindRecordByKey(key);
            return record == null ? UniTask.CompletedTask : ClosePanelInternal(record, cancellationToken);
        }

        /// <summary>关闭所有面板并清空页面栈</summary>
        public async UniTask CloseAllAsync(CancellationToken cancellationToken = default)
        {
            var snapshot = new List<PanelRecord>(_records);

            for (int i = snapshot.Count - 1; i >= 0; i--)
                await ClosePanelInternal(snapshot[i], cancellationToken);

            _pageStack.Clear();
        }

        private async UniTask ClosePanelInternal(PanelRecord record, CancellationToken cancellationToken)
        {
            var panel = record.Panel;

            if (panel == null)
            {
                Unregister(record);
                return;
            }

            // 已经是关闭状态的缓存面板：只需要把它从页面栈里摘掉
            if (!panel.IsOpen && !panel.IsTransitioning && !panel.gameObject.activeSelf)
            {
                RemoveFromPageStack(panel);
                return;
            }

            bool destroy = !record.Cache;
            bool wasStackTop = _pageStack.Count > 0 && _pageStack[_pageStack.Count - 1] == panel;

            RemoveFromPageStack(panel);

            PushInputBlock();

            try
            {
                if (record.Mask != null)
                    record.Mask.FadeOut(UISettings.Instance.popupMaskFadeDuration);

                bool completed = await panel.InternalCloseAsync(destroy, cancellationToken);

                // 关闭动画中途被重新打开，收尾交给接管的那次操作
                if (!completed)
                    return;

                LogVerbose($"关闭 {panel.GetType().Name}（{(destroy ? "已释放" : "已缓存")}）");

                // 事件在实例真正释放之前派发，订阅者还能安全读到面板信息
                OnPanelClosed?.Invoke(panel);

                if (destroy)
                {
                    Unregister(record);
                    DestroyMask(record);
                    GetAssetLoader().ReleaseInstance(panel.gameObject);
                }
                else if (record.Mask != null)
                {
                    record.Mask.gameObject.SetActive(false);
                }

                // D5：关掉的正好是栈顶，等价于 Pop，自动恢复下一页
                if (wasStackTop)
                    ResumeStackTop();
            }
            finally
            {
                PopInputBlock();
            }
        }

        // ============================================================ 查询

        /// <summary>取最上面的一个该类型面板。用基类查询也能拿到子类实例。</summary>
        public T GetPanel<T>() where T : UIPanel
        {
            for (int i = _records.Count - 1; i >= 0; i--)
            {
                if (_records[i].Panel is T typed)
                    return typed;
            }

            return null;
        }

        /// <summary>按资源 Key 取最上面的一个面板</summary>
        public UIPanel GetPanel(string key)
        {
            var record = FindRecordByKey(key);
            return record?.Panel;
        }

        /// <summary>该类型是否有实例处于打开状态</summary>
        public bool IsOpen<T>() where T : UIPanel
        {
            for (int i = _records.Count - 1; i >= 0; i--)
            {
                if (_records[i].Panel is T typed && typed.IsOpen)
                    return true;
            }

            return false;
        }

        /// <summary>该类型是否已有实例（含缓存中的隐藏实例）</summary>
        public bool IsLoaded<T>() where T : UIPanel
        {
            return GetPanel<T>() != null;
        }

        /// <summary>当前存活的面板实例数（含缓存）</summary>
        public int LivePanelCount => _records.Count;

        private PanelRecord FindRecordByKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return null;

            for (int i = _records.Count - 1; i >= 0; i--)
            {
                if (_records[i].Key == key && _records[i].Panel != null)
                    return _records[i];
            }

            return null;
        }

        private void Unregister(PanelRecord record)
        {
            _records.Remove(record);

            if (record.Panel != null)
                _recordByPanel.Remove(record.Panel);
        }

        // ============================================================ 预加载与缓存治理

        /// <summary>
        /// 预加载面板但不显示。加载完成后进入缓存，之后 OpenAsync 直接复用。
        /// </summary>
        public async UniTask<T> PreloadAsync<T>(
            string key = null,
            UILayer? layer = null,
            CancellationToken cancellationToken = default) where T : UIPanel
        {
            var meta = UIPanelMeta.Resolve(typeof(T), key, layer, true, null);

            var existing = FindRecordByKey(meta.Key);
            if (existing != null)
                return existing.Panel as T;

            var record = await CreateRecordAsync(meta, cancellationToken);

            if (record == null)
                return null;

            record.Panel.gameObject.SetActive(false);
            return record.Panel as T;
        }

        /// <summary>释放指定 Key 的缓存面板（必须处于关闭状态）</summary>
        public bool ReleaseCached(string key)
        {
            var record = FindRecordByKey(key);
            return record != null && ReleaseCachedRecord(record);
        }

        /// <summary>释放指定类型的缓存面板（必须处于关闭状态）</summary>
        public bool ReleaseCached<T>() where T : UIPanel
        {
            for (int i = _records.Count - 1; i >= 0; i--)
            {
                if (_records[i].Panel is T)
                    return ReleaseCachedRecord(_records[i]);
            }

            return false;
        }

        /// <summary>
        /// 释放所有处于关闭状态的缓存面板。切场景时调用可以回收一批内存。
        /// </summary>
        /// <param name="includePersistent">是否连 Persistent 面板一起释放</param>
        /// <returns>实际释放的数量</returns>
        public int ReleaseAllCached(bool includePersistent = false)
        {
            int released = 0;
            var snapshot = new List<PanelRecord>(_records);

            foreach (var record in snapshot)
            {
                if (!includePersistent && record.Meta.Persistent)
                    continue;

                if (ReleaseCachedRecord(record))
                    released++;
            }

            return released;
        }

        private bool ReleaseCachedRecord(PanelRecord record)
        {
            var panel = record.Panel;

            if (panel == null)
            {
                Unregister(record);
                return false;
            }

            if (panel.IsOpen || panel.IsTransitioning || panel.gameObject.activeSelf)
                return false;

            RemoveFromPageStack(panel);
            Unregister(record);
            DestroyMask(record);

            panel.InternalRelease();
            GetAssetLoader().ReleaseInstance(panel.gameObject);

            return true;
        }

        /// <summary>缓存数量超过配置上限时，按最久未使用顺序释放</summary>
        private void TrimCache()
        {
            int max = UISettings.Instance.maxCachedPanels;

            if (max <= 0)
                return;

            var candidates = new List<PanelRecord>();

            foreach (var record in _records)
            {
                if (record.Meta.Persistent || record.Panel == null)
                    continue;

                if (record.Panel.IsOpen || record.Panel.IsTransitioning || record.Panel.gameObject.activeSelf)
                    continue;

                candidates.Add(record);
            }

            if (candidates.Count <= max)
                return;

            candidates.Sort((a, b) => a.LastUseTime.CompareTo(b.LastUseTime));

            int removeCount = candidates.Count - max;

            for (int i = 0; i < removeCount; i++)
                ReleaseCachedRecord(candidates[i]);
        }

        // ============================================================ 弹窗遮罩

        private void EnsureMask(PanelRecord record)
        {
            if (!NeedsMask(record))
            {
                if (record.Mask != null)
                    record.Mask.gameObject.SetActive(false);

                return;
            }

            var parent = EnsureRoot().GetLayerRoot(record.Layer);

            if (record.Mask == null)
            {
                record.Mask = UIPopupMask.Create(parent, UISettings.Instance.popupMaskColor);

                var captured = record;
                record.Mask.Clicked += () => CloseAsync(captured.Panel).Forget();
            }
            else if (record.Mask.transform.parent != parent)
            {
                record.Mask.transform.SetParent(parent, false);
            }

            record.Mask.Clickable = UISettings.Instance.popupCloseOnMaskClick && record.Meta.CloseOnMaskClick;

            // 遮罩先置顶，紧接着面板再置顶，面板就正好压在自己的遮罩上面
            record.Mask.transform.SetAsLastSibling();
            record.Mask.FadeIn(UISettings.Instance.popupMaskFadeDuration);
        }

        private static bool NeedsMask(PanelRecord record)
        {
            switch (record.Meta.Mask)
            {
                case UIMaskMode.Always:
                    return true;

                case UIMaskMode.Never:
                    return false;

                default:
                    return record.ShowMode == UIShowMode.Popup && UISettings.Instance.enablePopupMask;
            }
        }

        private void DestroyMask(PanelRecord record)
        {
            if (record.Mask == null)
                return;

            Destroy(record.Mask.gameObject);
            record.Mask = null;
        }

        // ============================================================ MessageBox

        /// <summary>弹出消息框，等待用户选择，返回是否点了确定</summary>
        public async UniTask<bool> ShowMessageBoxAsync(
            MessageBoxRequest request,
            CancellationToken cancellationToken = default)
        {
            var result = await ShowMessageBoxResultAsync(request, cancellationToken);
            return result == MessageBoxResult.Confirm;
        }

        /// <summary>弹出消息框的便捷重载</summary>
        public UniTask<bool> ShowMessageBoxAsync(
            string title,
            string message,
            bool withCancel = false,
            CancellationToken cancellationToken = default)
        {
            return ShowMessageBoxAsync(new MessageBoxRequest
            {
                Title = title,
                Message = message,
                Style = withCancel ? MessageBoxStyle.ConfirmCancel : MessageBoxStyle.Confirm
            }, cancellationToken);
        }

        /// <summary>
        /// 弹出消息框并返回完整结果。消息框是多实例面板，
        /// 同时弹多个会自动叠放，各自的 await 互不干扰。
        /// </summary>
        public async UniTask<MessageBoxResult> ShowMessageBoxResultAsync(
            MessageBoxRequest request,
            CancellationToken cancellationToken = default)
        {
            request ??= new MessageBoxRequest();

            string key = !string.IsNullOrWhiteSpace(request.PanelKey)
                ? request.PanelKey
                : NullIfEmpty(UISettings.Instance.messageBoxKey);

            var panel = await OpenAsync<MessageBoxPanel>(
                UIContext.Of(request),
                key,
                cancellationToken: cancellationToken);

            if (panel == null)
                return MessageBoxResult.Cancel;

            return await panel.WaitForResultAsync();
        }

        // ============================================================ Toast / Loading

        /// <summary>飘一条提示。duration 传 0 使用配置里的默认时长。</summary>
        public void ShowToast(string text, float duration = 0f)
        {
            var settings = UISettings.Instance;

            EnsureToastService();

            _toastService.Show(
                text,
                duration > 0f ? duration : settings.toastDuration,
                settings.toastFadeDuration,
                settings.toastMaxVisible);
        }

        /// <summary>清掉当前所有 Toast</summary>
        public void ClearToasts()
        {
            _toastService?.Clear();
        }

        /// <summary>
        /// 显示全屏 Loading。内部按引用计数，
        /// 多处并发调用时需要配对同样次数的 <see cref="HideLoading"/>。
        /// </summary>
        public void ShowLoading(string text = null)
        {
            EnsureLoadingView();

            _loadingRefCount++;
            _loadingView.SetText(text ?? UISettings.Instance.loadingDefaultText);
            _loadingView.Show();
        }

        /// <summary>隐藏一层 Loading</summary>
        public void HideLoading()
        {
            if (_loadingRefCount <= 0)
                return;

            _loadingRefCount--;

            if (_loadingRefCount == 0)
                _loadingView?.Hide();
        }

        /// <summary>强制关掉 Loading（异常兜底）</summary>
        public void HideLoadingAll()
        {
            _loadingRefCount = 0;
            _loadingView?.Hide();
        }

        private void EnsureToastService()
        {
            if (_toastService != null)
                return;

            _toastService = UIToastService.Create(EnsureRoot().GetLayerRoot(UILayer.Tips));
        }

        private void EnsureLoadingView()
        {
            if (_loadingView != null)
                return;

            _loadingView = UILoadingView.Create(EnsureRoot().GetLayerRoot(UILayer.Top));
        }

        // ============================================================ 返回键

        private static bool WasBackKeyPressed()
        {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            return keyboard != null && keyboard.escapeKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.Escape);
#endif
        }

        /// <summary>
        /// 返回键处理顺序：最上层弹窗 → 栈顶页面 → 返回上一页 → 交给业务。
        /// 面板重写 <see cref="UIPanel.OnBackPressed"/> 返回 true 可以拦下这一步。
        /// </summary>
        private async UniTaskVoid HandleBackKeyAsync()
        {
            _handlingBackKey = true;

            try
            {
                var popup = FindTopMostOpenPopup();

                if (popup != null)
                {
                    if (popup.OnBackPressed())
                        return;

                    if (_recordByPanel.TryGetValue(popup, out var record) && record.Meta.CloseOnBackKey)
                    {
                        await ClosePanelInternal(record, default);
                        return;
                    }
                }

                var top = PeekPage();

                if (top != null && top.OnBackPressed())
                    return;

                if (_pageStack.Count > 1)
                {
                    await PopAsync();
                    return;
                }

                OnBackKeyUnhandled?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            finally
            {
                _handlingBackKey = false;
            }
        }

        private UIPanel FindTopMostOpenPopup()
        {
            UIPanel best = null;
            int bestLayer = int.MinValue;
            int bestSibling = int.MinValue;

            foreach (var record in _records)
            {
                var panel = record.Panel;

                if (panel == null || !panel.IsOpen || record.ShowMode != UIShowMode.Popup)
                    continue;

                int layer = (int)record.Layer;
                int sibling = panel.transform.GetSiblingIndex();

                if (layer > bestLayer || (layer == bestLayer && sibling > bestSibling))
                {
                    best = panel;
                    bestLayer = layer;
                    bestSibling = sibling;
                }
            }

            return best;
        }

        // ============================================================ 杂项

        /// <summary>UIRoot 通常在 Awake 里就建好了，这里只是兜底，避免任何路径拿到 null</summary>
        private static UIRoot EnsureRoot()
        {
            return UIRoot.HasInstance ? UIRoot.Instance : UIRoot.Create();
        }

        private static void PushInputBlock()
        {
            if (!UISettings.Instance.blockInputDuringTransition)
                return;

            if (UIRoot.HasInstance && UIRoot.Instance.InputBlocker != null)
                UIRoot.Instance.InputBlocker.Push();
        }

        private static void PopInputBlock()
        {
            if (!UISettings.Instance.blockInputDuringTransition)
                return;

            if (UIRoot.HasInstance && UIRoot.Instance.InputBlocker != null)
                UIRoot.Instance.InputBlocker.Pop();
        }

        private static string NullIfEmpty(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }

        private static void LogVerbose(string message)
        {
            if (UISettings.Instance.verboseLog)
                Debug.Log($"[UIManager] {message}");
        }

        /// <summary>面板运行时状态快照，给调试窗口和日志用</summary>
        public readonly struct PanelSnapshot
        {
            public readonly UIPanel Panel;
            public readonly string TypeName;
            public readonly string Key;
            public readonly UILayer Layer;
            public readonly UIShowMode ShowMode;
            public readonly bool Cache;
            public readonly bool Persistent;
            public readonly bool MultiInstance;
            public readonly bool IsOpen;
            public readonly bool IsPaused;
            public readonly bool IsTransitioning;
            public readonly int StackIndex;

            internal PanelSnapshot(
                UIPanel panel, string typeName, string key, UILayer layer, UIShowMode showMode,
                bool cache, bool persistent, bool multiInstance,
                bool isOpen, bool isPaused, bool isTransitioning, int stackIndex)
            {
                Panel = panel;
                TypeName = typeName;
                Key = key;
                Layer = layer;
                ShowMode = showMode;
                Cache = cache;
                Persistent = persistent;
                MultiInstance = multiInstance;
                IsOpen = isOpen;
                IsPaused = isPaused;
                IsTransitioning = isTransitioning;
                StackIndex = stackIndex;
            }

            /// <summary>打开 / 暂停 / 缓存 / 已销毁</summary>
            public string StateText =>
                Panel == null ? "已销毁"
                : IsTransitioning ? "过渡中"
                : IsPaused ? "暂停"
                : IsOpen ? "打开"
                : "缓存";
        }

        /// <summary>取当前所有面板实例的状态快照</summary>
        public List<PanelSnapshot> GetSnapshot()
        {
            var result = new List<PanelSnapshot>(_records.Count);

            foreach (var record in _records)
            {
                var panel = record.Panel;

                result.Add(new PanelSnapshot(
                    panel,
                    record.Meta.PanelType.Name,
                    record.Key,
                    record.Layer,
                    record.ShowMode,
                    record.Cache,
                    record.Meta.Persistent,
                    record.Meta.MultiInstance,
                    panel != null && panel.IsOpen,
                    panel != null && panel.IsPaused,
                    panel != null && panel.IsTransitioning,
                    panel == null ? -1 : _pageStack.IndexOf(panel)));
            }

            return result;
        }

        /// <summary>把当前面板与页面栈状态打印出来，排查问题用</summary>
        public string DumpState()
        {
            var builder = new System.Text.StringBuilder();
            builder.AppendLine($"[UIManager] 面板实例 {_records.Count} 个，页面栈深 {_pageStack.Count}");

            foreach (var record in _records)
            {
                string state = record.Panel == null
                    ? "已销毁"
                    : record.Panel.IsOpen ? "打开" : record.Panel.IsPaused ? "暂停" : "缓存";

                builder.AppendLine(
                    $"  · {record.Meta.PanelType.Name,-24} key={record.Key,-28} " +
                    $"layer={record.Layer,-6} mode={record.ShowMode,-6} cache={record.Cache,-5} 状态={state}");
            }

            for (int i = 0; i < _pageStack.Count; i++)
            {
                var panel = _pageStack[i];
                builder.AppendLine($"  栈[{i}] {(panel != null ? panel.GetType().Name : "null")}");
            }

            return builder.ToString();
        }
    }
}
