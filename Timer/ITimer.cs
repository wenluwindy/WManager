namespace WManager
{
    /// <summary>
    /// 计时工具接口。
    /// Launch/Pause/Resume/Stop 返回 ITimer，便于沿用具体类型继续链式调用
    /// （实现类在内部以协变方式返回更具体的子类类型，调用方在具体类型上仍可拿到子类类型）。
    /// </summary>
    public interface ITimer
    {
        /// <summary>
        /// 是否计时完成
        /// </summary>
        bool IsCompleted { get; }
        /// <summary>
        /// 是否暂停
        /// </summary>
        bool IsPaused { get; }
        /// <summary>
        /// 是否在运行中
        /// </summary>
        bool IsRunning { get; }
        /// <summary>
        /// 启动计时
        /// </summary>
        ITimer Launch();
        /// <summary>
        /// 暂停
        /// </summary>
        ITimer Pause();
        /// <summary>
        /// 恢复/继续
        /// </summary>
        ITimer Resume();
        /// <summary>
        /// 停止
        /// </summary>
        ITimer Stop();
        /// <summary>
        /// 计时
        /// </summary>
        /// <returns>计时完成返回true 否则返回false</returns>
        bool Execute();
        /// <summary>
        /// 剩余时间（秒）。
        /// 倒计时/周期计时器返回真实剩余秒数；
        /// 正向计时器（Clock/Chronometer）返回 -1。
        /// </summary>
        float RemainingTime { get; }
    }
}