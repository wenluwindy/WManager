using UnityEngine;

namespace WManager
{
    /// <summary>
    /// 音频对象
    /// </summary>
    public class Audio
    {
        /// <summary>
        /// 音频的ID
        /// </summary>
        public int AudioID { get; private set; }

        /// <summary>
        /// 音频的类型
        /// </summary>
        public AudioType Type { get; private set; }

        /// <summary>
        /// 当前是否正在播放音频
        /// </summary>
        public bool IsPlaying { get; private set; }

        /// <summary>
        /// 暂停
        /// </summary>
        public bool Paused { get; private set; }

        /// <summary>
        /// 停止
        /// </summary>
        public bool Stopping { get; private set; }

        /// <summary>
        /// 是否至少创建和更新一次音频。
        /// </summary>
        public bool Activated { get; private set; }

        /// <summary>
        /// 音频当前是否已合并。不要修改此值，它是由SoundManager专门使用的。
        /// </summary>
        public bool Pooled { get; set; }

        /// <summary>
        /// 此音频的音量
        /// </summary>
        public float Volume { get; private set; }

        /// <summary>
        /// 负责此音频的音频源。不可直接修改音频源！
        /// </summary>
        public AudioSource AudioSource { get; private set; }

        /// <summary>
        /// 音频的位置
        /// </summary>
        public Transform SourceTransform
        {
            get { return sourceTransform; }
            set
            {
                if (value == null)
                {
                    sourceTransform = SoundManager.Gameobject.transform;
                }
                else
                {
                    sourceTransform = value;
                }
            }
        }

        /// <summary>
        /// 要播放/正在播放的音频剪辑
        /// </summary>
        public AudioClip Clip
        {
            get { return clip; }
            set
            {
                clip = value;
                if (AudioSource != null)
                {
                    AudioSource.clip = clip;
                }
            }
        }

        /// <summary>
        /// 循环
        /// </summary>
        public bool Loop
        {
            get { return loop; }
            set
            {
                loop = value;
                if (AudioSource != null)
                {
                    AudioSource.loop = loop;
                }
            }
        }

        /// <summary>
        /// 静音
        /// </summary>
        public bool Mute
        {
            get { return mute; }
            set
            {
                mute = value;
                if (AudioSource != null)
                {
                    AudioSource.mute = mute;
                }
            }
        }

        /// <summary>
        /// 设置音频的优先级
        /// </summary>
        public int Priority
        {
            get { return priority; }
            set
            {
                priority = Mathf.Clamp(value, 0, 256);
                if (AudioSource != null)
                {
                    AudioSource.priority = priority;
                }
            }
        }

        /// <summary>
        /// 音频的音调
        /// </summary>
        public float Pitch
        {
            get { return pitch; }
            set
            {
                pitch = Mathf.Clamp(value, -3, 3);
                if (AudioSource != null)
                {
                    AudioSource.pitch = pitch;
                }
            }
        }

        /// <summary>
        /// 以立体声方式（向左或向右）平移播放声音。这仅适用于单声道或立体声的声音。
        /// </summary>
        public float StereoPan
        {
            get { return stereoPan; }
            set
            {
                stereoPan = Mathf.Clamp(value, -1, 1);
                if (AudioSource != null)
                {
                    AudioSource.panStereo = stereoPan;
                }
            }
        }

        /// <summary>
        /// 设置此AudioSource受3D空间化计算（衰减、多普勒等）的影响程度。0.0表示声音为2D，1.0表示声音为3D。
        /// </summary>
        public float SpatialBlend
        {
            get { return spatialBlend; }
            set
            {
                spatialBlend = Mathf.Clamp01(value);
                if (AudioSource != null)
                {
                    AudioSource.spatialBlend = spatialBlend;
                }
            }
        }

        /// <summary>
        /// 来自AudioSource的信号将混合到与混响区相关的全局混响中的量。
        /// </summary>
        public float ReverbZoneMix
        {
            get { return reverbZoneMix; }
            set
            {
                reverbZoneMix = Mathf.Clamp(value, 0, 1.1f);
                if (AudioSource != null)
                {
                    AudioSource.reverbZoneMix = reverbZoneMix;
                }
            }
        }

        /// <summary>
        /// 音频的多普勒级别
        /// </summary>
        public float DopplerLevel
        {
            get { return dopplerLevel; }
            set
            {
                dopplerLevel = Mathf.Clamp(value, 0, 5);
                if (AudioSource != null)
                {
                    AudioSource.dopplerLevel = dopplerLevel;
                }
            }
        }

        /// <summary>
        /// 扬声器空间中三维立体声或多声道声音的扩散角（以度为单位）。
        /// </summary>
        public float Spread
        {
            get { return spread; }
            set
            {
                spread = Mathf.Clamp(value, 0, 360);
                if (AudioSource != null)
                {
                    AudioSource.spread = spread;
                }
            }
        }

