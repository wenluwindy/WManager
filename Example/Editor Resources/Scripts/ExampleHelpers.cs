// ----------------------------------------------------------------------------
// ExampleHelpers.cs
//
// 示例项目共用的辅助方法。
// ----------------------------------------------------------------------------
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using WManager;

namespace WManager.Example
{
    /// <summary>
    /// 提供与 Unity 版本兼容的"销毁令牌"获取方式：
    /// Unity 2022.2 以上自带 <c>MonoBehaviour.destroyCancellationToken</c>；
    /// 2021 用 UniTask 的 <c>GetCancellationTokenOnDestroy()</c>。
    /// 这里统一封装成 <see cref="GetDestroyToken"/>，让示例代码在两个版本都能编译。
    /// </summary>
    public static class ExampleHelpers
    {
        public static CancellationToken GetDestroyToken(this MonoBehaviour self)
        {
#if UNITY_2022_2_OR_NEWER
            return self.destroyCancellationToken;
#else
            return self.GetCancellationTokenOnDestroy();
#endif
        }
    }
}
