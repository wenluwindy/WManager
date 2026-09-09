using System;

namespace WManager
{
    /// <summary>
    /// 音量控制器：管理总音量 + 三个通道（Music/Eff/UISound）。
    /// 提供 VolumeChanged 事件，UI/调试器可订阅，避免每帧轮询。
    /// </summary>
    public static class VolumeController
    {
        private static float _global = 1f;
        private static float _music = 1f;
        private static float _eff = 1f;
        private static float _uiSound = 1f;

        /// <summary>
        /// 全局音量（与三个通道相乘）
        /// </summary>
        public static float Global
        {
            get => _global;
            set
            {
                value = Clamp01(value);
                if (Math.Abs(_global - value) > 0.0001f)
                {
                    _global = value;
                    OnVolumeChanged(SoundChannel.Global);
                }
            }
        }

        /// <summary>
        /// 音乐音量
        /// </summary>
        public static float Music
        {
            get => _music;
            set
            {
                value = Clamp01(value);
                if (Math.Abs(_music - value) > 0.0001f)
                {
                    _music = value;
                    OnVolumeChanged(SoundChannel.Music);
                }
            }
        }

        /// <summary>
        /// 特效音量
        /// </summary>
        public static float Eff
        {
            get => _eff;
            set
            {
                value = Clamp01(value);
                if (Math.Abs(_eff - value) > 0.0001f)
                {
                    _eff = value;
                    OnVolumeChanged(SoundChannel.Eff);
                }
            }
        }

        /// <summary>
        /// UI 音效音量
        /// </summary>
        public static float UISound
        {
            get => _uiSound;
            set
            {
                value = Clamp01(value);
                if (Math.Abs(_uiSound - value) > 0.0001f)
                {
                    _uiSound = value;
                    OnVolumeChanged(SoundChannel.UISound);
                }
            }
        }

        /// <summary>
        /// 音量变更事件。订阅者收到的是发生变化的通道类型。
        /// </summary>
        public static event Action<SoundChannel> VolumeChanged;

        /// <summary>
        /// 把所有通道音量重置为 1（用于"恢复默认"按钮等场景）
        /// </summary>
        public static void ResetToDefault()
        {
            Global = 1f;
            Music = 1f;
            Eff = 1f;
            UISound = 1f;
        }

        private static void OnVolumeChanged(SoundChannel channel)
        {
            VolumeChanged?.Invoke(channel);
        }

        private static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);
    }

    /// <summary>
    /// 声音通道类型
    /// </summary>
    public enum SoundChannel
    {
        Global,
        Music,
        Eff,
        UISound
    }
}