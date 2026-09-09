# WManager 框架使用示例

本目录是 WManager 框架各模块的**可编译、可运行**示例代码。
所有示例均位于 `Assets/Scripts/WManager/Example/Editor Resources/` 下，按模块分子目录：

- `Scripts/`：逻辑脚本（计时器、事件、网络、扩展等）
- `UI/GameLauncher.cs` + `UI/Panels/`：UI 启动流程 + 各个示例面板
- `Save/`：存档系统示例 + 示例数据类
- `Editor Resources/` 下的 `Audio/`、`Image/`、`Model/`、`Editor Resources/Prefabs/`：示例用美术资源

> 所有示例均以 `WManager.Example` 命名空间组织，避免与工程内业务类名冲突。
> API 签名、命名规则、生命周期顺序与源码 1:1 对齐，建议作为项目接入的参考实现。

## 目录索引

### `Scripts/` —— 框架逻辑示例

| 文件 | 覆盖模块 | 关键演示 |
|---|---|---|
| `ExampleHelpers.cs` | 兼容 | `GetDestroyToken()`：Unity 2022.2+ 用 `destroyCancellationToken`，2021 退回 UniTask 的 `GetCancellationTokenOnDestroy()` |
| `TimerExamples.cs` | TimerFactory / TimerBase / TimerScheduler / TimerPool / TimeTool | Countdown / EverySeconds / Clock / Alarm / NextFrame，链式 `OnLaunch/OnExecute/OnPause/OnResume/OnStop/StopWhen`，`await WaitAsync()`，对象池 `Get<T>` / `Return`，`TimerScheduler.PauseAll/ResumeAll/StopAllAndRemove`，`TimeTool.ToStandardTimeFormat/ToHMSFTimeFormat/Convert2Seconds/GetTimeStamp` |
| `EventExamples.cs` | EventManager / EventsGroup | `StartListening/StopListening`、`StartListeningWithPriority`、`EmitEvent(name, sender, delay)`、`EmitEventData` + `GetSender`/`GetInt`、`PauseListening/RestartListening`、`EventsGroup.Add/StartListening/StopListening`、`StopAll/DisposeAll`，以及"单例管理器订阅全局事件"模式 |
| `SoundExamples.cs` | SoundManager / VolumeController / GlobalVolumeController | Music / Eff / UISound 三通道播放与暂停恢复，`GlobalVolume/GlobalMusicVolume/GlobalEffsVolume/GlobalUISoundsVolume` 静态属性，`VolumeController.VolumeChanged` 事件订阅，`StopAll(musicFadeOutSeconds)` |
| `AssetExamples.cs` | AddressablesManager / IAssetLoader | `InitializeAsync/LoadAsync/Release`、`InstantiateAsync/ReleaseInstance`、`UpdateCatalogsAsync/GetDownloadSizeAsync/DownloadUpdateAsync`、`ReleaseAllAssets/ReleaseAllInstances/PrintDebugInfo`，自定义 `IAssetLoader` 通过 `UIManager.Instance.SetAssetLoader(...)` 注入（Resources 兜底） |
| `SaveExamples.cs` | SaveManager | 文件存档 `Save/Load/Exists/DeleteData`（自动 AES 加密）、PlayerPrefs `SaveToPrefs/LoadFromPrefs/ExistsInPrefs/DeletePref`、异步 `SaveAsync/LoadAsync` + `CancellationToken`，附带"物品解锁"业务示例 |
| `Save/PlayerSaveData.cs` | SaveManager 数据契约 | `[Serializable]` 数据类样本：必须 public 字段、`JsonUtility` 支持的类型 |
| `WebRequestExamples.cs` | WebRequest / IHttpClient | `GetAsync`、`PostJsonAsync`（原始 JSON）、`HttpRetry/HttpRetryInterval/HttpTimeout` 全局配置、CancellationToken 取消、`ServiceLocator.Resolve<IHttpClient>()` 解耦调用 |
| `ServiceLocatorExamples.cs` | IServiceLocator / DefaultServiceLocator / `Result<T>` | `Register<T>(instance)` / `Register<T>(Func<T>)` 工厂、`Resolve`（未注册返回 null）/ `Require`（未注册抛 `InvalidOperationException`）/ `Unregister`，以及 `Result<T>.Success/Failure/Match` 用法 |
| `SceneTransitionExample.cs` | 场景切换 + WManager 清理 | `GoToSceneAsync` 标准清理顺序：`UI.CloseAllAsync + ReleaseAllCached → TimerScheduler.StopAllAndRemove → AddressablesManager.ReleaseAll → SoundManager.StopAll → EventManager.StopAll/DisposeAll`，包含一个嵌套的 `InGameHUDExample` UIPanel 演示 |
| `CameraAndMiniMapExample.cs` | SurroundingCamera / MiniMap | 主相机自动加 `SurroundingCamera` + `ChangeTarget`、链式 `MiniMap.SetBase/SetMapRT/SetTarget3D/SetTarget2D` |
| `EventTriggerExample.cs` | EventTriggerManager | 主相机加 `PhysicsRaycaster`、`AddEvent(物体, EventTriggerType, 回调)` 注册 `PointerClick/PointerEnter/PointerExit/BeginDrag/Drag`、`RemoveEvent` / `RemoveAllEvent` |
| `ExtensionExamples.cs` | StringExtension / ArrayExtension / ListExtension / DictionaryExtension / BoolExtension / ClassExtension | 真实存在的扩展方法：`SplitString/ToBool/ToInt/ToLong/ToMD5String/ToSHA1String/ToSHA256String/Base64Encode/Base64Decode/ToFirstUpperStr/ToFirstLowerStr/AESEncrypt/AESDecrypt/RandomString`、`Array.ForEach/ForEachReverse/Merge/SortBubble`、`List.TryAdd/AddIfNotContains/ForEach/ForEachReverse`、`Dictionary.ForEach/AddRange(isOverride)`、`bool.Execute(action)` / `bool.Execute(trueAction, falseAction)`、`object.Execute(action)` |

