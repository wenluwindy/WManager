using System;
using DG.Tweening;

namespace WManager
{
    /// <summary>
    /// DoTween动画事件
    /// 依赖DoTween工具
    /// </summary>
    public class TweenAction : AbstractAction
    {
        private Tween tween;
        private readonly Func<Tween> action;
        private bool isBegan;

        public TweenAction(Func<Tween> action)
        {
            this.action = action;
        }

        protected override void OnInvoke()
        {
            if (!isBegan)
            {
                isBegan = true;
                tween = action.Invoke();
            }
            isCompleted = !tween.IsPlaying();
        }

        protected override void OnReset()
        {
            isBegan = false;
        }
    }

    public static class TweenActionExtension
    {
        /// <summary>
        /// DoTween动画事件
        /// </summary>
        /// <param name="chain"></param>
        /// <param name="tweenAction"></param>
        /// <returns></returns>
        public static IActionChain Tween(this IActionChain chain, Func<Tween> tweenAction)
        {
            return chain.Append(new TweenAction(tweenAction));
        }
    }
}