using UnityEngine;
using UnityEngine.UI;
using WManager;

/// <summary>背包的打开参数</summary>
public class BagPanelArgs
{
    /// <summary>默认选中的页签</summary>
    public int Tab;
}

/// <summary>
/// 横竖屏双布局示例。
///
/// 要点：两套布局的按钮在 OnCreate 里一次性全部绑好，
/// 之后方向怎么切都不用重新绑定 —— 同一时刻只有一套布局是激活的。
/// </summary>
[UIPanelInfo(Key = "UI/BagPanel", Layer = UILayer.Normal, Cache = true)]
public class BagPanel : UIAdaptivePanel<BagPanelArgs>
{
    [Header("横屏")]
    [SerializeField] private Button landscapeBackButton;

    [Header("竖屏")]
    [SerializeField] private Button portraitBackButton;

    private int _tab;

    protected override void OnCreate()
    {
        BindClickAll(Close, landscapeBackButton, portraitBackButton);
    }

    protected override void OnWillOpen(BagPanelArgs args)
    {
        _tab = args?.Tab ?? 0;
        Refresh();
    }

    protected override void OnOrientationChanged(UIOrientation orientation)
    {
        Refresh();
    }

    protected override void OnResume()
    {
        Refresh();
    }

    private void Refresh()
    {
        Debug.Log($"[背包] 刷新视图：方向 = {CurrentOrientation}，页签 = {_tab}");
    }
}
