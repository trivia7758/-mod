using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using CaoCao.Common;

namespace CaoCao.Battle
{
    public class BattleGridMap : MonoBehaviour
    {
        [SerializeField] Vector2Int mapSize = new(12, 8);
        [SerializeField] Vector2Int tileSize = new(48, 48);

        [Header("Tilemap")]
        [SerializeField] Tilemap tilemap;
        [SerializeField] TerrainType fallbackTerrain;

        public Vector2Int MapSize => mapSize;
        public Vector2Int TileSize => tileSize;
        public Vector2 Origin { get; set; }

        Dictionary<Vector2Int, TerrainType> _terrainCache = new();

        // Cache for TerrainType assets loaded from Resources (name→asset)
        Dictionary<string, TerrainType> _terrainTypeByName;

        // Hardcoded fallback costs when no Tilemap is assigned
        static readonly Dictionary<string, int> FallbackCosts = new()
        {
            { "road", 1 }, { "plain", 1 }, { "forest", 2 },
            { "water", -1 }, { "mountain", -1 }
        };
        Dictionary<Vector2Int, string> _fallbackTerrain = new();

        void Awake()
        {
            // Try multiple methods to find the Tilemap
            if (tilemap == null)
            {
                Debug.Log("[BattleGridMap] Serialized tilemap is null, searching scene...");

                // Method 1: FindAnyObjectByType (active objects only)
                tilemap = FindAnyObjectByType<Tilemap>();

                // Method 2: Search including inactive objects
                if (tilemap == null)
                {
                    var allTilemaps = FindObjectsByType<Tilemap>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                    if (allTilemaps.Length > 0)
                    {
                        tilemap = allTilemaps[0];
                        Debug.Log($"[BattleGridMap] Found inactive Tilemap: {tilemap.gameObject.name} (activeSelf={tilemap.gameObject.activeSelf})");
                    }
                }

                // Method 3: Search all Grids and check children
                if (tilemap == null)
                {
                    var grids = FindObjectsByType<Grid>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                    foreach (var grid in grids)
                    {
                        tilemap = grid.GetComponentInChildren<Tilemap>(true);
                        if (tilemap != null)
                        {
                            Debug.Log($"[BattleGridMap] Found Tilemap as child of Grid '{grid.name}'");
                            break;
                        }
                    }
                }

                if (tilemap == null)
                    Debug.LogWarning("[BattleGridMap] No Tilemap found in scene! Using demo map.");
                else
                    Debug.Log($"[BattleGridMap] Found Tilemap: '{tilemap.gameObject.name}' on '{tilemap.transform.root.name}'");
            }
            else
            {
                Debug.Log($"[BattleGridMap] Using serialized Tilemap: '{tilemap.gameObject.name}'");
            }

            if (tilemap != null)
            {
                // Read tileSize from Grid component so it's always in sync
                var grid = tilemap.layoutGrid;
                if (grid != null)
                {
                    tileSize = new Vector2Int(Mathf.RoundToInt(grid.cellSize.x), Mathf.RoundToInt(grid.cellSize.y));
                    Debug.Log($"[BattleGridMap] Grid cellSize={grid.cellSize}, tileSize={tileSize}");
                }
                BuildFromTilemap();
            }
            else
            {
                BuildDemoMap();
            }

            Debug.Log($"[BattleGridMap] Init complete: tilemap={(tilemap != null ? "YES" : "NO")}, mapSize={mapSize}, tileSize={tileSize}, Origin={Origin}, terrainCache={_terrainCache.Count}");
        }

        // ── Tilemap-based initialization ──

