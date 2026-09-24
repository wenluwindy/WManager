using UnityEngine;
using Sirenix.OdinInspector;

namespace WManager
{
    /// <summary>
    /// UI 框架全局配置。
    /// 在任意 Resources 目录下放一个命名为 UISettings 的资产即可生效
    /// （菜单：Assets/Create/WManager/UI Settings）。
    /// 找不到配置时使用内置默认值，框架依然可以零配置运行。
    ///
    /// 本类在安装了 Odin Inspector（定义了 ODIN_INSPECTOR）的工程中使用 Odin 提供的
    /// 分页、分组、折叠与条件显隐（顶部布局为「设置」标签栏）；未安装 Odin 时由
    /// Core/OdinAttributeStubs.cs 提供同名属性桩，仍可正常编译与展示。
    /// </summary>
    [CreateAssetMenu(fileName = "UISettings", menuName = "WManager/UI Settings")]
    [Title("WManager · UI 全局配置", "零配置即可运行 · 缺失配置自动回退内置默认值")]
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
        /// 手动指定配置（单元测试或切换不同控制方案时使用）。传 null 恢复自动加载。
        /// </summary>
        public static void Override(UISettings settings)
        {
            _instance = settings;
        }

        // ============================================================ 画布

        [TabGroup("设置", "画布")]
        [BoxGroup("设置/画布/CanvasScaler")]
        [LabelText("参考分辨率")]
        [InfoBox("CanvasScaler 参考分辨率与横竖屏的 matchWidthOrHeight 权重，决定 UI 在不同屏幕比例下的缩放基准。")]
        [Tooltip("CanvasScaler 参考分辨率")]
        public Vector2 referenceResolution = new Vector2(1920f, 1080f);

        [BoxGroup("设置/画布/CanvasScaler")]
        [LabelText("横屏权重")]
        [Tooltip("横屏时的 CanvasScaler.matchWidthOrHeight")]
        [Range(0f, 1f)]
        public float landscapeMatch = 0.35f;

        [BoxGroup("设置/画布/CanvasScaler")]
        [LabelText("竖屏权重")]
        [Tooltip("竖屏时的 CanvasScaler.matchWidthOrHeight")]
        [Range(0f, 1f)]
        public float portraitMatch = 0.8f;

        [BoxGroup("设置/画布/排序")]
        [LabelText("UIRoot 根渲染排序")]
        [Tooltip("UIRoot 根 Canvas 的 sortingOrder")]
        public int rootSortingOrder = 1000;

        [BoxGroup("设置/画布/排序")]
        [LabelText("层与层间隔")]
        [Tooltip("相邻 UI 层之间的 sortingOrder 间隔")]
        [Min(1)]
        public int layerSortingStep = 100;

        [BoxGroup("设置/画布/安全区")]
        [LabelText("Bottom 忽略安全区")]
        [Tooltip("Bottom 层是否忽略安全区，让全屏背景铺满刘海与圆角区域")]
        public bool bottomLayerIgnoresSafeArea = true;

        // ============================================================ 面板

        [TabGroup("设置", "面板")]
        [BoxGroup("设置/面板/默认命名")]
        [LabelText("默认 Key 前缀")]
        [Tooltip("面板未通过 [UIPanelInfo] 指定 Key 时，默认 Key = 该前缀 + 类名")]
        public string defaultKeyPrefix = "UI/";

        [BoxGroup("设置/面板/默认命名")]
        [LabelText("示例 Key")]
        [ReadOnly]
        [ShowInInspector]
        private string ExamplePanelKey => defaultKeyPrefix + "ExampleMainPanel";

        // ============================================================ 弹窗

        [TabGroup("设置", "弹窗")]
        [BoxGroup("设置/弹窗/遮罩")]
        [LabelText("启用弹窗遮罩")]
        [Tooltip("UIShowMode.Popup 的面板是否自动垫一层遮罩")]
        public bool enablePopupMask = true;

        [BoxGroup("设置/弹窗/遮罩")]
        [LabelText("遮罩填充色")]
        [Tooltip("弹窗遮罩的填充颜色")]
        [ShowIf(nameof(enablePopupMask))]
        public Color popupMaskColor = new Color(0f, 0f, 0f, 0.6f);

        [BoxGroup("设置/弹窗/遮罩")]
        [LabelText("遮罩淡入时长")]
        [Tooltip("遮罩淡入淡出时长（秒）")]
        [ShowIf(nameof(enablePopupMask))]
        [Min(0f)]
        public float popupMaskFadeDuration = 0.2f;

