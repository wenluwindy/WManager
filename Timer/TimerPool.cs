using System.Collections.Generic;

namespace WManager
{
    /// <summary>
    /// 计时器对象池：缓存已停止的计时器实例，避免频繁创建。
    /// 用法：调用 Get&lt;Countdown&gt;() 取得（如果有）缓存实例，调用后 Reset() 还回池。
    /// 注意：业务层应负责在外部持有引用 + 调 Reset 归还，本类不做自动回收。
    /// </summary>
    public static class TimerPool
    {
        // 每种计时器类型一个栈
        private static readonly Dictionary<System.Type, Stack<ITimer>> _pools = new Dictionary<System.Type, Stack<ITimer>>();

        /// <summary>
        /// 从池中取出一个实例。若池为空，返回 null（由调用方决定是否新建）。
        /// </summary>
        public static T Get<T>() where T : ITimer
        {
            if (_pools.TryGetValue(typeof(T), out var stack) && stack.Count > 0)
            {
                return (T)stack.Pop();
            }
            return default;
        }

        /// <summary>
        /// 把计时器实例还回池。会在内部调用 Stop() 确保状态干净。
        /// </summary>
        public static void Return<T>(T timer) where T : ITimer
        {
            if (timer == null) return;
            timer.Stop();

            if (!_pools.TryGetValue(typeof(T), out var stack))
            {
                stack = new Stack<ITimer>();
                _pools[typeof(T)] = stack;
            }
            stack.Push(timer);
        }

        /// <summary>
        /// 清空所有池
        /// </summary>
        public static void ClearAll()
        {
            _pools.Clear();
        }

        /// <summary>
        /// 当前池大小（用于监控/调试）
        /// </summary>
        public static int GetPoolSize<T>() where T : ITimer
        {
            if (_pools.TryGetValue(typeof(T), out var stack))
                return stack.Count;
            return 0;
        }
    }
}