using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WManager
{
    /// <summary>
    /// 通用消息框。
    ///
    /// 声明为多实例面板，因此同时弹多个会自动叠放，
    /// 每个 <c>await ShowMessageBoxAsync</c> 各自拿到自己那一个的结果，互不干扰。
    ///
    /// 业务侧不需要直接用这个类，走 <c>UI.ShowMessageBoxAsync</c> 即可。
    /// </summary>
    [UIPanelInfo(
        Key = "UI/MessageBoxPanel",
        Layer = UILayer.Popup,
        ShowMode = UIShowMode.Popup,
        Cache = false,
        MultiInstance = true,
        CloseOnMaskClick = false)]
    public class MessageBoxPanel : UIPanel<MessageBoxRequest>
    {
        [Header("UI")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private TMP_Text confirmButtonText;
        [SerializeField] private TMP_Text cancelButtonText;

        private UniTaskCompletionSource<MessageBoxResult> _completion;
        private bool _resolved;

        protected override void OnCreate()
        {
            BindClick(confirmButton, () => Resolve(MessageBoxResult.Confirm));
            BindClick(cancelButton, () => Resolve(MessageBoxResult.Cancel));
        }

        protected override void OnWillOpen(MessageBoxRequest request)
        {
            _resolved = false;
            _completion = new UniTaskCompletionSource<MessageBoxResult>();

            request ??= new MessageBoxRequest { Message = "消息内容为空" };

            if (titleText != null)
                titleText.text = request.ResolvedTitle;

            if (messageText != null)
                messageText.text = request.Message ?? string.Empty;

            if (confirmButtonText != null)
                confirmButtonText.text = request.ResolvedConfirmText;

            if (cancelButtonText != null)
                cancelButtonText.text = request.ResolvedCancelText;

            if (cancelButton != null)
                cancelButton.gameObject.SetActive(request.Style == MessageBoxStyle.ConfirmCancel);
        }

        /// <summary>等待用户选择。框架内部使用，业务侧走 UI.ShowMessageBoxAsync。</summary>
        public UniTask<MessageBoxResult> WaitForResultAsync()
        {
            return _completion != null
                ? _completion.Task
                : UniTask.FromResult(MessageBoxResult.Cancel);
        }

        /// <summary>
        /// 带取消按钮时返回键等同于取消；只有确定按钮时把返回键吞掉，
        /// 强制用户明确点一次确定。两种情况都返回 true，框架不会再去关它。
        /// </summary>
        protected internal override bool OnBackPressed()
        {
            bool cancellable = HasArgs && Args != null && Args.Style == MessageBoxStyle.ConfirmCancel;

            if (cancellable)
                Resolve(MessageBoxResult.Cancel);

            return true;
        }

        /// <summary>被外部强行关掉（例如 CloseAllAsync）时，按取消处理，绝不让 await 悬着</summary>
        protected override void OnDidClose()
        {
            Settle(MessageBoxResult.Cancel);
        }

        private void Resolve(MessageBoxResult result)
        {
            if (_resolved)
                return;

            var request = HasArgs ? Args : null;

            if (result == MessageBoxResult.Confirm)
                request?.OnConfirm?.Invoke();
            else
                request?.OnCancel?.Invoke();

            Settle(result);
            Close();
        }

        private void Settle(MessageBoxResult result)
        {
            if (_resolved)
                return;

            _resolved = true;
            _completion?.TrySetResult(result);
        }
    }
}
