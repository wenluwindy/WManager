using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WManager
{
    ///<summary>
    ///Animation动画事件
    ///</summary>
    public class AnimationAction : AbstractAction
    {
        private readonly Animation animation;
        private readonly string stateName;
        private bool isPlay;
        private bool isBegan;
        private float duration;
        private float beginTime;

        public AnimationAction(Animation animation, string stateName)
        {
            this.animation = animation;
            this.stateName = stateName;
        }
        protected override void OnInvoke()
        {
            if (!isPlay)
            {
                isPlay = true;
                animation.Play(stateName);
                return;
            }
            if (!isBegan)
            {
                isBegan = true;
                beginTime = Time.time;
                duration = animation[stateName].length;
            }
            isCompleted = Time.time - beginTime >= duration;
        }

        protected override void OnReset()
        {
            isPlay = false;
            isBegan = false;
        }
    }
}
