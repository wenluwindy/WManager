# 纹路风 » Extension 常用函数扩展使用说明

## 简介

使用**this**关键字给一些类型做的拓展函数,为了支持链式编程或记录、封装了一些功能。

## Array

```csharp
using UnityEngine;
using WManager;

public class Example : MonoBehaviour
{
  private void Start()
  {
    string[] exampleArray = new string[] { "AAA", "BBB" };
    //遍历
    exampleArray.ForEach((i, s) => Debug.Log(string.Format("{0}.{1}", i + 1, s)));
    //倒序遍历
    exampleArray.ForEachReverse(m => Debug.Log(m));
    //倒序遍历
    exampleArray.ForEachReverse((i, s) => Debug.Log(string.Format("{0}.{1}", i + 1, s)));

    //Array合并
    string[] target = new string[] { "CCC", "DDD" };
    string[] merge = exampleArray.Merge(target);

    int[] intArray = new int[] { 55, 32, 57, 89, 13, 87 , 9, 21};
    //希尔排序
    intArray.SortInsertion();
    //选择排序
    intArray.SortSelection();
    //冒泡排序
    intArray.SortBubble();
    intArray.ForEach((i, s) => Debug.Log(string.Format("{0}.{1}", i + 1, s)));
  }
}
```

## bool

```csharp
// 如果bool值为true 则执行事件
//如果flag为true 则会打印日志true
flag.Execute(() => Debug.Log("true"));
// 根据bool值执行Action<bool>类型事件
//如果flag为true 则会打印日志true 否则打印日志false
flag.Execute(isTrue => Debug.Log(isTrue));
// bool值为true则执行第一个事件 否则执行第二个事件
//如果flag为true 则会打印日志true 否则打印日志false
flag.Execute(() => Debug.Log("true"), () => Debug.Log("false"));
```

## Class

```csharp
using UnityEngine;
using WManager;

public class Example : MonoBehaviour
{
  public class Person
  {
    public string name;
  }

  private Person person;

  private void Start()
  {
    //如果对象不为null则执行事件
    person.Execute(m => Debug.Log(m.name));
  }
}
```

## Dictionary

```csharp
Dictionary<int, string> dic = new Dictionary<int, string>() { { 5, "AAA" }, { 10, "BBB" } };

//遍历字典
dic.ForEach(m => Debug.Log(string.Format("Key{0} Value{1}", m.Key, m.Value)));

Dictionary<int, string> target = new Dictionary<int, string>() { { 11, "CCC" }, { 20, "DDD" } };

//合并字典
dic.AddRange(target);

//将字典的所有值放入到一个列表中
List<string> list = dic.Values.ToList();

//将字典的所有值放入到一个Array中
string[] array = dic.Values.ToArray();

//尝试添加
if (dic.TryAdd(20, "DDD")) Debug.Log("添加成功");
```

## List

```csharp
List<string> list = new List<string>() { "AAA", "BBB" };

//遍历
list.ForEach(m => Debug.Log(m));

//遍历
list.ForEach((i, m) => Debug.Log(string.Format("{0}.{1}", i + 1, m)));

//倒序遍历
list.ForEachReverse(m => Debug.Log(m));

//倒序遍历
list.ForEachReverse((i, m) => Debug.Log(string.Format("{0}.{1}", i + 1, m)));

//尝试添加
if (list.TryAdd("CCC")) Debug.Log("添加成功");
```

## Queue

```csharp
Queue<string> queue = new Queue<string>();

queue.Enqueue("A");
queue.Enqueue("b");
queue.Enqueue("e");
queue.Enqueue("g");

//遍历队列
queue.ForEach(m => Debug.Log(m));
```

## String

### 1. SplitString

- 描述:将输入的 string 内容按照指定字符分割,并返回分割后的列表

- 参数:

  - str:需要分割的字符串
  - separator:指定的分割节点字符

- 示例:

```csharp
var str = "abc\ndef\nghi";
var lines = str.SplitString('\n');
// lines = ["abc", "def", "ghi"]
```

### 2. ToBool

- 描述:字符串转 bool

- 参数:

  - str:字符串

- 返回值:bool

### 3. ToBytes_FromBase64Str

- 描述:base64 字符串转字节数组

- 参数:

  - base64Str:base64 字符串

- 返回值:字节数组

### 4. ToMD5String

- 描述:字符串转 MD5 加密后的字符串(默认 32 位)

- 参数:

  - str:字符串

- 返回值:MD5 加密后的字符串

### 5. ToMD5String16

- 描述:字符串转 MD5 加密后的字符串(16 位)

- 参数:
  - str:字符串
- 返回值:MD5 加密后的字符串(16 位)

### 6. Base64Encode

- 描述:Base64 加密,默认 UTF8 编码

- 参数:
  - source:待加密明文
- 返回值:加密后的字符串

### 7. Base64Encode

- 描述:Base64 加密

- 参数:
  - source:待加密明文
  - encoding:编码方式
- 返回值:加密后的字符串

### 8. Base64Decode

- 描述:Base64 解密,默认 UTF8 编码

- 参数:
  - result:待解密密文
- 返回值:解密后的字符串

### 9. Base64Decode

- 描述:Base64 解密

