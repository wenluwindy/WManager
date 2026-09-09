using UnityEngine;

namespace WManager
{
    /// <summary>
    /// FPS 显示面板
    /// </summary>
    public class FpsPanel : DebugPanel
    {
        public override string Name => "FPS";

        private int _fps;
        private int _frameNumber;
        private float _lastShowFPSTime;

        /// <summary>
        /// 当前 FPS（最近 1 秒内的平均帧数）。未到 1 秒时为 0。
        /// Debugger 标题栏用这个值代替硬编码的 "???" / "..."。
        /// </summary>
        public int CurrentFps => _fps;

        public void Tick(float unscaledDeltaTime)
        {
            // unscaledDeltaTime 在 Debugger.Update 传入，受 timeScale 影响；
            // 这里用 realtimeSinceStartup 做统计，与之无关。形参保留以兼容现有调用。
            _ = unscaledDeltaTime;

            _frameNumber++;
            float time = Time.realtimeSinceStartup - _lastShowFPSTime;
            if (time >= 1f)
            {
                _fps = (int)(_frameNumber / time);
                _frameNumber = 0;
                _lastShowFPSTime = Time.realtimeSinceStartup;
            }
        }

        public override void DrawGUI()
        {
            GUILayout.Label($"FPS: {_fps}");
        }
    }
}