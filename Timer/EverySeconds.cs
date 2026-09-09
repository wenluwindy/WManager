using System;
using UnityEngine;
using UnityEngine.Events;

namespace WManager
{
    /// <summary>
    /// 周期性计时器：每隔指定秒数执行一次
    /// </summary>
    public sealed class EverySeconds : TimeBasedTimerBase<EverySeconds>
    {
        private readonly float _duration;
        private float _remainingTime;
        private int _loops;
        private readonly UnityAction _everyAction;

        public override float RemainingTime => _remainingTime;

        public EverySeconds(UnityAction everyAction, float duration = 1f, bool isIgnoreTimeScale = false, int loops = -1)
            : base(isIgnoreTimeScale)
        {
            _duration = duration;
            _everyAction = everyAction;
            _loops = loops;
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
                _everyAction?.Invoke();
                if (_loops > 0) _loops--;
                if (_loops == 0)
                {
                    Stop();
                    return true;
                }
                // 重置进入下一周期
                _beginTime = currentTime;
                _remainingTime = _duration;
            }

            return CheckStopCondition();
        }
    }
}