# 纹路风 » WebRequest 使用说明

这个 WebRequest 脚本提供了在 Unity 中简单发送 HTTP GET 和 POST 请求的功能。

## 使用方法

要使用 WebRequest,首先获取单例实例:

```csharp
WebRequest request = WebRequest.Instance;
```

然后调用`Get()`来发送 GET 请求:

```csharp
request.Get("http://example.com", OnRequestFinished);
```

或者调用`Post()`来发送 POST 请求:

```csharp
byte[] postData = System.Text.Encoding.UTF8.GetBytes("name=value");
request.Post("http://example.com", postData, OnRequestFinished);
```

你可以传一个回调函数来处理响应:

```csharp
private void OnRequestFinished(HttpCallBackArgs args)
{
  if(!args.HasError) {
    // 请求成功
    string response = args.Value;
  }
  else {
    // 请求失败
    string error = args.Value;
  }
}
```

`HttpCallBackArgs`包含了响应数据或任何错误信息的属性。

要取消一个请求,调用`Cancel()`。

## 配置

有几个静态属性可以配置请求的行为:

- `HttpRetry` - 失败后重试次数
- `HttpRetryInterval` - 重试间隔时间(秒)
- `PostContentType` - POST 请求的 Content-Type 头

## 注意事项

- 请求是异步的。
- 脚本会处理如果正在进行中则排队请求。
- 可以配置失败后重试。
- 简单的接口用于 Unity 中的 HTTP 调用。
