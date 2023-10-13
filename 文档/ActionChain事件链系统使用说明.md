# 纹路风 » ActionChain 事件链系统使用说明 v1.0

> ## 简介

这是一个顺序执行的系统，通过这个系统，可以实现事件的顺序执行，底层采用协程。

> ## Action 事件
>
> 系统内置了八种类型的事件，分别是：

1. Simple 普通事件：最简单的事件，类似于一个简单的回调函数。
2. Delay 延迟事件：指定一段时间后执行指定的回调函数。
3. Timer 定时事件：类似定时器，可以正计时或倒计时，通过参数指定。
4. Until 条件事件：直到指定条件成立时执行回调函数，如按钮点击事件等。
5. While 条件事件：在条件成立时一直执行回调函数，直到条件不再成立。
6. Tween 动画事件：播放 DoTween 动画，事件在动画结束后结束。
7. Animate 动画事件：通过 Animator 播放动画，需要指定 Animator 组件和动画状态名称。
8. Timeline 时间轴事件：需要指定事件开始的时间节点和执行时长，需要与 Timeline ActionChain 时间轴事件链配合使用。

也可以通过继承`AbstractAction`抽象事件类，重写`OnInvoke`和`OnReset`函数来自定义事件。

> ## Action Chain 事件链
>
> 事件链包含三种类型：

1. Timeline 时间轴事件链
2. Sequence 序列事件链
3. Concurrent 并发事件链

它们均继承自`IActionChain`接口，包含了启动、暂停、恢复、终止等函数。

#### 接口代码

```csharp
///事件链接口
public interface IActionChain
{
  IActionChain Append(IAction action);
  IActionChain Begin();
  void Stop();
  void Pause();
  void Resume();
  bool IsPaused { get; }
  IActionChain StopWhen(Func predicate);
  IActionChain OnStop(Action action);
  IActionChain SetLoops(int loops);
}
```

### 1. Timeline 时间轴事件链

Timeline 事件链的执行依赖于协程，可以通过当前的`MonoBehaviour`或`ActionChain`的管理类开启协程。

```csharp
//通过当前MonoBehaviour获取事件链
IActionChain chain1 = this.Timeline();
//通过ActionChain获取事件链
IActionChain chain2 = ActionChain.Timeline();
```

时间轴事件链中包含的核心变量有：

- `CurrentTime`：当前执行的时间节点
- `Speed`：执行速度

#### 示例

```csharp
using UnityEngine;
using WManager;

public class Example : MonoBehaviour
{
    [SerializeField] private GameObject cube;
    [SerializeField] private GameObject sphere;

    private TimelineActionChain timeline;

    private void Start()
    {
        timeline = this.Timeline()
            // 通过Append添加时间轴事件
            // 第一个参数表示该事件开始的时间节点
            // 第二个参数表示该事件的时长
            .Append(0f, 5f, s => cube.transform.position = Vector3.Lerp(Vector3.zero, new Vector3(0, 0, 5f), s))
            .Append(2f, 4f, s => sphere.transform.position = Vector3.Lerp(Vector3.zero, Vector3.up * 2f, s))
            .Begin() as TimelineActionChain;

        // 2倍速
        timeline.Speed = 2f;
    }

    private void OnGUI()
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label("时间轴");
        // 通过Slider调整CurrentTime 实现从指定的时间节点执行
        timeline.CurrentTime = GUILayout.HorizontalSlider(timeline.CurrentTime, 0f, 6f,
            GUILayout.Width(300f), GUILayout.Height(50f));
        GUILayout.EndHorizontal();
    }
}
```

