using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using WManager;

/// <summary>页面栈示例第三页</summary>
[UIPanelInfo(Key = "UI/PushDemo3Panel", Layer = UILayer.Normal, Cache = true)]
public class PushDemo3Panel : UIPanel
{
    [SerializeField] private Button demo1Btm;
    [SerializeField] private Button demo2Btm;
    [SerializeField] private Button closeBtn;

    protected override void OnCreate()
    {
        BindClick(demo1Btm, GoToDemo1Async);
        BindClick(demo2Btm, GoToDemo2Async);
        BindClick(closeBtn, Pop);
    }

    private async UniTask GoToDemo1Async() => await UI.PushAsync<PushDemo1Panel>();

    private async UniTask GoToDemo2Async() => await UI.PushAsync<PushDemo2Panel>();
}
