using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WManager;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

///<summary>
///功能：
///</summary>
public class SceneDemo : MonoBehaviour
{
    //场景加载过渡界面
    [SerializeField] GameObject loadingView;
    //加载进度条
    [SerializeField] Slider slider;
    //加载进度文本
    [SerializeField] Text progressText;
    SceneLoader d;
    void Start()
    {
        //加载BuildIndex为1的场景
        d = SceneLoader.LoadSceneAsync(0, LoadSceneMode.Additive)
            //加载前将加载界面打开
            .OnBegin(() => loadingView.SetActive(true))
            //设置场景加载完是否马上激活
            //.SetAllowSceneActivation(false)
            //设置场景激活延迟
            .SetSceneActivationDelay(2)
            //加载中事件，将进度值赋给进度条及进度文本
            .OnLoading(s =>
            {
                slider.value = s;
                progressText.text = string.Format("{0}%", Mathf.Round(s * 100));
            })
            //加载结束后关闭加载界面
            .OnCompleted(() => loadingView.SetActive(false));

    }
    /// <summary>
    /// 调用事件激活场景
    /// </summary>
    public void ssss()
    {
        d.SetAllowSceneActivation(true);
    }
}
