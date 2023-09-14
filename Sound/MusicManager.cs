using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

namespace WManager
{
    /// <summary>
    /// 音乐管理器：读取StreamingAssets文件夹下音乐并进行管理
    /// </summary>
    public class StreamingAssetsMusicLoad : MonoBehaviour
    {
        #region 单例模式
        private const string ASSET_BUNDLE_MANAGER_NAME = "StreamingAssetsMusicLoad";
        public static StreamingAssetsMusicLoad _instance;
        public static StreamingAssetsMusicLoad Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new GameObject(ASSET_BUNDLE_MANAGER_NAME).AddComponent<StreamingAssetsMusicLoad>();
                    DontDestroyOnLoad(_instance);
                }
                return _instance;
            }
        }
        #endregion

        static readonly string streamingAssetsPath = Application.streamingAssetsPath + "/Music/";
        public static List<AudioClip> BackgroundMusics = new List<AudioClip>();
        /// <summary>
        /// 初始化
        /// </summary>
        public static void Initialization()
        {
            // 使用Directory.GetFiles来获取所有文件名
            string[] files = Directory.GetFiles(streamingAssetsPath);
            // 遍历所有文件名并输出它们
            foreach (string file in files)
            {
                //Debug.Log("文件: " + Path.GetFileName(file));
                StreamingAssetsLoader.LoadAudioAsset("Music/" + Path.GetFileName(file), (clip) =>
                {
                    BackgroundMusics.Add(clip);
                    //print(BackgroundMusics.Count);
                });
            }
        }
        /// <summary>
        /// 随机播放
        /// </summary>
        public static void RandomPlay()
        {
            //print(BackgroundMusics.Count);
            int index = Random.Range(0, BackgroundMusics.Count);
            SoundManager.PlayMusic(BackgroundMusics[index], 1, true, true);
        }
    }
}
