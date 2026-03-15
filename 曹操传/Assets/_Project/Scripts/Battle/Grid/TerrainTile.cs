using UnityEngine;
using UnityEngine.Tilemaps;

namespace CaoCao.Battle
{
    [CreateAssetMenu(fileName = "NewTerrainTile", menuName = "CaoCao/Terrain Tile")]
    public class TerrainTile : TileBase
    {
        public TerrainType terrainType;
        public Sprite tileSprite;
        public Color tileColor = Color.white;

        public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
        {
            tileData.sprite = tileSprite;
            tileData.color = tileColor;
            tileData.colliderType = Tile.ColliderType.None;
        }
    }
}
