using UnityEngine;
using WManager.UI;
using TMPro;

///<summary>
///功能：加载视图案例
///</summary>
public class UILoadDemo : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        UIManager.LoadViewResource<HomeView>();//加载Resources文件夹中的HomeView
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            UIManager.GetView("HomeView").Show();
        }
        if (Input.GetKeyDown(KeyCode.S))
        {
            UIManager.GetView("HomeView").Hide();
        }
        //加载StreamingAssets文件夹下的AB包，推荐方式
        if (Input.GetKeyDown(KeyCode.Z))
        {
            UIManager.LoadViewAB<HomeView>("AssetBundles/homeview.ab", "homeview", (f) => Debug.Log(f), (b, a) =>
            {
                if (b)
                {
                    //解决ab包中有tmp字体无法加载问题，将tmp字体放入Resources文件夹进行加载
                    TextMeshProUGUI[] textMeshProFromPrefab = a.GetComponentsInChildren<TextMeshProUGUI>();
                    foreach (var item in textMeshProFromPrefab)
                    {
                        Debug.Log(item.name);
                        var textMeshPro = Resources.Load<TMP_FontAsset>("MiSans");
                        Debug.Log(textMeshPro.name);
                        item.fontMaterial = textMeshPro.material;
                    }
                }
            });
        }
    }

}
