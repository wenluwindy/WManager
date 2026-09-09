using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace WManager
{
    /// <summary>
    /// 资源加载抽象。
    /// UIManager 通过此接口与具体资源加载实现（Addressables / Resources / AssetBundle）解耦。
    /// </summary>
    public interface IAssetLoader
    {
        /// <summary>
        /// 实例化 GameObject
        /// </summary>
        UniTask<GameObject> InstantiateAsync(string key, Transform parent = null, bool worldPositionStays = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// 释放实例
        /// </summary>
        void ReleaseInstance(GameObject instance);

        /// <summary>
        /// 预加载资源（可选；某些实现可能不缓存）
        /// </summary>
        UniTask<bool> PreloadAsync<T>(string key) where T : class;
    }
}