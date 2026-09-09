using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using WManager;

/// <summary>页面栈示例第二页</summary>
[UIPanelInfo(Key = "UI/PushDemo2Panel", Layer = UILayer.Normal, Cache = true)]
public class PushDemo2Panel : UIPanel
{
    [SerializeField] private Button demo1Btm;
    [SerializeField] private Button demo3Btm;
    [SerializeField] private Button closeBtn;

    protected override void OnCreate()
    {
        BindClick(demo1Btm, GoToDemo1Async);
        BindClick(demo3Btm, GoToDemo3Async);
        BindClick(closeBtn, Pop);
    }

    private async UniTask GoToDemo1Async() => await UI.PushAsync<PushDemo1Panel>();

    private async UniTask GoToDemo3Async() => await UI.PushAsync<PushDemo3Panel>();
}
