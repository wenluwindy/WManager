using System.Threading;
using Cysharp.Threading.Tasks;

namespace WManager
{
    /// <summary>
    /// 面板显示 / 隐藏过渡的抽象。
    /// <see cref="UIPanel"/> 只依赖这个接口，因此框架核心不绑定任何补间库：
    /// 挂 <see cref="UITweenTransition"/> 走 DOTween，挂 <see cref="UIAnimatorTransition"/> 走 Animator，
    /// 什么都不挂则没有动画，直接显示 / 隐藏。
    ///
    /// 实现约定：
    /// 1. 新的过渡开始时必须打断上一段过渡，并让上一段的 await 以
    ///    <see cref="System.OperationCanceledException"/> 结束，绝不能让它永远挂着。
    /// 2. 打断时保留当前视觉状态，新动画从当前值继续，不要跳回起点。
    /// 3. 不负责 SetActive，激活状态由 <see cref="UIPanel"/> 统一管理。
    /// </summary>
    public interface IUITransition
    {
        /// <summary>是否正在播放过渡</summary>
        bool IsPlaying { get; }

        /// <summary>播放显示过渡。被后续过渡打断时抛出 OperationCanceledException。</summary>
        UniTask PlayShowAsync(CancellationToken cancellationToken = default);

        /// <summary>播放隐藏过渡。被后续过渡打断时抛出 OperationCanceledException。</summary>
        UniTask PlayHideAsync(CancellationToken cancellationToken = default);

        /// <summary>不播动画，立刻置为显示完成状态</summary>
        void SetShownImmediate();

        /// <summary>不播动画，立刻置为隐藏完成状态</summary>
        void SetHiddenImmediate();
    }
}
