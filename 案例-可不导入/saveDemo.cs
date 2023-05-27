using UnityEngine;
using UnityEngine.UI;
using System;

namespace WManager.Save
{
    /// <summary>
    ///  存档系统案例
    /// </summary>
    public class saveDemo : MonoBehaviour
    {
        public InputField text;
        public Slider knobX;
        public Slider knobY;
        public RectTransform knob;

        public Transform a;

        [Serializable]
        public class PlayerDate
        {
            public int hp;
            public float cd;
            public string name;
            public double time;
            public bool enabled;
            public Color color;
            public Vector2 vector2 = new Vector2(0, 0);
            public Vector3 vector3 = new Vector3(0, 0, 0);
        }
        public PlayerDate gamer;

        public void SetKnobPos()
        {
            if (!knob || !knobX || !knobY) return;
            knob.anchoredPosition = new Vector2(knobX.value, knobY.value);//原点跟随拖动条
        }

        public void Save()
        {
            if (!text || !knob) return;
            SaveManager.Save(text.text, "测试文本");//保存string
            SaveManager.Save(knob, "圆点位置");//保存RectTransform
            SaveManager.Save(a, "a");//保存Transform
            SaveManager.Save(gamer, "玩家数据");//保存自定义类

            //webGL只能使用web保存方式
            SaveManager.SaveWeb(knobX.value, "x值");
            SaveManager.SaveWeb(knobY.value, "y值");
        }

        public void Load()
        {
            if (!text || !knob || !knobX || !knobY) return;
            text.text = SaveManager.Load<string>("测试文本");
            var rt = SaveManager.Load<RectTransform>("圆点位置");
            knob.localPosition = rt.localPosition;
            var a1 = SaveManager.Load<Transform>("a");
            a.position = a1.localPosition;
            gamer = SaveManager.Load<PlayerDate>("玩家数据");

            knobX.value = SaveManager.LoadWeb<float>("x值");
            knobY.value = SaveManager.LoadWeb<float>("y值");
        }
    }
}