### `UI/` —— UI 启动 + 面板示例

| 文件 | 覆盖模块 | 关键演示 |
|---|---|---|
| `GameLauncher.cs` | 启动流程（SDK + 热更） | 阶段一纯 HTTP：`MultiServerClient.Init → 游客登录 → CheckAsync（带重试）`；阶段二：`ResourceApi.SetRemoteLoadUrl` + `AddressablesRuntimeProperties.SetPropertyValue` → `AddressablesManager.InitializeAsync` → 打开 `UpdatePanel` → `UpdateCatalogsAsync` → `GetDownloadSizeAsync` → `DownloadUpdateAsync` → `CloseAsync<UpdatePanel>` → `OpenAsync<MainMenuPanel>`；含 `ShowForceUpdateAsync`、`ConfirmDownloadAsync` 弹窗、离线兜底 |
| `Panels/MainMenuPanel.cs` | UIPanel / UIContext / MessageBox / Addressables | `[UIPanelInfo]` 声明 Key/Layer/Cache；`OnCreate` 用 `BindClick` 绑定按钮；`OnDidOpen(UIContext)` 读取 `context.Get("title", "默认")`；演示 `UI.PushAsync<T>`、`UI.OpenAsync<T>`、`UI.OpenAsync<T, TArgs>(强类型参数)`、`UI.ShowMessageBoxAsync`、`AddressablesManager.InstantiateAsync` 加载模型 |
| `Panels/PushDemo1Panel.cs` / `PushDemo2Panel.cs` / `PushDemo3Panel.cs` | 页面栈（Push/Pop） | 演示 `UI.PushAsync<T>()` 与 `Pop()` 跳转；同一个 `PushAsync<T>` 目标已在栈中时会直接返回到该页 |
| `Panels/SettingsPanel.cs` | 弹窗 / MessageBox / Toast | `[UIPanelInfo(ShowMode = UIShowMode.Popup)]` 框架自动加遮罩；`OnCreate` 绑按钮 → `OnClickCloseAsync` 用 `MessageBoxRequest` 二次确认 → `CloseAsync` → `UI.ShowToast("设置已保存")` |
| `Panels/BagPanel.cs` | UIAdaptivePanel\<TArgs\> | 横竖屏双布局：`landscapeBackButton/portraitBackButton` 两套按钮 `BindClickAll` 一次性绑定；`OnWillOpen(BagPanelArgs args)` 接收强类型参数；`OnOrientationChanged(orientation)` 方向变化时刷新；`OnResume` 被 Push 回来时刷新 |
| `Panels/UpdatePanel.cs` | 热更进度面板 | 必须放 Addressables **Local 组**（否则会去下载"更新界面"本身）；`OnWillOpen` 重置进度条；`UpdateProgress(progress, totalSizeBytes)`；`OnBackPressed() => true` 拦截返回键 |

## 5 分钟跑通

> 真实示例项目请打开 `demo.unity` 或 `UI/UIdemo.unity` 场景，对应的引导脚本是 `GameLauncher`。

1. **准备场景**：打开 `UI/UIdemo.unity`，确认场景里有 `EventSystem`（UI 输入必需）和挂着 `GameLauncher` 的空 GameObject。
2. **配置 Addressables 资源**：在 Addressables Groups 里至少准备好下列 Prefab（Key 与示例面板的 `[UIPanelInfo(Key=...)]` 一一对应）：
   - `UI/MainMenuPanel` / `UI/SettingsPanel` / `UI/BagPanel`
   - `UI/PushDemo1Panel` / `UI/PushDemo2Panel` / `UI/PushDemo3Panel`
   - `UI/UpdatePanel` —— **必须放在 Local 组**
   - 可选：`Model/飞机`（`MainMenuPanel.modelKey`，用来演示运行时加载模型）
