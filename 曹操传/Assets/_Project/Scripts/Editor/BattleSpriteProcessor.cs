using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

namespace CaoCao.Editor
{
    /// <summary>
    /// Converts battle BMP sprite sheets to PNG with transparent background,
    /// then configures import settings for proper sprite slicing.
    ///
    /// test_m.bmp → test_m.png (11 frames, 48x48 each)
    /// test_s.bmp → test_s.png (5 frames, 48x48 each)
    ///
    /// Purple background RGB(247, 0, 255) is replaced with transparency.
    /// </summary>
    public static class BattleSpriteProcessor
    {
        const int FrameW = 48;
        const int FrameH = 48;
        const string BattleSpritePath = "Assets/_Project/Resources/Sprites/Battle";

        [MenuItem("CaoCao/Process Battle Sprites (BMP → PNG)")]
        public static void ProcessBattleSprites()
        {
            ProcessFile("test_m", 11);
            ProcessFile("test_s", 5);
            AssetDatabase.Refresh();
            Debug.Log("[BattleSpriteProcessor] Done! Check Sprites/Battle for PNG files.");
        }

        static void ProcessFile(string baseName, int frameCount)
        {
            string bmpPath = Path.Combine(BattleSpritePath, baseName + ".bmp");
            string pngPath = Path.Combine(BattleSpritePath, baseName + ".png");
            string fullBmpPath = Path.GetFullPath(bmpPath);

            if (!File.Exists(fullBmpPath))
            {
                Debug.LogError($"[BattleSpriteProcessor] BMP not found: {fullBmpPath}");
                return;
            }

            // Step 1: Ensure BMP is readable so we can GetPixels
            var bmpImporter = AssetImporter.GetAtPath(bmpPath) as TextureImporter;
            if (bmpImporter != null)
            {
                bool needsReimport = false;
                if (!bmpImporter.isReadable)
                {
                    bmpImporter.isReadable = true;
                    needsReimport = true;
                }
                if (bmpImporter.textureCompression != TextureImporterCompression.Uncompressed)
                {
                    bmpImporter.textureCompression = TextureImporterCompression.Uncompressed;
                    needsReimport = true;
                }
                if (needsReimport)
                {
                    bmpImporter.SaveAndReimport();
                }
            }

            // Step 2: Load BMP as Texture2D
            var bmpTex = AssetDatabase.LoadAssetAtPath<Texture2D>(bmpPath);
            if (bmpTex == null)
            {
                Debug.LogError($"[BattleSpriteProcessor] Cannot load: {bmpPath}");
                return;
            }

            // Step 3: Create new RGBA texture, copy pixels, replace purple with transparent
            int w = bmpTex.width;
            int h = bmpTex.height;
            var pixels = bmpTex.GetPixels32();
            int replaced = 0;

            for (int i = 0; i < pixels.Length; i++)
            {
                var p = pixels[i];
                // Purple background: R≈247, G≈0, B≈255 (with some tolerance)
                if (p.r >= 230 && p.g <= 30 && p.b >= 240)
                {
                    pixels[i] = new Color32(0, 0, 0, 0);
                    replaced++;
                }
            }

            Debug.Log($"[BattleSpriteProcessor] {baseName}: {w}x{h}, replaced {replaced} purple pixels");

            var pngTex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            pngTex.SetPixels32(pixels);
            pngTex.Apply();

            // Step 4: Save as PNG
            byte[] pngData = pngTex.EncodeToPNG();
            string fullPngPath = Path.GetFullPath(pngPath);
            File.WriteAllBytes(fullPngPath, pngData);
            Object.DestroyImmediate(pngTex);

            Debug.Log($"[BattleSpriteProcessor] Saved: {pngPath}");

            // Step 5: Refresh so Unity sees the new PNG
            AssetDatabase.Refresh();

            // Step 6: Configure PNG import settings
            var pngImporter = AssetImporter.GetAtPath(pngPath) as TextureImporter;
            if (pngImporter == null)
            {
                Debug.LogError($"[BattleSpriteProcessor] Cannot get importer for: {pngPath}");
                return;
            }

            pngImporter.textureType = TextureImporterType.Sprite;
            pngImporter.spriteImportMode = SpriteImportMode.Multiple;
            pngImporter.spritePixelsPerUnit = 1f; // 1 pixel = 1 world unit (48px tile)
            pngImporter.filterMode = FilterMode.Point; // Pixel-perfect, no blur
            pngImporter.textureCompression = TextureImporterCompression.Uncompressed;
            pngImporter.alphaIsTransparency = true;
            pngImporter.isReadable = true;
            pngImporter.mipmapEnabled = false;

            // Step 7: Define sprite rects (top-to-bottom slicing)
            var spriteMetaData = new List<SpriteMetaData>();
            for (int i = 0; i < frameCount; i++)
            {
                // Frame 0 is at top of image. In Unity texture coords, y=0 is bottom.
                float y = h - (i + 1) * FrameH;
                spriteMetaData.Add(new SpriteMetaData
                {
                    name = $"{baseName}_{i}",
                    rect = new Rect(0, y, FrameW, FrameH),
                    alignment = (int)SpriteAlignment.Center,
                    pivot = new Vector2(0.5f, 0.5f)
                });
            }

            pngImporter.spritesheet = spriteMetaData.ToArray();
            pngImporter.SaveAndReimport();

            Debug.Log($"[BattleSpriteProcessor] Configured {pngPath}: {frameCount} sprites, PPU=1, Point filter");
        }
    }
}
