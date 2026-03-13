using UnityEngine;

namespace CaoCao.Battle
{
    public enum UnitTeam { Player, Enemy }

    [CreateAssetMenu(fileName = "NewBattleUnit", menuName = "CaoCao/BattleUnitDefinition")]
    public class BattleUnitDefinition : ScriptableObject
    {
        public string unitName = "Soldier";
        public int maxHp = 20;
        public int atk = 6;
        public int def = 2;
        public int mov = 5;
        public UnitTeam team;
        public Sprite unitSprite;
        public Color teamColor = Color.blue;
    }
}
