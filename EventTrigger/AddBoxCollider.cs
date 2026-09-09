using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace WManager
{
    /// <summary>
    /// 自动添加BoxCollider到物体
    /// </summary>
    public class AddBoxCollider
    {
#if UNITY_EDITOR
        [MenuItem("GameObject/自动添加BoxCollider")]
        static void EditorAddBoxCollider()
        {
            //获取当前选中的物体
            GameObject[] selectsGameObjects = Selection.gameObjects;
            AutoBoxCollider(selectsGameObjects);
        }
#endif

        /// <summary>
        /// 自动添加BoxCollider到物体
        /// </summary>
        /// <param name="gameObjects">需要添加BoxCollider的物体数组</param>
        public static void AutoBoxCollider(GameObject[] gameObjects)
        {
            //如果未选中任何物体 返回
            GameObject[] selections = gameObjects;
            if (selections == null) return;
            // 遍历所有选择的对象
            foreach (GameObject gameObject in selections)
            {
                //计算中心点
                Vector3 center = Vector3.zero;
                var renders = gameObject.GetComponentsInChildren<Renderer>();
                for (int i = 0; i < renders.Length; i++)
                {
                    center += renders[i].bounds.center;
                }
                center /= renders.Length;
                //创建边界盒
                Bounds bounds = new Bounds(center, Vector3.zero);
                foreach (var render in renders)
                {
                    bounds.Encapsulate(render.bounds);
                }

                // 转换包围盒尺寸和中心点到局部坐标系
                Vector3 localCenter = gameObject.transform.InverseTransformPoint(bounds.center);
                Vector3 localSize = bounds.size;

                // 处理父级变换的影响 - 将世界坐标系的尺寸转换为本地坐标系
                Transform parentTransform = gameObject.transform.parent;
                if (parentTransform != null)
                {
                    // 获取从世界到本地的缩放转换
                    Vector3 parentLossyScale = parentTransform.lossyScale;
                    localSize = new Vector3(
                        bounds.size.x / Mathf.Abs(parentLossyScale.x),
                        bounds.size.y / Mathf.Abs(parentLossyScale.y),
                        bounds.size.z / Mathf.Abs(parentLossyScale.z)
                    );
                }

                //先判断当前是否有碰撞器 进行销毁
                var currentCollider = gameObject.GetComponent<Collider>();
                if (currentCollider != null) Object.DestroyImmediate(currentCollider);
                //添加BoxCollider 设置中心点及大小
                var boxCollider = gameObject.AddComponent<BoxCollider>();
                boxCollider.center = localCenter;
                boxCollider.size = localSize;
            }
        }
    }
}