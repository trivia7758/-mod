using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;
using CaoCao.Battle;
using CaoCao.Common;
using System.IO;

namespace CaoCao.Editor
{
    public static class SetupBattleTilemap
    {
        static readonly string TerrainDir = "Assets/_Project/Resources/Data/Terrains";
        static readonly string TileDir = "Assets/_Project/Resources/Data/TerrainTiles";
        static readonly string TileSpriteDir = "Assets/_Project/Resources/Sprites/TerrainTileSprites";

        // All 28 terrain definitions matching terrain_data.json
        // Format: (id, displayName, defaultCost, hitBonus, avoidBonus, minimapColor, infantry, cavalry, archer, naval)
        static readonly (string id, string name, int cost, int hit, int avoid, Color color,
            int inf, int cav, int arc, int nav)[] AllTerrains =
        {
            // ── 自然地形 ──
            ("plain",     "平原",   1,  0,  0, new Color(0.60f, 0.80f, 0.30f),  1,  1,  1, -1),
            ("grassland", "草原",   1,  0,  0, new Color(0.50f, 0.75f, 0.25f),  1,  1,  1, -1),
            ("forest",    "树林",   2,  0, 20, new Color(0.20f, 0.50f, 0.10f),  1,  2,  1, -1),
            ("wasteland", "荒地",   2,  0, 10, new Color(0.65f, 0.55f, 0.35f),  1,  2,  1, -1),
            ("mountain",  "山地",   3,  0, 30, new Color(0.50f, 0.40f, 0.30f),  2,  3,  2, -1),
            ("rock",      "岩山",  -1,  0,  0, new Color(0.40f, 0.35f, 0.30f), -1, -1, -1, -1),
            ("cliff",     "山崖",  -1,  0,  0, new Color(0.35f, 0.30f, 0.25f), -1, -1, -1, -1),
            ("snow",      "雪原",   2,  0, 10, new Color(0.90f, 0.92f, 0.95f),  1,  2,  1, -1),

            // ── 水域地形 ──
            ("bridge",    "桥梁",   1,  0,  0, new Color(0.60f, 0.50f, 0.30f),  1,  1,  1,  1),
            ("shallows",  "浅滩",   2,  0,  0, new Color(0.55f, 0.70f, 0.80f),  2,  2,  2,  1),
            ("swamp",     "沼泽",   3,  0,  0, new Color(0.35f, 0.45f, 0.30f),  2,  3,  2,  1),
            ("pond",      "池塘",  -1,  0,  0, new Color(0.30f, 0.55f, 0.80f), -1, -1, -1, -1),
            ("stream",    "小河",  -1,  0,  0, new Color(0.35f, 0.60f, 0.85f), -1, -1, -1, -1),
            ("river",     "大河",   2,  0,  0, new Color(0.20f, 0.40f, 0.90f),  1,  2,  1,  1),
            ("pool",      "水池",  -1,  0,  0, new Color(0.25f, 0.50f, 0.75f), -1, -1, -1, -1),

            // ── 建筑/人造地形 ──
            ("fence",     "栅栏",  -1,  0,  0, new Color(0.55f, 0.45f, 0.25f), -1, -1, -1, -1),
            ("wall",      "城墙",  -1, 10,  0, new Color(0.50f, 0.50f, 0.50f), -1, -1, -1, -1),
            ("city",      "城内",   1, 10, 10, new Color(0.70f, 0.65f, 0.55f),  1,  1,  1, -1),
            ("gate",      "城门",  -1,  0,  0, new Color(0.60f, 0.55f, 0.45f), -1, -1, -1, -1),
            ("castle",    "城池",   1, 20, 20, new Color(0.75f, 0.70f, 0.50f),  1,  1,  1, -1),
            ("pass",      "关隘",   1, 20, 20, new Color(0.65f, 0.55f, 0.40f),  1,  1,  1, -1),
            ("deerfence", "鹿砦",   2, 25, 25, new Color(0.55f, 0.50f, 0.30f),  1,  2,  1, -1),

            // ── 村落/设施 ──
            ("village",   "村庄",   2, 10, 10, new Color(0.80f, 0.60f, 0.30f),  1,  2,  1, -1),
            ("barracks",  "兵营",   2, 10, 10, new Color(0.70f, 0.45f, 0.25f),  1,  2,  1, -1),
            ("house",     "民居",   3,  0, 10, new Color(0.75f, 0.55f, 0.35f),  2,  3,  2, -1),
            ("treasury",  "宝物库", 2,  0,  0, new Color(0.85f, 0.75f, 0.30f),  1,  2,  1, -1),

            // ── 特殊地形 ──
            ("fire",      "火",    -1,  0,  0, new Color(0.95f, 0.30f, 0.10f), -1, -1, -1, -1),
            ("ship",      "船",    -1,  0,  0, new Color(0.50f, 0.40f, 0.20f), -1, -1, -1, -1),
        };

