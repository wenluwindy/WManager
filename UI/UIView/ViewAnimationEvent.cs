using System;
using UnityEngine.Events;
using UnityEngine;

namespace WManager.UI
{
    /// <summary>
    /// 视图动画事件
    /// </summary>
    [Serializable]
    public class ViewAnimationEvent
    {
        public ViewAnimation animation;

        public UnityEvent onBeganEvent;

        public UnityEvent onEndEvent;

        public AudioClip onBeganSound;

        public AudioClip onEndSound;
    }
}