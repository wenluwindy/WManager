using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace WManager
{
    /// <summary>
    /// UI 服务抽象。业务模块依赖这个接口而不是 <see cref="UIManager"/> 具体类，
    /// 便于在编辑器工具、单元测试或独立模块里替换成假实现。
    ///
    /// 运行时实例通过 <c>UI.Service</c> 获取。
    /// </summary>
    public interface IUIService
    {
        // ---------------------------------------------------------------- 打开

        UniTask<T> OpenAsync<T>(
            UIContext context = null,
            string key = null,
            UILayer? layer = null,
            bool? cache = null,
            bool bringToTop = true,
            UIShowMode? showMode = null,
            CancellationToken cancellationToken = default) where T : UIPanel;

        UniTask<T> OpenAsync<T, TArgs>(
            TArgs args,
            string key = null,
            UILayer? layer = null,
            bool? cache = null,
            bool bringToTop = true,
            UIShowMode? showMode = null,
            CancellationToken cancellationToken = default) where T : UIPanel<TArgs>;

        // ---------------------------------------------------------------- 页面栈

        UniTask<T> PushAsync<T>(
            UIContext context = null,
            string key = null,
            UILayer? layer = null,
            bool? cache = null,
            CancellationToken cancellationToken = default) where T : UIPanel;

        UniTask<T> PushAsync<T, TArgs>(
            TArgs args,
            string key = null,
            UILayer? layer = null,
            bool? cache = null,
            CancellationToken cancellationToken = default) where T : UIPanel<TArgs>;

        UniTask PopAsync(CancellationToken cancellationToken = default);

        UniTask PopToRootAsync(CancellationToken cancellationToken = default);

        UniTask PopToAsync(UIPanel target, CancellationToken cancellationToken = default);

        UniTask PopToAsync<T>(CancellationToken cancellationToken = default) where T : UIPanel;

        UIPanel PeekPage();

        int PageStackCount { get; }

        // ---------------------------------------------------------------- 关闭

        UniTask CloseAsync(UIPanel panel, CancellationToken cancellationToken = default);

        UniTask CloseAsync<T>(CancellationToken cancellationToken = default) where T : UIPanel;

        UniTask CloseAsync(string key, CancellationToken cancellationToken = default);

        UniTask CloseAllAsync(CancellationToken cancellationToken = default);

        // ---------------------------------------------------------------- 查询与缓存

        UniTask<T> PreloadAsync<T>(
            string key = null,
            UILayer? layer = null,
            CancellationToken cancellationToken = default) where T : UIPanel;

        T GetPanel<T>() where T : UIPanel;

        UIPanel GetPanel(string key);

        bool IsOpen<T>() where T : UIPanel;

        bool IsLoaded<T>() where T : UIPanel;

        bool ReleaseCached(string key);

        bool ReleaseCached<T>() where T : UIPanel;

        int ReleaseAllCached(bool includePersistent = false);

        // ---------------------------------------------------------------- 通用界面

        UniTask<bool> ShowMessageBoxAsync(
            MessageBoxRequest request,
            CancellationToken cancellationToken = default);

        UniTask<bool> ShowMessageBoxAsync(
            string title,
            string message,
            bool withCancel = false,
            CancellationToken cancellationToken = default);

        UniTask<MessageBoxResult> ShowMessageBoxResultAsync(
            MessageBoxRequest request,
            CancellationToken cancellationToken = default);

        void ShowToast(string text, float duration = 0f);

        void ShowLoading(string text = null);

        void HideLoading();

        void HideLoadingAll();

        // ---------------------------------------------------------------- 事件

        event Action<UIPanel> OnPanelOpened;

        event Action<UIPanel> OnPanelClosed;

        event Action<UIPanel> OnPanelPushed;

        event Action<UIPanel> OnPanelPopped;
    }
}
