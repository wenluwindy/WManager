// ----------------------------------------------------------------------------
// SoundExamples.cs
//
// 演示 SoundManager（Music / Eff / UISound 三类通道）和 VolumeController。
// - 静默调用：SoundManager.PlayMusic / PlayEff / PlayUISound
// - 静态属性直接调节：SoundManager.GlobalMusicVolume 等
// - 订阅 VolumeController.VolumeChanged 实现 UI 同步
// ----------------------------------------------------------------------------
using UnityEngine;
using UnityEngine.UI;
using WManager;

namespace WManager.Example
{
    public class SoundExample : MonoBehaviour
    {
        [Header("音频片段（直接拖 AudioClip 进来）")]
        [SerializeField] private AudioClip bgm;
        [SerializeField] private AudioClip uiClick;
        [SerializeField] private AudioClip skillEff;

        [Header("UI")]
        [SerializeField] private Slider musicSlider;
        [SerializeField] private Slider effSlider;
        [SerializeField] private Slider uiSlider;
        [SerializeField] private Slider globalSlider;
        [SerializeField] private Button playMusicButton;
        [SerializeField] private Button stopAllButton;
        [SerializeField] private Button playUiButton;
        [SerializeField] private Button playEffButton;

        private void OnEnable()
        {
            // UI 切到静音/音量条时调用：设进 VolumeController
            globalSlider?.onValueChanged.AddListener(v => SoundManager.GlobalVolume = v);
            musicSlider?.onValueChanged.AddListener(v => SoundManager.GlobalMusicVolume = v);
            effSlider?.onValueChanged.AddListener(v => SoundManager.GlobalEffsVolume = v);
            uiSlider?.onValueChanged.AddListener(v => SoundManager.GlobalUISoundsVolume = v);

            playMusicButton?.onClick.AddListener(OnClickPlayMusic);
            stopAllButton?.onClick.AddListener(OnClickStopAll);
            playUiButton?.onClick.AddListener(OnClickPlayUi);
            playEffButton?.onClick.AddListener(OnClickPlayEff);

            // 订阅 VolumeController：UI 拖动时 Slider 的值也跟着刷新（多端同步）
            VolumeController.VolumeChanged += OnVolumeChanged;

            // 启动时把 Slider 拉到当前值
            SyncSliders();
        }

        private void OnDisable()
        {
            globalSlider?.onValueChanged.RemoveAllListeners();
            musicSlider?.onValueChanged.RemoveAllListeners();
            effSlider?.onValueChanged.RemoveAllListeners();
            uiSlider?.onValueChanged.RemoveAllListeners();

            playMusicButton?.onClick.RemoveAllListeners();
            stopAllButton?.onClick.RemoveAllListeners();
            playUiButton?.onClick.RemoveAllListeners();
            playEffButton?.onClick.RemoveAllListeners();

            VolumeController.VolumeChanged -= OnVolumeChanged;
        }

        // ===== 按钮回调 =====
        private void OnClickPlayMusic()
        {
            // PlayMusic(AudioClip, volume, loop, persist, fadeInSeconds, fadeOutSeconds, currentMusicFadeOutSeconds, sourceTransform)
            // 完整签名；最常用的是下面这个简写
            if (bgm == null) return;
            SoundManager.PlayMusic(bgm, volume: 1f, loop: true, persist: true);
        }

        private void OnClickPlayUi()
        {
            SoundManager.PlayUISound(uiClick);
        }

        private void OnClickPlayEff()
        {
            // 3D 音效：把发声位置传进去
            // SoundManager.PlayEff(skillEff, volume: 1f, loop: false, sourceTransform: transform);

            // 普通 2D 音效
            SoundManager.PlayEff(skillEff);
        }

        private void OnClickStopAll()
        {
            // 全部停止：可选传入 musicFadeOutSeconds 让音乐淡出
            SoundManager.StopAll(musicFadeOutSeconds: 1.5f);
        }

        private void OnVolumeChanged(SoundChannel channel)
        {
            // 收到通道变化 → 同步 Slider 位置
            switch (channel)
            {
                case SoundChannel.Global:
                    if (globalSlider != null) globalSlider.SetValueWithoutNotify(VolumeController.Global);
                    break;
                case SoundChannel.Music:
                    if (musicSlider != null) musicSlider.SetValueWithoutNotify(VolumeController.Music);
                    break;
                case SoundChannel.Eff:
                    if (effSlider != null) effSlider.SetValueWithoutNotify(VolumeController.Eff);
                    break;
                case SoundChannel.UISound:
                    if (uiSlider != null) uiSlider.SetValueWithoutNotify(VolumeController.UISound);
                    break;
            }
        }

        private void SyncSliders()
        {
            if (globalSlider != null) globalSlider.SetValueWithoutNotify(VolumeController.Global);
            if (musicSlider != null) musicSlider.SetValueWithoutNotify(VolumeController.Music);
            if (effSlider != null) effSlider.SetValueWithoutNotify(VolumeController.Eff);
            if (uiSlider != null) uiSlider.SetValueWithoutNotify(VolumeController.UISound);
        }

        // ============================================================================
        // 在 Inspector 上拖一个 GlobalVolumeController 组件，自动把音量变化同步给所有通道
        // ============================================================================
        public void AddGlobalVolumeController()
        {
            var go = new GameObject("VolumeController");
            go.AddComponent<GlobalVolumeController>();
            // 之后在 Inspector 上调 GVolume / GMusicVolume / GEffVolume / GUIVolume 即可
        }

        // ============================================================================
        // 通道音量相乘的关系：实际播放音量 = GlobalVolume * ChannelVolume
        // ============================================================================
        public void ExplainVolumeComposition()
        {
            // 设 Global = 0.5, Music = 0.8，则 Music 通道实际输出 = 0.5 * 0.8 = 0.4
            // 想"全部静音"必须 Global = 0，只设某个通道不会全静音

            SoundManager.GlobalVolume = 0f;          // 全局静音
            SoundManager.GlobalMusicVolume = 0f;    // 仅静音音乐
            SoundManager.GlobalEffsVolume = 0f;     // 仅静音特效
            SoundManager.GlobalUISoundsVolume = 0f; // 仅静音 UI 音效

            SoundManager.GlobalVolume = 1f;         // 恢复全局
            VolumeController.ResetToDefault();      // 全部通道回到 1
        }

        // ============================================================================
        // 全场景控制
        // ============================================================================
        public void PauseResumeExample()
        {
            // 玩家切到暂停菜单时
            SoundManager.PauseAllMusic();

            // 恢复
            SoundManager.ResumeAllMusic();
        }
    }
}
