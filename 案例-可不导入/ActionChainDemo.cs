using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using WManager;
using DG.Tweening;

///<summary>
///事件链案例
///</summary>
public class ActionChainDemo : MonoBehaviour
{
    public Button button;
    public GameObject Object;
    public Animator animator;
    public Animation animt;
    IActionChain chain;
    // Start is called before the first frame update
    void Start()
    {
        //编辑事件链：序列事件链
        chain = ActionChain.Sequence()
            //普通事件
            .Event(() => Debug.Log("开始事件链"))
            //延迟2秒
            .Delay(2f)
            //普通事件
            .Event(() => Debug.Log("经过2秒"))
            .Event(() => Debug.Log("等待按下A键"))
            //直到按下键盘A键
            .Until(() => Input.GetKeyDown(KeyCode.A))
            //普通事件
            .Event(() => Debug.Log("按下A键"))
            //DoTween动画事件
            .Tween(() => transform.DOMove(new Vector3(0f, 1f, 0f), 2f))
            //按钮点击事件
            .Event(() => print("等待点击" + button.name))
            .Until(button.isClickBtn())
            .Event(() => print("点击了" + button.name))
            //物体点击事件
            .Event(() => print("等待点击" + Object.name))
            .Until(Object.isClickObj())
            .Event(() => print("点击了" + Object.name))
            //动画事件
            .Event(() => print("等待动画a"))
            .Animate(animator, "a")
            .Event(() => print("a播放完毕"))

            //嵌套一个并发事件链
            .Append(new ConcurrentActionChain()
                .Delay(1f, () => Debug.Log("1f"))
                .Delay(2f, () => Debug.Log("2f"))
                .Delay(3f, () => Debug.Log("3f"))
                as IAction)
            //并发事件链执行完成后 继续执行序列事件链

            //定时事件
            .Timer(3f, false, s => Debug.Log(s))
            .Event(() => print("等待动画q"))
            .Animation(animt, "q")
            .Event(() => print("q播放完毕"));

        //执行事件链
        chain.Begin()
        .OnStop(() => Debug.Log("事件结束"));
    }

    public void aaaa()
    {
        chain.Stop();
    }
}
