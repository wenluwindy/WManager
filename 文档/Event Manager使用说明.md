# 纹路风 » Event Manager 说明 API

## 简介

Event Manager 是一个事件管理器系统。能全局发送事件消息和接受事件消息。

### 原理

一个事件可以被看作是在场景中全局发送的消息。这个消息可以被在场景中的任何人发送,并且可以被在场景中的任何人听到。
这意味着事件管理器能够发出一个事件(发送消息)和监听一个事件(监听消息)。事件管理器可以在发出事件时存储任何类型的数据,并可以将该数据发布给正在监听的对象。

### 使用方法

引用**WManager**命名空间(`using WManager`);用 EventManager.EmitEvent()发送消息,用 EventManager.startlistening()监听事件,StopListening()方法停止监停,其它方法看例子

## 发射事件

发送事件意味着向场景中的每个人全局发送消息。只有监听事件的人才能够接收事件。  
事件可以从 c#脚本的任何位置触发。每次触发事件时,侦听器都会检测该事件并执行定义的回调函数。

### EMITEVENT

要发出事件,必须使用 EmitEvent()方法。这个方法有不同的选项和特性,所以它附带了一些重载 overloads。

```
[1]EventManager.EmitEvent(eventName)

[2]EventManager.EmitEvent(eventName, sender)

[3]EventManager.EmitEvent(eventName, delay)

[4]EventManager.EmitEvent(eventName, delay, sender)

[5]EventManager.EmitEvent(eventName, filter, delay = 0)
```

| 参数      | 类型   | 描述                                                                 |
| --------- | ------ | -------------------------------------------------------------------- |
| eventName | string | 要监听的事件的名称。                                                 |
| sender    | object | 发出此事件的对象。                                                   |
| delay     | float  | 在发出此事件之前等待的秒数。                                         |
| filter    | string | 用于选择哪些侦听器必须接收此事件的筛选器(请参阅筛选器一段详细信息)。 |

```csharp
// 导入命名空间用于访问EventManager类。
using WManager;

public class Demo : MonoBehaviour
{
  void Start()
  {
    // 基本事件
    EventManager.EmitEvent("你好,世界");

    // 基本延迟事件(2秒后)。
    EventManager.EmitEvent("你好,世界", 2);

    // 使用过滤器:标签为Player,的基本事件。
    EventManager.EmitEvent("你好,世界", "tag:Player");
  }

}
```

## 监听事件

为了让你的游戏对象能够监听事件,你必须使用**StartListening()**方法。  
需要记住的是,这个方法必须被游戏对象执行**一次** 。  
必须在**Start()**或**OnEnable()**或**Awake()** Unity 函数中编写这个方法(这个函数你要确保它只在游戏开始时执行一次)。

### STARTLISTENING

StartListening()是你必须调用的方法,让游戏对象能够监听事件。  
基本用法总是需要**事件的名称**(在**EmitEvent()**方法中指定的名称)和在监听事件时要执行的**函数名称**(callback 函数)。

```
[1]EventManager.StartListening(eventName, callBack)

[2]EventManager.StartListening(eventName, target, callBack, callBackID = "")
```

| 参数       | 类型              | 描述                                                                                                                              |
| ---------- | ----------------- | --------------------------------------------------------------------------------------------------------------------------------- |
| eventName  | string            | 要收听的事件的名称。                                                                                                              |
| target     | GameObject        | Unity GameObject 正在监听这个事件。它必须定义为过滤特性。                                                                         |
| callBack   | function          | 每次检测到此事件时要调用的函数的名称。                                                                                            |
| callBackID | string (optional) | 当指定时,callBack 函数将通过这个唯一的 ID 进行标识。您可以在 StopListening()方法中使用这个 ID 字符串,而不是使用 callBack 函数名。 |

```csharp
// 导入命名空间用于访问EventManager类。
using WManager;

public class Demo : MonoBehaviour
{

  void Start()
  {
    // 基础监听
    EventManager.startlistening("你好世界", MyCallBackFunction);

    // 高级监听
    EventManager.startlistening("你好世界", gameObject, MyCallBackFunction);
  }

  void MyCallBackFunction()
  {
    // 执行的代码...
  }

}
```

### STOPLISTENING

StopListening()方法停止监听给定游戏对象的事件名称。</br>
这个方法只是从事件管理中移除游戏对象回调函数,它不会停止整个事件。

```
[1]EventManager.StopListening(eventName, callBack)

[2]EventManager.StopListening(eventName, callBackID)
```

