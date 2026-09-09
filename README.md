# WManager

> Unity 通用运行时框架：UI、Addressables、计时器、事件、音频、存档、网络、相机、小地图、调试与常用扩展。
>
> 本文档面向"新加入的开发者或另一个 AI"，目标是**凭这一份 README 在 30 分钟内拼出最小可运行接入代码**。各模块的详细说明文档作为延伸阅读，只在末尾索引。
>
> 维护原则：**源码签名和实际行为优先于本文档**；如发现不一致，以源码为准并 PR 修正。

## 0. 30 秒极简上手

1. 场景里放 `EventSystem`（UI 输入必需）。
2. 打开 `Example/Editor Resources/UI/UIdemo.unity` 看一个完整可跑的场景；引导脚本是 `GameLauncher.cs`，对应面板在 `Example/Editor Resources/UI/Panels/`。
3. 在 Addressables 里把每个面板 Prefab 的 Key 配成与 `[UIPanelInfo(Key = "UI/xxx")]` 一致；`UI/UpdatePanel` 必须放在 **Local 组**（它是热更界面本身）。
4. 配置 `GameLauncher` 上的 `Server Url` / `App Key` / `Update Label` / `Ask Before Download` / `Allow Offline Fallback`，运行即可。

如果只想在自己的工程里接入，跳到第 5 节"最小接入模板"。

## 1. 框架定位

- **轻量**：业务架构零侵入，按需使用任意模块。
- **可组合**：核心服务通过接口抽象：`IAssetLoader`、`IUIService`、`ISaveService`、`ISoundService`、`IHttpClient`、`ICameraController`、`IUITransition`。
- **异步优先**：UI、Addressables、网络、计时器等待接口全部基于 `UniTask`，提供 `CancellationToken`。
- **生命周期集中**：管理器大多继承 `SingletonBehaviour<T>`，自动创建并跨场景保留；提供 `HasInstance` 判活。
- **编辑期/运行时并存**：UI Panel 自动创建工具、UIDebugger、EventTrigger 工具、示例场景与 Prefab。

**统一命名空间：**

```csharp
using WManager;
```

## 2. 环境与依赖

| 依赖 | 版本 / 说明 |
|---|---|
| Unity | `2021+`（`ExampleHelpers.cs` 用 `UNITY_2022_2_OR_NEWER` 判断 `destroyCancellationToken`） |
| UniTask | `WManager.Core.asmdef` 已显式引用 |
| Addressables | `2.x` |
| UGUI | 内置 |
| DOTween | **可选** —— 仅在挂 `UITweenTransition` / `UIPopupMask` 时使用，未挂则面板瞬时显示/隐藏 |
| UnityWebRequest | 内置 |
| TextMeshPro | Toast / Loading / UpdatePanel 用 `TMP_Text` |

前置检查清单：

- ✅ Addressables 已安装并构建过 Catalog
- ✅ 场景中存在 `EventSystem`
- ✅ Resources 根下放了一份 `UISettings.asset`（用 `UI/Editor/UIPanelCreatorWindow` 菜单创建面板时会自动生成；找不到时框架有默认值兜底）

## 3. 目录结构

