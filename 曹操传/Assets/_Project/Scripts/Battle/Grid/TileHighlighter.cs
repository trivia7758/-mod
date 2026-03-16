using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace CaoCao.Battle
{
    public class TileHighlighter : MonoBehaviour
    {
        [SerializeField] Vector2Int tileSize = new(48, 48);
        [SerializeField] Color moveColor = new(0.2f, 0.5f, 0.95f, 0.35f);
        [SerializeField] Color attackColor = new(0.95f, 0.15f, 0.15f, 0.35f);
        [SerializeField] Color healColor = new(0.2f, 0.9f, 0.3f, 0.35f);
        [SerializeField] Color hoverColor = new(1f, 0.9f, 0.2f, 1f);

        public Vector2 Origin { get; set; }
        public Vector2Int TileSize { get => tileSize; set => tileSize = value; }

        readonly List<GameObject> _moveQuads = new();
        readonly List<GameObject> _attackQuads = new();
        readonly List<GameObject> _effectLabels = new();
        GameObject _hoverQuad;
        Vector2Int _hoverCell = new(9999, 9999);

        // ── Public API ──

        /// <summary>Show movement range (blue) with optional effect% labels.</summary>
        public void SetMoveCells(List<Vector2Int> cells, Dictionary<Vector2Int, int> effects = null)
        {
            ClearQuads(_moveQuads);
            ClearQuads(_effectLabels);

            foreach (var cell in cells)
            {
                _moveQuads.Add(CreateQuad(cell, moveColor, "MoveHL"));

                // Show effect% label if available and not 100%
                if (effects != null && effects.TryGetValue(cell, out int pct))
                {
                    _effectLabels.Add(CreateEffectLabel(cell, pct));
                }
            }
        }

        /// <summary>Show attack range (red) with optional effect% labels.</summary>
        public void SetAttackCells(List<Vector2Int> cells, Dictionary<Vector2Int, int> effects = null)
        {
            ClearQuads(_attackQuads);
            foreach (var cell in cells)
            {
                _attackQuads.Add(CreateQuad(cell, attackColor, "AtkHL"));
                if (effects != null && effects.TryGetValue(cell, out int pct))
                    _effectLabels.Add(CreateEffectLabel(cell, pct));
            }
        }

        /// <summary>Show heal range (green).</summary>
        public void SetHealCells(List<Vector2Int> cells)
        {
            ClearQuads(_attackQuads); // reuse attack quads layer
            foreach (var cell in cells)
                _attackQuads.Add(CreateQuad(cell, healColor, "HealHL"));
        }

        /// <summary>Show hover outline (yellow border).</summary>
        public void SetHoverCell(Vector2Int cell)
        {
            _hoverCell = cell;
            UpdateHover();
        }

        /// <summary>Clear all highlights.</summary>
        public void ClearAll()
        {
            ClearQuads(_moveQuads);
            ClearQuads(_attackQuads);
            ClearQuads(_effectLabels);
            _hoverCell = new Vector2Int(9999, 9999);
            if (_hoverQuad != null) { Destroy(_hoverQuad); _hoverQuad = null; }
        }

        // ── Internal ──

        void UpdateHover()
        {
            if (_hoverQuad != null)
                Destroy(_hoverQuad);

            if (_hoverCell.x < 9999)
                _hoverQuad = CreateQuadOutline(_hoverCell, hoverColor, "HoverHL");
        }

        GameObject CreateQuad(Vector2Int cell, Color color, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = CreatePixelSprite();
            sr.color = color;
            sr.sortingOrder = 1;
            go.transform.localScale = new Vector3(tileSize.x, tileSize.y, 1);
            go.transform.position = CellToWorldCenter(cell);
            return go;
        }

        GameObject CreateEffectLabel(Vector2Int cell, int effectPct)
        {
            var go = new GameObject("EffectLabel");
            go.transform.SetParent(transform);

            var tmp = go.AddComponent<TextMeshPro>();
            tmp.text = effectPct.ToString();
            tmp.fontSize = 150;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.sortingOrder = 5;
            tmp.enableWordWrapping = false;
            tmp.overflowMode = TextOverflowModes.Overflow;

            // Black outline for readability
            tmp.outlineWidth = 0.25f;
            tmp.outlineColor = new Color32(0, 0, 0, 200);

            // Color by effect value
            if (effectPct >= 120)
                tmp.color = new Color(1f, 0.85f, 0f);       // 金色 ★120%
            else if (effectPct >= 110)
                tmp.color = new Color(0.3f, 1f, 0.3f);      // 绿色 ◎110%
            else if (effectPct == 100)
                tmp.color = new Color(1f, 1f, 1f, 0.85f);   // 白色 ○100%
            else if (effectPct >= 90)
                tmp.color = new Color(1f, 0.7f, 0.3f);      // 橙色 △90%
            else
                tmp.color = new Color(1f, 0.3f, 0.3f);      // 红色 ×80%

            // Fit text into the tile
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(tileSize.x, tileSize.y);

            var pos = CellToWorldCenter(cell);
            pos.z = -0.1f; // In front of highlight
            go.transform.position = pos;

            return go;
        }

        GameObject CreateQuadOutline(Vector2Int cell, Color color, string name)
        {
            var parent = new GameObject(name);
            parent.transform.SetParent(transform);
            var center = CellToWorldCenter(cell);
            float w = tileSize.x;
            float h = tileSize.y;
            float thickness = 2f;

            CreateBorderLine(parent, center + new Vector3(0, h / 2, 0), w, thickness, color);
            CreateBorderLine(parent, center + new Vector3(0, -h / 2, 0), w, thickness, color);
            CreateBorderLine(parent, center + new Vector3(-w / 2, 0, 0), thickness, h, color);
            CreateBorderLine(parent, center + new Vector3(w / 2, 0, 0), thickness, h, color);

            return parent;
        }

        void CreateBorderLine(GameObject parent, Vector3 pos, float width, float height, Color color)
        {
            var go = new GameObject("Border");
            go.transform.SetParent(parent.transform);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = CreatePixelSprite();
            sr.color = color;
            sr.sortingOrder = 2;
            go.transform.localScale = new Vector3(width, height, 1);
            go.transform.position = pos;
        }

        Vector3 CellToWorldCenter(Vector2Int cell)
        {
            return (Vector3)Origin + new Vector3(
                cell.x * tileSize.x + tileSize.x * 0.5f,
                cell.y * tileSize.y + tileSize.y * 0.5f,
                0
            );
        }

        static void ClearQuads(List<GameObject> quads)
        {
            foreach (var q in quads)
                if (q != null) Destroy(q);
            quads.Clear();
        }

        static Sprite _pixelSprite;
        static Sprite CreatePixelSprite()
        {
            if (_pixelSprite != null) return _pixelSprite;
            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            _pixelSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1);
            return _pixelSprite;
        }
    }
}
