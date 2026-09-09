// ----------------------------------------------------------------------------
// ServiceLocatorExamples.cs
//
// 演示 IServiceLocator / DefaultServiceLocator：
// - 注册单例、工厂方法
// - 从业务模块取服务，不直接依赖具体实现类
// - Require 抛异常、Resolve 返回 null
// ----------------------------------------------------------------------------
using UnityEngine;
using WManager;

namespace WManager.Example
{
    /// <summary>
    /// 全局单例 ServiceLocator（模块间解耦的核心）。
    /// 用法：
    ///   ServiceLocatorExample.Locator.Register&lt;IXxxService&gt;(new XxxService());
    ///   var svc = ServiceLocatorExample.Locator.Require&lt;IXxxService&gt;();
    /// </summary>
    public static class ServiceLocatorExample
    {
        public static readonly IServiceLocator Locator = new DefaultServiceLocator();

        // 想自定义实现可以替换：
        // public static IServiceLocator Locator { get; } = new MyCustomLocator();
    }

    // ============================================================================
    // 示例接口与实现
    // ============================================================================

    /// <summary>金币服务（业务侧只依赖接口）</summary>
    public interface IPlayerWalletService
    {
        int GetCoin();
        void AddCoin(int amount);
        bool SpendCoin(int amount);
    }

    /// <summary>金币服务的本地实现</summary>
    public class PlayerWalletServiceExample : IPlayerWalletService
    {
        private int _coin;

        public int GetCoin() => _coin;

        public void AddCoin(int amount)
        {
            _coin += amount;
            Debug.Log($"[Wallet] +{amount} → coin = {_coin}");
        }

        public bool SpendCoin(int amount)
        {
            if (_coin < amount) return false;
            _coin -= amount;
            Debug.Log($"[Wallet] -{amount} → coin = {_coin}");
            return true;
        }
    }

    /// <summary>日志服务（用 Register&lt;T&gt;(Func&lt;T&gt;) 注册延迟构造）</summary>
    public interface ILoggerService
    {
        void Log(string msg);
    }

    public class UnityLoggerServiceExample : ILoggerService
    {
        public void Log(string msg) => Debug.Log($"[Logger] {msg}");
    }

    public class BootstrapRegistrarExample : MonoBehaviour
    {
        private void Awake()
        {
            // 1) 直接注册实例：适合无状态或单例服务
            ServiceLocatorExample.Locator.Register<IPlayerWalletService>(new PlayerWalletServiceExample());

            // 2) 注册工厂：第一次 Resolve 时才创建
            ServiceLocatorExample.Locator.Register<ILoggerService>(() => new UnityLoggerServiceExample());

            // 3) 也可以注册 UnityEngine.Object（注入 MonoBehaviour 单例）
            ServiceLocatorExample.Locator.Register<WebRequest>(WebRequest.Instance);
        }
    }

    public class SomeBusinessModuleExample : MonoBehaviour
    {
        // 业务侧只引用接口，不引用具体实现 → 可替换实现、可单元测试
        private IPlayerWalletService _wallet;
        private ILoggerService _logger;

        private void Awake()
        {
            // Resolve：未注册返回 null；想强制要求用 Require（未注册抛异常）
            _wallet = ServiceLocatorExample.Locator.Resolve<IPlayerWalletService>();
            _logger = ServiceLocatorExample.Locator.Resolve<ILoggerService>();

            if (_wallet == null)
            {
                Debug.LogError("IPlayerWalletService 未注册");
                return;
            }
        }

        public void OnBuyItem(int price)
        {
            if (_wallet.SpendCoin(price))
            {
                _logger?.Log($"购买成功，剩余 {_wallet.GetCoin()}");
            }
            else
            {
                _logger?.Log("金币不足");
            }
        }

        public void OnGameReward(int amount)
        {
            _wallet.AddCoin(amount);
        }

        // Require 用法：缺少依赖时直接抛 InvalidOperationException
        public void MustUseService()
        {
            var svc = ServiceLocatorExample.Locator.Require<IPlayerWalletService>();
            svc.AddCoin(100);
        }

        // 注销：模块销毁/登出时调用
        public void OnLogout()
        {
            ServiceLocatorExample.Locator.Unregister<IPlayerWalletService>();
            ServiceLocatorExample.Locator.Unregister<ILoggerService>();
        }
    }

    // ============================================================================
    // Result&lt;T&gt; 示例：把"成功/失败+错误信息"显式化
    // ============================================================================
    public class ResultExample
    {
        // 业务侧
        public Result<int> TryLoadCoin()
        {
            if (SaveManager.Exists("Player/MainSave"))
            {
                int coin = SaveManager.Load<int>("Player/MainSave");
                return Result<int>.Success(coin);
            }
            return Result<int>.Failure("存档不存在");
        }

        public void UseIt()
        {
            var result = TryLoadCoin();

            // 1) 命令式
            if (result.IsSuccess)
                Debug.Log($"有存档，金币 = {result.Value}");
            else
                Debug.LogWarning($"读档失败：{result.Error}");

            // 2) 函数式 Match：避免 if/else 嵌套
            var msg = result.Match(
                onSuccess: coin => $"金币 {coin}",
                onFailure: err => $"读取失败：{err}");
            Debug.Log(msg);
        }
    }
}