```text
WManager/
├─ Core/                  SingletonBehaviour / Result / IServiceLocator / DefaultServiceLocator / AsyncCompat
├─ Timer/                 TimerFactory / TimerScheduler / TimerPool / TimeTool + Alarm/Chronometer/Clock/Countdown/EveryFrames/EverySeconds
├─ UI/
│  ├─ Core/               UIManager / UIRoot / UIPanel(+Generic) / UI(静态门面) / UIContext / UILayer / UIShowMode / UIPanelInfo / UIFactory / UIInputBlocker / UIPopupMask / IUITransition / IUIService / UISettings
│  ├─ Transition/         UITweenTransition (DOTween) / UIAnimatorTransition (Animator)
│  ├─ PageLayoutSwitch/   UIAdaptivePanel / UIOrientationBinder / UIOrientationService / UISafeAreaFitter
│  ├─ MessageBox/         MessageBoxPanel / MessageBoxRequest (+MessageBoxStyle / MessageBoxResult)
│  ├─ Widgets/            UIToastService / UILoadingView
│  └─ Editor/             UIPanelCreatorWindow / UIPanelAutoBinder / UIDebuggerWindow
├─ AssetsResource/        AddressablesManager / IAssetLoader / StreamingAssetsLoader
├─ Sound/                 SoundManager / Audio / VolumeController / GlobalVolumeController / ISoundService
├─ Save/                  SaveManager / SaveEncryption / SaveDataClasses(Save_TransformData 等) / ISaveService
├─ Event/                 EventManager / DataGroup / EventsGroup + Editor Window
├─ EventTrigger/          EventTriggerManager + CustomEventTrigger 扩展 + AddBoxCollider 工具
├─ WebRequest/            WebRequest + IHttpClient + HttpCallBackArgs
├─ Camera/                SurroundingCamera / FirstPersonController / RoamCameraController + ICameraController
├─ MiniMap/               MiniMap（世界 X/Z → UI RectTransform 映射）
├─ Debug/                 Debugger + DebugPanel / FpsPanel / MemoryPanel / SystemPanel
├─ Extension/             String / Array / List / Dictionary / Bool / Class / IEnumerable / Queue 扩展
├─ Example/               示例场景 + Prefab + 13 个示例脚本（详见 Example/README.md）
└─ Scene/                 加载图标动画 / 进度条资源
```

## 4. 模块 → 典型调用 一行对照

> UI 一律走 `UI` 静态门面（`UI.OpenAsync<T>` / `UI.PushAsync<T>` / `UI.PopAsync()` / `UI.CloseAsync<T>()` / `UI.ShowMessageBoxAsync` / `UI.ShowToast` / `UI.ShowLoading`）。
> `UIManager.Instance` 只用于框架扩展（如 `SetAssetLoader`）和需要 `IUIService` 接口的低层调用。

| 场景 | 一行代码 |
|---|---|
| 打开普通 UI 页面 | `await UI.OpenAsync<MainMenuPanel>();` |
| 打开带强类型参数 | `await UI.OpenAsync<BagPanel, BagPanelArgs>(new BagPanelArgs { Tab = 0 });` |
| 压栈跳转 | `await UI.PushAsync<PushDemo2Panel>();` |
| 返回上一页 | `await UI.PopAsync();` |
| 关闭指定页 | `await UI.CloseAsync<UpdatePanel>();` |
| 关闭全部 UI | `await UI.CloseAllAsync();` |
| 弹消息框 | `bool ok = await UI.ShowMessageBoxAsync(new MessageBoxRequest { Title = "提示", Message = "确定？", Style = MessageBoxStyle.Confirm });` |
| Toast | `UI.ShowToast("保存成功");` |
| 加载 Sprite | `var icon = await AddressablesManager.Instance.LoadAsync<Sprite>("Icon/Bag");` |
| 释放资源（引用计数 -1） | `AddressablesManager.Instance.Release("Icon/Bag");` |
| 实例化 Prefab | `var go = await AddressablesManager.Instance.InstantiateAsync("FX/Hit", parent);` |
| 释放 Prefab 实例 | `AddressablesManager.Instance.ReleaseInstance(go);` |
| 检查资源更新大小 | `long size = await AddressablesManager.Instance.GetDownloadSizeAsync("default");` |
| 下载资源（带进度） | `await AddressablesManager.Instance.DownloadUpdateAsync("default", (cur, total) => progress = total > 0 ? (float)cur / total : 0f);` |
| 倒计时（先 Launch 再 WaitAsync） | `var t = TimerFactory.Countdown(3f); t.Launch(); await t.WaitAsync(ct);` |
| 周期任务 | `TimerFactory.EverySeconds(2f, () => Tick()).Launch();` |
| 发事件 | `EventManager.EmitEvent("OnPlayerLevelUp");` |
| 监听事件（带优先级） | `EventManager.StartListeningWithPriority("OnPlayerLevelUp", OnUp, priority: 0);` |
| 播放音乐 | `SoundManager.PlayMusic(bgm, volume: 1f, loop: true, persist: true);` |
| 播放音效 | `SoundManager.PlayEff(clickClip);` |
| HTTP GET | `var r = await WebRequest.Instance.GetAsync(url, ct);` |
| HTTP POST JSON | `var r = await WebRequest.Instance.PostJsonAsync(url, json, ct);` |
| 存档（文件 + AES） | `SaveManager.Save(data, "Player/MainSave");` |
| 读档（文件） | `var d = SaveManager.Load<PlayerSaveData>("Player/MainSave");` |
| 存档（PlayerPrefs） | `SaveManager.SaveToPrefs(data, "PlayerQuickSlot");` |
| 注册服务（实例） | `ServiceLocatorExample.Locator.Register<IHttpClient>(WebRequest.Instance);` |
| 注册服务（工厂） | `ServiceLocatorExample.Locator.Register<ILoggerService>(() => new UnityLoggerServiceExample());` |

