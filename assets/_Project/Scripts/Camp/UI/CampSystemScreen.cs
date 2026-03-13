using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CaoCao.Core;
using CaoCao.Data;

namespace CaoCao.Camp
{
    /// <summary>
    /// System screen (系统) with save/load. Uses parchment style matching MainMenu's LoadDialog.
    /// Includes confirmation dialog before save/load actions.
    /// </summary>
    public class CampSystemScreen : MonoBehaviour
    {
        public event Action OnBack;
        public event Action OnReturnToMainMenu;

        GameStateManager _gsm;
        GameDataRegistry _registry;

        // State
        bool _isSaveMode = true;
        const int SlotCount = 10;

        // Parchment colors (matching LoadDialog)
        static readonly Color ParchmentBg = new(0.92f, 0.9f, 0.86f, 1f);
        static readonly Color RowText = new(0.3f, 0.2f, 0.1f, 1f);
        static readonly Color RowHover = new(0.95f, 0.92f, 0.85f, 1f);
        static readonly Color RowAlt = new(0.88f, 0.86f, 0.82f, 1f);
        static readonly Color TabActive = new(0.75f, 0.60f, 0.35f, 1f);
        static readonly Color TabNormal = new(0.2f, 0.16f, 0.12f, 1f);

        // UI references
        Button _saveTabBtn, _loadTabBtn;
        RectTransform _slotContainer;
        TMP_Text _titleText;
        TMP_Text _statusText;
        List<Button> _slotRows = new();

        // Confirmation dialog
        GameObject _confirmDialog;
        TMP_Text _confirmText;
        int _pendingSlot = -1;

        public void Build(GameStateManager gsm, GameDataRegistry registry)
        {
            _gsm = gsm;
            _registry = registry;
            BuildLayout();
        }

        public void Refresh()
        {
            // Re-fetch gsm in case it was bootstrapped after initial Build
            if (_gsm == null)
                _gsm = ServiceLocator.Get<GameStateManager>();
            RefreshSlotList();
        }

