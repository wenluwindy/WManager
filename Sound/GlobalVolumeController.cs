using UnityEngine;

namespace WManager
{
    /// <summary>
    /// 全局音量控制器：作为 UI（Slider）的桥梁，把 Inspector 或 Slider 的音量值同步给 VolumeController。
    /// 改为订阅 VolumeController.VolumeChanged 事件而非每帧轮询，避免无效 CPU 开销。
    /// </summary>
    [AddComponentMenu("管理器/全局音量控制器")]
    public class GlobalVolumeController : MonoBehaviour
    {
        [Tooltip("总音量")]
        [Range(0, 1)]
        public float GVolume = 1;
        [Tooltip("总特效音量")]
        [Range(0, 1)]
        public float GEffVolume = 1;
        [Tooltip("总音乐音量")]
        [Range(0, 1)]
        public float GMusicVolume = 1;
        [Tooltip("总UI效果音量")]
        [Range(0, 1)]
        public float GUIVolume = 1;

        private void OnEnable()
        {
            VolumeController.VolumeChanged += OnVolumeChanged;
            // 启动时主动同步一次（覆盖 Play Mode 之前的状态）
            ApplyAll();
        }

        private void OnDisable()
        {
            VolumeController.VolumeChanged -= OnVolumeChanged;
        }

#if UNITY_EDITOR
        // Inspector 上直接修改字段时同步给 VolumeController
        private void OnValidate()
        {
            if (!Application.isPlaying) return;
            ApplyAll();
        }
#endif

        private void OnVolumeChanged(SoundChannel channel)
        {
            switch (channel)
            {
                case SoundChannel.Global: GlobalVolume(GVolume); break;
                case SoundChannel.Music: GlobalMusicVolume(GMusicVolume); break;
                case SoundChannel.Eff: GlobalEffVolume(GEffVolume); break;
                case SoundChannel.UISound: GlobalUIVolume(GUIVolume); break;
            }
        }

        private void ApplyAll()
        {
            GlobalVolume(GVolume);
            GlobalMusicVolume(GMusicVolume);
            GlobalEffVolume(GEffVolume);
            GlobalUIVolume(GUIVolume);
        }

        /// <summary>
        /// 全局音量,绑定Slider
        /// </summary>
        public void GlobalVolume(float volume) => SoundManager.GlobalVolume = volume;

        /// <summary>
        /// 全局效果音量，绑定Slider
        /// </summary>
        public void GlobalEffVolume(float volume) => SoundManager.GlobalEffsVolume = volume;

        /// <summary>
        /// 全局音乐音量，绑定Slider
        /// </summary>
        public void GlobalMusicVolume(float volume) => SoundManager.GlobalMusicVolume = volume;

        /// <summary>
        /// 全局UI特效音量，绑定Slider
        /// </summary>
        public void GlobalUIVolume(float volume) => SoundManager.GlobalUISoundsVolume = volume;
    }
}