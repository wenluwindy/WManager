# WManager UI 框架

基于 UniTask 的 Unity UI 框架：面板生命周期、页面栈、弹窗、层级、缓存、横竖屏与安全区适配。
资源加载走 `IAssetLoader` 抽象，默认接 Addressables，可替换成 Resources / AssetBundle。

---

## 1. 快速开始

三步就能打开第一个界面。

**第一步：写面板类。** 用菜单 `Tools/WManager/UI/新建 UI 面板` 可以一键生成脚本 + 预制体 + Addressables 条目，也可以手写：

```csharp
using UnityEngine;
using UnityEngine.UI;
using WManager;

[UIPanelInfo(Key = "UI/ShopPanel", Layer = UILayer.Normal, Cache = true)]
public class ShopPanel : UIPanel
{
    [SerializeField] private Button closeButton;

    protected override void OnCreate()
    {
        BindClick(closeButton, Close);
    }
}
```

**第二步：做预制体。** 根节点挂上这个脚本，把 `Key` 注册到 Addressables。想要出场动画就再挂一个 `UITweenTransition`。

**第三步：打开。**

```csharp
await UI.OpenAsync<ShopPanel>();
```

不需要传 key、层级、缓存策略 —— 这些都写在面板类自己的 `[UIPanelInfo]` 上了。

---

## 2. 目录结构

```
UI/
├── Core/               框架核心
│   ├── UI.cs                     静态门面，业务侧的推荐入口
│   ├── UIManager.cs              管理器，实现 IUIService
│   ├── UIPanel.cs                面板基类
│   ├── UIPanelGeneric.cs         UIPanel<TArgs>，强类型参数
│   ├── UIPanelInfoAttribute.cs   面板属性声明
│   ├── UIRoot.cs                 Canvas / 层级 / 安全区 / 输入拦截
│   ├── UISettings.cs             全局配置 ScriptableObject
│   ├── UIContext.cs              弱类型传参
│   ├── UIPopupMask.cs            弹窗遮罩
│   ├── UIInputBlocker.cs         过渡期输入拦截
│   ├── IUITransition.cs          过渡动画抽象
│   └── IUIService.cs             服务接口
├── Transition/         UITweenTransition（DOTween）/ UIAnimatorTransition（Animator）
├── MessageBox/         通用消息框
├── PageLayoutSwitch/   横竖屏双布局与安全区
├── Widgets/            Toast / Loading
└── Editor/             新建面板向导、运行时调试器、引用自动绑定
```

示例代码在 `WManager/Samples/UI/`，框架本身不依赖它。

---

## 3. 面板声明：`[UIPanelInfo]`

面板的固有属性写在类上一次，调用侧就不用重复传参了。

```csharp
[UIPanelInfo(
    Key = "UI/SettingsPanel",       // 资源 Key，留空则为 UISettings.defaultKeyPrefix + 类名
    Layer = UILayer.Popup,          // 层级
    ShowMode = UIShowMode.Popup,    // 显示模式
    Cache = true,                   // 关闭后保留实例
    MultiInstance = false,          // 是否允许同时存在多个实例
    Persistent = false,             // 常驻，不被缓存回收
    Mask = UIMaskMode.Auto,         // 遮罩策略
    CloseOnMaskClick = true,        // 点遮罩关闭
    CloseOnBackKey = true)]         // Esc / 返回键关闭
```

不加特性也能用，按约定推导：`Key = "UI/" + 类名`、`Layer = Normal`、`Cache = true`、`ShowMode = Normal`。

个别调用要覆盖时，显式参数优先级更高：

```csharp
await UI.OpenAsync<SettingsPanel>(layer: UILayer.Top, cache: false);
```

**层级**（渲染顺序由低到高）：`Bottom` 背景 → `Normal` 页面 → `Popup` 弹窗 → `Top` 顶层常驻 → `Tips` 飘字。
`Bottom` 默认在安全区之外，全屏背景会铺满刘海区域。

**显示模式**：`Normal` 普通、`Stack` 参与页面栈（由 `PushAsync` 自动设置）、`Popup` 弹窗（自动垫遮罩、响应返回键）。

---

## 4. 生命周期

