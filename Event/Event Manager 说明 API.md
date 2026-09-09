# EventManager API 说明

> 基于 `Assets/Scripts/WManager/Event/EventManager.cs` 当前源码。
> `EventManager` 是进程级静态事件总线，提供监听/发射、过滤、数据传递、优先级订阅、暂停恢复与批量管理。

## 1. 能力概览

- 普通监听与按 ID 注销。
- 立即发射与按秒数延迟发射，可保存 `sender`。
- 按 `name` / `tag` / `layer` 过滤目标监听者。
- 单对象、`DataGroup`、`IndexedDataGroup` 数据传递。
- 优先级订阅（priority 越小越先）。
- 全局或单事件暂停与恢复。
- `EventsGroup` 批量启停一组事件。
- `Dispose` / `DisposeAll` 清理数据缓存（不注销监听）。

> 全部 API 均为静态方法，事件字典在进程内共享。使用前 `using WManager;`。

## 2. 监听与注销

~~~csharp
EventManager.StartListening("OnPlayerLevelUp", OnLevelUp);
EventManager.StartListening("OnPlayerLevelUp", OnLevelUp, "InventoryListener");

// 带目标监听：同时注册原事件和内部过滤事件
EventManager.StartListening("OnDamage", enemyGo, OnEnemyDamage);

EventManager.StopListening("OnPlayerLevelUp", "InventoryListener");
EventManager.StopListening("OnPlayerLevelUp", OnLevelUp);
~~~

- `callBackID` 不为空时，回调被存入 `callBacks` 字典；`StopListening(name, ID)` 走 ID 路径。
- 带 `GameObject target` 的重载会同时向原事件和内部过滤事件名注册；不需要过滤时不要传 `target`。
- `target` 为 `null` 时记录错误并直接返回。
- 注销必须配对，建议放在 `OnDisable` / `OnDestroy` 中。

## 3. 发射

| API | 行为 |
|---|---|
| `EmitEvent(string eventName)` | 立即调用普通监听与优先级监听 |
| `EmitEvent(string eventName, object sender)` | 保存 sender 后立即发射 |
| `EmitEvent(string eventName, float delay)` | 延迟指定秒后调用普通监听 |
| `EmitEvent(string eventName, float delay, object sender)` | 保存 sender 后延迟发射 |
| `EmitEvent(string eventName, string filter, float delay = 0, object sender = null)` | 按过滤条件调用内部过滤事件 |
| `EmitEventData(string eventName, object data, float delay = 0)` | 先 `SetData` 再 `EmitEvent` |

- `delay <= 0` 立即执行，`> 0` 通过 UniTask `Task.Delay` 延迟，回调仍在主线程。
- 延迟是 fire-and-forget，不受 `StopListening` 影响；如需取消请调用 `PauseListening(eventName)`。
- 事件被暂停时（`isPaused` 返回 `true`）普通发射和延迟发射都不会触发回调。
- `EmitEvent(name, sender)` 会覆盖该事件名的 `sender` 缓存。

## 4. 过滤格式

过滤语法：`name:对象名;tag:标签名;layer:图层序号`，值支持 `*` 通配：

- `*Boss`：结尾匹配。
- `Boss*`：开头匹配。
- `*Boss*`：包含匹配。
- 不含 `*`：完全匹配。

仅对带 `target` 注册的监听生效；过滤事件使用内部 key，因此匹配项是注册过该 `target` 的对象。

## 5. 数据 API

### 5.1 单值与发送者

~~~csharp
EventManager.SetData("GoldChanged", 100);
int gold = EventManager.GetInt("GoldChanged");
object sender = EventManager.GetSender("GoldChanged");
~~~

类型不匹配或不存在时分别返回 `null`、`0`、`false`、`0f`、空字符串（实现用 `try-catch` 包裹）。

### 5.2 DataGroup

~~~csharp
EventManager.SetDataGroup("OnLootDrop",
    new Vector3(1,2,3), "GoldCoin", 50, playerPrefab);

