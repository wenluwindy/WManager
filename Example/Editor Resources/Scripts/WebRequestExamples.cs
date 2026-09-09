// ----------------------------------------------------------------------------
// WebRequestExamples.cs
//
// 演示 WebRequest（基于 UnityWebRequest + UniTask）：
// - GetAsync / PostAsync / PostJsonAsync
// - 重试次数 / 重试间隔 / 单次超时（静态属性）
// - 通过 CancellationToken 取消请求
// - 用 IServiceLocator 注入 IHttpClient，让业务模块只依赖接口
// ----------------------------------------------------------------------------
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using WManager;

namespace WManager.Example
{
    /// <summary>
    /// 业务侧只需要 IHttpClient，不直接依赖 WebRequest。
    /// 把 IHttpClient 注册到 IServiceLocator 之后，任意位置 Require&lt;IHttpClient&gt;() 即可。
    /// </summary>
    public class WebRequestExample : MonoBehaviour
    {
        // 启动期注入一次（一般在 Bootstrap 里）
        public static void RegisterHttpClient()
        {
            ServiceLocatorExample.Locator.Register<IHttpClient>(WebRequest.Instance);
        }

        private async void Start()
        {
            Debug.Log("[WebRequest] ====== 开始初始化 WebRequest 示例 ======");
            
            // 调整全局重试和超时（按需）
            WebRequest.HttpRetry = 3;
            WebRequest.HttpRetryInterval = 1f;
            WebRequest.HttpTimeout = 10;
            
            Debug.Log($"[WebRequest] 全局配置 - 重试次数: {WebRequest.HttpRetry}, 重试间隔: {WebRequest.HttpRetryInterval}s, 超时: {WebRequest.HttpTimeout}s");

            try
            {
                Debug.Log("[WebRequest] >>> 步骤 1/4: 开始执行 GET 请求示例");
                await GetExampleAsync();
                Debug.Log("[WebRequest] <<< 步骤 1/4: GET 请求示例完成");
                
                Debug.Log("[WebRequest] >>> 步骤 2/4: 开始执行 POST JSON 请求示例");
                await PostJsonExampleAsync();
                Debug.Log("[WebRequest] <<< 步骤 2/4: POST JSON 请求示例完成");
                
                Debug.Log("[WebRequest] >>> 步骤 3/4: 开始执行 IServiceLocator 调用示例");
                await UseFromLocatorAsync();
                Debug.Log("[WebRequest] <<< 步骤 3/4: IServiceLocator 调用示例完成");
                
                Debug.Log("[WebRequest] >>> 步骤 4/4: 开始执行下载示例（将等待 10 秒）");
                await StartDownload();
                Debug.Log("[WebRequest] <<< 步骤 4/4: 下载示例完成");
                
                Debug.Log("[WebRequest] ====== 所有 WebRequest 示例执行完毕 ======");
            }
            catch (OperationCanceledException)
            {
                Debug.LogWarning("[WebRequest] ⚠ 请求被取消（物体销毁时正常取消）");
            }
            catch (Exception e)
            {
                Debug.LogError($"[WebRequest] ❌ 失败：{e.Message}\n堆栈：{e.StackTrace}");
            }
        }

        // ============================================================================
        // GET 请求
        // ============================================================================
        private async UniTask GetExampleAsync(CancellationToken ct = default)
        {
            string url = "https://httpbin.org/get?foo=bar";
            Debug.Log($"[WebRequest.GET] 准备发起请求 -> URL: {url}");
            
            var startTime = Time.realtimeSinceStartup;
            var response = await WebRequest.Instance.GetAsync(url, ct);
            var elapsed = Time.realtimeSinceStartup - startTime;
            
            Debug.Log($"[WebRequest.GET] 请求完成 - 耗时: {elapsed:F2}s, URL: {url}");

            if (response.HasError)
            {
                Debug.LogError($"[WebRequest.GET] ❌ 失败 - URL: {url}, 错误: {response.Value}");
                return;
            }

            Debug.Log($"[WebRequest.GET] ✅ 成功 - 响应长度: {response.Value.Length} 字符");
            Debug.Log($"[WebRequest.GET] 响应预览: {response.Value.Substring(0, Mathf.Min(120, response.Value.Length))}...");
            // response.Data 是原始字节
        }

