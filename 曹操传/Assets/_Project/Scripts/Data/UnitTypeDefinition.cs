using UnityEngine;
using CaoCao.Common;

namespace CaoCao.Data
{
    /// <summary>
    /// Defines a unit class/type (兵种), e.g. 轻步兵, 重骑兵.
    /// Each hero has a default unit type that can be upgraded.
    /// </summary>
    [CreateAssetMenu(fileName = "NewUnitType", menuName = "CaoCao/Data/Unit Type")]
    public class UnitTypeDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string id;               // "light_infantry"
        public string displayName;      // "轻步兵"

        [Header("Movement")]
        public MovementType movementType = MovementType.Infantry;

        [Header("Stat Modifiers")]
        public int atkModifier;
        public int defModifier;
        public int movModifier;
        public int speedModifier;

        [Header("Upgrade Path")]
        [Tooltip("Next unit type in the upgrade chain. Null if max level.")]
        public UnitTypeDefinition upgradeTo;
        [Tooltip("Hero level required to upgrade to the next unit type.")]
        public int upgradeLevel;
    }
}