        [BoxGroup("设置/弹窗/交互")]
        [LabelText("点击遮罩关闭")]
        [Tooltip("点击遮罩是否关闭弹窗。面板可用 [UIPanelInfo(CloseOnMaskClick = false)] 单独覆盖")]
        public bool popupCloseOnMaskClick = true;

        // ============================================================ MessageBox

        [TabGroup("设置", "消息框")]
        [BoxGroup("设置/消息框/路由")]
        [LabelText("MessageBox Key")]
        [Tooltip("留空则使用 MessageBoxPanel 上 [UIPanelInfo] 声明的 Key")]
        public string messageBoxKey = "";

        [BoxGroup("设置/消息框/默认文本")]
        [LabelText("默认标题")]
        public string messageBoxDefaultTitle = "提示";

        [BoxGroup("设置/消息框/默认文本")]
        [LabelText("确认按钮文本")]
        public string messageBoxDefaultConfirmText = "确定";

        [BoxGroup("设置/消息框/默认文本")]
        [LabelText("取消按钮文本")]
        public string messageBoxDefaultCancelText = "取消";

        // ============================================================ Toast

        [TabGroup("设置", "Toast")]
        [BoxGroup("设置/Toast/时效")]
        [LabelText("停留时长")]
        [Tooltip("默认停留时长（不含淡入淡出）")]
        [Min(0.1f)]
        public float toastDuration = 2f;

        [BoxGroup("设置/Toast/时效")]
        [LabelText("同时可见上限")]
        [Tooltip("同时可见的 Toast 数量上限，超出后最旧的立刻淡出")]
        [Min(1)]
        public int toastMaxVisible = 3;

        [BoxGroup("设置/Toast/时效")]
        [LabelText("淡入淡出时长")]
        [Min(0f)]
        public float toastFadeDuration = 0.25f;

        [BoxGroup("设置/Toast/布局")]
        [LabelText("距屏幕底部")]
        [Tooltip("距离屏幕底部的距离")]
        public float toastBottomOffset = 180f;

        [BoxGroup("设置/Toast/样式")]
        [LabelText("背景色")]
        public Color toastBackgroundColor = new Color(0f, 0f, 0f, 0.82f);

        [BoxGroup("设置/Toast/样式")]
        [LabelText("文字色")]
        public Color toastTextColor = Color.white;

        [BoxGroup("设置/Toast/样式")]
        [LabelText("字号")]
        [Min(1f)]
        public float toastFontSize = 32f;

        // ============================================================ Loading

        [TabGroup("设置", "加载")]
        [BoxGroup("设置/加载/默认文案")]
        [LabelText("默认文案")]
        public string loadingDefaultText = "加载中...";

        [BoxGroup("设置/加载/样式")]
        [LabelText("背景色")]
        public Color loadingBackgroundColor = new Color(0f, 0f, 0f, 0.5f);

        [BoxGroup("设置/加载/样式")]
        [LabelText("文字色")]
        public Color loadingTextColor = Color.white;

        [BoxGroup("设置/加载/样式")]
        [LabelText("字号")]
        [Min(1f)]
        public float loadingFontSize = 32f;

        // ============================================================ 输入

        [TabGroup("设置", "输入")]
        [BoxGroup("设置/输入/EventSystem")]
        [LabelText("自动创建 EventSystem")]
        [Tooltip("场景里没有 EventSystem 时自动创建一个")]
        public bool autoCreateEventSystem = true;

        [BoxGroup("设置/输入/过渡")]
        [LabelText("过渡期屏蔽输入")]
        [Tooltip("面板加载与过渡动画期间屏蔽全局输入，避免连点造成重入")]
        public bool blockInputDuringTransition = true;

        [BoxGroup("设置/输入/返回键")]
        [LabelText("启用返回键")]
        [Tooltip("响应 Esc / Android 返回键")]
        public bool enableBackKey = true;

        // ============================================================ 缓存与日志

        [TabGroup("设置", "缓存与日志")]
        [BoxGroup("设置/缓存与日志/缓存")]
        [LabelText("面板缓存上限")]
        [Tooltip("缓存面板数量上限，0 表示不限制。超出后按最久未使用顺序释放，Persistent 面板不参与")]
        [Min(0)]
        public int maxCachedPanels = 0;

        [BoxGroup("设置/缓存与日志/日志")]
        [LabelText("详细日志")]
        [Tooltip("打开后会输出面板打开/关闭/入栈出栈的详细日志")]
        public bool verboseLog = false;
    }
}