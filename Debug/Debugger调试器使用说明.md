# Debugger 调试器使用说明

> 基于 `Assets/Scripts/WManager/Debug/Debugger.cs` 当前源码。
> 运行时调试工具，继承 `SingletonBehaviour<Debugger>`，提供控制台、层级检视、内存/系统/屏幕/质量/环境信息面板。

## 1. 能力概览

- 实时抓取并显示 `Application.logMessageReceived` 日志（Info/Warning/Error/Fatal）。
- 分类筛选、清除、堆栈查看。
- 层级面板：浏览当前活动场景与 `DontDestroyOnLoad` 物体，可启用/禁用物体。
- 组件检视：编辑选中物体的 Transform（Position/Rotation/Scale）。
- FPS、内存、系统、屏幕、质量、环境信息面板。
- 可拖动、可缩放窗口；快捷键切换。

## 2. 部署步骤

1. 在场景中创建一个空 GameObject 并命名 `Debugger`。
2. 挂载 `Debugger` 脚本。
3. 在 Inspector 配置 `My Skin`（`GUISkin`）和 `Window Scale`。

> 也可以不挂场景物体，`Debugger` 会通过 `SingletonBehaviour<Debugger>` 懒创建 `[Debugger]` GameObject 并 `DontDestroyOnLoad`。

## 3. 字段

| 字段 | 说明 |
|---|---|
| `bool AllowDebugging` | 是否启用调试 |
| `KeyCode AllowDebuggingHotKey` | 切换 `AllowDebugging` 的快捷键，默认 `BackQuote` |
| `GUISkin mySkin` | OnGUI 皮肤 |
| `float windowScale` | GUI 缩放比例 |

## 4. 运行时操作

- 默认反引号（`~`）切换调试器开关。
- 顶部 FPS 按钮可切换展开/收起状态。
- 拖动顶部标题栏移动窗口；右下角 `◢` 区域拖动缩放。
- 切换标签页：`控制台` / `层级` / `内存` / `系统` / `屏幕` / `质量` / `环境`。

## 5. 面板能力

### 5.1 控制台

- 计数显示 Info/Warning/Error/Fatal。
- 勾选切换显示类别。
- 点击日志行选中查看完整堆栈。
- `清除` 按钮清空所有缓存。

### 5.2 层级

- 显示当前活动场景所有根 GameObject。
- 显示 `DontDestroyOnLoad` 下的常驻物体。
- 三角箭头展开/收起子节点。
- 物体名前的勾选可启用/禁用 GameObject。
- 点击物体名称选中，下方组件面板查看/编辑。

### 5.3 组件检视

- 显示选中物体全部组件及其启用状态。
- `Transform` 提供 Position/Rotation/Scale 三组 X/Y/Z 输入框。
- 编辑器环境下点击物体名会同步 `UnityEditor.Selection.activeGameObject`。

### 5.4 内存

`MemoryPanel` 展示：总内存、已占用、Mono 堆等数据，并提供：

- 卸载未使用的资源。
- 强制 GC。

### 5.5 系统

`SystemPanel` 展示操作系统、CPU、GPU、显存、设备 ID 等。

### 5.6 屏幕

- DPI、当前分辨率。
- 全屏切换按钮。

### 5.7 质量

- 当前质量等级。
- 提升/降低一级质量。

### 5.8 环境

- 产品名、Identifier、版本、Unity 版本、公司名。
- 退出程序按钮（调用 `Application.Quit`）。

## 6. 编写自定义面板（可选）

`DebugPanel` 抽象基类位于 `Debug/DebugPanel.cs`：

```csharp
public abstract class DebugPanel
{
    public abstract string Name { get; }
    public abstract void DrawGUI();
}
```

`FpsPanel` / `MemoryPanel` / `SystemPanel` 均继承该类。需要在 Debugger 内显示自定义面板时，扩展 `Debugger` 源码并注册到 `ExpansionGUIWindow` 的 `switch`。

## 7. 注意事项

- 调试器使用 OnGUI 与 `FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)`，层级面板有大量物体时会有性能开销。
- 正式版本建议关闭 `AllowDebugging`、移除组件或剥离 Debugger。
- 抓取到的日志仅保存在内存；不会写文件，需要持久化请自行扩展。
- 切到其它平台时，按键与 GUI 行为可能略有差异。
- 与 `EventSystem` 无关，调试器不走 UGUI 输入。
