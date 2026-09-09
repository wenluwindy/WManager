using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace WManager
{
    /// <summary>
    /// UI 根节点。负责创建 Canvas、各层级节点、安全区容器和输入拦截层。
    ///
    /// 层级结构：
    /// <code>
    /// [UIRoot]                Canvas + CanvasScaler + GraphicRaycaster
    /// ├── Bottom              全屏背景层，默认忽略安全区，铺满刘海与圆角
    /// ├── SafeArea            UISafeAreaFitter 驱动
    /// │   ├── Normal
    /// │   ├── Popup
    /// │   ├── Top
    /// │   └── Tips
    /// └── Blocker             加载 / 过渡期间的透明输入拦截层
    /// </code>
    ///
    /// 安全区适配挂在 SafeArea 子节点上而不是 Canvas 本体：
    /// ScreenSpaceOverlay 的 Canvas 会驱动自己的 RectTransform，
    /// 挂在上面改 anchor 会被 Canvas 覆盖掉，等于没适配。
    /// </summary>
    [DisallowMultipleComponent]
    public class UIRoot : MonoBehaviour
    {
        public static UIRoot Instance { get; private set; }

        public static bool HasInstance => Instance != null;

        public Canvas RootCanvas { get; private set; }
        public CanvasScaler CanvasScaler { get; private set; }
        public GraphicRaycaster GraphicRaycaster { get; private set; }

        /// <summary>安全区容器。Bottom 之外的层都挂在它下面。</summary>
        public RectTransform SafeAreaRoot { get; private set; }

        /// <summary>全屏输入拦截层</summary>
        public UIInputBlocker InputBlocker { get; private set; }

        private readonly Dictionary<UILayer, RectTransform> _layerRoots = new();
        private bool _initialized;

        /// <summary>
        /// 创建（或返回已有的）UI 根节点。
        /// <see cref="UIManager"/> 第一次被访问时会自动调用，通常不需要业务侧手动调。
        /// </summary>
        public static UIRoot Create()
        {
            if (Instance != null)
                return Instance;

            var go = new GameObject(
                "[UIRoot]",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));

            // AddComponent 会同步触发 Awake，Instance 与 Initialize 都在那里完成
            go.AddComponent<UIRoot>();

            return Instance;
        }

        /// <summary>
        /// 取层级节点。层级不存在时回退到 Normal，不会抛异常。
        /// </summary>
        public RectTransform GetLayerRoot(UILayer layer)
        {
            if (_layerRoots.TryGetValue(layer, out var root) && root != null)
                return root;

            Debug.LogError($"[UIRoot] 找不到层级节点 {layer}，已回退到 Normal 层。");

            if (_layerRoots.TryGetValue(UILayer.Normal, out var fallback) && fallback != null)
                return fallback;

            return (RectTransform)transform;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[UIRoot] 场景里已存在 UIRoot，销毁重复实例。", this);
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            Initialize();
        }

        private void OnDestroy()
        {
            if (UIOrientationService.HasInstance)
                UIOrientationService.Instance.OnOrientationChanged -= HandleOrientationChanged;

            if (Instance == this)
                Instance = null;
        }

        private void Initialize()
        {
            if (_initialized)
                return;

            _initialized = true;

            var settings = UISettings.Instance;

            UIOrientationService.Create();
            EnsureEventSystem(settings);

            RootCanvas = GetOrAdd<Canvas>();
            RootCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            RootCanvas.sortingOrder = settings.rootSortingOrder;

            CanvasScaler = GetOrAdd<CanvasScaler>();
            CanvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            CanvasScaler.referenceResolution = settings.referenceResolution;
            CanvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;

            GraphicRaycaster = GetOrAdd<GraphicRaycaster>();

            ApplyMatchRule();

            SafeAreaRoot = UIFactory.CreateStretchNode("SafeArea", transform);
            SafeAreaRoot.gameObject.AddComponent<UISafeAreaFitter>();

            CreateLayers(settings);

            InputBlocker = UIInputBlocker.Create(
                transform,
                GetLayerSortingOrder(UILayer.Tips, settings) + settings.layerSortingStep);

            if (UIOrientationService.HasInstance)
                UIOrientationService.Instance.OnOrientationChanged += HandleOrientationChanged;
        }

        private void CreateLayers(UISettings settings)
        {
            foreach (UILayer layer in Enum.GetValues(typeof(UILayer)))
            {
                // Bottom 放在安全区外面，全屏背景才能铺满刘海区域
                bool outsideSafeArea = layer == UILayer.Bottom && settings.bottomLayerIgnoresSafeArea;
                var parent = outsideSafeArea ? transform : SafeAreaRoot;

                var rect = UIFactory.CreateStretchNode(layer.ToString(), parent);
                UIFactory.AddSortingCanvas(rect.gameObject, GetLayerSortingOrder(layer, settings));

                _layerRoots[layer] = rect;
            }
        }

        private static int GetLayerSortingOrder(UILayer layer, UISettings settings)
        {
            return (int)layer * settings.layerSortingStep;
        }

        private void HandleOrientationChanged(UIOrientation orientation)
        {
            ApplyMatchRule();
        }

        /// <summary>
        /// 横屏按宽度对齐、竖屏按高度对齐，避免同一套 UI 在两种方向上一个溢出一个留白。
        /// </summary>
        private void ApplyMatchRule()
        {
            if (CanvasScaler == null)
                return;

            var settings = UISettings.Instance;

            var orientation = UIOrientationService.HasInstance
                ? UIOrientationService.Instance.CurrentOrientation
                : (Screen.width >= Screen.height ? UIOrientation.Landscape : UIOrientation.Portrait);

            CanvasScaler.matchWidthOrHeight = orientation == UIOrientation.Landscape
                ? settings.landscapeMatch
                : settings.portraitMatch;
        }

        private static void EnsureEventSystem(UISettings settings)
        {
            if (!settings.autoCreateEventSystem)
                return;

            if (EventSystem.current != null)
                return;

#if UNITY_2023_1_OR_NEWER
            var existing = FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include);
#else
            var existing = FindObjectOfType<EventSystem>(true);
#endif
            if (existing != null)
                return;

            var go = new GameObject("[EventSystem]", typeof(EventSystem));

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            go.AddComponent<StandaloneInputModule>();
#endif

            DontDestroyOnLoad(go);

            Debug.Log("[UIRoot] 场景里没有 EventSystem，已自动创建一个。");
        }

        private T GetOrAdd<T>() where T : Component
        {
            var component = GetComponent<T>();
            return component != null ? component : gameObject.AddComponent<T>();
        }
    }
}