        void BuildLayout()
        {
            // ── Parchment background panel ──
            var parchment = new GameObject("ParchmentBg");
            parchment.transform.SetParent(transform, false);
            var prt = parchment.AddComponent<RectTransform>();
            prt.anchorMin = new Vector2(0.05f, 0.08f);
            prt.anchorMax = new Vector2(0.95f, 0.95f);
            prt.offsetMin = Vector2.zero;
            prt.offsetMax = Vector2.zero;
            var pImg = parchment.AddComponent<Image>();
            pImg.color = ParchmentBg;

            // ── Title ──
            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(parchment.transform, false);
            var trt = titleGo.AddComponent<RectTransform>();
            trt.anchorMin = new Vector2(0, 0.92f);
            trt.anchorMax = new Vector2(1, 1);
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;
            _titleText = titleGo.AddComponent<TextMeshProUGUI>();
            _titleText.text = "存档记录";
            _titleText.fontSize = 26f;
            _titleText.color = TabActive;
            _titleText.alignment = TextAlignmentOptions.Center;
            _titleText.fontStyle = FontStyles.Bold;

            // ── Column headers ──
            var headerGo = new GameObject("Header");
            headerGo.transform.SetParent(parchment.transform, false);
            var hrt = headerGo.AddComponent<RectTransform>();
            hrt.anchorMin = new Vector2(0, 0.85f);
            hrt.anchorMax = new Vector2(1, 0.92f);
            hrt.offsetMin = Vector2.zero;
            hrt.offsetMax = Vector2.zero;

            var hhlg = headerGo.AddComponent<HorizontalLayoutGroup>();
            hhlg.spacing = 0;
            hhlg.childAlignment = TextAnchor.MiddleLeft;
            hhlg.childForceExpandWidth = false;
            hhlg.childForceExpandHeight = true;
            hhlg.childControlWidth = false;
            hhlg.childControlHeight = true;

            CreateHeaderCell(headerGo.transform, "编号", 100);
            CreateHeaderCell(headerGo.transform, "等级", 130);
            CreateHeaderCell(headerGo.transform, "记录", 400);
            CreateHeaderCell(headerGo.transform, "时间", 190);

            // ── Slot list container (simple VLG, no ScrollRect) ──
            var slotArea = new GameObject("SlotContainer");
            slotArea.transform.SetParent(parchment.transform, false);
            var saRt = slotArea.AddComponent<RectTransform>();
            saRt.anchorMin = new Vector2(0, 0.08f);
            saRt.anchorMax = new Vector2(1, 0.85f);
            saRt.offsetMin = Vector2.zero;
            saRt.offsetMax = Vector2.zero;

            var vlg = slotArea.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 2;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.padding = new RectOffset(0, 0, 0, 0);

            _slotContainer = saRt;

            // ── Status text ──
            var statusGo = new GameObject("StatusText");
            statusGo.transform.SetParent(parchment.transform, false);
            var srt = statusGo.AddComponent<RectTransform>();
            srt.anchorMin = new Vector2(0, 0);
            srt.anchorMax = new Vector2(1, 0.08f);
            srt.offsetMin = new Vector2(10, 0);
            srt.offsetMax = new Vector2(-10, 0);
            _statusText = statusGo.AddComponent<TextMeshProUGUI>();
            _statusText.fontSize = 18f;
            _statusText.color = TabActive;
            _statusText.alignment = TextAlignmentOptions.Center;
            _statusText.text = "";

            // ── Close X ──
            var closeBtn = CreateCloseButton(parchment.transform);
            closeBtn.onClick.AddListener(() => OnBack?.Invoke());

            // ── Tab bar ──
            _saveTabBtn = CampUIHelper.CreateAnchoredButton(transform, "存档",
                new Vector2(0.2f, 0.96f), new Vector2(0.49f, 1f),
                Vector2.zero, Vector2.zero, 20f);
            _saveTabBtn.onClick.AddListener(() => SetMode(true));

            _loadTabBtn = CampUIHelper.CreateAnchoredButton(transform, "读档",
                new Vector2(0.51f, 0.96f), new Vector2(0.8f, 1f),
                Vector2.zero, Vector2.zero, 20f);
            _loadTabBtn.onClick.AddListener(() => SetMode(false));

            // ── Bottom buttons ──
            var backBtn = CampUIHelper.CreateAnchoredButton(transform, "返回",
                new Vector2(0.15f, 0), new Vector2(0.45f, 0.07f),
                new Vector2(0, 2), Vector2.zero);
            backBtn.onClick.AddListener(() => OnBack?.Invoke());

            var menuBtn = CampUIHelper.CreateAnchoredButton(transform, "回主菜单",
                new Vector2(0.55f, 0), new Vector2(0.85f, 0.07f),
                new Vector2(0, 2), Vector2.zero);
            menuBtn.onClick.AddListener(() => OnReturnToMainMenu?.Invoke());

            // ── Build confirmation dialog (hidden) ──
            BuildConfirmDialog();

            // ── Initial slot list ──
            RefreshSlotList();
        }

        // ================================================================
        // Confirmation Dialog
        // ================================================================

        void BuildConfirmDialog()
        {
            _confirmDialog = new GameObject("ConfirmDialog");
            _confirmDialog.transform.SetParent(transform, false);
            var drt = _confirmDialog.AddComponent<RectTransform>();
            drt.anchorMin = Vector2.zero;
            drt.anchorMax = Vector2.one;
            drt.offsetMin = Vector2.zero;
            drt.offsetMax = Vector2.zero;

            // Semi-transparent overlay to block clicks behind
            var overlay = _confirmDialog.AddComponent<Image>();
            overlay.color = new Color(0, 0, 0, 0.5f);

            // Dialog box
            var box = new GameObject("Box");
            box.transform.SetParent(_confirmDialog.transform, false);
            var brt = box.AddComponent<RectTransform>();
            brt.anchorMin = new Vector2(0.25f, 0.35f);
            brt.anchorMax = new Vector2(0.75f, 0.65f);
            brt.offsetMin = Vector2.zero;
            brt.offsetMax = Vector2.zero;
            var boxImg = box.AddComponent<Image>();
            boxImg.color = ParchmentBg;
            var boxOutline = box.AddComponent<Outline>();
            boxOutline.effectColor = TabActive;
            boxOutline.effectDistance = new Vector2(2, -2);

            // Confirm text
            var txtGo = new GameObject("Text");
            txtGo.transform.SetParent(box.transform, false);
            var txtRt = txtGo.AddComponent<RectTransform>();
            txtRt.anchorMin = new Vector2(0.05f, 0.4f);
            txtRt.anchorMax = new Vector2(0.95f, 0.9f);
            txtRt.offsetMin = Vector2.zero;
            txtRt.offsetMax = Vector2.zero;
            _confirmText = txtGo.AddComponent<TextMeshProUGUI>();
            _confirmText.fontSize = 22f;
            _confirmText.color = RowText;
            _confirmText.alignment = TextAlignmentOptions.Center;

            // Confirm button
            var confirmBtn = CreateDialogButton(box.transform, "确认",
                new Vector2(0.1f, 0.05f), new Vector2(0.45f, 0.35f));
            confirmBtn.onClick.AddListener(OnConfirmYes);

            // Cancel button
            var cancelBtn = CreateDialogButton(box.transform, "取消",
                new Vector2(0.55f, 0.05f), new Vector2(0.9f, 0.35f));
            cancelBtn.onClick.AddListener(OnConfirmNo);

            _confirmDialog.SetActive(false);
        }

