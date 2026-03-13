using UnityEngine;
using UnityEditor;
using System.IO;

namespace CaoCao.Editor
{
    /// <summary>
    /// Replaces magenta (#FF00FF) background and edge fringe in sprite PNGs
    /// with actual alpha transparency.
    /// </summary>
    public static class SpriteAlphaFixer
    {
        [MenuItem("CaoCao/Fix Sprite Magenta Background")]
        public static void RunFromMenu()
        {
            string[] paths = new[]
            {
                "Assets/_Project/Resources/Sprites/Story/test_r1.png",
                "Assets/_Project/Resources/Sprites/Story/test_r2.png"
            };

            int totalFixed = 0;

            foreach (string assetPath in paths)
            {
                string fullPath = Path.GetFullPath(assetPath);
                if (!File.Exists(fullPath))
                {
                    Debug.LogWarning($"[SpriteAlphaFixer] File not found: {fullPath}");
                    continue;
                }

                byte[] rawBytes = File.ReadAllBytes(fullPath);
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                tex.LoadImage(rawBytes);

                var pixels = tex.GetPixels32();
                int w = tex.width;
                int h = tex.height;
                int changed = 0;

                // Pass 1: Clear pure magenta pixels (already transparent from first run,
                // but also catch any remaining ones)
                for (int i = 0; i < pixels.Length; i++)
                {
                    var p = pixels[i];
                    if (p.a == 0) continue; // already transparent
                    // Magenta-ish: R high, G low, B high
                    if (p.r >= 180 && p.g < 80 && p.b >= 180)
                    {
                        pixels[i] = new Color32(0, 0, 0, 0);
                        changed++;
                    }
                }

                // Pass 2: Clean up fringe — any pixel that is semi-transparent
                // and has a strong magenta tint, reduce the magenta contribution
                for (int i = 0; i < pixels.Length; i++)
                {
                    var p = pixels[i];
                    if (p.a == 0 || p.a == 255) continue; // skip fully transparent or opaque
                    // Check if pixel has magenta tint (R and B much higher than G)
                    int magentaness = (p.r + p.b) / 2 - p.g;
                    if (magentaness > 80)
                    {
                        // Reduce magenta influence: bring R and B closer to G
                        float factor = Mathf.Clamp01(1f - magentaness / 255f);
                        pixels[i] = new Color32(
                            (byte)Mathf.Lerp(p.g, p.r, factor),
                            p.g,
                            (byte)Mathf.Lerp(p.g, p.b, factor),
                            p.a
                        );
                        changed++;
                    }
                }

                // Pass 3: Remove isolated semi-transparent pixels that are adjacent
                // to transparent areas (fringe cleanup)
                var cleaned = new Color32[pixels.Length];
                System.Array.Copy(pixels, cleaned, pixels.Length);
                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        int idx = y * w + x;
                        var p = pixels[idx];
                        if (p.a == 0 || p.a > 200) continue;

                        // Count transparent neighbors
                        int transNeighbors = 0;
                        int totalNeighbors = 0;
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            for (int dx = -1; dx <= 1; dx++)
                            {
                                if (dx == 0 && dy == 0) continue;
                                int nx = x + dx, ny = y + dy;
                                if (nx < 0 || nx >= w || ny < 0 || ny >= h) continue;
                                totalNeighbors++;
                                if (pixels[ny * w + nx].a == 0)
                                    transNeighbors++;
                            }
                        }

                        // If mostly surrounded by transparent pixels, make this transparent too
                        if (totalNeighbors > 0 && transNeighbors >= totalNeighbors / 2)
                        {
                            cleaned[idx] = new Color32(0, 0, 0, 0);
                            changed++;
                        }
                    }
                }

                if (changed > 0)
                {
                    tex.SetPixels32(cleaned);
                    tex.Apply();
                    byte[] pngData = tex.EncodeToPNG();
                    File.WriteAllBytes(fullPath, pngData);
                    Debug.Log($"[SpriteAlphaFixer] Fixed {changed} pixels in {assetPath}");
                    totalFixed += changed;
                }
                else
                {
                    Debug.Log($"[SpriteAlphaFixer] {assetPath} is clean");
                }

                Object.DestroyImmediate(tex);
            }

            if (totalFixed > 0)
                AssetDatabase.Refresh();
            Debug.Log($"[SpriteAlphaFixer] Done! {totalFixed} pixels fixed.");
        }
    }
}
