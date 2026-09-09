using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace WManager
{
    /// <summary>
    /// 计时器调度器：负责单例生命周期、每帧驱动所有活跃计时器、批量控制。
    /// 静态工厂方法已拆分到 TimerFactory。
    /// </summary>
    public sealed class TimerScheduler : SingletonBehaviour<TimerScheduler>
    {
        private readonly List<ITimer> _activeTimers = new List<ITimer>();

        private void Update()
        {
            // 倒序遍历避免在移除时引发索引异常
            for (int i = _activeTimers.Count - 1; i >= 0; i--)
            {
                var timer = _activeTimers[i];
                if (timer.Execute())
                    _activeTimers.RemoveAt(i);
            }
        }

        protected override void OnDestroy()
        {
            if (Instance == this)
            {
                _activeTimers.Clear();
            }
            base.OnDestroy();
        }

        /// <summary>
        /// 注册计时器（由具体计时器在 Launch() 时调用）
        /// </summary>
        internal void Register(ITimer timer)
        {
            if (timer != null && !_activeTimers.Contains(timer))
                _activeTimers.Add(timer);
        }

        /// <summary>
        /// 注销计时器（外部停止某个计时器后可手动调用）
        /// </summary>
        public void Unregister(ITimer timer)
        {
            _activeTimers.Remove(timer);
        }

        #region 全局控制接口

        public void PauseAll()
        {
            foreach (var t in _activeTimers) t.Pause();
        }

        public void ResumeAll()
        {
            foreach (var t in _activeTimers) t.Resume();
        }

        /// <summary>
        /// 停止全部计时器，并触发它们的 OnStop 回调，然后从调度器移除。
        /// </summary>
        public void StopAllAndRemove()
        {
            foreach (var t in _activeTimers) t.Stop();
            _activeTimers.Clear();
        }

        /// <summary>
        /// 仅从调度器移除所有计时器引用，不触发 OnStop 回调。
        /// </summary>
        public void RemoveAll()
        {
            _activeTimers.Clear();
        }

        #endregion
    }

    /// <summary>
    /// 计时器静态工厂门面。所有计时器实例通过这里创建。
    /// </summary>
    public static class TimerFactory
    {
        public static Clock Clock(bool isIgnoreTimeScale = false) => new Clock(isIgnoreTimeScale);
        public static Countdown Countdown(float duration, bool isIgnoreTimeScale = false) => new Countdown(duration, isIgnoreTimeScale);
        public static Chronometer Chronometer(bool isIgnoreTimeScale = false) => new Chronometer(isIgnoreTimeScale);
        public static EverySeconds EverySecond(UnityAction everyAction, bool isIgnoreTimeScale = false, int loops = -1) => new EverySeconds(everyAction, 1f, isIgnoreTimeScale, loops);
        public static EverySeconds EverySeconds(float seconds, UnityAction everyAction, bool isIgnoreTimeScale = false, int loops = -1) => new EverySeconds(everyAction, seconds, isIgnoreTimeScale, loops);
        public static EveryFrames EveryFrame(UnityAction everyAction, int loops = -1) => new EveryFrames(everyAction, 1, loops);
        public static EveryFrames EveryFrames(int frameCount, UnityAction everyAction, int loops = -1) => new EveryFrames(everyAction, frameCount, loops);
        public static EveryFrames NextFrame(UnityAction callback) => new EveryFrames(callback, 1, 1);
        public static Alarm Alarm(int hour, int minute, int second, UnityAction callback) => new Alarm(hour, minute, second, callback);
    }
}