        /// <summary>
        /// 音频随距离衰减模式
        /// </summary>
        public AudioRolloffMode RolloffMode
        {
            get { return rolloffMode; }
            set
            {
                rolloffMode = value;
                if (AudioSource != null)
                {
                    AudioSource.rolloffMode = rolloffMode;
                }
            }
        }

        /// <summary>
        /// （对数衰减）声音衰减最大距离。
        /// </summary>
        public float Max3DDistance
        {
            get { return max3DDistance; }
            set
            {
                max3DDistance = Mathf.Max(value, 0.01f);
                if (AudioSource != null)
                {
                    AudioSource.maxDistance = max3DDistance;
                }
            }
        }

        /// <summary>
        /// 在最小距离内，音频音量将停止变大。
        /// </summary>
        public float Min3DDistance
        {
            get { return min3DDistance; }
            set
            {
                min3DDistance = Mathf.Max(value, 0);
                if (AudioSource != null)
                {
                    AudioSource.minDistance = min3DDistance;
                }
            }
        }

        /// <summary>
        /// 场景更改之间音频是否持续存在
        /// </summary>
        public bool Persist { get; set; }

        /// <summary>
        /// 音频淡入/达到目标音量需要多少秒（如果高于当前音量）
        /// </summary>
        public float FadeInSeconds { get; set; }

        /// <summary>
        /// 音频淡出/达到目标音量需要多少秒（如果低于当前音量）
        /// </summary>
        public float FadeOutSeconds { get; set; }

        /// <summary>
        /// 音频类型
        /// </summary>
        public enum AudioType
        {
            Music,
            Eff,
            UISound
        }

        private static int audioCounter = 0;

        private AudioClip clip;
        private bool loop;
        private bool mute;
        private int priority;
        private float pitch;
        private float stereoPan;
        private float spatialBlend;
        private float reverbZoneMix;
        private float dopplerLevel;
        private float spread;
        private AudioRolloffMode rolloffMode;
        private float max3DDistance;
        private float min3DDistance;

        private float targetVolume;
        private float initTargetVolume;
        private float tempFadeSeconds;
        private float fadeInterpolater;
        private float onFadeStartVolume;
        private Transform sourceTransform;

        public Audio(AudioType audioType, AudioClip clip, bool loop, bool persist, float volume, float fadeInValue, float fadeOutValue, Transform sourceTransform)
        {
            // 设置唯一音频ID
            AudioID = audioCounter;
            audioCounter++;

            // 初始化值
            this.Type = audioType;
            this.Clip = clip;
            this.SourceTransform = sourceTransform;
            this.Loop = loop;
            this.Persist = persist;
            this.targetVolume = volume;
            this.initTargetVolume = volume;
            this.tempFadeSeconds = -1;
            this.FadeInSeconds = fadeInValue;
            this.FadeOutSeconds = fadeOutValue;

            Volume = 0f;
            Pooled = false;

            // 设置音频源默认值
            Mute = false;
            Priority = 128;
            Pitch = 1;
            StereoPan = 0;
            if (sourceTransform != null && sourceTransform != SoundManager.Gameobject.transform)
            {
                SpatialBlend = 1;
            }
            ReverbZoneMix = 1;
            DopplerLevel = 1;
            Spread = 0;
            RolloffMode = AudioRolloffMode.Logarithmic;
            Min3DDistance = 1;
            Max3DDistance = 500;

            // 初始化状态
            IsPlaying = false;
            Paused = false;
            Activated = false;
        }

        /// <summary>
        /// 使用适当的值创建和初始化音频源组件
        /// </summary>
        private void CreateAudiosource()
        {
            AudioSource = SourceTransform.gameObject.AddComponent<AudioSource>() as AudioSource;
            AudioSource.clip = Clip;
            AudioSource.loop = Loop;
            AudioSource.mute = Mute;
            AudioSource.volume = Volume;
            AudioSource.priority = Priority;
            AudioSource.pitch = Pitch;
            AudioSource.panStereo = StereoPan;
            AudioSource.spatialBlend = SpatialBlend;
            AudioSource.reverbZoneMix = ReverbZoneMix;
            AudioSource.dopplerLevel = DopplerLevel;
            AudioSource.spread = Spread;
            AudioSource.rolloffMode = RolloffMode;
            AudioSource.maxDistance = Max3DDistance;
            AudioSource.minDistance = Min3DDistance;
        }

        /// <summary>
        /// 从头开始播放音频
        /// </summary>
        public void Play()
        {
            Play(initTargetVolume);
        }

