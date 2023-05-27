using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Events;

namespace WManager
{
    ///<summary>
    ///功能：AssetBundle加载器
    ///</summary>
    public class AssetBundleLoader : MonoBehaviour
    {
        private static readonly Dictionary<string, AssetBundle> loadedAssetBundles = new Dictionary<string, AssetBundle>();
        private static Dictionary<AssetBundle, List<GameObject>> referencedObjects = new Dictionary<AssetBundle, List<GameObject>>();
        public static Action<float> onLoading;
        private const string ASSET_BUNDLE_MANAGER_NAME = "AssetBundleLoader";
        private static AssetBundleLoader instance;
        public static AssetBundleLoader Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new GameObject(ASSET_BUNDLE_MANAGER_NAME).AddComponent<AssetBundleLoader>();
                    DontDestroyOnLoad(instance);
                }
                return instance;
            }
        }

        /// <summary>
        /// 本地同步加载AssetBundle包并且获取GameObject
        /// </summary>
        /// <param name="path">streamingAssets下的文件名</param>
        /// <param name="name">要加载的物体名</param>
        /// <returns>回传的游戏物体</returns>
        public static GameObject LoadObjFromFile(string path, string name)
        {
            //传入这个AB包存在本地的路径加载本地资源
            AssetBundle ab = AssetBundle.LoadFromFile(GetAbsolutePath(path));
            loadedAssetBundles.Add(path, ab);

            //获取可使用AssetBundle这个类里面的LoadAsset<T>(string name)方法获取资源
            return ab.LoadAsset<GameObject>(name);
        }
        /// <summary>
        /// 本地同步加载AssetBundle包
        /// </summary>
        /// <param name="path">streamingAssets下的文件名</param> 
        /// <returns>AssetBundle包</returns>
        public static AssetBundle LoadABFromFile(string path)
        {
            // 传入这个AB包存在本地的路径加载本地资源
            AssetBundle ab = AssetBundle.LoadFromFile(GetAbsolutePath(path));
            loadedAssetBundles.Add(path, ab);
            return ab;
        }

        /// <summary>
        /// 本地异步加载AssetBundle包并且得到GameObject
        /// </summary>
        /// <param name="path">streamingAssets下的文件名</param>
        /// <param name="name">要加载的物体名</param>
        /// <param name="callBack">实例化这个游戏物体的回调方法</param>
        public static void LoadObjFromFileAsync(string path, string name, UnityAction<GameObject, float> callBack)
        {
            //开始携程
            Instance.StartCoroutine(LoadGameObjectFromFileAsyncCoroutine(path, name, callBack));
        }
        static IEnumerator LoadGameObjectFromFileAsyncCoroutine(string path, string name, UnityAction<GameObject, float> callBack)
        {
            AssetBundleCreateRequest request = AssetBundle.LoadFromFileAsync(GetAbsolutePath(path));
            float progress = 0;
            while (!request.isDone)
            {
                progress = request.progress;
                callBack(null, progress);
                yield return null;
            }
            if (request.isDone)
            {
                AssetBundle ab = request.assetBundle;//获取加载出来的东西
                loadedAssetBundles.Add(path, ab);
                callBack(ab.LoadAsset<GameObject>(name), 1);
            }
            else
            {
                Debug.LogError($" 从 {path} 加载AssetBundle失败");
            }
        }
        /// <summary>
        /// 本地异步加载AssetBundle包
        /// </summary>
        /// <param name="path">streamingAssets下的文件名</param>
        /// <param name="callback">加载完成的回调</param>
        public static void LoadABFromFileAsync(string path, UnityAction<AssetBundle, float> callback)
        {
            Instance.StartCoroutine(LoadAssetBundleFromFileAsyncCoroutine(path, callback));
        }
        private static IEnumerator LoadAssetBundleFromFileAsyncCoroutine(string path, UnityAction<AssetBundle, float> callback)
        {
            AssetBundleCreateRequest request = AssetBundle.LoadFromFileAsync(GetAbsolutePath(path));
            float progress = 0;
            while (!request.isDone)
            {
                progress = request.progress;
                callback(null, progress);
                yield return null;
            }
            if (request.isDone)
            {
                AssetBundle ab = request.assetBundle;
                loadedAssetBundles.Add(path, ab);
                callback(ab, 1);
            }
            else
            {
                Debug.LogError($" 从 {path} 加载AssetBundle失败");
            }
        }

        /// <summary>
        /// 从内存加载AssetBundle包
        /// </summary>
        /// <param name="bytes">AssetBundle包的二进制数据</param>
        /// <returns>AssetBundle包</returns>
        public static AssetBundle LoadABFromMemory(byte[] bytes)
        {
            AssetBundle ab = AssetBundle.LoadFromMemory(bytes);
            loadedAssetBundles.Add(ab.name, ab); // 将 AssetBundle 的 name 作为键,AssetBundle 作为值加入字典
            return ab;
        }

        /// <summary>
        /// 服务器下载AB包并得到GameObject
        /// </summary>
        /// <param name="ABName">AB包名称，如："model.ab"</param>
        /// <param name="name">包内需要加载的名称</param>
        /// <param name="callBack">回调的序列化对象</param>
        /// <returns></returns>
        public static void LoadObjFromWeb(string ABName, string name, UnityAction<GameObject, float> callBack)
        {
            Instance.StartCoroutine(LoadObjFromWebCoroutine(ABName, name, callBack));
        }
        static IEnumerator LoadObjFromWebCoroutine(string ABName, string name, UnityAction<GameObject, float> callBack)
        {
            //服务器上下载
            UnityWebRequest request = UnityWebRequestAssetBundle.GetAssetBundle(GetAbsolutePath(ABName));
            yield return request.SendWebRequest();

            float progress = 0;
            while (!request.isDone)
            {
                progress = request.downloadProgress;
                callBack(null, progress);
                //监听加载进度
                if (onLoading != null)
                {
                    onLoading(progress);
                }
                yield return null;
            }

            if (!string.IsNullOrEmpty(request.error))//判断下载有没有出错,request.error表示错误信息
            {
                Debug.LogError($" 从 {ABName} 加载AssetBundle失败.错误为: {request.error}");//输出错误
                yield break;//退出携程
            }
            else
            {
                //直接获取到UnityWebRequest下载到的东西然后强转成DownloadHandlerAssetBundle,然后再获取到AssetBundle
                AssetBundle ab = (request.downloadHandler as DownloadHandlerAssetBundle).assetBundle;
                GameObject prefab = ab.LoadAsset<GameObject>(name);
                loadedAssetBundles.Add(ABName, ab);
                callBack(prefab, 1);
                //加载完成,移除监听器
                onLoading = null;
            }
        }
        /// <summary>
        /// 从网络URL异步加载AssetBundle包
        /// </summary>
        /// <param name="url">AssetBundle包的URL</param>
        /// <param name="callback">加载完成的回调</param>
        public static void LoadABFromWeb(string url, UnityAction<AssetBundle, float> callback)
        {
            Instance.StartCoroutine(LoadAssetBundleFromWebCoroutine(url, callback));
        }
        private static IEnumerator LoadAssetBundleFromWebCoroutine(string url, UnityAction<AssetBundle, float> callback)
        {
            UnityWebRequest request = UnityWebRequestAssetBundle.GetAssetBundle(url);
            yield return request.SendWebRequest();

            float progress = 0;
            while (!request.isDone)
            {
                progress = request.downloadProgress;
                callback(null, progress);
                //监听加载进度
                if (onLoading != null)
                {
                    onLoading(progress);
                }
                yield return null;
            }

            if (!string.IsNullOrEmpty(request.error))
            {
                Debug.LogError($" 从 {url} 加载AssetBundle失败.错误为: {request.error}");
            }
            else
            {
                var downloadHandler = request.downloadHandler as DownloadHandlerAssetBundle;
                AssetBundle ab = downloadHandler.assetBundle;
                loadedAssetBundles.Add(url, ab);
                callback(ab, 1);
                //加载完成,移除监听器
                onLoading = null;
            }
        }
        static string GetAbsolutePath(string path)
        {
            //本地加载路径前面要加上file:// 否则会出错
#if UNITY_WIN_STANDALONE || UNITY_IPHONE && !UNITY_EDITOR
            return "file://"+ Application.streamingAssetsPath + path;
#else
            return Application.streamingAssetsPath + "/" + path;
#endif
        }
        /// <summary>
        /// 根据名称查找AssetBundle
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static AssetBundle GetAssetBundle(string name)
        {
            loadedAssetBundles.TryGetValue(name, out AssetBundle ab);
            return ab;
        }

        /// <summary>
        /// 根据名称卸载AssetBundle
        /// </summary>
        /// <param name="name">AB包名</param>
        public static void UnloadAssetBundle(string name)
        {
            if (loadedAssetBundles.ContainsKey(name))
            {
                loadedAssetBundles[name].Unload(false);
                loadedAssetBundles.Remove(name);
            }
        }
        /// <summary>
        /// 卸载指定AssetBundle
        /// </summary>
        /// <param name="ab">AB包</param>
        public static void UnloadAssetBundle(AssetBundle ab)
        {
            if (loadedAssetBundles.ContainsValue(ab))
            {
                string name = null;
                foreach (var pair in loadedAssetBundles)
                {
                    if (pair.Value == ab)
                    {
                        name = pair.Key;
                        break;
                    }
                }
                if (name != null)
                {
                    loadedAssetBundles.Remove(name);
                    ab.Unload(false);
                }
            }
        }
        /// <summary>
        /// 卸载所有AssetBundle
        /// </summary>
        public static void UnloadAllAssetBundles()
        {
            // 遍历字典所有键值对
            foreach (var pair in loadedAssetBundles)
            {
                // 获取AssetBundle
                AssetBundle ab = pair.Value;
                // 从字典中移除
                loadedAssetBundles.Remove(pair.Key);
                // 卸载AssetBundle
                ab.Unload(false);
            }
        }
    }
}


