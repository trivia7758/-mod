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
        public string terrainName = "plain";

        [Header("Movement Costs")]
        [Tooltip("Default cost if MovementType not found in the override list. -1 = impassable.")]
        public int defaultMovementCost = 1;

        [Tooltip("Per-unit-type movement cost overrides.")]
        public MovementCostEntry[] movementCosts;

        [Header("Combat Bonuses")]
        public int hitBonus;
        public int avoidBonus;

        [Header("Healing")]
        [Tooltip("HP recovered per turn when a unit stands on this terrain. 0 = no healing.")]
        public int healPerTurn;

        [Header("Visual")]
        public Color minimapColor = Color.green;

        /// <summary>
        /// Get movement cost for a specific unit type.
        /// Returns -1 if impassable for that type.
        /// </summary>
        public int GetMovementCost(MovementType moveType)
        {
            if (movementCosts != null)
            {
                foreach (var entry in movementCosts)
                    if (entry.movementType == moveType)
                        return entry.cost;
            }
            return defaultMovementCost;
        }

        /// <summary>
        /// Check if terrain is passable for a specific unit type.
        /// </summary>
        public bool IsPassableFor(MovementType moveType) => GetMovementCost(moveType) > 0;

        /// <summary>
        /// Check if terrain is passable using default cost (backward compat).
        /// </summary>
        public bool IsPassable => defaultMovementCost > 0;
    }
}
