using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace WManager
{
    /// <summary>
    /// Addressables 管理器
    /// 功能：
    /// 1. 资源加载 / 缓存 / 引用计数
    /// 2. 实例化 / 实例释放
    /// 3. key 是否存在检查
    /// 4. 安全释放 + 异常保护
    /// 依赖：UniTask
    /// 继承 SingletonBehaviour，统一单例生命周期。
    /// </summary>
    public class AddressablesManager : SingletonBehaviour<AddressablesManager>, IAssetLoader
    {
        /// <summary>
        /// 资源缓存项
        /// </summary>
        private class AssetEntry
        {
            public AsyncOperationHandle Handle;
            public Type AssetType;
            public int RefCount;
        }

        /// <summary>
        /// 实例缓存项
        /// </summary>
        private class InstanceEntry
        {
            public string Key;
            public AsyncOperationHandle<GameObject> Handle;
        }

        /// <summary>
        /// 已加载资源：key -> entry
        /// </summary>
        private readonly Dictionary<string, AssetEntry> _assetEntries = new();

        /// <summary>
        /// 已实例化对象：instanceID -> entry
        /// </summary>
        private readonly Dictionary<int, InstanceEntry> _instanceEntries = new();

        protected override void Awake()
        {
            // 基类负责单例注册与重复销毁检测
            base.Awake();
        }

        #region HotUpdate

        private bool _addressablesInitialized;

        /// <summary>
        /// Addressables 是否已完成初始化
        /// </summary>
        public bool IsAddressablesInitialized => _addressablesInitialized;

        /// <summary>
        /// 初始化 Addressables。
        ///
        /// 【顺序要求】调用之前必须已经把远程加载地址写好（见 GameLauncher.ApplyRemoteUrl）。
        /// Profile 里 {xxx} 形式的运行时变量只在加载 Catalog 的那一刻求值一次，
        /// 求值结果会被固化进所有资源定位信息，之后再改就没用了。
        /// </summary>
        public async UniTask<bool> InitializeAsync()
        {
            if (_addressablesInitialized)
                return true;

            try
            {
                await Addressables.InitializeAsync().ToUniTask();
                _addressablesInitialized = true;
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[AddressablesManager] Addressables 初始化失败：\n{e}");
                return false;
            }
        }

        /// <summary>
        /// 检查并拉取最新的远程 Catalog
        /// </summary>
        /// <returns>是否真的更新了 Catalog</returns>
        public async UniTask<bool> UpdateCatalogsAsync()
        {
            AsyncOperationHandle<List<string>> checkHandle = default;

            try
            {
                checkHandle = Addressables.CheckForCatalogUpdates(false);
                await checkHandle.ToUniTask();
                var catalogs = checkHandle.Result;

                if (catalogs == null || catalogs.Count == 0)
                    return false;

                var updateHandle = Addressables.UpdateCatalogs(catalogs, false);
                try
                {
                    await updateHandle.ToUniTask();
                }
                finally
                {
                    if (updateHandle.IsValid())
                        Addressables.Release(updateHandle);
                }

                Debug.Log($"[AddressablesManager] 已更新 {catalogs.Count} 个 Catalog");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[AddressablesManager] 更新 Catalog 失败：\n{e}");
                return false;
            }
            finally
            {
                if (checkHandle.IsValid())
                    Addressables.Release(checkHandle);
            }
        }

        /// <summary>
        /// 获取指定标签资源还需要下载的字节数。
        /// 必须在 <see cref="UpdateCatalogsAsync"/> 之后调用，否则算的是旧 Catalog 的账。
        /// </summary>
        /// <param name="label">资源标签</param>
        /// <returns>需要下载的字节数，0 表示无需下载</returns>
        public async UniTask<long> GetDownloadSizeAsync(string label)
        {
            AsyncOperationHandle<IList<IResourceLocation>> locationHandle = default;

            try
            {
                locationHandle = Addressables.LoadResourceLocationsAsync(label);
                await locationHandle.ToUniTask();
                var locations = locationHandle.Result;

                if (locations == null || locations.Count == 0)
                {
                    Debug.LogWarning($"[AddressablesManager] 未找到标签为 '{label}' 的资源，跳过下载。");
                    return 0;
                }

                var sizeHandle = Addressables.GetDownloadSizeAsync(label);
                try
                {
                    await sizeHandle.ToUniTask();
                    return sizeHandle.Result;
                }
                finally
                {
                    if (sizeHandle.IsValid())
                        Addressables.Release(sizeHandle);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[AddressablesManager] 获取下载大小失败：label = {label}\n{e}");
                return 0;
            }
            finally
            {
                if (locationHandle.IsValid())
                    Addressables.Release(locationHandle);
            }
        }

        /// <summary>
        /// 一步完成：初始化 → 更新 Catalog → 返回待下载大小
        /// </summary>
        /// <param name="label">资源标签</param>
        /// <returns>需要下载的字节数，0 表示不需要更新</returns>
        public async UniTask<long> CheckUpdateSizeAsync(string label = "default")
        {
            if (!await InitializeAsync())
                return 0;

            // 顺序不能反：先把 Catalog 换成最新的，再去查资源和大小，
            // 否则新增的资源在旧 Catalog 里查不到，会被误判成"不需要更新"。
            await UpdateCatalogsAsync();

            return await GetDownloadSizeAsync(label);
        }

        /// <summary>
        /// 下载更新资源，并汇报进度
        /// </summary>
        /// <param name="label">资源标签</param>
        /// <param name="onProgress">进度回调 (已下载字节数, 总字节数)</param>
        public async UniTask<bool> DownloadUpdateAsync(string label, Action<long, long> onProgress = null)
        {
            AsyncOperationHandle downloadHandle = default;

            try
            {
                downloadHandle = Addressables.DownloadDependenciesAsync(label, false);

                // 用 GetDownloadStatus 而不是 PercentComplete：前者给的是真实字节数，
                // 能直接喂给"已下载 X MB / 共 Y MB"的界面。
                while (!downloadHandle.IsDone)
                {
                    if (onProgress != null)
                    {
                        var status = downloadHandle.GetDownloadStatus();
                        onProgress(status.DownloadedBytes, status.TotalBytes);
                    }

                    await UniTask.Yield();
                }

                if (downloadHandle.Status != AsyncOperationStatus.Succeeded)
                {
                    Debug.LogError($"[AddressablesManager] 下载更新资源失败：label = {label}\n{downloadHandle.OperationException}");
                    return false;
                }

                if (onProgress != null)
                {
                    var status = downloadHandle.GetDownloadStatus();
                    onProgress(status.TotalBytes, status.TotalBytes);
                }

                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[AddressablesManager] 下载更新资源异常：label = {label}\n{e}");
                return false;
            }
            finally
            {
                if (downloadHandle.IsValid())
                    Addressables.Release(downloadHandle);
            }
        }

        /// <summary>
        /// 清掉指定标签已缓存的 Bundle。下载损坏需要强制重下时用。
        /// </summary>
        public async UniTask<bool> ClearDownloadCacheAsync(string label)
        {
            try
            {
                var handle = Addressables.ClearDependencyCacheAsync(label, false);
                try
                {
                    await handle.ToUniTask();
                    return handle.Result;
                }
                finally
                {
                    if (handle.IsValid())
                        Addressables.Release(handle);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[AddressablesManager] 清理下载缓存失败：label = {label}\n{e}");
                return false;
            }
        }

        #endregion

        #region Utils
        /// <summary>
        /// 判断 key 是否为空
        /// </summary>
        private bool IsInvalidKey(string key, string callerName)
        {
            if (!string.IsNullOrWhiteSpace(key))
                return false;

            Debug.LogError($"[AddressablesManager] {callerName} 失败：key 为空。");
            return true;
        }

        /// <summary>
        /// 检查 Addressable key 是否存在
        /// </summary>
        public async UniTask<bool> ExistsAsync(string key, Type type = null)
        {
            if (string.IsNullOrWhiteSpace(key))
                return false;

            AsyncOperationHandle<IList<IResourceLocation>> locationHandle = default;

            try
            {
                locationHandle = Addressables.LoadResourceLocationsAsync(key, type);
                await locationHandle.ToUniTask();

                return locationHandle.Result != null && locationHandle.Result.Count > 0;
            }
            catch (Exception e)
            {
                Debug.LogError($"[AddressablesManager] ExistsAsync 异常：key = {key}\n{e}");
                return false;
            }
            finally
            {
                if (locationHandle.IsValid())
                    Addressables.Release(locationHandle);
            }
        }

        /// <summary>
        /// 检查缓存中的资源类型是否匹配
        /// </summary>
        private bool IsTypeCompatible(Type cachedType, Type requestType)
        {
            return cachedType == requestType
                   || requestType.IsAssignableFrom(cachedType)
                   || cachedType.IsAssignableFrom(requestType);
        }

        #endregion

        #region Load

        /// <summary>
        /// 加载资源（自动缓存 + 引用计数）
        /// 相同 key 重复加载时，会直接复用缓存并增加引用计数。
        /// </summary>
        public async UniTask<T> LoadAsync<T>(string key) where T : class
        {
            if (IsInvalidKey(key, nameof(LoadAsync)))
                return default;

            // 已缓存
            if (_assetEntries.TryGetValue(key, out var cachedEntry))
            {
                if (!cachedEntry.Handle.IsValid())
                {
                    Debug.LogWarning($"[AddressablesManager] 发现无效缓存句柄，已移除：{key}");
                    _assetEntries.Remove(key);
                }
                else
                {
                    if (!IsTypeCompatible(cachedEntry.AssetType, typeof(T)))
                    {
                        Debug.LogError(
                            $"[AddressablesManager] LoadAsync 类型不匹配：key = {key}，" +
                            $"已缓存类型 = {cachedEntry.AssetType.Name}，请求类型 = {typeof(T).Name}");
                        return default;
                    }

                    if (cachedEntry.Handle.Status == AsyncOperationStatus.Succeeded)
                    {
                        cachedEntry.RefCount++;
                        return cachedEntry.Handle.Result as T;
                    }

                    Debug.LogWarning($"[AddressablesManager] 缓存句柄状态异常，已移除：{key}");
                    _assetEntries.Remove(key);
                }
            }

            // 先检查 key 是否存在
            bool exists = await ExistsAsync(key, typeof(T));
            if (!exists)
            {
                Debug.LogError($"[AddressablesManager] LoadAsync 失败：资源不存在或类型不匹配，key = {key}，type = {typeof(T).Name}");
                return default;
            }

            AsyncOperationHandle<T> handle = default;

            try
            {
                handle = Addressables.LoadAssetAsync<T>(key);
                await handle.ToUniTask();

                if (handle.Status != AsyncOperationStatus.Succeeded || handle.Result == null)
                {
                    Debug.LogError($"[AddressablesManager] LoadAsync 失败：key = {key}");
                    if (handle.IsValid())
                        Addressables.Release(handle);
                    return default;
                }

                _assetEntries[key] = new AssetEntry
                {
                    Handle = handle,
                    AssetType = typeof(T),
                    RefCount = 1
                };

                return handle.Result;
            }
            catch (Exception e)
            {
                Debug.LogError($"[AddressablesManager] LoadAsync 异常：key = {key}\n{e}");

                if (handle.IsValid())
                    Addressables.Release(handle);

                return default;
            }
        }

        /// <summary>
        /// 预加载资源
        /// </summary>
        public async UniTask<bool> PreloadAsync<T>(string key) where T : class
        {
            var result = await LoadAsync<T>(key);
            return result != null;
        }

        /// <summary>
        /// 尝试加载，返回成功标记
        /// </summary>
        public async UniTask<(bool success, T asset)> TryLoadAsync<T>(string key) where T : class
        {
            var asset = await LoadAsync<T>(key);
            return (asset != null, asset);
        }

        /// <summary>
        /// 释放已加载资源（引用计数 -1）
        /// 注意：这里只释放通过 LoadAsync 加载的资源
        /// </summary>
        public void Release(string key)
        {
            if (IsInvalidKey(key, nameof(Release)))
                return;

            if (!_assetEntries.TryGetValue(key, out var entry))
            {
                Debug.LogWarning($"[AddressablesManager] Release 忽略：未找到已加载资源，key = {key}");
                return;
            }

            entry.RefCount--;

            if (entry.RefCount > 0)
                return;

            if (entry.Handle.IsValid())
                Addressables.Release(entry.Handle);

            _assetEntries.Remove(key);
        }

        /// <summary>
        /// 判断资源是否已加载
        /// </summary>
        public bool IsLoaded(string key)
        {
            return !string.IsNullOrWhiteSpace(key)
                   && _assetEntries.TryGetValue(key, out var entry)
                   && entry.Handle.IsValid()
                   && entry.Handle.Status == AsyncOperationStatus.Succeeded;
        }

        /// <summary>
        /// 获取资源引用计数
        /// </summary>
        public int GetAssetRefCount(string key)
        {
            return _assetEntries.TryGetValue(key, out var entry) ? entry.RefCount : 0;
        }

        #endregion

        #region Instantiate

        /// <summary>
        /// 实例化 Addressable 预制体
        /// 注意：实例生命周期与 LoadAsync 加载出来的资源生命周期分开管理
        /// </summary>
        public async UniTask<GameObject> InstantiateAsync(string key, Transform parent = null, bool worldPositionStays = false, CancellationToken cancellationToken = default)
        {
            {
                if (IsInvalidKey(key, nameof(InstantiateAsync)))
                    return null;

                bool exists = await ExistsAsync(key, typeof(GameObject));
                if (!exists)
                {
                    Debug.LogError($"[AddressablesManager] InstantiateAsync 失败：Prefab 不存在，key = {key}");
                    return null;
                }

                AsyncOperationHandle<GameObject> handle = default;

                try
                {
                    handle = Addressables.InstantiateAsync(key, parent, worldPositionStays);
                    await handle.ToUniTask();

                    if (handle.Status != AsyncOperationStatus.Succeeded || handle.Result == null)
                    {
                        Debug.LogError($"[AddressablesManager] InstantiateAsync 失败：key = {key}");

                        if (handle.IsValid())
                            Addressables.Release(handle);

                        return null;
                    }

                    var instance = handle.Result;
                    int instanceId = instance.GetInstanceID();

                    _instanceEntries[instanceId] = new InstanceEntry
                    {
                        Key = key,
                        Handle = handle
                    };

                    return instance;
                }
                catch (Exception e)
                {
                    Debug.LogError($"[AddressablesManager] InstantiateAsync 异常：key = {key}\n{e}");

                    if (handle.IsValid())
                        Addressables.Release(handle);

                    return null;
                }
            }
        }

        /// <summary>
        /// 释放实例
        /// 注意：这里只释放通过 InstantiateAsync 创建的实例
        /// </summary>
        public void ReleaseInstance(GameObject instance)
        {
            if (instance == null)
            {
                Debug.LogWarning("[AddressablesManager] ReleaseInstance 失败：instance 为空。");
                return;
            }

            int instanceId = instance.GetInstanceID();

            // 优先使用我们记录的 handle 释放
            if (_instanceEntries.TryGetValue(instanceId, out var entry))
            {
                bool released = false;

                if (entry.Handle.IsValid())
                    released = Addressables.ReleaseInstance(entry.Handle);
                else
                    released = Addressables.ReleaseInstance(instance);

                _instanceEntries.Remove(instanceId);

                if (!released)
                {
                    Debug.LogWarning($"[AddressablesManager] ReleaseInstance 失败：key = {entry.Key}, instance = {instance.name}");
                }

                return;
            }

            // 兜底：尝试直接按实例释放
            bool fallbackReleased = Addressables.ReleaseInstance(instance);
            if (!fallbackReleased)
            {
                Debug.LogWarning($"[AddressablesManager] ReleaseInstance 失败：对象可能不是由 Addressables.InstantiateAsync 创建。instance = {instance.name}");
            }
        }

        /// <summary>
        /// 按 key 释放实例
        /// 同一个 key 可能实例化过多个对象，这里会把它们全部释放。
        /// 注意：这里只释放通过 InstantiateAsync 创建的实例
        /// </summary>
        /// <param name="key">实例化时使用的 Addressable key</param>
        /// <returns>实际释放成功的实例数量</returns>
        public int ReleaseInstanceByKey(string key)
        {
            if (IsInvalidKey(key, nameof(ReleaseInstanceByKey)))
                return 0;

            // 先收集再释放：ReleaseInstance 会改动 _instanceEntries，不能边遍历边删。
            var targetIds = new List<int>();

            foreach (var kv in _instanceEntries)
            {
                if (kv.Value.Key == key)
                    targetIds.Add(kv.Key);
            }

            if (targetIds.Count == 0)
            {
                Debug.LogWarning($"[AddressablesManager] ReleaseInstanceByKey 忽略：未找到该 key 的实例，key = {key}");
                return 0;
            }

            int releasedCount = 0;

            foreach (var id in targetIds)
            {
                if (!_instanceEntries.TryGetValue(id, out var entry))
                    continue;

                _instanceEntries.Remove(id);

                if (!entry.Handle.IsValid())
                    continue;

                if (Addressables.ReleaseInstance(entry.Handle))
                    releasedCount++;
                else
                    Debug.LogWarning($"[AddressablesManager] ReleaseInstanceByKey 释放失败：key = {key}, instanceId = {id}");
            }

            return releasedCount;
        }

        /// <summary>
        /// 获取当前实例数量
        /// </summary>
        public int GetInstanceCount()
        {
            return _instanceEntries.Count;
        }

        /// <summary>
        /// 获取指定 key 当前的实例数量
        /// </summary>
        public int GetInstanceCount(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return 0;

            int count = 0;

            foreach (var kv in _instanceEntries)
            {
                if (kv.Value.Key == key)
                    count++;
            }

            return count;
        }

        #endregion

        #region Release All

        /// <summary>
        /// 释放全部实例
        /// </summary>
        public void ReleaseAllInstances()
        {
            var ids = new List<int>(_instanceEntries.Keys);

            foreach (var id in ids)
            {
                if (!_instanceEntries.TryGetValue(id, out var entry))
                    continue;

                if (entry.Handle.IsValid())
                    Addressables.ReleaseInstance(entry.Handle);
            }

            _instanceEntries.Clear();
        }

        /// <summary>
        /// 释放全部已加载资源
        /// </summary>
        public void ReleaseAllAssets()
        {
            foreach (var kv in _assetEntries)
            {
                if (kv.Value.Handle.IsValid())
                    Addressables.Release(kv.Value.Handle);
            }

            _assetEntries.Clear();
        }

        /// <summary>
        /// 强制释放全部内容（实例 + 资源）
        /// </summary>
        public void ReleaseAll()
        {
            ReleaseAllInstances();
            ReleaseAllAssets();
        }

        #endregion

        #region Debug

        public void PrintDebugInfo()
        {
            Debug.Log(
                $"[AddressablesManager] 已加载资源数 = {_assetEntries.Count}, 已实例数 = {_instanceEntries.Count}");
        }

        #endregion

        protected override void OnDestroy()
        {
            if (Instance == this)
            {
                ReleaseAll();
            }
        }
    }
}