## 5. 最小接入模板（拷贝即可用）

```csharp
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using WManager;

public class Bootstrap : MonoBehaviour
{
    [SerializeField] private string firstUIPanel = nameof(MainMenuPanel);
    [SerializeField] private float  musicVolume = 0.8f;

    private async void Start()
    {
        // 1. EventSystem 兜底（场景里已有就跳过）
        if (FindAnyObjectByType<EventSystem>() == null)
        {
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();   // 或 InputSystemUIInputModule
            DontDestroyOnLoad(go);
        }

        // 2. 首次访问 UI.Manager 触发 UIManager.Awake → UIRoot.Create()
        _ = UI.Manager;

        // 3. 应用音量
        SoundManager.GlobalMusicVolume = musicVolume;

        // 4. 初始化 Addressables（幂等）
        if (!await AddressablesManager.Instance.InitializeAsync())
        {
            Debug.LogError("[Bootstrap] Addressables 初始化失败");
            return;
        }

        // 5. 打开首屏 UI（用 [UIPanelInfo] 里的默认 Key/Layer/Cache）
        await UI.OpenAsync<MainMenuPanel>();
    }
}
```

> 说明：以上用 `MainMenuPanel`（来自 `Example/Editor Resources/UI/Panels/`）做演示，自己接入时换成继承 `UIPanel` 的业务面板即可。

更复杂的启动（含登录、热更、UpdatePanel 进度条）请直接看 `Example/Editor Resources/UI/GameLauncher.cs`。

## 6. 完整 UI 面板示例

### 6.1 脚本 `MyMainPanel.cs`

```csharp
using UnityEngine;
using UnityEngine.UI;
using WManager;

[UIPanelInfo(Key = "UI/MyMainPanel", Layer = UILayer.Normal, Cache = true)]
public class MyMainPanel : UIPanel
{
    [SerializeField] private Button closeBtn;

    protected override void OnCreate()
    {
        // BindClick 内部自动捕获异常、防重入；面板销毁时自动解绑
        BindClick(closeBtn, OnClickCloseAsync);
    }

    protected override void OnWillOpen(UIContext context)
    {
        string user = context?.Get<string>("user", "World") ?? "World";
        Debug.Log($"Hello, {user}!");
    }

    protected override void OnDidOpen(UIContext context)
    {
        EventManager.StartListening("OnServerReady", OnServerReady);
    }

    protected override void OnDidClose()
    {
        EventManager.StopListening("OnServerReady", OnServerReady);
    }

    private async UniTask OnClickCloseAsync() => await CloseAsync();

    private void OnServerReady()
    {
        Debug.Log("Server ready, UI 可继续后续操作");
    }
}
```

### 6.2 Prefab 结构

```text
MyMainPanel.prefab
└─ MyMainPanel  (挂上面脚本 + RectTransform 自动加)
   ├─ Canvas (自己挂；UIRoot 会处理层级与排序)
   ├─ CloseButton (Button)   ← 拖到 MyMainPanel.closeBtn
   └─ ... 其他子节点 ...
```

