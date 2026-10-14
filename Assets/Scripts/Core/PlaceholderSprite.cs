using System.Collections.Generic;
using UnityEngine;

namespace PixelDash.Core
{
    /// <summary>
    /// 程序化占位精灵：美术资源未迁移时用白色方块生成 Sprite。
    /// MakeSprite(worldSize) 会按目标世界尺寸反推 pixelsPerUnit，使 Sprite 世界尺寸恰为 worldSize，
    /// 从而无需依赖 transform.localScale（避免与 Collider2D 尺寸互相干扰）。
    /// </summary>
    public static class PlaceholderSprite
    {
        private static readonly Dictionary<int, Sprite> Cache = new Dictionary<int, Sprite>();

        public static Sprite MakeSprite(float worldSize)
        {
            int key = Mathf.Max(1, Mathf.RoundToInt(worldSize * 100f));
            if (Cache.TryGetValue(key, out var cached) && cached != null)
                return cached;

            const int px = 8;
            var tex = new Texture2D(px, px, TextureFormat.RGBA32, false);
            var colors = new Color32[px * px];
            for (int i = 0; i < colors.Length; i++)
                colors[i] = new Color32(255, 255, 255, 255);
            tex.SetPixels32(colors);
            tex.Apply();
            tex.filterMode = FilterMode.Point;

            float ppu = px / Mathf.Max(0.0001f, worldSize); // pixelsPerUnit：世界尺寸 = px / ppu = worldSize
            var sp = Sprite.Create(tex, new Rect(0, 0, px, px), new Vector2(0.5f, 0.5f), ppu);
            sp.name = "placeholder_" + key;
            Cache[key] = sp;
            return sp;
        }

        /// <summary>
        /// 为 GameObject 装配占位渲染。
        /// 若该对象已有 sprite（用户在 Inspector 指定了真实美术），则**不覆盖**，仅在缺省时补占位。
        /// </summary>
        public static SpriteRenderer EnsureRenderer(GameObject go, Color color, float worldSize = 1f)
        {
            var sr = go.GetComponent<SpriteRenderer>();
            if (sr == null) sr = go.AddComponent<SpriteRenderer>();
            if (sr.sprite == null)
            {
                sr.sprite = MakeSprite(worldSize);
                sr.color = color;
            }
            return sr;
        }
    }
}