![](https://gitee.com/wenlufeng/tuchuang/raw/master/%E4%BA%8B%E4%BB%B6%E9%93%BE.gif)

### 2. Sequence 序列事件链

序列事件链中的事件是依次执行的，每个事件在上一个事件执行结束后开始执行。

```csharp
this.Sequence()
  .Event(() => Debug.Log("Begin"))
  .Delay(3f)
  .Event(() => Debug.Log("3f"))
  .Begin();
```

其他一些事件的使用示例如下：

```csharp
using UnityEngine;
using DG.Tweening;
using WManager;

public class Example : MonoBehaviour
{
    private void Start()
    {
        this.Sequence()
            // 普通事件
            .Event(() => Debug.Log("开始"))
            // 延迟2秒
            .Delay(2f)
            // 普通事件
            .Event(() => Debug.Log("2f"))
            // 直到按下键盘A键
            .Until(() => Input.GetKeyDown(KeyCode.A))
            // 普通事件
            .Event(() => Debug.Log("按下A键"))
            // DoTween动画事件
            .Tween(() => transform.DOMove(new Vector3(0f, 0f, 1f), 2f))
            // 定时事件
            .Timer(3f, false, s => Debug.Log(s))
            .Begin()
            .OnStop(() => Debug.Log("结束"));
    }
}
```

### 3. Concurrent 并发事件链

并发事件链中的事件是同时执行的，在所有事件都执行完成后，事件链终止。

```csharp
this.Concurrent()
  .Event(() => Debug.Log("开始"))
  .Delay(1f, () => Debug.Log("1f"))
  .Delay(2f, () => Debug.Log("2f"))
  .Delay(3f, () => Debug.Log("3f"))
  .Until(() => Input.GetKeyDown(KeyCode.A))
  .Begin()
  .OnStop(() => Debug.Log("结束"));
```

### 4. 事件链嵌套

事件链之间支持互相嵌套，可以实现更复杂的事件控制。

```csharp
this.Sequence()
  .Event(() => Debug.Log("开始"))
  //嵌套一个并发事件链
  .Append(new ConcurrentActionChain()
    .Delay(1f, () => Debug.Log("1f"))
    .Delay(2f, () => Debug.Log("2f"))
    .Delay(3f, () => Debug.Log("3f"))
    as IAction)
  //并发事件链执行完成后 继续执行序列事件链
  .Until(() => Input.GetKeyDown(KeyCode.A))
  .Event(() => Debug.Log("A Pressed."))
  .Timer(3f, false, s => Debug.Log(s))
  .Begin()
  .OnStop(() => Debug.Log("结束"));
```

> ## 使用案例
>
> 一般用于顺序执行的步骤、方法

### 1. 事件链一般使用案例

```csharp
//编辑事件链：序列事件链
chain = ActionChain.Sequence()
    //普通事件
    .Event(() => Debug.Log("开始事件链"))
    //延迟2秒
    .Delay(2f)
    //普通事件
    .Event(() => Debug.Log("经过2秒"))
    .Event(() => Debug.Log("等待按下A键"))
    //直到按下键盘A键
    .Until(() => Input.GetKeyDown(KeyCode.A))
    //普通事件
    .Event(() => Debug.Log("按下A键"))
    //DoTween动画事件
    .Tween(() => transform.DOMove(new Vector3(0f, 1f, 0f), 2f))
    //按钮点击事件
    .Event(() => print("等待点击" + button.name))
    .Until(button.isClickBtn())
    .Event(() => print("点击了" + button.name))
    //物体点击事件
    .Event(() => print("等待点击" + Object.name))
    .Until(Object.isClickObj())
    .Event(() => print("点击了" + Object.name))
    //动画事件
    .Event(() => print("等待动画a"))
    .Animate(animator, "a")
    .Event(() => print("a播放完毕"))

    //嵌套一个并发事件链
    .Append(new ConcurrentActionChain()
        .Delay(1f, () => Debug.Log("1f"))
        .Delay(2f, () => Debug.Log("2f"))
        .Delay(3f, () => Debug.Log("3f"))
        as IAction)
    //并发事件链执行完成后 继续执行序列事件链

    //定时事件
    .Timer(3f, false, s => Debug.Log(s))
    .Event(() => print("等待动画q"))
    .Animation(animt, "q")
    .Event(() => print("q播放完毕"));

//执行事件链
chain.Begin()
.OnStop(() => Debug.Log("事件结束"));
```

### 2.上一步、下一步、跳步功能案例

> 先将要用到的事件链存为不同的方法，并配置：停止后的事件（可选）、在管理器中预启动
```csharp
    private void Begin1()
    {
        //编辑事件链：序列事件链
        var chain = ActionChain.Sequence()
            //普通事件
            .Event(() => Debug.Log("开始事件链1"))
            //延迟2秒
            .Delay(2f)
            //普通事件
            .Event(() => Debug.Log("经过2秒"))
            .Event(() => Debug.Log("等待按下A键"))
            //直到按下键盘A键
            .Until(() => Input.GetKeyDown(KeyCode.A))
            //普通事件
            .Event(() => Debug.Log("按下A键"))
            .Event(() => Debug.Log("11111111"))
            ;
        MethodManager.currentChain = chain;
        MethodManager.currentChain.OnStop(() =>
        {
            Debug.Log("事件1结束");
        });
        MethodManager.currentChain.Begin();
    }
    private void Begin2()
    {
        //编辑事件链：序列事件链
        var chain = ActionChain.Sequence()
            //普通事件
            .Event(() => Debug.Log("开始事件链2"))
            //物体点击事件
            .Event(() => print("等待点击" + Object.name))
            .Until(Object.isClickObj())
            .Event(() => print("点击了" + Object.name))
            .Event(() => Debug.Log("4"))
            .Event(() => Debug.Log("5"))
            .Event(() => Debug.Log("6"))
            .Event(() => Debug.Log("2222222"))
            ;
        MethodManager.currentChain = chain;
        MethodManager.currentChain.OnStop(() =>
        {
            Debug.Log("事件2结束");
        });
        MethodManager.currentChain.Begin();
    }
    private void Begin3()
    {
        //编辑事件链：序列事件链
        var chain1 = ActionChain.Sequence()
            //普通事件
            .Event(() => Debug.Log("开始事件链1"))
            .Event(() => Debug.Log("等待按下S键"))
            //直到按下键盘A键
            .Until(() => Input.GetKeyDown(KeyCode.S))
            //普通事件
            .Event(() => Debug.Log("按下S键"))
            .Event(() => Debug.Log("666666666666"))
            ;
        MethodManager.currentChain = chain1;
        MethodManager.currentChain.OnStop(() =>
        {
            Debug.Log("事件3结束");
        });
        MethodManager.currentChain.Begin();
    }
```
> 在初始化时将其加入方法管理器，点击上一步下一步即可查看效果
```csharp
public Button next;
public Button Previous;
void Start()
{
    MethodManager.AddMethod(Begin1);
    MethodManager.AddMethod(Begin2);
    MethodManager.AddMethod(Begin3);
    next.onClick.AddListener(() =>
    {
        MethodManager.NextMethod();
    });
    Previous.onClick.AddListener(() =>
    {
        MethodManager.PreviousMethod();
    });
}
```
> 跳跃步骤：直接引用方法到按钮上，或者在脚本中直接使用**MethodManager.JumpMethod(int)**
```csharp
public void jump(int i)
{
    MethodManager.JumpMethod(i);
}
```