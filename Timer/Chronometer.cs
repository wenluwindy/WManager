using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine.Events;

namespace WManager
{
    /// <summary>
    /// 秒表计时器：支持中途打点记录
    /// </summary>
    public sealed class Chronometer : TimeBasedTimerBase<Chronometer>
    {
        public sealed class Record
        {
            public readonly object Context;
            public readonly float Time;
            public Record(object context, float time) { Context = context; Time = time; }
        }

        private Func<bool> _shotWhen;
        private readonly List<Record> _records;

        public float ElapsedTime { get; private set; }
        public override float RemainingTime => -1f; // 秒表无剩余时间概念
        public ReadOnlyCollection<Record> Records => new ReadOnlyCollection<Record>(_records);

        public Chronometer(bool isIgnoreTimeScale = false) : base(isIgnoreTimeScale)
        {
            _records = new List<Record>();
        }

        public Chronometer ShotWhen(Func<bool> predicate) { _shotWhen = predicate; return this; }

        /// <summary>
        /// 手动打点记录当前耗时
        /// </summary>
        public void Shot(object context = null)
        {
            _records.Add(new Record(context, ElapsedTime));
        }

        protected override void OnBeforeLaunch()
        {
            ElapsedTime = 0f;
            _beginTime = GetCurrentTime();
            _records.Clear();
        }

        public override bool Execute()
        {
            if (!_isRunning || IsCompleted) return true;
            if (IsPaused) return false;

            ElapsedTime = GetCurrentTime() - _beginTime;
            _onExecute?.Invoke(ElapsedTime);

            if (_shotWhen != null && _shotWhen.Invoke()) Shot();
            return CheckStopCondition();
        }
    }
}