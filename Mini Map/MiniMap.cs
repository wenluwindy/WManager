using UnityEngine;
using Sirenix.OdinInspector;

namespace WManager
{
    /// <summary>
    /// 小地图功能
    /// </summary>
    [AddComponentMenu("管理器/小地图")]
    public class MiniMap : MonoBehaviour
    {
        //基点 场景中的左下角和右上角 (以z轴正方向为上、以x轴正方向为右)
        [InfoBox("以z轴正方向为上、以x轴正方向为右")]
        [SerializeField, LabelText("场景中的左下角")]
        private Transform leftBottom;
        [SerializeField, LabelText("场景中的右上角")]
        private Transform rightTop;
        //小地图的RectTransform组件
        [SerializeField, LabelText("小地图的背景图")]
        private RectTransform mapRt;

        [Space(10)]
        //场景中的目标物体
        [SerializeField, LabelText("场景中的目标物体")] private Transform target3d;
        //小地图中的目标物体
        [SerializeField, LabelText("小地图中的目标物体")] private RectTransform target2d;

        [Space(10)]
        //是否启用旋转
        [SerializeField, LabelText("是否启用旋转")] private bool isEnableRot = true;
        //是否启用插值运算来处理旋转
        [SerializeField, LabelText("是否启用插值运算来处理旋转")] private bool isEnableRotLerp = true;
        //插值到目标角度所需的时间
        [Range(0.01f, 1f), SerializeField, LabelText("插值到目标角度所需的时间")] private float rotationLerpTime = .2f;

        private void Update()
        {
            if (target3d == null || target2d == null || mapRt == null || leftBottom == null || rightTop == null) return;

            //水平方向上的比例 = 小地图的长度 / 场景中右上基点与左下基点的x坐标差值
            float horizontalProportion = mapRt.rect.width / (rightTop.position.x - leftBottom.position.x);
            //垂直方向上的比例 = 小地图的宽度 / 场景中右上基点与左下基点的z坐标差值
            float verticalProportion = mapRt.rect.height / (rightTop.position.z - leftBottom.position.z);

            //三维目标距左下基点的x方向上的距离
            float horizontal = target3d.position.x - leftBottom.position.x;
            //三维目标距左下基点的z方向上的距离
            float vertical = target3d.position.z - leftBottom.position.z;

            //二维目标距小地图左下角x方向上的距离
            float x = horizontal * horizontalProportion;
            //二维目标距小地图左下角y方向上的距离
            float y = vertical * verticalProportion;

            //将锚点设在左下角
            target2d.anchorMin = Vector2.zero;
            target2d.anchorMax = Vector2.zero;

            //最终赋值坐标
            target2d.anchoredPosition = new Vector2(x, y);

            //计算角度
            if (isEnableRot)
            {
                Vector3 angles = target2d.eulerAngles;
                angles.z = -target3d.eulerAngles.y;
                if (!isEnableRotLerp)
                {
                    target2d.eulerAngles = angles;
                }
                else
                {
                    float lerpPct = 1f - Mathf.Exp(Mathf.Log(1f - .99f) / rotationLerpTime * Time.deltaTime);
                    target2d.rotation = Quaternion.Lerp(target2d.rotation, Quaternion.Euler(angles), lerpPct);
                }
            }
        }

        /// <summary>
        /// 设置场景中的基点
        /// </summary>
        /// <param name="leftBottom">左下角基点</param>
        /// <param name="rightTop">右上角基点</param>
        /// <returns></returns>
        public MiniMap SetBase(Transform leftBottom, Transform rightTop)
        {
            this.leftBottom = leftBottom;
            this.rightTop = rightTop;
            return this;
        }
        /// <summary>
        /// 设置小地图RectTransform
        /// </summary>
        /// <param name="mapRt"></param>
        /// <returns></returns>
        public MiniMap SetMapRT(RectTransform mapRt)
        {
            this.mapRt = mapRt;
            return this;
        }
        /// <summary>
        /// 设置三维目标物体
        /// </summary>
        /// <param name="target3d"></param>
        /// <returns></returns>
        public MiniMap SetTarget3D(Transform target3d)
        {
            this.target3d = target3d;
            return this;
        }
        /// <summary>
        /// 设置二维目标物体
        /// </summary>
        /// <param name="target2d"></param>
        /// <returns></returns>
        public MiniMap SetTarget2D(RectTransform target2d)
        {
            this.target2d = target2d;
            return this;
        }
    }
}