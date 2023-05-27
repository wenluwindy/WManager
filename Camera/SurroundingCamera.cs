namespace WManager
{
    using UnityEngine;
    /// <summary>
    /// 绕物相机代码，放到主相机上使用
    /// </summary>
    public class SurroundingCamera : MonoBehaviour
    {

        public Transform target;
        public Mouse RotationMode;//在面板中选择需要按住的鼠标按键
        public float xSpeed = 200, ySpeed = 200, mSpeed = 10;
        public float yMinLimit = 5, yMaxLimit = 50;
        public float distance = 50, minDistance = 2, maxDistance = 100;
        public bool needDamping = true;
        float damping = 5f;
        public float x = 0f;
        public float y = 0f;

        private Vector3 m_mouseMovePos;
        private Camera cam;

        /// <summary>
        /// 鼠标选择
        /// </summary>
        public enum Mouse
        {
            Left = 0,
            Right = 1,
            Middle = 2
        }
        private void Start()
        {
            cam = GetComponent<Camera>();
        }

        void LateUpdate()
        {
            if (target)
            {
                //按住指定的鼠标按键，围绕target旋转移动相机，改变视野
                if (Input.GetMouseButton((int)RotationMode))
                {
                    x += Input.GetAxis("Mouse X") * xSpeed * 0.02f;
                    y -= Input.GetAxis("Mouse Y") * ySpeed * 0.02f;


                    y = ClampAngle(y, yMinLimit, yMaxLimit);
                }
                distance -= Input.GetAxis("Mouse ScrollWheel") * mSpeed;
                distance = Mathf.Clamp(distance, minDistance, maxDistance);

                Quaternion rotation = Quaternion.Euler(y, x, 0.0f);  //
                Vector3 disVector = new Vector3(0f, 0f, -distance);
                Vector3 position = rotation * disVector + target.position;
                if (needDamping)
                {
                    transform.rotation = Quaternion.Lerp(transform.rotation, rotation, Time.deltaTime * damping);
                    transform.position = Vector3.Lerp(transform.position, position, Time.deltaTime * damping);
                }
                else
                {
                    transform.rotation = rotation;
                    transform.position = position;
                }
            }
        }
        static float ClampAngle(float angle, float min, float max)
        {
            if (angle < -360)
                angle += 360;
            if (angle > 360)
                angle -= 360;
            return Mathf.Clamp(angle, min, max);
        }

        /// <summary>
        /// 更改绕物相机对象，在事件或者按钮事件调用，拖放需旋转对象的视点
        /// </summary>
        /// <param name="T"></param>
        public void ChangeTarget(Transform T)
        {
            target = T;
        }
    }
}