把 Prefab 注册到 Addressables Group，Key 与 `[UIPanelInfo(Key = "UI/MyMainPanel")]` 一致。

### 6.3 调用方

```csharp
// 不带参数（用 [UIPanelInfo] 里声明的默认 Key/Layer/Cache）
await UI.OpenAsync<MyMainPanel>();

// 带参数
await UI.OpenAsync<MyMainPanel>(new UIContext().Set("user", "Albert"));
```

> **`UIManager.Instance.OpenAsync<T>(...)` 仍然可用**，但参数顺序为 `context, key, layer, cache, bringToTop, showMode, ct`，新手建议直接走 `UI.OpenAsync<T>()` 让 `[UIPanelInfo]` 当默认配置。

## 7. 页面生命周期（UIPanel）

`UIPanel` 只 `[RequireComponent(typeof(RectTransform))]`；过渡动画是可选的 `IUITransition` 实现（`UITweenTransition` 用 DOTween，`UIAnimatorTransition` 用 Animator），没挂就瞬时显示/隐藏。

| 钩子 | 何时调用 | 适用 |
|---|---|---|
| `OnCreate()` | 仅首次实例化（面板第 1 次被加载时） | 一次性绑定事件、查找子节点 |
| `OnWillOpen(UIContext)` | 每次打开前（过渡动画开始前） | 拿参数、刷新数据 |
| `OnDidOpen(UIContext)` | 每次打开完成（过渡动画结束后） | 注册业务事件、播放音效 |
| `OnPause()` | 被新页面 Push 覆盖时 | 暂停刷新、停掉本页面计时器 |
| `OnResume()` | Push 后返回栈顶时 | 恢复刷新 |
| `OnWillClose()` | 关闭前 | 提示用户保存 |
| `OnDidClose()` | 关闭后 | 解绑业务事件 |
| `OnHidden()` | 缓存面板隐藏完成（`Cache = true`） | 释放重资源 |
| `OnRelease()` | 实例销毁前（`Cache = false`） | 释放所有引用 |
| `protected internal virtual bool OnBackPressed()` | Esc / 返回键；返回 true 拦截 | 防误关（如 `UpdatePanel` 用这个） |

Push 覆盖时只走 `OnPause`；返回栈顶走 `OnWillOpen → OnDidOpen → OnResume`。

## 8. 自定义过渡动画

把下面任一组件挂到 `UIPanel` Prefab 上即可（**可选**）：

- **`UITweenTransition`**：用 DOTween 驱动，可配置 `duration / delay / easeType / enableScale/startScale/targetScale / enableFade / startAlpha/targetAlpha / enableMove / startPosition/targetPosition`。事件 `OnShowStart / OnShowComplete / OnHideStart / OnHideComplete` 由 `UIPanel` 自动驱动。
- **`UIAnimatorTransition`**：用 `Animator` Controller 驱动，自己配置 `showTrigger / hideTrigger`。

不挂任何 transition 组件时，`UIPanel` 走瞬时显示/隐藏（`gameObject.SetActive`），适合不需要动效的页面。

## 9. 注入自定义资源加载器

> `IAssetLoader` 接口当前**只有 3 个方法**：`InstantiateAsync` / `ReleaseInstance` / `PreloadAsync<T>`。`LoadAsync<T>` / `Release` 是 `AddressablesManager` 的实现细节，不要在自定义 loader 里写。

```csharp
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using WManager;

public class ResourcesAssetLoader : IAssetLoader
{
    public async UniTask<GameObject> InstantiateAsync(
        string key,
        Transform parent = null,
        bool worldPositionStays = false,
        CancellationToken cancellationToken = default)
    {
        await UniTask.Yield(cancellationToken);
        var prefab = Resources.Load<GameObject>(key);
        return prefab == null ? null : Object.Instantiate(prefab, parent, worldPositionStays);
    }

    public void ReleaseInstance(GameObject instance)
    {
        if (instance != null) Object.Destroy(instance);
    }

    public async UniTask<bool> PreloadAsync<T>(string key) where T : class
    {
        await UniTask.Yield();
        return Resources.Load<UnityEngine.Object>(key) != null;
    }
}

// 第一次访问 UIManager.Instance 触发初始化，再注入自定义 loader
_ = UIManager.Instance;
UIManager.Instance.SetAssetLoader(new ResourcesAssetLoader());
```

