using System.IO;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace WManager.Save
{
    /// <summary> 存档系统 </summary>
    public class SaveManager : MonoBehaviour
    {
        private static SaveManager instance;
        public static SaveManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = (SaveManager)FindObjectOfType(typeof(SaveManager));
                    if (instance == null)
                    {
                        // 创建gameObject并添加组件
                        instance = (new GameObject("SaveManager")).AddComponent<SaveManager>();
                    }
                }
                return instance;
            }
        }
        private static bool initialized = false;

        static SaveManager()
        {
            Instance.Init();
        }
        private void Init()
        {
            if (!initialized)
            {
                initialized = true;
                DontDestroyOnLoad(this);
            }
        }

        /// <summary> 检查特定文件是否已经被保存</summary>
        /// <param name="path">文件路径，如“Albert”。</param>
        public static bool Exists(string path)
        {
            string dataPath = Application.persistentDataPath + path + ".save";
            return File.Exists(dataPath);
        }
        /// <summary> 检查特定键是否已经被保存 </summary>
        /// <param name="key">保存的键，如“Albert”。</param>
        public static bool ExistsWeb(string key)
        { return PlayerPrefs.HasKey(key); }

        /// <summary> 如果文件存在，则删除保存的文件。</summary>
        /// <param name="path">文件路径，如“Albert”。</param>
        /// <returns></returns>
        public static void DeleteData(string path)
        {
            string dataPath = Application.persistentDataPath + path + ".save";

            if (File.Exists(dataPath)) File.Delete(dataPath);
            else Debug.LogError("指定的保存文件'" + path + "'不存在。");
        }
        /// <summary> 如果键存在，则删除保存的键</summary>
        /// <param name="key">保存的键</param>
        /// <returns></returns>
        public static void DeleteDataWeb(string key)
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
                    Save_TransformData tData = new Save_TransformData(ft);
                    Save_Encryption.Save(tData, path);
                }
                else if (typeof(T) == typeof(RectTransform))
                {
                    var ft = data as RectTransform;
                    Save_RectTransformData tData = new Save_RectTransformData(ft);
                    Save_Encryption.Save(tData, path);
                }
                else
                {
                    // 默认的序列化保存方法
                    Save_Encryption.Save(data, path);
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
        public static void SaveWeb<T>(T data, string key)
        {
            try
            {
                if (typeof(T) == typeof(Transform))
                {
                    var ft = data as Transform;
                    Save_TransformData tData = new Save_TransformData(ft);
                    Save_Encryption.SaveWeb(tData, key);
                }
                else if (typeof(T) == typeof(RectTransform))
                {
                    var ft = data as RectTransform;
                    Save_RectTransformData tData = new Save_RectTransformData(ft);
                    Save_Encryption.SaveWeb(tData, key);
                }
                else
                {
                    // 默认的序列化保存方法
                    Save_Encryption.SaveWeb(data, key);
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
                    var tData = Save_Encryption.Load<Save_TransformData>(path);
                    Transform transform;
                    GameObject go = new GameObject();
                    transform = go.transform;
                    transform.localPosition = tData.localPosition;
                    transform.localRotation = tData.localRotation;
                    transform.localScale = tData.localScale;
                    transform.position = tData.position;
                    transform.rotation = tData.rotation;
                    transform.eulerAngles = tData.eulerAngles;
                    transform.localEulerAngles = tData.localEulerAngles;
                    Destroy(go);
                    return (T)(object)transform;
                }
                else if (typeof(T) == typeof(RectTransform))
                {
                    var tData = Save_Encryption.Load<Save_RectTransformData>(path);
                    GameObject go = new GameObject();
                    RectTransform rectTransform = go.AddComponent<RectTransform>();
                    rectTransform.anchoredPosition = tData.anchoredPosition;
                    rectTransform.eulerAngles = tData.eulerAngles;
                    rectTransform.sizeDelta = tData.sizeDelta;
                    Destroy(go);
                    return (T)(object)rectTransform;
                }
                else
                {
                    // 默认的序列化保存方法
                    return Save_Encryption.Load<T>(path);
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
        public static T LoadWeb<T>(string key)
        {
            try
            {
                if (typeof(T) == typeof(Transform))
                {
                    var tData = Save_Encryption.LoadWeb<Save_TransformData>(key);
                    Transform transform;
                    GameObject go = new GameObject();
                    transform = go.transform;
                    transform.localPosition = tData.localPosition;
                    transform.localRotation = tData.localRotation;
                    transform.localScale = tData.localScale;
                    transform.position = tData.position;
                    transform.rotation = tData.rotation;
                    transform.eulerAngles = tData.eulerAngles;
                    transform.localEulerAngles = tData.localEulerAngles;
                    Destroy(go);
                    return (T)(object)transform;
                }
                else if (typeof(T) == typeof(RectTransform))
                {
                    var tData = Save_Encryption.LoadWeb<Save_RectTransformData>(key);
                    GameObject go = new GameObject();
                    RectTransform rectTransform = go.AddComponent<RectTransform>();
                    rectTransform.anchoredPosition = tData.anchoredPosition;
                    rectTransform.eulerAngles = tData.eulerAngles;
                    rectTransform.sizeDelta = tData.sizeDelta;
                    Destroy(go);
                    return (T)(object)rectTransform;
                }
                else
                {
                    // 默认的序列化保存方法
                    return Save_Encryption.LoadWeb<T>(key);
                }
            }
            catch (Exception e)
            {
                Debug.LogError("加载" + key + "失败:" + e.Message);
                return default(T);
            }
        }
    }
}