using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace WManager
{
    /// <summary>
    /// 绕物相机代码，放到主相机上使用
    /// 按住鼠标中键可平移画面
    /// </summary>
    public class SurroundingCamera : MonoBehaviour
    {
        public Transform target;

        public Mouse RotationMode;//在面板中选择需要按住的鼠标按键

        public float xSpeed = 200;
        public float ySpeed = 200;
        public float mSpeed = 10;

        public float yMinLimit = 5;
        public float yMaxLimit = 50;

        public float distance = 50;
        public float minDistance = 2;
        public float maxDistance = 100;

        public bool needDamping = true;

        private float damping = 5f;

        public float x = 0f;
        public float y = 0f;

        public float panSpeed = 0.01f;
        public Vector3 panOffset = Vector3.zero;

        /// <summary>
        /// 是否开启键盘控制目标移动
        /// 默认关闭
        /// </summary>
        [Header("Keyboard Control")]
        public bool enableKeyboardMove = false;

        /// <summary>
        /// 目标移动速度
        /// </summary>
        public float keyboardMoveSpeed = 5f;

        private Vector3 lastMousePosition;


        /// <summary>
        /// 鼠标选择
        /// </summary>
        public enum Mouse
        {
            Left = 0,
            Right = 1
        }


        /// <summary>
        /// 检测鼠标是否在可交互的UI上
        /// （仅检测raycastTarget为true的UI元素）
        /// </summary>
        private bool IsMouseOverUI()
        {
            if (EventSystem.current == null)
                return false;

            PointerEventData eventData =
                new PointerEventData(EventSystem.current);

            eventData.position = Input.mousePosition;

            // 获取所有被射线击中的UI元素
            List<RaycastResult> raycastResults =
                new List<RaycastResult>();

            EventSystem.current.RaycastAll(
                eventData,
                raycastResults
            );

            // 检查是否有raycastTarget为true的UI元素
            foreach (RaycastResult result in raycastResults)
            {
                if (result.gameObject.TryGetComponent<Graphic>(
                    out Graphic graphic)
                    && graphic.raycastTarget)
                {
                    return true;
                }
            }

            return false;
        }


        void LateUpdate()
        {
            // 如果没有指定target，则返回
            if (target == null)
            {
                return;
            }

            // =========================
            // 键盘控制目标移动
            // =========================
            if (enableKeyboardMove)
            {
                HandleKeyboardMove();
            }


            // 如果鼠标在UI上，则返回
            if (IsMouseOverUI())
            {
                return;
            }


            // 按住指定的鼠标按键
            // 围绕target旋转移动相机
            if (Input.GetMouseButton((int)RotationMode))
            {
                x += Input.GetAxis("Mouse X")
                     * xSpeed
                     * 0.02f;

                y -= Input.GetAxis("Mouse Y")
                     * ySpeed
                     * 0.02f;

                y = ClampAngle(
                    y,
                    yMinLimit,
                    yMaxLimit
                );
            }


            // 中键平移
            if (Input.GetMouseButtonDown(2))
            {
                lastMousePosition =
                    Input.mousePosition;
            }
            else if (Input.GetMouseButton(2))
            {
                Vector3 delta =
                    Input.mousePosition
                    - lastMousePosition;

                Vector3 move =
                    -transform.right
                    * delta.x
                    * panSpeed

                    + -transform.up
                    * delta.y
                    * panSpeed;

                panOffset += move;

                lastMousePosition =
                    Input.mousePosition;
            }


            // 滚轮缩放
            distance -=
                Input.GetAxis("Mouse ScrollWheel")
                * mSpeed;

            distance = Mathf.Clamp(
                distance,
                minDistance,
                maxDistance
            );


            // =========================
            // 更新相机位置和旋转
            // =========================

            Quaternion rotation =
                Quaternion.Euler(
                    y,
                    x,
                    0.0f
                );

            Vector3 disVector =
                new Vector3(
                    0f,
                    0f,
                    -distance
                );

            Vector3 position =
                rotation
                * disVector
                + target.position
                + panOffset;


            if (needDamping)
            {
                transform.rotation =
                    Quaternion.Lerp(
                        transform.rotation,
                        rotation,
                        Time.deltaTime
                        * damping
                    );

                transform.position =
                    Vector3.Lerp(
                        transform.position,
                        position,
                        Time.deltaTime
                        * damping
                    );
            }
            else
            {
                transform.rotation =
                    rotation;

                transform.position =
                    position;
            }
        }


        /// <summary>
        /// 键盘控制目标在XZ平面移动
        /// 移动方向以相机当前视角为基准
        /// </summary>
        private void HandleKeyboardMove()
        {
            // 获取输入
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");

            // 没有输入直接返回
            if (horizontal == 0 && vertical == 0)
            {
                return;
            }


            // =========================
            // 获取相机在XZ平面的前方方向
            // =========================

            Vector3 forward = transform.forward;

            // 移除Y轴，保证只在XZ平面移动
            forward.y = 0f;

            forward.Normalize();


            // =========================
            // 获取相机在XZ平面的右方向
            // =========================

            Vector3 right = transform.right;

            // 移除Y轴
            right.y = 0f;

            right.Normalize();


            // =========================
            // 计算移动方向
            // =========================

            Vector3 moveDirection =
                forward * vertical
                + right * horizontal;


            // 防止斜向移动速度更快
            if (moveDirection.sqrMagnitude > 1f)
            {
                moveDirection.Normalize();
            }


            // 移动target
            target.position +=
                moveDirection
                * keyboardMoveSpeed
                * Time.deltaTime;
        }


        static float ClampAngle(
            float angle,
            float min,
            float max)
        {
            if (angle < -360)
                angle += 360;

            if (angle > 360)
                angle -= 360;

            return Mathf.Clamp(
                angle,
                min,
                max
            );
        }


        /// <summary>
        /// 更改绕物相机对象
        /// 在事件或者按钮事件调用
        /// </summary>
        public void ChangeTarget(Transform T)
        {
            target = T;

            panOffset =
                Vector3.zero;
        }
    }
}