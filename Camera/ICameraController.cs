using UnityEngine;

namespace WManager
{
    /// <summary>
    /// 相机控制器抽象。统一三个相机控制器（Roam / Surrounding / FirstPerson）的行为约定。
    /// </summary>
    public interface ICameraController
    {
        /// <summary>
        /// 当前是否聚焦在某个目标
        /// </summary>
        bool IsFocused { get; }

        /// <summary>
        /// 让相机聚焦到某个 Transform/位置
        /// </summary>
        void Focus(Vector3 worldPosition, Quaternion worldRotation, float duration = 0f);

        /// <summary>
        /// 重置相机到默认状态
        /// </summary>
        void Reset();
    }
}