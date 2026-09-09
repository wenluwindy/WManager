using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using WManager;

/// <summary>
/// 页面栈示例第一页。
///
/// 注意 Push 一个已经在栈里的页面时，框架不会重复入栈，
/// 而是一路返回到那一页。所以在第 3 页点"去第 1 页"会直接退回栈底。
/// </summary>
[UIPanelInfo(Key = "UI/PushDemo1Panel", Layer = UILayer.Normal, Cache = true)]
public class PushDemo1Panel : UIPanel
{
    [SerializeField] private Button demo2Btm;
    [SerializeField] private Button demo3Btm;
    [SerializeField] private Button closeBtn;

    protected override void OnCreate()
    {
        BindClick(demo2Btm, GoToDemo2Async);
        BindClick(demo3Btm, GoToDemo3Async);
        BindClick(closeBtn, Pop);
    }

    private async UniTask GoToDemo2Async() => await UI.PushAsync<PushDemo2Panel>();

    private async UniTask GoToDemo3Async() => await UI.PushAsync<PushDemo3Panel>();
}
