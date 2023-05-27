using UnityEngine;
using TMPro;
using WManager;
using System;

///<summary>
/// AB包加载案例
/// 根据配置文件初始化场景
///</summary>
public class ToConfigure : MonoBehaviour
{
    public Transform LookTF;
    public TMP_Text TMP_Title;
    public TMP_Text TMP_Content;
    public SurroundingCamera SCamera;
    public ModelData info;
    private string ConfPath;
    [Serializable]
    public class ModelData
    {
        public string url;
        public string title;
        public string content;
        public int distance;
    }
    void Awake()
    {
        StreamingAssetsLoader.LoadTextAsset("Configure.json",
        (s) =>
        {
            Debug.Log("读取txt文件成功");
            //将数据赋给数组
            info = JsonUtility.FromJson<ModelData>(s);
            Debug.Log("数据赋给数组成功");
            TMP_Title.text = info.title;
            Debug.Log(info.title);
            TMP_Content.text = info.content;
            Debug.Log(info.content);
            SCamera.distance = info.distance;
            Debug.Log(info.distance);
        });
    }

    void Start()
    {
        //同步加载
        // GameObject game = AssetBundleManager.LoadFromFile("model.ab", "推土机");
        // Debug.Log("同步加载成功");
        // Instantiate(game);
        // Debug.Log("实例化成功");
        // game.transform.parent = LookTF;
        // Debug.Log("名字为：" + game.name);

        //webgl加载
        AssetBundleLoader.LoadObjFromWeb("AssetBundles/model.ab", "推土机", (obj, f) =>
        {
            Debug.Log("加载进度：" + f);
            Instantiate(obj);
            (obj as GameObject).transform.parent = LookTF;
        });

        //网址
        // AssetBundleLoader.LoadABFromWeb("http://xr.kmtxedu.cn:81/unity-webgl/1/StreamingAssets/model.ab", (ab, f) =>
        // {
        //     Debug.Log("加载进度：" + f);
        //     // 从 AssetBundle 加载预制体
        //     GameObject prefab = ab.LoadAsset<GameObject>("推土机");
        //     // 实例化预制体
        //     GameObject obj = Instantiate(prefab);
        //     obj.transform.parent = LookTF;
        // });

    }
}
