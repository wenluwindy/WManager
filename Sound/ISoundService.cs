using UnityEngine;
using UnityEngine.Audio;

namespace WManager
{
    /// <summary>
    /// 声音服务抽象。业务模块通过此接口访问音频功能，
    /// 不需要直接依赖 SoundManager。
    /// </summary>
    public interface ISoundService
    {
        /// <summary>
        /// 全局音量（影响所有通道）
        /// </summary>
        float GlobalVolume { get; set; }

        /// <summary>
        /// 音乐音量
        /// </summary>
        float MusicVolume { get; set; }

        /// <summary>
        /// 特效音量
        /// </summary>
        float EffVolume { get; set; }

        /// <summary>
        /// UI 音效音量
        /// </summary>
        float UISoundVolume { get; set; }

        /// <summary>
        /// 播放音乐
        /// </summary>
        int PlayMusic(AudioClip clip, float volume = 1f, bool loop = true, bool persist = false);

        /// <summary>
        /// 播放特效音
        /// </summary>
        int PlayEff(AudioClip clip, float volume = 1f);

        /// <summary>
        /// 播放 UI 音效
        /// </summary>
        int PlayUISound(AudioClip clip, float volume = 1f);

        /// <summary>
        /// 停止所有音效（音乐可选）
        /// </summary>
        void StopAll(bool includeMusic = true);

        /// <summary>
        /// 音量变更事件
        /// </summary>
        event System.Action<SoundChannel> VolumeChanged;
    }
}