        void BuildFromTilemap()
        {
            _terrainCache.Clear();

            // Compress bounds to only include actually painted tiles
            tilemap.CompressBounds();
            var bounds = tilemap.cellBounds;

            if (bounds.size.x <= 0 || bounds.size.y <= 0)
            {
                Debug.LogWarning($"[BattleGridMap] Tilemap bounds are empty ({bounds.size}). No tiles painted? Falling back to demo.");
                BuildDemoMap();
                return;
            }

            // Derive map size from tilemap bounds
            mapSize = new Vector2Int(bounds.size.x, bounds.size.y);

            // Set Origin from tilemap's actual world position of the bottom-left cell
            Vector3 worldMin = tilemap.CellToWorld(new Vector3Int(bounds.xMin, bounds.yMin, 0));
            Origin = new Vector2(worldMin.x, worldMin.y);

            Debug.Log($"[BattleGridMap] Tilemap bounds: min=({bounds.xMin},{bounds.yMin}), max=({bounds.xMax},{bounds.yMax}), size={bounds.size}, worldMin={worldMin}");

            int terrainTileCount = 0;
            int matchedByName = 0;
            int fallbackUsed = 0;
            int emptyCount = 0;

            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                for (int x = bounds.xMin; x < bounds.xMax; x++)
                {
                    var cellPos = new Vector3Int(x, y, 0);
                    // Map from tilemap coordinates to game grid coordinates (0-based)
                    var gameCell = new Vector2Int(x - bounds.xMin, y - bounds.yMin);

                    // Try 1: Read as custom TerrainTile
                    var terrainTile = tilemap.GetTile<TerrainTile>(cellPos);
                    if (terrainTile != null && terrainTile.terrainType != null)
                    {
                        _terrainCache[gameCell] = terrainTile.terrainType;
                        terrainTileCount++;
                        continue;
                    }

                    // Try 2: Read as any TileBase, match by name to TerrainType in Resources
                    var anyTile = tilemap.GetTile(cellPos);
                    if (anyTile != null)
                    {
                        var matched = TryMatchTerrainType(anyTile);
                        if (matched != null)
                        {
                            _terrainCache[gameCell] = matched;
                            matchedByName++;
                            continue;
                        }

                        // Log first few unmatched tiles for debugging
                        if (matchedByName == 0 && terrainTileCount == 0 && fallbackUsed < 3)
                        {
                            Debug.Log($"[BattleGridMap] Tile at ({x},{y}): type={anyTile.GetType().Name}, name='{anyTile.name}' — not a TerrainTile and no name match");
                        }
                    }
                    else
                    {
                        emptyCount++;
                        // Empty cell — still use fallback if available
                    }

                    // Try 3: Use fallback terrain
                    if (fallbackTerrain != null)
                    {
                        _terrainCache[gameCell] = fallbackTerrain;
                        fallbackUsed++;
                    }
                }
            }

            Debug.Log($"[BattleGridMap] BuildFromTilemap: {terrainTileCount} TerrainTile, {matchedByName} matched by name, {fallbackUsed} fallback, {emptyCount} empty, total={_terrainCache.Count}");

            if (terrainTileCount == 0 && matchedByName == 0)
            {
                Debug.LogWarning("[BattleGridMap] ⚠ No TerrainTile found! Your tiles may be regular Unity Tile (not TerrainTile). " +
                    "Use the Tile Palette with TerrainTile assets (CaoCao menu > Battle Tilemap > 2. Create Terrain Tiles), " +
                    "or re-paint the tilemap using the generated TerrainTile assets.");
            }
        }