```
OnCreate                首次实例化，只调一次。绑按钮、订阅事件。
OnWillOpen(ctx)         打开前，过渡动画开始之前。刷新数据。
OnDidOpen(ctx)          打开后，过渡动画播完。
OnPause / OnResume      被 Push 的新页面覆盖 / 重新回到栈顶。
OnWillClose             关闭前。
OnDidClose              关闭后。被覆盖的隐藏页面同样会收到。
OnHidden                缓存面板隐藏完成（Cache = true）。
OnRelease               实例即将销毁（Cache = false）。
OnBackPressed           Esc / 返回键，返回 true 表示已自行处理。
```

过渡动画被后续操作打断时，本次打开或关闭会停在动画处，不再回调 `OnDidOpen` / `OnDidClose`，由接管的那次操作走完自己的流程。所以快速连点不会出现回调乱序或 `await` 永远不返回。

---

## 5. 打开、关闭与页面栈

```csharp
// 打开
await UI.OpenAsync<ShopPanel>();
await UI.OpenAsync<ShopPanel>(new UIContext().Set("tab", 2));

// 关闭
await UI.CloseAsync<ShopPanel>();      // 关最上面的一个
await UI.CloseAsync(panelInstance);     // 关指定实例（多实例面板用这个）
await UI.CloseAllAsync();
Close();                                // 面板内部关自己

// 页面栈
await UI.PushAsync<Page1>();
await UI.PopAsync();                    // 返回上一页
await UI.PopToRootAsync();              // 返回栈底
await UI.PopToAsync<Page1>();           // 返回到指定页面

UIPanel top = UI.PeekPage();
int depth = UI.PageStackCount;
```

`PushAsync` 会暂停并隐藏当前栈顶（`OnPause` + `SetActive(false)`），`PopAsync` 时自动恢复（`OnResume`）。

Push 一个**已经在栈里**的页面不会重复入栈，而是一路返回到它。想让第 3 页回到第 1 页，直接 `PushAsync<Page1>()` 就行。

用 `CloseAsync` 关掉的如果正好是栈顶页面，效果等同于 `PopAsync`，下一页会自动恢复。

---

## 6. 强类型参数

`UIContext` 用字符串键，打错字只会静默拿到默认值。需要类型安全就继承 `UIPanel<TArgs>`：

```csharp
public class ShopArgs
{
    public int Tab;
    public bool ShowDiscount;
}

[UIPanelInfo(Key = "UI/ShopPanel")]
public class ShopPanel : UIPanel<ShopArgs>
{
    protected override void OnWillOpen(ShopArgs args)
    {
        SelectTab(args?.Tab ?? 0);
    }
}

await UI.OpenAsync<ShopPanel, ShopArgs>(new ShopArgs { Tab = 2 });
await UI.PushAsync<ShopPanel, ShopArgs>(new ShopArgs { Tab = 2 });
```

横竖屏双布局同时想要强类型参数，继承 `UIAdaptivePanel<TArgs>`。

---

## 7. 按钮绑定

`BindClick` 相比自己写 `async void`：异常会被捕获打日志、执行期间自动防重入（按钮置灰）、面板释放时自动解绑。

```csharp
protected override void OnCreate()
{
    BindClick(closeButton, Close);                     // 同步
    BindClick(buyButton, OnClickBuyAsync);             // 异步
    BindClickAll(Close, landscapeBack, portraitBack);  // 一个回调绑多个按钮
}

private async UniTask OnClickBuyAsync()
{
    UI.ShowLoading("购买中...");
    try   { await Server.BuyAsync(); }
    finally { UI.HideLoading(); }
}
```

> 异步回调请传方法名（`OnClickBuyAsync`）而不是 lambda。`() => UI.OpenAsync<X>()` 的返回值是 `UniTask<X>`，会落到同步重载上，拿不到防重入和异常捕获。

---

## 8. MessageBox、Toast、Loading

