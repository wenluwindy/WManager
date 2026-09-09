using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace WManager
{
    /// <summary>
    /// UI 静态门面。业务侧的推荐入口，省掉每次都写 <c>UIManager.Instance</c>。
    ///
    /// <code>
    /// await UI.OpenAsync&lt;MainMenuPanel&gt;();
    /// await UI.PushAsync&lt;BagPanel, BagArgs&gt;(new BagArgs { Tab = 1 });
    /// await UI.PopAsync();
    /// UI.ShowToast("保存成功");
    /// bool ok = await UI.ShowMessageBoxAsync("提示", "确定要退出吗？", withCancel: true);
    /// </code>
    /// </summary>
    public static class UI
    {
        /// <summary>底层管理器。需要 UIManager 独有的 API 时用它。</summary>
        public static UIManager Manager => UIManager.Instance;

        /// <summary>接口形式的 UI 服务，便于注入与替换</summary>
        public static IUIService Service => UIManager.Instance;

        /// <summary>UI 根节点</summary>
        public static UIRoot Root => UIRoot.Instance;

        /// <summary>UIManager 是否已经创建。在 OnDestroy 等销毁流程里判断用。</summary>
        public static bool IsAlive => UIManager.HasInstance;

        // ---------------------------------------------------------------- 打开

        /// <inheritdoc cref="UIManager.OpenAsync{T}" />
        public static UniTask<T> OpenAsync<T>(
            UIContext context = null,
            string key = null,
            UILayer? layer = null,
            bool? cache = null,
            bool bringToTop = true,
            UIShowMode? showMode = null,
            CancellationToken cancellationToken = default) where T : UIPanel
        {
            return UIManager.Instance.OpenAsync<T>(context, key, layer, cache, bringToTop, showMode, cancellationToken);
        }

        /// <summary>打开带强类型参数的面板</summary>
        public static UniTask<T> OpenAsync<T, TArgs>(
            TArgs args,
            string key = null,
            UILayer? layer = null,
            bool? cache = null,
            bool bringToTop = true,
            UIShowMode? showMode = null,
            CancellationToken cancellationToken = default) where T : UIPanel<TArgs>
        {
            return UIManager.Instance.OpenAsync<T, TArgs>(args, key, layer, cache, bringToTop, showMode, cancellationToken);
        }

        // ---------------------------------------------------------------- 页面栈

        /// <inheritdoc cref="UIManager.PushAsync{T}" />
        public static UniTask<T> PushAsync<T>(
            UIContext context = null,
            string key = null,
            UILayer? layer = null,
            bool? cache = null,
            CancellationToken cancellationToken = default) where T : UIPanel
        {
            return UIManager.Instance.PushAsync<T>(context, key, layer, cache, cancellationToken);
        }

        /// <summary>入栈打开带强类型参数的页面</summary>
        public static UniTask<T> PushAsync<T, TArgs>(
            TArgs args,
            string key = null,
            UILayer? layer = null,
            bool? cache = null,
            CancellationToken cancellationToken = default) where T : UIPanel<TArgs>
        {
            return UIManager.Instance.PushAsync<T, TArgs>(args, key, layer, cache, cancellationToken);
        }

        public static UniTask PopAsync(CancellationToken cancellationToken = default)
        {
            return UIManager.Instance.PopAsync(cancellationToken);
        }

        public static UniTask PopToRootAsync(CancellationToken cancellationToken = default)
        {
            return UIManager.Instance.PopToRootAsync(cancellationToken);
        }

        public static UniTask PopToAsync<T>(CancellationToken cancellationToken = default) where T : UIPanel
        {
            return UIManager.Instance.PopToAsync<T>(cancellationToken);
        }

        public static UIPanel PeekPage() => UIManager.Instance.PeekPage();

        public static int PageStackCount => UIManager.Instance.PageStackCount;

        // ---------------------------------------------------------------- 关闭

        public static UniTask CloseAsync(UIPanel panel, CancellationToken cancellationToken = default)
        {
            return UIManager.Instance.CloseAsync(panel, cancellationToken);
        }

        public static UniTask CloseAsync<T>(CancellationToken cancellationToken = default) where T : UIPanel
        {
            return UIManager.Instance.CloseAsync<T>(cancellationToken);
        }

        public static UniTask CloseAsync(string key, CancellationToken cancellationToken = default)
        {
            return UIManager.Instance.CloseAsync(key, cancellationToken);
        }

        public static UniTask CloseAllAsync(CancellationToken cancellationToken = default)
        {
            return UIManager.Instance.CloseAllAsync(cancellationToken);
        }

        // ---------------------------------------------------------------- 查询与缓存

        public static UniTask<T> PreloadAsync<T>(
            string key = null,
            UILayer? layer = null,
            CancellationToken cancellationToken = default) where T : UIPanel
        {
            return UIManager.Instance.PreloadAsync<T>(key, layer, cancellationToken);
        }

        public static T GetPanel<T>() where T : UIPanel => UIManager.Instance.GetPanel<T>();

        public static bool IsOpen<T>() where T : UIPanel => UIManager.Instance.IsOpen<T>();

        public static bool IsLoaded<T>() where T : UIPanel => UIManager.Instance.IsLoaded<T>();

        public static bool ReleaseCached<T>() where T : UIPanel => UIManager.Instance.ReleaseCached<T>();

        public static int ReleaseAllCached(bool includePersistent = false)
        {
            return UIManager.Instance.ReleaseAllCached(includePersistent);
        }

        // ---------------------------------------------------------------- 通用界面

        public static UniTask<bool> ShowMessageBoxAsync(
            MessageBoxRequest request,
            CancellationToken cancellationToken = default)
        {
            return UIManager.Instance.ShowMessageBoxAsync(request, cancellationToken);
        }

        public static UniTask<bool> ShowMessageBoxAsync(
            string title,
            string message,
            bool withCancel = false,
            CancellationToken cancellationToken = default)
        {
            return UIManager.Instance.ShowMessageBoxAsync(title, message, withCancel, cancellationToken);
        }

        public static UniTask<MessageBoxResult> ShowMessageBoxResultAsync(
            MessageBoxRequest request,
            CancellationToken cancellationToken = default)
        {
            return UIManager.Instance.ShowMessageBoxResultAsync(request, cancellationToken);
        }

        public static void ShowToast(string text, float duration = 0f)
        {
            UIManager.Instance.ShowToast(text, duration);
        }

        public static void ShowLoading(string text = null) => UIManager.Instance.ShowLoading(text);

        public static void HideLoading() => UIManager.Instance.HideLoading();

        public static void HideLoadingAll() => UIManager.Instance.HideLoadingAll();

        // ---------------------------------------------------------------- 事件

        /// <summary>面板打开完成</summary>
        public static event Action<UIPanel> OnPanelOpened
        {
            add => UIManager.Instance.OnPanelOpened += value;
            remove
            {
                if (UIManager.HasInstance)
                    UIManager.Instance.OnPanelOpened -= value;
            }
        }

        /// <summary>面板关闭完成</summary>
        public static event Action<UIPanel> OnPanelClosed
        {
            add => UIManager.Instance.OnPanelClosed += value;
            remove
            {
                if (UIManager.HasInstance)
                    UIManager.Instance.OnPanelClosed -= value;
            }
        }
    }
}