约束：

- `SetAssetLoader` 必须在首次 `OpenAsync` / `PreloadAsync` 之前调用。
- 实现必须保证 `InstantiateAsync/ReleaseInstance` 配对；引用计数由调用方负责。

## 10. 服务定位器

```csharp
using WManager;

var locator = new DefaultServiceLocator();
locator.Register<IHttpClient>(WebRequest.Instance);
locator.Register<ISaveService>(new MySaveAdapter());
locator.Register<IMySingleton>(() => MySingleton.Instance);   // 延迟创建

var http = locator.Require<IHttpClient>();     // 未注册抛 InvalidOperationException
var save = locator.Resolve<ISaveService>();    // 未注册返回 null

locator.Unregister<ISaveService>();
```

`Resolve` vs `Require`：

- **`Resolve<T>`**：未注册返回 null；适合"可选服务"。
- **`Require<T>`**：未注册直接抛 `InvalidOperationException`；业务模块推荐用这个，配置缺失早暴露比晚崩溃好。

适用场景：跨模块共享服务（HTTP、存档、玩家数据、自定义业务服务），避免业务脚本之间相互引用具体类。

## 11. UIContext 数据传递

`UIContext` 内部是字符串 key → object value 的字典；类型由 `Get<T>` 自动强转。`Set` 链式写法。

```csharp
var ctx = new UIContext()
    .Set("tab", 2)
    .Set("title", "背包")
    .Set("user", playerData);

await UI.OpenAsync<BagPanel>(ctx);

// 面板内读取
protected override void OnWillOpen(UIContext ctx)
{
    int tab = ctx.Get<int>("tab", 0);
    string title = ctx.Get<string>("title", "");
    PlayerData user = ctx.Get<PlayerData>("user");

    if (ctx.Has("debug")) { /* ... */ }
}
```

强类型参数推荐用 `UI.OpenAsync<T, TArgs>(args)`，编译期就能查错。

## 12. UI 配置（UISettings）

`UISettings` 是 ScriptableObject，框架启动时通过 `Resources.Load("UISettings")` 读取，**找不到时使用默认值**。用 `UISettings.Override(...)` 注入自定义实例（适用于没有 Resources 的工程）。

| 字段 | 默认 | 说明 |
|---|---|---|
| `referenceResolution` | `(1920, 1080)` | Canvas Scaler 参考分辨率 |
| `landscapeMatch / portraitMatch` | `0.35 / 0.8` | 横竖屏 Match 值 |
| `rootSortingOrder` | `1000` | UI 根起始 sortingOrder |
| `layerSortingStep` | `100` | 每层之间的 sortingOrder 步长 |
| `defaultKeyPrefix` | `"UI/"` | `[UIPanelInfo]` 未指定 Key 时拼到类名前面 |
| `bottomLayerIgnoresSafeArea` | `true` | Bottom 层是否绕过安全区 |
| `enablePopupMask / popupMaskColor / popupMaskFadeDuration / popupCloseOnMaskClick` | `true / 黑半透 / 0.2s / true` | 弹窗遮罩 |
| `messageBoxKey / messageBoxDefaultTitle / messageBoxDefaultConfirmText / messageBoxDefaultCancelText` | 空 / "提示" / "确定" / "取消" | MessageBox |
| `toastDuration / toastMaxVisible / toastFadeDuration / toastBottomOffset / toastBackgroundColor / toastTextColor / toastFontSize` | `2 / 3 / 0.25 / 180 / 黑色82% / 白 / 32` | Toast |
| `loadingDefaultText / loadingBackgroundColor / loadingTextColor / loadingFontSize` | "加载中..." / 半透黑 / 白 / 32 | Loading |

## 13. 场景切换与清理顺序

