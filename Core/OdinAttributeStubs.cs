#if !ODIN_INSPECTOR
using System;
using UnityEngine;

namespace Sirenix.OdinInspector
{
    /// <summary>
    /// 无 Odin 时的属性桩，保证 WManager 在未安装 Odin Inspector 的工程中仍可编译。
    /// 安装 Odin 后会定义 ODIN_INSPECTOR，本文件不参与编译。
    /// </summary>
    public enum ButtonSizes
    {
        Small = 0,
        Medium = 1,
        Large = 2,
        Gigantic = 3
    }

    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    public sealed class LabelTextAttribute : Attribute
    {
        public LabelTextAttribute(string text) { }
        public LabelTextAttribute(string text, bool nicifyText) { }
    }

    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    public sealed class TabGroupAttribute : Attribute
    {
        public TabGroupAttribute(string group) { }
        public TabGroupAttribute(string group, string tab) { }
        public TabGroupAttribute(string group, int order) { }
    }

    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    public sealed class ShowIfAttribute : Attribute
    {
        public ShowIfAttribute(string condition) { }
        public ShowIfAttribute(string condition, object value) { }
    }

    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    public sealed class InfoBoxAttribute : Attribute
    {
        public InfoBoxAttribute(string message) { }
        public InfoBoxAttribute(string message, InfoMessageType infoMessageType) { }
    }

    public enum InfoMessageType
    {
        None = 0,
        Info = 1,
        Warning = 2,
        Error = 3
    }

    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    public sealed class BoxGroupAttribute : Attribute
    {
        public BoxGroupAttribute(string group) { }
    }

    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    public sealed class ButtonAttribute : Attribute
    {
        public ButtonAttribute() { }
        public ButtonAttribute(string name) { }
        public ButtonAttribute(ButtonSizes size) { }
        public ButtonAttribute(string name, ButtonSizes size) { }
    }

    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    public sealed class TitleAttribute : Attribute
    {
        public TitleAttribute(string title) { }
        public TitleAttribute(string title, string subtitle) { }
    }

    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    public sealed class ShowInInspectorAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    public sealed class ReadOnlyAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    public sealed class GUIColorAttribute : Attribute
    {
        public GUIColorAttribute(float r, float g, float b, float a = 1f) { }
        public GUIColorAttribute(string getColor) { }
    }
}
#endif
