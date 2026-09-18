using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if DOTWEEN
using DG.Tweening;
#endif

namespace WManager
{
    /// <summary>
    /// 弹窗遮罩。<see cref="UIShowMode.Popup"/> 的面板打开时由
    /// <see cref="UIManager"/> 自动在其下方垫一层，关闭时一起收走。
    /// 每个弹窗一层遮罩，因此多个弹窗叠放时层次依然正确。
    /// </summary>
    [DisallowMultipleComponent]
    public class UIPopupMask : MonoBehaviour, IPointerClickHandler
    {
        private Image _image;
        private Color _color;
#if DOTWEEN
        private Tween _tween;
#endif

        /// <summary>点击遮罩空白处</summary>
        public event Action Clicked;

        /// <summary>是否响应点击</summary>
        public bool Clickable { get; set; }

        internal static UIPopupMask Create(Transform parent, Color color)
        {
            var rect = UIFactory.CreateStretchNode(
                "PopupMask",
                parent,
                typeof(CanvasRenderer),
                typeof(Image));

            var mask = rect.gameObject.AddComponent<UIPopupMask>();
            mask._image = rect.GetComponent<Image>();
            mask._color = color;
            mask._image.color = new Color(color.r, color.g, color.b, 0f);
            mask._image.raycastTarget = true;

            return mask;
        }

        internal void FadeIn(float duration)
        {
            gameObject.SetActive(true);

            KillTween();

            if (duration <= 0f)
            {
                _image.color = _color;
                return;
            }

#if DOTWEEN
            _tween = DOTween.To(
                    () => _image.color.a,
                    a =>
                    {
                        var c = _image.color;
                        c.a = a;
                        _image.color = c;
                    },
                    _color.a,
                    duration)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true);
#else
            _image.color = _color;
#endif
        }

        internal void FadeOut(float duration, bool deactivateOnComplete = true)
        {
            KillTween();

            if (duration <= 0f)
            {
                _image.color = new Color(_color.r, _color.g, _color.b, 0f);

                if (deactivateOnComplete)
                    gameObject.SetActive(false);

                return;
            }

#if DOTWEEN
            _tween = DOTween.To(
                    () => _image.color.a,
                    a =>
                    {
                        var c = _image.color;
                        c.a = a;
                        _image.color = c;
                    },
                    0f,
                    duration)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    if (deactivateOnComplete && this != null)
                        gameObject.SetActive(false);
                });
#else
            _image.color = new Color(_color.r, _color.g, _color.b, 0f);

            if (deactivateOnComplete)
                gameObject.SetActive(false);
#endif
        }

        void IPointerClickHandler.OnPointerClick(PointerEventData eventData)
        {
            if (Clickable)
                Clicked?.Invoke();
        }

        private void OnDestroy()
        {
            KillTween();
            Clicked = null;
        }

        private void KillTween()
        {
#if DOTWEEN
            if (_tween != null && _tween.IsActive())
                _tween.Kill();

            _tween = null;
#endif
        }
    }
}
