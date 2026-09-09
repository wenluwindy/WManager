using System;
using UnityEngine;

namespace WManager
{
    /// <summary> 存储类数据 </summary>
    [System.Serializable]
    public class Save_Data<T>
    {
        /// <summary>
        /// 存档格式版本号。当前 1。
        /// 旧存档（Version=0 或缺失）视为 v1 兼容。
        /// 业务方可在自己的数据类型里加 Version 字段做更细粒度版本管理。
        /// </summary>
        public int Version = 1;

        /// <summary>
        /// 保存时间戳（UTC 毫秒）。仅诊断用，不参与业务逻辑。
        /// </summary>
        public long SavedAtUtcMs;

        /// <summary>
        /// 实际保存的数据
        /// </summary>
        public T SaveData;

        public Save_Data() { }

        /// <summary> 构造函数，传入要保存的数据。 </summary>
        public Save_Data(T Data)
        {
            SaveData = Data;
            SavedAtUtcMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }
    }

    /// <summary> 用于存储Transform数据的类。 </summary>
    [System.Serializable]
    public class Save_TransformData
    {
        public Vector3 localScale; // 本地缩放
        public Vector3 position;
        public Quaternion rotation;

        public Save_TransformData() { } // 无参构造函数

        /// <summary> 构造函数，传入要保存的Transform数据。 </summary>
        public Save_TransformData(Transform Data)
        {
            if (Data == null) throw new ArgumentNullException(nameof(Data));
            localScale = Data.localScale; // 获取Transform的本地缩放
            position = Data.position;
            rotation = Data.rotation;
        }
    }

    /// <summary> 用于存储RectTransform数据的类。 </summary>
    [System.Serializable]
    public class Save_RectTransformData
    {
        public Vector2 anchoredPosition; // 锚点位置
        public Vector3 eulerAngles; // 欧拉角
        public Vector2 sizeDelta; // 大小

        public Save_RectTransformData() { } // 无参构造函数

        /// <summary> 构造函数，传入要保存的RectTransform数据。 </summary>
        public Save_RectTransformData(RectTransform Data)
        {
            if (Data == null) throw new ArgumentNullException(nameof(Data));
            anchoredPosition = Data.anchoredPosition; // 获取RectTransform的锚点位置
            eulerAngles = Data.eulerAngles; // 获取RectTransform的欧拉角
            sizeDelta = Data.sizeDelta; // 获取RectTransform的大小
        }
    }
}