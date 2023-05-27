using UnityEngine.Events;

namespace WManager
{
    /// <summary>
    /// 事件链接口
    /// </summary>
    public interface IActionChain
    {
        IActionChain Append(IAction action);

        /// <summary>
        /// 开始事件链
        /// </summary>
        /// <returns></returns>
        IActionChain Begin();

        /// <summary>
        /// 停止事件链
        /// </summary>
        void Stop();
        /// <summary>
        /// 暂停事件链
        /// </summary>
        void Pause();
        /// <summary>
        /// 重启事件链
        /// </summary>
        void Resume();

        bool IsPaused { get; }

        /// <summary>
        /// 设置停止条件
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        IActionChain StopWhen(System.Func<bool> predicate);

        /// <summary>
        /// 停止事件链后执行的事件
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        IActionChain OnStop(UnityAction action);

        /// <summary>
        /// 设置循环次数
        /// </summary>
        /// <param name="loops"></param>
        /// <returns></returns>
        IActionChain SetLoops(int loops);
    }
}