using System;
using UnityEngine;

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

        /// <summary>
        /// Recalculate all derived stats from HeroDefinition base + growth + equipment bonuses.
        /// Must be called after loading, leveling up, or changing equipment.
        /// </summary>
        public void RecalculateStats(HeroDefinition heroDef, GameDataRegistry registry)
        {
            if (heroDef == null) return;

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
