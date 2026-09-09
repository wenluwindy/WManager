// ----------------------------------------------------------------------------
// SaveExamples.cs
//
// 演示 SaveManager（使用 Odin Inspector 按钮）：
// - 文件存档 Save/Load（自动 AES 加密）
// - PlayerPrefs 存档 SaveToPrefs/LoadFromPrefs
// - 自定义可序列化数据类
// - 异步版本 SaveAsync/LoadAsync
// ----------------------------------------------------------------------------
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using WManager;

namespace WManager.Example
{
    public class SaveExamples : MonoBehaviour
    {
        [Title("文件存档设置")]
        [LabelText("存档路径")]
        private const string FilePath = "Player/MainSave";

        [Title("PlayerPrefs 存档设置")]
        [LabelText("Prefs 键名")]
        private const string PrefsKey = "PlayerQuickSlot";

        [Title("测试数据")]
        [LabelText("玩家名称")]
        public string playerName = "Albert";

        [LabelText("玩家等级")]
        public int playerLevel = 12;

        [LabelText("金币数量")]
        public int playerCoin = 9999;

        [LabelText("位置坐标")]
        public Vector3 playerPosition = new Vector3(1.5f, 0f, -3.2f);

        [LabelText("已解锁物品")]
        public bool[] unlockedItems = new[] { true, false, true, true };

        private string _lastOperationResult = "";

        [ShowInInspector]
        [LabelText("操作结果")]
        [TextArea(3, 6)]
        [ReadOnly]
        private string LastOperationResult
        {
            get => _lastOperationResult;
            set => _lastOperationResult = value;
        }

        // ============================================================================
        // 文件存档按钮
        // ============================================================================
        [Button("保存游戏（文件）", ButtonSizes.Large)]
        [GUIColor(0.2f, 0.8f, 0.2f)]
        private void SaveToFile()
        {
            try
            {
                var data = CreateSaveData();
                SaveManager.Save(data, FilePath);
                _lastOperationResult = $"[成功] 同步存档完成\n路径: {FilePath}\n数据: {data}";
                Debug.Log("[Save] 同步存档完成：" + data);
            }
            catch (Exception ex)
            {
                _lastOperationResult = $"[失败] 存档出错\n{ex.Message}";
                Debug.LogError("[Save] 存档失败：" + ex);
            }
        }

        [Button("读取游戏（文件）", ButtonSizes.Large)]
        [GUIColor(0.2f, 0.5f, 0.9f)]
        private void LoadFromFile()
        {
            try
            {
                if (!SaveManager.Exists(FilePath))
                {
                    _lastOperationResult = $"[提示] 存档不存在：{FilePath}\n请先进行存档";
                    Debug.LogWarning($"[Save] 存档不存在：{FilePath}");
                    return;
                }

                var data = SaveManager.Load<PlayerSaveData>(FilePath);
                if (data == null)
                {
                    _lastOperationResult = "[失败] 反序列化失败，可能被改动或文件损坏";
                    Debug.LogError("[Save] 反序列化失败，可能被改动或文件损坏");
                    return;
                }

                _lastOperationResult = $"[成功] 同步读档完成\n数据: {data}";
                Debug.Log("[Save] 同步读档：" + data);

                // 同步到 Inspector 显示
                SyncDataToInspector(data);
            }
            catch (Exception ex)
            {
                _lastOperationResult = $"[失败] 读档出错\n{ex.Message}";
                Debug.LogError("[Save] 读档失败：" + ex);
            }
        }

        [Button("删除存档（文件）", ButtonSizes.Medium)]
        [GUIColor(0.9f, 0.3f, 0.3f)]
        private void DeleteFileSave()
        {
            try
            {
                SaveManager.DeleteData(FilePath);
                _lastOperationResult = $"[成功] 已删除存档：{FilePath}";
                Debug.Log($"[Save] 已删除存档：{FilePath}");
            }
            catch (Exception ex)
            {
                _lastOperationResult = $"[失败] 删除失败\n{ex.Message}";
                Debug.LogError("[Save] 删除失败：" + ex);
            }
        }

        // ============================================================================
        // 异步存档按钮
        // ============================================================================
        [Button("异步保存游戏", ButtonSizes.Medium)]
        [GUIColor(0.3f, 0.7f, 0.7f)]
        private async void SaveAsync()
        {
            try
            {
                var data = CreateSaveData();
                // 1. 直接获取 DestroyToken
                var token = this.GetDestroyToken();

                _lastOperationResult = "[进行中] 异步存档中...";

                // 2. 将 token 传给异步操作
                await SaveManager.SaveAsync(data, FilePath, token);

                var loaded = await SaveManager.LoadAsync<PlayerSaveData>(FilePath, token);
                _lastOperationResult = $"[成功] 异步存档并验证完成\n数据: {loaded}";
                Debug.Log("[Save] 异步存档完成：" + loaded);
            }
            catch (OperationCanceledException)
            {
                _lastOperationResult = "[取消] 异步存档已取消";
                Debug.Log("[Save] 异步存档已取消");
            }
            catch (Exception ex)
            {
                _lastOperationResult = $"[失败] 异步存档出错\n{ex.Message}";
                Debug.LogError("[Save] 异步存档失败：" + ex);
            }
        }

