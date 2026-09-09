using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace WManager
{
    /// <summary>
    /// 运行时构建 UI 节点的内部工具。
    /// 框架自带的层节点、遮罩、Toast、Loading 都用它程序化生成，
    /// 因此这些功能不依赖任何美术资源，导入框架即可直接用。
    /// </summary>
    internal static class UIFactory
    {
        /// <summary>创建一个铺满父节点的 RectTransform 节点</summary>
        public static RectTransform CreateStretchNode(string name, Transform parent, params Type[] components)
        {
            var go = components == null || components.Length == 0
                ? new GameObject(name, typeof(RectTransform))
                : new GameObject(name, Combine(typeof(RectTransform), components));

            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            Stretch(rect);

            return rect;
        }

        /// <summary>把 RectTransform 铺满父节点</summary>
        public static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
        }

        /// <summary>创建一个纯色 / 圆角底图</summary>
        public static Image CreateImage(string name, Transform parent, Color color, bool rounded = false)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);

            var image = go.GetComponent<Image>();
            image.color = color;

            if (rounded)
            {
                image.sprite = UIProceduralSprite.RoundedRect;
                image.type = Image.Type.Sliced;
                image.pixelsPerUnitMultiplier = 1f;
            }

            return image;
        }

        /// <summary>创建一个 TMP 文本</summary>
        public static TextMeshProUGUI CreateText(
            string name,
            Transform parent,
            string content,
            float fontSize,
            Color color,
            TextAlignmentOptions alignment = TextAlignmentOptions.Center)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);

            var text = go.AddComponent<TextMeshProUGUI>();
            text.text = content;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = alignment;
            text.raycastTarget = false;
            text.richText = true;

            return text;
        }

        /// <summary>给节点加一个用于独立排序的子 Canvas</summary>
        public static Canvas AddSortingCanvas(GameObject go, int sortingOrder)
        {
            var canvas = go.GetComponent<Canvas>();
            if (canvas == null)
                canvas = go.AddComponent<Canvas>();

            canvas.overrideSorting = true;
            canvas.sortingOrder = sortingOrder;

            if (go.GetComponent<GraphicRaycaster>() == null)
                go.AddComponent<GraphicRaycaster>();

            return canvas;
        }

        private static Type[] Combine(Type first, Type[] rest)
        {
            var result = new Type[rest.Length + 1];
            result[0] = first;
            Array.Copy(rest, 0, result, 1, rest.Length);
            return result;
        }
    }

    /// <summary>
    /// 程序化生成的通用 Sprite。避免框架自带控件依赖美术资源。
    /// 纹理带 HideAndDontSave，不会被场景卸载连带释放。
    /// </summary>
    internal static class UIProceduralSprite
    {
        private static Sprite _roundedRect;
        private static Sprite _ring;

        /// <summary>九宫格圆角矩形，配合 Image.Type.Sliced 使用</summary>
        public static Sprite RoundedRect
        {
            get
            {
                if (_roundedRect == null)
                    _roundedRect = CreateRoundedRect(48, 16);

                return _roundedRect;
            }
        }

        /// <summary>带渐隐拖尾的圆环，用作 Loading 转圈</summary>
        public static Sprite Ring
        {
            get
            {
                if (_ring == null)
                    _ring = CreateRing(96, 0.33f, 0.45f);

                return _ring;
            }
        }

        private static Sprite CreateRoundedRect(int size, int radius)
        {
            var texture = NewTexture(size, "UI_RoundedRect");
            var pixels = new Color32[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // 到圆角圆心的距离：直边区域为 0，四角为真实距离
                    float dx = Mathf.Max(radius - (x + 0.5f), (x + 0.5f) - (size - radius), 0f);
                    float dy = Mathf.Max(radius - (y + 0.5f), (y + 0.5f) - (size - radius), 0f);
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);

                    float alpha = Mathf.Clamp01(radius - distance + 0.5f);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, false);

            var sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                new Vector4(radius, radius, radius, radius));

            sprite.name = "UI_RoundedRect";
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        private static Sprite CreateRing(int size, float innerRadius, float outerRadius)
        {
            var texture = NewTexture(size, "UI_Ring");
            var pixels = new Color32[size * size];

            float center = size * 0.5f;
            float edge = 1.5f / size;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float px = (x + 0.5f - center) / size;
                    float py = (y + 0.5f - center) / size;
                    float radius = Mathf.Sqrt(px * px + py * py);

                    float alpha = Mathf.Clamp01((radius - innerRadius) / edge)
                                  * Mathf.Clamp01((outerRadius - radius) / edge);

                    // 沿角度做渐隐，转起来才有"拖尾"的感觉
                    float angle = Mathf.Atan2(py, px);
                    float t = (angle + Mathf.PI) / (2f * Mathf.PI);
                    alpha *= Mathf.Clamp01(t);

                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, false);

            var sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
            sprite.name = "UI_Ring";
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        private static Texture2D NewTexture(int size, string name)
        {
            return new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = name,
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
        }
    }
}
