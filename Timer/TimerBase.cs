using System;
using UnityEngine.Events;

namespace WManager
{
    /// <summary>
    /// 计时器抽象基类。提供：
    /// - 通用状态机（IsRunning / IsPaused / IsCompleted）
    /// - 链式配置回调（OnLaunch/OnExecute/OnPause/OnResume/OnStop/StopWhen）
    /// - 与 TimerScheduler 的注册/反注册
    ///
    /// 子类只需覆写 Execute()，按需覆写 GetCurrentTime() 与受保护的 Launch/Stop 钩子。
    ///
    /// 采用 CRTP：所有链式方法返回 TSelf（子类），以便子类类型沿着链路保留，
    /// 调用方不再需要写强制转型（如 (Countdown)TimerFactory.Countdown(3f).OnLaunch(...)）。
    /// </summary>
    public abstract class TimerBase<TSelf> : ITimer where TSelf : TimerBase<TSelf>
    {
        protected UnityAction _onLaunch;
        protected UnityAction<float> _onExecute;
        protected UnityAction _onPause;
        protected UnityAction _onResume;
        protected UnityAction _onStop;
        protected Func<bool> _stopWhen;

        protected bool _isRunning;

        public bool IsCompleted { get; protected set; }
        public bool IsPaused { get; protected set; }
        public bool IsRunning => _isRunning;
        public abstract float RemainingTime { get; }

        #region 链式配置（返回子类类型，避免向下转型）

        public TSelf OnLaunch(UnityAction callback) { _onLaunch = callback; return (TSelf)this; }
        public TSelf OnExecute(UnityAction<float> callback) { _onExecute = callback; return (TSelf)this; }
        public TSelf OnPause(UnityAction callback) { _onPause = callback; return (TSelf)this; }
        public TSelf OnResume(UnityAction callback) { _onResume = callback; return (TSelf)this; }
        public TSelf OnStop(UnityAction callback) { _onStop = callback; return (TSelf)this; }
        public TSelf StopWhen(Func<bool> predicate) { _stopWhen = predicate; return (TSelf)this; }

        #endregion

        /// <summary>
        /// 启动计时。子类一般无需覆写，若需在启动前重置额外字段可覆写 OnBeforeLaunch 钩子。
        /// </summary>
        public virtual ITimer Launch()
        {
            if (_isRunning || IsCompleted) return this;

            _isRunning = true;
            IsCompleted = false;
            IsPaused = false;
            OnBeforeLaunch();
            _onLaunch?.Invoke();
            TimerScheduler.Instance?.Register(this);
            return this;
        }

        /// <summary>
        /// 启动前的额外初始化钩子（重置子类字段）。子类按需覆写。
        /// </summary>
        protected virtual void OnBeforeLaunch() { }

        public virtual ITimer Pause()
        {
            if (!_isRunning || IsPaused || IsCompleted) return this;
            IsPaused = true;
            OnPauseTime();
            _onPause?.Invoke();
            return this;
        }

        /// <summary>
        /// 暂停时记录当前时间戳的钩子。TimeBasedTimerBase 会覆写它。
        /// </summary>
        protected virtual void OnPauseTime() { }

        public virtual ITimer Resume()
        {
            if (!_isRunning || !IsPaused || IsCompleted) return this;
            IsPaused = false;
            OnResumeCompensate();
            _onResume?.Invoke();
            return this;
        }

        /// <summary>
        /// 恢复时补偿暂停流逝时间的钩子。TimeBasedTimerBase 会覆写它。
        /// </summary>
        protected virtual void OnResumeCompensate() { }

        public virtual ITimer Stop()
        {
            if (IsCompleted) return this;
            IsCompleted = true;
            _isRunning = false;
            IsPaused = false;
            _onStop?.Invoke();
            return this;
        }

        /// <summary>
        /// 由 TimerScheduler 每帧调用。
        /// 返回 true 表示计时器已结束，应从调度器移除。
        /// </summary>
        public abstract bool Execute();

        /// <summary>
        /// 通用停止条件检查：子类 Execute() 末尾调用即可。
        /// </summary>
        protected bool CheckStopCondition()
        {
            if (_stopWhen != null && _stopWhen.Invoke())
            {
                Stop();
                return true;
            }
            return false;
        }
    }

    /// <summary>
    /// 非泛型基类，保持向后兼容。
    /// 新代码建议继承泛型版 TimerBase&lt;TSelf&gt; 以保留子类类型用于链式调用。
    /// </summary>
    public abstract class TimerBase : TimerBase<TimerBase>
    {
    }
}