        // ============================================================================
        // POST JSON（直接发原始 JSON，不会再用 dict 包一层 value）
        // ============================================================================
        private async UniTask PostJsonExampleAsync(CancellationToken ct = default)
        {
            string url = "https://httpbin.org/post";

            // 直接传 JSON 字符串
            string json = "{\"name\":\"Albert\",\"level\":12}";
            Debug.Log($"[WebRequest.POST] 准备发起请求 -> URL: {url}");
            Debug.Log($"[WebRequest.POST] 请求体: {json}");
            
            var startTime = Time.realtimeSinceStartup;
            var response = await WebRequest.Instance.PostJsonAsync(url, json, ct);
            var elapsed = Time.realtimeSinceStartup - startTime;
            
            Debug.Log($"[WebRequest.POST] 请求完成 - 耗时: {elapsed:F2}s, URL: {url}");

            if (response.HasError)
            {
                Debug.LogError($"[WebRequest.POST] ❌ 失败 - URL: {url}, 错误: {response.Value}");
                return;
            }

            Debug.Log($"[WebRequest.POST] ✅ 成功 - 响应长度: {response.Value.Length} 字符");
            Debug.Log($"[WebRequest.POST] 响应预览: {response.Value.Substring(0, Mathf.Min(120, response.Value.Length))}...");
        }

        // ============================================================================
        // 通过 IServiceLocator 拿 IHttpClient 调用：业务模块推荐写法
        // ============================================================================
        private async UniTask UseFromLocatorAsync(CancellationToken ct = default)
        {
            Debug.Log("[WebRequest.Locator] 准备通过 IServiceLocator 解析 IHttpClient");
            
            // 把 WebRequest 注册为 IHttpClient（Bootstrap 阶段一次性注册即可）
            IHttpClient client = ServiceLocatorExample.Locator.Resolve<IHttpClient>();
            if (client == null)
            {
                Debug.LogWarning("[WebRequest.Locator] ⚠ IHttpClient 未注册，先调用 WebRequestExample.RegisterHttpClient()");
                return;
            }
            
            Debug.Log($"[WebRequest.Locator] ✅ 成功解析 IHttpClient -> 类型: {client.GetType().Name}");

            string url = "https://httpbin.org/uuid";
            Debug.Log($"[WebRequest.Locator] 准备发起请求 -> URL: {url}");
            
            var startTime = Time.realtimeSinceStartup;
            var response = await client.GetAsync(url, ct);
            var elapsed = Time.realtimeSinceStartup - startTime;
            
            Debug.Log($"[WebRequest.Locator] 请求完成 - 耗时: {elapsed:F2}s, URL: {url}");
            
            if (response.HasError)
            {
                Debug.LogError($"[WebRequest.Locator] ❌ 失败 - URL: {url}, 错误: {response.Value}");
                return;
            }

            Debug.Log($"[WebRequest.Locator] ✅ 成功 - 响应: {response.Value}");
        }

        // ============================================================================
        // 取消示例：玩家点"取消下载"按钮
        // ============================================================================
        private CancellationTokenSource _downloadCts;

        public async UniTask StartDownload()
        {
            _downloadCts = new CancellationTokenSource();
            string url = "https://httpbin.org/delay/10";
            
            Debug.Log($"[WebRequest.Download] 开始下载 -> URL: {url}");
            Debug.Log($"[WebRequest.Download] 提示: 此请求将等待 10 秒，可通过 CancelDownload() 取消");
            
            var startTime = Time.realtimeSinceStartup;
            try
            {
                var response = await WebRequest.Instance.GetAsync(url, _downloadCts.Token);
                var elapsed = Time.realtimeSinceStartup - startTime;
                
                if (response.HasError)
                {
                    Debug.LogError($"[WebRequest.Download] ❌ 失败 - 耗时: {elapsed:F2}s, 错误: {response.Value}");
                }
                else
                {
                    Debug.Log($"[WebRequest.Download] ✅ 成功 - 耗时: {elapsed:F2}s");
                }
            }
            catch (OperationCanceledException)
            {
                var elapsed = Time.realtimeSinceStartup - startTime;
                Debug.Log($"[WebRequest.Download] ⚠ 下载已取消 - 已耗时: {elapsed:F2}s");
            }
        }

        public void CancelDownload()
        {
            if (_downloadCts != null && !_downloadCts.IsCancellationRequested)
            {
                Debug.Log("[WebRequest.Download] 收到取消请求，正在取消下载...");
                _downloadCts.Cancel();
            }
            _downloadCts?.Dispose();
            _downloadCts = null;
        }

        private void OnDestroy()
        {
            CancelDownload();
        }
    }
}