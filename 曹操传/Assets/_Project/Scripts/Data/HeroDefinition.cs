using UnityEngine;

namespace CaoCao.Data
{
    /// <summary>
    /// Master definition for a hero character (武将).
    /// Contains identity, base attributes, growth rates, skills, and unit type.
    /// Runtime mutable state is stored separately in HeroRuntimeData.
    /// </summary>
    [CreateAssetMenu(fileName = "NewHero", menuName = "CaoCao/Data/Hero")]
    public class HeroDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string id;               // "cao_cao"
        public string displayName;      // "曹操"
        public Sprite portrait;
        public Sprite unitSprite;

        [Header("Base Attributes (六维)")]
        public int force;               // 武力
        public int intelligence;        // 智力
        public int command;             // 统帅
        public int agility;             // 敏捷
        public int luck;                // 运气
        public int breakthrough;        // 破敌

        [Header("Base Stats (Level 1)")]
        public int baseMaxHp = 100;
        public int baseMaxMp = 50;
        public int baseAtk = 10;
        public int baseDef = 8;
        public int baseMov = 5;
        public int baseSpeed = 5;

        [Header("Growth Rates (per level)")]
        public float hpGrowth = 8f;
        public float mpGrowth = 3f;
        public float atkGrowth = 1.5f;
        public float defGrowth = 1.2f;

        [Header("Unit Type (兵种)")]
        public UnitTypeDefinition defaultUnitType;

        [Header("Skills (策略)")]
        public SkillDefinition[] learnableSkills;

        [Header("Passive Abilities (特技)")]
        [Tooltip("Passive ability IDs this hero can unlock.")]
        public string[] passiveAbilityIds;

        [Header("Recruitment")]
        [Tooltip("Chapter number when this hero becomes recruitable.")]
        public int recruitChapter;
        [Tooltip("If true, this hero is always in the roster (e.g. Cao Cao).")]
        public bool isRequired;

        // --- Stat Calculation Methods ---

        public int GetMaxHp(int level) => baseMaxHp + Mathf.RoundToInt(hpGrowth * (level - 1));
        public int GetMaxMp(int level) => baseMaxMp + Mathf.RoundToInt(mpGrowth * (level - 1));
        public int GetAtk(int level) => baseAtk + Mathf.RoundToInt(atkGrowth * (level - 1));
        public int GetDef(int level) => baseDef + Mathf.RoundToInt(defGrowth * (level - 1));
    }
}
