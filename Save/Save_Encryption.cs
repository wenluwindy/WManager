using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Xml.Serialization;
using UnityEngine;

namespace WManager.Save
{
    /// <summary> 存档加密解密 </summary> 
    public static class Save_Encryption
    {
        //密钥字符串
        private static readonly string keyString = "250 192 34 149 21 46 249 203 233 24 21 152 226 218 169 215 104 43 18 180 104 19 12 20 37 3 7 223 58 70 222 98";
        //密钥字节数组
        private static readonly byte[] key = GetBytes(keyString);
        //AES加密
        private static AesCryptoServiceProvider aes = new AesCryptoServiceProvider();

        // 反序列化 https://stackoverflow.com/questions/2434534/serialize-an-object-to-string
        public static T Deserialize<T>(this string toDeserialize)
        {
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
        // https://docs.microsoft.com/en-us/dotnet/api/system.security.cryptography.aescryptoserviceprovider?view=netframework-4.7.2
        public static byte[] EncryptStringToBytes_Aes(string plainText, byte[] Key, byte[] IV)
        {
            //检查参数
            if (plainText == null || plainText.Length <= 0)
                throw new System.ArgumentNullException(nameof(plainText));
            if (Key == null || Key.Length <= 0)
                throw new System.ArgumentNullException(nameof(Key));
            if (IV == null || IV.Length <= 0)
                throw new System.ArgumentNullException(nameof(IV));
            byte[] encrypted;

            //创建AesCryptoServiceProvider对象
            //按指定密钥和IV
            using (AesCryptoServiceProvider aesAlg = new AesCryptoServiceProvider())
            {
                aesAlg.Key = Key;
                aesAlg.IV = IV;

                // Create an encryptor to perform the stream transform.
                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                // Create the streams used for encryption.
                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {
                            //Write all data to the stream.
                            swEncrypt.Write(plainText);
                        }
                        encrypted = msEncrypt.ToArray();
                    }
                }
            }

            //返回内存流中的加密字节
            return encrypted;

        }
        // AES 解密字节数组为字符串
        public static string DecryptStringFromBytes_Aes(byte[] cipherText, byte[] Key, byte[] IV)
        {
            //检查参数
            if (cipherText == null || cipherText.Length <= 0)
                throw new System.ArgumentNullException(nameof(cipherText));
            if (Key == null || Key.Length <= 0)
                throw new System.ArgumentNullException(nameof(Key));
            if (IV == null || IV.Length <= 0)
                throw new System.ArgumentNullException(nameof(IV));

            //声明用于保存解密文本的字符串
            string plaintext = null;

            //创建AesCryptoServiceProvider对象
            //按指定密钥和IV
            using (AesCryptoServiceProvider aesAlg = new AesCryptoServiceProvider())
            {
                aesAlg.Key = Key;
                aesAlg.IV = IV;

                // Create a decryptor to perform the stream transform.
                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                // Create the streams used for decryption.
                using (MemoryStream msDecrypt = new MemoryStream(cipherText))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {
                            // Read the decrypted bytes from the decrypting stream
                            // and place them in a string.
                            plaintext = srDecrypt.ReadToEnd();
                        }
                    }
                }

            }

            return plaintext;

        }

        //修复解密字符串
        public static string FixDecrypted(string pDecrypted)
        {
            int len = pDecrypted.Length;
            int start = 0;
            for (int i = 0; i < len; ++i)
            {
                if (pDecrypted.Substring(i, 3) == ".0\"") { start = i + 3; break; }
                if (i > 30) break;
            }
            pDecrypted = "<?xml version=\"1.0\"" + pDecrypted.Substring(start);
            return pDecrypted;
        }

        //获取字节数组
        public static byte[] GetBytes(string pData)
        {
            string[] encrypted = pData.Split(char.Parse(" "));
            byte[] bytes = new byte[encrypted.Length];
            int len = encrypted.Length;

            for (int i = 0; i < len; ++i)
            { bytes[i] = byte.Parse(encrypted[i]); }
            return bytes;
        }
        //获取字符串
        public static string GetString(byte[] pData)
        {
            string str = "";
            int len = pData.Length;
            for (int i = 0; i < len; ++i)
            {
                str += "" + pData[i];
                if (i < len - 1) str += " ";
            }
            return str;
        }

        /// <summary> 保存加密数据</summary>
        /// <param name="pData">要保存的数据，如“Albert”或0、1.5、false等。</param>
        /// <param name="pPath">将数据保存到的路径，例如“Player Data/Albert”。</param>
        public static void Save<T>(T pData, string pPath)
        {
            string dataPath = Application.persistentDataPath + pPath + ".save";
            BinaryFormatter formatter = new BinaryFormatter();
            FileStream stream = new FileStream(dataPath, FileMode.Create);

            Save_Data<T> saveData = new Save_Data<T>(pData);
            string serialized = Serialize(saveData);
            byte[] bytes;
            aes.Key = key;
            aes.GenerateIV();
            bytes = EncryptStringToBytes_Aes(serialized, aes.Key, aes.IV);
            formatter.Serialize(stream, bytes);

            stream.Close();
        }
        /// <summary> 保存Web加密数据 </summary>
        /// <param name="pData">要保存的数据，如“Albert”或0、1.5、false等。</param>
        /// <param name="pKey">The key to save Data into, such as 'Albert'.</param>
        public static void SaveWeb<T>(T pData, string pKey)
        {
            Save_Data<T> saveData = new Save_Data<T>(pData);
            string serialized = Serialize(saveData);
            byte[] bytes;
            aes.Key = key;
            aes.GenerateIV();
            bytes = EncryptStringToBytes_Aes(serialized, aes.Key, aes.IV);
            string encrypted = GetString(bytes);
            PlayerPrefs.SetString(pKey, encrypted);
            PlayerPrefs.Save();
        }

        /// <summary>从加密内存中返回数据对象(如果存在)。</summary>
        /// <param name="pPath">The Path to load Data from, such as 'Player Data/Albert'.</param>
        /// <returns></returns>
        public static T Load<T>(string pPath)
        {
            string dataPath = Application.persistentDataPath + pPath + ".save";

            if (File.Exists(dataPath))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                FileStream stream = new FileStream(dataPath, FileMode.Open);
                byte[] bytes = (byte[])formatter.Deserialize(stream);
                string decrypted;
                aes.Key = key;
                aes.GenerateIV();
                decrypted = DecryptStringFromBytes_Aes(bytes, aes.Key, aes.IV);
                Save_Data<T> Data = Deserialize<Save_Data<T>>(FixDecrypted(decrypted));
                stream.Close();
                return Data.SaveData;
            }

            Debug.LogError("给定的保存文件'" + pPath + "' 不存在");
            return default;
        }
        /// <summary> 从Web的加密内存中返回数据对象(如果存在)。</summary>
        /// <param name="pKey">The key to load Data from, such as 'Albert'.</param>
        /// <returns></returns>
        public static T LoadWeb<T>(string pKey)
        {
            if (PlayerPrefs.HasKey(pKey))
            {
                byte[] bytes = GetBytes(PlayerPrefs.GetString(pKey));
                string decrypted;
                aes.Key = key;
                aes.GenerateIV();
                decrypted = DecryptStringFromBytes_Aes(bytes, aes.Key, aes.IV);
                Save_Data<T> Data = Deserialize<Save_Data<T>>(FixDecrypted(decrypted));

                return Data.SaveData;
            }

            Debug.LogError("给定的保存文件 '" + pKey + "'不存在");
            return default;
        }
    }
}