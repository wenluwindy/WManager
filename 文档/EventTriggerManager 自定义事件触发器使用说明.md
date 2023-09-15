# EventTriggerManager 自定义事件触发器使用说明

这个脚本可以在 Unity 中为游戏对象添加自定义事件触发器组件,实现鼠标点击、进入、退出等交互事件。

## 使用方法

要为游戏对象添加点击事件:

```csharp
gameObject.AddEvent(EventTriggerType.PointerClick, OnClick);
```

移除点击事件:

```csharp
gameObject.RemoveEvent(EventTriggerType.PointerClick, OnClick);
```

也可以使用更简单的方法添加点击、进入、退出事件:

```csharp
gameObject.OnClickAddListener(OnClick);

gameObject.OnEnterAddListener(OnEnter);

gameObject.OnExitAddListener(OnExit);
```

删除事件同理:

```csharp
gameObject.OnClickRemoveListener(OnClick);

gameObject.OnEnterRemoveListener(OnEnter);

gameObject.OnExitRemoveListener(OnExit);
```

删除游戏对象上的所有事件:

```csharp
gameObject.RemoveAllEvent();
```

## 实现原理

- 使用字典存储游戏对象与事件触发器、响应函数的映射关系
- 动态添加 EventTrigger 组件实现事件触发
- 提供了自定义的便捷方法

## 注意事项

- 需要为游戏对象添加 Collider 组件，若没有则会自动添加(自动的不一定准确)
- 删除所有事件后会删除 EventTrigger 组件