        /// <summary>
        /// Try to match a non-TerrainTile by its asset name to a TerrainType in Resources.
        /// Supports names like "plain", "forest", "plain_tile", "forest_Tile", etc.
        /// </summary>
        TerrainType TryMatchTerrainType(TileBase tile)
        {
            if (tile == null) return null;

            // Lazy-load all TerrainType assets from Resources
            if (_terrainTypeByName == null)
            {
                _terrainTypeByName = new Dictionary<string, TerrainType>();
                var allTerrains = Resources.LoadAll<TerrainType>("Data/Terrains");
                foreach (var tt in allTerrains)
                {
                    // Index by asset name (e.g., "plain", "forest")
                    _terrainTypeByName[tt.name.ToLower()] = tt;
                    // Also index by terrainId (e.g., "plain", "forest")
                    if (!string.IsNullOrEmpty(tt.terrainId))
                        _terrainTypeByName[tt.terrainId.ToLower()] = tt;
                    // Also index by terrainName (e.g., "平原", "森林")
                    if (!string.IsNullOrEmpty(tt.terrainName))
                        _terrainTypeByName[tt.terrainName.ToLower()] = tt;
                }
                Debug.Log($"[BattleGridMap] Loaded {allTerrains.Length} TerrainType assets for name-matching");
            }

            // Try exact name match
            string tileName = tile.name.ToLower().Trim();
            if (_terrainTypeByName.TryGetValue(tileName, out var match))
                return match;

            // Try stripping common suffixes: "plain_tile" → "plain", "forest Tile" → "forest"
            foreach (var suffix in new[] { "_tile", " tile", "_terrain", " terrain" })
            {
                if (tileName.EndsWith(suffix))
                {
                    string stripped = tileName.Substring(0, tileName.Length - suffix.Length);
                    if (_terrainTypeByName.TryGetValue(stripped, out var m))
                        return m;
                }
            }

            // Try matching by partial name (tile name contains terrain name)
            foreach (var kvp in _terrainTypeByName)
            {
                if (tileName.Contains(kvp.Key) || kvp.Key.Contains(tileName))
                    return kvp.Value;
            }

            return null;
        }

        // ── Demo fallback (no Tilemap assigned) ──

        void BuildDemoMap()
        {
            _fallbackTerrain.Clear();
            for (int y = 0; y < mapSize.y; y++)
            {
                for (int x = 0; x < mapSize.x; x++)
                {
                    // Default everything to plain
                    _fallbackTerrain[new Vector2Int(x, y)] = "plain";
                }
            }

            Debug.Log($"[BattleGridMap] BuildDemoMap: {_fallbackTerrain.Count} cells, mapSize={mapSize}");
        }

        public void Rebuild(Vector2Int newSize)
        {
            mapSize = newSize;
            if (tilemap != null)
                BuildFromTilemap();
            else
                BuildDemoMap();
        }

        // ── Queries ──

        public bool IsInBounds(Vector2Int cell)
        {
            return cell.x >= 0 && cell.y >= 0 && cell.x < mapSize.x && cell.y < mapSize.y;
        }

        /// <summary>
        /// Get the TerrainType ScriptableObject for a cell. Returns null if using demo fallback.
        /// </summary>
        public TerrainType GetTerrainType(Vector2Int cell)
        {
            return _terrainCache.GetValueOrDefault(cell, fallbackTerrain);
        }

        /// <summary>
        /// Get terrain name string (for display / backward compat).
        /// </summary>
        public string GetTerrain(Vector2Int cell)
        {
            if (_terrainCache.TryGetValue(cell, out var tt) && tt != null)
                return tt.terrainName;

            return _fallbackTerrain.GetValueOrDefault(cell, "plain");
        }

        /// <summary>
        /// Get movement cost for a specific unit type.
        /// </summary>
        public int GetCost(Vector2Int cell, MovementType moveType)
        {
            if (_terrainCache.TryGetValue(cell, out var tt) && tt != null)
                return tt.GetMovementCost(moveType);

            // Demo fallback — all unit types use the same cost
            string terrain = _fallbackTerrain.GetValueOrDefault(cell, "plain");
            return FallbackCosts.GetValueOrDefault(terrain, 1);
        }

        /// <summary>
        /// Get movement cost using default (Infantry). Backward compat.
        /// </summary>
        public int GetCost(Vector2Int cell)
        {
            return GetCost(cell, MovementType.Infantry);
        }

        /// <summary>
        /// Check passability for a specific unit type.
        /// </summary>
        public bool IsPassable(Vector2Int cell, MovementType moveType)
        {
            if (!IsInBounds(cell)) return false;
            return GetCost(cell, moveType) > 0;
        }