```csharp
// 消息框，直接 await 拿结果
bool ok = await UI.ShowMessageBoxAsync("提示", "确定要退出吗？", withCancel: true);

var result = await UI.ShowMessageBoxResultAsync(new MessageBoxRequest
{
    Title = "保存提示",
    Message = "要保存当前设置吗？",
    ConfirmText = "保存并退出",
    CancelText = "返回",
    Style = MessageBoxStyle.ConfirmCancel,
});

// 飘字
UI.ShowToast("保存成功");
UI.ShowToast("网络异常", duration: 3f);

// 全屏 Loading，按引用计数，可以安全嵌套
UI.ShowLoading("加载中...");
UI.HideLoading();
UI.HideLoadingAll();    // 异常兜底，强制关掉
```

消息框是多实例面板，同时弹多个会自动叠放，每个 `await` 各自拿到自己那一个的结果。

Toast 和 Loading 的默认样式由代码生成，不需要任何美术资源；颜色、字号、时长在 `UISettings` 里改。
需要完全定制的样式，自己写一个 `UIPanel` 即可。

---

## 9. 弹窗遮罩

`ShowMode = UIShowMode.Popup` 的面板打开时，框架会自动在它下面插一层遮罩，关闭时一起收走。
每个弹窗一层遮罩，所以多个弹窗叠放时层次依然正确。

遮罩颜色和淡入时长在 `UISettings` 里配；某个面板不想要遮罩就写 `Mask = UIMaskMode.Never`，
不想被点空白处关掉就写 `CloseOnMaskClick = false`。

---

## 10. 横竖屏与安全区

**安全区**：`UIRoot` 下有一个 `SafeArea` 容器节点跟随 `Screen.safeArea`，`Normal / Popup / Top / Tips` 四层都在它里面。
`Bottom` 默认在外面，全屏背景才能铺满刘海。给单独的节点做适配可以直接挂 `UISafeAreaFitter`。

> 不要把 `UISafeAreaFitter` 挂在 ScreenSpaceOverlay 的 Canvas 本体上 —— Canvas 会驱动自己的 RectTransform，改 anchor 会被覆盖掉。

**双布局页面**：继承 `UIAdaptivePanel`，在 Inspector 里挂上横屏和竖屏两个布局根节点。

```csharp
[UIPanelInfo(Key = "UI/BagPanel")]
public class BagPanel : UIAdaptivePanel
{
    [SerializeField] private Button landscapeBackButton;
    [SerializeField] private Button portraitBackButton;

    protected override void OnCreate()
    {
        // 两套布局的按钮一次全绑好，之后方向怎么切都不用重新绑定
        BindClickAll(Close, landscapeBackButton, portraitBackButton);
    }

    protected override void OnOrientationChanged(UIOrientation orientation)
    {
        RefreshList();      // 重新排版
    }
}
```

同一时刻只有一套布局是激活的，另一套收不到点击，所以不存在"切换后要重新绑引用"的问题。
需要按方向取不同对象时用 `Pick(landscapeThing, portraitThing)`。

`CanvasScaler.matchWidthOrHeight` 会跟着方向自动切换（横屏偏宽、竖屏偏高），数值在 `UISettings` 里配。

---

## 11. 过渡动画

面板基类只依赖 `IUITransition` 接口，框架核心不绑定任何补间库。

- `UITweenTransition`（DOTween）：缩放 / 淡入淡出 / 位移任意组合，Inspector 里配。
- `UIAnimatorTransition`（Animator）：播 Animator 里的 `Show` / `Hide` 状态。
- 什么都不挂：没有动画，直接显示 / 隐藏。

新的过渡开始时会打断上一段，并且**从当前视觉状态平滑接上**，不会跳回起点。
被打断的那次 `await` 以取消结束，不会永远挂着。

自己实现 `IUITransition` 就能接入任何动画方案。

---

## 12. 全局配置 `UISettings`

菜单 `Tools/WManager/UI/定位 UISettings 配置` 可以定位或创建。
放在任意 `Resources` 目录下、命名为 `UISettings.asset` 即可自动生效；没有配置就用内置默认值，框架零配置也能跑。

可配置项：画布参考分辨率与 match 规则、层级间隔、默认 Key 前缀、安全区策略、遮罩样式、
MessageBox 默认文案（可接本地化）、Toast / Loading 样式、EventSystem 自动创建、过渡期输入拦截、
返回键开关、缓存上限、详细日志。

---

## 13. 缓存与内存

`Cache = true` 的面板关闭后只是隐藏，实例保留，下次打开秒开。