3. **配置 GameLauncher**：
   - `Server Url`：MultiServer 地址，结尾不要带 `/`
   - `App Key`：项目 AppKey（**AppSecret 绝对不要写在客户端**）
   - `Update Label`：需要热更的 Addressables Label（默认 `default`）
   - `Ask Before Download`：是否弹窗询问下载
   - `Allow Offline Fallback`：服务器不可达时是否用上次成功的地址离线启动
4. **运行**：阶段一纯 HTTP 登录 → 检查资源版本 → 阶段二初始化 Addressables → 弹 `UpdatePanel` → 下资源 → 进 `MainMenuPanel`。

> 主菜单 Prefab 上**不需要**手动挂 `UIViewController` 或 `CanvasGroup`，那是 README 旧版的过时要求。
> `UIPanel` 只 `[RequireComponent(typeof(RectTransform))]`；过渡动画是可选组件（实现 `IUITransition`），没挂就直接显示/隐藏。

## 模块 → 典型调用 一行对照

> UI 操作一律通过 `UI` 静态门面（`UI.OpenAsync<T>` / `UI.PushAsync<T>` / `UI.PopAsync()` / `UI.CloseAsync<T>()` / `UI.ShowMessageBoxAsync` / `UI.ShowToast`）。
> `UIManager.Instance` 一般只用于框架扩展或自定义 `IAssetLoader` 注入。

| 场景 | 一行代码 |
|---|---|
| 打开普通 UI 页面 | `await UI.OpenAsync<MainMenuPanel>();` |
| 打开带强类型参数 | `await UI.OpenAsync<BagPanel, BagPanelArgs>(new BagPanelArgs { Tab = 0 });` |
| 压栈跳转 | `await UI.PushAsync<PushDemo2Panel>();` |
| 返回上一页 | `await UI.PopAsync();` |
| 关闭指定页 | `await UI.CloseAsync<UpdatePanel>();` |
| 关闭全部 UI | `await UI.CloseAllAsync();` |
| 弹消息框 | `bool ok = await UI.ShowMessageBoxAsync(new MessageBoxRequest { Title = ..., Message = ..., Style = MessageBoxStyle.Confirm });` |
| Toast | `UI.ShowToast("保存成功");` |
| 加载 Sprite | `var s = await AddressablesManager.Instance.LoadAsync<Sprite>("Icon/Bag");` |
| 实例化 Prefab | `var go = await AddressablesManager.Instance.InstantiateAsync("FX/Hit", transform);` |
| 检查资源更新大小 | `long size = await AddressablesManager.Instance.GetDownloadSizeAsync("default");` |
| 下载资源（带进度） | `await AddressablesManager.Instance.DownloadUpdateAsync("default", (d, t) => progress = t > 0 ? (float)d / t : 0f);` |
| 倒计时（先 Launch 再 WaitAsync） | `var t = TimerFactory.Countdown(3f); t.Launch(); await t.WaitAsync(ct);` |
| 周期任务 | `TimerFactory.EverySeconds(2f, () => Tick()).Launch();` |
| 发事件 | `EventManager.EmitEvent("OnPlayerLevelUp");` |
| 监听事件（带优先级） | `EventManager.StartListeningWithPriority("OnPlayerLevelUp", OnUp, priority: 0);` |
| 播放音乐 | `SoundManager.PlayMusic(bgm, volume: 1f, loop: true, persist: true);` |
| 播放音效 | `SoundManager.PlayEff(clickClip);` |
| HTTP GET | `var r = await WebRequest.Instance.GetAsync(url, ct);` |
| 存档（文件 + AES） | `SaveManager.Save(data, "Player/MainSave");` |
| 读档（文件） | `var d = SaveManager.Load<PlayerSaveData>("Player/MainSave");` |
| 存档（PlayerPrefs） | `SaveManager.SaveToPrefs(data, "PlayerQuickSlot");` |
| 注册服务（实例） | `ServiceLocatorExample.Locator.Register<IHttpClient>(WebRequest.Instance);` |
| 注册服务（工厂） | `ServiceLocatorExample.Locator.Register<ILoggerService>(() => new UnityLoggerServiceExample());` |

## 重要约定（踩坑清单）

> 这些都是实测出来的坑，使用前请逐条确认。

