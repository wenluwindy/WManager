// ----------------------------------------------------------------------------
// TimerExamples.cs
//
// 演示 TimerFactory（静态工厂）、TimerBase 链式回调、TimerScheduler 全局控制、
// TimerPool 对象池、TimerExtensions.WaitAsync。
// 注意：所有计时器都依赖 TimerScheduler.Instance（它是 MonoBehaviour 单例，
// 所以确保场景里有挂过 TimerScheduler，或者通过访问 .Instance 自动创建）。
// ----------------------------------------------------------------------------
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using WManager;

namespace WManager.Example
{
    public class TimerExample : MonoBehaviour
    {
        // 演示用：保存当前正在跑的定时器，便于停止
        private Countdown _countdown;
        private EverySeconds _ticker;
        private Clock _clock;

        private void Start()
        {
            // ---- 1. 倒计时 3 秒：到点触发回调 ----
            // 注意：OnLaunch/OnExecute/OnStop 等链式方法保留 Countdown 子类类型，
            // 因此这里 _countdown 字段可以直接接收，不再需要 (Countdown) 强制转型。
            _countdown = TimerFactory.Countdown(3f)
                .OnLaunch(() => Debug.Log("[Timer] Countdown Launch"))                   // 启动时
                .OnExecute(remain => Debug.Log($"[Timer] Countdown Remain={remain:F2}"))  // 每帧回调（参数是剩余秒数）
                .OnStop(() => Debug.Log("[Timer] Countdown Stop"));                      // 正常结束或 Stop() 时

            _countdown.Launch();

            // ---- 2. 周期计时器：每 1 秒执行一次，共 5 次 ----
            _ticker = TimerFactory.EverySeconds(
                seconds: 1f,
                everyAction: () => Debug.Log("[Timer] Tick"),
                isIgnoreTimeScale: false,
                loops: 5);
            _ticker.Launch();

            // ---- 3. 正向计时器：从 0 开始累加 ----
            _clock = TimerFactory.Clock(isIgnoreTimeScale: false);
            _clock.Launch();
        }

        private void Update()
        {
            // Clock.RemainingTime 对正向计时器返回 -1，这里仅示意如何读取
            // 如果是倒计时/周期计时器则返回剩余秒数
            if (_clock != null && _clock.IsRunning)
            {
                // Clock 没有 RemainingTime 概念，这里只是演示 IsRunning 检查
            }
        }

        private void OnDisable()
        {
            // 退出前手动停掉，否则物体销毁时计时器还会跑一帧
            _countdown?.Stop();
            _ticker?.Stop();
            _clock?.Stop();

            _countdown = null;
            _ticker = null;
            _clock = null;
        }

        // ============================================================================
        // await 风格的写法：等到计时结束再继续
        // ============================================================================
        public async UniTask CountdownWithAwaitAsync(CancellationToken ct = default)
        {
            Debug.Log("[Timer] 开始 await 3 秒");

            // 必须先 Launch，再 WaitAsync；不然会一直挂起直到 ct 取消
            var timer = TimerFactory.Countdown(3f);
            timer.Launch();
            await timer.WaitAsync(ct);

            Debug.Log("[Timer] 3 秒到了");
        }

        // ============================================================================
        // 链式调用演示：倒计时 + 自动暂停 + 自动恢复 + 条件停止
        // ============================================================================
        public void ChainedCountdown()
        {
            bool gameOver = false;

            var cd = TimerFactory.Countdown(10f)
                .OnLaunch(() => Debug.Log("[Timer] 开始 10 秒倒计时"))
                .OnExecute(remain => Debug.Log($"[Timer] 还剩 {remain:F1} 秒"))
                .OnPause(() => Debug.Log("[Timer] 暂停（玩家按 ESC 时）"))
                .OnResume(() => Debug.Log("[Timer] 恢复"))
                .OnStop(() => Debug.Log("[Timer] 结束"))
                .StopWhen(() => gameOver);   // 返回 true 立刻停止

            cd.Launch();

            // 3 秒后模拟玩家暂停（被切后台）
            TimerFactory.EverySeconds(3f, () =>
            {
                Debug.Log("[Timer] 触发暂停");
                cd.Pause();

                // 2 秒后恢复
                TimerFactory.EverySeconds(2f, () =>
                {
                    Debug.Log("[Timer] 触发恢复");
                    cd.Resume();
                }).Launch();
            }).Launch();
        }

        // ============================================================================
        // 时间格式化工具：TimeTool
        // ============================================================================
        public void TimeFormatExamples()
        {
            float seconds = 3725.5f; // 1h2m5.5s

            Debug.Log(TimeTool.ToStandardTimeFormat(seconds));   // "01:02:05"
            Debug.Log(TimeTool.ToMSTimeFormat(seconds));         // "62:05"
            Debug.Log(TimeTool.ToHMSFTimeFormat(seconds));       // "01:02:05.500"
            Debug.Log(TimeTool.ToMSFTimeFormat(seconds));        // "62:05.500"

            // 时间单位 → 秒
            Debug.Log(TimeTool.Convert2Seconds(2, TimeUnit.Hour));   // 7200
            Debug.Log(TimeTool.Convert2Seconds(3, TimeUnit.Minute)); // 180

            // 时间戳（毫秒）
            Debug.Log(TimeTool.GetTimeStamp(System.DateTime.UtcNow));
        }

        // ============================================================================
        // 计时器对象池：复用 Countdown 实例，避免频繁分配
        // ============================================================================
        public async UniTask PooledCountdownExampleAsync()
        {
            // 从池里取一个（池空就返回 null）
            var cd = TimerPool.Get<Countdown>();
            if (cd == null)
            {
                // 第一次没缓存，新创建一个
                cd = TimerFactory.Countdown(2f);
            }

            cd.Launch();
            await cd.WaitAsync();

            // 用完了还回池；Return 会先 Stop 并清理状态
            TimerPool.Return(cd);
        }

        // ============================================================================
        // 全局控制：暂停 / 恢复 / 停止所有计时器
        // ============================================================================
        public void GlobalControls()
        {
            // 暂停所有（玩家切到暂停菜单时）
            TimerScheduler.Instance.PauseAll();

            // 全部恢复
            TimerScheduler.Instance.ResumeAll();

            // 全部停止并触发 OnStop（场景销毁时用）
            TimerScheduler.Instance.StopAllAndRemove();

            // 仅清空调度器引用，不触发 OnStop（极端情况，比如要重新注册同一计时器）
            // TimerScheduler.Instance.RemoveAll();
        }

        // ============================================================================
        // 下一帧延迟（替代 yield return null）
        // ============================================================================
        public async UniTask NextFrameAsync()
        {
            var t = TimerFactory.NextFrame(() => Debug.Log("下一帧"));
            t.Launch();
            await t.WaitAsync();
        }

        // ============================================================================
        // 闹钟：每天某个时刻触发
        // ============================================================================
        public void AlarmExample()
        {
            var now = System.DateTime.Now;
            // 下一个整点
            var alarm = TimerFactory.Alarm(
                hour: now.Hour,
                minute: (now.Minute + 1) % 60,
                second: 0,
                callback: () => Debug.Log("整点报时！"));
            alarm.Launch();
        }

        // ============================================================================
        // 适配没有 UniTask 的项目：UniTask <-> Task 桥接
        // ============================================================================
        public async System.Threading.Tasks.Task TimerWithTaskAsync()
        {
            var t = TimerFactory.Countdown(2f);
            t.Launch();
            await t.WaitAsync().AsTask();
        }
    }
}
