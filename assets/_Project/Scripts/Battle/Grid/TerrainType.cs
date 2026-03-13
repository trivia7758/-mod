using UnityEngine;

namespace CaoCao.Battle
{
    [CreateAssetMenu(fileName = "NewTerrain", menuName = "CaoCao/TerrainType")]
    public class TerrainType : ScriptableObject
    {
        public string terrainName = "plain";
        public int movementCost = 1; // -1 = impassable
        public int hitBonus;
        public int avoidBonus;
        public Color minimapColor = Color.green;

        public bool IsPassable => movementCost > 0;
    }
}
