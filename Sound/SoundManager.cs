using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.Linq;

namespace WManager
{
    /// <summary>
    /// 声音管理：负责播放和管理音频
    ///  Music Eff UISound
    /// </summary>
    public class SoundManager : MonoBehaviour
    {
        /// <summary>
        /// 声音管理器附加到的游戏对象
        /// </summary>
        public static GameObject Gameobject { get { return Instance.gameObject; } }

        /// <summary>
        /// 当设置为true时，与任何其他音乐音频具有相同音频剪辑的新音乐音频将被忽略
        /// </summary>
        public static bool IgnoreDuplicateMusic { get; set; }

        /// <summary>
        /// 设置为true时，将忽略与任何其他声音音频具有相同音频剪辑的新声音音频
        /// </summary>
        public static bool IgnoreDuplicateEffs { get; set; }

        /// <summary>
        /// 设置为true时，将忽略与任何其他UI声音音频具有相同音频剪辑的新UI声音音频
        /// </summary>
        public static bool IgnoreDuplicateUISounds { get; set; }

        /// <summary>
        /// 全局音量
        /// </summary>
        public static float GlobalVolume { get; set; }

        /// <summary>
        /// 全局音乐音量
        /// </summary>
        public static float GlobalMusicVolume { get; set; }

        /// <summary>
        /// 全局声音音量
        /// </summary>
        public static float GlobalEffsVolume { get; set; }

        /// <summary>
        /// 全局UI声音音量
        /// </summary>
        public static float GlobalUISoundsVolume { get; set; }

        private static SoundManager instance = null;

        private static Dictionary<int, Audio> musicAudio;
        private static Dictionary<int, Audio> effsAudio;
        private static Dictionary<int, Audio> UISoundsAudio;
        private static Dictionary<int, Audio> audioPool;

        private static bool initialized = false;

