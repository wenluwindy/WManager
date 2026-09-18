using System;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets.Initialization;
using WManager;
#if WMANAGER_MULTISERVER
using MultiServer.Sdk;
#endif

/// <summary>
/// 游戏启动流程。
///
/// 阶段一（纯 HTTP，绝对不能触碰 Addressables）：
///     初始化 SDK → 登录 → 向服务器查资源版本，拿到 baseUrl
/// 阶段二（拿到地址后才允许用 Addressables）：
///     写入远程地址 → 初始化 Addressables → 更新 Catalog → 下载资源 → 进主菜单
///
/// 为什么阶段一一行 Addressables 代码都不能写：
/// Profile 里 {MultiServer.Sdk.ResourceApi.RemoteLoadUrl} 这种花括号变量，
/// 只在「加载 Catalog 的那一刻」求值一次，结果会被固化进所有资源的定位信息里。
/// 只要 Addressables 在地址还是空串时完成了初始化，本次运行的远程资源就再也救不回来。
///
/// 未定义 WMANAGER_MULTISERVER 时走精简启动（仅 Addressables + 主菜单），
/// 便于把 WManager 单独导入到没有 MultiServer 的工程。
/// </summary>
public class GameLauncher : MonoBehaviour
{
    [Header("服务器")]
    [Tooltip("MultiServer 地址，结尾不要带 /")]
    [SerializeField] private string serverUrl = "http://localhost:5173";

    [Tooltip("项目 AppKey。注意 AppSecret 是后台密钥，绝对不要写进客户端")]
    [SerializeField] private string appKey = "1dd714ec18da3516";

    [Header("热更新")]
    [Tooltip("需要热更的 Addressables Label")]
    [SerializeField] private string updateLabel = "default";

    [Tooltip("有更新时是否弹窗询问；关掉则静默下载")]
    [SerializeField] private bool askBeforeDownload = true;

    [Tooltip("检查资源版本失败时的重试次数")]
    [SerializeField] private int checkRetryCount = 3;

    [Header("启动流程")]
    [Tooltip("启动时做一次游客登录。热更本身只需要 AppKey，不需要登录")]
    [SerializeField] private bool guestLoginOnLaunch = true;

    [Tooltip("服务器连不上时，用上次成功的地址离线启动（资源走本地缓存）")]
    [SerializeField] private bool allowOfflineFallback = true;

    [Header("引导期状态文字（可选）")]
    [Tooltip("必须是场景里的静态对象，不能是 Addressables 资源 —— 这时候 Addressables 还没初始化")]
    [SerializeField] private TMP_Text bootStatusText;

#if WMANAGER_MULTISERVER
    /// <summary>
    /// Addressables Profile 里 Remote.LoadPath 必须填成 "{这个字符串}"（带花括号）
    /// </summary>
    private const string RemoteUrlVariable = "MultiServer.Sdk.ResourceApi.RemoteLoadUrl";
#endif

    private UpdatePanel _updatePanel;
#if WMANAGER_MULTISERVER
    private string _checkError;

    /// <summary>服务器明确返回了业务错误（AppKey 无效等），这种情况不该走离线兜底掩盖问题</summary>
    private bool _serverRejected;
#endif

    private async void Start()
    {
        try
        {
            await LaunchAsync();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            SetStatus($"启动失败：{e.Message}");
        }
    }

