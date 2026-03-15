using UnityEngine;
using CaoCao.Common;

namespace CaoCao.Battle
{
    [System.Serializable]
    public struct MovementCostEntry
    {
        public MovementType movementType;
        public int cost; // -1 = impassable
    }

    [CreateAssetMenu(fileName = "NewTerrain", menuName = "CaoCao/TerrainType")]
    public class TerrainType : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("English id matching terrain_data.json (e.g. plain, forest, mountain)")]
        public string terrainId = "plain";

        [Tooltip("Chinese display name (e.g. 平原, 树林). Auto-filled from DB if empty.")]
        public string terrainName = "平原";

        [Header("Legacy Movement Costs (fallback when DB unavailable)")]
        [Tooltip("Default cost if MovementType not found. -1 = impassable.")]
        public int defaultMovementCost = 1;

        [Tooltip("Per-MovementType cost overrides (legacy).")]
        public MovementCostEntry[] movementCosts;

        [Header("Combat Bonuses (legacy)")]
        public int hitBonus;
        public int avoidBonus;

        [Header("Healing (legacy, prefer DB healPercent)")]
        [Tooltip("HP recovered per turn. 0 = no healing.")]
        public int healPerTurn;

        [Header("Visual")]
        public Color minimapColor = Color.green;

        // ── Cached DB reference ──
        TerrainData _dbData;
        bool _dbLookedUp;

        /// <summary>
        /// Get the TerrainData from the database for full UnitClass lookups.
        /// Returns null if not found.
        /// </summary>
        public TerrainData DbData
        {
            get
            {
                if (!_dbLookedUp)
                {
                    _dbLookedUp = true;
                    _dbData = TerrainDatabase.Instance.Get(terrainId)
                           ?? TerrainDatabase.Instance.Get(terrainName);
                }
                return _dbData;
            }
        }

        // ─────────────────────────────────────────────
        //  New API — per UnitClass (preferred)
        // ─────────────────────────────────────────────

        /// <summary>Get effect% for a unit class (100 = normal). -1 = impassable.</summary>
        public int GetEffect(UnitClass uc)
        {
            var db = DbData;
            return db != null ? db.GetEffect(uc) : 100;
        }

        /// <summary>Get movement cost for a unit class. -1 = impassable.</summary>
        public int GetMoveCost(UnitClass uc)
        {
            var db = DbData;
            return db != null ? db.GetMoveCost(uc) : defaultMovementCost;
        }

        /// <summary>Check if passable for a specific unit class.</summary>
        public bool IsPassableFor(UnitClass uc) => GetMoveCost(uc) > 0;

        /// <summary>Get heal amount for a unit standing on this terrain.</summary>
        public int CalcHeal(int maxHp)
        {
            var db = DbData;
            if (db != null && db.healPercent > 0)
                return Mathf.FloorToInt(maxHp * db.healPercent / 100f);
            return healPerTurn; // legacy fallback
        }

        /// <summary>Get heal percent from DB (0 if none).</summary>
        public int HealPercent
        {
            get
            {
                var db = DbData;
                return db != null ? db.healPercent : 0;
            }
        }

        // ─────────────────────────────────────────────
        //  Legacy API — per MovementType (backward compat)
        // ─────────────────────────────────────────────

        /// <summary>Get movement cost for a legacy MovementType.</summary>
        public int GetMovementCost(MovementType moveType)
        {
            // Try DB first via a rough mapping
            var db = DbData;
            if (db != null)
            {
                UnitClass uc = MovementTypeToUnitClass(moveType);
                return db.GetMoveCost(uc);
            }

            // Legacy fallback
            if (movementCosts != null)
            {
                foreach (var entry in movementCosts)
                    if (entry.movementType == moveType)
                        return entry.cost;
            }
            return defaultMovementCost;
        }

        /// <summary>Check passable for legacy MovementType.</summary>
        public bool IsPassableFor(MovementType moveType) => GetMovementCost(moveType) > 0;

        /// <summary>Check passable using default cost (backward compat).</summary>
        public bool IsPassable => defaultMovementCost > 0;

        /// <summary>Rough mapping from legacy MovementType to UnitClass for DB lookups.</summary>
        static UnitClass MovementTypeToUnitClass(MovementType mt)
        {
            switch (mt)
            {
                case MovementType.Cavalry: return UnitClass.Cavalry;
                case MovementType.Archer:  return UnitClass.Archer;
                case MovementType.Naval:   return UnitClass.Admiral;
                default:                   return UnitClass.Infantry;
            }
        }
    }
}
