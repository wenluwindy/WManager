using System;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace WManager
{
    /// <summary>
    /// HTTP 访问器。
    /// 特点：
    /// - 基于 UniTask 异步，支持并发请求（不再用 IsBusy 锁）
    /// - 支持失败自动重试（带间隔）
    /// - 通过 CancellationToken 可取消单个请求
    /// 继承 SingletonBehaviour 统一单例生命周期（仅用于保留 MonoBehaviour 宿主；本类无实例字段）。
    /// </summary>
    public class WebRequest : SingletonBehaviour<WebRequest>, IHttpClient
    {
        /// <summary>
        /// HTTP 调用失败后的最大重试次数。设为 0 禁用重试。
        /// </summary>
        public static int HttpRetry { get; set; } = 5;

        /// <summary>
        /// 两次重试之间的间隔（秒）。
        /// </summary>
        public static float HttpRetryInterval { get; set; } = 0.5f;

        /// <summary>
        /// 单次请求的超时时间（秒）。
        /// </summary>
        public static int HttpTimeout { get; set; } = 5;

        /// <summary>
        /// Post 请求默认 Content-Type。
        /// </summary>
        public static string PostContentType = "application/json";

        /// <summary>
        /// GET 请求
        /// </summary>
        public UniTask<HttpCallBackArgs> GetAsync(string url, CancellationToken cancellationToken = default)
        {
            return SendWithRetryAsync(url, UnityWebRequest.kHttpVerbGET, null, null, cancellationToken);
        }

        /// <summary>
        /// POST 请求（byte[] 形式）
        /// </summary>
        public UniTask<HttpCallBackArgs> PostAsync(
            string url,
            byte[] data,
            string contentType = null,
            CancellationToken cancellationToken = default)
        {
            return SendWithRetryAsync(url, UnityWebRequest.kHttpVerbPOST, data, contentType, cancellationToken);
        }

        /// <summary>
        /// POST 请求（json 字符串形式）。
        /// 直接发送原始 JSON，不会再用 dict 包一层 value 字段。
        /// </summary>
        public UniTask<HttpCallBackArgs> PostJsonAsync(
            string url,
            string json,
            CancellationToken cancellationToken = default)
        {
            return PostAsync(url, Encoding.UTF8.GetBytes(json ?? string.Empty), PostContentType, cancellationToken);
        }

        /// <summary>
        /// 通用请求方法（带重试）
        /// </summary>
        private async UniTask<HttpCallBackArgs> SendWithRetryAsync(
            string url,
            string method,
            byte[] body,
            string contentType,
            CancellationToken cancellationToken)
        {
            var args = new HttpCallBackArgs();
            int retry = 0;

            while (true)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    args.HasError = true;
                    args.Value = "请求已取消";
                    return args;
                }

                UnityWebRequest req = null;
                try
                {
                    req = BuildRequest(url, method, body, contentType);
                    req.timeout = HttpTimeout;
                    await req.SendWebRequest().WithCancellation(cancellationToken);

                    if (req.result == UnityWebRequest.Result.Success)
                    {
                        args.HasError = false;
                        args.Value = req.downloadHandler.text;
                        args.Data = req.downloadHandler.data;
                        return args;
                    }

                    // 失败分支
                    if (retry >= HttpRetry)
                    {
                        args.HasError = true;
                        args.Value = req.error;
                        return args;
                    }

                    retry++;
                    if (HttpRetryInterval > 0)
                        await UniTask.Delay((int)(HttpRetryInterval * 1000), cancellationToken: cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    args.HasError = true;
                    args.Value = "请求已取消";
                    return args;
                }
                catch (Exception e)
                {
                    if (retry >= HttpRetry)
                    {
                        args.HasError = true;
                        args.Value = e.Message;
                        return args;
                    }
                    retry++;
                }
                finally
                {
                    req?.Dispose();
                }
            }
        }

        private static UnityWebRequest BuildRequest(string url, string method, byte[] body, string contentType)
        {
            var req = new UnityWebRequest(url, method)
            {
                downloadHandler = new DownloadHandlerBuffer()
            };
            if (body != null)
            {
                req.uploadHandler = new UploadHandlerRaw(body);
            }
            if (!string.IsNullOrWhiteSpace(contentType))
            {
                req.SetRequestHeader("Content-Type", contentType);
            }
            return req;
        }
    }
}