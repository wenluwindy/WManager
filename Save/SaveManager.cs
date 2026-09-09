using System.IO;
using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace WManager
{
    /// <summary> 存档系统（纯静态工具类，不需要 MonoBehaviour） </summary>
    public static class SaveManager
    {

        /// <summary> 检查特定文件是否已经被保存</summary>
        /// <param name="path">文件路径，如“Albert”。</param>
        public static bool Exists(string path)
        {
            string dataPath = Path.Combine(Application.persistentDataPath, path + ".save");
            return File.Exists(dataPath);
        }
        /// <summary> 检查特定键是否已经被保存 </summary>
        /// <param name="key">保存的键，如“Albert”。</param>
        public static bool ExistsInPrefs(string key)
        { return PlayerPrefs.HasKey(key); }

        /// <summary> 如果文件存在，则删除保存的文件。</summary>
        /// <param name="path">文件路径，如“Albert”。</param>
        /// <returns></returns>
        public static void DeleteData(string path)
        {
            string dataPath = Path.Combine(Application.persistentDataPath, path + ".save");

            if (File.Exists(dataPath)) File.Delete(dataPath);
            else Debug.LogError("指定的保存文件'" + path + "'不存在。");
        }
        /// <summary> 如果键存在，则删除保存的键</summary>
        /// <param name="key">保存的键</param>
        /// <returns></returns>
        public static void DeletePref(string key)
        {
            if (PlayerPrefs.HasKey(key)) PlayerPrefs.DeleteKey(key);
            else Debug.LogError("指定的保存键'" + key + "'不存在。");
        }

        /// <summary> 将数据保存到加密内存中 </summary>
        /// <param name="data">要保存的数据，如“Albert”，或0、1.5、false等。</param>
        /// <param name="path">要保存数据的路径，如“Player Data/Albert”。</param>
        public static void Save<T>(T data, string path)
        {
            try
            {
                if (typeof(T) == typeof(Transform))
                {
                    var ft = data as Transform;
                    if (ft == null)
                    {
                        Debug.LogError("保存" + path + "失败: Transform数据不能为null");
                        return;
                    }
                    Save_TransformData tData = new Save_TransformData(ft);
                    SaveEncryption.Save(tData, path);
                }
                else if (typeof(T) == typeof(RectTransform))
                {
                    var ft = data as RectTransform;
                    if (ft == null)
                    {
                        Debug.LogError("保存" + path + "失败: RectTransform数据不能为null");
                        return;
                    }
                    Save_RectTransformData tData = new Save_RectTransformData(ft);
                    SaveEncryption.Save(tData, path);
                }
                else
                {
                    // 默认的序列化保存方法
                    SaveEncryption.Save(data, path);
                }
            }
            catch (Exception e)
            {
                Debug.LogError("保存" + path + "失败:" + e.Message);
            }
        }

        /// <summary> 将数据保存到加密内存中。</summary>
        /// <param name="data">要保存的数据，如“Albert”，或0、1.5、false等。</param>
        /// <param name="key">要保存数据的键，如“Albert”。</param>
        public static void SaveToPrefs<T>(T data, string key)
        {
            try
            {
                if (typeof(T) == typeof(Transform))
                {
                    var ft = data as Transform;
                    if (ft == null)
                    {
                        Debug.LogError("保存" + key + "失败: Transform数据不能为null");
                        return;
                    }
                    Save_TransformData tData = new Save_TransformData(ft);
                    SaveEncryption.SaveToPrefs(tData, key);
                }
                else if (typeof(T) == typeof(RectTransform))
                {
                    var ft = data as RectTransform;
                    if (ft == null)
                    {
                        Debug.LogError("保存" + key + "失败: RectTransform数据不能为null");
                        return;
                    }
                    Save_RectTransformData tData = new Save_RectTransformData(ft);
                    SaveEncryption.SaveToPrefs(tData, key);
                }
                else
                {
                    // 默认的序列化保存方法
                    SaveEncryption.SaveToPrefs(data, key);
                }
            }
            catch (Exception e)
            {
                Debug.LogError("保存" + key + "失败:" + e.Message);
            }
        }

        /// <summary> 从加密内存中返回数据对象(如果存在)。 </summary>
        /// <param name="path">数据路径,比如'Player Data/Albert'.</param>
        /// <returns></returns>
        public static T Load<T>(string path)
        {
            try
            {
                if (typeof(T) == typeof(Transform))
                {
                    var tData = SaveEncryption.Load<Save_TransformData>(path);
                    if (tData == null) return default;
                    
                    GameObject go = new GameObject("[LoadedTransform]");
                    go.hideFlags = HideFlags.HideAndDontSave;
                    Transform transform = go.transform;
                    transform.localScale = tData.localScale;
                    transform.position = tData.position;
                    transform.rotation = tData.rotation;
                    return (T)(object)transform;
                }
                else if (typeof(T) == typeof(RectTransform))
                {
                    var tData = SaveEncryption.Load<Save_RectTransformData>(path);
                    if (tData == null) return default;
                    
                    GameObject go = new GameObject("[LoadedRectTransform]");
                    go.hideFlags = HideFlags.HideAndDontSave;
                    RectTransform rectTransform = go.AddComponent<RectTransform>();
                    rectTransform.anchoredPosition = tData.anchoredPosition;
                    rectTransform.eulerAngles = tData.eulerAngles;
                    rectTransform.sizeDelta = tData.sizeDelta;
                    return (T)(object)rectTransform;
                }
                else
                {
                    // 默认的序列化保存方法
                    return SaveEncryption.Load<T>(path);
                }
            }
            catch (Exception e)
            {
                Debug.LogError("加载" + path + "失败:" + e.Message);
                return default(T);
            }
        }

        /// <summary> 从Web的加密内存中返回数据对象(如果存在)。 </summary>
        /// <param name="key">加载数据的密钥,比如'Albert'.</param>
        /// <returns></returns>
        public static T LoadFromPrefs<T>(string key)
        {
            try
            {
                if (typeof(T) == typeof(Transform))
                {
                    var tData = SaveEncryption.LoadFromPrefs<Save_TransformData>(key);
                    if (tData == null) return default;
                    
                    GameObject go = new GameObject("[LoadedTransform]");
                    go.hideFlags = HideFlags.HideAndDontSave;
                    Transform transform = go.transform;
                    transform.localScale = tData.localScale;
                    transform.position = tData.position;
                    transform.rotation = tData.rotation;
                    return (T)(object)transform;
                }
                else if (typeof(T) == typeof(RectTransform))
                {
                    var tData = SaveEncryption.LoadFromPrefs<Save_RectTransformData>(key);
                    if (tData == null) return default;
                    
                    GameObject go = new GameObject("[LoadedRectTransform]");
                    go.hideFlags = HideFlags.HideAndDontSave;
                    RectTransform rectTransform = go.AddComponent<RectTransform>();
                    rectTransform.anchoredPosition = tData.anchoredPosition;
                    rectTransform.eulerAngles = tData.eulerAngles;
                    rectTransform.sizeDelta = tData.sizeDelta;
                    return (T)(object)rectTransform;
                }
                else
                {
                    // 默认的序列化保存方法
                    return SaveEncryption.LoadFromPrefs<T>(key);
                }
            }
            catch (Exception e)
            {
                Debug.LogError("加载" + key + "失败:" + e.Message);
                return default(T);
            }
        }

        #region 异步版本（UniTask）

        /// <summary>
        /// 异步保存。文件 IO + 加解密在后台线程执行，大数据不会卡主线程。
        /// 注意：在调度到线程池前，会先在主线程把 "path" 解析为 <see cref="Application.persistentDataPath"/> 下的绝对路径，
        /// 避免后台线程调用 Application.persistentDataPath 触发 "can only be called from the main thread" 异常。
        /// </summary>
        public static UniTask SaveAsync<T>(T data, string path, CancellationToken cancellationToken = default)
        {
            // 在调用线程（一般为 Unity 主线程）解析出绝对路径，确保 Application.persistentDataPath 被合法访问。
            string dataPath = Path.Combine(Application.persistentDataPath, path + ".save");
            return UniTask.RunOnThreadPool(() => SaveEncryption.SaveToAbsolutePath(data, dataPath), cancellationToken: cancellationToken);
        }

        /// <summary>
        /// 异步加载。文件 IO + 加解密在后台线程执行。
        /// 注意：Transform / RectTransform 的重建必须在主线程（涉及 GameObject 创建），由上层调用 await 后处理。
        /// 同样会在调度到线程池前先在主线程解析持久化路径。
        /// </summary>
        public static UniTask<T> LoadAsync<T>(string path, CancellationToken cancellationToken = default)
        {
            string dataPath = Path.Combine(Application.persistentDataPath, path + ".save");
            return UniTask.RunOnThreadPool(() => SaveEncryption.LoadFromAbsolutePath<T>(dataPath, path), cancellationToken: cancellationToken);
        }

        #endregion
    }
}