        /// <summary>
        /// 从头开始播放音频
        /// </summary>
        /// <param name="volume">音量</param>
        public void Play(float volume)
        {
            // 检查声音管理器中是否仍存在音频
            if (Pooled)
            {
                // 如果没有，就从音频池恢复
                bool restoredFromPool = SoundManager.RestoreAudioFromPool(Type, AudioID);
                if (!restoredFromPool)
                {
                    return;
                }

                Pooled = true;
            }

            // 如果音频源不存在，则重新创建
            if (AudioSource == null)
            {
                CreateAudiosource();
            }

            AudioSource.Play();
            IsPlaying = true;

            fadeInterpolater = 0f;
            onFadeStartVolume = this.Volume;
            targetVolume = volume;
        }

        /// <summary>
        /// 停止播放
        /// </summary>
        public void Stop()
        {
            fadeInterpolater = 0f;
            onFadeStartVolume = Volume;
            targetVolume = 0f;

            Stopping = true;
        }

        /// <summary>
        /// 暂停播放
        /// </summary>
        public void Pause()
        {
            AudioSource.Pause();
            Paused = true;
        }

        /// <summary>
        /// 继续播放音频
        /// </summary>
        public void UnPause()
        {
            AudioSource.UnPause();
            Paused = false;
        }

        /// <summary>
        /// 继续播放音频
        /// </summary>
        public void Resume()
        {
            AudioSource.UnPause();
            Paused = false;
        }

        /// <summary>
        /// 设置音频音量
        /// </summary>
        /// <param name="volume">音量</param>
        public void SetVolume(float volume)
        {
            if (volume > targetVolume)
            {
                SetVolume(volume, FadeOutSeconds);
            }
            else
            {
                SetVolume(volume, FadeInSeconds);
            }
        }

        /// <summary>
        /// 设置音频音量
        /// </summary>
        /// <param name="volume">音量</param>
        /// <param name="fadeSeconds">音频淡入/淡出需要多少秒才能达到目标音量。如果通过，它将覆盖音频的淡入/淡出秒数，但仅用于此转换</param>
        public void SetVolume(float volume, float fadeSeconds)
        {
            SetVolume(volume, fadeSeconds, this.Volume);
        }

        /// <summary>
        /// 设置音频音量
        /// </summary>
        /// <param name="volume">音量</param>
        /// <param name="fadeSeconds">音频淡入/淡出需要多少秒才能达到目标音量。如果通过，它将覆盖音频的淡入/淡出秒数，但仅用于此转换</param>
        /// <param name="startVolume">在开始淡入之前，立即将音量设置为该值。如果未通过，音频将开始从当前音量向目标音量衰减</param>
        public void SetVolume(float volume, float fadeSeconds, float startVolume)
        {
            targetVolume = Mathf.Clamp01(volume);
            fadeInterpolater = 0;
            onFadeStartVolume = startVolume;
            tempFadeSeconds = fadeSeconds;
        }

        /// <summary>
        /// 设置音频3D距离
        /// </summary>
        /// <param name="min">最小距离</param>
        /// <param name="max">最大距离</param>
        public void Set3DDistances(float min, float max)
        {
            Min3DDistance = min;
            Max3DDistance = max;
        }

        /// <summary>
        /// 音频的更新循环。这是从声音管理器本身自动调用的。不要在其他地方使用此功能。
        /// </summary>
        public void Update()
        {
            if (AudioSource == null)
            {
                return;
            }

            Activated = true;

            // 增加/减少音量以达到当前目标
            if (Volume != targetVolume)
            {
                float fadeValue;
                fadeInterpolater += Time.unscaledDeltaTime;
                if (Volume > targetVolume)
                {
                    fadeValue = tempFadeSeconds != -1 ? tempFadeSeconds : FadeOutSeconds;
                }
                else
                {
                    fadeValue = tempFadeSeconds != -1 ? tempFadeSeconds : FadeInSeconds;
                }

                Volume = Mathf.Lerp(onFadeStartVolume, targetVolume, fadeInterpolater / fadeValue);
            }
            else if (tempFadeSeconds != -1)
            {
                tempFadeSeconds = -1;
            }

            // 设置音量，同时考虑全局音量。
            switch (Type)
            {
                case AudioType.Music:
                    {
                        AudioSource.volume = Volume * SoundManager.GlobalMusicVolume * SoundManager.GlobalVolume;
                        break;
                    }
                case AudioType.Eff:
                    {
                        AudioSource.volume = Volume * SoundManager.GlobalEffsVolume * SoundManager.GlobalVolume;
                        break;
                    }
                case AudioType.UISound:
                    {
                        AudioSource.volume = Volume * SoundManager.GlobalUISoundsVolume * SoundManager.GlobalVolume;
                        break;
                    }
            }

            // 如果音频完成停止过程，则完全停止音频
            if (Volume == 0f && Stopping)
            {
                AudioSource.Stop();
                Stopping = false;
                IsPlaying = false;
                Paused = false;
            }

            // 更新播放状态
            if (AudioSource.isPlaying != IsPlaying && Application.isFocused)
            {
                IsPlaying = AudioSource.isPlaying;
            }
        }
    }
}
