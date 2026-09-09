using UnityEngine;
using UnityEngine.Profiling;

namespace WManager
{
    /// <summary>
    /// 内存监控面板
    /// </summary>
    public class MemoryPanel : DebugPanel
    {
        public override string Name => "内存";

        public override void DrawGUI()
        {
            GUILayout.Label($"总内存：{Profiler.GetTotalReservedMemoryLong() / 1000000}MB");
            GUILayout.Label($"已占用内存：{Profiler.GetTotalAllocatedMemoryLong() / 1000000}MB");
            GUILayout.Label($"空闲中内存：{Profiler.GetTotalUnusedReservedMemoryLong() / 1000000}MB");
            GUILayout.Label($"总Mono堆内存：{Profiler.GetMonoHeapSizeLong() / 1000000}MB");
            GUILayout.Label($"已占用Mono堆内存：{Profiler.GetMonoUsedSizeLong() / 1000000}MB");

            if (GUILayout.Button("卸载未使用的资源"))
            {
                Resources.UnloadUnusedAssets();
            }
            if (GUILayout.Button("使用GC垃圾回收"))
            {
                System.GC.Collect();
            }
        }
    }
}