| 参数       | 类型     | 描述                            |
| ---------- | -------- | ------------------------------- |
| eventName  | string   | 不想再监听的事件的名字。        |
| callBack   | function | 被调用的 callBack 函数的名称。  |
| callBackID | string   | 与 callBack 函数关联的唯一 ID。 |

```csharp
// 导入命名空间用于访问EventManager类。
using WManager;

public class Demo : MonoBehaviour
{

  void Start()
  {
    // V 1: 基本监听
    EventManager.startlistening("你好世界", MyCallBackFunction);

    // V 2: 使用callBackID进行基本监听。
    EventManager.startlistening("你好世界", MyCallBackFunction, "回调函数");
  }

  void MyCallBackFunction()
  {
    // 记住StopListening()只对这个游戏对象起作用。
    // "你好世界" 事件继续工作,它被现场的所有对象侦听。

    // V 1: 使用callBack函数名。
    EventManager.stoplistening("你好世界", MyCallBackFunction);

    // V 2: 使用回调ID。
    EventManager.stoplistening("你好世界", "回调函数");
  }

}
```

## 过滤器

在发出事件期间,可以使用规则指定哪些侦听器可以接收事件。目前,您可以通过名称**name**、标签**tag**和层号**layer number**进行过滤。此外,使用通配符,您可以过滤开始**starting**、结束**ending**或包含**containing**特定字符串的名称和标记。

### 怎么使用过滤器

为了使用过滤特性,您必须以适当的方式使用**StartListening()** 和**EmitEvent()**方法。

1. 首先,使用 StartListening 方法定义目标参数,在所有你想要涉及到这个功能的游戏对象中。Event Manager 系统将注册过滤所需的目标信息:

```csharp
// 导入命名空间用于访问EventManager类。
using WManager;

public class Demo : MonoBehaviour
{

  void Start()
  {
    EventManager.startlistening("你好世界", gameObject, MyCallBackFunction);
  }

  void MyCallBackFunction()
  {
  }

}
```

2. 使用**过滤器**参数触发事件。该参数必须是以下形式的字符串:

```
name: <gameobject_name>;
tag: <gameobject_tag>;
layer: <layer_number>
```

- name, tag, layer 命令必须用;
- 您可以只使用其中一个、两个或所有命令

在下面的例子中,事件被发送给所有监听名为“你好世界”事件的监听器,但只有带有“Player”标签的游戏对象会检测到这个事件。

```csharp
// 导入命名空间用于访问EventManager类。
using WManager;

public class Demo : MonoBehaviour
{

  void Start()
  {
    // 只有带有“Player”标签的游戏对象才会监听这个事件。
    EventManager.EmitEvent("你好世界", "tag:Player");
  }

}
```

### 使用通配符

你也可以使用\*符号来过滤从字符串开始,结束或包含一个字符串的名称和标签(不是层):

- String\* : 以'String'开头的过滤器名称和标签;
- \*String : 以'String'结尾的过滤器名称和标签;
- _String_ : 包含'String'的过滤器名称和标签;

在下面的例子中,事件只会被默认层中名称以“Play”开头,标签包含“super”的游戏对象接收。

```csharp
// 导入命名空间用于访问EventManager类。
using WManager;

public class Demo : MonoBehaviour
{

  void Start()
  {
    // 只有名称以“Play”开头,标签包含“super”的游戏对象,在0层,才会监听这个事件。
    EventManager.EmitEvent("你好世界", "name:Play*;tag:*super*;layer:0");
  }

}
```

## 监听一组事件

这种替代方法提供了一种更实用的方法来定义、启动和停止侦听器。其思想是将所有侦听器收集到一个组中,然后启动/停止该组。

### 使用方法

首先,声明 EventsGroup 类的一个全局实例。  
然后,在通常声明所有侦听器的地方,向该实例添加事件名称和回调函数,并从它调用 StartListening()方法。  
同样,当需要停止侦听时,只需在该实例上使用 StopListening()方法。

```csharp
// 导入命名空间用于访问EventManager类。
using WManager;

public class Demo : MonoBehaviour
{

  // EventsGroup声明。
  EventsGroup myGroup = new EventsGroup();

  void Start()
  {
    // 添加一些听众。
    myGroup.add("事件1", MyCallBack_1);
    myGroup.add("事件2", MyCallBack_2);
    myGroup.add("事件3", MyCallBack_3);

    // 启动组。
    myGroup.startlistening();
  }

  void OnFinish()
  {
    // 停止组。
    myGroup.stoplistening();
  }

}
```

