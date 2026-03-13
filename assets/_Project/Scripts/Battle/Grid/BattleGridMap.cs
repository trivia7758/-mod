using System.Collections.Generic;
using UnityEngine;

namespace CaoCao.Battle
{
    public class BattleGridMap : MonoBehaviour
    {
        [SerializeField] Vector2Int mapSize = new(12, 8);
        [SerializeField] Vector2Int tileSize = new(48, 48);

        public Vector2Int MapSize => mapSize;
        public Vector2Int TileSize => tileSize;
        public Vector2 Origin { get; set; }

        static readonly Dictionary<string, int> TerrainCosts = new()
        {
            { "road", 1 }, { "plain", 1 }, { "forest", 2 },
            { "water", -1 }, { "mountain", -1 }
        };

        Dictionary<Vector2Int, string> _terrain = new();

        void Awake()
        {
            BuildDemoMap();
        }

        public void Rebuild(Vector2Int newSize)
        {
            mapSize = newSize;
            BuildDemoMap();
        }

        void BuildDemoMap()
        {
            _terrain.Clear();
            for (int y = 0; y < mapSize.y; y++)
            {
                for (int x = 0; x < mapSize.x; x++)
                {
                    string t = "plain";
                    if (y == 3) t = "road";
                    if ((x == 2 || x == 3 || x == 4) && (y == 1 || y == 2)) t = "forest";
                    if ((x == 7 || x == 8) && (y == 5 || y == 6)) t = "water";
                    if (x == 10 && (y == 2 || y == 3 || y == 4)) t = "mountain";
                    _terrain[new Vector2Int(x, y)] = t;
                }
            }
        }

        public bool IsInBounds(Vector2Int cell)
        {
            return cell.x >= 0 && cell.y >= 0 && cell.x < mapSize.x && cell.y < mapSize.y;
        }

        public string GetTerrain(Vector2Int cell)
        {
            return _terrain.GetValueOrDefault(cell, "plain");
        }

        public bool IsPassable(Vector2Int cell)
        {
            if (!IsInBounds(cell)) return false;
            return GetCost(cell) > 0;
        }

        public int GetCost(Vector2Int cell)
        {
            string terrain = GetTerrain(cell);
            return TerrainCosts.GetValueOrDefault(terrain, 1);
        }

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

        public TerrainInfo GetTerrainInfo(Vector2Int cell)
        {
            return new TerrainInfo
            {
                Name = GetTerrain(cell),
                Hit = 0,
                Avoid = 0
            };
        }
    }

    public struct TerrainInfo
    {
        public string Name;
        public int Hit;
        public int Avoid;
    }
}
