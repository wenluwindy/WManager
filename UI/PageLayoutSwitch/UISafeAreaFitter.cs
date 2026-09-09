using UnityEngine;

namespace WManager
{
    /// <summary>
    /// 让节点跟随屏幕安全区（刘海、圆角、home 指示条）。
    ///
    /// 注意不要挂在 ScreenSpaceOverlay 的 Canvas 本体上：
    /// Canvas 会驱动自己的 RectTransform，anchor 改了也会被覆盖。
    /// 正确做法是在 Canvas 下建一个容器节点，挂在容器上。
    /// <see cref="UIRoot"/> 的 SafeArea 节点就是这样组织的。
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class UISafeAreaFitter : MonoBehaviour
    {
        [Tooltip("适配水平方向（横屏刘海、圆角）")]
        [SerializeField] private bool applyX = true;

        [Tooltip("适配垂直方向（竖屏刘海、home 指示条）")]
        [SerializeField] private bool applyY = true;

        [Tooltip("勾选后完全不做适配，节点铺满整个屏幕")]
        [SerializeField] private bool ignoreSafeArea = false;

        private RectTransform _rectTransform;
        private Rect _lastSafeArea = Rect.zero;
        private Vector2Int _lastScreenSize = Vector2Int.zero;
        private bool _applied;

        /// <summary>运行时切换是否忽略安全区</summary>
        public bool IgnoreSafeArea
        {
            get => ignoreSafeArea;
            set
            {
                if (ignoreSafeArea == value)
                    return;

                ignoreSafeArea = value;
                ApplySafeArea();
            }
        }

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            ApplySafeArea();
        }

        private void OnEnable()
        {
            ApplySafeArea();
        }

        private void Update()
        {
            if (_rectTransform == null)
                _rectTransform = GetComponent<RectTransform>();

            if (!_applied ||
                _lastSafeArea != Screen.safeArea ||
                _lastScreenSize.x != Screen.width ||
                _lastScreenSize.y != Screen.height)
            {
                ApplySafeArea();
            }
        }

        /// <summary>立刻按当前屏幕安全区刷新一次</summary>
        public void ApplySafeArea()
        {
            if (_rectTransform == null)
                _rectTransform = GetComponent<RectTransform>();

            if (_rectTransform == null)
                return;

            if (Screen.width <= 0 || Screen.height <= 0)
                return;

            _lastSafeArea = Screen.safeArea;
            _lastScreenSize = new Vector2Int(Screen.width, Screen.height);
            _applied = true;

            var anchorMin = Vector2.zero;
            var anchorMax = Vector2.one;

            if (!ignoreSafeArea)
            {
                var safeArea = Screen.safeArea;

                anchorMin = safeArea.position;
                anchorMax = safeArea.position + safeArea.size;

                anchorMin.x /= Screen.width;
                anchorMin.y /= Screen.height;
                anchorMax.x /= Screen.width;
                anchorMax.y /= Screen.height;
            }

            var currentMin = _rectTransform.anchorMin;
            var currentMax = _rectTransform.anchorMax;

            if (applyX || ignoreSafeArea)
            {
                currentMin.x = anchorMin.x;
                currentMax.x = anchorMax.x;
            }

            if (applyY || ignoreSafeArea)
            {
                currentMin.y = anchorMin.y;
                currentMax.y = anchorMax.y;
            }

            _rectTransform.anchorMin = currentMin;
            _rectTransform.anchorMax = currentMax;
            _rectTransform.offsetMin = Vector2.zero;
            _rectTransform.offsetMax = Vector2.zero;
        }
    }
}