```csharp
await UI.PreloadAsync<ShopPanel>();     // 预加载但不显示

UI.ReleaseCached<ShopPanel>();          // 释放单个（必须处于关闭状态）
UI.ReleaseAllCached();                  // 释放所有缓存面板，切场景时调用
UI.ReleaseAllCached(includePersistent: true);
```

`Persistent = true` 的面板不会被 `ReleaseAllCached` 和缓存上限回收。
`UISettings.maxCachedPanels` 设成大于 0 时启用上限，超出后按最久未使用顺序释放，默认 0 表示不限制。

---

## 14. 返回键

Esc（以及 Android 返回键）的处理顺序：

1. 最上层的打开着的弹窗 —— 先问它 `OnBackPressed()`，没处理且 `CloseOnBackKey = true` 就关掉它；
2. 栈顶页面 —— 问它 `OnBackPressed()`；
3. 栈深大于 1 就 `PopAsync()`；
4. 都没处理，派发 `UI.Manager.OnBackKeyUnhandled`（通常在这里做"再按一次退出游戏"）。

面板重写 `OnBackPressed()` 返回 `true` 就能拦下这一步，比如热更界面不允许中途返回。
整体开关在 `UISettings.enableBackKey`。

---

## 15. 全局事件与解耦

```csharp
UI.Manager.OnPanelOpened += panel => Analytics.Track($"open_{panel.GetType().Name}");
UI.Manager.OnPanelClosed += panel => SoundManager.PlaySfx("ui_close");
UI.Manager.OnPanelPushed += panel => { };
UI.Manager.OnPanelPopped += panel => { };
```

业务模块想要解耦时依赖 `IUIService` 而不是具体类：

```csharp
IUIService ui = UI.Service;
await ui.OpenAsync<ShopPanel>();
```

换资源加载方案（Resources / AssetBundle / 自研）实现 `IAssetLoader` 后注入，
必须在第一次 `OpenAsync` / `PreloadAsync` 之前完成：

```csharp
UI.Manager.SetAssetLoader(new MyAssetLoader());
```

---

## 16. 编辑器工具

菜单都在 `Tools/WManager/UI/` 下。

| 工具 | 作用 |
|---|---|
| 新建 UI 面板 | 填个表，自动生成脚本 + 预制体 + Addressables 条目 |
| UI 调试器 | 运行时查看面板实例、状态、页面栈；一键关闭、清缓存、试 Toast |
| 自动绑定选中面板的 UI 引用 | 按命名约定把 `[SerializeField]` 引用绑到子节点 |
| 定位 UISettings 配置 | 定位或创建全局配置 |

自动绑定的匹配规则：字段名和节点名都拆成词元，去掉类型相关的词元后比较剩下的部分。
所以 `closeButton` 能匹配 `CloseButton` / `btn_Close` / `CloseBtn` / `Close`，
`titleText` 能匹配 `TitleText` / `txt_Title` / `TitleLabel`。找到多个候选时会跳过并给出警告，不会乱猜。
组件右键菜单里也有同样的入口。

---

## 17. 常见问题

**点不动界面。** 检查场景里有没有 `EventSystem`（`UIRoot` 会自动补一个），
以及是不是有面板正在播过渡动画 —— 这段时间输入被 `Blocker` 层挡住了，
可以在 `UISettings.blockInputDuringTransition` 关掉。

**打开失败，日志说实例化不出预制体。** Key 没注册到 Addressables，或者和 `[UIPanelInfo(Key = ...)]` 对不上。

**日志说预制体上没有对应组件。** 面板脚本要挂在预制体的**根节点**上。

**方向切换没反应。** `UIAdaptivePanel` 的两个布局根节点都要在 Inspector 里挂上。
另外如果子类自己写了 `OnEnable` / `OnDisable`，记得调 `base.OnEnable()` / `base.OnDisable()`，否则订阅会断。

**想看运行时到底开了哪些面板。** 用 `Tools/WManager/UI/UI 调试器`，或者 `Debug.Log(UI.Manager.DumpState())`。

**过渡动画期间面板状态。** `IsTransitioning` 为 `true`，`IsOpen` 要等动画播完才变。
需要在动画播完后做事就写在 `OnDidOpen` 里。