        // ── Step 1: Create ALL 28 TerrainType assets ──
        [MenuItem("CaoCao/Battle Tilemap/1. Create Terrain Types (All 28)")]
        static void CreateTerrainTypes()
        {
            EnsureDir(TerrainDir);

            int count = 0;
            foreach (var t in AllTerrains)
            {
                CreateTerrain(t.id, t.name, t.cost, t.hit, t.avoid, t.color,
                    t.inf, t.cav, t.arc, t.nav);
                count++;
            }

            // Remove legacy "road" asset if it exists (not in terrain_data.json)
            string roadPath = $"{TerrainDir}/road.asset";
            if (AssetDatabase.LoadAssetAtPath<TerrainType>(roadPath) != null)
            {
                AssetDatabase.DeleteAsset(roadPath);
                Debug.Log("[SetupBattleTilemap] Removed legacy 'road.asset' (not in terrain_data.json)");
            }
            // Remove legacy "water" asset if it exists
            string waterPath = $"{TerrainDir}/water.asset";
            if (AssetDatabase.LoadAssetAtPath<TerrainType>(waterPath) != null)
            {
                AssetDatabase.DeleteAsset(waterPath);
                Debug.Log("[SetupBattleTilemap] Removed legacy 'water.asset' (not in terrain_data.json)");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[SetupBattleTilemap] Created {count} TerrainType assets in {TerrainDir}");
        }

        // ── Step 2: Create TerrainTile assets + colored sprites for ALL terrains ──
        [MenuItem("CaoCao/Battle Tilemap/2. Create Terrain Tiles (All 28)")]
        static void CreateTerrainTiles()
        {
            // Ask user for tile size
            int ts = EditorPrefs.GetInt("CaoCao_TileSize", 48);
            ts = EditorUtility.DisplayDialogComplex(
                "Tile Size",
                "每个格子多大 (像素)?\n当前设置: " + ts,
                "48x48", "40x40", "自定义"
            ) switch
            {
                0 => 48,
                1 => 40,
                _ => ts
            };

            // If custom, let user type
            if (ts != 48 && ts != 40)
            {
                string input = EditorInputDialog.Show("自定义 Tile Size", "输入 tile 边长 (像素):", ts.ToString());
                if (!string.IsNullOrEmpty(input) && int.TryParse(input, out int custom))
                    ts = custom;
            }
            EditorPrefs.SetInt("CaoCao_TileSize", ts);

            EnsureDir(TileDir);
            EnsureDir(TileSpriteDir);

            int count = 0;
            foreach (var t in AllTerrains)
            {
                string terrainPath = $"{TerrainDir}/{t.id}.asset";
                var terrainType = AssetDatabase.LoadAssetAtPath<TerrainType>(terrainPath);
                if (terrainType == null)
                {
                    Debug.LogWarning($"TerrainType not found: {terrainPath}. Run Step 1 first.");
                    continue;
                }

                // Create a solid color PNG sprite (size = tileSize)
                var sprite = CreateOrLoadColorSprite(t.id, terrainType.minimapColor, ts);

                string tilePath = $"{TileDir}/{t.id}_tile.asset";
                var tile = AssetDatabase.LoadAssetAtPath<TerrainTile>(tilePath);
                if (tile == null)
                {
                    tile = ScriptableObject.CreateInstance<TerrainTile>();
                    AssetDatabase.CreateAsset(tile, tilePath);
                }

                tile.terrainType = terrainType;
                tile.tileColor = new Color(1, 1, 1, 0.45f); // semi-transparent overlay
                tile.tileSprite = sprite;
                EditorUtility.SetDirty(tile);
                count++;
            }

            // Clean up legacy tiles
            string[] legacyTiles = { "road_tile", "water_tile" };
            foreach (var lt in legacyTiles)
            {
                string ltPath = $"{TileDir}/{lt}.asset";
                if (AssetDatabase.LoadAssetAtPath<TerrainTile>(ltPath) != null)
                {
                    AssetDatabase.DeleteAsset(ltPath);
                    Debug.Log($"[SetupBattleTilemap] Removed legacy tile: {lt}");
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[SetupBattleTilemap] Created {count} TerrainTile assets ({ts}x{ts} px) in {TileDir}");
        }

        // ── Step 3: Setup scene with background image + Grid + Tilemap ──
        [MenuItem("CaoCao/Battle Tilemap/3. Setup Scene (Background + Grid + Tilemap)")]
        static void SetupScene()
        {
            // Find background image
            string bgSpritePath = FindBackgroundImage();
            if (bgSpritePath == null)
            {
                Debug.LogError("[SetupBattleTilemap] No background image found. Place a map image in Resources/Sprites/Battle/");
                return;
            }

            int ts = EditorPrefs.GetInt("CaoCao_TileSize", 48);

            // Configure sprite import
            ConfigureBackgroundSprite(bgSpritePath);
            AssetDatabase.Refresh();

            var bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>(bgSpritePath);
            if (bgSprite == null)
            {
                Debug.LogError("[SetupBattleTilemap] Could not load background as Sprite.");
                return;
            }

            int imgW = Mathf.RoundToInt(bgSprite.rect.width);
            int imgH = Mathf.RoundToInt(bgSprite.rect.height);
            int mapW = imgW / ts;
            int mapH = imgH / ts;

            if (imgW % ts != 0 || imgH % ts != 0)
            {
                Debug.LogWarning($"[SetupBattleTilemap] Image {imgW}x{imgH} not perfectly divisible by {ts}. Grid: {mapW}x{mapH} (trimmed).");
            }

            Debug.Log($"[SetupBattleTilemap] Map: {imgW}x{imgH} px, tile: {ts}x{ts}, grid: {mapW}x{mapH}");

            // --- Background sprite ---
            var bgGo = GameObject.Find("MapBackground");
            if (bgGo == null) bgGo = new GameObject("MapBackground");
            var bgSr = bgGo.GetComponent<SpriteRenderer>();
            if (bgSr == null) bgSr = bgGo.AddComponent<SpriteRenderer>();
            bgSr.sprite = bgSprite;
            bgSr.sortingOrder = -100;
            bgGo.transform.position = Vector3.zero;

            // --- Grid + Tilemap ---
            var existingGrid = Object.FindAnyObjectByType<Grid>();
            GameObject gridGo;
            if (existingGrid != null)
            {
                gridGo = existingGrid.gameObject;
                existingGrid.cellSize = new Vector3(ts, ts, 0);
            }
            else
            {
                gridGo = new GameObject("BattleGrid");
                var grid = gridGo.AddComponent<Grid>();
                grid.cellSize = new Vector3(ts, ts, 0);
                grid.cellLayout = GridLayout.CellLayout.Rectangle;
            }

            // Tilemap child
            var tilemapTransform = gridGo.transform.Find("TerrainTilemap");
            Tilemap tilemap;
            if (tilemapTransform != null)
            {
                tilemap = tilemapTransform.GetComponent<Tilemap>();
            }
            else
            {
                var tilemapGo = new GameObject("TerrainTilemap");
                tilemapGo.transform.SetParent(gridGo.transform);
                tilemap = tilemapGo.AddComponent<Tilemap>();
                var renderer = tilemapGo.AddComponent<TilemapRenderer>();
                renderer.sortingOrder = -9;
            }

            // Pre-fill with plain terrain
            string plainTilePath = $"{TileDir}/plain_tile.asset";
            var plainTile = AssetDatabase.LoadAssetAtPath<TerrainTile>(plainTilePath);
            if (plainTile != null)
            {
                for (int y = 0; y < mapH; y++)
                    for (int x = 0; x < mapW; x++)
                        tilemap.SetTile(new Vector3Int(x, y, 0), plainTile);
                Debug.Log($"[SetupBattleTilemap] Pre-filled {mapW}x{mapH} with plain tiles.");
            }

            // Wire BattleGridMap
            var battleGridMap = Object.FindAnyObjectByType<BattleGridMap>();
            if (battleGridMap != null)
            {
                var so = new SerializedObject(battleGridMap);
                var tilemapProp = so.FindProperty("tilemap");
                if (tilemapProp != null)
                    tilemapProp.objectReferenceValue = tilemap;
                var sizeProp = so.FindProperty("mapSize");
                if (sizeProp != null)
                    sizeProp.vector2IntValue = new Vector2Int(mapW, mapH);
                var tileSizeProp = so.FindProperty("tileSize");
                if (tileSizeProp != null)
                    tileSizeProp.vector2IntValue = new Vector2Int(ts, ts);
                so.ApplyModifiedProperties();
                Debug.Log("[SetupBattleTilemap] Wired Tilemap to BattleGridMap.");
            }

            Selection.activeGameObject = tilemap.gameObject;
            Debug.Log("[SetupBattleTilemap] Done! Now open Window > 2D > Tile Palette to paint terrain.");
        }

        // ── Fix existing TerrainType assets: set terrainId from asset file name ──
        [MenuItem("CaoCao/Battle Tilemap/Fix TerrainType IDs")]
        static void FixTerrainTypeIds()
        {
            var allTerrains = Resources.LoadAll<TerrainType>("Data/Terrains");
            int fixed_ = 0;
            foreach (var tt in allTerrains)
            {
                // Asset name = file name without extension (e.g. "plain", "forest")
                if (tt.terrainId != tt.name)
                {
                    tt.terrainId = tt.name;
                    EditorUtility.SetDirty(tt);
                    fixed_++;
                    Debug.Log($"[FixTerrainTypeIDs] {tt.name}: terrainId = \"{tt.terrainId}\", terrainName = \"{tt.terrainName}\"");
                }
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[FixTerrainTypeIDs] Fixed {fixed_}/{allTerrains.Length} TerrainType assets.");
        }

        // ── Toggle overlay visibility ──
        [MenuItem("CaoCao/Battle Tilemap/Toggle Terrain Overlay")]
        static void ToggleTerrainOverlay()
        {
            var tilemap = Object.FindAnyObjectByType<Tilemap>();
            if (tilemap == null) return;
            var renderer = tilemap.GetComponent<TilemapRenderer>();
            if (renderer != null)
            {
                renderer.enabled = !renderer.enabled;
                Debug.Log($"Terrain overlay: {(renderer.enabled ? "VISIBLE" : "HIDDEN")}");
            }
        }

        // ── Helpers ──

        static string FindBackgroundImage()
        {
            string[] extensions = { "jpg", "png", "bmp" };
            foreach (var ext in extensions)
            {
                var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/_Project/Resources/Sprites/Battle" });
                foreach (var guid in guids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    if (path.Contains("level") || path.Contains("map") || path.Contains("bg"))
                        return path;
                }
            }
            // Fallback: first image in the folder
            var allGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/_Project/Resources/Sprites/Battle" });
            if (allGuids.Length > 0)
                return AssetDatabase.GUIDToAssetPath(allGuids[0]);
            return null;
        }

        static void ConfigureBackgroundSprite(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;

            bool changed = false;
            if (importer.textureType != TextureImporterType.Sprite)
            { importer.textureType = TextureImporterType.Sprite; changed = true; }
            if (importer.spriteImportMode != SpriteImportMode.Single)
            { importer.spriteImportMode = SpriteImportMode.Single; changed = true; }
            if (importer.spritePixelsPerUnit != 1)
            { importer.spritePixelsPerUnit = 1; changed = true; }
            if (importer.filterMode != FilterMode.Point)
            { importer.filterMode = FilterMode.Point; changed = true; }
            if (importer.maxTextureSize < 4096)
            { importer.maxTextureSize = 4096; changed = true; }

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            if (settings.spriteAlignment != (int)SpriteAlignment.BottomLeft)
            {
                settings.spriteAlignment = (int)SpriteAlignment.BottomLeft;
                settings.spritePivot = new Vector2(0, 0);
                importer.SetTextureSettings(settings);
                changed = true;
            }

            if (changed) importer.SaveAndReimport();
        }

        /// <summary>
        /// Create a colored PNG sprite with the Chinese terrain name rendered on it.
        /// Uses a larger texture (128x128) so text is legible in the Tile Palette,
        /// but the PPU is set so it still maps to one tile.
        /// </summary>
        static Sprite CreateOrLoadColorSprite(string terrainName, Color color, int tileSize)
        {
            string pngPath = $"{TileSpriteDir}/{terrainName}_color.png";

            // Use 128x128 for better text clarity, scale down via PPU
            int texSize = 128;
            var tex = new Texture2D(texSize, texSize, TextureFormat.RGBA32, false);

            // Fill background
            var pixels = new Color[texSize * texSize];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = color;
            tex.SetPixels(pixels);

            // Draw a subtle 2px border (darker)
            Color borderColor = color * 0.5f;
            borderColor.a = 1f;
            for (int i = 0; i < texSize; i++)
            {
                tex.SetPixel(i, 0, borderColor);
                tex.SetPixel(i, 1, borderColor);
                tex.SetPixel(i, texSize - 1, borderColor);
                tex.SetPixel(i, texSize - 2, borderColor);
                tex.SetPixel(0, i, borderColor);
                tex.SetPixel(1, i, borderColor);
                tex.SetPixel(texSize - 1, i, borderColor);
                tex.SetPixel(texSize - 2, i, borderColor);
            }

            // Find the Chinese display name
            string label = terrainName;
            foreach (var t in AllTerrains)
            {
                if (t.id == terrainName) { label = t.name; break; }
            }

            // Render text using a simple bitmap font approach
            DrawTextOnTexture(tex, label, texSize);

            tex.Apply();

            var pngBytes = tex.EncodeToPNG();
            Object.DestroyImmediate(tex);

            string fullPath = Path.Combine(Application.dataPath, "..", pngPath).Replace("\\", "/");
            File.WriteAllBytes(fullPath, pngBytes);
            AssetDatabase.ImportAsset(pngPath, ImportAssetOptions.ForceUpdate);

            var importer = AssetImporter.GetAtPath(pngPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                // PPU = texSize / tileSize so it maps to exactly one tile
                importer.spritePixelsPerUnit = (float)texSize / tileSize;
                importer.filterMode = FilterMode.Bilinear;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(pngPath);
        }

        /// <summary>
        /// Draw a distinctive pattern on the terrain tile texture to help identify it.
        /// Uses diagonal stripes, dots, or cross-hatch patterns depending on terrain category.
        /// Also draws a short label using a hardcoded 5x7 pixel font (ASCII only).
        /// </summary>
        static void DrawTextOnTexture(Texture2D tex, string text, int texSize)
        {
            // Find the terrain entry to get the id for the label
            string id = text; // text IS the Chinese name, find the id
            foreach (var t in AllTerrains)
            {
                if (t.name == text) { id = t.id; break; }
            }

            // Truncate id to fit (max ~8 chars for 128px)
            string label = id.Length > 8 ? id.Substring(0, 8) : id;

            // Draw the label with the built-in tiny pixel font
            DrawPixelText(tex, label, texSize);
        }

        // ── Tiny 5x7 pixel font for ASCII (A-Z, a-z, 0-9, common symbols) ──
        // Each character is encoded as 5 columns of 7 bits packed into a uint.
        static readonly System.Collections.Generic.Dictionary<char, uint[]> PixelFont = BuildPixelFont();

        static System.Collections.Generic.Dictionary<char, uint[]> BuildPixelFont()
        {
            var f = new System.Collections.Generic.Dictionary<char, uint[]>();
            // Format: each entry is uint[5] (5 columns), each uint has 7 bits (rows, bit0=top)
            f['a'] = new uint[] { 0x20,0x54,0x54,0x54,0x78 };
            f['b'] = new uint[] { 0x7f,0x48,0x44,0x44,0x38 };
            f['c'] = new uint[] { 0x38,0x44,0x44,0x44,0x20 };
            f['d'] = new uint[] { 0x38,0x44,0x44,0x48,0x7f };
            f['e'] = new uint[] { 0x38,0x54,0x54,0x54,0x18 };
            f['f'] = new uint[] { 0x08,0x7e,0x09,0x01,0x02 };
            f['g'] = new uint[] { 0x0c,0x52,0x52,0x52,0x3e };
            f['h'] = new uint[] { 0x7f,0x08,0x04,0x04,0x78 };
            f['i'] = new uint[] { 0x00,0x44,0x7d,0x40,0x00 };
            f['j'] = new uint[] { 0x20,0x40,0x44,0x3d,0x00 };
            f['k'] = new uint[] { 0x7f,0x10,0x28,0x44,0x00 };
            f['l'] = new uint[] { 0x00,0x41,0x7f,0x40,0x00 };
            f['m'] = new uint[] { 0x7c,0x04,0x18,0x04,0x78 };
            f['n'] = new uint[] { 0x7c,0x08,0x04,0x04,0x78 };
            f['o'] = new uint[] { 0x38,0x44,0x44,0x44,0x38 };
            f['p'] = new uint[] { 0x7c,0x14,0x14,0x14,0x08 };
            f['q'] = new uint[] { 0x08,0x14,0x14,0x18,0x7c };
            f['r'] = new uint[] { 0x7c,0x08,0x04,0x04,0x08 };
            f['s'] = new uint[] { 0x48,0x54,0x54,0x54,0x20 };
            f['t'] = new uint[] { 0x04,0x3f,0x44,0x40,0x20 };
            f['u'] = new uint[] { 0x3c,0x40,0x40,0x20,0x7c };
            f['v'] = new uint[] { 0x1c,0x20,0x40,0x20,0x1c };
            f['w'] = new uint[] { 0x3c,0x40,0x30,0x40,0x3c };
            f['x'] = new uint[] { 0x44,0x28,0x10,0x28,0x44 };
            f['y'] = new uint[] { 0x0c,0x50,0x50,0x50,0x3c };
            f['z'] = new uint[] { 0x44,0x64,0x54,0x4c,0x44 };
            return f;
        }

        static void DrawPixelText(Texture2D tex, string text, int texSize)
        {
            int scale = 2; // each font pixel = 2x2 texture pixels
            int charW = 5 * scale + scale; // 5 cols + 1 spacing, scaled
            int charH = 7 * scale;
            int totalW = text.Length * charW;
            int startX = (texSize - totalW) / 2;
            int startY = (texSize - charH) / 2;

            // Determine text color: use white with dark shadow, or dark with light shadow
            // depending on background brightness
            Color bgSample = tex.GetPixel(texSize / 2, texSize / 2);
            float brightness = bgSample.r * 0.299f + bgSample.g * 0.587f + bgSample.b * 0.114f;
            Color textColor = brightness > 0.5f ? Color.black : Color.white;
            Color shadowColor = brightness > 0.5f ? new Color(1, 1, 1, 0.4f) : new Color(0, 0, 0, 0.6f);

            for (int ci = 0; ci < text.Length; ci++)
            {
                char ch = text[ci];
                if (!PixelFont.ContainsKey(ch)) continue;
                var glyph = PixelFont[ch];

                int cx = startX + ci * charW;

                for (int col = 0; col < 5; col++)
                {
                    uint colBits = glyph[col];
                    for (int row = 0; row < 7; row++)
                    {
                        if ((colBits & (1u << row)) != 0)
                        {
                            // Draw scaled pixel (shadow + main)
                            for (int sy = 0; sy < scale; sy++)
                            {
                                for (int sx = 0; sx < scale; sx++)
                                {
                                    // Shadow (offset +1, +1)
                                    int shX = cx + col * scale + sx + 1;
                                    int shY = texSize - 1 - (startY + row * scale + sy + 1);
                                    if (shX >= 0 && shX < texSize && shY >= 0 && shY < texSize)
                                    {
                                        Color bg = tex.GetPixel(shX, shY);
                                        float a = shadowColor.a;
                                        tex.SetPixel(shX, shY, new Color(
                                            bg.r * (1 - a) + shadowColor.r * a,
                                            bg.g * (1 - a) + shadowColor.g * a,
                                            bg.b * (1 - a) + shadowColor.b * a, 1f));
                                    }

                                    // Main text
                                    int px = cx + col * scale + sx;
                                    int py = texSize - 1 - (startY + row * scale + sy);
                                    if (px >= 0 && px < texSize && py >= 0 && py < texSize)
                                        tex.SetPixel(px, py, textColor);
                                }
                            }
                        }
                    }
                }
            }
        }

        static void CreateTerrain(string id, string displayName, int defaultCost,
            int hit, int avoid, Color color,
            int infantry, int cavalry, int archer, int naval)
        {
            string path = $"{TerrainDir}/{id}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<TerrainType>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<TerrainType>();
                AssetDatabase.CreateAsset(asset, path);
            }

            asset.terrainId = id;
            asset.terrainName = displayName;
            asset.defaultMovementCost = defaultCost;
            asset.hitBonus = hit;
            asset.avoidBonus = avoid;
            asset.minimapColor = color;
            asset.movementCosts = new MovementCostEntry[]
            {
                new() { movementType = MovementType.Infantry, cost = infantry },
                new() { movementType = MovementType.Cavalry,  cost = cavalry },
                new() { movementType = MovementType.Archer,   cost = archer },
                new() { movementType = MovementType.Naval,    cost = naval }
            };

            EditorUtility.SetDirty(asset);
        }

        static void EnsureDir(string dir)
        {
            if (!AssetDatabase.IsValidFolder(dir))
            {
                var parts = dir.Split('/');
                string current = parts[0];
                for (int i = 1; i < parts.Length; i++)
                {
                    string next = current + "/" + parts[i];
                    if (!AssetDatabase.IsValidFolder(next))
                        AssetDatabase.CreateFolder(current, parts[i]);
                    current = next;
                }
            }
        }
    }

    /// <summary>
    /// Simple editor input dialog for entering a single string value.
    /// </summary>
    public class EditorInputDialog : EditorWindow
    {
        string _value;
        string _label;
        bool _confirmed;
        static string _result;

        public static string Show(string title, string label, string defaultValue)
        {
            _result = defaultValue;
            var wnd = CreateInstance<EditorInputDialog>();
            wnd.titleContent = new GUIContent(title);
            wnd._label = label;
            wnd._value = defaultValue;
            wnd._confirmed = false;
            wnd.minSize = new Vector2(300, 100);
            wnd.maxSize = new Vector2(300, 100);
            wnd.ShowModal();
            return _result;
        }

        void OnGUI()
        {
            GUILayout.Space(10);
            GUILayout.Label(_label);
            _value = GUILayout.TextField(_value);
            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("OK"))
            {
                _result = _value;
                _confirmed = true;
                Close();
            }
            if (GUILayout.Button("Cancel"))
            {
                Close();
            }
            GUILayout.EndHorizontal();
        }
    }
}
