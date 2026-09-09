using System;
using System.Collections.Generic;
using UnityEngine;

namespace WManager
{
    /// <summary>
    /// 遮罩策略
    /// </summary>
    public enum UIMaskMode
    {
        /// <summary>按 ShowMode 决定：Popup 有遮罩，其它没有</summary>
        Auto = 0,
        /// <summary>总是有遮罩</summary>
        Always = 1,
        /// <summary>总是没有遮罩</summary>
        Never = 2
    }

    /// <summary>
    /// 声明面板自身的固有属性（资源 Key、层级、缓存策略等），
    /// 这样调用侧只需要 <c>UI.OpenAsync&lt;XxxPanel&gt;()</c>，不必每次重复传参。
    ///
    /// <code>
    /// [UIPanelInfo(Key = "UI/SettingsPanel", Layer = UILayer.Popup, ShowMode = UIShowMode.Popup)]
    /// public class SettingsPanel : UIPanel { }
    /// </code>
    ///
    /// 不加特性时按约定推导：Key = <see cref="UISettings.defaultKeyPrefix"/> + 类名，
    /// Layer = Normal，Cache = true，ShowMode = Normal。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public sealed class UIPanelInfoAttribute : Attribute
    {
        /// <summary>资源 Key。留空则为 前缀 + 类名</summary>
        public string Key { get; set; }

        /// <summary>所属层级</summary>
        public UILayer Layer { get; set; } = UILayer.Normal;

        /// <summary>关闭后是否保留实例。false 表示关闭即释放</summary>
        public bool Cache { get; set; } = true;

        /// <summary>显示模式</summary>
        public UIShowMode ShowMode { get; set; } = UIShowMode.Normal;

        /// <summary>是否允许同时存在多个实例（如可叠放的 MessageBox / Toast）</summary>
        public bool MultiInstance { get; set; }

        /// <summary>常驻面板。不会被缓存上限或 ReleaseAllCached 回收</summary>
        public bool Persistent { get; set; }

        /// <summary>遮罩策略</summary>
        public UIMaskMode Mask { get; set; } = UIMaskMode.Auto;

        /// <summary>点击遮罩是否关闭本面板</summary>
        public bool CloseOnMaskClick { get; set; } = true;

        /// <summary>Esc / 返回键是否关闭本面板（仅对弹窗生效）</summary>
        public bool CloseOnBackKey { get; set; } = true;
    }

    /// <summary>
    /// 面板元数据：特性声明 + 调用侧覆盖参数合并后的最终结果。
    /// </summary>
    public readonly struct UIPanelMeta
    {
        public readonly Type PanelType;
        public readonly string Key;
        public readonly UILayer Layer;
        public readonly bool Cache;
        public readonly UIShowMode ShowMode;
        public readonly bool MultiInstance;
        public readonly bool Persistent;
        public readonly UIMaskMode Mask;
        public readonly bool CloseOnMaskClick;
        public readonly bool CloseOnBackKey;

        public UIPanelMeta(
            Type panelType,
            string key,
            UILayer layer,
            bool cache,
            UIShowMode showMode,
            bool multiInstance,
            bool persistent,
            UIMaskMode mask,
            bool closeOnMaskClick,
            bool closeOnBackKey)
        {
            PanelType = panelType;
            Key = key;
            Layer = layer;
            Cache = cache;
            ShowMode = showMode;
            MultiInstance = multiInstance;
            Persistent = persistent;
            Mask = mask;
            CloseOnMaskClick = closeOnMaskClick;
            CloseOnBackKey = closeOnBackKey;
        }

        /// <summary>本面板本次打开是否需要遮罩</summary>
        public bool NeedsMask =>
            Mask == UIMaskMode.Always ||
            (Mask == UIMaskMode.Auto && ShowMode == UIShowMode.Popup && UISettings.Instance.enablePopupMask);

        /// <summary>
        /// 读取类型上的 <see cref="UIPanelInfoAttribute"/>（带缓存），再用调用侧显式传入的参数覆盖。
        /// 显式参数优先级高于特性。
        /// </summary>
        public static UIPanelMeta Resolve(
            Type panelType,
            string key = null,
            UILayer? layer = null,
            bool? cache = null,
            UIShowMode? showMode = null)
        {
            var baseMeta = GetDeclared(panelType);

            return new UIPanelMeta(
                panelType,
                string.IsNullOrWhiteSpace(key) ? baseMeta.Key : key,
                layer ?? baseMeta.Layer,
                cache ?? baseMeta.Cache,
                showMode ?? baseMeta.ShowMode,
                baseMeta.MultiInstance,
                baseMeta.Persistent,
                baseMeta.Mask,
                baseMeta.CloseOnMaskClick,
                baseMeta.CloseOnBackKey);
        }

        /// <summary>只读取类型声明，不做任何覆盖</summary>
        public static UIPanelMeta GetDeclared(Type panelType)
        {
            if (panelType == null)
                throw new ArgumentNullException(nameof(panelType));

            if (_cache.TryGetValue(panelType, out var cached))
                return cached;

            var attr = GetAttribute(panelType);

            string key = attr != null && !string.IsNullOrWhiteSpace(attr.Key)
                ? attr.Key
                : UISettings.Instance.defaultKeyPrefix + panelType.Name;

            var meta = attr == null
                ? new UIPanelMeta(panelType, key, UILayer.Normal, true, UIShowMode.Normal,
                    false, false, UIMaskMode.Auto, true, true)
                : new UIPanelMeta(panelType, key, attr.Layer, attr.Cache, attr.ShowMode,
                    attr.MultiInstance, attr.Persistent, attr.Mask, attr.CloseOnMaskClick, attr.CloseOnBackKey);

            _cache[panelType] = meta;
            return meta;
        }

        private static readonly Dictionary<Type, UIPanelMeta> _cache = new();

        /// <summary>清空元数据缓存。关闭域重载进入 Play 模式时需要。</summary>
        internal static void ClearCache() => _cache.Clear();

        private static UIPanelInfoAttribute GetAttribute(Type type)
        {
            // Inherited = true 时基类特性也能取到，但 Key 会继承成基类名，
            // 所以只在类型自身没有声明时才向上找，且向上找到的 Key 一律忽略。
            var own = (UIPanelInfoAttribute[])type.GetCustomAttributes(typeof(UIPanelInfoAttribute), false);
            if (own.Length > 0)
                return own[0];

            var inherited = (UIPanelInfoAttribute[])type.GetCustomAttributes(typeof(UIPanelInfoAttribute), true);
            if (inherited.Length == 0)
                return null;

            var parent = inherited[0];
            return new UIPanelInfoAttribute
            {
                Key = null, // 基类的 Key 对子类没有意义，退回按类名推导
                Layer = parent.Layer,
                Cache = parent.Cache,
                ShowMode = parent.ShowMode,
                MultiInstance = parent.MultiInstance,
                Persistent = parent.Persistent,
                Mask = parent.Mask,
                CloseOnMaskClick = parent.CloseOnMaskClick,
                CloseOnBackKey = parent.CloseOnBackKey
            };
        }
    }

    /// <summary>
    /// 关闭域重载（Enter Play Mode Options）时静态字段不会自动清空，
    /// 这里负责在进入 Play 模式前把面板元数据缓存重置掉。
    /// </summary>
    internal static class UIPanelMetaLifecycle
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnEnterPlayMode()
        {
            UIPanelMeta.ClearCache();
        }
    }
}
