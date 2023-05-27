using UnityEngine;

namespace WManager.Save
{
    /// <summary> 存储类数据</summary>
    [System.Serializable]
    public class Save_Data<T>
    {
        public T SaveData;// 保存数据的成员变量
        public Save_Data() { } // 无参构造函数

        /// <summary> 构造函数，传入要保存的数据。 </summary>
        public Save_Data(T Data)
        {
            SaveData = Data;
        }
    }

    /// <summary> 用于存储Transform数据的类。 </summary>
    [System.Serializable]
    public class Save_TransformData
    {
        public Vector3 localPosition; // 本地坐标
        public Quaternion localRotation; // 本地旋转
        public Vector3 localScale; // 本地缩放
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 eulerAngles;
        public Vector3 localEulerAngles;

        public Save_TransformData() { } // 无参构造函数

        /// <summary> 构造函数，传入要保存的Transform数据。 </summary>
        public Save_TransformData(Transform Data)
        {
            localPosition = Data.localPosition; // 获取Transform的本地坐标
            localRotation = Data.localRotation; // 获取Transform的本地旋转
            localScale = Data.localScale; // 获取Transform的本地缩放
            position = Data.position;
            rotation = Data.rotation;
            eulerAngles = Data.eulerAngles;
            localEulerAngles = Data.localEulerAngles;
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
            anchoredPosition = Data.anchoredPosition; // 获取RectTransform的锚点位置
            eulerAngles = Data.eulerAngles; // 获取RectTransform的欧拉角
            sizeDelta = Data.sizeDelta; // 获取RectTransform的大小
        }
    }
}