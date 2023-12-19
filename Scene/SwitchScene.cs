using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using WManager;
using Sirenix.OdinInspector;
using UnityEngine.SceneManagement;
using WManager.UI;

public class SwitchScene : MonoBehaviour
{
    private static SwitchScene _instance;

    [LabelText("切换场景视图")]
    public UIView loadingView;
    [LabelText("切换场景模式")]
    public LoadSceneMode switchMode;

    private void Awake()
    {
        if (_instance == null)
        {
            // 如果实例为空，则将当前对象设置为实例，并保留在加载场景时不被销毁
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            // 如果实例已存在并且不是当前对象，则销毁当前对象
            if (_instance != this)
            {
                Destroy(gameObject);
            }
        }
    }

    void Start()
    {
        EventManager.StartListening("切换场景", OnJumpScene);
    }

    void OnJumpScene()
    {
        var sValue = EventManager.GetString("切换场景");
        if (sValue == "")
        {
            var iValue = EventManager.GetInt("切换场景");
            if (iValue != -1)
            {
                SceneLoader.LoadSceneAsync(iValue, switchMode)
                    .OnBegin(() => loadingView.Show())
                    .OnCompleted(() => loadingView.Hide());
            }
            else
            {
                Debug.Log("未传入场景索引或场景名");
            }
        }
        else
        {
            SceneLoader.LoadSceneAsync(sValue, switchMode)
                .OnBegin(() => loadingView.Show())
                .SetSceneActivationDelay(1)
                .OnCompleted(() =>
                {
                    loadingView.Hide();
                    Invoke(nameof(CloseActive), 1.3f);
                });
        }
    }
    void CloseActive()
    {
        loadingView.gameObject.SetActive(false);
    }
}
