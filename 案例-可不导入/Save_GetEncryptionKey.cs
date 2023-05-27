using UnityEngine;
using System.Security.Cryptography;

namespace WManager.Save
{
    /// <summary> 启动时打印出密钥。用于获取一次密钥，将Save_Encryption脚本中的密钥替换为新打印的密钥。</summary>
    public class Save_GetEncryptionKey : MonoBehaviour
    {
        private void Start()
        {
            AesCryptoServiceProvider newAes = new AesCryptoServiceProvider();
            newAes.GenerateKey();
#if UNITY_EDITOR
            Debug.Log(Save_Encryption.GetString(newAes.Key));
#endif
        }
    }
}