# 纹路风 » SaveManager 存档系统使用说明

## 简介

SaveManager 存档系统可以存储并加载信息。

### 实现原理

系统附带两种存档方式:

1. 将信息转换为 xml 文件保存在本地。
2. 使用 unity 自带的方法 PlayerPrefs 存储信息,**web 方法保存的值清除缓存后会消失。**

两种方式都附带加密,无法常规打开修改。

### 使用注意事项

使用**SaveManager**需调用命名空间: **using WManager.Save;**

## 属性及方法调用

### 支持的存档类型

| 类型          | 说明                                                               |
| ------------- | ------------------------------------------------------------------ |
| String        |                                                                    |
| Bool          |                                                                    |
| Int           |                                                                    |
| Float         |                                                                    |
| double        |                                                                    |
| Color         | 颜色                                                               |
| Vector2       | 2 坐标                                                             |
| Vector3       | 3 坐标                                                             |
| Transform     | 支持的不是组件,是组件上的值,使用时要将值赋给要改值的 Transform     |
| RectTransform | 支持的不是组件,是组件上的值,使用时要将值赋给要改值的 RectTransform |

### 附带方法

| 方法                                   | 说明                                                                       |
| -------------------------------------- | -------------------------------------------------------------------------- |
| SaveManager.Exists(string path);       | 检查特定文件 path 是否已经被保存,返回 bool 值                              |
| SaveManager.ExistsWeb(string key);     | 检查特定文件 key 是否已经被保存,返回 bool 值                               |
| SaveManager.DeleteData(string path);   | 删除特定文件 path 的数据                                                   |
| SaveManager.DeleteDataWeb(string key); | 删除特定文件 key 的数据                                                    |
| SaveManager.Save(变量, string path);   | 保存“变量”的值到“path”                                                     |
| SaveManager.SaveWeb(变量, string key); | webgl 使用,保存“变量”的值到“key”,保存的值清除缓存后消失                    |
| SaveManager.Load<变量>(string path);   | 从“path”加载“变量”的值,返回变量类型                                        |
| SaveManager.LoadWeb<变量>(string key); | webgl 使用,从“key”加载“变量”的值,返回变量类型,加载的值清除缓存后会加载失败 |

## 使用方法

```csharp
// 导入命名空间用于访问SaveManager。
using WManager.Save;

public class Demo : MonoBehaviour
{
  public string text;

  void Start()
  {
    // 括号后跟存储变量,后面的string为存档名
    SaveManager.Save(text, "测试文本");
  }

  // 加载时调用此方法
  void Load()
  {
    // 尖括号中为变量类型,后面的string为存档名
    text = SaveManager.Load<string>("测试文本");
  }
}
```

```csharp
using UnityEngine;
using System;
// 导入命名空间用于访问SaveManager。
using WManager.Save;

public class Example : MonoBehaviour
{
  public Transform a;

  private void Start()
  {
    // 保存Transform
    SaveManager.Save(a, "a");
  }

  // 加载时调用此方法
  void Load()
  {
    // 尖括号中为变量类型,后面的string为存档名,尖括号无法显示,这里我使用《》
    var a1 = SaveManager.Load<Transform>("a");
    a.position = a1.localPosition;
  }
}
```

```csharp
using UnityEngine;
using System;
// 导入命名空间用于访问SaveManager。
using WManager.Save;

public class Example : MonoBehaviour
{
  [Serializable]
  public class PlayerDate
  {
    public int hp;
    public float cd;
    public string name;
    public double time;
    public bool enabled;
    public Color color;
    public Vector2 vector2 = new Vector2(0, 0);
    public Vector3 vector3 = new Vector3(0, 0, 0);
  }

  public PlayerDate gamer;

  private void Start()
  {
    // 保存自定义类
    SaveManager.Save(gamer, "玩家数据");
  }

  // 加载时调用此方法
  void Load()
  {
    // 尖括号中为变量类型,后面的string为存档名(这里无法显示尖括号,我用《代替)
    gamer = SaveManager.Load<PlayerDate>("玩家数据");
  }
}
```
