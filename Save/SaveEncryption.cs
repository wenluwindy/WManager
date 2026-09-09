using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Serialization;
using UnityEngine;

namespace WManager
{
    /// <summary> 存档加密解密 </summary> 
    public static class SaveEncryption
    {
        //密钥字符串
        private static readonly string keyString = "250 192 34 149 21 46 249 203 233 24 21 152 226 218 169 215 104 43 18 180 104 19 12 20 37 3 7 223 58 70 222 98";
        //密钥字节数组
        private static readonly byte[] key = GetBytes(keyString);

        // 反序列化
        public static T Deserialize<T>(this string toDeserialize)
        {
            if (string.IsNullOrEmpty(toDeserialize))
                throw new ArgumentNullException(nameof(toDeserialize));

            XmlSerializer xmlSerializer = new XmlSerializer(typeof(T));
            using (StringReader textReader = new StringReader(toDeserialize))
            {
                return (T)xmlSerializer.Deserialize(textReader);
            }
        }
        //序列化
        public static string Serialize<T>(this T toSerialize)
        {
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(T));
            using (StringWriter textWriter = new StringWriter())
            {
                xmlSerializer.Serialize(textWriter, toSerialize);
                return textWriter.ToString();
            }
        }

        // 加密字符串为字节数组——AES 
        public static byte[] EncryptStringToBytes_Aes(string plainText, byte[] Key, byte[] IV)
        {
            //检查参数
            if (plainText == null || plainText.Length <= 0)
                throw new ArgumentNullException(nameof(plainText));
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException(nameof(Key));
            if (IV == null || IV.Length <= 0)
                throw new ArgumentNullException(nameof(IV));

            using (AesCryptoServiceProvider aesAlg = new AesCryptoServiceProvider())
            {
                aesAlg.Key = Key;
                aesAlg.IV = IV;

                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {
                            swEncrypt.Write(plainText);
                        }
                    }
                    return msEncrypt.ToArray();
                }
            }
        }
        // AES 解密字节数组为字符串
        public static string DecryptStringFromBytes_Aes(byte[] cipherText, byte[] Key, byte[] IV)
        {
            //检查参数
            if (cipherText == null || cipherText.Length <= 0)
                throw new ArgumentNullException(nameof(cipherText));
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException(nameof(Key));
            if (IV == null || IV.Length <= 0)
                throw new ArgumentNullException(nameof(IV));

            using (AesCryptoServiceProvider aesAlg = new AesCryptoServiceProvider())
            {
                aesAlg.Key = Key;
                aesAlg.IV = IV;

                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                using (MemoryStream msDecrypt = new MemoryStream(cipherText))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {
                            return srDecrypt.ReadToEnd();
                        }
                    }
                }
            }
        }

        //获取字节数组
        public static byte[] GetBytes(string pData)
        {
            if (string.IsNullOrWhiteSpace(pData))
                return Array.Empty<byte>();

            string[] encrypted = pData.Split(' ');
            byte[] bytes = new byte[encrypted.Length];
            int len = encrypted.Length;

            for (int i = 0; i < len; ++i)
            {
                bytes[i] = byte.Parse(encrypted[i]);
            }
            return bytes;
        }
        //获取字符串
        public static string GetString(byte[] pData)
        {
            if (pData == null || pData.Length == 0)
                return string.Empty;

            StringBuilder sb = new StringBuilder();
            int len = pData.Length;
            for (int i = 0; i < len; ++i)
            {
                sb.Append(pData[i]);
                if (i < len - 1) sb.Append(' ');
            }
            return sb.ToString();
        }

        /// <summary> 保存加密数据</summary>
        /// <param name="pData">要保存的数据，如“Albert”或0、1.5、false等。</param>
        /// <param name="pPath">将数据保存到的路径，例如“Player Data/Albert”。</param>
        public static void Save<T>(T pData, string pPath)
        {
            string dataPath = Path.Combine(Application.persistentDataPath, pPath + ".save");
            SaveToAbsolutePath(pData, dataPath);
        }

        /// <summary>
        /// 保存加密数据到绝对路径（避免在后台线程访问 Application.persistentDataPath）。
        /// 调用方必须在主线程提前解析出绝对路径，再把工作交给后台线程。
        /// </summary>
        public static void SaveToAbsolutePath<T>(T pData, string dataPath)
        {
            string directory = Path.GetDirectoryName(dataPath);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            Save_Data<T> saveData = new Save_Data<T>(pData);
            string serialized = Serialize(saveData);

            using (AesCryptoServiceProvider aes = new AesCryptoServiceProvider())
            {
                aes.Key = key;
                aes.GenerateIV();
                byte[] encrypted = EncryptStringToBytes_Aes(serialized, aes.Key, aes.IV);
                byte[] iv = aes.IV;

                // 格式: [4字节IV长度][IV][密文]
                byte[] result = new byte[4 + iv.Length + encrypted.Length];
                BitConverter.GetBytes(iv.Length).CopyTo(result, 0);
                iv.CopyTo(result, 4);
                encrypted.CopyTo(result, 4 + iv.Length);

                string tempPath = dataPath + ".tmp";
                File.WriteAllBytes(tempPath, result);

                if (File.Exists(dataPath))
                    File.Delete(dataPath);
                File.Move(tempPath, dataPath);
            }
        }
        /// <summary> 保存到 PlayerPrefs（加密） </summary>
        /// <param name="pData">要保存的数据，如“Albert”或0、1.5、false等。</param>
        /// <param name="pKey">PlayerPrefs 的键，例如 'Albert'。</param>
        public static void SaveToPrefs<T>(T pData, string pKey)
        {
            Save_Data<T> saveData = new Save_Data<T>(pData);
            string serialized = Serialize(saveData);

            using (AesCryptoServiceProvider aes = new AesCryptoServiceProvider())
            {
                aes.Key = key;
                aes.GenerateIV();
                byte[] encrypted = EncryptStringToBytes_Aes(serialized, aes.Key, aes.IV);
                byte[] iv = aes.IV;

                // 格式: [4字节IV长度][IV][密文] -> Base64
                byte[] result = new byte[4 + iv.Length + encrypted.Length];
                BitConverter.GetBytes(iv.Length).CopyTo(result, 0);
                iv.CopyTo(result, 4);
                encrypted.CopyTo(result, 4 + iv.Length);

                PlayerPrefs.SetString(pKey, Convert.ToBase64String(result));
            }
        }

        /// <summary>从加密内存中返回数据对象(如果存在)。</summary>
        /// <param name="pPath">The Path to load Data from, such as 'Player Data/Albert'.</param>
        /// <returns></returns>
        public static T Load<T>(string pPath)
        {
            string dataPath = Path.Combine(Application.persistentDataPath, pPath + ".save");
            return LoadFromAbsolutePath<T>(dataPath, pPath);
        }

        /// <summary>
        /// 从绝对路径加载数据（避免在后台线程访问 Application.persistentDataPath）。
        /// </summary>
        /// <param name="dataPath">完整文件绝对路径。</param>
        /// <param name="logicalPath">逻辑路径，仅用于错误日志（可为空）。</param>
        public static T LoadFromAbsolutePath<T>(string dataPath, string logicalPath = null)
        {
            string label = string.IsNullOrEmpty(logicalPath) ? dataPath : logicalPath;

            if (!File.Exists(dataPath))
            {
                Debug.LogError("给定的保存文件'" + label + "' 不存在");
                return default;
            }

            byte[] allBytes = File.ReadAllBytes(dataPath);
            if (allBytes.Length < 4)
            {
                Debug.LogError("保存文件'" + label + "' 已损坏或格式无效");
                return default;
            }

            int ivLength = BitConverter.ToInt32(allBytes, 0);
            if (ivLength <= 0 || ivLength > 256 || allBytes.Length < 4 + ivLength)
            {
                Debug.LogError("保存文件'" + label + "' 已损坏或IV长度无效");
                return default;
            }

            byte[] iv = new byte[ivLength];
            byte[] cipherText = new byte[allBytes.Length - 4 - ivLength];

            Buffer.BlockCopy(allBytes, 4, iv, 0, ivLength);
            Buffer.BlockCopy(allBytes, 4 + ivLength, cipherText, 0, cipherText.Length);

            using (AesCryptoServiceProvider aes = new AesCryptoServiceProvider())
            {
                aes.Key = key;
                string decrypted = DecryptStringFromBytes_Aes(cipherText, aes.Key, iv);
                Save_Data<T> Data = Deserialize<Save_Data<T>>(decrypted);
                return Data.SaveData;
            }
        }
        /// <summary> 从 PlayerPrefs（加密）中返回数据对象(如果存在)。</summary>
        /// <param name="pKey">PlayerPrefs 的键，例如 'Albert'。</param>
        /// <returns></returns>
        public static T LoadFromPrefs<T>(string pKey)
        {
            if (!PlayerPrefs.HasKey(pKey))
            {
                Debug.LogError("给定的保存文件 '" + pKey + "'不存在");
                return default;
            }

            string base64 = PlayerPrefs.GetString(pKey);
            byte[] allBytes;
            try
            {
                allBytes = Convert.FromBase64String(base64);
            }
            catch (FormatException)
            {
                Debug.LogError("保存键'" + pKey + "' 的数据格式无效");
                return default;
            }

            if (allBytes.Length < 4)
            {
                Debug.LogError("保存键'" + pKey + "' 的数据已损坏");
                return default;
            }

            int ivLength = BitConverter.ToInt32(allBytes, 0);
            if (ivLength <= 0 || ivLength > 256 || allBytes.Length < 4 + ivLength)
            {
                Debug.LogError("保存键'" + pKey + "' 的数据已损坏或IV长度无效");
                return default;
            }

            byte[] iv = new byte[ivLength];
            byte[] cipherText = new byte[allBytes.Length - 4 - ivLength];

            Buffer.BlockCopy(allBytes, 4, iv, 0, ivLength);
            Buffer.BlockCopy(allBytes, 4 + ivLength, cipherText, 0, cipherText.Length);

            using (AesCryptoServiceProvider aes = new AesCryptoServiceProvider())
            {
                aes.Key = key;
                string decrypted = DecryptStringFromBytes_Aes(cipherText, aes.Key, iv);
                Save_Data<T> Data = Deserialize<Save_Data<T>>(decrypted);
                return Data.SaveData;
            }
        }
    }
}