        Button CreateDialogButton(Transform parent, string text, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject("Btn_" + text);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var img = go.AddComponent<Image>();
            img.color = TabActive;

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1, 1, 1, 0.85f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            btn.colors = colors;

            var lblGo = new GameObject("Label");
            lblGo.transform.SetParent(go.transform, false);
            var lrt = lblGo.AddComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero;
            lrt.offsetMax = Vector2.zero;
            var tmp = lblGo.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 22f;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;

            return btn;
        }

        void ShowConfirm(string message, int slot)
        {
            _pendingSlot = slot;
            _confirmText.text = message;
            _confirmDialog.SetActive(true);
        }

        void OnConfirmYes()
        {
            _confirmDialog.SetActive(false);
            if (_pendingSlot < 0) return;

            int slot = _pendingSlot;
            _pendingSlot = -1;

            if (_gsm == null)
                _gsm = ServiceLocator.Get<GameStateManager>();

            if (_gsm == null)
            {
                _statusText.text = "存档系统未就绪";
                return;
            }

            if (_isSaveMode)
            {
                _gsm.State.currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                _gsm.SaveToSlot(slot);
                _statusText.text = $"已保存到存档位 {slot}";
                RefreshSlotList();
            }
            else
            {
                if (_gsm.SlotExists(slot))
                {
                    _gsm.LoadFromSlot(slot);
                    _statusText.text = $"已读取存档位 {slot}，正在加载...";
                    string sceneName = _gsm.State.currentScene;
                    if (string.IsNullOrEmpty(sceneName)) sceneName = "StoryScene";
                    var loader = ServiceLocator.Get<SceneLoader>();
                    if (loader != null)
                        loader.LoadScene(sceneName);
                    else
                        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
                }
                else
                {
                    _statusText.text = $"存档位 {slot} 为空";
                }
            }
        }

        void OnConfirmNo()
        {
            _confirmDialog.SetActive(false);
            _pendingSlot = -1;
        }

        // ================================================================
        // Header / Close / Tabs
        // ================================================================

        void CreateHeaderCell(Transform parent, string text, float width)
        {
            var go = new GameObject("Header_" + text);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(width, 30);
            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth = width;
            le.preferredHeight = 30;

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 18f;
            tmp.color = TabActive;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.margin = new Vector4(8, 0, 4, 0);
        }

