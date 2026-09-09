# WebRequest 使用说明

> 基于 `Assets/Scripts/WManager/WebRequest/WebRequest.cs` 当前源码。
> 基于 UniTask 的 HTTP 客户端：支持 GET、POST (byte[])、POST JSON；内置失败重试、超时与 `CancellationToken` 取消；实现 `IHttpClient`。

## 1. 能力概览

- 单例：`WebRequest.Instance`（继承 `SingletonBehaviour<WebRequest>`）。
- 异步返回 `UniTask<HttpCallBackArgs>`，内部使用 `UnityWebRequest`。
- 并发请求互不阻塞。
- 失败重试：`HttpRetry` + `HttpRetryInterval`，超过重试次数返回错误。
- 超时：`HttpTimeout`（秒）。
- 取消：`CancellationToken`，会立即停止等待并返回错误。
- `PostJsonAsync` 直接发送原始 JSON 字符串，不再额外包一层 value。

## 2. 基础用法

```csharp
// GET
var args = await WebRequest.Instance.GetAsync("https://example.com/config.json");
if (!args.HasError) Debug.Log(args.Value);
else Debug.LogError(args.Value);

// POST byte[]
byte[] body = Encoding.UTF8.GetBytes("name=Albert");
var post = await WebRequest.Instance.PostAsync("https://example.com/api", body);

// POST JSON
var json = await WebRequest.Instance.PostJsonAsync(
    "https://example.com/api",
    "{\"name\":\"Albert\"}");
```

## 3. HttpCallBackArgs

| 字段 | 类型 | 说明 |
|---|---|---|
| `HasError` | bool | 是否失败 |
| `Value` | string | 响应文本或错误信息 |
| `Data` | byte[] | 响应原始字节 |

`HttpSendDataCallBack` 委托保留以兼容旧代码，新代码建议直接 `await` 异步方法。

## 4. 取消令牌

```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
var result = await WebRequest.Instance.GetAsync(url, cts.Token);
```

- 取消触发后会立刻返回 `HttpCallBackArgs { HasError = true, Value = "请求已取消" }`。
- `SendWebRequest().WithCancellation(token)` 会在取消时抛出 `OperationCanceledException`，并被捕获。

## 5. 配置

```csharp
WebRequest.HttpRetry         = 3;    // 失败重试次数，0 禁用
WebRequest.HttpRetryInterval = 1.0f; // 重试间隔（秒）
WebRequest.HttpTimeout       = 10;   // 单次超时（秒）
WebRequest.PostContentType   = "application/json";
```

## 6. 重试与错误语义

- 成功：`HasError = false`，`Value = downloadHandler.text`，`Data = downloadHandler.data`。
- 失败且超过重试：`HasError = true`，`Value = req.error` 或异常消息。
- 取消：`HasError = true`，`Value = "请求已取消"`。
- 抛出的 `OperationCanceledException` 会被吞掉，统一以错误返回。

## 7. IHttpClient 接口

业务侧可通过 `IServiceLocator` 注册 `IHttpClient`，运行时拿到 `WebRequest` 实现，便于替换测试桩：

```csharp
var client = WebRequest.Instance; // 已是 IHttpClient
services.Register<IHttpClient>(client);
```

`IHttpClient` 签名与 `WebRequest` 一致：`GetAsync` / `PostAsync` / `PostJsonAsync`。

## 8. 注意事项

- 单例会自动创建 GameObject 并 `DontDestroyOnLoad`，不依赖场景挂载。
- HTTPS、自定义 Header、Cookie 等可由业务侧自行扩展；本类不封装。
- `req.Dispose()` 在 `finally` 中调用，不需要业务侧额外处理。
- `PostContentType` 默认为 `application/json`，非 JSON 时请调用 `PostAsync` 并显式传 `contentType`。
- 错误信息直接来自 `UnityWebRequest.error`，可能因平台/服务器不同而异。