        private static SoundManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = (SoundManager)FindObjectOfType(typeof(SoundManager));
                    if (instance == null)
                    {
                        // 创建gameObject并添加组件
                        instance = (new GameObject("SoundManager")).AddComponent<SoundManager>();
                    }
                }
                return instance;
            }
        }

        static SoundManager()
        {
            Instance.Init();
        }

        /// <summary>
        /// 初始化声音管理器
        /// </summary>
        private void Init()
        {
            if (!initialized)
            {
                musicAudio = new Dictionary<int, Audio>();
                effsAudio = new Dictionary<int, Audio>();
                UISoundsAudio = new Dictionary<int, Audio>();
                audioPool = new Dictionary<int, Audio>();

                GlobalVolume = 1;
                GlobalMusicVolume = 1;
                GlobalEffsVolume = 1;
                GlobalUISoundsVolume = 1;

                IgnoreDuplicateMusic = false;
                IgnoreDuplicateEffs = false;
                IgnoreDuplicateUISounds = false;

                initialized = true;
                DontDestroyOnLoad(this);
            }
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        /// <summary>
        /// 加载新场景时触发的事件
        /// </summary>
        /// <param name="scene">加载的场景</param>
        /// <param name="mode">场景加载模式</param>
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // 停止并删除所有非持久性音频
            RemoveNonPersistAudio(musicAudio);
            RemoveNonPersistAudio(effsAudio);
            RemoveNonPersistAudio(UISoundsAudio);
        }

        private void Update()
        {
            UpdateAllAudio(musicAudio);
            UpdateAllAudio(effsAudio);
            UpdateAllAudio(UISoundsAudio);
        }

        /// <summary>
        /// 根据音频类型查找音频字典
        /// </summary>
        /// <param name="audioType">要返回的字典的音频类型</param>
        /// <returns>一个音频字典</returns>
        private static Dictionary<int, Audio> GetAudioTypeDictionary(Audio.AudioType audioType)
        {
            Dictionary<int, Audio> audioDict = new Dictionary<int, Audio>();
            switch (audioType)
            {
                case Audio.AudioType.Music:
                    audioDict = musicAudio;
                    break;
                case Audio.AudioType.Eff:
                    audioDict = effsAudio;
                    break;
                case Audio.AudioType.UISound:
                    audioDict = UISoundsAudio;
                    break;
            }

            return audioDict;
        }

        /// <summary>
        /// 查找指定音频类型的音频的忽略重复项
        /// </summary>
        /// <param name="audioType">返回的忽略重复项设置影响的音频类型</param>
        /// <returns>忽略重复项（bool）</returns>
        private static bool GetAudioTypeIgnoreDuplicateSetting(Audio.AudioType audioType)
        {
            switch (audioType)
            {
                case Audio.AudioType.Music:
                    return IgnoreDuplicateMusic;
                case Audio.AudioType.Eff:
                    return IgnoreDuplicateEffs;
                case Audio.AudioType.UISound:
                    return IgnoreDuplicateUISounds;
                default:
                    return false;
            }
        }

        /// <summary>
        /// 更新音频词典中所有音频的状态
        /// </summary>
        /// <param name="audioDict">要更新的音频词典</param>
        private static void UpdateAllAudio(Dictionary<int, Audio> audioDict)
        {
            //浏览所有音频并更新它们
            List<int> keys = new List<int>(audioDict.Keys);
            foreach (int key in keys)
            {
                Audio audio = audioDict[key];
                audio.Update();

                //如果它不再活动（正在播放），将其删除
                if (!audio.IsPlaying && !audio.Paused)
                {
                    Destroy(audio.AudioSource);

                    // 将其添加到音频池，以备将来需要引用
                    audioPool.Add(key, audio);
                    audio.Pooled = true;
                    audioDict.Remove(key);
                }
            }
        }

        /// <summary>
        /// 从音频字典中删除所有非持久性音频
        /// </summary>
        /// <param name="audioDict">要删除的非持久音频的音频词典</param>
        private static void RemoveNonPersistAudio(Dictionary<int, Audio> audioDict)
        {
            // 浏览所有音频，如果它们不应该在场景中持续，则将其删除
            List<int> keys = new List<int>(audioDict.Keys);
            foreach (int key in keys)
            {
                Audio audio = audioDict[key];
                if (!audio.Persist && audio.Activated)
                {
                    Destroy(audio.AudioSource);
                    audioDict.Remove(key);
                }
            }

            // 浏览音频库中的所有音频，如果它们不应在场景中持续存在，则将其删除
            keys = new List<int>(audioPool.Keys);
            foreach (int key in keys)
            {
                Audio audio = audioPool[key];
                if (!audio.Persist && audio.Activated)
                {
                    audioPool.Remove(key);
                }
            }
        }

        /// <summary>
        /// 还原并将合并的音频重新添加到其对应的音频字典中
        /// </summary>
        /// <param name="audioType">要还原的音频的音频类型</param>
        /// <param name="audioID">要还原的音频的ID</param>
        /// <returns>如果音频已还原，则为True；如果音频不在音频池中，则为false。</returns>
        public static bool RestoreAudioFromPool(Audio.AudioType audioType, int audioID)
        {
            if (audioPool.ContainsKey(audioID))
            {
                Dictionary<int, Audio> audioDict = GetAudioTypeDictionary(audioType);
                audioDict.Add(audioID, audioPool[audioID]);
                audioPool.Remove(audioID);

                return true;
            }

            return false;
        }

        #region GetAudio 函数

        /// <summary>
        /// 如果找到audioID，则返回其id为audioID的音频，如果未找到此类音频，则返回null
        /// </summary>
        /// <param name="audioID">要检索的音频的id</param>
        /// <returns>音频id为audioID，如果未找到此类音频，则为空</returns>
        public static Audio GetAudio(int audioID)
        {
            Audio audio;

            audio = GetMusicAudio(audioID);
            if (audio != null)
            {
                return audio;
            }

            audio = GetEffAudio(audioID);
            if (audio != null)
            {
                return audio;
            }

            audio = GetUISoundAudio(audioID);
            if (audio != null)
            {
                return audio;
            }

            return null;
        }

        /// <summary>
        /// 返回给定音频第一次播放的音频。如果未找到此类音频，则返回null
        /// </summary>
        /// <param name="audioClip">要检索的音频的音频</param>
        /// <returns>首次出现播放音频剪辑的音频，如果未找到此类音频，则为空</returns>
        public static Audio GetAudio(AudioClip audioClip)
        {
            Audio audio = GetMusicAudio(audioClip);
            if (audio != null)
            {
                return audio;
            }

            audio = GetEffAudio(audioClip);
            if (audio != null)
            {
                return audio;
            }

            audio = GetUISoundAudio(audioClip);
            if (audio != null)
            {
                return audio;
            }

            return null;
        }

        /// <summary>
        /// 如果找到audioID，则返回其id为audioID的音乐，如果未找到此类音频，则返回null
        /// </summary>
        /// <param name="audioID">要返回的音乐音频的id</param>
        /// <returns>如果找到audioID，则返回id的音乐音频，如果未找到此类音频，则为空</returns>
        public static Audio GetMusicAudio(int audioID)
        {
            return GetAudio(Audio.AudioType.Music, true, audioID);
        }

        /// <summary>
        /// 返回给定音频第一次播放的音频。如果未找到此类音频，则返回null
        /// </summary>
        public static Audio GetMusicAudio(AudioClip audioClip)
        {
            return GetAudio(Audio.AudioType.Music, true, audioClip);
        }

        /// <summary>
        /// 如果找到audioID，则返回其id为audioID的特效声音，如果找不到此类音频，则返回null
        /// </summary>
        public static Audio GetEffAudio(int audioID)
        {
            return GetAudio(Audio.AudioType.Eff, true, audioID);
        }

        /// <summary>
        /// 返回给定音频第一次播放的音频。如果未找到此类音频，则返回null
        /// </summary>
        public static Audio GetEffAudio(AudioClip audioClip)
        {
            return GetAudio(Audio.AudioType.Eff, true, audioClip);
        }

        /// <summary>
        /// 如果找到audioID，则返回以audioID为id的UI声音，如果未找到此类音频，则返回null
        /// </summary>
        public static Audio GetUISoundAudio(int audioID)
        {
            return GetAudio(Audio.AudioType.UISound, true, audioID);
        }

        /// <summary>
        /// 返回给定音频第一次播放的音频。如果未找到此类音频，则返回null
        /// </summary>
        public static Audio GetUISoundAudio(AudioClip audioClip)
        {
            return GetAudio(Audio.AudioType.UISound, true, audioClip);
        }

        private static Audio GetAudio(Audio.AudioType audioType, bool usePool, int audioID)
        {
            Dictionary<int, Audio> audioDict = GetAudioTypeDictionary(audioType);

            if (audioDict.ContainsKey(audioID))
            {
                return audioDict[audioID];
            }

            if (usePool && audioPool.ContainsKey(audioID) && audioPool[audioID].Type == audioType)
            {
                return audioPool[audioID];
            }

            return null;
        }

        private static Audio GetAudio(Audio.AudioType audioType, bool usePool, AudioClip audioClip)
        {
            Dictionary<int, Audio> audioDict = GetAudioTypeDictionary(audioType);

            List<int> audioTypeKeys = new List<int>(audioDict.Keys);
            List<int> poolKeys = new List<int>(audioPool.Keys);
            List<int> keys = usePool ? audioTypeKeys.Concat(poolKeys).ToList() : audioTypeKeys;
            foreach (int key in keys)
            {
                Audio audio = null;
                if (audioDict.ContainsKey(key))
                {
                    audio = audioDict[key];
                }
                else if (audioPool.ContainsKey(key))
                {
                    audio = audioPool[key];
                }
                if (audio == null)
                {
                    return null;
                }
                if (audio.Clip == audioClip && audio.Type == audioType)
                {
                    return audio;
                }
            }

            return null;
        }

        #endregion

        #region Prepare Function

        /// <summary>
        /// 准备和初始化背景音乐
        /// </summary>
        /// <param name="clip">要准备的音频</param>
        /// <returns>创建的音频对象的ID</returns>
        public static int PrepareMusic(AudioClip clip)
        {
            return PrepareAudio(Audio.AudioType.Music, clip, 1f, false, false, 1f, 1f, -1f, null);
        }

        /// <summary>
        /// 准备和初始化背景音乐
        /// </summary>
        /// <param name="clip">要准备的音频</param>
        /// <param name="volume"> 音量</param>
        /// <returns>创建的音频对象的ID</returns>
        public static int PrepareMusic(AudioClip clip, float volume)
        {
            return PrepareAudio(Audio.AudioType.Music, clip, volume, false, false, 1f, 1f, -1f, null);
        }

        /// <summary>
        /// 准备和初始化背景音乐
        /// </summary>
        /// <param name="clip">要准备的音频</param>
        /// <param name="volume"> 音量</param>
        /// <param name="loop">是否循环</param>
        /// <param name = "persist" > 场景更改音频是否持续存在</param>
        /// <returns>创建的音频对象的ID</returns>
        public static int PrepareMusic(AudioClip clip, float volume, bool loop, bool persist)
        {
            return PrepareAudio(Audio.AudioType.Music, clip, volume, loop, persist, 1f, 1f, -1f, null);
        }

        /// <summary>
        /// Prerpares and initializes background music
        /// </summary>
        /// <param name="clip">要准备的音频</param>
        /// <param name="volume"> 音量</param>
        /// <param name="loop">是否循环</param>
        /// <param name="persist"> 场景更改音频是否持续存在</param>
        /// <param name="fadeInValue">淡入时间（如果高于当前音量）</param>
        /// <param name="fadeOutValue"> 淡出时间（如果低于当前音量）</param>
        /// <returns>创建的音频对象的ID</returns>
        public static int PrepareMusic(AudioClip clip, float volume, bool loop, bool persist, float fadeInSeconds, float fadeOutSeconds)
        {
            return PrepareAudio(Audio.AudioType.Music, clip, volume, loop, persist, fadeInSeconds, fadeOutSeconds, -1f, null);
        }

        /// <summary>
        /// 准备和初始化背景音乐
        /// </summary>
        /// <param name="clip">要准备的音频</param>
        /// <param name="volume"> 音量</param>
        /// <param name="loop">是否循环</param>
        /// <param name="persist"> 场景更改音频是否持续存在</param>
        /// <param name="fadeInValue">淡入时间（如果高于当前音量）</param>
        /// <param name="fadeOutValue"> 淡出时间（如果低于当前音量）</param>
        /// <param name="currentMusicfadeOutSeconds"> 当前音频淡出需要多少秒。它将覆盖自己的淡出秒数。如果通过-1，当前音乐将保持自己的淡出秒数</param>
        /// <param name="sourceTransform">将音频改为3D音频。如果不需要3D音频，请使用null</param>
        /// <returns>创建的音频对象的ID</returns>
        public static int PrepareMusic(AudioClip clip, float volume, bool loop, bool persist, float fadeInSeconds, float fadeOutSeconds, float currentMusicfadeOutSeconds, Transform sourceTransform)
        {
            return PrepareAudio(Audio.AudioType.Music, clip, volume, loop, persist, fadeInSeconds, fadeOutSeconds, currentMusicfadeOutSeconds, sourceTransform);
        }

        /// <summary>
        /// 准备并初始化特效声音
        /// </summary>
        /// <param name="clip">要准备的音频</param>
        /// <returns>创建的音频对象的ID</returns>
        public static int PrepareEff(AudioClip clip)
        {
            return PrepareAudio(Audio.AudioType.Eff, clip, 1f, false, false, 0f, 0f, -1f, null);
        }

        /// <summary>
        /// 准备并初始化特效声音
        /// </summary>
        /// <param name="clip">要准备的音频</param>
        /// <param name="volume"> 音量</param>
        /// <returns>创建的音频对象的ID</returns>
        public static int PrepareEff(AudioClip clip, float volume)
        {
            return PrepareAudio(Audio.AudioType.Eff, clip, volume, false, false, 0f, 0f, -1f, null);
        }

        /// <summary>
        /// 准备并初始化特效声音
        /// </summary>
        /// <param name="clip">要准备的音频</param>
        /// <param name="loop">是否循环</param>
        /// <returns>创建的音频对象的ID</returns>
        public static int PrepareEff(AudioClip clip, bool loop)
        {
            return PrepareAudio(Audio.AudioType.Eff, clip, 1f, loop, false, 0f, 0f, -1f, null);
        }

        /// <summary>
        /// 准备并初始化特效声音
        /// </summary>
        /// <param name="clip">要准备的音频</param>
        /// <param name="volume"> 音量</param>
        /// <param name="loop">是否循环</param>
        /// <param name="sourceTransform">将音频改为3D音频。如果不需要3D音频，请使用null</param>
        /// <returns>创建的音频对象的ID</returns>
        public static int PrepareEff(AudioClip clip, float volume, bool loop, Transform sourceTransform)
        {
            return PrepareAudio(Audio.AudioType.Eff, clip, volume, loop, false, 0f, 0f, -1f, sourceTransform);
        }

        /// <summary>
        /// 准备并初始化UI声音
        /// </summary>
        /// <param name="clip">要准备的音频</param>
        /// <returns>创建的音频对象的ID</returns>
        public static int PrepareUISound(AudioClip clip)
        {
            return PrepareAudio(Audio.AudioType.UISound, clip, 1f, false, false, 0f, 0f, -1f, null);
        }

        /// <summary>
        /// 准备并初始化UI声音
        /// </summary>
        /// <param name="clip">要准备的音频</param>
        /// <param name="volume"> 音量</param>
        /// <returns>创建的音频对象的ID</returns>
        public static int PrepareUISound(AudioClip clip, float volume)
        {
            return PrepareAudio(Audio.AudioType.UISound, clip, volume, false, false, 0f, 0f, -1f, null);
        }

        private static int PrepareAudio(Audio.AudioType audioType, AudioClip clip, float volume, bool loop, bool persist, float fadeInSeconds, float fadeOutSeconds, float currentMusicfadeOutSeconds, Transform sourceTransform)
        {
            if (clip == null)
            {
                Debug.LogError("[Sound Manager] 音频不存在为null", clip);
            }

            Dictionary<int, Audio> audioDict = GetAudioTypeDictionary(audioType);
            bool ignoreDuplicateAudio = GetAudioTypeIgnoreDuplicateSetting(audioType);

            if (ignoreDuplicateAudio)
            {
                Audio duplicateAudio = GetAudio(audioType, true, clip);
                if (duplicateAudio != null)
                {
                    return duplicateAudio.AudioID;
                }
            }

            // 创建音频源
            Audio audio = new Audio(audioType, clip, loop, persist, volume, fadeInSeconds, fadeOutSeconds, sourceTransform);

            // 将其添加到词典
            audioDict.Add(audio.AudioID, audio);

            return audio.AudioID;
        }

        #endregion

        #region Play Functions

        /// <summary>
        /// 播放背景音乐
        /// </summary>
        /// <param name="clip">要播放的音频</param>
        /// <returns>创建的音频对象的ID</returns>
        public static int PlayMusic(AudioClip clip)
        {
            return PlayAudio(Audio.AudioType.Music, clip, 1f, false, false, 1f, 1f, -1f, null);
        }

        /// <summary>
        /// 播放背景音乐
        /// </summary>
        /// <param name="clip">要播放的音频</param>
        /// <param name="volume"> 音量</param>
        /// <returns>创建的音频对象的ID</returns>
        public static int PlayMusic(AudioClip clip, float volume)
        {
            return PlayAudio(Audio.AudioType.Music, clip, volume, false, false, 1f, 1f, -1f, null);
        }

        /// <summary>
        /// 播放背景音乐
        /// </summary>
        /// <param name="clip">要播放的音频</param>
        /// <param name="volume"> 音量</param>
        /// <param name="loop">是否循环</param>
        /// <param name = "persist" > 场景更改音频是否持续存在</param>
        /// <returns>创建的音频对象的ID</returns>
        public static int PlayMusic(AudioClip clip, float volume, bool loop, bool persist)
        {
            return PlayAudio(Audio.AudioType.Music, clip, volume, loop, persist, 1f, 1f, -1f, null);
        }

        /// <summary>
        /// 播放背景音乐
        /// </summary>
        /// <param name="clip">要播放的音频</param>
        /// <param name="volume"> 音量</param>
        /// <param name="loop">是否循环</param>
        /// <param name="persist"> 场景更改音频是否持续存在</param>
        /// <param name="fadeInSeconds">淡入时间（如果高于当前音量）</param>
        /// <param name="fadeOutSeconds"> 淡出时间（如果低于当前音量）</param>
        /// <returns>创建的音频对象的ID</returns>
        public static int PlayMusic(AudioClip clip, float volume, bool loop, bool persist, float fadeInSeconds, float fadeOutSeconds)
        {
            return PlayAudio(Audio.AudioType.Music, clip, volume, loop, persist, fadeInSeconds, fadeOutSeconds, -1f, null);
        }

        /// <summary>
        /// 播放背景音乐
        /// </summary>
        /// <param name="clip">要播放的音频</param>
        /// <param name="volume"> 音量</param>
        /// <param name="loop">是否循环</param>
        /// <param name="persist"> 场景更改音频是否持续存在</param>
        /// <param name="fadeInSeconds">淡入时间（如果高于当前音量）</param>
        /// <param name="fadeOutSeconds"> 淡出时间（如果低于当前音量）</param>
        /// <param name="currentMusicfadeOutSeconds"> 当前音乐音频淡出需要多少秒。它将覆盖自己的淡出秒数。如果通过-1，当前音乐将保持自己的淡出秒数</param>
        /// <param name="sourceTransform">将音频改为3D音频。如果不需要3D音频，请使用null</param>
        /// <returns>创建的音频对象的ID</returns>
        public static int PlayMusic(AudioClip clip, float volume, bool loop, bool persist, float fadeInSeconds, float fadeOutSeconds, float currentMusicfadeOutSeconds, Transform sourceTransform)
        {
            return PlayAudio(Audio.AudioType.Music, clip, volume, loop, persist, fadeInSeconds, fadeOutSeconds, currentMusicfadeOutSeconds, sourceTransform);
        }

        /// <summary>
        /// 播放声音效果
        /// </summary>
        /// <param name="clip">要播放的音频</param>
        /// <returns>创建的音频对象的ID</returns>
        public static int PlayEff(AudioClip clip)
        {
            return PlayAudio(Audio.AudioType.Eff, clip, 1f, false, false, 0f, 0f, -1f, null);
        }

        /// <summary>
        /// 播放声音效果
        /// </summary>
        /// <param name="clip">要播放的音频</param>
        /// <param name="volume"> 音量</param>
        /// <returns>创建的音频对象的ID</returns>
        public static int PlayEff(AudioClip clip, float volume)
        {
            return PlayAudio(Audio.AudioType.Eff, clip, volume, false, false, 0f, 0f, -1f, null);
        }

        /// <summary>
        /// 播放声音效果
        /// </summary>
        /// <param name="clip">要播放的音频</param>
        /// <param name="loop">是否循环</param>
        /// <returns>创建的音频对象的ID</returns>
        public static int PlayEff(AudioClip clip, bool loop)
        {
            return PlayAudio(Audio.AudioType.Eff, clip, 1f, loop, false, 0f, 0f, -1f, null);
        }

        /// <summary>
        /// 播放声音效果
        /// </summary>
        /// <param name="clip">要播放的音频</param>
        /// <param name="volume"> 音量</param>
        /// <param name="loop">是否循环</param>
        /// <param name="sourceTransform">将音频改为3D音频。如果不需要3D音频，请使用null</param>
        /// <returns>创建的音频对象的ID</returns>
        public static int PlayEff(AudioClip clip, float volume, bool loop, Transform sourceTransform)
        {
            return PlayAudio(Audio.AudioType.Eff, clip, volume, loop, false, 0f, 0f, -1f, sourceTransform);
        }

        /// <summary>
        /// 播放UI声音效果
        /// </summary>
        /// <param name="clip">要播放的音频</param>
        /// <returns>创建的音频对象的ID</returns>
        public static int PlayUISound(AudioClip clip)
        {
            return PlayAudio(Audio.AudioType.UISound, clip, 1f, false, false, 0f, 0f, -1f, null);
        }

        /// <summary>
        /// 播放UI声音效果
        /// </summary>
        /// <param name="clip">要播放的音频</param>
        /// <param name="volume"> 音量</param>
        /// <returns>创建的音频对象的ID</returns>
        public static int PlayUISound(AudioClip clip, float volume)
        {
            return PlayAudio(Audio.AudioType.UISound, clip, volume, false, false, 0f, 0f, -1f, null);
        }

        private static int PlayAudio(Audio.AudioType audioType, AudioClip clip, float volume, bool loop, bool persist, float fadeInSeconds, float fadeOutSeconds, float currentMusicfadeOutSeconds, Transform sourceTransform)
        {
            int audioID = PrepareAudio(audioType, clip, volume, loop, persist, fadeInSeconds, fadeOutSeconds, currentMusicfadeOutSeconds, sourceTransform);

            //停止播放所有当前音乐
            if (audioType == Audio.AudioType.Music)
            {
                StopAllMusic(currentMusicfadeOutSeconds);
            }

            GetAudio(audioType, false, audioID).Play();

            return audioID;
        }

        #endregion

        #region Stop Functions

        /// <summary>
        /// 停止播放所有声音
        /// </summary>
        public static void StopAll()
        {
            StopAll(-1f);
        }

        /// <summary>
        /// 停止播放所有声音
        /// </summary>
        /// <param name="musicFadeOutSeconds"> 所有音乐音频淡出需要多少秒。它将覆盖他们自己的淡出秒数。如果通过-1，则所有音乐将保持自己的淡出秒数</param>
        public static void StopAll(float musicFadeOutSeconds)
        {
            StopAllMusic(musicFadeOutSeconds);
            StopAllEff();
            StopAllUISounds();
        }

        /// <summary>
        /// 停止所有音乐
        /// </summary>
        public static void StopAllMusic()
        {
            StopAllAudio(Audio.AudioType.Music, -1f);
        }

        /// <summary>
        /// 停止所有音乐
        /// </summary>
        /// <param name="fadeOutSeconds"> 所有音频淡出需要多少秒。它将覆盖他们自己的淡出秒数。如果通过-1，则所有音乐将保持自己的淡出秒数</param>
        public static void StopAllMusic(float fadeOutSeconds)
        {
            StopAllAudio(Audio.AudioType.Music, fadeOutSeconds);
        }

        /// <summary>
        /// 停止所有特效声
        /// </summary>
        public static void StopAllEff()
        {
            StopAllAudio(Audio.AudioType.Eff, -1f);
        }

        /// <summary>
        /// 停止所有UI效果音
        /// </summary>
        public static void StopAllUISounds()
        {
            StopAllAudio(Audio.AudioType.UISound, -1f);
        }

        private static void StopAllAudio(Audio.AudioType audioType, float fadeOutSeconds)
        {
            Dictionary<int, Audio> audioDict = GetAudioTypeDictionary(audioType);

            List<int> keys = new List<int>(audioDict.Keys);
            foreach (int key in keys)
            {
                Audio audio = audioDict[key];
                if (fadeOutSeconds > 0)
                {
                    audio.FadeOutSeconds = fadeOutSeconds;
                }
                audio.Stop();
            }
        }

        #endregion

        #region Pause Functions

        /// <summary>
        /// 暂停所有声音
        /// </summary>
        public static void PauseAll()
        {
            PauseAllMusic();
            PauseAllEff();
            PauseAllUISounds();
        }

        /// <summary>
        /// 暂停所有音乐
        /// </summary>
        public static void PauseAllMusic()
        {
            PauseAllAudio(Audio.AudioType.Music);
        }

        /// <summary>
        /// 暂停所有特效音
        /// </summary>
        public static void PauseAllEff()
        {
            PauseAllAudio(Audio.AudioType.Eff);
        }

        /// <summary>
        /// 暂停所有UI效果音
        /// </summary>
        public static void PauseAllUISounds()
        {
            PauseAllAudio(Audio.AudioType.UISound);
        }

        private static void PauseAllAudio(Audio.AudioType audioType)
        {
            Dictionary<int, Audio> audioDict = GetAudioTypeDictionary(audioType);

            List<int> keys = new List<int>(audioDict.Keys);
            foreach (int key in keys)
            {
                Audio audio = audioDict[key];
                audio.Pause();
            }
        }

        #endregion

        #region Resume Functions

        /// <summary>
        /// 恢复所有音频播放
        /// </summary>
        public static void ResumeAll()
        {
            ResumeAllMusic();
            ResumeAllEff();
            ResumeAllUISounds();
        }

        /// <summary>
        /// 恢复所有音乐播放
        /// </summary>
        public static void ResumeAllMusic()
        {
            ResumeAllAudio(Audio.AudioType.Music);
        }

        /// <summary>
        /// 恢复所有特效音播放
        /// </summary>
        public static void ResumeAllEff()
        {
            ResumeAllAudio(Audio.AudioType.Eff);
        }

        /// <summary>
        /// 恢复所有UI音效播放
        /// </summary>
        public static void ResumeAllUISounds()
        {
            ResumeAllAudio(Audio.AudioType.UISound);
        }

        private static void ResumeAllAudio(Audio.AudioType audioType)
        {
            Dictionary<int, Audio> audioDict = GetAudioTypeDictionary(audioType);

            List<int> keys = new List<int>(audioDict.Keys);
            foreach (int key in keys)
            {
                Audio audio = audioDict[key];
                audio.Resume();
            }
        }

        #endregion
    }
}