1. **场景里必须有 EventSystem。** UGUI 输入、`UIPanel` 按钮、`EventTriggerManager` 全部依赖。
2. **不要手动在场景挂 `UIRoot`。** 首次访问 `UIManager.Instance`（或调用 `UI.Manager`/`UI.Root`）内部会触发 `UIRoot.Create()`；重复创建会出错。`UIManager` 本身没有公开的 `Create()` 静态方法，走 `SingletonBehaviour<T>.Instance` 懒加载路径。
3. **`UI.OpenAsync<T>` 失败返回 `null`。** 调用方必须判空（例：`GameLauncher.cs` 里 `await UI.OpenAsync<UpdatePanel>()` 后检查 `if (_updatePanel == null)`）。
4. **`AddressablesManager.InstantiateAsync` 的对象必须 `ReleaseInstance`。** 不能 `Destroy`。
5. **Addressables 同一 key 不要先用 `Texture2D` 再用 `Sprite` 加载。** 缓存命中后类型不匹配会报错。
6. **`EventManager.EmitEvent(name, delay)` 是 fire-and-forget。** `StopListening` 不会撤销延迟发射；要取消用 `PauseListening`。
7. **Timer `WaitAsync()` 必须在 `Launch()` 之后调用。** 不然会一直挂起到取消（见 `TimerExamples.CountdownWithAwaitAsync`）。
8. **保存自定义数据类必须 `[Serializable]` 且字段 public。** 否则 `JsonUtility` 序列化失败。复杂对象/字典请改用文件存档（AES 加密路径是独立的，不是 `JsonUtility`）。
9. **不要直接访问 `SingletonBehaviour.Instance` 在退出阶段。** `OnApplicationQuit` 中可能为 null，先用 `HasInstance` 判活。
10. **`AddressablesManager.InitializeAsync` 是幂等的。** 重复调用安全。
11. **`UIPanel.OnCreate` 只在首次实例化调用。** 要每次打开做事的逻辑放 `OnWillOpen` / `OnDidOpen`；被 Push 回来的页面放 `OnResume`。
12. **页面生命周期顺序：**
    - 打开：`OnCreate`（仅首次） → `OnWillOpen` → 过渡动画 → `OnDidOpen`
    - 被新页 Push 覆盖：`OnPause`（未隐藏时）
    - 返回栈顶：`OnWillOpen` → `OnDidOpen` → `OnResume`
    - 关闭：`OnWillClose` → 过渡动画 → `OnDidClose`；若 `Cache = true` 还会走 `OnHidden`（面板 SetActive(false)，实例保留）
    - 销毁（`Cache = false`）：`OnRelease`
    - 实际顺序以 `UIPanel.InternalOpenAsync / InternalCloseAsync / InternalPause / InternalResume` 为准。
13. **`UIAdaptivePanel` 在 `OnCreate` 里用 `BindClickAll` 一次绑好所有横竖屏按钮**，方向切换时框架会自动激活对应布局，无需重新绑定。
14. **场景切换前按顺序清理：** 关 UI → 停计时器 → 释放 Addressables → 停音效 → 清事件总线（见 `SceneTransitionExample.GoToSceneAsync`）。
15. **音量是相乘关系：** `实际播放 = GlobalVolume × 通道音量`。只设某通道不会全静音；想全静音设 `GlobalVolume = 0`。`VolumeController.ResetToDefault()` 把全部通道重置为 1。
16. **`SoundManager.StopAll(musicFadeOutSeconds: 1.5f)` 才带淡出**，无参重载是立即停止。
17. **`TimerPool.Return<T>` 之前不要手动 `Stop`。** 池内部会 Stop，重复 Stop 是 no-op 但浪费。
18. **`ServiceLocator.Resolve<T>` 未注册返回 null；`Require<T>` 未注册直接抛 `InvalidOperationException`。** 业务模块建议用 `Require`，配置缺失早暴露比晚崩溃好。
19. **`ServiceLocatorExample.Locator` 是静态默认实现**，整个工程共享一个实例；想要可重置/可替换就换成自己的 `IServiceLocator` 实现。

## 框架未覆盖 → 自行补的常见场景

- 网络层业务封装：建议按 `WebRequestExamples.cs` 的写法包一层"业务 HTTP 接口"，错误码映射成业务异常再往外抛。
- 复杂 UI：把"列表项"、"Toast"、"Loading"等做成 `UIPanel` 子类复用同一套生命周期。
- 存档迁移：版本号机制自己做，`SaveManager` 只负责最底层 IO + 加密。
- 多语言：框架不带，自己加。
- 多端音量同步：`VolumeController.VolumeChanged` 事件 → 自家 Save → 下次启动还原。

## 进一步阅读

- 模块 API 详细说明：根目录 `Assets/Scripts/WManager/*/<Module>README.md`
- 源码即真相：所有示例最终都引用 `Assets/Scripts/WManager/...` 里的源码，遇到疑问先看源码签名