/*******************使用案例*******************
**本地同步加载 AssetBundle**
// 加载 AssetBundle
AssetBundle ab = AssetBundleLoader.LoadAssetBundleFromFile("assetbundle1");
// 从 AssetBundle 加载预制体
GameObject prefab = ab.LoadAsset<GameObject>("Prefab1");
// 实例化预制体
GameObject obj = Instantiate(prefab);

**本地异步加载 AssetBundle**
AssetBundleLoader.LoadABFromFileAsync("assetbundle1", (ab, progress) => 
{
    // 从 AssetBundle 加载预制体
    GameObject prefab = ab.LoadAsset<GameObject>("Prefab1");
    // 实例化预制体
    GameObject obj = Instantiate(prefab);
    // progress 介于 0 到 1,表示加载进度
});

**从内存加载 AssetBundle * *
// 读取 AssetBundle 文件到字节数组
byte[] bytes = File.ReadAllBytes("assetbundle1");
// 从字节数组加载 AssetBundle
AssetBundle ab = AssetBundleLoader.LoadAssetBundleFromMemory(bytes);
// 从 AssetBundle 加载预制体
GameObject prefab = ab.LoadAsset<GameObject>("Prefab1");
// 实例化预制体 
GameObject obj = Instantiate(prefab);

**从网络 URL 加载 AssetBundle**
AssetBundleLoader.LoadAssetBundleFromWeb("http://yourserver/assetbundle1", (ab, progress) => 
{
    // 从 AssetBundle 加载预制体
    GameObject prefab = ab.LoadAsset<GameObject>("Prefab1");
    // 实例化预制体
    GameObject obj = Instantiate(prefab);
    // progress 介于 0 到 1,表示加载进度
});

*******************使用案例*******************/