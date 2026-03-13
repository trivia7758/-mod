using UnityEngine;

namespace CaoCao.Common
{
    public static class GridUtility
    {
        // Orthographic (for battle)
        public static Vector2 CellToWorld(Vector2Int cell, Vector2Int tileSize, Vector2 origin)
        {
            return origin + new Vector2(
                cell.x * tileSize.x + tileSize.x * 0.5f,
                cell.y * tileSize.y + tileSize.y * 0.5f
            );
        }

        public static Vector2Int WorldToCell(Vector2 pos, Vector2Int tileSize, Vector2 origin)
        {
            Vector2 local = pos - origin;
            return new Vector2Int(
                Mathf.FloorToInt(local.x / tileSize.x),
                Mathf.FloorToInt(local.y / tileSize.y)
            );
        }

        // Isometric (for story)
        public static Vector2 IsoTileToWorld(Vector2Int tile, Vector2Int tileSize, Vector2 origin, Vector2 footOffset)
        {
            float halfW = tileSize.x * 0.5f;
            float halfH = tileSize.y * 0.5f;
            float wx = (tile.x - tile.y) * halfW;
            float wy = (tile.x + tile.y) * halfH;
            return new Vector2(wx, wy) + footOffset + origin;
        }

        public static Vector2Int IsoWorldToTile(Vector2 pos, Vector2Int tileSize, Vector2 origin)
        {
            Vector2 p = pos - origin;
            float halfW = tileSize.x * 0.5f;
            float halfH = tileSize.y * 0.5f;
            float x = (p.x / halfW + p.y / halfH) * 0.5f;
            float y = (p.y / halfH - p.x / halfW) * 0.5f;
            return new Vector2Int(Mathf.RoundToInt(x), Mathf.RoundToInt(y));
        }

        public static int ManhattanDistance(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        }

        public static Vector2Int[] GetNeighbors(Vector2Int cell)
        {
            return new Vector2Int[]
            {
                cell + Vector2Int.up,
                cell + Vector2Int.down,
                cell + Vector2Int.left,
                cell + Vector2Int.right
            };
        }
    }
}
