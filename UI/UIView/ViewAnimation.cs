using System;
using UnityEngine;
using UnityEngine.Events;
using WManager;

namespace WManager.UI
{
    [Serializable]
    public class ViewAnimation
    {
        public AnimationType type = AnimationType.Tween;

        public TweenAnimations animations;

        public string stateName;

        public IActionChain Play(UIView view, bool instant = false, UnityAction callback = null)
        {
            switch (type)
            {
                case AnimationType.Tween:
                    return animations.Play(view, view.transform as RectTransform, view.GetComponent<CanvasGroup>(), instant, callback);
                case AnimationType.Animator:
                    return ActionChain.Sequence(view)
                        .Animate(view.GetComponent<Animator>(), stateName)
                        .Event(callback)
                        .Begin();
                default: return null;
            }
        }
    }
}