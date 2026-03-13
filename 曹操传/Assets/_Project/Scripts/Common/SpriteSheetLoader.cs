using System.Collections.Generic;
using UnityEngine;

namespace CaoCao.Common
{
    public static class SpriteSheetLoader
    {
        static readonly Dictionary<string, Sprite[]> _cache = new();

        public static Sprite[] LoadFrames(string resourcePath, int frameWidth, int frameHeight, int pixelsPerUnit = 48)
        {
            if (_cache.TryGetValue(resourcePath, out var cached))
                return cached;

            var tex = Resources.Load<Texture2D>(resourcePath);
            if (tex == null)
            {
                Debug.LogWarning($"[SpriteSheetLoader] Texture not found: {resourcePath}");
                return new Sprite[0];
            }

            int rows = tex.height / frameHeight;
            var sprites = new Sprite[rows];
            for (int i = 0; i < rows; i++)
            {
                // Frames go top to bottom (row 0 = top of texture)
                int y = tex.height - (i + 1) * frameHeight;
                var rect = new Rect(0, y, frameWidth, frameHeight);
                var pivot = new Vector2(0.5f, 0f);
                sprites[i] = Sprite.Create(tex, rect, pivot, pixelsPerUnit);
            }

            _cache[resourcePath] = sprites;
            return sprites;
        }

        public static Sprite LoadSingleSprite(string resourcePath)
        {
            return Resources.Load<Sprite>(resourcePath);
        }

        public static void ClearCache()
        {
            _cache.Clear();
        }
    }
}
