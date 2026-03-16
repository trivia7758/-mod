using System;
using UnityEngine;
using CaoCao.Common;

namespace CaoCao.Data
{
    /// <summary>
    /// Mutable runtime state for a single hero. Serialized for save/load.
    /// Links to HeroDefinition via heroId for immutable data.
    /// </summary>
    [Serializable]
    public class HeroRuntimeData
    {
        [Header("Identity")]
        public string heroId;               // links to HeroDefinition.id

        [Header("Progression")]
        public int level = 1;
        public int exp = 0;

        [Header("Five Dimensions (五维) - Grows via grade system")]
        public int force;                   // 武力
        public int intelligence;            // 智力
        public int command;                 // 统帅
        public int agility;                 // 敏捷
        public int luck;                    // 气运

        [Header("Current Status")]
        public int currentHp;
        public int currentMp;

        [Header("Equipment (Item IDs)")]
        public string equippedWeaponId = "";
        public string equippedArmorId = "";
        public string equippedAuxiliaryId = "";

        [Header("State")]
        public bool isRecruited = true;
        public string currentUnitTypeId = "";

        // --- Computed stats (not saved, recalculated on load) ---
        [NonSerialized] public int maxHp;
        [NonSerialized] public int maxMp;
        [NonSerialized] public int atk;
        [NonSerialized] public int def;
        [NonSerialized] public int mov;
        [NonSerialized] public int speed;

        // --- Current grades (not saved, computed from attribute values) ---
        [NonSerialized] public GrowthGrade forceGrade;
        [NonSerialized] public GrowthGrade intelligenceGrade;
        [NonSerialized] public GrowthGrade commandGrade;
        [NonSerialized] public GrowthGrade agilityGrade;
        [NonSerialized] public GrowthGrade luckGrade;

        /// <summary>
        /// Initialize five dimensions from HeroDefinition base values.
        /// Called when first recruiting a hero.
        /// </summary>
        public void InitializeFiveDimensions(HeroDefinition heroDef)
        {
            if (heroDef == null) return;
            force = heroDef.force;
            intelligence = heroDef.intelligence;
            command = heroDef.command;
            agility = heroDef.agility;
            luck = heroDef.luck;
        }

        /// <summary>
        /// Apply one level-up: grow each attribute based on current grade.
        /// Call this when the hero gains a level.
        /// </summary>
        public void ApplyLevelUpGrowth(HeroDefinition heroDef)
        {
            if (heroDef == null) return;
            force += AttributeGrowthSystem.CalculateLevelUpGrowth(force, heroDef.forceGrade);
            intelligence += AttributeGrowthSystem.CalculateLevelUpGrowth(intelligence, heroDef.intelligenceGrade);
            command += AttributeGrowthSystem.CalculateLevelUpGrowth(command, heroDef.commandGrade);
            agility += AttributeGrowthSystem.CalculateLevelUpGrowth(agility, heroDef.agilityGrade);
            luck += AttributeGrowthSystem.CalculateLevelUpGrowth(luck, heroDef.luckGrade);
        }

        /// <summary>
        /// Recalculate current grades from attribute values.
        /// </summary>
        public void RecalculateGrades(HeroDefinition heroDef)
        {
            if (heroDef == null) return;
            forceGrade = AttributeGrowthSystem.GetCurrentGrade(force, heroDef.forceGrade);
            intelligenceGrade = AttributeGrowthSystem.GetCurrentGrade(intelligence, heroDef.intelligenceGrade);
            commandGrade = AttributeGrowthSystem.GetCurrentGrade(command, heroDef.commandGrade);
            agilityGrade = AttributeGrowthSystem.GetCurrentGrade(agility, heroDef.agilityGrade);
            luckGrade = AttributeGrowthSystem.GetCurrentGrade(luck, heroDef.luckGrade);
        }

        /// <summary>
        /// Recalculate all derived stats from HeroDefinition base + growth + equipment bonuses.
        /// Must be called after loading, leveling up, or changing equipment.
        /// </summary>
        public void RecalculateStats(HeroDefinition heroDef, GameDataRegistry registry)
        {
            if (heroDef == null) return;

            // Ensure five dimensions are initialized (for backward compatibility)
            if (force == 0 && intelligence == 0 && command == 0)
                InitializeFiveDimensions(heroDef);

            // Recalculate current grades
            RecalculateGrades(heroDef);

            // Base stats from definition + level growth
            maxHp = heroDef.GetMaxHp(level);
            maxMp = heroDef.GetMaxMp(level);
            atk = heroDef.GetAtk(level);
            def = heroDef.GetDef(level);
            mov = heroDef.baseMov;
            speed = heroDef.baseSpeed;

            // Unit type modifiers
            if (registry != null && !string.IsNullOrEmpty(currentUnitTypeId))
            {
                var unitType = registry.GetUnitType(currentUnitTypeId);
                if (unitType != null)
                {
                    atk += unitType.atkModifier;
                    def += unitType.defModifier;
                    mov += unitType.movModifier;
                    speed += unitType.speedModifier;
                }
            }

            // Equipment bonuses
            ApplyEquipmentBonus(equippedWeaponId, registry);
            ApplyEquipmentBonus(equippedArmorId, registry);
            ApplyEquipmentBonus(equippedAuxiliaryId, registry);

            // Clamp current HP/MP to max
            currentHp = Mathf.Clamp(currentHp, 0, maxHp);
            currentMp = Mathf.Clamp(currentMp, 0, maxMp);
        }

        void ApplyEquipmentBonus(string itemId, GameDataRegistry registry)
        {
            if (string.IsNullOrEmpty(itemId) || registry == null) return;
            var item = registry.GetItem(itemId);
            if (item == null) return;

            atk += item.atkBonus;
            def += item.defBonus;
            maxHp += item.hpBonus;
            maxMp += item.mpBonus;
            speed += item.speedBonus;
        }

        /// <summary>
        /// Get the equipped item ID for a given slot.
        /// </summary>
        public string GetEquippedItemId(CaoCao.Common.EquipSlot slot)
        {
            return slot switch
            {
                CaoCao.Common.EquipSlot.Weapon => equippedWeaponId,
                CaoCao.Common.EquipSlot.Armor => equippedArmorId,
                CaoCao.Common.EquipSlot.Auxiliary => equippedAuxiliaryId,
                _ => ""
            };
        }

        /// <summary>
        /// Set the equipped item ID for a given slot.
        /// </summary>
        public void SetEquippedItemId(CaoCao.Common.EquipSlot slot, string itemId)
        {
            switch (slot)
            {
                case CaoCao.Common.EquipSlot.Weapon:
                    equippedWeaponId = itemId ?? "";
                    break;
                case CaoCao.Common.EquipSlot.Armor:
                    equippedArmorId = itemId ?? "";
                    break;
                case CaoCao.Common.EquipSlot.Auxiliary:
                    equippedAuxiliaryId = itemId ?? "";
                    break;
            }
        }
    }
}
