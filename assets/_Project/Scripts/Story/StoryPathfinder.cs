using System.Collections.Generic;
using UnityEngine;

namespace CaoCao.Story
{
    public class StoryPathfinder
    {
        Vector2Int _gridSize;
        Vector2Int _origin;
        HashSet<Vector2Int> _blocked = new();

        public void Setup(Vector2Int gridSize, Vector2Int origin)
        {
            _gridSize = gridSize;
            _origin = origin;
            _blocked.Clear();
        }

        public void ClearBlocked()
        {
            _blocked.Clear();
        }

        public void SetBlocked(Vector2Int tile, bool blocked)
        {
            if (blocked) _blocked.Add(tile);
            else _blocked.Remove(tile);
        }

        public List<Vector2Int> FindPath(Vector2Int from, Vector2Int to)
        {
            if (from == to) return new List<Vector2Int>();
            if (!InBounds(from) || !InBounds(to)) return new List<Vector2Int>();

            var open = new List<Vector2Int> { from };
            var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
            var gScore = new Dictionary<Vector2Int, int> { { from, 0 } };
            var fScore = new Dictionary<Vector2Int, int> { { from, Manhattan(from, to) } };

            while (open.Count > 0)
            {
                // Get node with lowest fScore
                var current = open[0];
                int bestF = fScore.GetValueOrDefault(current, int.MaxValue);
                for (int i = 1; i < open.Count; i++)
                {
                    int f = fScore.GetValueOrDefault(open[i], int.MaxValue);
                    if (f < bestF)
                    {
                        bestF = f;
                        current = open[i];
                    }
                }

                if (current == to)
                    return ReconstructPath(cameFrom, current, from);

                open.Remove(current);
                int currentG = gScore.GetValueOrDefault(current, int.MaxValue);

                foreach (var n in Neighbors(current))
                {
                    if (!InBounds(n) || _blocked.Contains(n)) continue;

                    int tentG = currentG + 1;
                    if (tentG < gScore.GetValueOrDefault(n, int.MaxValue))
                    {
                        cameFrom[n] = current;
                        gScore[n] = tentG;
                        fScore[n] = tentG + Manhattan(n, to);
                        if (!open.Contains(n))
                            open.Add(n);
                    }
                }
            }

            return new List<Vector2Int>(); // No path found
        }

        List<Vector2Int> ReconstructPath(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int current, Vector2Int start)
        {
            var path = new List<Vector2Int>();
            while (current != start)
            {
                path.Add(current);
                current = cameFrom[current];
            }
            path.Reverse();
            return path;
        }

        bool InBounds(Vector2Int cell)
        {
            return cell.x >= _origin.x && cell.y >= _origin.y
                && cell.x < _origin.x + _gridSize.x
                && cell.y < _origin.y + _gridSize.y;
        }

        static int Manhattan(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        }

        static Vector2Int[] Neighbors(Vector2Int cell)
        {
            return new[]
            {
                cell + Vector2Int.up,
                cell + Vector2Int.down,
                cell + Vector2Int.left,
                cell + Vector2Int.right
            };
        }
    }
}
