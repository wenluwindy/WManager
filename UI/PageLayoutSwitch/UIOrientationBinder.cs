using System;
using UnityEngine;

namespace WManager
{
    /// <summary>
    /// 横竖屏布局切换的共用实现。<see cref="UIAdaptivePanel"/> 与
    /// <see cref="UIAdaptivePanel{TArgs}"/> 都委托给它，避免两份一样的代码。
    /// </summary>
    internal sealed class UIOrientationBinder
    {
        private readonly GameObject _landscapeRoot;
        private readonly GameObject _portraitRoot;
        private readonly Action<UIOrientation> _onChanged;

        private bool _subscribed;

        public UIOrientation Current { get; private set; }

        public UIOrientationBinder(GameObject landscapeRoot, GameObject portraitRoot, Action<UIOrientation> onChanged)
        {
            _landscapeRoot = landscapeRoot;
            _portraitRoot = portraitRoot;
            _onChanged = onChanged;

            Current = ReadCurrent();
        }

        /// <summary>订阅方向变化并立刻按当前方向摆好布局</summary>
        public void Enable()
        {
            if (UIOrientationService.HasInstance && !_subscribed)
            {
                UIOrientationService.Instance.OnOrientationChanged += Handle;
                _subscribed = true;
            }

            Current = ReadCurrent();
            Apply(Current);
        }

        public void Disable()
        {
            if (!_subscribed)
                return;

            if (UIOrientationService.HasInstance)
                UIOrientationService.Instance.OnOrientationChanged -= Handle;

            _subscribed = false;
        }

        private void Handle(UIOrientation orientation)
        {
            if (Current == orientation)
                return;

            Current = orientation;
            Apply(orientation);
            _onChanged?.Invoke(orientation);
        }

        private void Apply(UIOrientation orientation)
        {
            bool landscape = orientation == UIOrientation.Landscape;

            if (_landscapeRoot != null && _landscapeRoot.activeSelf != landscape)
                _landscapeRoot.SetActive(landscape);

            if (_portraitRoot != null && _portraitRoot.activeSelf == landscape)
                _portraitRoot.SetActive(!landscape);
        }

        private static UIOrientation ReadCurrent()
        {
            if (UIOrientationService.HasInstance)
                return UIOrientationService.Instance.CurrentOrientation;

            return Screen.width >= Screen.height ? UIOrientation.Landscape : UIOrientation.Portrait;
        }
    }
}
