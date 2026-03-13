using System.Collections.Generic;
using UnityEngine;

namespace CaoCao.Battle
{
    public class EnemyAI
    {
        public BattleUnit ChooseAttackTarget(BattleUnit unit, List<BattleUnit> players)
        {
            var inRange = new List<BattleUnit>();
            foreach (var p in players)
            {
                if (Manhattan(unit.cell, p.cell) == 1)
                    inRange.Add(p);
            }
            if (inRange.Count == 0) return null;
            inRange.Sort((a, b) => a.hp.CompareTo(b.hp));
            return inRange[0];
        }

        public List<Vector2Int> ChooseMoveTowards(
            BattleUnit unit, List<BattleUnit> players, List<BattleUnit> enemies,
            BattleGridMap map, BattlePathfinder pathfinder)
        {
            if (players.Count == 0) return new List<Vector2Int>();

            var closest = players[0];
            int bestD = Manhattan(unit.cell, closest.cell);
            foreach (var p in players)
            {
                int d = Manhattan(unit.cell, p.cell);
                if (d < bestD) { bestD = d; closest = p; }
            }

            pathfinder.GetCost = c => map.GetCost(c);
            pathfinder.IsPassable = c => map.IsPassable(c);
            pathfinder.IsBlocked = c => IsOccupied(c, unit, players, enemies);
            pathfinder.MapSize = map.MapSize;

            var result = pathfinder.ComputeReachable(unit.cell, unit.mov);
            var reachable = result.Reachable;
            var parent = result.Parent;

            var bestCell = unit.cell;
            int bestDist = Manhattan(unit.cell, closest.cell);
            foreach (var kv in reachable)
            {
                int d = Manhattan(kv.Key, closest.cell);
                if (d < bestDist || (d == bestDist && kv.Value > reachable.GetValueOrDefault(bestCell, 0)))
                {
                    bestDist = d;
                    bestCell = kv.Key;
                }
            }

            return pathfinder.BuildPath(bestCell, parent);
        }

        static int Manhattan(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        }

        static bool IsOccupied(Vector2Int cell, BattleUnit self, List<BattleUnit> players, List<BattleUnit> enemies)
        {
            foreach (var p in players)
                if (p != self && p.cell == cell && p.hp > 0) return true;
            foreach (var e in enemies)
                if (e != self && e.cell == cell && e.hp > 0) return true;
            return false;
        }
    }
}
