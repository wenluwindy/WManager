using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WManager
{
    /// <summary>
    /// Resource资源加载管理器
    /// </summary>
    public class ResourceManager : MonoBehaviour
    {
        private const string ASSET_BUNDLE_MANAGER_NAME = "ResourceManager";
        private static ResourceManager instance;
        public static ResourceManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new GameObject(ASSET_BUNDLE_MANAGER_NAME).AddComponent<ResourceManager>();
                    DontDestroyOnLoad(instance);
                }
                return instance;
            }
        }
        // 自定义回调委托
        public delegate void ResourceLoadedCallback(UnityEngine.Object resource);

        /// <summary>
        /// 加载资源的异步方法
        /// </summary>
        /// <param name="resourcePath">资源加载地址</param>
        /// <param name="callback">回调方法</param>
        public static void LoadResourceAsync(string resourcePath, ResourceLoadedCallback callback)
        {
            Instance.StartCoroutine(LoadResourceCoroutine(resourcePath, callback));
        }

        static IEnumerator LoadResourceCoroutine(string resourcePath, ResourceLoadedCallback callback)
        {
            ResourceRequest request = Resources.LoadAsync(resourcePath);

            while (!request.isDone)
            {
                yield return null;
            }

            if (request.asset != null)
            {
                if (callback != null)
                {
                    callback(request.asset);
                }
            }
            else
            {
                Debug.LogError($" 从 {resourcePath} 加载资源失败");
            }
        }
    }
}
