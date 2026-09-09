using System;
using UnityEngine;

namespace WManager
{
    /// <summary>
    /// 屏幕方向检测服务。只在方向真正翻转时派发事件，
    /// 单纯改变窗口尺寸但方向不变不会触发。
    /// </summary>
    [DisallowMultipleComponent]
    public class UIOrientationService : MonoBehaviour
    {
        public static UIOrientationService Instance { get; private set; }

        public static bool HasInstance => Instance != null;

        /// <summary>当前屏幕方向</summary>
        public UIOrientation CurrentOrientation { get; private set; }

        /// <summary>方向翻转事件</summary>
        public event Action<UIOrientation> OnOrientationChanged;

        /// <summary>屏幕尺寸变化事件（包含方向不变的情况，如 PC 拖窗口）</summary>
        public event Action<Vector2Int> OnScreenSizeChanged;

        private Vector2Int _lastScreenSize;

        public static UIOrientationService Create()
        {
            if (Instance != null)
                return Instance;

            var go = new GameObject("[UIOrientationService]");
            go.AddComponent<UIOrientationService>();

            return Instance;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            _lastScreenSize = new Vector2Int(Screen.width, Screen.height);
            CurrentOrientation = Evaluate(Screen.width, Screen.height);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void Update()
        {
            if (_lastScreenSize.x == Screen.width && _lastScreenSize.y == Screen.height)
                return;

            _lastScreenSize = new Vector2Int(Screen.width, Screen.height);
            OnScreenSizeChanged?.Invoke(_lastScreenSize);

            var orientation = Evaluate(Screen.width, Screen.height);
            if (orientation == CurrentOrientation)
                return;

            CurrentOrientation = orientation;
            OnOrientationChanged?.Invoke(CurrentOrientation);
        }

        private static UIOrientation Evaluate(int width, int height)
        {
            return width >= height ? UIOrientation.Landscape : UIOrientation.Portrait;
        }
    }
}
