using System.Collections.Generic;
using UnityEngine;

namespace CaoCao.Battle
{
    public class TileHighlighter : MonoBehaviour
    {
        [SerializeField] Vector2Int tileSize = new(48, 48);
        [SerializeField] Color moveColor = new(0.2f, 0.5f, 0.95f, 0.35f);
        [SerializeField] Color attackColor = new(0.9f, 0.2f, 0.2f, 0.35f);
        [SerializeField] Color hoverColor = new(1f, 0.9f, 0.2f, 1f);

        public Vector2 Origin { get; set; }
        public Vector2Int TileSize { get => tileSize; set => tileSize = value; }

        List<Vector2Int> _moveCells = new();
        List<Vector2Int> _attackCells = new();
        Vector2Int _hoverCell = new(9999, 9999);

        // Use quads for rendering highlights
        readonly List<GameObject> _moveQuads = new();
        readonly List<GameObject> _attackQuads = new();
        GameObject _hoverQuad;

        public void SetMoveCells(List<Vector2Int> cells)
        {
            _moveCells = new List<Vector2Int>(cells);
            RebuildHighlights();
        }

        public void SetAttackCells(List<Vector2Int> cells)
        {
            _attackCells = new List<Vector2Int>(cells);
            RebuildHighlights();
        }

        public void SetHoverCell(Vector2Int cell)
        {
            _hoverCell = cell;
            UpdateHover();
        }

        public void ClearAll()
        {
            _moveCells.Clear();
            _attackCells.Clear();
            _hoverCell = new Vector2Int(9999, 9999);
            RebuildHighlights();
        }

        void RebuildHighlights()
        {
            ClearQuads(_moveQuads);
            ClearQuads(_attackQuads);

            foreach (var cell in _moveCells)
                _moveQuads.Add(CreateQuad(cell, moveColor, "MoveHL"));
            foreach (var cell in _attackCells)
                _attackQuads.Add(CreateQuad(cell, attackColor, "AtkHL"));
        }

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

        GameObject CreateQuadOutline(Vector2Int cell, Color color, string name)
        {
            // For outline, use a slightly smaller quad with wireframe-like approach
            // Simple approach: use a thin border via 4 thin quads
            var parent = new GameObject(name);
            parent.transform.SetParent(transform);
            var center = CellToWorldCenter(cell);
            float w = tileSize.x;
            float h = tileSize.y;
            float thickness = 2f;

            CreateBorderLine(parent, center + new Vector3(0, h / 2, 0), w, thickness, color); // top
            CreateBorderLine(parent, center + new Vector3(0, -h / 2, 0), w, thickness, color); // bottom
            CreateBorderLine(parent, center + new Vector3(-w / 2, 0, 0), thickness, h, color); // left
            CreateBorderLine(parent, center + new Vector3(w / 2, 0, 0), thickness, h, color); // right

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
