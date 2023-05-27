using UnityEngine;
using WManager;

namespace WManager.UI
{
    /// <summary>
    /// UI视图基类
    /// </summary>
    [RequireComponent(typeof(RectTransform), typeof(CanvasGroup))]
    public class UIView : MonoBehaviour, IUIView
    {
        private CanvasGroup canvasGroup;
        private RectTransform rectTransform;

        //加载、显示事件
        [HideInInspector, SerializeField] private ViewAnimationEvent onVisible;
        //隐藏、卸载事件
        [HideInInspector, SerializeField] private ViewAnimationEvent onInvisible;

        protected IActionChain animationChain;

        public CanvasGroup CanvasGroup
        {
            get
            {
                if (canvasGroup == null)
                {
                    canvasGroup = GetComponent<CanvasGroup>();
                }
                return canvasGroup;
            }
        }
        public RectTransform RectTransform
        {
            get
            {
                if (rectTransform == null)
                {
                    rectTransform = GetComponent<RectTransform>();
                }
                return rectTransform;
            }
        }
        /// <summary>
        /// 视图名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 显示视图
        /// </summary>
        /// <param name="data">视图数据</param>
        /// <param name="instant">是否立即显示</param>
        public void Show(IViewData data = null, bool instant = false)
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            OnShow(data);

            //执行动画开始事件
            onVisible.onBeganEvent?.Invoke();
            //播放音效
            if (onVisible.onBeganSound != null)
                SoundManager.PlayUISound(onVisible.onBeganSound);
            //可交互性置为false
            CanvasGroup.interactable = false;
            //播放动画
            if (animationChain != null) animationChain.Stop();
            animationChain = onVisible.animation.Play(this, instant, () =>
            {
                //执行动画结束事件
                onVisible.onEndEvent?.Invoke();
                //播放音效
                if (onVisible.onEndSound != null)
                    SoundManager.PlayUISound(onVisible.onEndSound);
                //可交互性置为true
                CanvasGroup.interactable = true;
                CanvasGroup.blocksRaycasts = true;
                animationChain = null;
            });
        }
        /// <summary>
        /// 显示视图
        /// </summary>
        /// <param name="instant">是否立即显示</param>
        public void Show(bool instant)
        {
            Show(null, instant);
        }
        /// <summary>
        /// 隐藏视图
        /// </summary>
        /// <param name="instant">是否立即隐藏</param>
        public void Hide(bool instant = false)
        {
            OnHide();

            if (gameObject.activeSelf)
            {
                //执行动画开始事件
                onInvisible.onBeganEvent?.Invoke();
                //播放音效
                if (onInvisible.onBeganSound != null)
                    SoundManager.PlayUISound(onInvisible.onBeganSound);
                //可交互性置为false
                CanvasGroup.interactable = false;
                CanvasGroup.blocksRaycasts = false;
                //播放动画
                if (animationChain != null) animationChain.Stop();
                animationChain = onInvisible.animation.Play(this, instant, () =>
                {
                    //执行动画结束事件
                    onVisible.onEndEvent?.Invoke();
                    //播放音效
                    if (onInvisible.onEndSound != null)
                        SoundManager.PlayUISound(onInvisible.onEndSound);
                    animationChain = null;
                    gameObject.SetActive(false);
                });
            }
            else
            {
                Debug.Log(gameObject.name + "没有打开,无法影藏");
            }
        }
        /// <summary>
        /// 视图初始化
        /// </summary>
        /// <param name="data">视图数据</param>
        /// <param name="instant">是否立即显示</param>
        public void Init(IViewData data = null, bool instant = false)
        {
            OnInit(data);

            //执行动画开始事件
            onVisible.onBeganEvent?.Invoke();
            //播放音效
            if (onVisible.onBeganSound != null)
                SoundManager.PlayUISound(onVisible.onBeganSound);
            //可交互性置为false
            CanvasGroup.interactable = false;
            //播放动画
            if (animationChain != null) animationChain.Stop();
            animationChain = onVisible.animation.Play(this, instant, () =>
            {
                //执行动画结束事件
                onVisible.onEndEvent?.Invoke();
                //播放音效
                if (onInvisible.onEndSound != null)
                    SoundManager.PlayUISound(onVisible.onEndSound);
                //可交互性置为true
                CanvasGroup.interactable = true;
                animationChain = null;
            });
        }
        /// <summary>
        /// 卸载视图
        /// </summary>
        /// <param name="instant">是否立即卸载</param>
        public void Unload(bool instant = false)
        {
            UIManager.Remove(Name);
            OnUnload();

            //执行动画开始事件
            onInvisible.onBeganEvent?.Invoke();
            //播放音效
            if (onInvisible.onBeganSound != null)
                SoundManager.PlayUISound(onInvisible.onBeganSound);
            //可交互性置为false
            CanvasGroup.interactable = false;
            //播放动画
            if (animationChain != null) animationChain.Stop();
            animationChain = onInvisible.animation.Play(this, instant, () =>
            {
                //执行动画结束事件
                onVisible.onEndEvent?.Invoke();
                //播放音效
                if (onInvisible.onEndSound != null)
                    SoundManager.PlayUISound(onInvisible.onEndSound);
                //销毁视图物体
                Destroy(gameObject);
            });
        }

        protected virtual void OnInit(IViewData data) { }
        protected virtual void OnShow(IViewData data) { }
        protected virtual void OnHide() { }
        protected virtual void OnUnload() { }
    }
}