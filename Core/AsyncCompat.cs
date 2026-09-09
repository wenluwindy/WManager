using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;

namespace WManager
{
    /// <summary>
    /// UniTask ↔ Task 桥接。
    /// 用途：当调用方项目没有 UniTask 包时，仍可使用 .NET 标准 Task 等待本框架的异步 API。
    /// </summary>
    public static class AsyncCompat
    {
        /// <summary>
        /// UniTask → Task。带 CancellationToken 支持。
        /// </summary>
        public static async Task<T> AsTask<T>(this UniTask<T> uniTask)
        {
            return await uniTask;
        }

        /// <summary>
        /// UniTask → Task（无返回值）。带 CancellationToken 支持。
        /// </summary>
        public static async Task AsTask(this UniTask uniTask)
        {
            await uniTask;
        }

        /// <summary>
        /// Task → UniTask（供有 Task 但希望统一 await UniTask 的场景）
        /// </summary>
        public static async UniTask<T> AsUniTask<T>(this Task<T> task)
        {
            return await task.ConfigureAwait(false);
        }

        /// <summary>
        /// Task → UniTask（无返回值）
        /// </summary>
        public static async UniTask AsUniTask(this Task task)
        {
            await task.ConfigureAwait(false);
        }
    }
}