    private async UniTask LaunchAsync()
    {
#if WMANAGER_MULTISERVER
        await LaunchWithMultiServerAsync();
#else
        await LaunchWithoutMultiServerAsync();
#endif
    }

#if !WMANAGER_MULTISERVER
    private async UniTask LaunchWithoutMultiServerAsync()
    {
        Debug.LogWarning(
            "[GameLauncher] 未定义脚本宏 WMANAGER_MULTISERVER，跳过 MultiServer 热更流程。");

        SetStatus("正在初始化资源系统...");
        if (!await AddressablesManager.Instance.InitializeAsync())
        {
            SetStatus("资源系统初始化失败，请确认 Addressables 已配置");
            return;
        }

        await UI.OpenAsync<MainMenuPanel>(new UIContext().Set("title", "主菜单"));
    }
#endif

#if WMANAGER_MULTISERVER
    private async UniTask LaunchWithMultiServerAsync()
    {
        // ==================== 阶段一：只走 HTTP ====================

        SetStatus("正在连接服务器...");
        MultiServerClient.Init(serverUrl, appKey);

        if (guestLoginOnLaunch)
            await TryLoginAsync();

        var check = await CheckResourceWithRetryAsync();

        // 强更：引导下载新安装包，不再往下走
        if (check != null && check.updateType == "force")
        {
            if (!string.IsNullOrEmpty(check.baseUrl))
                ApplyRemoteUrl(check.baseUrl);

            await ShowForceUpdateAsync(check);
            return;
        }

        if (!TryResolveRemoteUrl(check, out string baseUrl))
            return;

        ApplyRemoteUrl(baseUrl);

        // ==================== 阶段二：从这里起才能用 Addressables ====================

        SetStatus("正在初始化资源系统...");
        if (!await AddressablesManager.Instance.InitializeAsync())
        {
            SetStatus("资源系统初始化失败，请确认资源服务器可访问");
            return;
        }

        // UpdatePanel 必须在 Local 组里，否则这一步会去下载"更新界面"本身，
        // 首次安装时既没网络反馈也没有界面可看。
        // Key / 层级 / 缓存策略都写在 UpdatePanel 的 [UIPanelInfo] 上，这里不用重复传。
        _updatePanel = await UI.OpenAsync<UpdatePanel>();

        if (_updatePanel == null)
            Debug.LogWarning("[GameLauncher] UpdatePanel 打开失败，更新过程将没有界面反馈。请确认它在 Local 组内。");

        SetStatus("正在检查资源更新...");
        await AddressablesManager.Instance.UpdateCatalogsAsync();

        long updateSize = await AddressablesManager.Instance.GetDownloadSizeAsync(updateLabel);

        if (updateSize > 0)
        {
            if (!await ConfirmDownloadAsync(updateSize))
            {
                Debug.Log("[GameLauncher] 用户取消了更新");
                QuitGame();
                return;
            }

            SetStatus("准备下载...");

            bool success = await AddressablesManager.Instance.DownloadUpdateAsync(
                updateLabel,
                (downloaded, total) =>
                {
                    if (_updatePanel == null)
                        return;

                    float progress = total > 0 ? (float)downloaded / total : 0f;
                    _updatePanel.UpdateProgress(progress, total);
                });

            if (!success)
            {
                SetStatus("下载失败，请检查网络后重启游戏");
                return;
            }

            // 只有下载成功才落版本号，失败时下次启动会重新更新
            if (check != null && !string.IsNullOrEmpty(check.latestResVersion))
                MultiServerClient.Resources.SaveLocalResVersion(check.latestResVersion);

            SetStatus("更新完成，正在进入游戏...");
        }
        else
        {
            if (check != null && !string.IsNullOrEmpty(check.latestResVersion))
                MultiServerClient.Resources.SaveLocalResVersion(check.latestResVersion);

            SetStatus("已经是最新版本");
        }

        await UniTask.Delay(500);

        await UI.CloseAsync<UpdatePanel>();
        _updatePanel = null;

        await UI.OpenAsync<MainMenuPanel>(new UIContext().Set("title", "主菜单"));
    }

    /// <summary>
    /// 把服务器下发的地址写进 Addressables 的运行时变量。必须在任何 Addressables 调用之前完成。
    /// </summary>
    private static void ApplyRemoteUrl(string baseUrl)
    {
        baseUrl = baseUrl.TrimEnd('/');

        ResourceApi.SetRemoteLoadUrl(baseUrl);

        // Addressables 会把 {变量} 的求值结果缓存下来，先清掉以防它在别处被提前求值成空串。
        // 直接写缓存而不是依赖反射读取属性，也顺便绕开了 IL2CPP 托管代码裁剪的风险。
        AddressablesRuntimeProperties.ClearCachedPropertyValues();
        AddressablesRuntimeProperties.SetPropertyValue(RemoteUrlVariable, baseUrl);

        Debug.Log($"[GameLauncher] Addressables 远程地址 = {baseUrl}");
    }