        // ============================================================================
        // PlayerPrefs 存档按钮
        // ============================================================================
        [Button("保存到 PlayerPrefs", ButtonSizes.Medium)]
        [GUIColor(0.6f, 0.4f, 0.8f)]
        private void SaveToPrefs()
        {
            try
            {
                var data = CreateSaveData();
                data.name = "Prefs_" + data.name;

                SaveManager.SaveToPrefs(data, PrefsKey);
                _lastOperationResult = $"[成功] 已保存到 PlayerPrefs\n键: {PrefsKey}";
                Debug.Log("[Save] 写入 PlayerPrefs");
            }
            catch (Exception ex)
            {
                _lastOperationResult = $"[失败] 保存失败\n{ex.Message}";
                Debug.LogError("[Save] PlayerPrefs 保存失败：" + ex);
            }
        }

        [Button("从 PlayerPrefs 读取", ButtonSizes.Medium)]
        [GUIColor(0.5f, 0.3f, 0.7f)]
        private void LoadFromPrefs()
        {
            try
            {
                if (!SaveManager.ExistsInPrefs(PrefsKey))
                {
                    _lastOperationResult = $"[提示] PlayerPrefs 中没有该 key：{PrefsKey}";
                    Debug.LogWarning("[Save] Prefs 中没有该 key");
                    return;
                }

                var data = SaveManager.LoadFromPrefs<PlayerSaveData>(PrefsKey);
                _lastOperationResult = $"[成功] 从 PlayerPrefs 读取\n数据: {data}";
                Debug.Log("[Save] 从 PlayerPrefs 读出：" + data);

                SyncDataToInspector(data);
            }
            catch (Exception ex)
            {
                _lastOperationResult = $"[失败] 读取失败\n{ex.Message}";
                Debug.LogError("[Save] PlayerPrefs 读取失败：" + ex);
            }
        }

        [Button("删除 PlayerPrefs 存档", ButtonSizes.Small)]
        [GUIColor(0.8f, 0.3f, 0.3f)]
        private void DeletePrefsSave()
        {
            try
            {
                SaveManager.DeletePref(PrefsKey);
                _lastOperationResult = $"[成功] 已删除 PlayerPrefs 存档：{PrefsKey}";
                Debug.Log($"[Save] 已删除 PlayerPrefs：{PrefsKey}");
            }
            catch (Exception ex)
            {
                _lastOperationResult = $"[失败] 删除失败\n{ex.Message}";
                Debug.LogError("[Save] 删除 PlayerPrefs 失败：" + ex);
            }
        }

        // ============================================================================
        // 物品解锁功能
        // ============================================================================
        [Title("物品解锁测试")]
        [LabelText("物品索引")]
        [Range(0, 20)]
        public int testItemIndex = 0;

        [Button("解锁物品", ButtonSizes.Small)]
        private void UnlockItemButton()
        {
            try
            {
                UnlockItem(testItemIndex);
                _lastOperationResult = $"[成功] 已解锁物品 #{testItemIndex}";
                Debug.Log($"[Save] 解锁物品 #{testItemIndex}");
            }
            catch (Exception ex)
            {
                _lastOperationResult = $"[失败] 解锁失败\n{ex.Message}";
                Debug.LogError("[Save] 解锁物品失败：" + ex);
            }
        }

        [Button("检查物品状态", ButtonSizes.Small)]
        private void CheckItemStatus()
        {
            try
            {
                bool unlocked = IsItemUnlocked(testItemIndex);
                _lastOperationResult = $"[物品 #{testItemIndex}] {(unlocked ? "已解锁" : "未解锁")}";
                Debug.Log($"[Save] 物品 #{testItemIndex} 状态：{(unlocked ? "已解锁" : "未解锁")}");
            }
            catch (Exception ex)
            {
                _lastOperationResult = $"[失败] 检查失败\n{ex.Message}";
                Debug.LogError("[Save] 检查物品状态失败：" + ex);
            }
        }

        // ============================================================================
        // 辅助方法
        // ============================================================================
        private PlayerSaveData CreateSaveData()
        {
            return new PlayerSaveData
            {
                name = playerName,
                level = playerLevel,
                coin = playerCoin,
                position = new[] { playerPosition.x, playerPosition.y, playerPosition.z },
                unlockedItems = unlockedItems,
            };
        }

        private void SyncDataToInspector(PlayerSaveData data)
        {
            playerName = data.name;
            playerLevel = data.level;
            playerCoin = data.coin;
            if (data.position != null && data.position.Length >= 3)
            {
                playerPosition = new Vector3(data.position[0], data.position[1], data.position[2]);
            }
            if (data.unlockedItems != null)
            {
                unlockedItems = data.unlockedItems;
            }
        }

        public bool IsItemUnlocked(int itemIndex)
        {
            var data = SaveManager.Load<PlayerSaveData>(FilePath);
            if (data?.unlockedItems == null) return false;
            return itemIndex >= 0 && itemIndex < data.unlockedItems.Length && data.unlockedItems[itemIndex];
        }

        public void UnlockItem(int itemIndex)
        {
            var data = SaveManager.Load<PlayerSaveData>(FilePath);
            if (data == null) data = new PlayerSaveData();

            if (data.unlockedItems == null)
                data.unlockedItems = new bool[itemIndex + 1];

            if (itemIndex >= data.unlockedItems.Length)
                Array.Resize(ref data.unlockedItems, itemIndex + 1);

            data.unlockedItems[itemIndex] = true;
            SaveManager.Save(data, FilePath);
        }
    }
}