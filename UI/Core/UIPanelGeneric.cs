namespace WManager
{
    /// <summary>
    /// 带强类型参数的面板基类。相比 <see cref="UIContext"/> 的字符串键，
    /// 参数写错会直接编译不过，而不是静默拿到默认值。
    ///
    /// <code>
    /// public class BagArgs { public int Tab; }
    ///
    /// [UIPanelInfo(Key = "UI/BagPanel")]
    /// public class BagPanel : UIPanel&lt;BagArgs&gt;
    /// {
    ///     protected override void OnWillOpen(BagArgs args) { SelectTab(args.Tab); }
    /// }
    ///
    /// await UI.OpenAsync&lt;BagPanel, BagArgs&gt;(new BagArgs { Tab = 2 });
    /// </code>
    /// </summary>
    /// <typeparam name="TArgs">参数类型</typeparam>
    public abstract class UIPanel<TArgs> : UIPanel
    {
        /// <summary>本次打开携带的参数。未传参时为 default。</summary>
        protected TArgs Args { get; private set; }

        /// <summary>本次打开是否真的带了参数</summary>
        protected bool HasArgs { get; private set; }

        protected sealed override void OnWillOpen(UIContext context)
        {
            if (context != null && context.TryGet(UIContext.ArgsKey, out TArgs args))
            {
                HasArgs = true;
                Args = args;
            }
            else
            {
                HasArgs = false;
                Args = default;
            }

            OnWillOpen(Args);
        }

        protected sealed override void OnDidOpen(UIContext context)
        {
            OnDidOpen(Args);
        }

        /// <summary>打开前，参数已就绪</summary>
        protected virtual void OnWillOpen(TArgs args) { }

        /// <summary>打开后，过渡动画播放完成</summary>
        protected virtual void OnDidOpen(TArgs args) { }
    }
}
