﻿namespace WManager
{
    /// <summary>
    /// 事件接口
    /// </summary>
    public interface IAction
    {
        bool Invoke();

        void Reset();
    }
}