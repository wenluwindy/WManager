# SaveManager 使用说明

> 基于 `Assets/Scripts/WManager/Save/SaveManager.cs` 当前源码。
> 纯静态工具类：本地文件存档与 PlayerPrefs 存档，集成 AES-256 加解密、原子写入、自定义可序列化类以及 Transform/RectTransform 数据；并提供 UniTask 异步版本。

## 1. 能力概览

- `Save` / `Load`：`Application.persistentDataPath/<path>.save` 的二进制存档。
- `SaveToPrefs` / `LoadFromPrefs`：基于 `PlayerPrefs` 的 Web/PlayerPrefs 存档。
- `Exists` / `DeleteData` / `DeletePref`：文件与键的存在性检查与删除。
- 内置 AES-256 加解密（`SaveEncryption`），使用默认密钥；如需自定义密钥请直接调用 `SaveEncryption`。
- `Save`/`SaveToPrefs` 自动将 `Transform` 与 `RectTransform` 转换为可序列化结构（`Save_TransformData`/`Save_RectTransformData`）。
- `Load`/`LoadFromPrefs` 对 `Transform`/`RectTransform` 会创建临时 `GameObject`（`HideAndDontSave`），由调用方负责后续清理。
- `SaveAsync` / `LoadAsync`：在 UniTask 线程池上执行 IO 与加解密。

> 自定义类型需标记 `[System.Serializable]`，并提供公开字段或可被默认序列化器访问的属性。

## 2. API 速查

| 能力 | 本地文件 | PlayerPrefs |
|---|---|---|
| 保存 | `Save<T>(data, path)` | `SaveToPrefs<T>(data, key)` |
| 加载 | `Load<T>(path)` | `LoadFromPrefs<T>(key)` |
| 存在 | `Exists(path)` | `ExistsInPrefs(key)` |
| 删除 | `DeleteData(path)` | `DeletePref(key)` |
| 异步保存 | `SaveAsync<T>(data, path, ct)` | （基于 PlayerPrefs，可在线程池内执行 `SaveToPrefs`） |
| 异步加载 | `LoadAsync<T>(path, ct)` | （类似） |

## 3. 基础类型

~~~csharp
SaveManager.Save(100, "Player/Score");
int score = SaveManager.Load<int>("Player/Score");

SaveManager.Save("Albert", "Player/Name");
SaveManager.Save(true, "Settings/MusicOn");
~~~

## 4. 自定义可序列化类

~~~csharp
[System.Serializable]
public class PlayerInfo
{
    public string name;
    public int level;
    public float health;
}

var info = new PlayerInfo { name = "Albert", level = 5, health = 80.5f };
SaveManager.Save(info, "Player/Info");

PlayerInfo loaded = SaveManager.Load<PlayerInfo>("Player/Info");
if (loaded != null) Debug.Log(loaded.name);
~~~

- 类必须标记 `[Serializable]`，字段为 public 或具备可序列化访问性。
- `JsonUtility` 不支持 `Dictionary`、接口、属性；如需这些结构请手动提供 `ISerializationCallbackReceiver` 或转 JSON 字符串后再存。

## 5. Transform / RectTransform

~~~csharp
// 保存
SaveManager.Save(playerTransform, "Player/Transform");
SaveManager.Save(myRectTransform, "UI/SettingsPanel");

// 加载（会创建隐藏 GameObject，应用结束后请自行 Destroy）
Transform loaded = SaveManager.Load<Transform>("Player/Transform");
RectTransform loadedRect = SaveManager.Load<RectTransform>("UI/SettingsPanel");

// 推荐：直接加载数据对象并自行应用
Save_TransformData td = SaveManager.Load<Save_TransformData>("Player/Transform");
myPlayer.position    = td.position;
myPlayer.rotation    = td.rotation;
myPlayer.localScale  = td.localScale;

Save_RectTransformData rd = SaveManager.Load<Save_RectTransformData>("UI/SettingsPanel");
myRect.anchoredPosition = rd.anchoredPosition;
myRect.eulerAngles      = rd.eulerAngles;
myRect.sizeDelta        = rd.sizeDelta;
~~~

- `Load<Transform>`/`Load<RectTransform>` 创建的 GameObject 设置了 `HideFlags.HideAndDontSave`，但仍需调用方在不需要时 `Destroy` 释放。
- `Save_TransformData`/`Save_RectTransformData` 已包含 `Version` 字段（默认 `1`），便于后续迁移。

## 6. 删除与检查

~~~csharp
if (SaveManager.Exists("Player/Score"))
    SaveManager.DeleteData("Player/Score");

if (SaveManager.ExistsInPrefs("PlayerName"))
    SaveManager.DeletePref("PlayerName");
~~~

不存在时 `DeleteData` / `DeletePref` 会输出错误日志，业务侧可先检查避免噪声。

## 7. 异步版本

~~~csharp
await SaveManager.SaveAsync(info, "Player/Info", cts.Token);
PlayerInfo loaded = await SaveManager.LoadAsync<PlayerInfo>("Player/Info", cts.Token);
~~~

- 内部使用 `UniTask.RunOnThreadPool`，IO 与加解密不会卡主线程。
- `LoadAsync<Transform>`/`LoadAsync<RectTransform>` 仍会创建 GameObject；建议只在主线程访问返回结果。

## 8. 加密说明

- 默认使用 `SaveEncryption` 的内置 32 字节密钥（AES-256），密钥硬编码在 `SaveEncryption` 中。
- 如果需要自定义密钥或切换算法，请直接调用 `SaveEncryption.Save<T>(data, path)` / `Load<T>(path)` 并自行管理密钥和 IV。
- 修改默认密钥会让旧存档读不到，需要做版本迁移。

## 9. 注意事项

- 存档目录位于 `Application.persistentDataPath`，不同平台路径不同。
- 重要存档建议增加备份文件（例如 `path.save.bak`），写入失败时回退。
- 不要在 PlayerPrefs 中存大量数据；大数据使用文件存档。
- `JsonUtility` 序列化不保留多态引用、`Dictionary`、接口；自定义数据结构时注意兼容。
- 异步方法内对 `Transform`/`RectTransform` 的加载仍会创建 GameObject，请确保在合适的生命周期调用与释放。
- 序列化失败的异常会被捕获并写入日志，因此存档接口不会向上抛异常。