## 发送和接收简单数据

StartListening()和 EmitEvent()方法的基本用法是发送和接收具有特定名称的事件。  
但是,有时我们会发出带有一些数据的事件,监听器可以接收和读取这些数据。  
Event Manager 有一些内置的方法来发送和接收任何类型的数据。

### 通用数据

- **发出**  
  要用发出的事件发送数据,必须使用**SetData**方法。此事件接受通用对象数据;这意味着您可以发送可以识别为对象数据类型的任何类型的数据。

  ```
  EventManager.SetData(eventName, dataToEmit)
  ```

  | 参数       | 类型   | 描述                 |
  | ---------- | ------ | -------------------- |
  | eventName  | string | 要发出的事件的名称。 |
  | dataToEmit | object | 与此事件关联的数据。 |

  ```csharp
  // 导入命名空间用于访问EventManager类。
  using WManager;

  public class Demo : MonoBehaviour
  {

    void Start()
    {
      EventManager.SetData("你好,世界", 100);
      EventManager.EmitEvent("你好,世界");
    }

  }
  ```

- **读取**  
  要读取由 Event 发出的数据,必须使用 GetData 方法或事件管理器系统附带的 Get*方法之一。  
  如果你使用 GetData,你会得到一个对象;这意味着必须将该对象转换为正确的数据类型。  
  其他的 Get*方法被实现来获取直接转换成特定数据类型的值。

  | 方法                         | 返回值     | 描述                                   |
  | ---------------------------- | ---------- | -------------------------------------- |
  | **GetData**(eventName)       | object     | 对象的值。它应该转换成正确的数据类型。 |
  | **GetGameObject**(eventName) | GameObject | GameObject 元素。                      |
  | **GetInt**(eventName)        | int        | 整数                                   |
  | **GetBool**(eventName)       | bool       | 布尔值                                 |
  | **GetFloat**(eventName)      | float      | 浮点数值。                             |
  | **GetString**(eventName)     | string     | 字符串值。                             |
  | **GetSender**(eventName)     | object     | 发送对象。详细信息请参见发件人参数。   |

  ```csharp
  // 导入命名空间用于访问EventManager类。
  using WManager;

  public class Demo : MonoBehaviour
  {

    void Start()
    {
      EventManager.startlistening("你好世界", MyCallBackFunction);
    }

    void MyCallBackFunction()
    {
      var value = EventManager.GetInt("你好世界");
    }

  }
  ```

### 发送器参数

- **发出**  
  EmitEvent 方法带有一个可选的 sender 对象参数。使用此方法重载时,事件管理器将保存具有事件名称的发件人。

  ```csharp
  // 导入命名空间用于访问EventManager类。
  using WManager;

  public class Demo : MonoBehaviour
  {

    void Start()
    {
      EventManager.EmitEvent("你好世界", gameObject);
    }

  }
  ```

- **读取**  
  一旦从监听器中检测到事件,你就可以使用 GetSender()方法读取发送者游戏对象。

  ```csharp
  // 导入命名空间用于访问EventManager类。
  using WManager;

  public class Demo : MonoBehaviour
  {

    void Start()
    {
      EventManager.startlistening("你好世界", MyCallBackFunction);
    }

    void MyCallBackFunction()
    {
      var sender = EventManager.GetSender("你好世界");
      if (sender != null) GameObject go = (GameObject)sender;
    }

  }
  ```

## 发送和接收复杂数据

SetData()方法是一种实用而简单的发送数据的方法。但是,它只接受一个值,在需要发出大量数据的情况下不太合适。因此,Event Manager 提供了两种方法来管理大量数据。

### 设置/获取数据组

该方法包括将数据收集到数组中,并使用内置转换方法在集合中实现该数据。

- **发出**  
  SetDataGroup() 方法允许发送无限的值列表。

  ```
  EventManager.SetDataGroup(eventName, parameters...);
  ```

  | 参数       | 类型   | 描述                 |
  | ---------- | ------ | -------------------- |
  | eventName  | string | 要发出的事件的名称。 |
  | parameters | array  | 无限的数据列表。     |

  ```csharp
  // 导入命名空间用于访问EventManager类。
  using WManager;

  public class Demo : MonoBehaviour
  {
    void Start()
    {
      // 这个方法接受任何类型的数据列表。
      EventManager.SetDataGroup("你好世界", 24, true, 20.5f, gameObject, "Hello", 267, false);
      EventManager.EmitEvent("你好世界");
    }
  }
  ```

