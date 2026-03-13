using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CaoCao.Core;
using CaoCao.Data;
using CaoCao.UI;

namespace CaoCao.Camp
{
    /// <summary>
    /// Save/Load screen (存档/读档).
    /// Two modes: save and load. Shows 50 slots with chapter name and timestamp.
    /// Uses parchment row styling matching the existing LoadDialog pattern.
    /// </summary>
    public class SaveLoadScreen : BaseScreen
    {
        [Header("Mode Tabs")]
        [SerializeField] Button saveTab;
        [SerializeField] Button loadTab;

        [Header("Slot List")]
        [SerializeField] Transform rowsContainer;
        [SerializeField] ScrollRect scrollRect;

        [Header("Navigation")]
        [SerializeField] Button backButton;

        [Header("Title")]
        [SerializeField] TMP_Text titleLabel;

        [Header("Confirmation")]
        [SerializeField] RectTransform confirmPanel;
        [SerializeField] TMP_Text confirmMessage;
        [SerializeField] Button confirmYesBtn;
        [SerializeField] Button confirmNoBtn;

        /// <summary>Fired when back button is pressed.</summary>
        public event Action OnBack;

        bool _isSaveMode = true;
        int _pendingSlot = -1;
        GameStateManager _gsm;

        const int MaxSlots = 50;

        // Row styling colors (matching LoadDialog parchment theme)
        static readonly Color RowBgEven = new(0.92f, 0.90f, 0.86f, 1f);
        static readonly Color RowBgOdd = new(0.88f, 0.85f, 0.80f, 1f);
        static readonly Color RowText = new(0.3f, 0.2f, 0.1f, 1f);
        static readonly Color RowHover = new(0.95f, 0.92f, 0.85f, 1f);
        static readonly Color EmptySlotText = new(0.6f, 0.5f, 0.4f, 1f);

        readonly List<GameObject> _rows = new();

        public override void Initialize()
        {
            _gsm = ServiceLocator.Get<GameStateManager>();

            saveTab?.onClick.AddListener(() => SetMode(true));
            loadTab?.onClick.AddListener(() => SetMode(false));
            backButton?.onClick.AddListener(() => OnBack?.Invoke());

            if (backButton != null) ThreeKingdomsTheme.StyleButton(backButton);

            confirmYesBtn?.onClick.AddListener(OnConfirmYes);
            confirmNoBtn?.onClick.AddListener(OnConfirmNo);

            HideConfirmPanel();
        }

        public override void Show()
        {
            base.Show();
            SetMode(true); // default to save mode
        }

        void SetMode(bool isSave)
        {
            _isSaveMode = isSave;
            HideConfirmPanel();

            // Highlight active tab
            HighlightTab(saveTab, isSave);
            HighlightTab(loadTab, !isSave);

            if (titleLabel != null)
            {
                titleLabel.text = isSave ? "存档" : "读取";
                titleLabel.color = ThreeKingdomsTheme.TextGold;
            }

            BuildRows();
        }

        void HighlightTab(Button tab, bool active)
        {
            if (tab == null) return;
            var img = tab.GetComponent<Image>();
            if (img != null)
                img.color = active
                    ? ThreeKingdomsTheme.SpeedActive
                    : ThreeKingdomsTheme.SpeedInactive;
        }

        void BuildRows()
        {
            // Clear existing rows
            foreach (var go in _rows)
                if (go != null) Destroy(go);
            _rows.Clear();

            if (rowsContainer == null || _gsm == null) return;

            for (int i = 1; i <= MaxSlots; i++)
            {
                var slotInfo = _gsm.GetSlotInfo(i);
                CreateRow(i, slotInfo);
            }
        }

        void CreateRow(int slotNum, SaveSlotInfo info)
        {
            // Row container
            var rowGo = new GameObject($"Slot_{slotNum:D2}");
            rowGo.transform.SetParent(rowsContainer, false);
            _rows.Add(rowGo);

            var rt = rowGo.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 40);

            // Background
            var bg = rowGo.AddComponent<Image>();
            bg.color = slotNum % 2 == 0 ? RowBgEven : RowBgOdd;

            // Layout
            var hlg = rowGo.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 10;
            hlg.padding = new RectOffset(10, 10, 5, 5);
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;

            // Slot number
            CreateCellText(rowGo.transform, $"No.{slotNum:D2}", 80, info.exists);

            // Chapter name
            string chapterText = info.exists ? info.chapterName : "-- 空 --";
            CreateCellText(rowGo.transform, chapterText, 400, info.exists);

            // Timestamp
            string timeText = info.exists ? info.timestamp : "";
            CreateCellText(rowGo.transform, timeText, 200, info.exists);

            // Click handler
            var btn = rowGo.AddComponent<Button>();
            btn.targetGraphic = bg;
            var colors = btn.colors;
            colors.normalColor = slotNum % 2 == 0 ? RowBgEven : RowBgOdd;
            colors.highlightedColor = RowHover;
            colors.pressedColor = RowHover;
            btn.colors = colors;

            int slot = slotNum; // capture for closure
            btn.onClick.AddListener(() => OnSlotClicked(slot));
        }

        void CreateCellText(Transform parent, string text, float width, bool hasData)
        {
            var go = new GameObject("Cell");
            go.transform.SetParent(parent, false);

            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth = width;
            le.flexibleWidth = width > 200 ? 1 : 0;

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 18;
            tmp.color = hasData ? RowText : EmptySlotText;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
        }

        void OnSlotClicked(int slot)
        {
            if (_isSaveMode)
            {
                // Confirm save (especially if overwriting)
                bool exists = _gsm.SlotExists(slot);
                _pendingSlot = slot;

                if (exists)
                {
                    ShowConfirmPanel($"位置 {slot} 已有存档，是否覆盖？");
                }
                else
                {
                    // Save directly
                    DoSave(slot);
                }
            }
            else
            {
                // Load mode
                if (!_gsm.SlotExists(slot))
                {
                    Debug.Log($"[SaveLoadScreen] Slot {slot} is empty.");
                    return;
                }

                _pendingSlot = slot;
                ShowConfirmPanel($"确认读取位置 {slot} 的存档？");
            }
        }

        void DoSave(int slot)
        {
            _gsm.SaveToSlot(slot);
            HideConfirmPanel();
            BuildRows(); // refresh display
        }

        void DoLoad(int slot)
        {
            if (_gsm.LoadFromSlot(slot))
            {
                HideConfirmPanel();
                // Optionally reload scene or notify
                Debug.Log($"[SaveLoadScreen] Loaded slot {slot} successfully.");
                BuildRows();
            }
        }

        void ShowConfirmPanel(string message)
        {
            if (confirmPanel != null)
                confirmPanel.gameObject.SetActive(true);
            if (confirmMessage != null)
                confirmMessage.text = message;
        }

        void HideConfirmPanel()
        {
            if (confirmPanel != null)
                confirmPanel.gameObject.SetActive(false);
            _pendingSlot = -1;
        }

        void OnConfirmYes()
        {
            if (_pendingSlot < 1) return;

            if (_isSaveMode)
                DoSave(_pendingSlot);
            else
                DoLoad(_pendingSlot);
        }

        void OnConfirmNo()
        {
            HideConfirmPanel();
        }
    }
}
