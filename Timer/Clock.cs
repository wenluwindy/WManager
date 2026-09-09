using UnityEngine;

namespace WManager
{
    /// <summary>
    /// 正向计时器：记录已流逝时间
    /// </summary>
    public sealed class Clock : TimeBasedTimerBase<Clock>
    {
        public float ElapsedTime { get; private set; }

        /// <summary>
        /// 正向计时器无"剩余时间"概念
        /// </summary>
        public override float RemainingTime => -1f;

        public Clock(bool isIgnoreTimeScale = false) : base(isIgnoreTimeScale) { }

        protected override void OnBeforeLaunch()
        {
            ElapsedTime = 0f;
            _beginTime = GetCurrentTime();
        }

        public override bool Execute()
        {
            if (!_isRunning || IsCompleted) return true;
            if (IsPaused) return false;

            ElapsedTime = GetCurrentTime() - _beginTime;
            _onExecute?.Invoke(ElapsedTime);
            return CheckStopCondition();
        }
    }
}