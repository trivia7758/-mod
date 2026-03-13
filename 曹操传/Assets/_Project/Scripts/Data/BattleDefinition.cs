using System;
using UnityEngine;
using CaoCao.Battle;

namespace CaoCao.Data
{
    /// <summary>
    /// Placement data for a single unit in a battle.
    /// </summary>
    [Serializable]
    public class BattleUnitPlacement
    {
        [Tooltip("Links to HeroDefinition.id for player units, or enemy type ID for enemies.")]
        public string unitId;
        public string displayName;
        public Vector2Int startCell;
        public UnitTeam team = UnitTeam.Player;
        [Tooltip("If true, this hero cannot be removed from deployment (shown in red).")]
        public bool isRequired;

        [Header("Enemy Stats (used when team == Enemy)")]
        public int maxHp = 20;
        public int atk = 6;
        public int def = 2;
        public int mov = 3;
    }

    /// <summary>
    /// Defines the configuration for a specific battle/level.
    /// Used by CampManager to set up deployment screen and by
    /// BattleController to spawn units.
    /// </summary>
    [CreateAssetMenu(fileName = "NewBattle", menuName = "CaoCao/Data/Battle Definition")]
    public class BattleDefinition : ScriptableObject
    {
        [Header("Battle Info")]
        public string battleName;            // "黄巾之乱"
        public string sceneName = "Battle";

        [Header("Deployment")]
        [Tooltip("Maximum number of heroes the player can deploy.")]
        public int maxDeployCount = 8;

        [Tooltip("Heroes that must be deployed (locked, shown in red).")]
        public BattleUnitPlacement[] requiredHeroes;

        [Tooltip("Available spawn cells for optional player heroes.")]
        public Vector2Int[] playerSpawnPoints;

        [Header("Enemies")]
        public BattleUnitPlacement[] enemyUnits;

        [Header("Visuals")]
        public Sprite battleBackground;
    }
}
