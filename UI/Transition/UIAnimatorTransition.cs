using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace WManager
{
    /// <summary>
    /// 基于 Animator 的面板过渡动画。适合用美术做好的复杂入场 / 退场动画。
    /// Animator Controller 里需要有两个状态（默认叫 Show 和 Hide），状态不存在时自动跳过动画。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Animator))]
    public class UIAnimatorTransition : MonoBehaviour, IUITransition
    {
        private enum VisualState
        {
            Hidden,
            Shown
        }

        [Header("状态名")]
        [SerializeField] private string showStateName = "Show";
        [SerializeField] private string hideStateName = "Hide";
        [SerializeField] private int layerIndex = 0;

        [Header("兜底")]
        [Tooltip("读不到状态时长时使用的时长")]
        [SerializeField, Min(0f)] private float fallbackDuration = 0.3f;

        [Tooltip("勾选后 Time.timeScale = 0 时动画依然播放")]
        [SerializeField] private bool ignoreTimeScale = true;

        private Animator _animator;
        private CancellationTokenSource _cts;
        private VisualState _intent = VisualState.Hidden;

        /// <inheritdoc />
        public bool IsPlaying => _cts != null;

        private void Awake()
        {
            EnsureAnimator();

            if (GetComponent<UIPanel>() != null)
                return;

            SetShownImmediate();
        }

        private void OnDestroy()
        {
            CancelRunning();
        }

        /// <inheritdoc />
        public UniTask PlayShowAsync(CancellationToken cancellationToken = default)
        {
            return PlayStateAsync(showStateName, VisualState.Shown, cancellationToken);
        }

        /// <inheritdoc />
        public UniTask PlayHideAsync(CancellationToken cancellationToken = default)
        {
            return PlayStateAsync(hideStateName, VisualState.Hidden, cancellationToken);
        }

        /// <inheritdoc />
        public void SetShownImmediate()
        {
            EnsureAnimator();
            CancelRunning();
            _intent = VisualState.Shown;
            JumpToStateEnd(showStateName);
        }

        /// <inheritdoc />
        public void SetHiddenImmediate()
        {
            EnsureAnimator();
            CancelRunning();
            _intent = VisualState.Hidden;
            JumpToStateEnd(hideStateName);
        }

        private async UniTask PlayStateAsync(string stateName, VisualState intent, CancellationToken external)
        {
            EnsureAnimator();

            var previous = _cts;
            var cts = CancellationTokenSource.CreateLinkedTokenSource(external);
            _cts = cts;
            _intent = intent;

            // 先装好新的再取消旧的：旧 await 的续体会同步恢复，那时状态必须已经是新的
            if (previous != null)
            {
                previous.Cancel();
                previous.Dispose();
            }

            try
            {
                float length = PlayAndGetLength(stateName);

                if (length > 0f)
                {
                    await UniTask.Delay(
                        TimeSpan.FromSeconds(length),
                        ignoreTimeScale,
                        PlayerLoopTiming.Update,
                        cts.Token);
                }
            }
            finally
            {
                if (_cts == cts)
                {
                    _cts = null;
                    cts.Dispose();
                }
            }
        }

        private float PlayAndGetLength(string stateName)
        {
            if (_animator == null || string.IsNullOrEmpty(stateName))
                return 0f;

            int hash = Animator.StringToHash(stateName);

            if (!_animator.HasState(layerIndex, hash))
            {
                Debug.LogWarning($"[UIAnimatorTransition] Animator 上没有状态 {stateName}，跳过动画。", this);
                return 0f;
            }

            _animator.enabled = true;
            _animator.updateMode = ignoreTimeScale
                ? AnimatorUpdateMode.UnscaledTime
                : AnimatorUpdateMode.Normal;

            _animator.Play(hash, layerIndex, 0f);

            // 不 Update 一下，GetCurrentAnimatorStateInfo 读到的还是切换前的状态
            _animator.Update(0f);

            float length = _animator.GetCurrentAnimatorStateInfo(layerIndex).length;
            return length > 0f ? length : fallbackDuration;
        }

        private void JumpToStateEnd(string stateName)
        {
            if (_animator == null || string.IsNullOrEmpty(stateName))
                return;

            int hash = Animator.StringToHash(stateName);
            if (!_animator.HasState(layerIndex, hash))
                return;

            _animator.Play(hash, layerIndex, 1f);
            _animator.Update(0f);
        }

        private void CancelRunning()
        {
            if (_cts == null)
                return;

            var cts = _cts;
            _cts = null;
            cts.Cancel();
            cts.Dispose();
        }

        private void EnsureAnimator()
        {
            if (_animator == null)
                _animator = GetComponent<Animator>();
        }
    }
}
