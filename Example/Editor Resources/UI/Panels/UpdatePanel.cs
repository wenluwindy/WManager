using TMPro;
using UnityEngine;
using UnityEngine.UI;
using WManager;

/// <summary>
/// 热更进度界面。
///
/// 这个面板必须放在 Addressables 的 Local 组里：
/// 走到这一步时远程资源还没下载完，放远程组会连"更新界面"本身都下不下来。
/// </summary>
[UIPanelInfo(Key = "UI/UpdatePanel", Layer = UILayer.Popup, Cache = false)]
public class UpdatePanel : UIPanel
{
    [SerializeField] private Slider progressBar;
    [SerializeField] private TMP_Text txtStatus;
    [SerializeField] private TMP_Text txtSize;

    protected override void OnWillOpen(UIContext context)
    {
        if (progressBar != null)
            progressBar.value = 0f;

        SetStatusText("正在检查更新...");

        if (txtSize != null)
            txtSize.text = string.Empty;
    }

    /// <summary>更新下载进度</summary>
    /// <param name="progress">0 ~ 1</param>
    /// <param name="totalSizeBytes">总字节数，传 0 表示不显示体积</param>
    public void UpdateProgress(float progress, long totalSizeBytes = 0)
    {
        if (progressBar != null)
            progressBar.value = progress;

        if (totalSizeBytes <= 0)
            return;

        float totalMb = totalSizeBytes / (1024f * 1024f);
        float downloadedMb = totalMb * progress;

        SetStatusText("正在下载资源...");

        if (txtSize != null)
            txtSize.text = $"{downloadedMb:F2} MB / {totalMb:F2} MB";
    }

    public void SetStatusText(string message)
    {
        if (txtStatus != null)
            txtStatus.text = message;
    }

    /// <summary>更新过程中不允许用返回键中断</summary>
    protected override bool OnBackPressed() => true;
}
