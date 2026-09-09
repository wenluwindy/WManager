# WManager 框架优化实施计划（Claude 实施版）

> 直接适配当前框架，不考虑旧版 API 兼容
> LicenseManager 已迁出，不再处理

---

## 实施原则

1. 每个 commit 对应一个原子任务，可独立回滚
2. 每个阶段完成后跑 `Example/` 示例验证
3. UniTask 强依赖（用户已确认）
4. 命名规范一次性重命名

---

## 任务清单（按优先级排序）

### 🔴 P0：Bug 修复（每个独立 commit）

- [ ] **T01** 修 `Sound/GlobalVolumeController.cs:110` — UI 音量赋值成特效音量的 Bug
- [ ] **T02** 修 `AssetsResourse/AddressablesManager.cs:66-75` — `Init()` 逻辑反向，新实例被 Destroy
- [ ] **T03** 修 `WebRequest/WebRequest.cs:215-247` — `m_CurrRetry` 在成功后不重置；`IsBusy` 跨请求不重置
- [ ] **T04** 修 `Sound/Audio.cs` — `Play()` 中 `Pooled = true` 重置时序错误
- [ ] **T05** 删 `Debug/Debugger.cs` — `#if UNITY_5` / `#if UNITY_7` 死条件
- [ ] **T06** 删 `Event/EventManager.cs` — `IndexedDataGroup.GetObject` 死方法（`return null`）
- [ ] **T07** 删 `Timer/Countdown.cs:29` — 构造函数 `executer` 死参数
- [ ] **T08** 改名 `Extension/StringExtension.cs` — `GetMacAddress()` → `GetPersistentGuid()`（实际读 PlayerPrefs GUID）

---

### 🟠 P1：Core 基础设施（提供后续重构基座）

- [ ] **T09** 新建 `Core/SingletonBehaviour.cs` — 泛型懒汉单例基类
- [ ] **T10** 新建 `Core/Result.cs` — 统一结果类型 `Result<T>`
- [ ] **T11** 新建 `Core/AsyncCompat.cs` — UniTask ↔ Task 桥接
- [ ] **T12** 新建 `Core/WManager.Core.asmdef` — Core 程序集

---

### 🟠 P1：迁移到 SingletonBehaviour（消除 5 种单例写法）

- [ ] **T13** `Timer/TimerManager` → `SingletonBehaviour<TimerManager>`
- [ ] **T14** `Sound/SoundManager` → `SingletonBehaviour<SoundManager>`
- [ ] **T15** `UI/Core/UIManager` → `SingletonBehaviour<UIManager>`
- [ ] **T16** `AssetsResourse/AddressablesManager` → `SingletonBehaviour<AddressablesManager>`
- [ ] **T17** `AssetsResourse/StreamingAssetsLoader` → `SingletonBehaviour<StreamingAssetsLoader>`
- [ ] **T18** `WebRequest/WebRequest` → `SingletonBehaviour<WebRequest>`
- [ ] **T19** `Debug/Debugger` → `SingletonBehaviour<Debugger>`
- [ ] **T20** `Save/SaveManager` — 改为纯静态类（删除 MonoBehaviour）

---

### 🟡 P1：命名规范一次性重命名

- [ ] **T21** 目录 `AssetsResourse/` → `AssetsResource/`，更新所有引用
- [ ] **T22** `Timer/TimeTool.cs` — `TimeUnit.Millsecond` → `TimeUnit.Millisecond`
- [ ] **T23** 目录 `Mini Map/` → `MiniMap/`
- [ ] **T24** 目录 `Camera/First Person Controller/` → `Camera/FirstPersonController/`
- [ ] **T25** 目录 `Camera/Roam Camera Controller/` → `Camera/RoamCameraController/`
- [ ] **T26** `Save/Save_Encryption.cs` → `Save/SaveEncryption.cs`
- [ ] **T27** `Save/Save_DataClasses.cs` → `Save/SaveDataClasses.cs`
- [ ] **T28** `Save/SaveManager.cs` — `SaveWeb/LoadWeb/ExistsWeb/DeleteDataWeb` → `SaveToPrefs/LoadFromPrefs/ExistsInPrefs/DeletePref`
- [ ] **T29** `Extension/StringExtension.cs` — `ToBytes_FromBase64Str` → `ToBytesFromBase64Str`，`ToInt0X` → `ToIntHex`
- [ ] **T30** 新建 `.editorconfig` — 统一代码风格

