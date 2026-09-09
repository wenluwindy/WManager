using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace WManager
{
    public static class StringExtension
    {
        /// <summary>
        /// 将输入的string内容按照指定字符分割，并返回分割后的列表，strArray:需要分割的字符串 separator：指定的分割节点字符 举例格式 '\n'
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static List<string> SplitString(this string strArray, char separator)  //分割字符串的方法
        {
            List<string> lines = new List<string>();   //创建列表以存储输出行
            string currentLine = "";                 //初始化当前行为空

            for (int i = 0; i < strArray.Length; i++)     //遍历每个字符
            {
                if (strArray[i] == separator)                  //如果当前字符是换行符
                {
                    lines.Add(currentLine);         //将当前行添加到列表
                    currentLine = "";               //清空当前行  
                }
                else
                {
                    currentLine += strArray[i];         //否则将字符追加到当前行
                }
            }

            if (currentLine != "") lines.Add(currentLine);   //如果最后一行不为空也加入列表
            return lines;                                  //返回行列表
        }
        /// <summary>
        /// 1.转为bool
        /// </summary>
        /// <param name="str">字符串</param>
        /// <returns></returns>
        public static bool ToBool(this string str)
        {
            return bool.Parse(str);
        }
        /// <summary>
        /// 2.转为字节数组
        /// </summary>
        /// <param name="base64Str">base64字符串</param>
        /// <returns></returns>
        public static byte[] ToBytesFromBase64Str(this string base64Str)
        {
            return Convert.FromBase64String(base64Str);
        }
        /// <summary>
        /// 3.转换为MD5加密后的字符串（默认加密为32位）
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string ToMD5String(this string str)
        {
            MD5 md5 = MD5.Create();
            byte[] inputBytes = Encoding.UTF8.GetBytes(str);
            byte[] hashBytes = md5.ComputeHash(inputBytes);

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hashBytes.Length; i++)
            {
                sb.Append(hashBytes[i].ToString("x2"));
            }
            md5.Dispose();

            return sb.ToString();
        }

        /// <summary>
        /// 4.转换为MD5加密后的字符串（16位）
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string ToMD5String16(this string str)
        {
            return str.ToMD5String().Substring(8, 16);
        }

        /// <summary>
        /// 5.Base64加密
        /// 注:默认采用UTF8编码
        /// </summary>
        /// <param name="source">待加密的明文</param>
        /// <returns>加密后的字符串</returns>
        public static string Base64Encode(this string source)
        {
            return Base64Encode(source, Encoding.UTF8);
        }

        /// <summary>
        /// 6.Base64加密
        /// </summary>
        /// <param name="source">待加密的明文</param>
        /// <param name="encoding">加密采用的编码方式</param>
        /// <returns></returns>
        public static string Base64Encode(this string source, Encoding encoding)
        {
            string encode = string.Empty;
            byte[] bytes = encoding.GetBytes(source);
            try
            {
                encode = Convert.ToBase64String(bytes);
            }
            catch
            {
                encode = source;
            }
            return encode;
        }

        /// <summary>
        /// 7.Base64解密
        /// 注:默认使用UTF8编码
        /// </summary>
        /// <param name="result">待解密的密文</param>
        /// <returns>解密后的字符串</returns>
        public static string Base64Decode(this string result)
        {
            return Base64Decode(result, Encoding.UTF8);
        }

        /// <summary>
        /// 8.Base64解密
        /// </summary>
        /// <param name="result">待解密的密文</param>
        /// <param name="encoding">解密采用的编码方式，注意和加密时采用的方式一致</param>
        /// <returns>解密后的字符串</returns>
        public static string Base64Decode(this string result, Encoding encoding)
        {
            string decode = string.Empty;
            byte[] bytes = Convert.FromBase64String(result);
            try
            {
                decode = encoding.GetString(bytes);
            }
            catch
            {
                decode = result;
            }
            return decode;
        }

        /// <summary>
        /// 9.Base64Url编码
        /// </summary>
        /// <param name="text">待编码的文本字符串</param>
        /// <returns>编码的文本字符串</returns>
        public static string Base64UrlEncode(this string text)
        {
            var plainTextBytes = Encoding.UTF8.GetBytes(text);
            var base64 = Convert.ToBase64String(plainTextBytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');

            return base64;
        }

        /// <summary>
        /// 10.Base64Url解码
        /// </summary>
        /// <param name="base64UrlStr">使用Base64Url编码后的字符串</param>
        /// <returns>解码后的内容</returns>
        public static string Base64UrlDecode(this string base64UrlStr)
        {
            base64UrlStr = base64UrlStr.Replace('-', '+').Replace('_', '/');
            switch (base64UrlStr.Length % 4)
            {
                case 2:
                    base64UrlStr += "==";
                    break;
                case 3:
                    base64UrlStr += "=";
                    break;
            }
            var bytes = Convert.FromBase64String(base64UrlStr);

            return Encoding.UTF8.GetString(bytes);
        }

        /// <summary>
        /// 11.计算SHA1摘要
        /// 注：默认使用UTF8编码
        /// </summary>
        /// <param name="str">字符串</param>
        /// <returns></returns>
        public static byte[] ToSHA1Bytes(this string str)
        {
            return str.ToSHA1Bytes(Encoding.UTF8);
        }

        /// <summary>
        /// 12.计算SHA1摘要
        /// </summary>
        /// <param name="str">字符串</param>
        /// <param name="encoding">编码</param>
        /// <returns></returns>
        public static byte[] ToSHA1Bytes(this string str, Encoding encoding)
        {
            SHA1 sha1 = new SHA1CryptoServiceProvider();
            byte[] inputBytes = encoding.GetBytes(str);
            byte[] outputBytes = sha1.ComputeHash(inputBytes);

            return outputBytes;
        }

        /// <summary>
        /// 13.转为SHA1哈希加密字符串
        /// 注：默认使用UTF8编码
        /// </summary>
        /// <param name="str">字符串</param>
        /// <returns></returns>
        public static string ToSHA1String(this string str)
        {
            return str.ToSHA1String(Encoding.UTF8);
        }

        /// <summary>
        /// 14.转为SHA1哈希
        /// </summary>
        /// <param name="str">字符串</param>
        /// <param name="encoding">编码</param>
        /// <returns></returns>
        public static string ToSHA1String(this string str, Encoding encoding)
        {
            byte[] sha1Bytes = str.ToSHA1Bytes(encoding);
            string resStr = BitConverter.ToString(sha1Bytes);
            return resStr.Replace("-", "").ToLower();
        }

        /// <summary>
        /// 15.SHA256加密
        /// </summary>
        /// <param name="str">字符串</param>
        /// <returns></returns>
        public static string ToSHA256String(this string str)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(str);
            byte[] hash = SHA256.Create().ComputeHash(bytes);

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                builder.Append(hash[i].ToString("x2"));
            }

            return builder.ToString();
        }

        /// <summary>
        /// 16.HMACSHA256算法
        /// </summary>
        /// <param name="text">内容</param>
        /// <param name="secret">密钥</param>
        /// <returns></returns>
        public static string ToHMACSHA256String(this string text, string secret)
        {
            secret = secret ?? "";
            byte[] keyByte = Encoding.UTF8.GetBytes(secret);
            byte[] messageBytes = Encoding.UTF8.GetBytes(text);
            using (var hmacsha256 = new HMACSHA256(keyByte))
            {
                byte[] hashmessage = hmacsha256.ComputeHash(messageBytes);
                return Convert.ToBase64String(hashmessage).Replace('+', '-').Replace('/', '_').TrimEnd('=');
            }
        }

        /// <summary>
        /// 17.string转int
        /// </summary>
        /// <param name="str">字符串</param>
        /// <returns></returns>
        public static int ToInt(this string str)
        {
            str = str.Replace("\0", "");
            if (string.IsNullOrEmpty(str))
                return 0;
            return Convert.ToInt32(str);
        }

        /// <summary>
        /// 18.string转long
        /// </summary>
        /// <param name="str">字符串</param>
        /// <returns></returns>
        public static long ToLong(this string str)
        {
            str = str.Replace("\0", "");
            if (string.IsNullOrEmpty(str))
                return 0;

            return Convert.ToInt64(str);
        }

        /// <summary>
        /// 19.二进制字符串转为Int
        /// </summary>
        /// <param name="str">二进制字符串</param>
        /// <returns></returns>
        public static int ToInt_FromBinString(this string str)
        {
            return Convert.ToInt32(str, 2);
        }

        /// <summary>
        /// 20.将16进制字符串转为Int
        /// </summary>
        /// <param name="str">数值</param>
        /// <returns></returns>
        public static int ToIntHex(this string str)
        {
            int num = Int32.Parse(str, NumberStyles.HexNumber);
            return num;
        }

        /// <summary>
        /// 21.转换为double
        /// </summary>
        /// <param name="str">字符串</param>
        /// <returns></returns>
        public static double ToDouble(this string str)
        {
            return Convert.ToDouble(str);
        }

        /// <summary>
        /// 22.string转byte[]
        /// </summary>
        /// <param name="str">字符串</param>
        /// <returns></returns>
        public static byte[] ToBytes(this string str)
        {
            return Encoding.Default.GetBytes(str);
        }

        /// <summary>
        /// 23.string转byte[]
        /// </summary>
        /// <param name="str">字符串</param>
        /// <param name="theEncoding">需要的编码</param>
        /// <returns></returns>
        public static byte[] ToBytes(this string str, Encoding theEncoding)
        {
            return theEncoding.GetBytes(str);
        }

        /// <summary>
        /// 24.将16进制字符串转为Byte数组
        /// </summary>
        /// <param name="str">16进制字符串(2个16进制字符表示一个Byte)</param>
        /// <returns></returns>
        public static byte[] To0XBytes(this string str)
        {
            List<byte> resBytes = new List<byte>();
            for (int i = 0; i < str.Length; i = i + 2)
            {
                string numStr = $@"{str[i]}{str[i + 1]}";
                resBytes.Add((byte)numStr.ToIntHex());
            }

            return resBytes.ToArray();
        }

        /// <summary>
        /// 25.将ASCII码形式的字符串转为对应字节数组
        /// 注：一个字节一个ASCII码字符
        /// </summary>
        /// <param name="str">字符串</param>
        /// <returns></returns>
        public static byte[] ToASCIIBytes(this string str)
        {
            return str.ToList().Select(x => (byte)x).ToArray();
        }

        /// <summary>
        /// 26.转换为日期格式
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static DateTime ToDateTime(this string str)
        {
            return Convert.ToDateTime(str);
        }

        /// <summary>
        /// 27.删除Json字符串中键中的@符号
        /// </summary>
        /// <param name="jsonStr">json字符串</param>
        /// <returns></returns>
        public static string RemoveAt(this string jsonStr)
        {
            Regex reg = new Regex("\"@([^ \"]*)\"\\s*:\\s*\"(([^ \"]+\\s*)*)\"");
            string strPatten = "\"$1\":\"$2\"";
            return reg.Replace(jsonStr, strPatten);
        }

        // /// <summary>
        // /// 28.将XML字符串反序列化为对象
        // /// </summary>
        /// <param name="json">json字符串</param>
        /// <returns></returns>
        public static T ToEntity<T>(this string json)
        {
            if (json == null || json == "")
                return default(T);

            Type type = typeof(T);
            object obj = Activator.CreateInstance(type, null);

            foreach (var item in type.GetProperties())
            {
                PropertyInfo info = obj.GetType().GetProperty(item.Name);
                string pattern;
                pattern = "\"" + item.Name + "\":\"(.*?)\"";
                foreach (Match match in Regex.Matches(json, pattern))
                {
                    switch (item.PropertyType.ToString())
                    {
                        case "System.String": info.SetValue(obj, match.Groups[1].ToString(), null); break;
                        case "System.Int32": info.SetValue(obj, match.Groups[1].ToString().ToInt(), null); ; break;
                        case "System.Int64": info.SetValue(obj, Convert.ToInt64(match.Groups[1].ToString()), null); ; break;
                        case "System.DateTime": info.SetValue(obj, Convert.ToDateTime(match.Groups[1].ToString()), null); ; break;
                    }
                }
            }
            return (T)obj;
        }

        /// <summary>
        /// 35.转为首字母大写
        /// </summary>
        /// <param name="str">字符串</param>
        /// <returns></returns>
        public static string ToFirstUpperStr(this string str)
        {
            return str.Substring(0, 1).ToUpper() + str.Substring(1);
        }

        /// <summary>
        /// 36.转为首字母小写
        /// </summary>
        /// <param name="str">字符串</param>
        /// <returns></returns>
        public static string ToFirstLowerStr(this string str)
        {
            return str.Substring(0, 1).ToLower() + str.Substring(1);
        }

        /// <summary>
        /// 38.将枚举类型的文本转为枚举类型
        /// </summary>
        /// <typeparam name="TEnum">枚举类型</typeparam>
        /// <param name="enumText">枚举文本</param>
        /// <returns></returns>
        public static TEnum ToEnum<TEnum>(this string enumText) where TEnum : struct
        {
            Enum.TryParse(enumText, out TEnum value);

            return value;
        }

        /// <summary>
        /// 39.是否为弱密码
        /// 注:密码必须包含数字、小写字母、大写字母和其他符号中的两种并且长度大于8
        /// </summary>
        /// <param name="pwd">密码</param>
        /// <returns></returns>
        public static bool IsWeakPwd(this string pwd)
        {
            if (pwd == "")
                throw new Exception("pwd不能为空");

            string pattern = "(^[0-9]+$)|(^[a-z]+$)|(^[A-Z]+$)|(^.{0,8}$)";
            if (Regex.IsMatch(pwd, pattern))
                return true;
            else
                return false;
        }
        /// <summary>
        /// AES加密
        /// </summary>
        /// <remarks>
        /// 密钥可以是任意长度（不限 16/24/32 字节），内部会用 SHA256 派生为 32 字节（AES-256），
        /// 避免因密钥长度非法抛出 <see cref="CryptographicException"/>。
        /// 使用 ECB 模式 + PKCS7 填充。
        /// </remarks>
        /// <param name="text">要加密的文本</param>
        /// <param name="EncryptionKey">秘钥（任意长度）</param>
        /// <returns>Base64 字符串；输入为 null 时返回 null，加密失败时返回空串</returns>
        public static string AESEncrypt(this string text, string EncryptionKey)
        {
            if (text == null) return null;
            if (string.IsNullOrEmpty(EncryptionKey))
                throw new ArgumentException("EncryptionKey 不能为空", nameof(EncryptionKey));

            try
            {
                // 用 SHA256 把任意长度的密钥归一化成 32 字节（AES-256），绕开 Key 长度限制
                byte[] key;
                using (var sha = SHA256.Create())
                {
                    key = sha.ComputeHash(Encoding.UTF8.GetBytes(EncryptionKey));
                }

                using (var aes = Aes.Create())
                {
                    aes.Key = key;
                    aes.Mode = cipherMode;
                    aes.Padding = paddingMode;

                    using (var encryptor = aes.CreateEncryptor())
                    {
                        byte[] inputBuffer = Encoding.UTF8.GetBytes(text);
                        byte[] outputBuffer = encryptor.TransformFinalBlock(inputBuffer, 0, inputBuffer.Length);
                        return Convert.ToBase64String(outputBuffer);
                    }
                }
            }
            catch (CryptographicException)
            {
                // 加密过程出错，返回空串而不是抛出，避免调用链整体崩溃
                return string.Empty;
            }
        }

        /// <summary>
        /// AES解密
        /// </summary>
        /// <remarks>
        /// 密钥可以是任意长度（不限 16/24/32 字节），内部会用 SHA256 派生为 32 字节（AES-256），
        /// 与 <see cref="AESEncrypt"/> 配对使用。
        /// </remarks>
        /// <param name="cipherText">要解密的 Base64 文本</param>
        /// <param name="EncryptionKey">秘钥（任意长度）</param>
        /// <returns>解密后的明文；失败时返回 "错误"，输入为 null 时返回 null</returns>
        public static string AESDecrypt(this string cipherText, string EncryptionKey)
        {
            if (cipherText == null) return null;
            if (string.IsNullOrEmpty(cipherText) || string.IsNullOrEmpty(EncryptionKey))
                return string.Empty;

            try
            {
                // 用 SHA256 把任意长度的密钥归一化成 32 字节（AES-256），与加密端保持一致
                byte[] key;
                using (var sha = SHA256.Create())
                {
                    key = sha.ComputeHash(Encoding.UTF8.GetBytes(EncryptionKey));
                }

                using (var aes = Aes.Create())
                {
                    aes.Key = key;
                    aes.Mode = cipherMode;
                    aes.Padding = paddingMode;

                    using (var decryptor = aes.CreateDecryptor())
                    {
                        byte[] inputBuffer = Convert.FromBase64String(cipherText);
                        byte[] outputBuffer = decryptor.TransformFinalBlock(inputBuffer, 0, inputBuffer.Length);
                        return Encoding.UTF8.GetString(outputBuffer);
                    }
                }
            }
            catch
            {
                return "错误";
            }
        }

        // AES 使用的模式/填充，集中在这里方便以后切换成 CBC 等更安全的模式
        private const CipherMode cipherMode = CipherMode.ECB;
        private const PaddingMode paddingMode = PaddingMode.PKCS7;

        /// <summary>
        /// AES加密(8个中文字符或16个英文数字)
        /// </summary>
        /// <remarks>
        /// ⚠️ 旧版 API：直接使用 UTF-8 字节作为密钥，仅支持 16 / 24 / 32 字节，否则会抛 <see cref="CryptographicException"/>。
        /// 新代码请使用 <see cref="AESEncrypt(string, string)"/>（任意长度密钥，内部 SHA256 派生）。
        /// 保留本方法仅为兼容已经存量的密文。
        /// </remarks>
        /// <param name="text">要加密的文本</param>
        /// <param name="EncryptionKey">秘钥（必须 16 / 24 / 32 字节）</param>
        /// <returns>Base64 字符串</returns>
        public static string AESEncryptLegacy(this string text, string EncryptionKey)
        {
            byte[] key = Encoding.UTF8.GetBytes(EncryptionKey);
            using (var aes = Aes.Create())
            {
                aes.Key = key;
                aes.Mode = cipherMode;
                aes.Padding = paddingMode;

                using (var encryptor = aes.CreateEncryptor())
                {
                    byte[] inputBuffer = Encoding.UTF8.GetBytes(text);
                    byte[] outputBuffer = encryptor.TransformFinalBlock(inputBuffer, 0, inputBuffer.Length);
                    return Convert.ToBase64String(outputBuffer);
                }
            }
        }

        /// <summary>
        /// AES解密（与 <see cref="AESEncryptLegacy"/> 配对）
        /// </summary>
        public static string AESDecryptLegacy(this string cipherText, string EncryptionKey)
        {
            byte[] key = Encoding.UTF8.GetBytes(EncryptionKey);
            try
            {
                using (var aes = Aes.Create())
                {
                    aes.Key = key;
                    aes.Mode = cipherMode;
                    aes.Padding = paddingMode;

                    using (var decryptor = aes.CreateDecryptor())
                    {
                        byte[] inputBuffer = Convert.FromBase64String(cipherText);
                        byte[] outputBuffer = decryptor.TransformFinalBlock(inputBuffer, 0, inputBuffer.Length);
                        return Encoding.UTF8.GetString(outputBuffer);
                    }
                }
            }
            catch
            {
                return "错误";
            }
        }
        // /// <summary>
        // /// 获取MAC地址，仅在exe程序中可使用
        // /// </summary>
        // /// <returns>mac地址</returns>
        // public static string GetMacAddress()
        // {
        //     string physicalAddress = "";

        //     NetworkInterface[] nice = NetworkInterface.GetAllNetworkInterfaces();

        //     foreach (NetworkInterface adaper in nice)
        //     {
        //         if (adaper.Description == "en0")
        //         {
        //             physicalAddress = adaper.GetPhysicalAddress().ToString();
        //             break;
        //         }
        //         else
        //         {
        //             physicalAddress = adaper.GetPhysicalAddress().ToString();

        //             if (physicalAddress != "")
        //             {
        //                 break;
        //             };
        //         }
        //     }

        //     return physicalAddress;
        // }

        /// <summary>
        /// 获取持久化唯一标识符。首次调用时生成 GUID 并写入 PlayerPrefs，之后返回同一值。
        /// （注意：实际是 GUID，不是 MAC 地址。WebGL 等没有真实 MAC 的平台请使用此方法。）
        /// </summary>
        public static string GetPersistentGuid()
        {
            string uniqueIdentifier = PlayerPrefs.GetString("UniqueIdentifier");

            if (string.IsNullOrEmpty(uniqueIdentifier))
            {
                // 如果UniqueIdentifier尚不存在，则创建一个新的身份
                uniqueIdentifier = System.Guid.NewGuid().ToString();
                PlayerPrefs.SetString("UniqueIdentifier", uniqueIdentifier);
                PlayerPrefs.Save();
            }

            return uniqueIdentifier;
        }
        /// <summary>
        /// 生成随机字符串
        /// </summary>
        /// <param name="length">长度</param>
        /// <returns></returns>
        public static string RandomString(int length)
        {
            System.Random random = new System.Random();
            string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghizklmnopqrstuvwxyz0123456789";

            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}