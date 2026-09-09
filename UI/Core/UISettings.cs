using UnityEngine;

namespace WManager
{
    /// <summary>
    /// UI 框架全局配置。
    /// 在任意 Resources 目录下放一个命名为 UISettings 的资产即可生效
    /// （菜单：Assets/Create/WManager/UI Settings）。
    /// 找不到配置时使用内置默认值，框架依然可以零配置运行。
    /// </summary>
    [CreateAssetMenu(fileName = "UISettings", menuName = "WManager/UI Settings")]
    public class UISettings : ScriptableObject
    {
        /// <summary>Resources 下的资产名</summary>
        public const string ResourcesPath = "UISettings";

        private static UISettings _instance;

        /// <summary>
        /// 全局配置实例。首次访问时从 Resources 加载，缺失则回退到内置默认值。
        /// </summary>
        public static UISettings Instance
        {
            get
            {
                if (_instance != null)
                    return _instance;

                _instance = Resources.Load<UISettings>(ResourcesPath);

                if (_instance == null)
                {
                    _instance = CreateInstance<UISettings>();
                    _instance.name = "UISettings (内置默认值)";
                }

                return _instance;
            }
        }

        /// <summary>
        /// 手动指定配置（单元测试或多套主题切换时使用）。传 null 恢复自动加载。
        /// </summary>
        public static void Override(UISettings settings)
        {
            _instance = settings;
        }

        // ---------------------------------------------------------------- 画布

        [Header("画布")]
        [Tooltip("CanvasScaler 参考分辨率")]
        public Vector2 referenceResolution = new Vector2(1920f, 1080f);

        [Tooltip("横屏时的 CanvasScaler.matchWidthOrHeight")]
        [Range(0f, 1f)]
        public float landscapeMatch = 0.35f;

        [Tooltip("竖屏时的 CanvasScaler.matchWidthOrHeight")]
        [Range(0f, 1f)]
        public float portraitMatch = 0.8f;

        [Tooltip("UIRoot 根 Canvas 的 sortingOrder")]
        public int rootSortingOrder = 1000;

        [Tooltip("相邻 UI 层之间的 sortingOrder 间隔")]
        [Min(1)]
        public int layerSortingStep = 100;

        // ---------------------------------------------------------------- 面板

        [Header("面板")]
        [Tooltip("面板未通过 [UIPanelInfo] 指定 Key 时，默认 Key = 该前缀 + 类名")]
        public string defaultKeyPrefix = "UI/";

        // ---------------------------------------------------------------- 安全区

        [Header("安全区")]
        [Tooltip("Bottom 层是否忽略安全区，让全屏背景铺满刘海与圆角区域")]
        public bool bottomLayerIgnoresSafeArea = true;

        // ---------------------------------------------------------------- 弹窗遮罩

        [Header("弹窗遮罩")]
        [Tooltip("UIShowMode.Popup 的面板是否自动垫一层遮罩")]
        public bool enablePopupMask = true;

        public Color popupMaskColor = new Color(0f, 0f, 0f, 0.6f);

        [Min(0f)]
        public float popupMaskFadeDuration = 0.2f;

        [Tooltip("点击遮罩是否关闭弹窗。面板可用 [UIPanelInfo(CloseOnMaskClick = false)] 单独覆盖")]
        public bool popupCloseOnMaskClick = true;

        // ---------------------------------------------------------------- MessageBox

        [Header("MessageBox")]
        [Tooltip("留空则使用 MessageBoxPanel 上 [UIPanelInfo] 声明的 Key")]
        public string messageBoxKey = "";

        public string messageBoxDefaultTitle = "提示";
        public string messageBoxDefaultConfirmText = "确定";
        public string messageBoxDefaultCancelText = "取消";

        // ---------------------------------------------------------------- Toast

        [Header("Toast")]
        [Tooltip("默认停留时长（不含淡入淡出）")]
        [Min(0.1f)]
        public float toastDuration = 2f;

        [Tooltip("同时可见的 Toast 数量上限，超出后最旧的立刻淡出")]
        [Min(1)]
        public int toastMaxVisible = 3;

        [Min(0f)]
        public float toastFadeDuration = 0.25f;

        [Tooltip("距离屏幕底部的距离")]
        public float toastBottomOffset = 180f;

        public Color toastBackgroundColor = new Color(0f, 0f, 0f, 0.82f);
        public Color toastTextColor = Color.white;

        [Min(1f)]
        public float toastFontSize = 32f;

        // ---------------------------------------------------------------- Loading

        [Header("Loading")]
        public string loadingDefaultText = "加载中...";

        public Color loadingBackgroundColor = new Color(0f, 0f, 0f, 0.5f);
        public Color loadingTextColor = Color.white;

        [Min(1f)]
        public float loadingFontSize = 32f;

        // ---------------------------------------------------------------- 输入

        [Header("输入")]
        [Tooltip("场景里没有 EventSystem 时自动创建一个")]
        public bool autoCreateEventSystem = true;

        [Tooltip("面板加载与过渡动画期间屏蔽全局输入，避免连点造成重入")]
        public bool blockInputDuringTransition = true;

        [Tooltip("响应 Esc / Android 返回键")]
        public bool enableBackKey = true;

        // ---------------------------------------------------------------- 缓存

        [Header("缓存")]
        [Tooltip("缓存面板数量上限，0 表示不限制。超出后按最久未使用顺序释放，Persistent 面板不参与")]
        [Min(0)]
        public int maxCachedPanels = 0;

        // ---------------------------------------------------------------- 日志

        [Header("日志")]
        [Tooltip("打开后会输出面板打开/关闭/入栈出栈的详细日志")]
        public bool verboseLog = false;
    }
}
