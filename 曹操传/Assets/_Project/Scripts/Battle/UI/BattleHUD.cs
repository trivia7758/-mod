using UnityEngine;
using TMPro;

namespace CaoCao.Battle
{
    public class BattleHUD : MonoBehaviour
    {
        [SerializeField] TMP_Text terrainLabel;
        [SerializeField] TMP_Text hitLabel;
        [SerializeField] TMP_Text avoidLabel;

        public void UpdateTerrain(TerrainInfo info)
        {
            if (terrainLabel != null) terrainLabel.text = $"Terrain: {info.Name}";
            if (hitLabel != null) hitLabel.text = $"Hit: {info.Hit}";
            if (avoidLabel != null) avoidLabel.text = $"Avoid: {info.Avoid}";
        }
    }
}
