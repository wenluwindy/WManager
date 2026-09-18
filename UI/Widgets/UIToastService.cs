using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if DOTWEEN
using DG.Tweening;
#endif

namespace WManager
{
    /// <summary>
    /// 飘字提示。挂在 <see cref="UILayer.Tips"/> 层，自动排队叠放并回收复用。
    /// 默认样式由代码生成，不需要任何预制体或美术资源。
    /// </summary>
    [DisallowMultipleComponent]
    internal class UIToastService : MonoBehaviour
    {
        private RectTransform _container;
        private readonly List<UIToastItem> _active = new();
        private readonly Stack<UIToastItem> _pool = new();

        public static UIToastService Create(Transform parent)
        {
            var go = new GameObject("ToastService", typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            UIFactory.Stretch(rect);

            var service = go.AddComponent<UIToastService>();
            service.BuildContainer(rect);

            return service;
        }

        private void BuildContainer(RectTransform parent)
        {
            var go = new GameObject("Container", typeof(RectTransform));
            _container = (RectTransform)go.transform;
            _container.SetParent(parent, false);

            _container.anchorMin = new Vector2(0.5f, 0f);
            _container.anchorMax = new Vector2(0.5f, 0f);
            _container.pivot = new Vector2(0.5f, 0f);
            _container.anchoredPosition = new Vector2(0f, UISettings.Instance.toastBottomOffset);

            var layout = go.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 12f;
            layout.childAlignment = TextAnchor.LowerCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var fitter = go.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        public void Show(string text, float duration, float fadeDuration, int maxVisible)
        {
            if (string.IsNullOrEmpty(text))
                return;

            // 超出可见上限时，最旧的立刻收走，避免一屏全是提示
            while (_active.Count >= Mathf.Max(1, maxVisible))
            {
                var oldest = _active[0];
                _active.RemoveAt(0);
                oldest.KillAndRecycle();
            }

            var item = _pool.Count > 0 ? _pool.Pop() : UIToastItem.Create(_container);

            item.transform.SetAsLastSibling();
            _active.Add(item);

            item.Play(text, duration, fadeDuration, Recycle);
        }

        public void Clear()
        {
            for (int i = _active.Count - 1; i >= 0; i--)
                _active[i].KillAndRecycle();

            _active.Clear();
        }

        private void Recycle(UIToastItem item)
        {
            _active.Remove(item);

            if (item == null)
                return;

            item.gameObject.SetActive(false);
            _pool.Push(item);
        }
    }

    /// <summary>单条飘字</summary>
    [DisallowMultipleComponent]
    internal class UIToastItem : MonoBehaviour
    {
        private CanvasGroup _canvasGroup;
        private TextMeshProUGUI _text;
#if DOTWEEN
        private Sequence _sequence;
#endif
        private Action<UIToastItem> _onFinished;

        public static UIToastItem Create(Transform parent)
        {
            var settings = UISettings.Instance;

            var go = new GameObject("Toast", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);

            var background = go.GetComponent<Image>();
            background.sprite = UIProceduralSprite.RoundedRect;
            background.type = Image.Type.Sliced;
            background.pixelsPerUnitMultiplier = 1f;
            background.color = settings.toastBackgroundColor;
            background.raycastTarget = false;

            var layout = go.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(32, 32, 18, 18);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var fitter = go.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var element = go.AddComponent<LayoutElement>();
            element.preferredWidth = 620f;

            var item = go.AddComponent<UIToastItem>();
            item._canvasGroup = go.AddComponent<CanvasGroup>();
            item._canvasGroup.blocksRaycasts = false;
            item._canvasGroup.interactable = false;

            item._text = UIFactory.CreateText(
                "Text", rect, string.Empty, settings.toastFontSize, settings.toastTextColor);

            go.SetActive(false);
            return item;
        }

        public void Play(string content, float duration, float fadeDuration, Action<UIToastItem> onFinished)
        {
            _onFinished = onFinished;
            _text.text = content;

            gameObject.SetActive(true);

            KillSequence();

            _canvasGroup.alpha = 0f;

#if DOTWEEN
            _sequence = DOTween.Sequence().SetUpdate(true);
            _sequence.Append(DOTween.To(
                    () => _canvasGroup.alpha,
                    v => _canvasGroup.alpha = v,
                    1f,
                    fadeDuration)
                .SetEase(Ease.OutQuad));
            _sequence.AppendInterval(Mathf.Max(0f, duration));
            _sequence.Append(DOTween.To(
                    () => _canvasGroup.alpha,
                    v => _canvasGroup.alpha = v,
                    0f,
                    fadeDuration)
                .SetEase(Ease.InQuad));
            _sequence.OnComplete(Finish);
#else
            _canvasGroup.alpha = 1f;
            Finish();
#endif
        }

        public void KillAndRecycle()
        {
            KillSequence();
            Finish();
        }

        private void Finish()
        {
            var callback = _onFinished;
            _onFinished = null;
            callback?.Invoke(this);
        }

        private void OnDestroy()
        {
            KillSequence();
            _onFinished = null;
        }

        private void KillSequence()
        {
#if DOTWEEN
            if (_sequence != null && _sequence.IsActive())
                _sequence.Kill();

            _sequence = null;
#endif
        }
    }
}