    /// <summary>
    /// 决定这次启动用哪个远程地址。返回 false 表示无法继续，原因已经提示给玩家。
    /// </summary>
    private bool TryResolveRemoteUrl(ResourceCheckResult check, out string baseUrl)
    {
        baseUrl = null;

        // 服务器不可达 —— 退回上次成功的地址，Bundle 走本地缓存。
        // 但服务器明确拒绝（AppKey 错、项目禁用…）时不兜底，否则配置错误会被悄悄掩盖。
        if (check == null)
        {
            string cached = ResourceApi.CachedBaseUrl;

            if (_serverRejected || !allowOfflineFallback || string.IsNullOrEmpty(cached))
            {
                SetStatus(_checkError ?? "无法连接服务器，请检查网络后重启游戏");
                return false;
            }

            baseUrl = cached;
            Debug.LogWarning($"[GameLauncher] 服务器不可达，使用上次的资源地址离线启动：{baseUrl}");
            return true;
        }

        // updateType = none 时服务器仍会下发 baseUrl；为空说明该平台压根没有已发布版本
        if (string.IsNullOrEmpty(check.baseUrl))
        {
            string platform = ResourceApi.GetDefaultPlatform();
            SetStatus($"服务器上没有 {platform} 平台的已发布资源版本");
            Debug.LogError(
                $"[GameLauncher] 服务器未下发 baseUrl（platform = {platform}）。" +
                $"请在管理后台上传该平台的 ServerData 并发布。");
            return false;
        }

        baseUrl = check.baseUrl;
        return true;
    }

    private async UniTask<ResourceCheckResult> CheckResourceWithRetryAsync()
    {
        int maxAttempts = Mathf.Max(1, checkRetryCount);

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                SetStatus(attempt == 1
                    ? "正在检查资源版本..."
                    : $"连接失败，正在重试（{attempt}/{maxAttempts}）...");

                var result = await MultiServerClient.Resources.CheckAsync();

                Debug.Log(
                    $"[GameLauncher] 平台 = {ResourceApi.GetDefaultPlatform()}，" +
                    $"App 版本 = {Application.version}，" +
                    $"本地资源版本 = {MultiServerClient.Resources.LocalResVersion ?? "无"} → " +
                    $"updateType = {result.updateType}，最新版本 = {result.latestResVersion}，baseUrl = {result.baseUrl}");

                return result;
            }
            catch (MultiServerException e) when (e.Code != -1)
            {
                // 业务错误（AppKey 无效、平台无版本…），重试没有意义
                _serverRejected = true;
                _checkError = $"服务器返回错误 {e.Code}：{e.Message}";
                Debug.LogError($"[GameLauncher] {_checkError}");
                return null;
            }
            catch (Exception e)
            {
                _checkError = "无法连接服务器，请检查网络后重启游戏";
                Debug.LogWarning($"[GameLauncher] 第 {attempt} 次检查资源更新失败：{e.Message}");
            }

            if (attempt < maxAttempts)
                await UniTask.Delay(1000);
        }

        return null;
    }

    private async UniTask TryLoginAsync()
    {
        try
        {
            SetStatus("正在登录...");

            var auth = await MultiServerClient.Auth.TryRefreshAsync()
                       ?? await MultiServerClient.Auth.GuestLoginAsync();

            Debug.Log($"[GameLauncher] 登录成功：{auth.nickname}（id = {auth.userId}，游客 = {auth.isGuest}）");
        }
        catch (Exception e)
        {
            // 热更只需要 AppKey，登录失败不阻断启动
            Debug.LogWarning($"[GameLauncher] 登录失败，继续走热更流程：{e.Message}");
        }
    }

    private async UniTask ShowForceUpdateAsync(ResourceCheckResult check)
    {
        SetStatus("需要下载新版本安装包");

        bool hasUrl = !string.IsNullOrEmpty(check.appDownloadUrl);

        await UI.ShowMessageBoxAsync(new MessageBoxRequest
        {
            Title = "需要更新",
            Message = string.IsNullOrEmpty(check.notes)
                ? "当前版本过低，请下载最新安装包后再进入游戏。"
                : check.notes,
            ConfirmText = hasUrl ? "前往下载" : "退出",
            Style = MessageBoxStyle.Confirm
        });

        if (hasUrl)
            Application.OpenURL(check.appDownloadUrl);

        QuitGame();
    }

    private async UniTask<bool> ConfirmDownloadAsync(long sizeBytes)
    {
        if (!askBeforeDownload)
            return true;

        float sizeMb = sizeBytes / (1024f * 1024f);

        return await UI.ShowMessageBoxAsync(new MessageBoxRequest
        {
            Title = "发现新版本",
            Message = $"需要下载 {sizeMb:F2} MB 更新资源，是否继续？",
            ConfirmText = "继续",
            CancelText = "取消",
            Style = MessageBoxStyle.ConfirmCancel
        });
    }
#endif

    /// <summary>
    /// UpdatePanel 打开之前状态文字走场景里的静态 Text，之后走面板自己的
    /// </summary>
    private void SetStatus(string message)
    {
        if (_updatePanel != null)
            _updatePanel.SetStatusText(message);
        else if (bootStatusText != null)
            bootStatusText.text = message;

        Debug.Log($"[GameLauncher] {message}");
    }

    private static void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