---

### 🟡 P2：Timer 模块重构（设计最佳，重点打磨）

- [ ] **T31** 新建 `Timer/TimerBase.cs` — 抽象基类，统一 Launch/Stop/Pause/Resume/链式回调
- [ ] **T32** 新建 `Timer/TimeBasedTimerBase.cs` — 抽出 `_beginTime`/`_pausedTime`/`GetCurrentTime()`
- [ ] **T33** 修 `Timer/ITimer.cs` — 加 `IsRunning`；拆 `IForwardTimer.ElapsedTime`
- [ ] **T34** `Timer/Clock` → 继承 `TimeBasedTimerBase`
- [ ] **T35** `Timer/Countdown` → 继承 `TimeBasedTimerBase`，删除 `executer` 死参数
- [ ] **T36** `Timer/EverySeconds` → 继承 `TimeBasedTimerBase`
- [ ] **T37** `Timer/EveryFrames` → 继承 `TimerBase`；`OnExecute` 签名统一为 `UnityAction<float>`
- [ ] **T38** `Timer/Chronometer` → 继承 `TimeBasedTimerBase`，实现 `IForwardTimer`
- [ ] **T39** `Timer/Alarm` → 补 `OnLaunch/OnPause/OnResume` 链式 API 或标注不支持
- [ ] **T40** 拆 `TimerManager` 为 `TimerScheduler`（MonoBehaviour）+ `TimerFactory`（静态类）
- [ ] **T41** `Timer/TimerManager.cs` — `StopAll` / `Clear` 改名 `StopAllAndRemove` / `RemoveAll`

---

### 🟡 P2：God Class 拆分

#### Sound 拆分
- [ ] **T42** 拆 `Sound/Audio.cs`（622 行） → `AudioSourcePool.cs` + `AudioClipCache.cs` + `FadePlayer.cs`
- [ ] **T43** 新建 `Sound/VolumeController.cs` — 4 通道独立控制 + `event VolumeChanged`
- [ ] **T44** 修 `Sound/GlobalVolumeController.cs` — 订阅 `VolumeChanged` 而非每帧轮询
- [ ] **T45** `Sound/SoundManager.cs` — 改为纯 facade

#### Debug 拆分
- [ ] **T46** 拆 `Debug/Debugger.cs`（700+ 行 OnGUI） → `FpsPanel.cs` + `MemoryPanel.cs` + `InputPanel.cs`
- [ ] **T47** 新建 `Debug/DebugPanel.cs` — OnGUI 绘制抽象基类
- [ ] **T48** `Debug/Debugger.cs` — 入口只管快捷键 + 面板管理

#### Event 精简
- [ ] **T49** 删 `Event/EventManager.cs` — 三套存储 `storage`/`storage2`/`storage3` 合并为一套
- [ ] **T50** `Event/EventManager.cs` — `async void DelayedInvoke` 改为 `UniTask` + `TimerManager.Countdown`
- [ ] **T51** `Event/EventManager.cs` — `GetSender/GetData` 改为 `Result<T>` 显式返回
- [ ] **T52** 整合 `Event/EventTriggerManager.cs` 与 `EventTrigger/EventTriggerManager.cs` — 拆保留业务相关、合并通用部分

#### WebRequest 重构
- [ ] **T53** `WebRequest/WebRequest.cs` — 改为 `UniTask<HttpCallBackArgs>` 异步，去除 IsBusy 单例锁
- [ ] **T54** 新建 `WebRequest/HttpRequestQueue.cs` — 支持并发请求队列
- [ ] **T55** `WebRequest/WebRequest.cs:187-192` — `Post` 不再把 string 重复包装为 JSON value

---

