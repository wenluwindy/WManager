// ----------------------------------------------------------------------------
// ExtensionExamples.cs
//
// 演示 Extension 模块的扩展方法（String / Array / List / Dictionary / Bool / Class）。
// 这些都是真实的框架 API，命名严格对应源码里的方法名。
// ----------------------------------------------------------------------------
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using WManager;

namespace WManager.Example
{
    public class ExtensionExample : MonoBehaviour
    {
        private void Start()
        {
            ExtensionDemosAsync().Forget();
        }
        async UniTask ExtensionDemosAsync()
        {
            StringDemos();
            await UniTask.Delay(1000);
            ArrayDemos();
            await UniTask.Delay(1000);
            ListDemos();
            await UniTask.Delay(1000);
            DictionaryDemos();
            await UniTask.Delay(1000);
            BoolDemos();
            await UniTask.Delay(1000);
            ClassDemos();
        }

        // ============================================================================
        // StringExtension
        // ============================================================================
        private void StringDemos()
        {
            string s = "Hello,World,Foo,Bar";
            Debug.Log($"[stringExt] 原始字符串 = {s}");

            // 按分隔符拆分（保留空行处理）
            var parts = s.SplitString(',');
            Debug.Log($"[stringExt] SplitString 结果数 = {parts.Count}");

            // bool / 数字 转换
            Debug.Log($"[stringExt] ToBool = {"true".ToBool()}");
            Debug.Log($"[stringExt] ToInt  = {"42".ToInt()}");
            Debug.Log($"[stringExt] ToLong = {"9999999999".ToLong()}");

            // MD5
            Debug.Log($"[stringExt] MD5(\"hello\") 32 = {"hello".ToMD5String()}");
            Debug.Log($"[stringExt] MD5(\"hello\") 16 = {"hello".ToMD5String16()}");

            // SHA1 / SHA256
            Debug.Log($"[stringExt] SHA1  = {"hello".ToSHA1String()}");
            Debug.Log($"[stringExt] SHA256= {"hello".ToSHA256String()}");

            // Base64（实际方法名是 Base64Encode / Base64Decode）
            string b64 = "你好".Base64Encode();
            Debug.Log($"[stringExt] Base64 round-trip = {b64.Base64Decode()}");

            // 首字母大小写
            Debug.Log($"[stringExt] ToFirstUpperStr(\"albert\") = {"albert".ToFirstUpperStr()}");
            Debug.Log($"[stringExt] ToFirstLowerStr(\"ALBERT\") = {"ALBERT".ToFirstLowerStr()}");

            // 枚举
            Debug.Log($"[stringExt] \"Running\" -> UIOrientation = (跳过，仅作示例)");

            // AES（用于自己手动加解密字符串；存档系统内部就用 SaveEncryption）
            // 新版 API：任意长度密钥都可以（内部用 SHA256 派生为 32 字节 AES-256）
            string cipher = "Hello".AESEncrypt("MySecretKey");   // 任意长度都行
            string plain = cipher.AESDecrypt("MySecretKey");
            Debug.Log($"[stringExt] AES round-trip = {plain}");

            // 随机字符串
            string r = StringExtension.RandomString(12);
            Debug.Log($"[stringExt] RandomString(12) = {r}");
        }

        // ============================================================================
        // ArrayExtension：注意只有 int[] 有排序扩展，其他 T[] 用 .Sort()
        // ============================================================================
        private void ArrayDemos()
        {
            int[] arr = { 1, 2, 3, 4, 5 };
            Debug.Log($"[ArrayExt] 原始数组 = [{string.Join(",", arr)}]");

            // ForEach：带 index
            arr.ForEach((i, v) => Debug.Log($"[ArrayExt] arr[{i}] = {v}"));

            // 倒序遍历（带/不带 index 都有重载）
            arr.ForEachReverse(v => Debug.Log($"[ArrayExt] reverse: {v}"));
            arr.ForEachReverse((i, v) => Debug.Log($"[ArrayExt] reverse [{i}] = {v}"));

            // 合并
            int[] merged = arr.Merge(new[] { 6, 7 });
            Debug.Log($"[ArrayExt] merged length = {merged.Length}");

            // int[] 排序扩展（冒泡 / 选择 / 插入 / 希尔）
            int[] unsorted = { 5, 2, 8, 1, 4 };
            unsorted.SortBubble();
            Debug.Log($"[ArrayExt] SortBubble -> [{string.Join(",", unsorted)}]");
        }

        // ============================================================================
        // ListExtension
        // ============================================================================
        private void ListDemos()
        {
            var list = new List<int> { 1, 2, 3 };
            Debug.Log($"[ListExt] 原始列表 = [{string.Join(",", list)}]");

            // ForEach（带/不带 index）
            list.ForEach(v => Debug.Log($"[ListExt] list v = {v}"));
            list.ForEach((i, v) => Debug.Log($"[ListExt] list [{i}] = {v}"));

            // TryAdd / AddIfNotContains：不重复才加入（两个 API 等价）
            list.TryAdd(3);     // 不加
            list.TryAdd(4);     // 加
            list.AddIfNotContains(5);
            Debug.Log($"[ListExt] TryAdd/AddIfNotContains 后 count = {list.Count}");

            // 倒序遍历
            list.ForEachReverse(v => Debug.Log($"[ListExt] reverse v = {v}"));
        }

        // ============================================================================
        // DictionaryExtension
        // ============================================================================
        private void DictionaryDemos()
        {
            var dict = new Dictionary<string, int>
            {
                ["hp"] = 100,
                ["mp"] = 50,
            };
            Debug.Log($"[DictionaryExt] 原始字典 = [{string.Join(",", dict)}]");

            // ForEach（带 key/value）
            dict.ForEach((k, v) => Debug.Log($"[DictionaryExt] {k} = {v}"));

            // AddRange / 合并字典
            // 注意：target 中若已存在 key，默认不覆盖；传 isOverride=true 覆盖
            dict.AddRange(new Dictionary<string, int>
            {
                ["hp"] = 999,  // 已存在，不会覆盖
                ["atk"] = 20,  // 新增
            }, isOverride: false);

            Debug.Log($"[DictionaryExt] merged hp = {dict["hp"]} (未覆盖), atk = {dict["atk"]}");
        }

        // ============================================================================
        // BoolExtension：如果为 true 则执行 action
        // ============================================================================
        private void BoolDemos()
        {
            bool isLogin = true;
            Debug.Log($"[BoolExt] 原始布尔值 = {isLogin}");

            // 单个 action
            isLogin.Execute(() => Debug.Log("[BoolExt] 登录了"));
            isLogin.Execute(loggedIn => Debug.Log($"[BoolExt] loggedIn = {loggedIn}"));

            // 三元 action：true 一个、false 一个
            bool isVip = false;
            isVip.Execute(
                () => Debug.Log("[BoolExt] 是 VIP"),
                () => Debug.Log("[BoolExt] 不是 VIP"));
        }

        // ============================================================================
        // ClassExtension：对象不为 null 时执行 action
        // ============================================================================
        private void ClassDemos()
        {
            string maybeNull = null;
            string notNull = "hello";

            maybeNull.Execute(s => Debug.Log($"[ClassExt] maybeNull: {s}"));    // 不执行
            notNull.Execute(s => Debug.Log($"[ClassExt] notNull: {s}"));        // 执行
        }
    }
}
