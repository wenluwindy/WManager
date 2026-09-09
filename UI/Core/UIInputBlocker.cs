using UnityEngine;
using UnityEngine.UI;

namespace WManager
{
    /// <summary>
    /// 全屏透明输入拦截层。面板加载中或过渡动画播放中启用，
    /// 挡掉这段时间的点击，从根本上避免连点造成的重复打开与状态错乱。
    /// 用引用计数管理，可以安全嵌套。
    /// </summary>
    [DisallowMultipleComponent]
    public class UIInputBlocker : MonoBehaviour
    {
        private int _count;

        /// <summary>当前是否正在拦截输入</summary>
        public bool IsBlocking => _count > 0;

        /// <summary>当前的嵌套层数</summary>
        public int Depth => _count;

        internal static UIInputBlocker Create(Transform parent, int sortingOrder)
        {
            var rect = UIFactory.CreateStretchNode(
                "Blocker",
                parent,
                typeof(CanvasRenderer),
                typeof(Image));

            var go = rect.gameObject;
            UIFactory.AddSortingCanvas(go, sortingOrder);

            var image = go.GetComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0f);
            image.raycastTarget = true;

            var blocker = go.AddComponent<UIInputBlocker>();
            go.SetActive(false);

            return blocker;
        }

        /// <summary>开始拦截</summary>
        public void Push()
        {
            _count++;
            Refresh();
        }

        /// <summary>结束一层拦截</summary>
        public void Pop()
        {
            _count = Mathf.Max(0, _count - 1);
            Refresh();
        }

        /// <summary>强制清空（异常兜底 / 切场景时）</summary>
        public void Clear()
        {
            _count = 0;
            Refresh();
        }

        private void Refresh()
        {
            bool shouldBlock = _count > 0;

            if (gameObject.activeSelf != shouldBlock)
                gameObject.SetActive(shouldBlock);

            if (shouldBlock)
                transform.SetAsLastSibling();
        }
    }
}
