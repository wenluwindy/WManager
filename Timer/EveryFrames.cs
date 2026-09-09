using System;
using UnityEngine;
using UnityEngine.Events;

namespace WManager
{
    /// <summary>
    /// 周期性帧计时器：每隔指定帧数执行一次
    /// </summary>
    public sealed class EveryFrames : TimerBase<EveryFrames>
    {
        private int _beginFrame;
        private readonly int _duration;
        private int _pausedFrame;
        private int _remainingFrame;
        private int _loops;
        private readonly UnityAction _everyAction;

        /// <summary>
        /// 剩余时间（基于 Time.deltaTime 估算）
        /// </summary>
        public override float RemainingTime => _remainingFrame * Time.deltaTime;

        public EveryFrames(UnityAction everyAction, int duration = 1, int loops = -1)
        {
            _duration = duration;
            _everyAction = everyAction;
            _loops = loops;
            _remainingFrame = duration;
        }

        protected override void OnBeforeLaunch()
        {
            _remainingFrame = _duration;
            _beginFrame = Time.frameCount;
        }

        protected override void OnPauseTime()
        {
            _pausedFrame = Time.frameCount;
        }

        protected override void OnResumeCompensate()
        {
            _beginFrame += Time.frameCount - _pausedFrame;
        }

        public override bool Execute()
        {
            if (!_isRunning || IsCompleted) return true;
            if (IsPaused) return false;

            _remainingFrame = _duration - (Time.frameCount - _beginFrame);
            _onExecute?.Invoke(RemainingTime); // 统一传 float：剩余秒数估算

            if (_remainingFrame <= 0)
            {
                _everyAction?.Invoke();
                if (_loops > 0) _loops--;
                if (_loops == 0)
                {
                    Stop();
                    return true;
                }
                _beginFrame = Time.frameCount;
                _remainingFrame = _duration;
            }

            return CheckStopCondition();
        }
    }
}