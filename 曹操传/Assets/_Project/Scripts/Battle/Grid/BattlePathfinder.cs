using System;
using System.Collections.Generic;
using UnityEngine;

namespace CaoCao.Battle
{
    public class BattlePathfinder
    {
        public Vector2Int MapSize;
        public Func<Vector2Int, int> GetCost;
        public Func<Vector2Int, bool> IsPassable;
        public Func<Vector2Int, bool> IsBlocked;

        public PathResult ComputeReachable(Vector2Int start, int movPoints)
        {
            var reachable = new Dictionary<Vector2Int, int>();
            var parent = new Dictionary<Vector2Int, Vector2Int?>();
            var frontier = new List<Vector2Int> { start };
            var costSoFar = new Dictionary<Vector2Int, int> { { start, 0 } };
            reachable[start] = movPoints;
            parent[start] = null;

            while (frontier.Count > 0)
            {
                var current = PopLowestCost(frontier, costSoFar);
                foreach (var n in Neighbors(current))
                {
                    if (!InBounds(n)) continue;
                    if (IsBlocked != null && IsBlocked(n)) continue;
                    if (IsPassable != null && !IsPassable(n)) continue;

                    int stepCost = GetCost != null ? GetCost(n) : 1;
                    if (stepCost <= 0) continue;
                    int newCost = costSoFar[current] + stepCost;
                    if (newCost > movPoints) continue;

                    if (!costSoFar.ContainsKey(n) || newCost < costSoFar[n])
                    {
                        costSoFar[n] = newCost;
                        reachable[n] = movPoints - newCost;
                        parent[n] = current;
                        if (!frontier.Contains(n))
                            frontier.Add(n);
                    }
                }
            }
            return new PathResult { Reachable = reachable, Parent = parent };
        }

        public List<Vector2Int> BuildPath(Vector2Int target, Dictionary<Vector2Int, Vector2Int?> parent)
        {
            var path = new List<Vector2Int>();
            Vector2Int? current = target;
            // Walk parent chain back to start; stop when parent is null (= start cell)
            while (current.HasValue && parent.ContainsKey(current.Value) && parent[current.Value].HasValue)
            {
                path.Add(current.Value);
                current = parent[current.Value];
            }
            path.Reverse();
            return path;
        }

        bool InBounds(Vector2Int cell)
        {
            return cell.x >= 0 && cell.y >= 0 && cell.x < MapSize.x && cell.y < MapSize.y;
        }

        static Vector2Int[] Neighbors(Vector2Int cell)
        {
            return new[]
            {
                cell + Vector2Int.up, cell + Vector2Int.down,
                cell + Vector2Int.left, cell + Vector2Int.right
            };
        }

        static Vector2Int PopLowestCost(List<Vector2Int> frontier, Dictionary<Vector2Int, int> costSoFar)
        {
            var best = frontier[0];
            int bestCost = costSoFar.GetValueOrDefault(best, 999999);
            for (int i = 1; i < frontier.Count; i++)
            {
                int c = costSoFar.GetValueOrDefault(frontier[i], 999999);
                if (c < bestCost)
                {
                    bestCost = c;
                    best = frontier[i];
                }
            }
            frontier.Remove(best);
            return best;
        }
    }

    public struct PathResult
    {
        public Dictionary<Vector2Int, int> Reachable;
        public Dictionary<Vector2Int, Vector2Int?> Parent;
    }
}
