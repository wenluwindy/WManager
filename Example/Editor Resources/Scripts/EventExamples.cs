// ----------------------------------------------------------------------------
// EventExamples.cs
//
// 演示 EventManager（基于 UnityEvent 的全局事件总线）：
// - 基本订阅/反订阅
// - 延迟发射 / 发送者信息 / 数据存储
// - 优先级订阅
// - 暂停与恢复监听
// - EventsGroup：把一组订阅打包
// ----------------------------------------------------------------------------
using Sirenix.OdinInspector;
using UnityEngine;
using WManager;

namespace WManager.Example
{
    public class EventExample : MonoBehaviour
    {
        private EventsGroup _group;
        private bool _isLevelUpPaused;   // 用于 PauseAndResumeExample 的切换状态

        private void OnEnable()
        {
            // ----- 1. 基本订阅：使用名字完全匹配 -----
            EventManager.StartListening("OnPlayerLevelUp", OnPlayerLevelUp);

            // ----- 2. 带 callBackID 订阅：之后可以用 ID 解绑（不依赖具体方法引用）-----
            EventManager.StartListening("OnGamePaused", OnGamePaused, callBackID: "EventExample");

            // ----- 3. 优先级订阅：数值越小越先触发；和普通订阅并存 -----
            EventManager.StartListeningWithPriority("OnPlayerLevelUp", OnPlayerLevelUpLate, priority: 100);
            EventManager.StartListeningWithPriority("OnPlayerLevelUp", OnPlayerLevelUpEarly, priority: 0);

            // ----- 4. EventsGroup：把多个订阅打包 -----
            _group = new EventsGroup();
            _group.Add("OnPlayerLevelUp", OnLevelUp_Group);
            _group.Add("OnCoinChanged", OnCoinChanged_Group);
            _group.Add("OnCoinBonus", OnCoinBonus_Group);   // 对应 Emit #3 的延迟事件
            _group.StartListening();
        }

        private void OnDisable()
        {
            // 必须配对，避免内存泄漏
            EventManager.StopListening("OnPlayerLevelUp", OnPlayerLevelUp);
            EventManager.StopListening("OnPlayerLevelUp", OnPlayerLevelUpEarly);
            EventManager.StopListening("OnPlayerLevelUp", OnPlayerLevelUpLate);

            // 带 callBackID 停止监听
            EventManager.StopListening("OnGamePaused", "EventExample");

            // 整组停止
            _group.StopListening();
            _group = null;

            // 重置 toggle 状态，避免下次启用时状态错乱
            _isLevelUpPaused = false;
        }

        // ============================================================================
        // 触发事件
        // ============================================================================
        [Button("测试事件")]
        public void EmitEvents()
        {
            // ─── A. 基本发射：无 sender ───────────────────────────────
            Debug.Log("<color=#FFB347>━━━ [Emit #1] EmitEvent(string) —— 不带 sender 触发 OnPlayerLevelUp ━━━</color>");
            EventManager.EmitEvent("OnPlayerLevelUp");

            // ─── B. 带发送者（接收方可通过 EventManager.GetSender("xxx") 取到）──
            Debug.Log("<color=#FFB347>━━━ [Emit #2] EmitEvent(string, object) —— 带 sender=this 触发 OnPlayerLevelUp ━━━</color>");
            EventManager.EmitEvent("OnPlayerLevelUp", this);

            // ─── C. 延迟发射（fire-and-forget；注意 StopListening 不能取消延迟发射）──
            // 故意用不同的事件名 OnCoinBonus，避免与 Emit #4 的 OnCoinChanged 混淆
            Debug.Log("<color=#FFB347>━━━ [Emit #3] EmitEvent(string, delay=1s) —— 1 秒后触发 OnCoinBonus ━━━</color>");
            EventManager.EmitEvent("OnCoinBonus", delay: 1f);

            // ─── D. 带数据存储：发射 + 写一个对象，接收方用 GetData 取 ──
            Debug.Log("<color=#FFB347>━━━ [Emit #4] EmitEventData(string, object, delay=3s) —— 3 秒后触发 OnCoinChanged 并写入 data=999 ━━━</color>");
            EventManager.EmitEventData("OnCoinChanged", data: 999, delay: 3f);
        }

        // ============================================================================
        // 回调签名必须是 UnityAction（无参）
        // ============================================================================

