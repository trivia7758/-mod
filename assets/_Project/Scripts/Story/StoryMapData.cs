using System.Collections.Generic;
using UnityEngine;

namespace CaoCao.Story
{
    /// <summary>
    /// Known placeable unit IDs. Used by editor dropdowns.
    /// </summary>
    public static class StoryUnitIds
    {
        public static readonly string[] All = { "hero", "oldman" };
    }

    [System.Serializable]
    public class UnitPlacement
    {
        public string unitId = "hero";
        public Vector2Int tile;
    }

    [CreateAssetMenu(fileName = "NewStoryMap", menuName = "CaoCao/StoryMapData")]
    public class StoryMapData : ScriptableObject
    {
        [Tooltip("Background key, e.g. 'story/home', 'xuanwo', 'story/jiaoqu2'")]
        public string mapKey = "";

        [Header("Grid Settings")]
        [Tooltip("Grid dimensions in tiles (columns x rows)")]
        public Vector2Int gridSize = new(25, 20);

        [Header("Editor Grid Overlay (editor-only, does NOT affect runtime)")]
        [Tooltip("Diamond half-width in world units (match background tile width)")]
        public float editorTileWidth = 0.24f;
        [Tooltip("Diamond half-height in world units (match background tile height)")]
        public float editorTileHeight = 0.24f;
        [Tooltip("Grid origin offset in world space (position tile 0,0)")]
        public Vector2 editorGridOffset = new(0.24f, -0.24f);

        [Header("Unit Positions")]
        [Tooltip("Starting positions for units on this map")]
        public List<UnitPlacement> unitPlacements = new();

        [Header("Blocked Tiles")]
        [Tooltip("Tiles marked as blocked/unwalkable")]
        public List<Vector2Int> blockedTiles = new();

        public void ApplyTo(StoryPathfinder pathfinder)
        {
            pathfinder.ClearBlocked();
            foreach (var tile in blockedTiles)
                pathfinder.SetBlocked(tile, true);
        }

        public bool IsBlocked(Vector2Int tile)
        {
            return blockedTiles.Contains(tile);
        }

        public void SetBlocked(Vector2Int tile, bool blocked)
        {
            if (blocked && !blockedTiles.Contains(tile))
                blockedTiles.Add(tile);
            else if (!blocked)
                blockedTiles.Remove(tile);
        }
    }
}
