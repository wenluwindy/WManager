# EventTriggerManager 使用说明

> 基于 `Assets/Scripts/WManager/EventTrigger/EventTriggerManager.cs` 与 `AddBoxCollider.cs` 当前源码。
> 以纯代码方式给任意 `GameObject` 添加/移除 UGUI 与 3D 交互事件，自动补齐 `EventTrigger` 与 `BoxCollider`，并提供编辑器批量 `BoxCollider` 生成工具。

## 1. 能力概览

- 静态方法与 `GameObject` 扩展方法两种调用风格。
- 自动检测/添加 `EventTrigger`，自动管理 `BoxCollider`（Trigger）。
- 无参回调与 `BaseEventData` 回调两种签名。
- `OnClick` / `OnEnter` / `OnExit` 快捷封装。
- `RemoveAllEvent` 一键清理本管理器添加的事件。
- `AddBoxCollider.AutoBoxCollider` 根据子节点 Renderer 生成贴合的 `BoxCollider`。

> 使用前引入命名空间：`using WManager;`

## 2. EventTriggerManager 静态方法

### 2.1 添加事件

~~~csharp
EventTriggerManager.AddEvent(go, EventTriggerType.PointerClick, OnClick);
EventTriggerManager.AddEvent(go, EventTriggerType.PointerDown, data => { });
~~~

首次为对象注册时会：

- 将 `go.isStatic` 设为 `false`（强依赖静态合批的物体需谨慎）。
- 调用 `DontDestroyOnLoad(go)`，因此临时 UI 或场景物件使用完应主动清理。
- 添加内部 `AutoCleanup` 组件，对象销毁时清理内部字典。
- 若对象没有 `EventTrigger`，自动添加。
- 若对象没有 `Collider`，添加触发用 `BoxCollider`；已有碰撞体时把现有碰撞体的 `isTrigger` 设为 `true`。

源码入口先访问 `go.isStatic`，因此 `go` 为 `null` 时会先触发空引用异常，请勿传入 `null`。

### 2.2 移除事件

~~~csharp
EventTriggerManager.RemoveEvent(go, EventTriggerType.PointerClick, OnClick);
EventTriggerManager.RemoveAllEvent(go);
~~~

- `RemoveEvent(BaseEventData 重载)`：从内部记录移除委托，并尝试清理对应 `EventTrigger.Entry`。由于运行时 listener 与持久化 listener 的清理逻辑差异，单项移除后可能仍残留可触发的 Entry；需要彻底清理时使用 `RemoveAllEvent`。内部记录为空时会移除自动清理组件。
- `RemoveEvent(无参数 重载)`：无参回调在添加时被包装为 lambda，源码无法按原委托精确比较，且清理逻辑会修改正在遍历的列表，**不要依赖此重载做可靠的单项注销**。如需可精确移除，应保存并使用 `UnityAction<BaseEventData>` 委托引用。
- `RemoveAllEvent`：清空并销毁对象上的整个 `EventTrigger`，同时销毁本管理器添加的 `BoxCollider` 与清理组件，移除内部记录。**Inspector 或其他脚本添加的条目也会一并删除**。原有 `Collider` 不会被销毁，但其 `isTrigger` 状态不会恢复。

## 3. CustomEventTrigger 扩展方法（推荐）

所有扩展方法转发到 `EventTriggerManager`：

| 扩展方法 | 默认事件类型 |
|---|---|
| `AddEvent(EventTriggerType type, UnityAction action)` | 调用方指定 |
| `AddEvent(EventTriggerType type, UnityAction<BaseEventData> action)` | 调用方指定 |
| `RemoveEvent(EventTriggerType type, UnityAction action)` | 调用方指定 |
| `RemoveEvent(EventTriggerType type, UnityAction<BaseEventData> action)` | 调用方指定 |
| `OnClickAddListener(UnityAction action, EventTriggerType type = PointerClick)` | `PointerClick` |
| `OnClickAddListener(UnityAction<BaseEventData> action, EventTriggerType type = PointerClick)` | `PointerClick` |
| `OnClickRemoveListener(UnityAction action, EventTriggerType type = PointerClick)` | `PointerClick` |
| `OnClickRemoveListener(UnityAction<BaseEventData> action, EventTriggerType type = PointerClick)` | `PointerClick` |
| `OnEnterAddListener(...)` / `OnEnterRemoveListener(...)` | `PointerEnter` |
| `OnExitAddListener(...)` / `OnExitRemoveListener(...)` | `PointerExit` |

## 4. 使用示例

### 4.1 基础交互绑定

~~~csharp
using UnityEngine.EventSystems;
using WManager;

public class InteractionExample : MonoBehaviour
{
    public GameObject interactableObj;

    private void Start()
    {
        interactableObj.OnClickAddListener(OnObjectClicked);
        interactableObj.OnEnterAddListener(OnMouseEnter);
        interactableObj.OnExitAddListener(OnMouseExit);

        interactableObj.AddEvent(EventTriggerType.PointerDown, data =>
        {
            if (data is PointerEventData p) Debug.Log($"按下坐标：{p.position}");
        });
    }
}
~~~

### 4.2 对象池复用

~~~csharp
private void OnEnable()  => gameObject.OnClickAddListener(OnItemClicked);
private void OnDisable() => gameObject.RemoveAllEvent();
~~~

## 5. 运行条件与限制

- 场景必须存在 `EventSystem` 与对应输入模块；UGUI 使用 `GraphicRaycaster`，3D 物体使用 `PhysicsRaycaster` 或 `Physics 2D Raycaster`。
- 已有碰撞体被改为触发器，但不会记录原始 `isTrigger` 状态；移除事件时只销毁本管理器添加的 `BoxCollider`。
- `RemoveAllEvent` 会清空整个 `EventTrigger`，包括外部条目；保留其他监听者时请使用 `RemoveEvent(BaseEventData)` 或手动管理。
- `EventTriggerType` 可传入 Unity 提供的任意枚举值，例如 `PointerClick`、`PointerDown`、`PointerUp`、`PointerEnter`、`PointerExit`、`Drag`、`Drop`、`Scroll` 等。

## 6. AddBoxCollider 编辑器工具

### 6.1 菜单

`GameObject → 自动添加BoxCollider`，选中一个或多个 GameObject 后调用。

### 6.2 代码调用

~~~csharp
#if UNITY_EDITOR
AddBoxCollider.AutoBoxCollider(targets);
#endif
~~~

### 6.3 行为

- 销毁物体上原有 Collider（不可逆）。
- 计算所有子节点 `Renderer` 的合并包围盒，生成贴合的 `BoxCollider`。
- 已包含父节点 `lossyScale` 逆向计算以修正 LocalSize。

### 6.4 限制

- 使用 `Object.DestroyImmediate`，运行时调用会抛异常。
- 物体及其子节点必须含 `Renderer`；无 Renderer 会导致 `renders.Length == 0` 引发除零。
- 父节点某轴缩放为 0 时尺寸计算异常。

## 7. 组合示例：动态加载模型并加交互

~~~csharp
var go = Instantiate(modelPrefab);
// 编辑器阶段已处理碰撞体；运行时直接绑定交互
go.OnClickAddListener(() => Debug.Log($"点击 {go.name}"));
go.RemoveAllEvent(); // 销毁前