- 参数:
  - result:待解密密文
  - encoding:与加密时相同的编码方式
- 返回值:解密后的字符串

### 10. Base64UrlEncode

- 描述:Base64Url 编码

- 参数:
  - text:待编码文本字符串
- 返回值:编码后的文本字符串

### 11. Base64UrlDecode

- 描述:Base64Url 解码

- 参数:
  - base64UrlStr:Base64Url 编码后的字符串
- 返回值:解码后的内容

### 12. ToSHA1Bytes

- 描述:计算 SHA1 摘要,默认 UTF8 编码

- 参数:
  - str:字符串
- 返回值:SHA1 摘要字节数组

### 13. ToSHA1Bytes

- 描述:计算 SHA1 摘要

- 参数:
  - str:字符串
  - encoding:编码
- 返回值:SHA1 摘要字节数组

### 14. ToSHA1String

- 描述:字符串转 SHA1 哈希,默认 UTF8 编码

- 参数:
  - str:字符串
- 返回值:SHA1 哈希字符串

### 15. ToSHA1String

- 描述:字符串转 SHA1 哈希

- 参数:
  - str:字符串
  - encoding:编码
- 返回值:SHA1 哈希字符串

### 16. ToSHA256String

- 描述:SHA256 加密

- 参数:
  - str:字符串
- 返回值:SHA256 加密后的字符串

### 17. ToHMACSHA256String

- 描述:HMACSHA256 算法

- 参数:
  - text:内容
  - secret:密钥
- 返回值:HMACSHA256 加密后的字符串

### 18. ToInt

- 描述:字符串转 int

- 参数:
  - str:字符串
- 返回值:int

### 19. ToLong

- 描述:字符串转 long

- 参数:
  - str:字符串
- 返回值:long

### 20. ToInt_FromBinString

- 描述:二进制字符串转 int

- 参数:
  - str:二进制字符串
- 返回值:int

### 21. ToInt0X

- 描述:16 进制字符串转 int

- 参数:
  - str:16 进制字符串
- 返回值:int

### 22. ToDouble

- 描述:字符串转 double

- 参数:
  - str:字符串
- 返回值:double

### 23. ToBytes

- 描述:字符串转 byte[]

- 参数:
  - str:字符串
- 返回值:字节数组

### 24. ToBytes

- 描述:字符串转 byte[],可指定编码

- 参数:
  - str:字符串
  - theEncoding:编码
- 返回值:字节数组

### 25. To0XBytes

- 描述:16 进制字符串转 Byte 数组

- 参数:
  - str:16 进制字符串
- 返回值:字节数组

### 26. ToASCIIBytes

- 描述:ASCII 字符串转字节数组

- 参数:
  - str:ASCII 字符串
- 返回值:字节数组

### 27. ToDateTime

- 描述:字符串转 DateTime

- 参数:
  - str:字符串
- 返回值:DateTime

### 28. RemoveAt

- 描述:删除 Json 字符串中键中的@符号

- 参数:
  - jsonStr:json 字符串
- 返回值:处理后的字符串

### 29. XmlStrToObject

- 描述:XML 字符串反序列化为对象

- 参数:
  - xmlStr:XML 字符串
- 返回值:对象

### 30. XmlStrToJObject

- 描述:XML 字符串转为 JObject

- 参数:
  - xmlStr:XML 字符串
- 返回值:JObject

### 31. ToList

- 描述:Json 字符串转 List

- 参数:
  - jsonStr:Json 字符串
- 返回值:List

### 32. ToDataTable

- 描述:Json 字符串转 DataTable

- 参数:
  - jsonStr:Json 字符串
- 返回值:DataTable

### 33. ToJObject

- 描述:Json 字符串转 JObject

- 参数:
  - jsonStr:Json 字符串
- 返回值:JObject

### 34. ToJArray

- 描述:Json 字符串转 JArray

- 参数:
  - jsonStr:Json 字符串
- 返回值:JArray

### 35. ToEntity

- 描述:json 数据转实体类,仅用于单个实体类,速度很快

- 参数:
  - json:json 字符串
- 返回值:实体类对象

### 36. ToFirstUpperStr

- 描述:字符串首字母转大写

- 参数:
  - str:字符串
- 返回值:首字母大写的字符串

### 37. ToFirstLowerStr

- 描述:字符串首字母转小写

- 参数:
  - str:字符串
- 返回值:首字母小写的字符串

### 38. ToEnum

- 描述:枚举文本转枚举值

- 参数:
  - enumText:枚举文本
- 返回值:枚举值

### 39. IsWeakPwd

- 描述:判断是否为弱密码

- 参数:
  - pwd:密码
- 返回值:bool

### 40. AESEncrypt

- 描述:AES 加密

- 参数:
  - text:待加密文本
  - EncryptionKey:密钥
- 返回值:加密后的密文

### 41. AESDecrypt

- 描述:AES 解密

- 参数:
  - cipherText:密文
  - EncryptionKey:密钥
- 返回值:解密后的明文

### 42. GetMacAddress

- 描述:获取 MAC 地址

- 返回值:MAC 地址字符串

### 43. RandomString

- 描述:生成随机字符串

- 参数:
  - length:字符串长度
- 返回值:随机字符串
