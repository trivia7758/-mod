using UnityEngine;
using CaoCao.Common;

namespace CaoCao.Data
{
    /// <summary>
    /// Defines an item or piece of equipment (物品/装备).
    /// ItemType determines which warehouse tab it appears under
    /// and which equipment slot it occupies (if equippable).
    /// </summary>
    [CreateAssetMenu(fileName = "NewItem", menuName = "CaoCao/Data/Item")]
    public class ItemDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string id;               // "iron_sword"
        public string displayName;      // "铁剑"
        [TextArea(2, 4)]
        public string description;
        public Sprite icon;

        [Header("Type")]
        public ItemType itemType = ItemType.Consumable;

        [Header("Equipment Bonuses")]
        [Tooltip("Only meaningful for Weapon/Armor/Auxiliary types.")]
        public int atkBonus;
        public int defBonus;
        public int speedBonus;
        public int hpBonus;
        public int mpBonus;

        [Header("Consumable")]
        [Tooltip("HP restored when used (Consumable type).")]
        public int healAmount;

        [Header("Usage")]
        public bool usableInBattle;
        public bool usableInCamp;

        [Header("Economy")]
        public int buyPrice;
        public int sellPrice;

        [Header("Restrictions")]
        [Tooltip("Hero IDs that can equip this item. Empty = no hero restriction.")]
        public string[] restrictToHeroIds;

        [Tooltip("Unit type IDs that can equip this item (e.g. light_infantry, heavy_cavalry). Empty = no unit type restriction.")]
        public string[] restrictToUnitTypeIds;

        /// <summary>
        /// Whether this item is equipment (can be equipped in a slot).
        /// </summary>
        public bool IsEquipment =>
            itemType == ItemType.Weapon ||
            itemType == ItemType.Armor ||
            itemType == ItemType.Auxiliary;

        /// <summary>
        /// Returns the EquipSlot this item occupies. Only valid for equipment.
        /// </summary>
        public EquipSlot GetEquipSlot()
        {
            return itemType switch
            {
                ItemType.Weapon => EquipSlot.Weapon,
                ItemType.Armor => EquipSlot.Armor,
                ItemType.Auxiliary => EquipSlot.Auxiliary,
                _ => EquipSlot.Weapon
            };
        }

        /// <summary>
        /// Check if a specific hero can equip this item, considering both hero ID and unit type.
        /// </summary>
        public bool CanHeroEquip(string heroId, string unitTypeId = null)
        {
            if (!IsEquipment) return false;

            // Check hero restriction
            if (restrictToHeroIds != null && restrictToHeroIds.Length > 0)
            {
                bool heroMatch = false;
                foreach (var id in restrictToHeroIds)
                {
                    if (id == heroId) { heroMatch = true; break; }
                }
                if (!heroMatch) return false;
            }

            // Check unit type restriction (weapons & armor)
            if (restrictToUnitTypeIds != null && restrictToUnitTypeIds.Length > 0
                && !string.IsNullOrEmpty(unitTypeId))
            {
                bool unitMatch = false;
                foreach (var id in restrictToUnitTypeIds)
                {
                    if (id == unitTypeId) { unitMatch = true; break; }
                }
                if (!unitMatch) return false;
            }

            return true;
        }
    }
}
