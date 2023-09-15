using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Events;
using System.IO;

namespace WManager
{
    /// <summary>
    /// 读取StreamingAsset中的文件
    /// </summary>
    public class StreamingAssetsLoader : MonoBehaviour
    {
        private const string ASSET_BUNDLE_MANAGER_NAME = "StreamingAssetsLoader";
        private static StreamingAssetsLoader instance;
        public static StreamingAssetsLoader Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new GameObject(ASSET_BUNDLE_MANAGER_NAME).AddComponent<StreamingAssetsLoader>();
                    DontDestroyOnLoad(instance);
                }
                return instance;
            }
        }
        static string GetAbsolutePath(string path)
        {
            //本地加载路径前面要加上file:// 否则会出错
#if UNITY_WIN_STANDALONE || UNITY_IPHONE && !UNITY_EDITOR
            return "file://"+ Application.streamingAssetsPath + path;
#else
            return Application.streamingAssetsPath + "/" + path;
#endif
        }
        /// <summary>
        /// 读取StreamingAsset中的txt文件
        /// 包含.txt和.json文件
        /// </summary>
        /// <param name="configName">文件名，包含后缀</param>
        /// <param name="action">回调的string方法</param>
        public static void LoadTextAsset(string configName, UnityAction<string> action)
        {
            Instance.StartCoroutine(ITextReader(configName, action));
        }
        static IEnumerator ITextReader(string configName, UnityAction<string> action = null)
        {
            UnityWebRequest unityWebRequest = UnityWebRequest.Get(GetAbsolutePath(configName));
            yield return unityWebRequest.SendWebRequest();

            if (unityWebRequest.error != null)
                Debug.Log(unityWebRequest.error);
            else
            {
                string content = unityWebRequest.downloadHandler.text;
                if (action != null)
                    action(content);
            }
        }

        /// <summary>
        /// 读取streamingAsset中的图片
        /// </summary>
        /// <param name="mediaName">图片名称,带后缀</param>
        /// <param name="action">回调的图片方法</param>
        public static void LoadTextureAsset(string mediaName, UnityAction<Texture> action)
        {
            Instance.StartCoroutine(ITextureReader(mediaName, action));
        }
        static IEnumerator ITextureReader(string mediaName, UnityAction<Texture> action)
        {
            UnityWebRequest unityWebRequest = UnityWebRequestTexture.GetTexture(GetAbsolutePath(mediaName));
            yield return unityWebRequest.SendWebRequest();

            if (unityWebRequest.error != null)
                Debug.Log(unityWebRequest.error);
            else
            {
                byte[] bts = unityWebRequest.downloadHandler.data;
                if (action != null)
                {
                    action(DownloadHandlerTexture.GetContent(unityWebRequest));
                }
            }
        }
        /// <summary>
        /// 读取streamingAsset文件夹中的多媒体（音频）
        /// </summary>
        /// <param name="mediaName">音频名称:.mp4,.ogg,.wav,.aiff,.mpeg</param>
        /// <param name="action">音频回调方法</param>
        public static void LoadAudioAsset(string mediaName, UnityAction<AudioClip> action)
        {
            Instance.StartCoroutine(IAudioClipReader(mediaName, action));
        }
        static IEnumerator IAudioClipReader(string mediaName, UnityAction<AudioClip> action)
        {
            FileInfo fileInfo = new FileInfo(GetAbsolutePath(mediaName));
            string fileExtension = fileInfo.Extension;
            AudioType audioType;

            switch (fileExtension)
            {
                case ".mp3":
                    audioType = AudioType.MPEG;
                    break;
                case ".ogg":
                    audioType = AudioType.OGGVORBIS;
                    break;
                case ".wav":
                    audioType = AudioType.WAV;
                    break;
                case ".aiff":
                    audioType = AudioType.AIFF;
                    break;
                default:
                    audioType = AudioType.UNKNOWN;
                    break;
            }

            if (audioType == AudioType.UNKNOWN)
            {
                //Debug.Log("不支持的音频格式,跳过");
            }
            else
            {
                UnityWebRequest unityWebRequest = UnityWebRequestMultimedia.GetAudioClip(GetAbsolutePath(mediaName), audioType);
                yield return unityWebRequest.SendWebRequest();

                if (unityWebRequest.error != null)
                    Debug.Log(unityWebRequest.error);
                else
                {
                    action?.Invoke(DownloadHandlerAudioClip.GetContent(unityWebRequest));
                }
            }
        }
    }
}

