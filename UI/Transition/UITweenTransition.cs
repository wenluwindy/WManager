using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace WManager
{
    /// <summary>
    /// 基于 DOTween 的面板过渡动画，支持缩放 / 淡入淡出 / 位移三种效果任意组合。
    ///
    /// 打断策略：新过渡开始时不会把对象弹回起点，而是从当前值平滑过渡到新目标，
    /// 同时让上一段过渡的 await 以取消结束，因此快速连点不会造成界面跳变或异步挂死。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(CanvasGroup))]
    public class UITweenTransition : MonoBehaviour, IUITransition
    {
        private enum VisualState
        {
            Hidden,
            Shown
        }

        /// <summary>一段正在进行的过渡</summary>
        private sealed class Running
        {
            public Tween Tween;
            public UniTaskCompletionSource Completion;

            public void Cancel()
            {
                if (Tween != null && Tween.IsActive())
                    Tween.Kill();

                Completion?.TrySetCanceled();
            }
        }

#if ODIN_INSPECTOR
        [BoxGroup("基础设置"), LabelText("独立于 TimeScale")]
        [Tooltip("勾选后 Time.timeScale = 0 时动画依然播放，暂停界面必须打开")]
#else
        [Header("基础设置")]
        [Tooltip("勾选后 Time.timeScale = 0 时动画依然播放，暂停界面必须打开")]
#endif
        public bool ignoreTimeScale = true;

#if ODIN_INSPECTOR
        [BoxGroup("基础设置"), LabelText("默认显示状态")]
        [Tooltip("仅当本组件没有和 UIPanel 挂在一起时生效；作为面板过渡使用时由 UIPanel 接管")]
#else
        [Tooltip("仅当本组件没有和 UIPanel 挂在一起时生效；作为面板过渡使用时由 UIPanel 接管")]
#endif
        public bool defaultShow = true;

#if ODIN_INSPECTOR
        [BoxGroup("动画设置"), LabelText("动画持续时间")]
#else
        [Header("动画设置")]
#endif
        [Min(0f)]
        public float duration = 0.3f;

#if ODIN_INSPECTOR
        [BoxGroup("动画设置"), LabelText("动画延迟时间")]
#endif
        [Min(0f)]
        public float delay = 0f;

#if ODIN_INSPECTOR
        [BoxGroup("动画设置"), LabelText("缓动类型")]
#endif
        public Ease easeType = Ease.OutQuad;

#if ODIN_INSPECTOR
        [BoxGroup("缩放效果"), LabelText("启用缩放")]
#else
        [Header("缩放效果")]
#endif
        public bool enableScale = false;

#if ODIN_INSPECTOR
        [BoxGroup("缩放效果"), LabelText("起始缩放"), ShowIf("enableScale")]
#endif
        public Vector3 startScale = Vector3.zero;

#if ODIN_INSPECTOR
        [BoxGroup("缩放效果"), LabelText("目标缩放"), ShowIf("enableScale")]
#endif
        public Vector3 targetScale = Vector3.one;

#if ODIN_INSPECTOR
        [BoxGroup("渐隐渐现效果"), LabelText("启用透明度")]
#else
        [Header("渐隐渐现效果")]
#endif
        public bool enableFade = false;

#if ODIN_INSPECTOR
        [BoxGroup("渐隐渐现效果"), LabelText("起始透明度"), ShowIf("enableFade")]
#endif
        [Range(0f, 1f)]
        public float startAlpha = 0f;

#if ODIN_INSPECTOR
        [BoxGroup("渐隐渐现效果"), LabelText("目标透明度"), ShowIf("enableFade")]
#endif
        [Range(0f, 1f)]
        public float targetAlpha = 1f;

#if ODIN_INSPECTOR
        [BoxGroup("移动效果"), LabelText("启用移动")]
#else
        [Header("移动效果")]
#endif
        public bool enableMove = false;

#if ODIN_INSPECTOR
        [BoxGroup("移动效果"), LabelText("起始位置"), ShowIf("enableMove")]
#endif
        public Vector3 startPosition = Vector3.zero;

#if ODIN_INSPECTOR
        [BoxGroup("移动效果"), LabelText("目标位置"), ShowIf("enableMove")]
#endif
        public Vector3 targetPosition = Vector3.zero;

        [Header("事件回调")]
        public UnityEvent OnShowStart;
        public UnityEvent OnShowComplete;
        public UnityEvent OnHideStart;
        public UnityEvent OnHideComplete;

        private RectTransform _rectTransform;
        private CanvasGroup _canvasGroup;
        private Running _running;
        private VisualState _intent = VisualState.Hidden;

        /// <inheritdoc />
        public bool IsPlaying => _running != null;

        private void Awake()
        {
            InitializeComponents();

            // 和 UIPanel 挂在一起时，初始状态由 UIPanel 统一设置，这里不要抢
            if (GetComponent<UIPanel>() != null)
                return;

            if (defaultShow)
                SetShownImmediate();
            else
                SetHiddenImmediate();
        }

        private void OnDestroy()
        {
            Detach()?.Cancel();
        }

        private void InitializeComponents()
        {
            if (_rectTransform == null)
                _rectTransform = GetComponent<RectTransform>();

            if (_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
                if (_canvasGroup == null)
                    _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        // ------------------------------------------------------------ IUITransition

        /// <inheritdoc />
        public UniTask PlayShowAsync(CancellationToken cancellationToken = default)
        {
            InitializeComponents();

            var previous = Detach();

            // 已经是显示状态且没有动画在跑：直接落到目标值，不空转一个 duration
            if (previous == null && _intent == VisualState.Shown)
            {
                ApplyValues(targetScale, targetAlpha, targetPosition);
                OnShowStart?.Invoke();
                OnShowComplete?.Invoke();
                return UniTask.CompletedTask;
            }

            // 没有被打断时才回到起点；被打断时从当前视觉状态平滑接上
            if (previous == null)
                ApplyValues(startScale, startAlpha, startPosition);

            _intent = VisualState.Shown;

            return PlayAsync(targetScale, targetAlpha, targetPosition,
                OnShowStart, OnShowComplete, previous, cancellationToken);
        }

        /// <inheritdoc />
        public UniTask PlayHideAsync(CancellationToken cancellationToken = default)
        {
            InitializeComponents();

            var previous = Detach();

            if (previous == null && _intent == VisualState.Hidden)
            {
                ApplyValues(startScale, startAlpha, startPosition);
                OnHideStart?.Invoke();
                OnHideComplete?.Invoke();
                return UniTask.CompletedTask;
            }

            _intent = VisualState.Hidden;

            return PlayAsync(startScale, startAlpha, startPosition,
                OnHideStart, OnHideComplete, previous, cancellationToken);
        }

        /// <inheritdoc />
        public void SetShownImmediate()
        {
            InitializeComponents();
            Detach()?.Cancel();
            _intent = VisualState.Shown;
            ApplyValues(targetScale, targetAlpha, targetPosition);
        }

        /// <inheritdoc />
        public void SetHiddenImmediate()
        {
            InitializeComponents();
            Detach()?.Cancel();
            _intent = VisualState.Hidden;
            ApplyValues(startScale, startAlpha, startPosition);
        }

        // ------------------------------------------------------------ 内部实现

        private async UniTask PlayAsync(
            Vector3 scale,
            float alpha,
            Vector3 position,
            UnityEvent onStart,
            UnityEvent onComplete,
            Running previous,
            CancellationToken cancellationToken)
        {
            onStart?.Invoke();

            bool animatesScale = enableScale && _rectTransform != null;
            bool animatesFade = enableFade && _canvasGroup != null;
            bool animatesMove = enableMove && _rectTransform != null;

            // 一个效果都没开：同步完成，避免 DOTween 生成空 Sequence
            if (!animatesScale && !animatesFade && !animatesMove && delay <= 0f)
            {
                previous?.Cancel();
                ApplyValues(scale, alpha, position);
                onComplete?.Invoke();
                return;
            }

            var sequence = DOTween.Sequence().SetUpdate(ignoreTimeScale);

            if (animatesScale)
                sequence.Join(_rectTransform.DOScale(scale, duration).SetEase(easeType));

            if (animatesFade)
                sequence.Join(_canvasGroup.DOFade(alpha, duration).SetEase(easeType));

            if (animatesMove)
                sequence.Join(_rectTransform.DOAnchorPos(position, duration).SetEase(easeType));

            if (delay > 0f)
                sequence.SetDelay(delay);

            var completion = new UniTaskCompletionSource();
            var running = new Running { Tween = sequence, Completion = completion };
            _running = running;

            sequence.OnComplete(() =>
            {
                if (_running == running)
                    _running = null;

                completion.TrySetResult();
            });

            // 先把新过渡装好再取消旧的：旧 await 的续体会同步恢复，
            // 那时组件状态必须已经是"新过渡正在跑"，否则会读到中间态。
            previous?.Cancel();

            try
            {
                await completion.Task.AttachExternalCancellation(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                if (_running == running)
                {
                    _running = null;
                    if (sequence.IsActive())
                        sequence.Kill();
                }

                throw;
            }

            // DOTween 可能停在 0.999，收尾时精确落到目标值
            ApplyValues(scale, alpha, position);
            onComplete?.Invoke();
        }

        private Running Detach()
        {
            var running = _running;
            _running = null;
            return running;
        }

        private void ApplyValues(Vector3 scale, float alpha, Vector3 position)
        {
            if (enableScale && _rectTransform != null)
                _rectTransform.localScale = scale;

            if (enableFade && _canvasGroup != null)
                _canvasGroup.alpha = alpha;

            if (enableMove && _rectTransform != null)
                _rectTransform.anchoredPosition = position;
        }

        // ------------------------------------------------------------ Inspector 调试按钮

#if ODIN_INSPECTOR
        [Button("显示"), BoxGroup("控制按钮")]
#endif
        public void Show()
        {
            gameObject.SetActive(true);
            PlayShowAsync().Forget();
        }

#if ODIN_INSPECTOR
        [Button("隐藏"), BoxGroup("控制按钮")]
#endif
        public void Hide()
        {
            HideAndDeactivateAsync().Forget();
        }

        private async UniTaskVoid HideAndDeactivateAsync()
        {
            try
            {
                await PlayHideAsync();
            }
            catch (OperationCanceledException)
            {
                return;
            }

            gameObject.SetActive(false);
        }
    }
}
