using UnityEngine;

namespace WManager
{
    /// <summary>
    /// 系统信息面板
    /// </summary>
    public class SystemPanel : DebugPanel
    {
        public override string Name => "系统";

        public override void DrawGUI()
        {
            GUILayout.Label($"操作系统：{SystemInfo.operatingSystem}");
            GUILayout.Label($"系统内存：{SystemInfo.systemMemorySize}MB");
            GUILayout.Label($"处理器：{SystemInfo.processorType}");
            GUILayout.Label($"处理器数量：{SystemInfo.processorCount}");
            GUILayout.Label($"显卡：{SystemInfo.graphicsDeviceName}");
            GUILayout.Label($"显卡类型：{SystemInfo.graphicsDeviceType}");
            GUILayout.Label($"显存：{SystemInfo.graphicsMemorySize}MB");
            GUILayout.Label($"显卡标识：{SystemInfo.graphicsDeviceID}");
            GUILayout.Label($"显卡供应商：{SystemInfo.graphicsDeviceVendor}");
            GUILayout.Label($"显卡供应商标识码：{SystemInfo.graphicsDeviceVendorID}");
            GUILayout.Label($"设备模式：{SystemInfo.deviceModel}");
            GUILayout.Label($"设备名称：{SystemInfo.deviceName}");
            GUILayout.Label($"设备类型：{SystemInfo.deviceType}");
            GUILayout.Label($"设备标识：{SystemInfo.deviceUniqueIdentifier}");
        }
    }
}