        Button CreateCloseButton(Transform parent)
        {
            var go = new GameObject("CloseBtn");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(1, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(1, 1);
            rt.anchoredPosition = new Vector2(-5, -5);
            rt.sizeDelta = new Vector2(36, 36);

            var img = go.AddComponent<Image>();
            img.color = Color.clear;

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;

            var lblGo = new GameObject("X");
            lblGo.transform.SetParent(go.transform, false);
            var lrt = lblGo.AddComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero;
            lrt.offsetMax = Vector2.zero;
            var tmp = lblGo.AddComponent<TextMeshProUGUI>();
            tmp.text = "X";
            tmp.fontSize = 24f;
            tmp.color = RowText;
            tmp.alignment = TextAlignmentOptions.Center;

            return btn;
        }

        void SetMode(bool saveMode)
        {
            _isSaveMode = saveMode;
            if (_titleText != null)
                _titleText.text = saveMode ? "存档记录" : "读取记录";
            HighlightTabs();
            RefreshSlotList();
            if (_statusText != null) _statusText.text = "";
        }

        void HighlightTabs()
        {
            SetTabStyle(_saveTabBtn, _isSaveMode);
            SetTabStyle(_loadTabBtn, !_isSaveMode);
        }

        void SetTabStyle(Button tab, bool active)
        {
            if (tab == null) return;
            var img = tab.GetComponent<Image>();
            if (img != null)
                img.color = active ? TabActive : TabNormal;
        }

        // ================================================================
        // Slot List
        // ================================================================

        void RefreshSlotList()
        {
            ClearSlotRows();
            HighlightTabs();

            if (_slotContainer == null)
            {
                Debug.LogWarning("[CampSystemScreen] _slotContainer is null!");
                return;
            }

            // Re-fetch gsm if null
            if (_gsm == null)
                _gsm = ServiceLocator.Get<GameStateManager>();

            for (int i = 1; i <= SlotCount; i++)
            {
                var info = _gsm?.GetSlotInfo(i);
                bool hasData = info != null && info.exists;

                var row = CreateSlotRow(i, hasData, info);
                int slot = i;
                row.onClick.AddListener(() => OnSlotClicked(slot));
                _slotRows.Add(row);
            }
        }

        Button CreateSlotRow(int slotNum, bool hasData, SaveSlotInfo info)
        {
            var go = new GameObject($"Slot_{slotNum}");
            go.transform.SetParent(_slotContainer, false);

            // Add RectTransform first
            var goRt = go.AddComponent<RectTransform>();
            goRt.sizeDelta = new Vector2(0, 38);

            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 38;
            le.minHeight = 38;

            var img = go.AddComponent<Image>();
            img.color = (slotNum % 2 == 0) ? RowAlt : ParchmentBg;

            var btn = go.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = RowHover;
            colors.pressedColor = new Color(0.92f, 0.88f, 0.78f, 1f);
            colors.selectedColor = Color.white;
            btn.colors = colors;
            btn.targetGraphic = img;

            var hlg = go.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 0;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;
            hlg.childControlWidth = false;
            hlg.childControlHeight = true;

            string no = $"No.{slotNum}";
            string level = hasData ? info.chapterName : "";
            string record = hasData ? "" : "空";
            string time = hasData ? info.timestamp : "";

            CreateRowCell(go.transform, no, 100);
            CreateRowCell(go.transform, level, 130);
            CreateRowCell(go.transform, record, 400);
            CreateRowCell(go.transform, time, 190);

            return btn;
        }

        void CreateRowCell(Transform parent, string text, float width)
        {
            var go = new GameObject("Cell");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(width, 38);

            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth = width;
            le.preferredHeight = 38;

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 18;
            tmp.color = RowText;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.margin = new Vector4(8, 0, 4, 0);
        }

        void OnSlotClicked(int slot)
        {
            if (_gsm == null)
                _gsm = ServiceLocator.Get<GameStateManager>();

            if (_gsm == null)
            {
                _statusText.text = "存档系统未就绪";
                return;
            }

            if (_isSaveMode)
            {
                var existingInfo = _gsm.GetSlotInfo(slot);
                bool hasData = existingInfo != null && existingInfo.exists;
                string msg = hasData
                    ? $"存档位 {slot} 已有数据，确认覆盖？"
                    : $"确认保存到存档位 {slot}？";
                ShowConfirm(msg, slot);
            }
            else
            {
                if (_gsm.SlotExists(slot))
                {
                    ShowConfirm($"确认读取存档位 {slot}？", slot);
                }
                else
                {
                    _statusText.text = $"存档位 {slot} 为空";
                }
            }
        }

        void ClearSlotRows()
        {
            foreach (var row in _slotRows)
                if (row != null) Destroy(row.gameObject);
            _slotRows.Clear();
        }
    }
}
