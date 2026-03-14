using System;
using UnityEngine;

namespace CaoCao.Battle
{
    /// <summary>
    /// Loads and caches battle unit sprite frames from sprite sheets.
    ///
    /// test_m.png (11 frames, 48x48 each, top-to-bottom):
    ///   0,1  = walk down
    ///   2,3  = walk up
    ///   4,5  = walk right  (flip for left)
    ///   6    = idle down
    ///   7    = idle up
    ///   8    = idle left    (flip for right)
    ///   9,10 = death gasp
    ///
    /// test_s.png (5 frames, 48x48 each, top-to-bottom):
    ///   0 = block down
    ///   1 = block up
    ///   2 = block left     (flip for right)
    ///   3 = hit/damaged
    ///   4 = victory/levelup
    /// </summary>
    public static class BattleSpriteSheet
    {
        static Sprite[] _moveSprites;  // 11 frames from test_m
        static Sprite[] _skillSprites; // 5 frames from test_s
        static bool _loaded;

        public static void EnsureLoaded()
        {
            if (_loaded) return;
            _loaded = true;

            // Try loading processed PNG sprites first (from editor-processed sprite sheet)
            _moveSprites = LoadSorted("Sprites/Battle/test_m");
            _skillSprites = LoadSorted("Sprites/Battle/test_s");

            // Fallback: if PNG not available or not sliced, try BMP with runtime slicing
            if (_moveSprites == null || _moveSprites.Length < 11)
            {
                Debug.LogWarning($"[BattleSpriteSheet] test_m: expected 11 sprites, got {_moveSprites?.Length ?? 0}. Runtime slicing from BMP...");
                _moveSprites = RuntimeSliceWithAlpha("Sprites/Battle/test_m", 11);
            }

            if (_skillSprites == null || _skillSprites.Length < 5)
            {
                Debug.LogWarning($"[BattleSpriteSheet] test_s: expected 5 sprites, got {_skillSprites?.Length ?? 0}. Runtime slicing from BMP...");
                _skillSprites = RuntimeSliceWithAlpha("Sprites/Battle/test_s", 5);
            }

            Debug.Log($"[BattleSpriteSheet] Loaded: move={_moveSprites?.Length}, skill={_skillSprites?.Length}");
        }

        static Sprite[] LoadSorted(string path)
        {
            var sprites = Resources.LoadAll<Sprite>(path);
            if (sprites != null && sprites.Length > 1)
                Array.Sort(sprites, (a, b) => ExtractIndex(a.name).CompareTo(ExtractIndex(b.name)));
            return sprites;
        }

        /// <summary>
        /// Fallback: load texture, replace purple bg with transparency, and slice at runtime.
        /// </summary>
        static Sprite[] RuntimeSliceWithAlpha(string path, int frameCount)
        {
            var srcTex = Resources.Load<Texture2D>(path);
            if (srcTex == null)
            {
                Debug.LogError($"[BattleSpriteSheet] Cannot load texture: {path}");
                return new Sprite[0];
            }

            // Create a readable RGBA copy and replace purple background
            var tex = new Texture2D(srcTex.width, srcTex.height, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;

            // Try to copy pixels — if source is not readable, this may fail
            Color32[] pixels;
            try
            {
                pixels = srcTex.GetPixels32();
            }
            catch
            {
                Debug.LogError($"[BattleSpriteSheet] Texture not readable: {path}. Run CaoCao > Process Battle Sprites first.");
                return new Sprite[0];
            }

            // Replace purple background RGB(247, 0, 255) with transparent
            for (int i = 0; i < pixels.Length; i++)
            {
                var p = pixels[i];
                if (p.r >= 230 && p.g <= 30 && p.b >= 240)
                    pixels[i] = new Color32(0, 0, 0, 0);
            }

            tex.SetPixels32(pixels);
            tex.Apply();

            int frameH = 48;
            int frameW = 48;
            var sprites = new Sprite[frameCount];

            for (int i = 0; i < frameCount; i++)
            {
                float y = tex.height - (i + 1) * frameH;
                var rect = new Rect(0, y, frameW, frameH);
                sprites[i] = Sprite.Create(tex, rect, new Vector2(0.5f, 0.5f), 1f);
                sprites[i].name = $"{tex.name}_{i}";
            }

            return sprites;
        }

        // === Move sprite accessors (test_m) ===

        // Walk frames (2 frames each direction)
        public static Sprite WalkDown0 => GetMove(0);
        public static Sprite WalkDown1 => GetMove(1);
        public static Sprite WalkUp0 => GetMove(2);
        public static Sprite WalkUp1 => GetMove(3);
        public static Sprite WalkRight0 => GetMove(4);
        public static Sprite WalkRight1 => GetMove(5);
        // Walk left = flip walk right (handled by animator)

        // Idle frames
        public static Sprite IdleDown => GetMove(6);
        public static Sprite IdleUp => GetMove(7);
        public static Sprite IdleLeft => GetMove(8);
        // Idle right = flip idle left (handled by animator)

        // Death frames
        public static Sprite DeathGasp0 => GetMove(9);
        public static Sprite DeathGasp1 => GetMove(10);

        // === Skill sprite accessors (test_s) ===
        public static Sprite BlockDown => GetSkill(0);
        public static Sprite BlockUp => GetSkill(1);
        public static Sprite BlockLeft => GetSkill(2);
        // Block right = flip block left
        public static Sprite Hit => GetSkill(3);
        public static Sprite Victory => GetSkill(4);

        /// <summary>
        /// Extract numeric suffix from sprite name (e.g., "test_m_10" → 10).
        /// </summary>
        static int ExtractIndex(string name)
        {
            int lastUnderscore = name.LastIndexOf('_');
            if (lastUnderscore >= 0 && int.TryParse(name.Substring(lastUnderscore + 1), out int index))
                return index;
            return 0;
        }

        static Sprite GetMove(int index)
        {
            EnsureLoaded();
            if (_moveSprites == null || index >= _moveSprites.Length) return null;
            return _moveSprites[index];
        }

        static Sprite GetSkill(int index)
        {
            EnsureLoaded();
            if (_skillSprites == null || index >= _skillSprites.Length) return null;
            return _skillSprites[index];
        }

        /// <summary>
        /// Get all move sprites (for sorting by name if needed).
        /// </summary>
        public static Sprite[] MoveSprites
        {
            get { EnsureLoaded(); return _moveSprites; }
        }

        public static Sprite[] SkillSprites
        {
            get { EnsureLoaded(); return _skillSprites; }
        }
    }
}
