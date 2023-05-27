using UnityEngine;
using DG.Tweening;
using Sirenix.OdinInspector;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace WManager
{
    /// <summary>
    /// 漫游视角 相机控制脚本
    /// </summary>
    [AddComponentMenu("管理器/漫游相机")]
    [RequireComponent(typeof(Camera))]
    public class RoamCameraController : MonoBehaviour
    {
        /// <summary>
        /// 相机状态类
        /// </summary>
        private class CameraState
        {
            /// <summary>
            /// 坐标x值
            /// </summary>
            public float posX;
            /// <summary>
            /// 坐标y值
            /// </summary>
            public float posY;
            /// <summary>
            /// 坐标z值
            /// </summary>
            public float posZ;
            /// <summary>
            /// 旋转x值
            /// </summary>
            public float rotX;
            /// <summary>
            /// 旋转y值
            /// </summary>
            public float rotY;
            /// <summary>
            /// 旋转z值 
            /// </summary>
            public float rotZ;

            //活动区域限制
            private readonly float xMinValue;
            private readonly float xMaxValue;
            private readonly float yMinValue;
            private readonly float yMaxValue;
            private readonly float zMinValue;
            private readonly float zMaxValue;

            /// <summary>
            /// 默认构造函数
            /// </summary>
            public CameraState()
            {
                xMinValue = float.MinValue;
                xMaxValue = float.MaxValue;
                yMinValue = float.MinValue;
                yMaxValue = float.MaxValue;
                zMinValue = float.MinValue;
                zMaxValue = float.MaxValue;
            }
            /// <summary>
            /// 构造函数
            /// </summary>
            /// <param name="xMinValue"></param>
            /// <param name="xMaxValue"></param>
            /// <param name="yMinValue"></param>
            /// <param name="yMaxValue"></param>
            /// <param name="zMinValue"></param>
            /// <param name="zMaxValue"></param>
            public CameraState(float xMinValue, float xMaxValue, float yMinValue, float yMaxValue, float zMinValue, float zMaxValue)
            {
                this.xMinValue = xMinValue;
                this.xMaxValue = xMaxValue;
                this.yMinValue = yMinValue;
                this.yMaxValue = yMaxValue;
                this.zMinValue = zMinValue;
                this.zMaxValue = zMaxValue;
            }

            /// <summary>
            /// 根据Transform组件更新状态
            /// </summary>
            /// <param name="t">Transform组件</param>
            public void SetFromTransform(Transform t)
            {
                posX = t.position.x;
                posY = t.position.y;
                posZ = t.position.z;
                rotX = t.eulerAngles.x;
                rotY = t.eulerAngles.y;
                rotZ = t.eulerAngles.z;
            }
            /// <summary>
            /// 移动
            /// </summary>
            public void Translate(Vector3 translation)
            {
                Vector3 rotatedTranslation = Quaternion.Euler(rotX, rotY, rotZ) * translation;
                posX += rotatedTranslation.x;
                posY += rotatedTranslation.y;
                posZ += rotatedTranslation.z;

                posX = Mathf.Clamp(posX, xMinValue, xMaxValue);
                posY = Mathf.Clamp(posY, yMinValue, yMaxValue);
                posZ = Mathf.Clamp(posZ, zMinValue, zMaxValue);
            }
            /// <summary>
            /// 根据目标状态插值运算
            /// </summary>
            /// <param name="target">目标状态</param>
            /// <param name="positionLerpPct">位置插值率</param>
            /// <param name="rotationLerpPct">旋转插值率</param>
            public void LerpTowards(CameraState target, float positionLerpPct, float rotationLerpPct)
            {
                posX = Mathf.Lerp(posX, target.posX, positionLerpPct);
                posY = Mathf.Lerp(posY, target.posY, positionLerpPct);
                posZ = Mathf.Lerp(posZ, target.posZ, positionLerpPct);
                rotX = Mathf.Lerp(rotX, target.rotX, rotationLerpPct);
                rotY = Mathf.Lerp(rotY, target.rotY, rotationLerpPct);
                rotZ = Mathf.Lerp(rotZ, target.rotZ, rotationLerpPct);
            }
            /// <summary>
            /// 根据状态刷新Transform组件
            /// </summary>
            /// <param name="t">Transform组件</param>
            public void UpdateTransform(Transform t)
            {
                t.position = new Vector3(posX, posY, posZ);
                t.rotation = Quaternion.Euler(rotX, rotY, rotZ);
            }
        }

        #region Private Variables
        //控制开关
        [LabelText("漫游相机开关")]
        public bool toggle = true;

        //是否限制活动范围
        [SerializeField, LabelText("是否限制活动范围")]
        private bool isRangeClamped;
        //限制范围 当isRangeClamped为true时起作用
        [SerializeField, LabelText("x最小值"), ShowIf("isRangeClamped")]
        private float xMinValue = -100f;   //x最小值
        [SerializeField, LabelText("x最大值"), ShowIf("isRangeClamped")]
        private float xMaxValue = 100f;    //x最大值
        [SerializeField, LabelText("y最小值"), ShowIf("isRangeClamped")]
        private float yMinValue = 0f;      //y最小值
        [SerializeField, LabelText("y最大值"), ShowIf("isRangeClamped")]
        private float yMaxValue = 100f;    //y最大值
        [SerializeField, LabelText("z最小值"), ShowIf("isRangeClamped")]
        private float zMinValue = -100f;   //z最小值
        [SerializeField, LabelText("z最大值"), ShowIf("isRangeClamped")]
        private float zMaxValue = 100f;    //z最大值

        //移动速度
        [SerializeField, LabelText("移动速度")]
        private float translateSpeed = 10f;
        //加速系数 Shift按下时起作用
        [SerializeField, LabelText("加速系数 Shift按下时起作用")]
        private float boost = 3.5f;
        //插值到目标位置所需的时间
        [Range(0.01f, 1f), SerializeField, LabelText("插值到目标位置所需的时间")]
        private float positionLerpTime = 1f;
        //插值到目标旋转所需的时间
        [Range(0.01f, 1f), SerializeField, LabelText("插值到目标旋转所需的时间")]
        private float rotationLerpTime = 1f;

        //鼠标运动的灵敏度
        [Range(0.1f, 10f), SerializeField, LabelText("鼠标运动的灵敏度")]
        private float mouseMovementSensitivity = 5f;
        //鼠标滚轮运动的速度
        [SerializeField, LabelText("鼠标滚轮运动的速度")]
        private float mouseScrollMoveSpeed = 10f;
        //用于鼠标滚轮移动 是否反转方向
        [SerializeField, LabelText("鼠标滚轮移动 是否反转方向")]
        private bool invertScrollDirection = false;
        //视角旋转是否反转y反向
        [SerializeField, LabelText("视角旋转是否反转y反向")]
        private bool invertY = false;
        //用于限制垂直方向上旋转的最大值
        [SerializeField, LabelText("垂直方向上旋转的最大值")]
        private float verticalLimitMax = 60f;
        //用于限制垂直方向上旋转的最小值
        [SerializeField, LabelText("垂直方向上旋转的最小值")]
        private float verticalLimitMin = -60f;

        //移动量
        private Vector3 translation = Vector3.zero;
        //Tween动画
        private Tween tween;

        private CameraState initialCameraState;
        private CameraState targetCameraState;
        private CameraState interpolatingCameraState;
        #endregion

        #region Private Methods
        private void Awake()
        {
            //初始化
            if (isRangeClamped)
            {
                initialCameraState = new CameraState(xMinValue, xMaxValue, yMinValue, yMaxValue, zMinValue, zMaxValue);
                targetCameraState = new CameraState(xMinValue, xMaxValue, yMinValue, yMaxValue, zMinValue, zMaxValue);
                interpolatingCameraState = new CameraState(xMinValue, xMaxValue, yMinValue, yMaxValue, zMinValue, zMaxValue);
            }
            else
            {
                initialCameraState = new CameraState();
                targetCameraState = new CameraState();
                interpolatingCameraState = new CameraState();
            }
        }
        private void OnEnable()
        {
            initialCameraState.SetFromTransform(transform);
            targetCameraState.SetFromTransform(transform);
            interpolatingCameraState.SetFromTransform(transform);
        }

        private void LateUpdate()
        {
            if (!toggle) return;

            OnRotateUpdate();
            OnTranslateUpdate();
            OnResetUpdate();
        }
        private void OnRotateUpdate()
        {
#if ENABLE_INPUT_SYSTEM
            bool pressed = Mouse.current.rightButton.isPressed;
#else
            bool pressed = Input.GetMouseButton(1);
#endif
            if (pressed)
            {
#if ENABLE_INPUT_SYSTEM
                Vector2 input = Mouse.current.delta.ReadValue();
#else
                Vector2 input = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
#endif
                input.y *= invertY ? 1 : -1;
                //float mouseSensitivityFactor = mouseRotationSensitivityCurve.Evaluate(input.magnitude);
                targetCameraState.rotY += input.x * mouseMovementSensitivity;
                targetCameraState.rotX += input.y * mouseMovementSensitivity;
                targetCameraState.rotX = Mathf.Clamp(targetCameraState.rotX, verticalLimitMin, verticalLimitMax);
            }
        }
        private void OnResetUpdate()
        {
#if ENABLE_INPUT_SYSTEM
            bool uPressed = Keyboard.current.uKey.wasPressedThisFrame;
#else
            bool uPressed = Input.GetKeyDown(KeyCode.U);
#endif
            //U键按下重置到初始状态
            if (uPressed)
            {
                ResetCamera();
            }
        }
        private void OnTranslateUpdate()
        {
            translation = GetInputTranslation() * Time.deltaTime * translateSpeed;
            targetCameraState.Translate(translation);
            float positionLerpPct = 1f - Mathf.Exp(Mathf.Log(1f - .99f) / positionLerpTime * Time.deltaTime);
            float rotationLerpPct = 1f - Mathf.Exp(Mathf.Log(1f - .99f) / rotationLerpTime * Time.deltaTime);
            interpolatingCameraState.LerpTowards(targetCameraState, positionLerpPct, rotationLerpPct);
            interpolatingCameraState.UpdateTransform(transform);
        }
        //获取输入
        private Vector3 GetInputTranslation()
        {
            Vector3 ts = new Vector3();
            //键盘W、A、S、D、Q、E控制运动方向
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current.wKey.isPressed) ts += Vector3.forward;
            if (Keyboard.current.sKey.isPressed) ts += Vector3.back;
            if (Keyboard.current.aKey.isPressed) ts += Vector3.left;
            if (Keyboard.current.dKey.isPressed) ts += Vector3.right;
            if (Keyboard.current.qKey.isPressed) ts += Vector3.down;
            if (Keyboard.current.eKey.isPressed) ts += Vector3.up;
#else
            if (Input.GetKey(KeyCode.W)) ts += Vector3.forward;
            if (Input.GetKey(KeyCode.S)) ts += Vector3.back;
            if (Input.GetKey(KeyCode.A)) ts += Vector3.left;
            if (Input.GetKey(KeyCode.D)) ts += Vector3.right;
            if (Input.GetKey(KeyCode.Q)) ts += Vector3.down;
            if (Input.GetKey(KeyCode.E)) ts += Vector3.up;
#endif

#if ENABLE_INPUT_SYSTEM
            //读取鼠标滚轮滚动值
            float wheelValue = Mouse.current.scroll.ReadValue().y;
#else
            float wheelValue = Input.GetAxis("Mouse ScrollWheel");
#endif
            ts += (wheelValue == 0 ? Vector3.zero : (wheelValue > 0 ? Vector3.forward : Vector3.back) * (invertScrollDirection ? -1 : 1)) * mouseScrollMoveSpeed;
#if ENABLE_INPUT_SYSTEM
            //左Shift键按下时加速
            if (Keyboard.current.leftShiftKey.isPressed) ts *= boost;
#else
            if (Input.GetKey(KeyCode.LeftShift)) ts *= boost;
#endif
            return ts;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            //如果限制活动范围 将区域范围绘制出来
            if (isRangeClamped)
            {
                Handles.color = Color.cyan;
                Vector3[] points = new Vector3[8]
                {
                    new Vector3(xMinValue, yMinValue, zMinValue),
                    new Vector3(xMaxValue, yMinValue, zMinValue),
                    new Vector3(xMaxValue, yMinValue, zMaxValue),
                    new Vector3(xMinValue, yMinValue, zMaxValue),
                    new Vector3(xMinValue, yMaxValue, zMinValue),
                    new Vector3(xMaxValue, yMaxValue, zMinValue),
                    new Vector3(xMaxValue, yMaxValue, zMaxValue),
                    new Vector3(xMinValue, yMaxValue, zMaxValue)
                };
                for (int i = 0; i < 4; i++)
                {
                    int start = i % 4;
                    int end = (i + 1) % 4;
                    Handles.DrawLine(points[start], points[end]);
                    Handles.DrawLine(points[start + 4], points[end + 4]);
                    Handles.DrawLine(points[start], points[i + 4]);
                }
            }
        }
#endif

        #endregion

        #region Public Methods
        /// <summary>
        /// 重置摄像机到初始状态
        /// </summary>
        public void ResetCamera()
        {
            initialCameraState.UpdateTransform(transform);
            targetCameraState.SetFromTransform(transform);
            interpolatingCameraState.SetFromTransform(transform);
        }
        /// <summary>
        /// 聚焦
        /// </summary>
        /// <param name="position">目标位置</param>
        /// <param name="rotation">目标旋转</param>
        /// <param name="duration">时长</param>
        public void Focus(Vector3 position, Vector3 rotation, float duration)
        {
            tween?.Kill();
            toggle = false;
            transform.DORotate(rotation, duration);
            tween = transform.DOMove(position, duration).Play()
                .OnKill(() =>
                {
                    toggle = true;
                    interpolatingCameraState.SetFromTransform(transform);
                    targetCameraState.SetFromTransform(transform);
                    tween = null;
                });
        }
        #endregion
    }
}