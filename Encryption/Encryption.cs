using UnityEngine;
using Sirenix.OdinInspector;
using UnityEngine.UI;

namespace WManager
{
    /// <summary>
    /// 加密管理器,定时授权
    /// </summary>
    public class Encryption : MonoBehaviour
    {
        [LabelText("服务器地址,需填入http://")]
        public string url;
        public int ID;
        [LabelText("秘钥")]
        public string Password;
        [LabelText("重复验证时间(秒)")]
        public int IntervalTime = 15;
        public Button retry;
        public class Send
        {
            public int id;
            public string code;
        }
        private string data;
        //定时器
        private Countdown countdown;
        private void Start()
        {
            DontDestroyOnLoad(this);
            //准备发送数据
            Send send = new Send
            {
                id = this.ID,
                code = StringExtension.GetMacAddress().AESEncrypt(Password)
            };
            data = JsonUtility.ToJson(send);

            RequestAuthorization();

            countdown = Timer.Countdown(IntervalTime)
                .OnStop(() =>
                {
                    Debug.Log("请求再次授权");
                    //请求再次授权
                    RequestAuthorization();
                });

            retry.onClick.AddListener(() => RequestAuthorization());
        }
        /// <summary>
        /// 请求授权
        /// </summary>
        private void RequestAuthorization()
        {
            WebRequest.Instance.Post(url, data, "application/json;charset=UTF-8", OnPostComplete);
        }
        /// <summary>
        /// 服务器回调函数
        /// </summary>
        /// <param name="args"></param>
        private void OnPostComplete(HttpCallBackArgs args)
        {
            if (args.HasError)//失败方法
            {
                Debug.LogError("返回错误：" + args.Value);

                Failed();
            }
            else//成功方法
            {
                Debug.Log("服务器返回：" + args.Value);
                string back = args.Value.AESDecrypt(Password);
                //Debug.Log(back);
                int lastIndex = back.Length - 1; // 计算最后一个字符的下标
                char lastCharacter = back[lastIndex]; // 获取最后一个字符
                string lastCharacterAsString = lastCharacter.ToString(); // 将字符转换为字符串
                if (lastCharacterAsString == "1")
                {
                    Success();
                }
                else
                {
                    Failed();
                }
            }
        }
        /// <summary>
        /// 授权成功
        /// </summary>
        private void Success()
        {
            //启动定时器
            countdown.Launch();
            Transform Child = transform.GetChild(0);
            Child.gameObject.SetActive(false);
        }
        /// <summary>
        /// 授权失败
        /// </summary>
        private void Failed()
        {
            //关闭定时器
            countdown.Stop();
            Transform Child = transform.GetChild(0);
            Child.gameObject.SetActive(true);
        }
    }
}