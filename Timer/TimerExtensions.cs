using System.Threading;
using Cysharp.Threading.Tasks;

namespace WManager
{
    /// <summary>
    /// ITimer 的 UniTask 等待扩展。
    /// </summary>
    public static class TimerExtensions
    {
        /// <summary>
        /// 等待计时器完成（IsCompleted = true）。完成或取消时返回。
        /// 注意：调用前必须已 Launch()，否则会一直等到取消。
        /// </summary>
        public static async UniTask WaitAsync(this ITimer timer, CancellationToken cancellationToken = default)
        {
            if (timer == null) return;

            // 已经完成则立即返回
            if (timer.IsCompleted) return;

            // 用每帧轮询实现等待；最简单且对单帧间隔无影响
            while (!timer.IsCompleted)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await UniTask.Yield();
            }
        }
    }
}