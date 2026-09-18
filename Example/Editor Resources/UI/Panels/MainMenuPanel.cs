using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using WManager;
#if WMANAGER_MULTISERVER
using MultiServer.Sdk;
#endif

/// <summary>
/// 主菜单示例。演示三种最常见的打开方式：
/// 入栈跳转、普通打开、带强类型参数打开。
/// </summary>
[UIPanelInfo(Key = "UI/MainMenuPanel", Layer = UILayer.Normal, Cache = true)]
public class MainMenuPanel : UIPanel
{
    [Header("按钮")]
    [SerializeField] private Button pushButton;
    [SerializeField] private Button settingButton;
    [SerializeField] private Button bagButton;
    [SerializeField] private Button quitButton;

    [Header("演示：加载一个模型")]
    [Tooltip("模型挂载点。留空则不加载模型。")]
    [SerializeField] private Transform modelRoot;
    [SerializeField] private string modelKey = "Model/飞机";

    protected override void OnCreate()
    {
        // BindClick 会自动捕获异常、执行期间防重入，面板释放时自动解绑
        BindClick(pushButton, OnClickPushAsync);
        BindClick(settingButton, OnClickSettingsAsync);
        BindClick(bagButton, OnClickBagAsync);
        BindClick(quitButton, QuitGame);

        LoadModelAsync().Forget();
    }

    protected override void OnDidOpen(UIContext context)
    {
        string title = context?.Get("title", "主菜单");
        Debug.Log($"[主菜单] 打开完成，title = {title}");

        ShowLatestNoticeAsync().Forget();
    }

    private async UniTask OnClickPushAsync()
    {
        // 页面栈跳转：当前页会被暂停并隐藏，返回时自动恢复
        await UI.PushAsync<PushDemo1Panel>();
    }

    private async UniTask OnClickSettingsAsync()
    {
        // SettingsPanel 声明了 ShowMode = Popup，框架会自动垫一层遮罩
        await UI.OpenAsync<SettingsPanel>();
    }

    private async UniTask OnClickBagAsync()
    {
        // 强类型参数，打错字直接编译不过
        await UI.OpenAsync<BagPanel, BagPanelArgs>(new BagPanelArgs { Tab = 0 });
    }

    /// <summary>拉公告并弹出第一条</summary>
    private async UniTask ShowLatestNoticeAsync()
    {
#if !WMANAGER_MULTISERVER
        await UniTask.CompletedTask;
#else
        List<Notice> notices;

        try
        {
            notices = await MultiServerClient.Notices.GetNoticesAsync();
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[主菜单] 拉取公告失败：{e.Message}");
            return;
        }

        if (notices == null || notices.Count == 0)
            return;

        var notice = notices[0];

        await UI.ShowMessageBoxAsync(new MessageBoxRequest
        {
            Title = notice.title,
            Message = notice.content,
            Style = MessageBoxStyle.Confirm
        });
#endif
    }

    private async UniTaskVoid LoadModelAsync()
    {
        if (modelRoot == null || string.IsNullOrEmpty(modelKey))
            return;

        var model = await AddressablesManager.Instance.InstantiateAsync(modelKey, modelRoot);

        if (model == null)
            Debug.LogWarning($"[主菜单] 模型加载失败：{modelKey}");
    }

    private void QuitGame()
    {
        Debug.Log("[主菜单] 退出游戏");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
