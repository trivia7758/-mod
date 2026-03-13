using UnityEngine;
using CaoCao.Common;

namespace CaoCao.Data
{
    /// <summary>
    /// Defines a strategy/skill (策略) that heroes can learn and use.
    /// </summary>
    [CreateAssetMenu(fileName = "NewSkill", menuName = "CaoCao/Data/Skill")]
    public class SkillDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string id;               // "fire_attack"
        public string displayName;      // "火攻"
        [TextArea(2, 4)]
        public string description;      // "对敌方造成智力伤害"

        [Header("Cost & Range")]
        public int mpCost = 5;
        public int range = 1;
        public int power = 10;

        [Header("Effect")]
        public SkillEffectType effectType = SkillEffectType.Damage;

        [Header("Learning")]
        [Tooltip("Hero level at which this skill is learned.")]
        public int learnLevel = 1;

        [Header("Usage")]
        public bool usableInBattle = true;
        public bool usableInCamp = false;
    }
}