        /// <summary>
        /// Check passability using default (Infantry). Backward compat.
        /// </summary>
        public bool IsPassable(Vector2Int cell)
        {
            return IsPassable(cell, MovementType.Infantry);
        }

        // ── UnitClass-based API (preferred) ──

        /// <summary>Get movement cost for a specific UnitClass.</summary>
        public int GetCost(Vector2Int cell, UnitClass unitClass)
        {
            if (_terrainCache.TryGetValue(cell, out var tt) && tt != null)
                return tt.GetMoveCost(unitClass);

            // Demo fallback
            string terrain = _fallbackTerrain.GetValueOrDefault(cell, "plain");
            return FallbackCosts.GetValueOrDefault(terrain, 1);
        }

        /// <summary>Check passability for a specific UnitClass.</summary>
        public bool IsPassable(Vector2Int cell, UnitClass unitClass)
        {
            if (!IsInBounds(cell)) return false;
            return GetCost(cell, unitClass) > 0;
        }

        /// <summary>Get effect% for a unit class on a cell (100=normal).</summary>
        public int GetEffect(Vector2Int cell, UnitClass unitClass)
        {
            if (_terrainCache.TryGetValue(cell, out var tt) && tt != null)
                return tt.GetEffect(unitClass);
            return 100;
        }

        /// <summary>Get heal amount for a unit on a cell this turn.</summary>
        public int GetHeal(Vector2Int cell, int maxHp)
        {
            if (_terrainCache.TryGetValue(cell, out var tt) && tt != null)
                return tt.CalcHeal(maxHp);
            return 0;
        }

        // ── Coordinate conversion ──

        public Vector2 CellToWorld(Vector2Int cell)
        {
            return Origin + new Vector2(
                cell.x * tileSize.x + tileSize.x * 0.5f,
                cell.y * tileSize.y + tileSize.y * 0.5f
            );
        }

        public Vector2Int WorldToCell(Vector2 pos)
        {
            Vector2 p = pos - Origin;
            return new Vector2Int(
                Mathf.FloorToInt(p.x / tileSize.x),
                Mathf.FloorToInt(p.y / tileSize.y)
            );
        }

        /// <summary>
        /// World-space bounds of the entire grid (bottom-left to top-right).
        /// </summary>
        public Rect WorldBounds
        {
            get
            {
                return new Rect(
                    Origin.x, Origin.y,
                    mapSize.x * tileSize.x,
                    mapSize.y * tileSize.y
                );
            }
        }

        // ── Terrain info (for HUD) ──

        public TerrainInfo GetTerrainInfo(Vector2Int cell, UnitClass unitClass = UnitClass.Infantry)
        {
            if (_terrainCache.TryGetValue(cell, out var tt) && tt != null)
            {
                return new TerrainInfo
                {
                    Name = tt.terrainName,
                    EffectPercent = tt.GetEffect(unitClass),
                    MoveCost = tt.GetMoveCost(unitClass),
                    HealPercent = tt.HealPercent,
                    // Legacy
                    Hit = tt.hitBonus,
                    Avoid = tt.avoidBonus,
                    HealPerTurn = tt.healPerTurn
                };
            }

            return new TerrainInfo
            {
                Name = _fallbackTerrain.GetValueOrDefault(cell, "plain"),
                EffectPercent = 100,
                MoveCost = 1,
                HealPercent = 0,
                Hit = 0,
                Avoid = 0,
                HealPerTurn = 0
            };
        }
    }

    public struct TerrainInfo
    {
        public string Name;
        public int EffectPercent;   // 发挥效果 (120/110/100/90/80)
        public int MoveCost;        // 消耗移动力 (1/2/3, -1=不可通行)
        public int HealPercent;     // 每回合恢复HP% (0=无)
        // Legacy
        public int Hit;
        public int Avoid;
        public int HealPerTurn;
    }
}
