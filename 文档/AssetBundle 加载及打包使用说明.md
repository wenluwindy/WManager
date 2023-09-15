# 纹路风 » AssetBundle 加载及打包使用说明 v1.0

> ## 简介

AssetBundle 是 Unity 中用于打包和加载资源的一种机制， 它允许你将游戏中的资源（如模型、纹理、声音、动画等）打包成独立的文件， 然后在运行时动态加载这些资源。这对于优化游戏性能、减少加载时间以及实现远程资源更新非常有用。

AssetBundle 工具包含三个模块：

1.**AssetBundle 浏览器**，编辑器使用：包含浏览、打包 AB 包功能；

2.**AssetBundleLoader**：代码调用的 AssetBundle 加载器；

3.**StreamingAssetsLoader**：代码调用读取 StreamingAsset 中的文件；

4.**ResourceManager**：代码调用提供异步加载 Resources 文件夹下的资源加载器。

> ## AssetBundle 浏览器

### 位置及窗口

![](https://gitee.com/wenlufeng/tuchuang/raw/master/%E6%89%93%E5%8C%851.jpg)

![](https://gitee.com/wenlufeng/tuchuang/raw/master/%E6%89%93%E5%8C%852.jpg)

### 资源标签

要打 AB 包，必须在预制体的检查器窗口中的最下方找到“资源标签”项，给予资源包名及后缀。

![](https://gitee.com/wenlufeng/unitywebtest/raw/master/1.jpg)

在左边的选项中选择 new 增加一个新项，代表这个 AssetBundle 包的包名

在右边的选项中选择 new 增加一个新项，代表这个 AssetBundle 包的后缀

### 浏览选项

提供浏览标记过“资源标签”的资源，查看其引用和预打包的资源。  
![](https://gitee.com/wenlufeng/tuchuang/raw/master/%E6%89%93%E5%8C%853.jpg)  
打包前需给资源添加“资源标签”，并将其拖入浏览选项中。

### 打包选项

提供打包标记过“资源标签”的资源，打包这些资源为 AssetBundle 包。  
![](https://gitee.com/wenlufeng/tuchuang/raw/master/%E6%89%93%E5%8C%854.jpg)  
在“打包目标平台”中选择要打包的平台类型，一般使用安卓、webgl 及 Windows 64；  
在“打包输出路径”中选择要存放打包后的 AssetBundle 包的路径。一般为：Assets/StreamingAssets/AssetBundles  
其它功能悬停鼠标会有说明。

点击“开始打包”会自动将所有“浏览选项”中的资源打包为 AssetBundle 包。

### 检查选项

提供检查打包文件夹中的打包资源，检查打包文件夹中的打包资源是否与预期一致。  
![](https://gitee.com/wenlufeng/tuchuang/raw/master/%E6%89%93%E5%8C%855.jpg)

> ## AssetBundleLoader

### 功能介绍

可在代码中使用这个脚本，加载 AssetBundle 包。

### 加载方法及类型

| 序号 | 方法名                                                                                                      | 描述                                           |
| ---- | ----------------------------------------------------------------------------------------------------------- | ---------------------------------------------- |
| 1    | `AssetBundleLoader.LoadObjFromFile(string path, string name)`                                               | 本地同步加载 AssetBundle 包并且获取 GameObject |
| 2    | `AssetBundleLoader.LoadABFromFile(string path)`                                                             | 本地同步加载 AssetBundle 包                    |
| 3    | `AssetBundleLoader.LoadObjFromFileAsync(string path, string name, UnityAction<GameObject, float> callBack)` | 本地异步加载 AssetBundle 包并且得到 GameObject |
| 4    | `AssetBundleLoader.LoadABFromFileAsync(string path, UnityAction<AssetBundle, float> callback)`              | 本地异步加载 AssetBundle 包                    |
| 5    | `AssetBundleLoader.LoadABFromMemory(byte[] bytes)`                                                          | 从内存加载 AssetBundle 包                      |
| 6    | `AssetBundleLoader.LoadObjFromWeb(string ABName, string name, UnityAction<GameObject, float> callBack)`     | 服务器下载 AB 包并得到 GameObject              |
| 7    | `AssetBundleLoader.LoadABFromWeb(string url, UnityAction<AssetBundle, float> callback)`                     | 从网络 URL 异步加载 AssetBundle 包             |
| 8    | `AssetBundleLoader.GetAssetBundle(string name)`                                                             | 根据名称查找 AssetBundle                       |
| 9    | `AssetBundleLoader.UnloadAssetBundle(string name)`                                                          | 根据名称卸载 AssetBundle                       |
| 10   | `AssetBundleLoader.UnloadAssetBundle(AssetBundle ab)`                                                       | 卸载指定 AssetBundle                           |
| 11   | `AssetBundleLoader.UnloadAllAssetBundles()`                                                                 | 卸载所有 AssetBundle                           |

### 代码示例

**本地同步加载 AssetBundle**

```csharp
// 加载 AssetBundle
AssetBundle ab = AssetBundleLoader.LoadAssetBundleFromFile("assetbundle1");
// 从 AssetBundle 加载预制体
GameObject prefab = ab.LoadAsset<GameObject>("Prefab1");
// 实例化预制体
GameObject obj = Instantiate(prefab);
```

**本地异步加载 AssetBundle**

```csharp
AssetBundleLoader.LoadABFromFileAsync("assetbundle1", (ab, progress) =>
{
    // 从 AssetBundle 加载预制体
    GameObject prefab = ab.LoadAsset<GameObject>("Prefab1");
    // 实例化预制体
    GameObject obj = Instantiate(prefab);
    // progress 介于 0 到 1,表示加载进度
});
```

**从内存加载 AssetBundle**

```csharp
// 读取 AssetBundle 文件到字节数组
byte[] bytes = File.ReadAllBytes("assetbundle1");
// 从字节数组加载 AssetBundle
AssetBundle ab = AssetBundleLoader.LoadAssetBundleFromMemory(bytes);
// 从 AssetBundle 加载预制体
GameObject prefab = ab.LoadAsset<GameObject>("Prefab1");
// 实例化预制体
GameObject obj = Instantiate(prefab);
```

**从网络 URL 加载 AssetBundle**

```csharp
AssetBundleLoader.LoadAssetBundleFromWeb("http://yourserver/assetbundle1", (ab, progress) =>
{
    // 从 AssetBundle 加载预制体
    GameObject prefab = ab.LoadAsset<GameObject>("Prefab1");
    // 实例化预制体
    GameObject obj = Instantiate(prefab);
    // progress 介于 0 到 1,表示加载进度
});
```

> ## StreamingAssetsLoader

### 功能介绍

可在代码中使用这个脚本，加载 StreamingAssets 中的文件：配置文件（json、txt）、图片、音频。

### 加载方法及类型

| 序号 | 方法名                                                                              | 描述                                                             |
| ---- | ----------------------------------------------------------------------------------- | ---------------------------------------------------------------- |
| 1    | `StreamingAssetsLoader.LoadTextAsset(string configName, UnityAction action)`        | 读取 StreamingAsset 中的.txt 和.json 文件                        |
| 2    | `StreamingAssetsLoader.LoadTextureAsset(string mediaName, UnityAction action)`      | 读取 streamingAsset 中的图片                                     |
| 3    | `StreamingAssetsLoader.LoadAudioAsset(string mediaName, UnityAction action = null)` | 读取 streamingAsset 文件夹中的音频（.mp4,.ogg,.wav,.aiff,.mpeg） |

### 代码示例

```csharp
public ModelData info;

[Serializable]
public class ModelData
{
    public string url;
    public string title;
    public string content;
    public int distance;
}

StreamingAssetsLoader.LoadTextAsset("Configure.json", (s) =>
{
    Debug.Log("成功读取文本文件");
    // 将数据赋给数组
    info = JsonUtility.FromJson<ModelData>(s);
    Debug.Log("成功将数据赋给数组");
    TMP_Title.text = info.title;
    Debug.Log(info.title);
    TMP_Content.text = info.content;
    Debug.Log(info.content);
    SCamera.distance = info.distance;
    Debug.Log(info.distance);
});
```

> ## ResourceManager

### 功能介绍

ResourceManager 提供了异步加载 Resources 文件夹下的资源的方法，主要用于加载 Resources 文件夹中的大型资源， 过大的资源推荐使用 AssetBundle 打包加载。

### 加载方法及类型

| 序号 | 方法名                                                                                    | 描述               |
| ---- | ----------------------------------------------------------------------------------------- | ------------------ |
| 1    | `ResourceManager.LoadResourceAsync(string resourcePath, ResourceLoadedCallback callback)` | 加载资源的异步方法 |

### 代码示例

```csharp
ResourceManager.LoadResourceAsync("Prefab", (obj) =>
  {
    // 实例化预制体
    Instantiate(obj);
  });
```
