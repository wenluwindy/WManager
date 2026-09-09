using UnityEngine;

namespace WManager
{
    /// <summary>
    /// 调试面板抽象基类。每个面板（如 FPS、内存、系统信息）继承此类，
    /// 由 Debugger 统一在 OnGUI 中调用 DrawGUI 进行绘制。
    /// </summary>
    public abstract class DebugPanel
    {
        public abstract string Name { get; }
        public abstract void DrawGUI();
    }
}