切场景、登出或退出账号前，按这个顺序清理避免野回调与资源泄漏：

```csharp
using Cysharp.Threading.Tasks;
using WManager;

public async UniTask TeardownAsync()
{
    // 1. 关闭所有 UI 页面（页面内 OnDidClose 自动解绑事件）
    if (UI.IsAlive)
    {
        await UI.CloseAllAsync();
        UI.ReleaseAllCached();   // includePersistent = false；Persistent 面板不会释放
    }

    // 2. 停掉所有计时器
    if (TimerScheduler.HasInstance)
        TimerScheduler.Instance.StopAllAndRemove();

    // 3. 释放所有 Addressables 实例和资源
    if (AddressablesManager.HasInstance)
        AddressablesManager.Instance.ReleaseAll();

    // 4. 停止/淡出音频
    if (SoundManager.HasInstance)
        SoundManager.StopAll(musicFadeOutSeconds: 0.5f);

    // 5. 清空事件总线
    EventManager.StopAll();
    EventManager.DisposeAll();
}
```

补充：

- 上述调用**不会**销毁 `UIManager` / `AddressablesManager` / `SoundManager` / `Debugger` 这些单例 GameObject；它们跨场景保留。
- 整个应用退出时可在 `OnApplicationQuit` 追加 `AddressablesManager.Instance.ReleaseAll()` + `EventManager.DisposeAll()`。
- `WebRequest` 内部用 `UnityWebRequest`，在 `finally` 自动 `Dispose`，业务侧不必手动清理。

## 14. 音量控制

```csharp
using WManager;

// 静态属性读写（便捷；底层走 VolumeController）
SoundManager.GlobalVolume        = 1f;   // 全局音量（与各通道音量相乘）
SoundManager.GlobalMusicVolume   = 0.8f;
SoundManager.GlobalEffsVolume    = 1f;
SoundManager.GlobalUISoundsVolume= 1f;

// 事件订阅（多端 UI 同步用）
VolumeController.VolumeChanged += channel => { /* 同步 Slider */ };
VolumeController.ResetToDefault();    // 全部通道重置为 1

// 暂停/恢复
SoundManager.PauseAllMusic();
SoundManager.ResumeAllMusic();
SoundManager.StopAll();               // 立即停止
SoundManager.StopAll(musicFadeOutSeconds: 1.5f);   // 带淡出

// 在 Inspector 上挂 GlobalVolumeController 组件可手动拖动
```

**`实际播放音量 = GlobalVolume × 通道音量`**，只调一个通道不会"静音全部"；要静音全部请设 `GlobalVolume = 0`。

## 15. AI 必读：常见错误与源码限制

> 新接入者（含 AI）第一次用这个框架时最容易踩的坑。

