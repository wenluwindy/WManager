using System;
using UnityEngine;
using Sirenix.OdinInspector;

namespace WManager
{
    /// <summary>
    /// 全局音量控制器
    /// </summary>
    [AddComponentMenu("管理器/全局音量控制器")]
    public class GlobalVolumeController : MonoBehaviour
    {
        [LabelText("总音量"), InfoBox("可绑定Slider进行调节,在方法中选择：GlobalVolume，GlobalEffVolume，GlobalMusicVolume，GlobalUIVolume")]
        [Range(0, 1)]
        public float GVolume = 1;
        [LabelText("总特效音量")]
        [Range(0, 1)]
        public float GEffVolume = 1;
        [LabelText("总音乐音量")]
        [Range(0, 1)]
        public float GMusicVolume = 1;
        [LabelText("总UI效果音量")]
        [Range(0, 1)]
        public float GUIVolume = 1;
        float a = 1;
        float b = 1;
        float c = 1;
        float d = 1;
        /// <summary>
        /// 判断当前在用什么调节音量
        /// 使用检查器面板时传入的音量为-1
        /// 使用Slider调节时传入的音量为Slider的值
        /// </summary>
        private void Update()
        {
            if (GVolume != a)
            {
                GlobalVolume();
                a = GVolume;
            }
            if (GEffVolume != b)
            {
                GlobalEffVolume();
                b = GEffVolume;
            }
            if (GMusicVolume != c)
            {
                GlobalMusicVolume();
                c = GMusicVolume;
            }
            if (GUIVolume != d)
            {
                GlobalUIVolume();
                d = GEffVolume;
            }
        }
        /// <summary>
        /// 全局音量,绑定Slider
        /// </summary>
        /// <param name="volume"></param>
        public void GlobalVolume(float volume = -1)
        {
            if (volume == -1)
            {
                SoundManager.GlobalVolume = GVolume;
            }
            else
            {
                SoundManager.GlobalVolume = volume;
            }
        }
        /// <summary>
        /// 全局效果音量，绑定Slider
        /// </summary>
        /// <param name="volume"></param>
        public void GlobalEffVolume(float volume = -1)
        {
            if (volume == -1)
            {
                SoundManager.GlobalEffsVolume = GEffVolume;
            }
            else
            {
                SoundManager.GlobalEffsVolume = volume;
            }
        }
        /// <summary>
        /// 全局音乐音量，绑定Slider
        /// </summary>
        /// <param name="volume"></param>
        public void GlobalMusicVolume(float volume = -1)
        {
            if (volume == -1)
            {
                SoundManager.GlobalMusicVolume = GMusicVolume;
            }
            else
            {
                SoundManager.GlobalMusicVolume = volume;
            }

        }
        /// <summary>
        /// 全局UI特效音量，绑定Slider
        /// </summary>
        /// <param name="volume"></param>
        public void GlobalUIVolume(float volume = -1)
        {
            if (volume == -1)
            {
                SoundManager.GlobalUISoundsVolume = GEffVolume;
            }
            else
            {
                SoundManager.GlobalUISoundsVolume = volume;
            }

        }
    }
}
