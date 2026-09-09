// ----------------------------------------------------------------------------
// CameraAndMiniMapExample.cs
//
// 演示相机/小地图模块的最小使用方式。
// ----------------------------------------------------------------------------
using UnityEngine;
using WManager;

namespace WManager.Example
{
    public class CameraAndMiniMapExample : MonoBehaviour
    {
        [Header("SurroundingCamera")]
        [SerializeField] private SurroundingCamera surround;
        [SerializeField] private Transform target;

        [Header("MiniMap")]
        [SerializeField] private MiniMap miniMap;
        [SerializeField] private Transform leftBottom;
        [SerializeField] private Transform rightTop;
        [SerializeField] private RectTransform mapRT;
        [SerializeField] private Transform player3D;
        [SerializeField] private RectTransform player2D;

        private void Awake()
        {
            //判断主相机是否有绕线相机组件
            if (!Camera.main.GetComponent<SurroundingCamera>())
            {
                //如果没有绕线相机组件，添加一个
                surround = Camera.main.gameObject.AddComponent<SurroundingCamera>();
            }
            else
            {
                //如果有绕线相机组件，获取它
                surround = Camera.main.GetComponent<SurroundingCamera>();
            }
        }
        private void Start()
        {
            // 1) 绕物相机：在 Inspector 上把 SurroundingCamera 挂到主相机并配置好按键
            //    运行时也可以通过代码切换 target
            if (surround != null)
                surround.ChangeTarget(target);

            surround.enableKeyboardMove = true;

            // 2) MiniMap：在场景里建一个 RawImage / Image 作为小地图背景，把左右下角和右上角基点拖好
            //    也可以用链式 API 在运行时配置
            if (miniMap != null)
            {
                miniMap
                    .SetBase(leftBottom, rightTop)
                    .SetMapRT(mapRT)
                    .SetTarget3D(player3D)
                    .SetTarget2D(player2D);
            }
        }

        // ============================================================================
        // 当玩家进入新区域时更新小地图基点
        // ============================================================================
        public void OnEnterNewArea(Transform newLeftBottom, Transform newRightTop)
        {
            if (miniMap != null) miniMap.SetBase(newLeftBottom, newRightTop);
        }

        // ============================================================================
        // 当玩家切换操作对象时更新相机焦点
        // ============================================================================
        public void OnSwitchTarget(Transform newTarget)
        {
            if (surround != null) surround.ChangeTarget(newTarget);
        }
    }
}