- **接收**  
   GetDataGroup() 方法允许读取发出的数据集合。
  数据以数组的形式可用。
  但是,Event Manager 使用 GetDataGroup 方法释放这个数组,该数组具有一个内置结构,带有以正确方式转换值的特定方法。

  ```csharp
  var eventData = EventManager.GetDataGroup(eventName);
  ```

  | 参数      | 类型   | 描述                 |
  | --------- | ------ | -------------------- |
  | eventName | string | 要发出的事件的名称。 |

  | 返回值    | 类型  | 描述                         |
  | --------- | ----- | ---------------------------- |
  | eventData | array | 一个具有内置转换方法的数组。 |

  ```csharp
  // 导入命名空间用于访问EventManager类。
  using WManager;

  public class Demo : MonoBehaviour
  {
    void Start()
    {
      EventManager.startlistening("你好世界", MyCallBackFunction);
    }

    void MyCallBackFunction()
    {
      // 访问数据数组。
      var eventData = EventManager.GetDataGroup("你好世界");

      // 数组读取与正确的值转换。
      int age = eventData[0].ToInt();
      bool canAttack = eventData[1].ToBool();
      float power = eventData[2].ToFloat();
      GameObject me = eventData[3].ToGameObject();
      string say = eventData[4].ToString();
      ...
    }
  }
  ```

### 索引数据组

作为一个数组,DataGroup 方法要求使用 0 到 X 索引来定位值,顺序与在 SendDataGroup()方法中声明值的顺序相同。 IndexedDataGroup 方法提供了一种更灵活的方式,允许为每个值定义唯一的 ID,就像在 c#字典数据类型中的那样。

- **发送**

SetIndexedDataGroup() 方法需要 DataGroup 类型来声明一对 id 和值。

```csharp
EventManager.SetIndexedDataGroup(eventName, dataGroupParameters...);
```

**参数类型说明**

- eventName - string 要发送的事件的名称。
- dataGroupParameters - parameters 一个无限制的 DataGroup 参数列表。

```csharp
EventManager.DataGroup(id, value);
```

**参数类型说明**

- id - string 标识此值的唯一 ID。
- value - object 分配的值

```csharp
// 导入命名空间用于访问EventManager类。
using WManager;

public class Demo : MonoBehaviour
{

  void Start()
  {
    // 这个方法接受任何类型的数据列表。
    EventManager.SetIndexedDataGroup(
      "你好世界",
      new EventManager.DataGroup { id = "生命值", data = 100 },
      new EventManager.DataGroup { id = "金币", data = 250 },
      new EventManager.DataGroup { id = "奖励", data = "铁盾" }
    );
    EventManager.EmitEvent("你好世界");
  }

}
```

### 读取

GetIndexedDataGroup()方法将发出的数据作为 c#字典式结构释放。读取值需要使用唯一的 ID。

```csharp
var eventData = EventManager.GetIndexedDataGroup(eventName);
```

**返回值类型说明**

- eventData - collection 具有内置转换方法的数据集。

```csharp
// 导入命名空间用于访问EventManager类。
using WManager;

public class Demo : MonoBehaviour
{

  void Start()
  {
    EventManager.StartListening("你好世界", MyCallBackFunction);
  }

  void MyCallBackFunction()
  {
    // 访问数据集。
    var eventData = EventManager.GetIndexedDataGroup("你好世界");

    // 使用正确的值转换读取集合。
    int strength = eventData.ToInt("生命值");
    int coins = eventData.ToInt("金币");
    int bonuses = eventData.ToString("奖励");
  }

}
```

## 管理方法

Event Manager 附带了一组方法,可用于获取信息或执行特定操作。

### 事件名称存在

如果给定名称的事件存在,则返回 true。

```csharp
EventManager.EventExists(eventName);
```

### 停止所有事件

关闭监听系统。

```csharp
EventManager.StopAll();
```

### 检查监听器

如果至少有一个监听器,则返回 true。

```csharp
EventManager.IsListening();
```

### 暂停/恢复

暂停并重新启动一个事件(如果指定)或所有事件(如果没有指定)的侦听。

```csharp
EventManager.PauseListening(eventName);
EventManager.RestartListening(eventName);
```

### 暂停检查

如果指定的事件已暂停,则返回 true。

```csharp
EventManager.isPaused(eventName);
```

### 处理数据存储器

清除单个事件或整个事件管理器系统占用的内存。此方法只清除数据,而侦听器继续工作。

```csharp
EventManager.Dispose(eventName);
EventManager.DisposeAll();
```
