# 纹路风 » Timer计时工具使用说明

## 简介

Timer 模块实现了一系列计时工具,包括定时器(倒计时)、计时器、秒表、闹钟等,它们均继承自接口 ITimer,支持启动、暂停、恢复、停止计时等行为。

## 使用说明

### 一、Countdown 定时器(倒计时)

获取一个定时器可以通过如下方式,计时类工具的运行依赖于携程,通过 this 获取定时器表示使用当前的 MonoBehaviour 开启携程,使用 Timer 获取定时器表示使用计时模块管理器的 MonoBehaviour 开启携程。

```csharp
Countdown countdown1 = this.Countdown(5f);
Countdown countdown2 = Timer.Countdown(10f, true);
```

第一个参数为 float 类型,表示定时的时长,第二个参数为 bool 类型,表示计时是否忽略时间的缩放,默认为 false。通过如下方式设置定时器的启动、执行、暂停、恢复、停止事件:

```csharp
Timer.Countdown(5f)
-.OnLaunch(() => Debug.Log("定时器启动"))
-.OnExecute(s => Debug.Log(string.Format("剩余时间{0}", s)))
-.OnPause(() => Debug.Log("定时器暂停"))
-.OnResume(() => Debug.Log("定时器恢复"))
-.OnStop(() => Debug.Log("定时器终止"));
```

```csharp
using UnityEngine;
using WManager;

public class Example : MonoBehaviour
{
  //定时器
  private Countdown countdown;

  private void Start()
  {
    countdown = Timer.Countdown(5f)
      .OnLaunch(() => Debug.Log("定时器启动"))
      .OnExecute(s => Debug.Log(string.Format("剩余时间{0}", s)))
      .OnPause(() => Debug.Log("定时器暂停"))
      .OnResume(() => Debug.Log("定时器恢复"))
      .OnStop(() => Debug.Log("定时器停止"));
  }

  private void OnGUI()
  {
    if (GUILayout.Button("启动", GUILayout.Width(200f), GUILayout.Height(50f)))
    {
      countdown.Launch();
    }
    if (GUILayout.Button("暂停", GUILayout.Width(200f), GUILayout.Height(50f)))
    {
      countdown.Pause();
    }
    if (GUILayout.Button("恢复", GUILayout.Width(200f), GUILayout.Height(50f)))
    {
      countdown.Resume();
    }
    if (GUILayout.Button("终止", GUILayout.Width(200f), GUILayout.Height(50f)))
    {
      countdown.Stop();
    }
  }
}
```

### 二、Clock 计时器

```csharp
Clock clock1 = this.Clock();
Clock clock2 = Timer.Clock(true);
```

计时器与定时器具有相同的事件,不同的是,定时器为倒计时,例如定时 5 秒,其值将会从 5 逐渐到 0,到 0 后自动停止,计时器为正向计时,需要调用 Stop 手动终止,可以通过 StopWhen 为其设置停止的条件,当条件满足时,计时器将自动停止。

```csharp
Timer.Clock()
-.OnExecute(s => Debug.Log(string.Format("已经计时{0}", s)))
//设置停止条件 当键盘A按下时 计时器停止
-.StopWhen(() => Input.GetKeyDown(KeyCode.A))
-.Launch();
```

### 三、Chronometer 秒表

```csharp
Chronometer chronometer1 = this.Chronometer();
Chronometer chronometer2 = Timer.Chronometer(true);
```

秒表在计时器的基础上增加了 Record 记录的功能,当调用 Shot 方法时,会产生一条记录,记录包含 context 上下文(object 类型)和 time 时间点:

```csharp
using UnityEngine;
using WManager;

public class Example : MonoBehaviour
{
  //秒表
  private Chronometer chronometer;

  private void Start()
  {
    chronometer = this.Chronometer(true);
    chronometer.Launch();
  }

  private void OnGUI()
  {
    if (GUILayout.Button("Shot", GUILayout.Width(200f), GUILayout.Height(50f)))
    {
      chronometer.Shot();
    }
    if (GUILayout.Button("Log", GUILayout.Width(200f), GUILayout.Height(50f)))
    {
      var records = chronometer.Records;
      for (int i = 0; i < records.Count; i++)
      {
        Debug.Log(string.Format("No.{0}: {1}", i + 1,records[i].time));
      }
    }
  }
}
```

### 四、Alarm 闹钟

```csharp
this.Alarm(10, 30, 0, () => Debug.Log("唤醒闹钟")).Launch();
```

前三个参数均为 int 类型,分别表示小时、分钟、秒,第四个参数为回调函数,上例表示当 10 点 30 分 0 秒时,将会执行回调函数,打印“唤醒闹钟”日志。

### 五、EverySeconds 与 EveryFrames

```csharp
this.EverySeconds(5f, () => Debug.Log("TODO"), false, -1).Launch();
```

EverySeconds 表示每隔指定时长,执行一次事件,第一个参数为 float 类型,表示间隔时长,第二个参数为 Action 事件,第三个参数表示是否忽略时间缩放,默认为 false,第四个参数表示执行的次数,传入负数代表一直循环执行,默认为-1。

```csharp
Timer.EveryFrame(() => Debug.Log("TODO"), -1).Launch();
this.EveryFrames(5, () => Debug.Log("TODO"), -1).Launch();
```

EveryFrames 与 EverySeconds 原理相同,不同的是以帧为单位,每隔指定帧执行一次事件。EveryFrame 则表示每帧,可以在不是 MonoBehaviour 的脚本里实现 Update 的功能。

### 六、TimeTool 功能

```csharp
//将2小时转换为秒
var a = TimeTool.Convert2Seconds(2, TimeUnit.Hour);
print(a);

//获取当前时间戳
var b = TimeTool.GetTimeStamp(DateTime.Now);
print(b);

//将8923秒转化为HH:mm:ss格式字符串
var c = TimeTool.ToStandardTimeFormat(8923);
print(c);

//将8923秒转化为mm:ss格式字符串
var d = TimeTool.ToMSTimeFormat(8923);
print(d);

//将8923秒转化为HH:mm:ss:fff格式字符串
var e = TimeTool.ToHMSFTimeFormat(8923);
print(e);

//将8923秒转化为mm:ss:fff格式字符串
var f = TimeTool.ToMSFTimeFormat(8923);
print(f);
```