        // —— 基本订阅（StartListening）——
        private void OnPlayerLevelUp()
        {
            object sender = EventManager.GetSender("OnPlayerLevelUp");
            string senderDesc = sender == null
                ? "<color=#FF6B6B>空</color>（来自 Emit #1：无 sender 重载）"
                : $"<color=#90EE90>{sender}</color>（来自 Emit #2：带 sender 重载）";
            Debug.Log($"<color=#87CEEB>[基本订阅]</color> OnPlayerLevelUp 触发，sender = {senderDesc}");
        }

        // —— 优先级订阅（priority 越小越先触发）——
        private void OnPlayerLevelUpEarly()
        {
            Debug.Log("<color=#DDA0DD>[优先级订阅 p=0]</color> OnPlayerLevelUpEarly <color=#FFD700>★ 先触发</color>");
        }

        private void OnPlayerLevelUpLate()
        {
            Debug.Log("<color=#DDA0DD>[优先级订阅 p=100]</color> OnPlayerLevelUpLate <color=#FFD700>★ 后触发</color>");
        }

        // —— 带 callBackID 订阅（按 ID 解绑）——
        private void OnGamePaused()
        {
            Debug.Log("<color=#98FB98>[带ID订阅 / callBackID=\"EventExample\"]</color> OnGamePaused 触发");
        }

        // —— EventsGroup 订阅 ──
        private void OnLevelUp_Group()
        {
            Debug.Log("<color=#FFA07A>[EventsGroup 订阅]</color> OnPlayerLevelUp 触发（与上面基本订阅走的是不同 UnityEvent 实例）");
        }

        private void OnCoinChanged_Group()
        {
            int coin = EventManager.GetInt("OnCoinChanged");
            // Emit #4 才会写 data=999，所以这里的 coin 一定是 999
            Debug.Log($"<color=#FFA07A>[EventsGroup 订阅]</color> OnCoinChanged 触发，value = {coin}   ← 来源：<color=#90EE90>Emit #4 EmitEventData(delay=3s)</color>");
        }

        private void OnCoinBonus_Group()
        {
            Debug.Log("<color=#FFA07A>[EventsGroup 订阅]</color> OnCoinBonus 触发   ← 来源：<color=#FF6B6B>Emit #3 EmitEvent(delay=1s)（1 秒延迟到达）</color>");
        }

        // ============================================================================
        // 暂停 / 恢复监听 —— 点一次按钮切换一次状态
        // ============================================================================
        [Button("测试暂停 / 恢复监听（点我切换）")]
        public void PauseAndResumeExample()
        {
            if (!_isLevelUpPaused)
            {
                // ----- 当前是"监听中"，点一下 → 暂停 -----
                EventManager.PauseListening("OnPlayerLevelUp");
                _isLevelUpPaused = true;
                Debug.Log("<color=#FF6B6B>[Pause] ▶ 已暂停 OnPlayerLevelUp</color>  —— 下面的 Emit 不会回调");

                // 演示：此时 emit 不会回调
                EventManager.EmitEvent("OnPlayerLevelUp");
                Debug.Log("    → 已 Emit，但订阅者【不会】收到回调（已暂停）");
            }
            else
            {
                // ----- 当前是"已暂停"，点一下 → 恢复 -----
                EventManager.RestartListening("OnPlayerLevelUp");
                _isLevelUpPaused = false;
                Debug.Log("<color=#90EE90>[Resume] ▶ 已恢复 OnPlayerLevelUp</color>  —— 下面的 Emit 会正常回调");

                // 演示：此时 emit 会回调
                EventManager.EmitEvent("OnPlayerLevelUp");
                Debug.Log("    → 已 Emit，订阅者【会】收到回调（已恢复）");
            }
        }

        // ============================================================================
        // 一次性清理（场景销毁 / 切账号）
        // ============================================================================
        public void ClearAll()
        {
            // StopAll 移除所有 UnityEvent 监听
            EventManager.StopAll();

            // DisposeAll 清空 storage / sender
            EventManager.DisposeAll();
        }
    }

    // ============================================================================
    // 用法 2：单例管理器也可以订阅全局事件
    // ============================================================================
    public class GameStateManagerExample : MonoBehaviour
    {
        public static GameStateManagerExample Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            EventManager.StartListening("OnPlayerLevelUp", HandleLevelUp);
        }

        private void OnDisable()
        {
            EventManager.StopListening("OnPlayerLevelUp", HandleLevelUp);
            if (Instance == this) Instance = null;
        }

        private void HandleLevelUp()
        {
            Debug.Log("<color=#87CEEB>[单例订阅]</color> GameStateManagerExample 收到 OnPlayerLevelUp，做存档/更新 UI 等");
        }
    }
}
