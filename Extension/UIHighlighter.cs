using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using HighlightingSystem;
using UnityEngine;
using UnityEngine.UI;

namespace WManager
{
    [RequireComponent(typeof(Outline))]
    public class UIHighlighter : MonoBehaviour
    {
        void Start()
        {
            // 获取主相机
            var mCamera = Camera.main;

            // 获取Outline组件并禁用
            var outline = GetComponent<Outline>();
            outline.enabled = false;

            // 检查主相机上是否含有HighlightingRenderer组件并且已启用
            if (mCamera.GetComponent<HighlightingRenderer>().enabled)
            {
                // 启用Outline组件并设置颜色和距离
                outline.enabled = true;
                outline.effectColor = Color.red;
                outline.effectDistance = new Vector2(3, -3);

                // 创建一个循环动画，将Outline颜色从当前颜色渐变到红色，并且使用线性缓动
                var a = DOTween.To(() => outline.effectColor, x => outline.effectColor = x, new Color(1, 0, 0, 0), 1f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.Linear);
            }
        }
        void OnDestroy()
        {
            Destroy(GetComponent<Outline>());
        }
    }
}
