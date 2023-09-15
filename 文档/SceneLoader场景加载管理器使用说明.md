# 纹路风 » SceneLoader场景加载管理器使用说明

## 简介

该管理器对 Unity 场景加载封装了一层,主要用于监听场景加载进度、执行加载前后事件等。

## 使用方法

### 通过名称加载场景

```csharp
using UnityEngine;
using WManager;

public class Example : MonoBehaviour {

  private void Start()
  {
    //加载名为Example的场景
    SceneLoader.LoadSceneAsync("Example");
  }
}
```

```csharp
using UnityEngine;
using WManager;
using UnityEngine.SceneManagement;

public class Example : MonoBehaviour {

  private void Start()
  {
    //加载名为Example的场景,设置加载模式为附加到当前场景,并设定场景激活延迟为5秒
    SceneLoader.LoadSceneAsync("Example", LoadSceneMode.Additive)
      .SetSceneActivationDelay(5);
  }
}
```

### 通过指针加载场景

```csharp
using UnityEngine;
using WManager;

public class Example : MonoBehaviour {

  private void Start()
  {
    //加载BuildIndex为1的场景
    SceneLoader.LoadSceneAsync(1);
  }
}
```

## 事件

场景加载事件包含开始事件、加载中事件、完成事件,通过如下方式设置:

```csharp
using UnityEngine;
using WManager;
using UnityEngine.SceneManagement;

public class Example : MonoBehaviour {

  //场景加载过渡界面
  [SerializeField] GameObject loadingView;

  //加载进度条
  [SerializeField] Slider slider;

  //加载进度文本
  [SerializeField] Text progressText;

  void Start()
  {
    //加载BuildIndex为1的场景
    var sld = SceneLoader.LoadSceneAsync(1, LoadSceneMode.Additive)

      //加载前将加载界面打开
      .OnBegin(() => loadingView.SetActive(true))

      //设置场景加载完是否马上激活
      .SetAllowSceneActivation(false)

      //设置场景激活延迟
      .SetSceneActivationDelay(2)

      //加载中事件,将进度值赋给进度条及进度文本
      .OnLoading(s => {
        slider.value = s;
        progressText.text = string.Format("{0}%", Mathf.Round(s * 100));
      })

      //加载结束后关闭加载界面
      .OnCompleted(() => loadingView.SetActive(false));

    //其它地方调用这个事件进行加载
    sld.SetAllowSceneActivation(true);
  }
}
```
