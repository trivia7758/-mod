using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using CaoCao.Core;

namespace CaoCao.UI
{
    public class LoadDialog : BaseScreen
    {
        [SerializeField] Button closeButton;
        [SerializeField] Button bottomButton;
        [SerializeField] Transform rowsContainer;

        readonly List<Button> slotButtons = new();

        public event Action<int> OnSlotSelected;
        public event Action OnClosed;

        static readonly Color ParchmentBg = new(0.92f, 0.9f, 0.86f, 1f);
        static readonly Color RowText = new(0.3f, 0.2f, 0.1f, 1f);
        static readonly Color RowHover = new(0.95f, 0.92f, 0.85f, 1f);

        const int SlotCount = 10;

        public override void Initialize()
        {
            if (closeButton != null)
                closeButton.onClick.AddListener(() => { Hide(); OnClosed?.Invoke(); });
            if (bottomButton != null)
                bottomButton.onClick.AddListener(() => { Hide(); OnClosed?.Invoke(); });

            Hide();
        }

        public override void Show()
        {
            base.Show();
            RefreshSlots();
        }

        void RefreshSlots()
        {
            ClearRows();

            if (rowsContainer == null) return;

            var gsm = ServiceLocator.Get<GameStateManager>();

            for (int i = 0; i < SlotCount; i++)
            {
                int slot = i + 1;
                var info = gsm?.GetSlotInfo(slot);

                Dictionary<string, string> data = null;
                if (info != null && info.exists)
                {
                    data = new Dictionary<string, string>
                    {
                        ["level"] = info.chapterName,
                        ["record"] = "",
                        ["time"] = info.timestamp
                    };
                }

                var rowBtn = CreateRowButton(rowsContainer, i, data);
                int capturedSlot = slot;
                rowBtn.onClick.AddListener(() => OnSlotSelected?.Invoke(capturedSlot));
                slotButtons.Add(rowBtn);
            }
        }

        void ClearRows()
        {
            foreach (var btn in slotButtons)
                if (btn != null) Destroy(btn.gameObject);
            slotButtons.Clear();
        }

        Button CreateRowButton(Transform parent, int index, Dictionary<string, string> data)
        {
            var go = new GameObject($"Row_{index}");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 36);

            var img = go.AddComponent<Image>();
            img.color = (index % 2 == 0) ? new Color(0.9f, 0.88f, 0.84f, 1f) : ParchmentBg;

            var btn = go.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = RowHover;
            colors.pressedColor = new Color(0.92f, 0.88f, 0.78f, 1f);
            colors.selectedColor = Color.white;
            btn.colors = colors;
            btn.targetGraphic = img;

            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 36;

            var hlg = go.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 0;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;
            hlg.childControlWidth = false;
            hlg.childControlHeight = true;

            string no = $"No.{index + 1}";
            string level = data != null && data.ContainsKey("level") ? data["level"] : "";
            string record = data != null && data.ContainsKey("record") ? data["record"] : "空";
            string time = data != null && data.ContainsKey("time") ? data["time"] : "";

            // If no data, show "空"
            if (data == null)
            {
                record = "空";
                level = "";
                time = "";
            }

            CreateRowCell(go.transform, no, 90);
            CreateRowCell(go.transform, level, 150);
            CreateRowCell(go.transform, record, 560);
            CreateRowCell(go.transform, time, 190);

            return btn;
        }

        void CreateRowCell(Transform parent, string text, float width)
        {
            var go = new GameObject("Cell");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(width, 36);

            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth = width;
            le.preferredHeight = 36;

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 18;
            tmp.color = RowText;
            tmp.alignment = TextAlignmentOptions.Left;
            tmp.margin = new Vector4(4, 0, 4, 0);
        }
    }
}
