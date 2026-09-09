using System.Collections;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Events;
using System.IO;

namespace WManager
{
    /// <summary>
    /// 资源加载管理器：支持 StreamingAssets 本地读取与网络 URL 下载
    /// 继承 SingletonBehaviour，统一单例生命周期。
    /// 同步风格 LoadText/LoadTexture/LoadAudio 保留；推荐使用 *Async 版本。
    /// </summary>
    public class StreamingAssetsLoader : SingletonBehaviour<StreamingAssetsLoader>
    {

        /// <summary>
        /// 内部工具：处理路径。如果是URL则直接返回，如果是文件名则补全StreamingAssets路径
        /// </summary>
        private string GetProcessedPath(string path)
        {
            if (path.Contains("://")) return path; // 已经是完整路径或URL

#if UNITY_EDITOR
            return "file://" + Application.streamingAssetsPath + "/" + path;
#elif UNITY_ANDROID
            return Application.streamingAssetsPath + "/" + path;
#else
            return "file://" + Application.streamingAssetsPath + "/" + path;
#endif
        }

        #region 公共接口（回调风格，保留兼容）

        /// <summary>
        /// 加载文本资源 (支持本地与URL)
        /// </summary>
        public static void LoadText(string path, UnityAction<string> callback)
        {
            Instance.StartCoroutine(Instance.RequestText(Instance.GetProcessedPath(path), callback));
        }

        /// <summary>
        /// 加载图片资源 (支持本地与URL)
        /// </summary>
        public static void LoadTexture(string path, UnityAction<Texture2D> callback)
        {
            Instance.StartCoroutine(Instance.RequestTexture(Instance.GetProcessedPath(path), callback));
        }

        /// <summary>
        /// 加载音频资源 (支持本地与URL)
        /// </summary>
        public static void LoadAudio(string path, UnityAction<AudioClip> callback)
        {
            Instance.StartCoroutine(Instance.RequestAudio(Instance.GetProcessedPath(path), callback));
        }

        #endregion

        #region UniTask 风格（新推荐）

        /// <summary>
        /// 异步加载文本
        /// </summary>
        public UniTask<string> LoadTextAsync(string path, CancellationToken cancellationToken = default)
        {
            return RequestTextAsync(GetProcessedPath(path), cancellationToken);
        }

        /// <summary>
        /// 异步加载图片
        /// </summary>
        public UniTask<Texture2D> LoadTextureAsync(string path, CancellationToken cancellationToken = default)
        {
            return RequestTextureAsync(GetProcessedPath(path), cancellationToken);
        }

        /// <summary>
        /// 异步加载音频
        /// </summary>
        public UniTask<AudioClip> LoadAudioAsync(string path, CancellationToken cancellationToken = default)
        {
            return RequestAudioAsync(GetProcessedPath(path), cancellationToken);
        }

        #endregion

        #region 内部协程实现

        private IEnumerator RequestText(string url, UnityAction<string> callback)
        {
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                    callback?.Invoke(request.downloadHandler.text);
                else
                    Debug.LogError($"[StreamingAssetsLoader] 文本加载错误: {request.error} | URL: {url}");
            }
        }

        private IEnumerator RequestTexture(string url, UnityAction<Texture2D> callback)
        {
            using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                    callback?.Invoke(DownloadHandlerTexture.GetContent(request));
                else
                    Debug.LogError($"[StreamingAssetsLoader] 图片加载错误: {request.error} | URL: {url}");
            }
        }

        private IEnumerator RequestAudio(string url, UnityAction<AudioClip> callback)
        {
            AudioType type = GetAudioType(url);
            using (UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(url, type))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                    callback?.Invoke(DownloadHandlerAudioClip.GetContent(request));
                else
                    Debug.LogError($"[StreamingAssetsLoader] 音频加载错误: {request.error} | URL: {url}");
            }
        }

        private async UniTask<string> RequestTextAsync(string url, CancellationToken cancellationToken)
        {
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                await request.SendWebRequest().WithCancellation(cancellationToken);
                if (request.result == UnityWebRequest.Result.Success)
                    return request.downloadHandler.text;

                Debug.LogError($"[StreamingAssetsLoader] 文本加载错误: {request.error} | URL: {url}");
                return null;
            }
        }

        private async UniTask<Texture2D> RequestTextureAsync(string url, CancellationToken cancellationToken)
        {
            using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
            {
                await request.SendWebRequest().WithCancellation(cancellationToken);
                if (request.result == UnityWebRequest.Result.Success)
                    return DownloadHandlerTexture.GetContent(request);

                Debug.LogError($"[StreamingAssetsLoader] 图片加载错误: {request.error} | URL: {url}");
                return null;
            }
        }

        private async UniTask<AudioClip> RequestAudioAsync(string url, CancellationToken cancellationToken)
        {
            AudioType type = GetAudioType(url);
            using (UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(url, type))
            {
                await request.SendWebRequest().WithCancellation(cancellationToken);
                if (request.result == UnityWebRequest.Result.Success)
                    return DownloadHandlerAudioClip.GetContent(request);

                Debug.LogError($"[StreamingAssetsLoader] 音频加载错误: {request.error} | URL: {url}");
                return null;
            }
        }

        /// <summary>
        /// 根据后缀名获取音频类型
        /// </summary>
        private AudioType GetAudioType(string url)
        {
            string ext = Path.GetExtension(url).ToLower();
            switch (ext)
            {
                case ".mp3": return AudioType.MPEG;
                case ".ogg": return AudioType.OGGVORBIS;
                case ".wav": return AudioType.WAV;
                case ".aiff": return AudioType.AIFF;
                default: return AudioType.UNKNOWN;
            }
        }

        #endregion
    }
}