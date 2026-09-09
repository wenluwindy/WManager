using System;
using UnityEngine;

namespace WManager
{
    /// <summary>
    /// 倒计时计时器
    /// </summary>
    public sealed class Countdown : TimeBasedTimerBase<Countdown>
    {
        private readonly float _duration;
        private float _remainingTime;

        public override float RemainingTime
        {
            get => _remainingTime;
        }

        public Countdown(float duration, bool isIgnoreTimeScale = false) : base(isIgnoreTimeScale)
        {
            if (duration < 0f) throw new ArgumentOutOfRangeException(nameof(duration), "时长必须为非负!");
            _duration = duration;
            _remainingTime = duration;
        }

        protected override void OnBeforeLaunch()
        {
            _remainingTime = _duration;
            _beginTime = GetCurrentTime();
        }

        public override bool Execute()
        {
            if (!_isRunning || IsCompleted) return true;
            if (IsPaused) return false;

            float currentTime = GetCurrentTime();
            _remainingTime = _duration - (currentTime - _beginTime);
            _remainingTime = Mathf.Clamp(_remainingTime, 0f, _duration);

            _onExecute?.Invoke(_remainingTime);

            if (_remainingTime <= 0f)
            {
                Stop();
                return true;
            }

            return CheckStopCondition();
        }
    }
}