1. **不要手动在场景挂 `UIRoot` Prefab。** 首次访问 `UI.Manager` / `UIManager.Instance` 会触发 `Awake → UIRoot.Create()`；重复创建会出错。`UIManager` 没有公开的 `Create()` 静态方法，走 `SingletonBehaviour<T>.Instance` 懒加载。
2. **场景里必须放 `EventSystem`。** 否则 UGUI 点击、EventTrigger、UIPanel 按钮全部失效。
3. **`UI.OpenAsync<T>()` 失败返回 `null`。** 业务侧必须判空，不要 `.GetComponent` 强转。
4. **`AddressablesManager.InstantiateAsync` 创建的对象必须 `ReleaseInstance`。** 直接 `Destroy` 会导致 Addressables 内部计数错乱。
5. **`EventTriggerManager.RemoveAllEvent(go)` 会清空该 GameObject 上的整个 `EventTrigger` 组件**，包括 Inspector 或其他脚本添加的条目；只想清本管理器的事件请用 `RemoveEvent`。
6. **`EventTriggerManager.AddEvent` / `RemoveEvent` 必须传相同的方法引用**（UnityAction / UnityAction<BaseEventData>）；lambda 传不同实例解绑不到，保存引用或改用具名方法。
7. **`EventManager.EmitEvent(name, delay)` 是 fire-and-forget。** `StopListening` 不会撤销延迟发射；如需取消请 `PauseListening(name)`。
8. **Timer `WaitAsync()` 必须在 `Launch()` 之后调用。** 否则会卡住直到 `CancellationToken`。
9. **`TimerPool.Return<T>` 之前不要手动 `Stop`。** 池内部会 Stop；重复 Stop 是 no-op 但浪费。
10. **`SaveManager.Load<Transform>` / `Load<RectTransform>` 会创建临时 GameObject。** 用完务必 `Destroy`；或者直接 `Load<Save_TransformData>` 自行应用，避免临时对象。
11. **`WebRequest.PostJsonAsync` 直接发送原始 JSON。** 不要 `JsonUtility.ToJson` 包一层 `{"value": ...}`，那是旧版本行为。
12. **`AddressablesManager.InitializeAsync` 是幂等的。** 重复调用安全，不要自己加 `bool inited` 标志。
13. **`UIPanel.OnCreate` 只在首次实例化调用。** 不要把每次打开都要做的逻辑放这里；放 `OnWillOpen` / `OnDidOpen`；被 Push 回来的页面放 `OnResume`。
14. **`UIAdaptivePanel` 在方向切换后会回调 `OnOrientationChanged`。** 想拿当前方向用 `CurrentOrientation`（`Landscape` / `Portrait`）；同一时刻只有一套布局是激活的，`OnCreate` 里用 `BindClickAll` 一次性绑好所有按钮即可。
15. **`Debugger` 用 OnGUI + `FindObjectsByType(Transform)`。** 物体很多时层级面板会卡顿，正式包不要保留。
16. **所有 `SingletonBehaviour<T>` 单例在退出阶段停止创建。** `OnApplicationQuit` 中访问 `Instance` 可能为 `null`，先用 `HasInstance` 判活。
17. **`UI.PushAsync` 内部已自动处理页面栈。** 不要手动 `OpenAsync<UIShowMode.Stack>` 重复压栈；同一页面已在栈里时再 `PushAsync` 会直接返回到该页。
18. **`MessageBoxRequest` 默认使用 `MessageBoxStyle.Confirm`（只有确定按钮）。** 要"确定+取消"设 `Style = MessageBoxStyle.ConfirmCancel`。
19. **`VolumeController.VolumeChanged` 事件只通知通道，不带旧值。** 要 old/new 请自行缓存。
20. **`UIAdaptivePanel<TArgs>` 的 `OnWillOpen(args)` 接收强类型参数，`UIPanel.OnWillOpen(UIContext context)` 接收字典。** 混用会编译不过。
21. **`IAssetLoader` 接口只有 `InstantiateAsync / ReleaseInstance / PreloadAsync<T>` 三个方法。** 不要写 `LoadAsync<T>` / `Release` / `IsLoaded`，那是 `AddressablesManager` 的私有实现。

## 16. 生命周期与资源管理规范

- 事件监听必须配对注销（`OnDisable` / `OnDestroy`）。
- Addressables `Load/Release`、`InstantiateAsync/ReleaseInstance` 必须成对；实例化对象不得直接 `Destroy`。
- UI 关闭时取消请求、计时器和方向订阅。
- 对象池对象在 `OnEnable` 绑定、`OnDisable` 清理。
- 计时器停止后再归还 `TimerPool`。
- 网络请求始终处理错误、超时和取消（`CancellationToken` 透传到最底层）。
- 调试器、编辑器工具和示例资源不要无意带入正式场景。
- 切换账号或退出游戏时按第 13 节顺序清理。

## 17. 维护说明

新增模块建议：

- 公共能力抽象为接口；
- 异步方法提供 `CancellationToken`；
- 明确创建、使用、释放的所有权；
- 在本 README 与对应模块文档同步更新；
- API 改动后先修正源码注释与示例，再更新文档。

---

**文档范围**：`Assets/Scripts/WManager/` 目录当前源码与随附示例资源。
**维护原则**：源码签名、序列化字段和实际运行行为优先于历史说明文档。
