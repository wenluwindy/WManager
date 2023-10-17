using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using WManager;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine.SceneManagement;
using DG.Tweening;

public class SwitchScene : MonoBehaviour
{
    [LabelText("要跳转的场景名")]
    public string SceneName;
    [LabelText("切换场景按钮")]
    public Button JumpScene;
    [LabelText("切换场景模式")]
    public LoadSceneMode switchMode;
    [LabelText("加载进度窗口")]
    public GameObject LoadingWindow;
    [LabelText("进度条遮罩")]
    public Image schedule;
    [LabelText("进度文字")]
    public TMP_Text ProgressText;

    void Start()
    {
        // 标记该游戏对象为永不销毁
        DontDestroyOnLoad(this.gameObject);
        JumpScene.onClick.AddListener(OnJumpSceneClick);
    }

    void OnJumpSceneClick()
    {
        switch (switchMode)
        {
            case LoadSceneMode.Single:
                SceneLoader.LoadSceneAsync(SceneName, LoadSceneMode.Single)
                .OnBegin(() =>
                {
                    LoadingWindow.SetActive(true);
                    LoadingWindow.GetComponent<CanvasGroup>().DOFade(1, 0.5f);
                })
                .OnLoading((p) =>
                {
                    schedule.fillAmount = p;
                    ProgressText.text = (p * 100).ToString("0.0") + "%";
                })
                .OnCompleted(() =>
                {
                    LoadingWindow.GetComponent<CanvasGroup>().DOFade(0, 0.5f)
                    .OnComplete(() =>
                    {
                        LoadingWindow.SetActive(false);
                        Destroy(gameObject, 0.5f);
                    });
                });
                break;
            case LoadSceneMode.Additive:
                SceneLoader.LoadSceneAsync(SceneName, LoadSceneMode.Additive)
                .OnBegin(() =>
                {
                    LoadingWindow.SetActive(true);
                    LoadingWindow.GetComponent<CanvasGroup>().DOFade(1, 0.5f);
                })
                .OnLoading((p) =>
                {
                    schedule.fillAmount = p;
                    ProgressText.text = (p * 100).ToString("0.0") + "%";
                })
                .OnCompleted(() =>
                {
                    LoadingWindow.GetComponent<CanvasGroup>().DOFade(0, 0.5f)
                    .OnComplete(() =>
                    {
                        LoadingWindow.SetActive(false);
                        Destroy(gameObject, 0.5f);
                    });
                });
                ;
                break;
        }
    }
}
