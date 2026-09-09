using System;
using UnityEngine.Events;

namespace WManager
{
    /// <summary>
    /// 闹钟：基于系统真实时间触发。
    /// 语义不同于其他计时器：基于绝对时间（时分秒），不支持 StopWhen。
    /// </summary>
    public sealed class Alarm : TimerBase<Alarm>
    {
        private readonly int _hour;
        private readonly int _minute;
        private readonly int _second;
        private readonly UnityAction _callback;

        public override float RemainingTime
            => (float)(DateTime.Now - new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, _hour, _minute, _second)).TotalSeconds;

        public Alarm(int hour, int minute, int second, UnityAction callback)
        {
            _hour = hour;
            _minute = minute;
            _second = second;
            _callback = callback;
        }

        public override bool Execute()
        {
            if (!_isRunning || IsCompleted) return true;
            if (IsPaused) return false;

            var now = DateTime.Now;
            if (now.Hour == _hour && now.Minute == _minute && now.Second == _second)
            {
                _callback?.Invoke();
                Stop();
                return true;
            }
            return false;
        }
    }
}