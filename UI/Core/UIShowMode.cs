namespace WManager
{
    /// <summary>
    /// 区分页面显示模式。
    /// </summary>
    public enum UIShowMode
    {
        /// <summary>
        /// 普通显示，不参与页面栈
        /// </summary>
        Normal = 0,
        /// <summary>
        /// 进入页面栈（像页面跳转）
        /// </summary>
        Stack = 1,
        /// <summary>
        /// 弹窗，不进入页面栈
        /// </summary>
        Popup = 2
    }
}