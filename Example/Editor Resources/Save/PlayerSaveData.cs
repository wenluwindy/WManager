using System;

namespace WManager.Example
{
    /// <summary>
    /// 一个简单的可序列化数据类。可以放在 Assets/Scripts 项目任意位置。
    /// 字段必须是 public + [Serializable]，否则 JsonUtility 序列化失败。
    /// </summary>
    [Serializable]
    public class PlayerSaveData
    {
        public string name = "Hero";
        public int level = 1;
        public int coin = 0;
        public float[] position = new float[3];
        public bool[] unlockedItems;

        public override string ToString()
        {
            return $"name={name}, level={level}, coin={coin}, " +
                   $"pos=({position[0]:F1},{position[1]:F1},{position[2]:F1}), " +
                   $"unlockedItems={unlockedItems?.Length ?? 0}";
        }
    }
}