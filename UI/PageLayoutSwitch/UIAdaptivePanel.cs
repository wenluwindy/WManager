using UnityEngine;

namespace WManager
{
    /// <summary>
    /// 需要横竖屏两套布局的页面基类。在 Inspector 里挂上两个布局根节点，
    /// 方向变化时自动切换，并回调 <see cref="OnOrientationChanged"/>。
    ///
    /// 两套布局里的按钮在 <c>OnCreate</c> 里用 <c>BindClickAll</c> 一次性全部绑好即可，
    /// 不需要在方向切换时重新绑定：同一时刻只有一套布局是激活的，另一套收不到点击。
    ///
    /// <code>
    /// [UIPanelInfo(Key = "UI/BagPanel")]
    /// public class BagPanel : UIAdaptivePanel
    /// {
    ///     [SerializeField] private Button landscapeBackButton;
    ///     [SerializeField] private Button portraitBackButton;
    ///
    ///     protected override void OnCreate()
    ///     {
    ///         BindClickAll(Close, landscapeBackButton, portraitBackButton);
    ///     }
    ///
    ///     protected override void OnOrientationChanged(UIOrientation orientation) => Refresh();
    /// }
    /// </code>
    ///
    /// 需要强类型打开参数时用 <see cref="UIAdaptivePanel{TArgs}"/>。
    /// </summary>
    public abstract class UIAdaptivePanel : UIPanel
    {
        [Header("自适应布局节点")]
        [SerializeField] private GameObject landscapeRoot;
        [SerializeField] private GameObject portraitRoot;

        private UIOrientationBinder _binder;

        private UIOrientationBinder Binder =>
            _binder ??= new UIOrientationBinder(landscapeRoot, portraitRoot, OnOrientationChanged);

        /// <summary>当前页面方向</summary>
        public UIOrientation CurrentOrientation => Binder.Current;

        /// <summary>横屏布局根节点</summary>
        protected GameObject LandscapeRoot => landscapeRoot;

        /// <summary>竖屏布局根节点</summary>
        protected GameObject PortraitRoot => portraitRoot;

        /// <summary>当前生效的布局根节点</summary>
        protected GameObject ActiveRoot => Pick(landscapeRoot, portraitRoot);

        /// <summary>按当前方向从两个候选里挑一个，省掉到处写 if</summary>
        protected T Pick<T>(T landscape, T portrait)
        {
            return CurrentOrientation == UIOrientation.Landscape ? landscape : portrait;
        }

        /// <summary>
        /// 方向变化后回调。此时布局节点已经切换完毕，
        /// 适合在这里重新计算列表行数、刷新排版之类的事情。
        /// </summary>
        protected virtual void OnOrientationChanged(UIOrientation orientation) { }

        // 订阅放在 OnEnable / OnDisable：既不需要子类记得调 base.OnCreate()，
        // 也不会在对象销毁时留下悬空委托。
        protected virtual void OnEnable() => Binder.Enable();

        protected virtual void OnDisable() => Binder.Disable();
    }

    /// <summary>
    /// 带强类型打开参数的横竖屏双布局页面。
    /// 行为与 <see cref="UIAdaptivePanel"/> 完全一致，只是参数从
    /// <see cref="UIContext"/> 换成了 <typeparamref name="TArgs"/>。
    /// </summary>
    public abstract class UIAdaptivePanel<TArgs> : UIPanel<TArgs>
    {
        [Header("自适应布局节点")]
        [SerializeField] private GameObject landscapeRoot;
        [SerializeField] private GameObject portraitRoot;

        private UIOrientationBinder _binder;

        private UIOrientationBinder Binder =>
            _binder ??= new UIOrientationBinder(landscapeRoot, portraitRoot, OnOrientationChanged);

        /// <inheritdoc cref="UIAdaptivePanel.CurrentOrientation" />
        public UIOrientation CurrentOrientation => Binder.Current;

        protected GameObject LandscapeRoot => landscapeRoot;

        protected GameObject PortraitRoot => portraitRoot;

        protected GameObject ActiveRoot => Pick(landscapeRoot, portraitRoot);

        /// <inheritdoc cref="UIAdaptivePanel.Pick{T}" />
        protected T Pick<T>(T landscape, T portrait)
        {
            return CurrentOrientation == UIOrientation.Landscape ? landscape : portrait;
        }

        /// <inheritdoc cref="UIAdaptivePanel.OnOrientationChanged" />
        protected virtual void OnOrientationChanged(UIOrientation orientation) { }

        protected virtual void OnEnable() => Binder.Enable();

        protected virtual void OnDisable() => Binder.Disable();
    }
}
