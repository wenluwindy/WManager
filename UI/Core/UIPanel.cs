using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace WManager
{
    /// <summary>
    /// UI 面板基类，负责单个页面的生命周期。
    ///
    /// 生命周期顺序：
    /// <code>
    /// OnCreate                 首次创建，只调一次
    /// OnWillOpen(ctx)          打开前（过渡动画开始前）
    /// OnDidOpen(ctx)           打开后（过渡动画结束后）
    /// OnPause / OnResume       被 Push 的新页面覆盖 / 重新回到栈顶
    /// OnWillClose              关闭前
    /// OnDidClose               关闭后
    /// OnHidden                 缓存面板隐藏完成（Cache = true）
    /// OnRelease                实例销毁前（Cache = false）
    /// </code>
    ///
    /// 过渡动画被后续操作打断时，本次打开 / 关闭会在动画处停止，
    /// 不再回调 OnDidOpen / OnDidClose，由接管的那次操作负责走完自己的流程。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public abstract class UIPanel : MonoBehaviour
    {
        private sealed class ClickBinding
        {
            public Button Button;
            public UnityAction Action;
        }

        /// <summary>资源 Key</summary>
        public string PanelKey { get; internal set; }

        /// <summary>所属层级</summary>
        public UILayer Layer { get; internal set; }

        /// <summary>显示模式</summary>
        public UIShowMode ShowMode { get; internal set; }

        /// <summary>是否处于打开状态（过渡动画播完之后才为 true）</summary>
        public bool IsOpen { get; private set; }

        /// <summary>是否已经执行过 OnCreate</summary>
        public bool IsInitialized { get; private set; }

        /// <summary>是否正在播放打开 / 关闭过渡</summary>
        public bool IsTransitioning { get; private set; }

        /// <summary>是否被 Push 的新页面覆盖</summary>
        public bool IsPaused { get; private set; }

        /// <summary>过渡动画组件，没挂时为 null（无动画直接显示 / 隐藏）</summary>
        protected IUITransition Transition { get; private set; }

        protected RectTransform RectTransform { get; private set; }
        protected CanvasGroup CanvasGroup { get; private set; }
        protected Canvas Canvas { get; private set; }

        private readonly List<ClickBinding> _clickBindings = new();

        /// <summary>每次打开 / 关闭自增，用来识别本次过渡是否已被后续操作接管</summary>
        private int _transitionVersion;

        // ============================================================ 框架内部调用

        internal void InternalSetup(string panelKey, UILayer layer, UIShowMode showMode)
        {
            PanelKey = panelKey;
            Layer = layer;
            ShowMode = showMode;

            RectTransform = GetComponent<RectTransform>();
            CanvasGroup = GetComponent<CanvasGroup>();
            Canvas = GetComponent<Canvas>();
            Transition = GetComponent<IUITransition>();

            if (IsInitialized)
                return;

            IsInitialized = true;

            // 初始一律置为隐藏态，随后由 InternalOpenAsync 播入场动画
            Transition?.SetHiddenImmediate();

            OnCreate();
        }

        /// <returns>true 表示本次打开走完了完整流程；false 表示中途被后续操作接管</returns>
        internal async UniTask<bool> InternalOpenAsync(UIContext context, CancellationToken cancellationToken = default)
        {
            int version = ++_transitionVersion;

            gameObject.SetActive(true);
            IsPaused = false;
            IsTransitioning = true;

            OnWillOpen(context);

            if (Transition != null)
            {
                try
                {
                    await Transition.PlayShowAsync(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    if (version == _transitionVersion)
                        IsTransitioning = false;

                    // 外部主动取消要让调用方知道；被后续操作接管则静默让出
                    if (cancellationToken.IsCancellationRequested)
                        throw;

                    return false;
                }
            }

            if (version != _transitionVersion)
                return false;

            IsTransitioning = false;
            IsOpen = true;

            OnDidOpen(context);
            return true;
        }

        /// <returns>true 表示本次关闭走完了完整流程；false 表示中途被后续操作接管</returns>
        internal async UniTask<bool> InternalCloseAsync(bool destroyAfterClose, CancellationToken cancellationToken = default)
        {
            int version = ++_transitionVersion;

            // 被 Push 覆盖的页面是 inactive 的，只跳过动画，回调一个都不能少
            bool playTransition = Transition != null && gameObject.activeSelf;

            IsTransitioning = true;

            OnWillClose();

            if (playTransition)
            {
                try
                {
                    await Transition.PlayHideAsync(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    if (version == _transitionVersion)
                        IsTransitioning = false;

                    if (cancellationToken.IsCancellationRequested)
                        throw;

                    return false;
                }
            }

            if (version != _transitionVersion)
                return false;

            IsTransitioning = false;
            IsOpen = false;
            IsPaused = false;

            OnDidClose();

            if (destroyAfterClose)
            {
                InternalRelease();
            }
            else
            {
                gameObject.SetActive(false);
                Transition?.SetHiddenImmediate();
                OnHidden();
            }

            return true;
        }

        internal void InternalRelease()
        {
            UnbindAllClicks();
            OnRelease();
        }

        internal void InternalPause()
        {
            if (IsPaused)
                return;

            IsPaused = true;
            OnPause();
        }

        internal void InternalResume()
        {
            if (!IsPaused)
                return;

            IsPaused = false;
            OnResume();
        }

        // ============================================================ 面板自助 API

        /// <summary>关闭自己。缓存策略由面板的 [UIPanelInfo] 决定。</summary>
        protected UniTask CloseAsync(CancellationToken cancellationToken = default)
        {
            if (!UIManager.HasInstance)
                return UniTask.CompletedTask;

            return UIManager.Instance.CloseAsync(this, cancellationToken);
        }

        /// <summary>关闭自己（不等待）。按钮回调里直接用这个。</summary>
        protected void Close()
        {
            CloseAsync().Forget();
        }

        /// <summary>返回上一页（自己在页面栈里时才有意义）</summary>
        protected UniTask PopAsync(CancellationToken cancellationToken = default)
        {
            if (!UIManager.HasInstance)
                return UniTask.CompletedTask;

            return UIManager.Instance.PopAsync(cancellationToken);
        }

        /// <summary>返回上一页（不等待）</summary>
        protected void Pop()
        {
            PopAsync().Forget();
        }

        // ============================================================ 按钮绑定

        /// <summary>
        /// 绑定同步点击回调。异常会被捕获并打日志，不会打断 UnityEvent 的其它监听者；
        /// 面板销毁时自动解绑。
        /// </summary>
        protected void BindClick(Button button, Action handler)
        {
            if (!ValidateBinding(button, handler))
                return;

            UnityAction action = () =>
            {
                try
                {
                    handler();
                }
                catch (Exception e)
                {
                    Debug.LogException(e, this);
                }
            };

            Register(button, action);
        }

        /// <summary>
        /// 绑定异步点击回调。相比自己写 <c>async void</c>：
        /// 异常会被捕获打日志、执行期间自动防重入、面板销毁时自动解绑。
        /// </summary>
        /// <param name="button">目标按钮</param>
        /// <param name="handler">异步回调</param>
        /// <param name="blockReentry">执行期间把按钮置灰，避免连点</param>
        protected void BindClick(Button button, Func<UniTask> handler, bool blockReentry = true)
        {
            if (!ValidateBinding(button, handler))
                return;

            bool running = false;

            async UniTaskVoid RunAsync()
            {
                if (running)
                    return;

                running = true;

                if (blockReentry && button != null)
                    button.interactable = false;

                try
                {
                    await handler();
                }
                catch (OperationCanceledException)
                {
                    // 正常的取消，不当作错误
                }
                catch (Exception e)
                {
                    Debug.LogException(e, this);
                }
                finally
                {
                    running = false;

                    if (blockReentry && button != null)
                        button.interactable = true;
                }
            }

            UnityAction action = () => RunAsync().Forget();

            Register(button, action);
        }

        /// <summary>
        /// 把同一个回调绑到多个按钮上。横竖屏双布局的页面尤其好用：
        /// 两套布局的按钮一次全绑上，之后方向怎么切都不用重新绑定，
        /// 因为同一时刻只有一套布局是激活的。
        /// </summary>
        protected void BindClickAll(Action handler, params Button[] buttons)
        {
            if (buttons == null)
                return;

            foreach (var button in buttons)
            {
                if (button != null)
                    BindClick(button, handler);
            }
        }

        /// <inheritdoc cref="BindClickAll(System.Action,UnityEngine.UI.Button[])" />
        protected void BindClickAll(Func<UniTask> handler, params Button[] buttons)
        {
            if (buttons == null)
                return;

            foreach (var button in buttons)
            {
                if (button != null)
                    BindClick(button, handler);
            }
        }

        /// <summary>解绑本面板通过 BindClick 建立的所有监听</summary>
        protected void UnbindAllClicks()
        {
            for (int i = 0; i < _clickBindings.Count; i++)
            {
                var binding = _clickBindings[i];
                if (binding.Button != null)
                    binding.Button.onClick.RemoveListener(binding.Action);
            }

            _clickBindings.Clear();
        }

        private bool ValidateBinding(Button button, object handler)
        {
            if (button == null)
            {
                Debug.LogWarning($"[{GetType().Name}] BindClick 失败：Button 为空，检查 Inspector 引用是否漏拖。", this);
                return false;
            }

            if (handler == null)
            {
                Debug.LogWarning($"[{GetType().Name}] BindClick 失败：回调为空。", this);
                return false;
            }

            return true;
        }

        private void Register(Button button, UnityAction action)
        {
            button.onClick.AddListener(action);
            _clickBindings.Add(new ClickBinding { Button = button, Action = action });
        }

        protected virtual void OnDestroy()
        {
            UnbindAllClicks();
        }

        // ============================================================ 可重写的生命周期

        /// <summary>首次创建，只调用一次。适合做引用绑定、事件订阅。</summary>
        protected virtual void OnCreate() { }

        /// <summary>打开前，过渡动画开始之前。适合刷新数据。</summary>
        protected virtual void OnWillOpen(UIContext context) { }

        /// <summary>打开后，过渡动画播放完成。</summary>
        protected virtual void OnDidOpen(UIContext context) { }

        /// <summary>关闭前，过渡动画开始之前。</summary>
        protected virtual void OnWillClose() { }

        /// <summary>关闭后，过渡动画播放完成。被覆盖的 inactive 页面也会收到。</summary>
        protected virtual void OnDidClose() { }

        /// <summary>缓存面板隐藏完成（Cache = true 时代替 OnRelease）。</summary>
        protected virtual void OnHidden() { }

        /// <summary>实例即将释放（Cache = false 时）。适合退订事件。</summary>
        protected virtual void OnRelease() { }

        /// <summary>被 Push 进来的新页面覆盖时。</summary>
        protected virtual void OnPause() { }

        /// <summary>重新回到栈顶时。</summary>
        protected virtual void OnResume() { }

        /// <summary>
        /// Esc / Android 返回键。返回 true 表示本面板已经自行处理，
        /// 框架不再执行默认的关闭弹窗或返回上一页。
        /// </summary>
        protected internal virtual bool OnBackPressed() => false;
    }
}
