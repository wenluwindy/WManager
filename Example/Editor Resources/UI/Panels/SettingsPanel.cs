using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using WManager;

/// <summary>
/// 弹窗示例。ShowMode = Popup 时框架会自动在下方垫一层遮罩，
/// 关闭前用 MessageBox 做二次确认。
/// </summary>
[UIPanelInfo(
    Key = "UI/SettingsPanel",
    Layer = UILayer.Popup,
    ShowMode = UIShowMode.Popup,
    Cache = true,
    CloseOnMaskClick = false)]
public class SettingsPanel : UIPanel
{
    [SerializeField] private Button closeButton;

    protected override void OnCreate()
    {
        BindClick(closeButton, OnClickCloseAsync);
    }

    private async UniTask OnClickCloseAsync()
    {
        bool save = await UI.ShowMessageBoxAsync(new MessageBoxRequest
        {
            Title = "保存提示",
            Message = "要保存当前设置吗？",
            ConfirmText = "保存并退出",
            CancelText = "返回",
            Style = MessageBoxStyle.ConfirmCancel
        });

        if (!save)
            return;

        await CloseAsync();
        UI.ShowToast("设置已保存");
    }
}
