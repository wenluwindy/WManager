﻿using UnityEngine;
using UnityEngine.Events;

namespace WManager
{
    /// <summary>
    /// 延时事件
    /// </summary>
    public class DelayAction : AbstractAction
    {
        //延时时长
        private readonly float duration;
        //开始时间
        private float beginTime;
        //是否已经开始
        private bool isBegan;

        public DelayAction(float duration, UnityAction action)
        {
            this.duration = duration;
            onCompleted = action;
        }

        protected override void OnInvoke()
        {
            if (!isBegan)
            {
                isBegan = true;
                beginTime = Time.time;
            }
            isCompleted = Time.time - beginTime >= duration;
        }

        protected override void OnReset()
        {
            isBegan = false;
        }
    }
}