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

        // ── Step 1: Create TerrainType assets ──
        [MenuItem("CaoCao/Battle Tilemap/1. Create Terrain Types")]
        static void CreateTerrainTypes()
        {
            EnsureDir(TerrainDir);

            CreateTerrain("plain", "平原", 1, 0, 0, new Color(0.6f, 0.8f, 0.3f),
                infantry: 1, cavalry: 1, archer: 1, naval: -1);
            CreateTerrain("road", "道路", 1, 0, 0, new Color(0.7f, 0.6f, 0.4f),
                infantry: 1, cavalry: 1, archer: 1, naval: -1);
            CreateTerrain("forest", "森林", 2, 0, 20, new Color(0.2f, 0.5f, 0.1f),
                infantry: 2, cavalry: 3, archer: 2, naval: -1);
            CreateTerrain("mountain", "山地", -1, 0, 30, new Color(0.5f, 0.4f, 0.3f),
                infantry: 3, cavalry: -1, archer: 3, naval: -1);
            CreateTerrain("water", "水域", -1, 0, 0, new Color(0.2f, 0.4f, 0.9f),
                infantry: -1, cavalry: -1, archer: -1, naval: 1);
            CreateTerrain("bridge", "桥梁", 1, 0, 0, new Color(0.6f, 0.5f, 0.3f),
                infantry: 1, cavalry: 1, archer: 1, naval: 1);
            CreateTerrain("village", "村庄", 1, 0, 10, new Color(0.8f, 0.6f, 0.3f),
                infantry: 1, cavalry: 1, archer: 1, naval: -1);
            CreateTerrain("castle", "城墙", 2, 10, 30, new Color(0.5f, 0.5f, 0.5f),
                infantry: 2, cavalry: -1, archer: 2, naval: -1);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[SetupBattleTilemap] Created 8 TerrainType assets in " + TerrainDir);
        }

        // ── Step 2: Create TerrainTile assets with colored sprites ──
        [MenuItem("CaoCao/Battle Tilemap/2. Create Terrain Tiles")]
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

            string[] terrainNames = { "plain", "road", "forest", "mountain", "water", "bridge", "village", "castle" };

            foreach (var tname in terrainNames)
            {
                string terrainPath = $"{TerrainDir}/{tname}.asset";
                var terrainType = AssetDatabase.LoadAssetAtPath<TerrainType>(terrainPath);
                if (terrainType == null)
                {
                    Debug.LogWarning($"TerrainType not found: {terrainPath}. Run Step 1 first.");
                    continue;
                }

                // Create a solid color PNG sprite (size = tileSize)
                var sprite = CreateOrLoadColorSprite(tname, terrainType.minimapColor, ts);

                string tilePath = $"{TileDir}/{tname}_tile.asset";
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
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[SetupBattleTilemap] Created TerrainTile assets ({ts}x{ts} px) in {TileDir}");
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

        static Sprite CreateOrLoadColorSprite(string terrainName, Color color, int size)
        {
            string pngPath = $"{TileSpriteDir}/{terrainName}_color.png";

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = color;
            tex.SetPixels(pixels);
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
                importer.spritePixelsPerUnit = 1;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(pngPath);
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