### 🟡 P2：异步风格统一

- [ ] **T56** `AssetsResourse/StreamingAssetsLoader.cs` — 新增 `LoadTextAsync`/`LoadTextureAsync` UniTask 版本
- [ ] **T57** `Save/SaveManager.cs` — 新增 `SaveAsync<T>`/`LoadAsync<T>`（走 `UniTask.RunOnThreadPool`）
- [ ] **T58** `Timer/` — 新增 `WaitAsync(this ITimer timer, CancellationToken)` UniTask 等待扩展

---

### 🟢 P2：模块解耦与独立导入

- [ ] **T59** 新建 `Core/IServiceLocator.cs` + `ServiceLocator.cs` — 服务定位器
- [ ] **T60** `UI/Core/UIManager.cs` — 通过 `IAssetLoader` 注入替代硬依赖 `AddressablesManager`
- [ ] **T61** `AssetsResourse/AddressablesManager.cs` — 删除静态构造函数副作用，改为 `Initialize()` 显式调用
- [ ] **T62** 拆 asmdef：
  - `WManager.Core.asmdef`
  - `WManager.Timer.asmdef` → Core
  - `WManager.Event.asmdef` → Core
  - `WManager.Sound.asmdef` → Core
  - `WManager.Save.asmdef` → Core
  - `WManager.WebRequest.asmdef` → Core
  - `WManager.StreamingAssets.asmdef` → Core
  - `WManager.Addressables.asmdef` → Core（依赖 UniTask）
  - `WManager.UI.asmdef` → Core（可选 Addressables）
  - `WManager.Camera.asmdef` → Core
  - `WManager.MiniMap.asmdef` → Core
  - `WManager.Debug.asmdef` → Core
  - `WManager.Extension.asmdef` → Core

---

### 🟢 P2：接口抽象补齐

- [ ] **T63** 新建 `Sound/ISoundService.cs`
- [ ] **T64** 新建 `UI/Core/IUIService.cs`
- [ ] **T65** 新建 `AssetsResourse/IAssetLoader.cs`（UIManager 依赖此接口）
- [ ] **T66** 新建 `Camera/ICameraController.cs`（统一三个相机控制器）
- [ ] **T67** 新建 `Save/ISaveService.cs`
- [ ] **T68** 新建 `WebRequest/IHttpClient.cs`

---

### 🟢 P3：补全缺失功能

- [ ] **T69** `Timer/` — 计时器池 `TimerPool`
- [ ] **T70** `UI/Core/UIManager.cs` — 新增 `PreloadAsync<T>` 面板预加载、`BackAsync` 栈回退
- [ ] **T71** `AssetsResourse/AddressablesManager.cs` — 加 LRU 上限保护
- [ ] **T72** `Save/SaveManager.cs` — 存档版本兼容字段
- [ ] **T73** `WebRequest/WebRequest.cs` — 加超时 Token 支持
- [ ] **T74** `Event/EventManager.cs` — 加事件订阅优先级
- [ ] **T75** `Extension/StringExtension.cs` — 删除注释的 Newtonsoft.Json 死方法

---

## 实施顺序（Claude 执行流）

```
Phase 1: 8 个 P0 Bug 修复（T01-T08）            ← 立即开始
Phase 2: Core 基础设施（T09-T12）                ← 阻塞后续
Phase 3: SingletonBehaviour 迁移（T13-T20）     ← 依赖 Phase 2
Phase 4: 命名重命名（T21-T30）                  ← 可与 Phase 3 并行
Phase 5: Timer 重构（T31-T41）                  ← 设计最佳，重点打磨
Phase 6: God Class 拆分（T42-T55）              ← 独立可并行
Phase 7: 异步统一（T56-T58）                    ← 依赖 Phase 5
Phase 8: 解耦与接口（T59-T68）                  ← 收尾
Phase 9: 补全功能（T69-T75）                    ← 可选
```

---

## 当前状态

- [x] **Phase 0**：计划制定完成
- [ ] **Phase 1**：P0 Bug 修复
- [ ] **Phase 2-9**：待执行