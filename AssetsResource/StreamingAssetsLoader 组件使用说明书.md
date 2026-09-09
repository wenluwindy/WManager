# StreamingAssetsLoader 使用手册

> 基于 `Assets/Scripts/WManager/AssetsResource/StreamingAssetsLoader.cs` 当前源码。
> 自动区分本地 StreamingAssets 与远程 URL 的统一加载工具，覆盖 Android JAR、iOS、PC 等平台差异。

## 1. 能力概览

- 三个静态回调：`LoadText`、`LoadTexture`、`LoadAudio`。
- 路径包含 `://` 时视为远程 URL；否则按平台补全为 StreamingAssets 完整路径。
- Android 通过 `UnityWebRequest` 读取 JAR 内的资源；其他平台通过 `file://`。
- 后缀名自动映射 `AudioType`（mp3/ogg/wav/aiff）。
- 失败时打印 `[StreamingAssetsLoader]` 红色错误日志。

> 组件作为静态类运行；不强制挂场景，但 `AudioClip` 加载依赖 UnityWebRequest 临时协程。

## 2. API

~~~csharp
StreamingAssetsLoader.LoadText(string path, UnityAction<string> callback);
StreamingAssetsLoader.LoadTexture(string path, UnityAction<Texture2D> callback);
StreamingAssetsLoader.LoadAudio(string path, UnityAction<AudioClip> callback);
~~~

| 后缀 | AudioType |
|---|---|
| `.mp3` | `AudioType.MPEG` |
| `.ogg` | `AudioType.OGGVORBIS` |
| `.wav` | `AudioType.WAV` |
| `.aiff` | `AudioType.AIFF` |

## 3. 路径规则

- `data.txt` → `file://.../StreamingAssets/data.txt`（PC/iOS）；Android 自动走 UnityWebRequest 读取 JAR。
- `https://api.example.com/status.txt` → 原样返回。
- 包含 `://` 视为远程；不包含则视为相对 StreamingAssets 路径。

## 4. 使用示例

### 4.1 读取本地 JSON

~~~csharp
StreamingAssetsLoader.LoadText("Configs/settings.json", json =>
{
    Debug.Log($"本地配置：{json}");
});
~~~

### 4.2 下载远程文本

~~~csharp
StreamingAssetsLoader.LoadText("https://api.example.com/status.txt", text =>
{
    Debug.Log($"服务器返回：{text}");
});
~~~

### 4.3 加载 Texture 并显示

~~~csharp
string url = "https://example.com/images/equipment_01.png";
StreamingAssetsLoader.LoadTexture(url, tex =>
{
    myRawImage.texture = tex;
});
~~~

### 4.4 加载本地音频

~~~csharp
StreamingAssetsLoader.LoadAudio("Sounds/click_vfx.wav", clip =>
{
    audioSource.PlayOneShot(clip);
});
~~~

## 5. 注意事项

1. **必须包含文件后缀**，否则音频识别或路径补全可能失败。
2. 所有方法均为异步，加载完成前不会阻塞后续代码，请将后续处理写入回调。
3. 失败（404、文件不存在）会输出 `[StreamingAssetsLoader]` 错误日志。
4. 子目录路径需要包含完整相对路径，例如 `LoadText("Data/User/info.txt", ...)`。
5. `AudioClip` 回调可能在 Player Loop 中触发，注意在销毁对象前完成或丢弃回调。
6. 若使用远程 URL，请自行处理超时、HTTPS 证书与跨域。
