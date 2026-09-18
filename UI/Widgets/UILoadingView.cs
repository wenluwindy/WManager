using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if DOTWEEN
using DG.Tweening;
#endif

namespace WManager
{
    /// <summary>
    /// 全屏 Loading 遮罩。挂在 <see cref="UILayer.Top"/> 层，显示期间拦截输入。
    /// 默认样式由代码生成，不需要任何预制体或美术资源。
    /// </summary>
    [DisallowMultipleComponent]
    internal class UILoadingView : MonoBehaviour
    {
        private TextMeshProUGUI _text;
        private RectTransform _spinner;
#if DOTWEEN
        private Tween _spinTween;
#endif

        public static UILoadingView Create(Transform parent)
        {
            var rect = UIFactory.CreateStretchNode(
                "LoadingView",
                parent,
                typeof(CanvasRenderer),
                typeof(Image));

            var go = rect.gameObject;
            var settings = UISettings.Instance;

            var background = go.GetComponent<Image>();
            background.color = settings.loadingBackgroundColor;
            background.raycastTarget = true;

            var view = go.AddComponent<UILoadingView>();

            var spinnerImage = UIFactory.CreateImage("Spinner", rect, Color.white);
            spinnerImage.sprite = UIProceduralSprite.Ring;
            spinnerImage.raycastTarget = false;
            view._spinner = spinnerImage.rectTransform;
            view._spinner.sizeDelta = new Vector2(96f, 96f);
            view._spinner.anchoredPosition = new Vector2(0f, 40f);

            view._text = UIFactory.CreateText(
                "Text", rect, string.Empty, settings.loadingFontSize, settings.loadingTextColor);
            view._text.rectTransform.sizeDelta = new Vector2(600f, 48f);
            view._text.rectTransform.anchoredPosition = new Vector2(0f, -52f);

            go.SetActive(false);
            return view;
        }

        public void SetText(string text)
        {
            if (_text == null)
                return;

            _text.text = text ?? string.Empty;
            _text.gameObject.SetActive(!string.IsNullOrEmpty(text));
        }

        public void Show()
        {
            transform.SetAsLastSibling();
            gameObject.SetActive(true);

            KillTween();

#if DOTWEEN
            _spinTween = _spinner
                .DOLocalRotate(new Vector3(0f, 0f, -360f), 1.1f, RotateMode.FastBeyond360)
                .SetEase(Ease.Linear)
                .SetLoops(-1, LoopType.Restart)
                .SetUpdate(true);
#endif
        }

        public void Hide()
        {
            KillTween();
            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            KillTween();
        }

        private void KillTween()
        {
#if DOTWEEN
            if (_spinTween != null && _spinTween.IsActive())
                _spinTween.Kill();

            _spinTween = null;
#endif
        }
    }
}