var group = EventManager.GetDataGroup("OnLootDrop");
int amount = group[2].ToInt();
string name = group[1].ToString();
~~~

- 同一事件名已经存放 `IndexedDataGroup` 时，`SetDataGroup` 会警告并返回。
- `DataGroup` 字段为 `object data`、`string id`，提供 `ToGameObject/ToInt/ToFloat/ToString/ToBool`。

### 5.3 IndexedDataGroup

~~~csharp
EventManager.SetIndexedDataGroup("OnStageLoad",
    new EventManager.DataGroup { id = "level", data = 3 },
    new EventManager.DataGroup { id = "map",  data = "Forest" });

var idx = EventManager.GetIndexedDataGroup("OnStageLoad");
string map = idx.ToString("map");
int level = idx.ToInt("level");
if (idx.IsEmpty()) { /* ... */ }
~~~

- 同一事件名已经存放 `DataGroup` 时，`SetIndexedDataGroup` 会警告并返回。
- ID 不存在或类型不匹配时返回转换默认值。

## 6. 优先级监听

~~~csharp
EventManager.StartListeningWithPriority("OnBoot", OnBoot, priority: 10);
EventManager.StartListeningWithPriority("OnBoot", OnBootUI, priority: 20);

EventManager.StopListeningWithPriority("OnBoot", OnBoot);
~~~

- 优先级数字越小越先调用；插入时按升序排序，列表通常很短。
- 与普通 `StartListening` 互不影响，同一事件可同时存在普通订阅和优先级订阅。
- 注销需传入同一 `UnityAction` 引用。

## 7. 状态与清理

- `IsListening()`：普通事件字典存在条目返回 `true`；不统计仅有优先级监听的事件。
- `EventExists(name)`：普通事件是否注册过。
- `isPaused(name)`：检查暂停状态；事件从未注册时返回 `true`。
- `PauseListening()` / `PauseListening(name)`：暂停全局或指定普通事件，不移除监听。
- `RestartListening()` / `RestartListening(name)`：恢复全局或指定普通事件。
- `StopAll()`：移除普通事件的所有回调并清空普通事件字典；不清数据缓存或优先级监听。
- `Dispose(name)`：清理指定事件的单值、DataGroup、IndexedDataGroup 缓存；监听仍保留。
- `DisposeAll()`：清理全部上述缓存及 sender。

## 8. EventsGroup 批量管理

~~~csharp
var ui = new EventManager.EventsGroup();
ui.Add("OnShowShop", OpenShop);
ui.Add("OnHideShop", CloseShop);
ui.Add("OnUpdateCurrency", RefreshCurrency);

ui.StartListening();
ui.StopListening();           // 停止全部
ui.StopListening("OnShowShop"); // 仅停止并从组中移除
if (ui.Contains("OnShowShop")) { /* ... */ }
~~~

底层仍走普通 `StartListening` / `StopListening`，生命周期与单事件一致。

## 9. 编辑器工具

`EventManagerEditorWindow` 通过菜单 `Tools → 事件查看器` 打开，扫描项目内 `StartListening` / `EmitEvent` / `EmitEventData` 调用，按颜色区分：

- 🟢 有监听也有发射。
- 🔴 仅监听（可能漏写发射）。
- 🟡 仅发射（可能冗余）。

支持点击 `定位` 在 Project 窗口高亮脚本、`打开` 跳转 IDE 对应行；自动忽略 `//` 与 `/* */` 注释。

## 10. 注意事项

- 全部 API 面向 Unity 主线程；不要在子线程直接调用。
- 静态字典不会随业务对象自动释放，必须在 `OnDisable` / `OnDestroy` 注销监听。
- 临时数据使用 `Dispose(name)` / `DisposeAll()` 清理，否则 `storage` 会持续占用。
- 带目标监听会同时注册到原事件与内部过滤事件；移除时使用一致的 key。
- `PauseListening(name)` 只会暂停普通事件，不影响优先级监听。
- `StopListening(name, ID)` 与 `StopListening(name, callBack)` 都不会清空数据缓存。
