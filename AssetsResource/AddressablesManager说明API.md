# AddressablesManager 使用手册

> 基于 `Assets/Scripts/WManager/AssetsResource/AddressablesManager.cs` 当前源码。
> 单例资源管理器：基于引用计数的资源加载、实例化、缓存、热更新与调试输出。

## 1. 能力概览

- 异步资源加载（UniTask）。
- 资源引用计数 + 实例句柄管理。
- 初始化与目录更新检查。
- 下载大小计算与带进度回调的下载。
- 实例按 key 批量释放、按实例释放。
- 调试输出当前已加载/已实例化数量。
- Key 合法性校验、类型兼容性检查。

继承 `SingletonBehaviour<AddressablesManager>`，首次访问 `Instance` 时会自动创建同名 GameObject 并 `DontDestroyOnLoad`。

## 2. 初始化与热更新

~~~csharp
await AddressablesManager.Instance.InitializeAsync();
await AddressablesManager.Instance.UpdateCatalogsAsync();

long size = await AddressablesManager.Instance.CheckUpdateSizeAsync("Preload");
if (size > 0)
{
    var progress = Progress.Create<float>(v => Debug.Log($"进度：{v * 100}%"));
    bool ok = await AddressablesManager.Instance.DownloadUpdateAsync("Preload", progress);
}

await AddressablesManager.Instance.ClearDownloadCacheAsync("Preload");
~~~

| API | 说明 |
|---|---|
| `IsAddressablesInitialized` | 当前是否已初始化 |
| `InitializeAsync()` | 初始化 Addressables 系统 |
| `UpdateCatalogsAsync()` | 检查并更新资源目录 |
| `GetDownloadSizeAsync(label)` | 查询 label 的待下载大小 |
| `CheckUpdateSizeAsync(label = "default")` | 检查该 label 是否有可用更新及对应大小 |
| `DownloadUpdateAsync(label, onProgress)` | 下载该 label 资源；`onProgress` 形如 `(current, total)` |
| `ClearDownloadCacheAsync(label)` | 清除该 label 的本地下载缓存 |

## 3. 资源加载与释放

~~~csharp
Sprite icon = await AddressablesManager.Instance.LoadAsync<Sprite>("Icon/Bag");
// 使用...
AddressablesManager.Instance.Release("Icon/Bag");

bool loaded = await AddressablesManager.Instance.PreloadAsync<AudioClip>("BGM/Main");
var (ok, asset) = await AddressablesManager.Instance.TryLoadAsync<Texture2D>("Images/banner");

bool isLoaded = AddressablesManager.Instance.IsLoaded("Icon/Bag");
int refCount = AddressablesManager.Instance.GetAssetRefCount("Icon/Bag");
await AddressablesManager.Instance.ExistsAsync("Icon/Bag", typeof(Sprite));
~~~

| API | 说明 |
|---|---|
| `LoadAsync<T>(key)` | 加载并缓存，已加载时直接增加引用计数 |
| `PreloadAsync<T>(key)` | 预加载，不返回资源 |
| `TryLoadAsync<T>(key)` | 返回 `(bool success, T asset)` 元组 |
| `Release(key)` | 引用计数 -1，归零时真正释放 |
| `IsLoaded(key)` | 当前是否已加载且状态成功 |
| `GetAssetRefCount(key)` | 当前引用计数 |
| `ExistsAsync(key, type = null)` | 检查 key 是否存在，可指定类型 |

> 同一 key 不要先以 `Texture2D` 加载再以 `Sprite` 等其他不兼容类型加载；管理器内置类型检查。

## 4. 实例化与释放

~~~csharp
var go = await AddressablesManager.Instance.InstantiateAsync("UI/SettingsPanel", parent, false, cts.Token);
// 使用...
AddressablesManager.Instance.ReleaseInstance(go);

int n = AddressablesManager.Instance.ReleaseInstanceByKey("UI/SettingsPanel");
int total = AddressablesManager.Instance.GetInstanceCount();
int sameKey = AddressablesManager.Instance.GetInstanceCount("UI/SettingsPanel");
~~~

| API | 说明 |
|---|---|
| `InstantiateAsync(key, parent=null, worldPositionStays=false, ct=default)` | 实例化 Prefab；失败返回 null |
| `ReleaseInstance(GameObject)` | 释放单个实例（优先使用内部记录的 handle 释放） |
| `ReleaseInstanceByKey(key)` | 释放该 key 名下所有实例，返回实际释放数量 |
| `GetInstanceCount()` / `GetInstanceCount(key)` | 当前实例数量 |

> 通过 `InstantiateAsync` 创建的对象**必须**用 `ReleaseInstance` 或 `ReleaseInstanceByKey`，不要直接 `Destroy`，否则 Addressables 内部引用计数会错乱。

## 5. 释放全部

~~~csharp
AddressablesManager.Instance.ReleaseAllInstances(); // 仅释放实例
AddressablesManager.Instance.ReleaseAllAssets();    // 仅释放资源
AddressablesManager.Instance.ReleaseAll();          // 全部释放

// 单例销毁时也会自动 ReleaseAll
AddressablesManager.Instance.PrintDebugInfo();
// [AddressablesManager] 已加载资源数 = X, 已实例数 = Y
~~~

## 6. 释放与生命周期规则

1. 每次 `LoadAsync` 必须对应一次 `Release`；引用计数归零时才真正释放。
2. `InstantiateAsync` 创建的对象必须走 `ReleaseInstance` / `ReleaseInstanceByKey`。
3. `ReleaseInstance` 在找不到内部记录时会尝试 `Addressables.ReleaseInstance(go)` 兜底，但若对象不是通过 `InstantiateAsync` 创建，则只会在控制台输出警告。
4. `Destroy` 之前如果忘记 `ReleaseInstance`，该实例会残留在 Addressables 内部，建议在 `OnDestroy` 统一调用。
5. 单例销毁时会自动 `ReleaseAll`，所以无需在退出流程手动释放。

## 7. 注意事项

- 单例 `Instance` 在首次访问时懒创建，挂在 `[AddressablesManager]` GameObject 上。
- 调试时可调用 `PrintDebugInfo()` 快速确认内存状态。
- 异步加载支持 `CancellationToken`；取消后 `LoadAsync` 返回默认值、`InstantiateAsync` 返回 null。
- `DownloadUpdateAsync` 的 `onProgress` 为 `(long current, long total)`，单位是字节。
