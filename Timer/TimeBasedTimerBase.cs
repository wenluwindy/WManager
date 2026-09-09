using UnityEngine;

namespace WManager
{
    /// <summary>
    /// 基于 Time.time / Time.unscaledTime 的计时器基类。
    /// 处理 _beginTime / _pausedTime / 暂停时间补偿，子类无需关心这些样板。
    /// 同样采用 CRTP：子类继承时把自己作为泛型实参传入，链式方法沿用子类类型。
    /// </summary>
    public abstract class TimeBasedTimerBase<TSelf> : TimerBase<TSelf> where TSelf : TimeBasedTimerBase<TSelf>
    {
        protected float _beginTime;
        protected float _pausedTime;
        protected readonly bool _isIgnoreTimeScale;

        protected TimeBasedTimerBase(bool isIgnoreTimeScale = false)
        {
            _isIgnoreTimeScale = isIgnoreTimeScale;
        }

        /// <summary>
        /// 当前时间源。受 _isIgnoreTimeScale 控制。
        /// </summary>
        protected float GetCurrentTime()
        {
            return _isIgnoreTimeScale ? Time.unscaledTime : Time.time;
        }

        protected override void OnPauseTime()
        {
            _pausedTime = GetCurrentTime();
        }

        protected override void OnResumeCompensate()
        {
            // 补偿暂停期间流逝的时间，防止计时跳变
            _beginTime += GetCurrentTime() - _pausedTime;
        }
    }

    /// <summary>
    /// 兼容旧代码：非泛型 TimeBasedTimerBase 等价于 TimeBasedTimerBase&lt;TimeBasedTimerBase&gt;。
    /// 新代码建议继承泛型版。
    /// </summary>
    public abstract class TimeBasedTimerBase : TimeBasedTimerBase<TimeBasedTimerBase>
    {
        protected TimeBasedTimerBase(bool isIgnoreTimeScale = false) : base(isIgnoreTimeScale) { }
    }
}