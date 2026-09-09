using System.Threading;
using Cysharp.Threading.Tasks;

namespace WManager
{
    /// <summary>
    /// HTTP 客户端抽象。业务模块通过此接口访问网络功能，不需要直接依赖 WebRequest。
    /// </summary>
    public interface IHttpClient
    {
        UniTask<HttpCallBackArgs> GetAsync(string url, CancellationToken cancellationToken = default);

        UniTask<HttpCallBackArgs> PostAsync(string url, byte[] data, string contentType = null, CancellationToken cancellationToken = default);

        UniTask<HttpCallBackArgs> PostJsonAsync(string url, string json, CancellationToken cancellationToken = default);
    }
}