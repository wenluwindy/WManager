using System;

namespace WManager
{
    /// <summary>
    /// 消息框请求参数。文字留空时自动取 <see cref="UISettings"/> 里的默认值，
    /// 方便统一接本地化。
    /// </summary>
    public class MessageBoxRequest
    {
        /// <summary>标题</summary>
        public string Title;

        /// <summary>正文</summary>
        public string Message;

        /// <summary>确定按钮文字。留空取配置默认值。</summary>
        public string ConfirmText;

        /// <summary>取消按钮文字。留空取配置默认值。</summary>
        public string CancelText;

        /// <summary>按钮样式</summary>
        public MessageBoxStyle Style = MessageBoxStyle.Confirm;

        /// <summary>点确定的回调。也可以直接 await 返回值，二选一即可。</summary>
        public Action OnConfirm;

        /// <summary>点取消的回调</summary>
        public Action OnCancel;

        /// <summary>
        /// 指定另一套消息框预制体。留空则用 <see cref="UISettings.messageBoxKey"/>，
        /// 再留空则用 <see cref="MessageBoxPanel"/> 上声明的默认 Key。
        /// </summary>
        public string PanelKey;

        internal string ResolvedTitle =>
            string.IsNullOrEmpty(Title) ? UISettings.Instance.messageBoxDefaultTitle : Title;

        internal string ResolvedConfirmText =>
            string.IsNullOrEmpty(ConfirmText) ? UISettings.Instance.messageBoxDefaultConfirmText : ConfirmText;

        internal string ResolvedCancelText =>
            string.IsNullOrEmpty(CancelText) ? UISettings.Instance.messageBoxDefaultCancelText : CancelText;
    }

    /// <summary>消息框按钮样式</summary>
    public enum MessageBoxStyle
    {
        /// <summary>只有确定</summary>
        Confirm = 0,

        /// <summary>确定 + 取消</summary>
        ConfirmCancel = 1
    }

    /// <summary>消息框返回结果</summary>
    public enum MessageBoxResult
    {
        /// <summary>无</summary>
        None = 0,

        /// <summary>点了确定</summary>
        Confirm = 1,

        /// <summary>点了取消，或者消息框被外部关掉</summary>
        Cancel = 2
    }
}
