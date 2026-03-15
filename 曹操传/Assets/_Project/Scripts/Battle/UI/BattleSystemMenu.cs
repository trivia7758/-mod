using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CaoCao.Core;
using CaoCao.Data;

namespace CaoCao.Battle
{
    /// <summary>
    /// Battle system menu: Save / Load / Settings / Resume / Return to Main Menu.
    /// Programmatically built, parchment style matching CampSystemScreen.
    /// </summary>
    public class BattleSystemMenu : MonoBehaviour
    {
        public event Action OnResumed;
        public event Action OnReturnToMainMenu;

        // Parchment colors
        static readonly Color ParchmentBg = new(0.92f, 0.9f, 0.86f, 1f);
        static readonly Color RowText = new(0.3f, 0.2f, 0.1f, 1f);
        static readonly Color RowHover = new(0.95f, 0.92f, 0.85f, 1f);
        static readonly Color RowAlt = new(0.88f, 0.86f, 0.82f, 1f);
        static readonly Color TabActive = new(0.75f, 0.60f, 0.35f, 1f);
        static readonly Color TabNormal = new(0.2f, 0.16f, 0.12f, 1f);
        static readonly Color OverlayBg = new(0f, 0f, 0f, 0.6f);

        // State
        enum MenuPage { Main, Save, Load, Settings }
        MenuPage _page = MenuPage.Main;

        GameStateManager _gsm;
        const int SlotCount = 10;

        // UI refs
        Canvas _canvas;
        GameObject _mainPanel;
        GameObject _slotPanel;
        GameObject _settingsPanel;
        RectTransform _slotContainer;
        TMP_Text _slotTitle;
        TMP_Text _statusText;
        List<Button> _slotRows = new();

        // Confirmation dialog
        GameObject _confirmDialog;
        TMP_Text _confirmText;
        int _pendingSlot = -1;

        // Settings refs
        // SpeedSetting enum: Fast=0, Mid=1, Slow=2
        // UI buttons: [快(0), 中(1), 慢(2)]
        Button[] _msgSpeedBtns = new Button[3];
        Button[] _moveSpeedBtns = new Button[3];
        Button _zhBtn, _enBtn;
        Toggle _bgmToggle, _sfxToggle;
        Toggle _showHpToggle, _longPressToggle, _autoMinimapToggle;
        Toggle _dialogHoldToggle, _statusChangeToggle, _critLineToggle;
        Toggle _autoPlayToggle;

        public bool IsOpen => _canvas != null && _canvas.gameObject.activeSelf;

        public void Build()
        {
            EnsureServices();

            // Root canvas
            var canvasGo = new GameObject("BattleSystemMenuCanvas");
            canvasGo.transform.SetParent(transform, false);
            _canvas = canvasGo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 200;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            // Full-screen dark overlay (blocks clicks)
            var overlay = new GameObject("Overlay");
            overlay.transform.SetParent(canvasGo.transform, false);
            var ort = overlay.AddComponent<RectTransform>();
            ort.anchorMin = Vector2.zero;
            ort.anchorMax = Vector2.one;
            ort.offsetMin = Vector2.zero;
            ort.offsetMax = Vector2.zero;
            var oImg = overlay.AddComponent<Image>();
            oImg.color = OverlayBg;

            BuildMainMenu(canvasGo.transform);
            BuildSlotPanel(canvasGo.transform);
            BuildSettingsPanel(canvasGo.transform);
            BuildConfirmDialog(canvasGo.transform);
            WireToggleCallbacks();

            Hide();
        }

        /// <summary>
        /// Bootstrap essential services if not registered (same as CampManager).
        /// This handles the case where Battle is loaded without going through Boot/GameManager.
        /// </summary>
        void EnsureServices()
        {
            if (!ServiceLocator.Has<SaveSystem>())
            {
                Debug.Log("[BattleSystemMenu] Bootstrapping SaveSystem");
                var ss = new SaveSystem();
                ss.EnsureDataDirectory();
                ServiceLocator.Register(ss);
            }

            if (!ServiceLocator.Has<GameDataRegistry>())
            {
                Debug.Log("[BattleSystemMenu] Bootstrapping GameDataRegistry");
                var reg = Resources.Load<GameDataRegistry>("Data/GameDataRegistry");
                if (reg != null)
                {
                    reg.Initialize();
                    ServiceLocator.Register(reg);
                }
            }

            if (!ServiceLocator.Has<GameStateManager>())
            {
                Debug.Log("[BattleSystemMenu] Bootstrapping GameStateManager");
                var ss = ServiceLocator.Get<SaveSystem>();
                var reg = ServiceLocator.Get<GameDataRegistry>();
                var gsm = new GameStateManager(reg, ss);
                gsm.InitializeNewGame();
                ServiceLocator.Register(gsm);
            }

            _gsm = ServiceLocator.Get<GameStateManager>();
            Debug.Log($"[BattleSystemMenu] GSM ready: {_gsm != null}");
        }

        // ================================================================
        // Main Menu Page
        // ================================================================

        void BuildMainMenu(Transform root)
        {
            _mainPanel = new GameObject("MainPanel");
            _mainPanel.transform.SetParent(root, false);
            var rt = _mainPanel.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.3f, 0.15f);
            rt.anchorMax = new Vector2(0.7f, 0.85f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var img = _mainPanel.AddComponent<Image>();
            img.color = ParchmentBg;
            var outline = _mainPanel.AddComponent<Outline>();
            outline.effectColor = TabActive;
            outline.effectDistance = new Vector2(2, -2);

            var vlg = _mainPanel.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 12;
            vlg.padding = new RectOffset(40, 40, 30, 30);
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childAlignment = TextAnchor.UpperCenter;

            // Title
            CreateMenuLabel(_mainPanel.transform, "系统菜单", 30, FontStyles.Bold);
            CreateSpacer(_mainPanel.transform, 10);

            CreateMenuButton(_mainPanel.transform, "保存", () => ShowPage(MenuPage.Save));
            CreateMenuButton(_mainPanel.transform, "读取", () => ShowPage(MenuPage.Load));
            CreateMenuButton(_mainPanel.transform, "设置", () => ShowPage(MenuPage.Settings));
            CreateSpacer(_mainPanel.transform, 10);

            CreateMenuButton(_mainPanel.transform, "返回战斗", () =>
            {
                Hide();
                OnResumed?.Invoke();
            });

            CreateMenuButton(_mainPanel.transform, "回主菜单", () =>
            {
                Hide();
                OnReturnToMainMenu?.Invoke();
            }, new Color(0.6f, 0.25f, 0.2f, 1f));
        }

        void CreateMenuLabel(Transform parent, string text, float fontSize, FontStyles style = FontStyles.Normal)
        {
            var go = new GameObject("Label_" + text);
            go.transform.SetParent(parent, false);
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = fontSize + 16;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = TabActive;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontStyle = style;
        }

        void CreateSpacer(Transform parent, float height)
        {
            var go = new GameObject("Spacer");
            go.transform.SetParent(parent, false);
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = height;
        }

        void CreateMenuButton(Transform parent, string text, Action onClick, Color? textColor = null)
        {
            var go = new GameObject("Btn_" + text);
            go.transform.SetParent(parent, false);
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 50;

            var img = go.AddComponent<Image>();
            img.color = TabNormal;
            var btnOutline = go.AddComponent<Outline>();
            btnOutline.effectColor = TabActive;
            btnOutline.effectDistance = new Vector2(1, -1);

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.2f, 1.1f, 1f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f);
            btn.colors = colors;
            btn.onClick.AddListener(() => onClick?.Invoke());

            var lblGo = new GameObject("Label");
            lblGo.transform.SetParent(go.transform, false);
            var lrt = lblGo.AddComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero;
            lrt.offsetMax = Vector2.zero;
            var tmp = lblGo.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 24f;
            tmp.color = textColor ?? ThreeKingdomsTheme.TextPrimary;
            tmp.alignment = TextAlignmentOptions.Center;
        }

        // ================================================================
        // Save/Load Slot Panel
        // ================================================================

        void BuildSlotPanel(Transform root)
        {
            _slotPanel = new GameObject("SlotPanel");
            _slotPanel.transform.SetParent(root, false);
            var rt = _slotPanel.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.1f, 0.08f);
            rt.anchorMax = new Vector2(0.9f, 0.92f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var img = _slotPanel.AddComponent<Image>();
            img.color = ParchmentBg;
            var outline = _slotPanel.AddComponent<Outline>();
            outline.effectColor = TabActive;
            outline.effectDistance = new Vector2(2, -2);

            // Title
            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(_slotPanel.transform, false);
            var trt = titleGo.AddComponent<RectTransform>();
            trt.anchorMin = new Vector2(0, 0.92f);
            trt.anchorMax = new Vector2(1, 1);
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;
            _slotTitle = titleGo.AddComponent<TextMeshProUGUI>();
            _slotTitle.text = "存档记录";
            _slotTitle.fontSize = 26f;
            _slotTitle.color = TabActive;
            _slotTitle.alignment = TextAlignmentOptions.Center;
            _slotTitle.fontStyle = FontStyles.Bold;

            // Column headers
            var headerGo = new GameObject("Header");
            headerGo.transform.SetParent(_slotPanel.transform, false);
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
            CreateHeaderCell(headerGo.transform, "章节", 200);
            CreateHeaderCell(headerGo.transform, "场景", 250);
            CreateHeaderCell(headerGo.transform, "时间", 200);

            // Slot list container
            var slotArea = new GameObject("SlotContainer");
            slotArea.transform.SetParent(_slotPanel.transform, false);
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
            _slotContainer = saRt;

            // Status text
            var statusGo = new GameObject("StatusText");
            statusGo.transform.SetParent(_slotPanel.transform, false);
            var srt = statusGo.AddComponent<RectTransform>();
            srt.anchorMin = new Vector2(0, 0);
            srt.anchorMax = new Vector2(0.6f, 0.08f);
            srt.offsetMin = new Vector2(10, 0);
            srt.offsetMax = new Vector2(-10, 0);
            _statusText = statusGo.AddComponent<TextMeshProUGUI>();
            _statusText.fontSize = 18f;
            _statusText.color = TabActive;
            _statusText.alignment = TextAlignmentOptions.Left;

            // Back button
            var backBtn = CreatePanelButton(_slotPanel.transform, "返回",
                new Vector2(0.7f, 0.01f), new Vector2(0.95f, 0.07f));
            backBtn.onClick.AddListener(() => ShowPage(MenuPage.Main));

            _slotPanel.SetActive(false);
        }

        void CreateHeaderCell(Transform parent, string text, float width)
        {
            var go = new GameObject("Header_" + text);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>().sizeDelta = new Vector2(width, 30);
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

        void RefreshSlotList()
        {
            ClearSlotRows();
            if (_gsm == null) _gsm = ServiceLocator.Get<GameStateManager>();

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
            go.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 40);
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 40;
            le.minHeight = 40;

            var img = go.AddComponent<Image>();
            img.color = (slotNum % 2 == 0) ? RowAlt : ParchmentBg;

            var btn = go.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = RowHover;
            colors.pressedColor = new Color(0.92f, 0.88f, 0.78f, 1f);
            btn.colors = colors;
            btn.targetGraphic = img;

            var hlg = go.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 0;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;
            hlg.childControlWidth = false;
            hlg.childControlHeight = true;

            CreateRowCell(go.transform, $"No.{slotNum}", 100);
            CreateRowCell(go.transform, hasData ? info.chapterName : "", 200);
            CreateRowCell(go.transform, hasData ? "" : "空", 250);
            CreateRowCell(go.transform, hasData ? info.timestamp : "", 200);

            return btn;
        }

        void CreateRowCell(Transform parent, string text, float width)
        {
            var go = new GameObject("Cell");
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>().sizeDelta = new Vector2(width, 40);
            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth = width;
            le.preferredHeight = 40;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 18;
            tmp.color = RowText;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.margin = new Vector4(8, 0, 4, 0);
        }

        void OnSlotClicked(int slot)
        {
            if (_gsm == null) _gsm = ServiceLocator.Get<GameStateManager>();
            if (_gsm == null)
            {
                _statusText.text = "存档系统未就绪";
                return;
            }

            if (_page == MenuPage.Save)
            {
                var info = _gsm.GetSlotInfo(slot);
                bool hasData = info != null && info.exists;
                string msg = hasData ? $"存档位 {slot} 已有数据，确认覆盖？" : $"确认保存到存档位 {slot}？";
                ShowConfirm(msg, slot);
            }
            else
            {
                if (_gsm.SlotExists(slot))
                    ShowConfirm($"确认读取存档位 {slot}？", slot);
                else
                    _statusText.text = $"存档位 {slot} 为空";
            }
        }

        void ClearSlotRows()
        {
            foreach (var row in _slotRows)
                if (row != null) Destroy(row.gameObject);
            _slotRows.Clear();
        }

        // ================================================================
        // Settings Panel — matches SettingsDialog (Audio, Display, Speed, Language)
        // ================================================================

        void BuildSettingsPanel(Transform root)
        {
            _settingsPanel = new GameObject("SettingsPanel");
            _settingsPanel.transform.SetParent(root, false);
            var rt = _settingsPanel.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.1f, 0.03f);
            rt.anchorMax = new Vector2(0.9f, 0.97f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var img = _settingsPanel.AddComponent<Image>();
            img.color = ParchmentBg;
            var outline = _settingsPanel.AddComponent<Outline>();
            outline.effectColor = TabActive;
            outline.effectDistance = new Vector2(2, -2);

            // Title
            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(_settingsPanel.transform, false);
            var trt = titleGo.AddComponent<RectTransform>();
            trt.anchorMin = new Vector2(0, 0.94f);
            trt.anchorMax = new Vector2(1, 1);
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;
            var title = titleGo.AddComponent<TextMeshProUGUI>();
            title.text = "设置";
            title.fontSize = 28f;
            title.color = TabActive;
            title.alignment = TextAlignmentOptions.Center;
            title.fontStyle = FontStyles.Bold;

            // Scrollable content area — standard Unity ScrollRect structure:
            // ScrollView (ScrollRect + Image for drag) → Viewport (RectMask2D) → Content
            var scrollView = new GameObject("ScrollView");
            scrollView.transform.SetParent(_settingsPanel.transform, false);
            var svRt = scrollView.AddComponent<RectTransform>();
            svRt.anchorMin = new Vector2(0.02f, 0.09f);
            svRt.anchorMax = new Vector2(0.98f, 0.93f);
            svRt.offsetMin = Vector2.zero;
            svRt.offsetMax = Vector2.zero;
            var svImg = scrollView.AddComponent<Image>();
            svImg.color = Color.clear;

            var viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollView.transform, false);
            var vpRt = viewport.AddComponent<RectTransform>();
            vpRt.anchorMin = Vector2.zero;
            vpRt.anchorMax = Vector2.one;
            vpRt.offsetMin = Vector2.zero;
            vpRt.offsetMax = Vector2.zero;
            vpRt.pivot = new Vector2(0, 1);
            var vpImg = viewport.AddComponent<Image>();
            vpImg.color = Color.clear;
            vpImg.raycastTarget = false;   // 不拦截拖拽，让事件穿透到 ScrollView 的 ScrollRect
            viewport.AddComponent<RectMask2D>();

            var content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            var crt = content.AddComponent<RectTransform>();
            crt.anchorMin = new Vector2(0, 1);
            crt.anchorMax = new Vector2(1, 1);
            crt.pivot = new Vector2(0.5f, 1);
            crt.offsetMin = Vector2.zero;
            crt.offsetMax = Vector2.zero;
            var csf = content.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var cvlg = content.AddComponent<VerticalLayoutGroup>();
            cvlg.spacing = 6;
            cvlg.padding = new RectOffset(16, 16, 8, 8);
            cvlg.childForceExpandWidth = true;
            cvlg.childForceExpandHeight = false;
            cvlg.childAlignment = TextAnchor.UpperCenter;

            var scroll = scrollView.AddComponent<ScrollRect>();
            scroll.content = crt;
            scroll.viewport = vpRt;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30f;

            // === 音频设置 (Audio) ===
            CreateSectionLabel(content.transform, "音频设置");
            _bgmToggle = BuildToggleRow(content.transform, "背景音乐");
            _sfxToggle = BuildToggleRow(content.transform, "游戏音效");

            // === 显示设置 (Display) — 6 toggles, single column ===
            CreateSectionLabel(content.transform, "显示设置");
            _showHpToggle = BuildToggleRow(content.transform, "战斗时显示血条");
            _longPressToggle = BuildToggleRow(content.transform, "长按可使剧情加快");
            _autoMinimapToggle = BuildToggleRow(content.transform, "自动显示战场缩小图");
            _dialogHoldToggle = BuildToggleRow(content.transform, "会话窗口不自动关闭");
            _statusChangeToggle = BuildToggleRow(content.transform, "显示状态条变更");
            _critLineToggle = BuildToggleRow(content.transform, "开启暴击台词");

            // === 速度设置 (Speed) ===
            CreateSectionLabel(content.transform, "速度设置");
            BuildSpeedRow(content.transform, "信息显示速度", new[] { "快", "中", "慢" }, _msgSpeedBtns, OnMsgSpeedChanged);
            BuildSpeedRow(content.transform, "武将移动速度", new[] { "快", "中", "慢" }, _moveSpeedBtns, OnMoveSpeedChanged);

            // === 语言设置 (Language) ===
            CreateSectionLabel(content.transform, "语言设置");
            _autoPlayToggle = BuildToggleRow(content.transform, "剧情自动播放");
            BuildLanguageRow(content.transform);

            // 强制重建布局，让 ScrollRect 知道内容实际高度
            LayoutRebuilder.ForceRebuildLayoutImmediate(crt);

            // Back button
            var backBtn = CreatePanelButton(_settingsPanel.transform, "返回",
                new Vector2(0.35f, 0.02f), new Vector2(0.65f, 0.08f));
            backBtn.onClick.AddListener(() => ShowPage(MenuPage.Main));

            _settingsPanel.SetActive(false);
        }

        /// <summary>Create a single-row toggle with label.</summary>
        Toggle BuildToggleRow(Transform parent, string label)
        {
            var row = new GameObject("Toggle_" + label);
            row.transform.SetParent(parent, false);
            var le = row.AddComponent<LayoutElement>();
            le.preferredHeight = 38;
            var hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 10;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.padding = new RectOffset(10, 0, 0, 0);

            var toggle = BuildToggleWidget(row.transform);

            var lblGo = new GameObject("Label");
            lblGo.transform.SetParent(row.transform, false);
            var lle = lblGo.AddComponent<LayoutElement>();
            lle.preferredWidth = 400;
            var tmp = lblGo.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 20f;
            tmp.color = RowText;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;

            return toggle;
        }

        /// <summary>Create the actual Toggle UI widget (checkbox box + checkmark).</summary>
        Toggle BuildToggleWidget(Transform parent)
        {
            var toggleGo = new GameObject("ToggleWidget");
            toggleGo.transform.SetParent(parent, false);
            var tle = toggleGo.AddComponent<LayoutElement>();
            tle.preferredWidth = 30;
            tle.preferredHeight = 30;

            // Background box
            var bgGo = new GameObject("Background");
            bgGo.transform.SetParent(toggleGo.transform, false);
            var bgRt = bgGo.AddComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.color = new Color(0.25f, 0.2f, 0.15f, 1f);
            var bgOutline = bgGo.AddComponent<Outline>();
            bgOutline.effectColor = TabActive;
            bgOutline.effectDistance = new Vector2(1, -1);

            // Checkmark
            var checkGo = new GameObject("Checkmark");
            checkGo.transform.SetParent(bgGo.transform, false);
            var checkRt = checkGo.AddComponent<RectTransform>();
            checkRt.anchorMin = new Vector2(0.15f, 0.15f);
            checkRt.anchorMax = new Vector2(0.85f, 0.85f);
            checkRt.offsetMin = Vector2.zero;
            checkRt.offsetMax = Vector2.zero;
            var checkImg = checkGo.AddComponent<Image>();
            checkImg.color = ThreeKingdomsTheme.CheckboxActive;

            var toggle = toggleGo.AddComponent<Toggle>();
            toggle.targetGraphic = bgImg;
            toggle.graphic = checkImg;
            toggle.isOn = true;

            return toggle;
        }

        void CreateSectionLabel(Transform parent, string text)
        {
            var go = new GameObject("Section_" + text);
            go.transform.SetParent(parent, false);
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 32;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = $"── {text} ──";
            tmp.fontSize = 20f;
            tmp.color = TabActive;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
        }

        void BuildSpeedRow(Transform parent, string label, string[] options, Button[] btnArray, Action<int> callback)
        {
            var row = new GameObject("Row_" + label);
            row.transform.SetParent(parent, false);
            var le = row.AddComponent<LayoutElement>();
            le.preferredHeight = 44;
            var hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 10;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;
            hlg.childAlignment = TextAnchor.MiddleLeft;

            var lblGo = new GameObject("Label");
            lblGo.transform.SetParent(row.transform, false);
            var lle = lblGo.AddComponent<LayoutElement>();
            lle.preferredWidth = 180;
            var tmp = lblGo.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 20f;
            tmp.color = RowText;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;

            for (int i = 0; i < options.Length; i++)
            {
                int idx = i;
                var btnGo = new GameObject("Btn_" + options[i]);
                btnGo.transform.SetParent(row.transform, false);
                var ble = btnGo.AddComponent<LayoutElement>();
                ble.preferredWidth = 80;
                var bImg = btnGo.AddComponent<Image>();
                bImg.color = ThreeKingdomsTheme.SpeedInactive;
                var btn = btnGo.AddComponent<Button>();
                btn.targetGraphic = bImg;
                btn.onClick.AddListener(() => callback?.Invoke(idx));
                btnArray[i] = btn;

                var btextGo = new GameObject("Text");
                btextGo.transform.SetParent(btnGo.transform, false);
                var btrt = btextGo.AddComponent<RectTransform>();
                btrt.anchorMin = Vector2.zero;
                btrt.anchorMax = Vector2.one;
                btrt.offsetMin = Vector2.zero;
                btrt.offsetMax = Vector2.zero;
                var btmp = btextGo.AddComponent<TextMeshProUGUI>();
                btmp.text = options[i];
                btmp.fontSize = 20f;
                btmp.color = ThreeKingdomsTheme.TextPrimary;
                btmp.alignment = TextAlignmentOptions.Center;
            }
        }

        void BuildLanguageRow(Transform parent)
        {
            var row = new GameObject("Row_Language");
            row.transform.SetParent(parent, false);
            var le = row.AddComponent<LayoutElement>();
            le.preferredHeight = 44;
            var hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 10;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;
            hlg.childAlignment = TextAnchor.MiddleLeft;

            var lblGo = new GameObject("Label");
            lblGo.transform.SetParent(row.transform, false);
            var lle = lblGo.AddComponent<LayoutElement>();
            lle.preferredWidth = 160;
            var tmp = lblGo.AddComponent<TextMeshProUGUI>();
            tmp.text = "语言";
            tmp.fontSize = 20f;
            tmp.color = RowText;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;

            _zhBtn = CreateSpeedStyleButton(row.transform, "中文", 80);
            _enBtn = CreateSpeedStyleButton(row.transform, "English", 100);

            _zhBtn.onClick.AddListener(() =>
            {
                GameManager.Instance?.SetLanguage("zh");
                HighlightLangBtns(true);
            });
            _enBtn.onClick.AddListener(() =>
            {
                GameManager.Instance?.SetLanguage("en");
                HighlightLangBtns(false);
            });
        }

        void WireToggleCallbacks()
        {
            // AutoPlay
            if (_autoPlayToggle != null)
                _autoPlayToggle.onValueChanged.AddListener(v =>
                {
                    if (GameManager.Instance?.Settings != null)
                        GameManager.Instance.Settings.autoPlay = v;
                });

            // Audio (placeholder — no persistent field yet, just log)
            if (_bgmToggle != null)
                _bgmToggle.onValueChanged.AddListener(v =>
                    Debug.Log($"[Settings] BGM = {v}"));
            if (_sfxToggle != null)
                _sfxToggle.onValueChanged.AddListener(v =>
                    Debug.Log($"[Settings] SFX = {v}"));

            // Display (placeholder — no persistent field yet)
            if (_showHpToggle != null)
                _showHpToggle.onValueChanged.AddListener(v =>
                    Debug.Log($"[Settings] ShowHP = {v}"));
            if (_longPressToggle != null)
                _longPressToggle.onValueChanged.AddListener(v =>
                    Debug.Log($"[Settings] LongPress = {v}"));
            if (_autoMinimapToggle != null)
                _autoMinimapToggle.onValueChanged.AddListener(v =>
                    Debug.Log($"[Settings] AutoMinimap = {v}"));
            if (_dialogHoldToggle != null)
                _dialogHoldToggle.onValueChanged.AddListener(v =>
                    Debug.Log($"[Settings] DialogHold = {v}"));
            if (_statusChangeToggle != null)
                _statusChangeToggle.onValueChanged.AddListener(v =>
                    Debug.Log($"[Settings] StatusChange = {v}"));
            if (_critLineToggle != null)
                _critLineToggle.onValueChanged.AddListener(v =>
                    Debug.Log($"[Settings] CritLine = {v}"));
        }

        Button CreateSpeedStyleButton(Transform parent, string text, float width)
        {
            var go = new GameObject("Btn_" + text);
            go.transform.SetParent(parent, false);
            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth = width;
            var bImg = go.AddComponent<Image>();
            bImg.color = ThreeKingdomsTheme.SpeedInactive;
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = bImg;

            var tGo = new GameObject("Text");
            tGo.transform.SetParent(go.transform, false);
            var trt = tGo.AddComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;
            var btmp = tGo.AddComponent<TextMeshProUGUI>();
            btmp.text = text;
            btmp.fontSize = 20f;
            btmp.color = ThreeKingdomsTheme.TextPrimary;
            btmp.alignment = TextAlignmentOptions.Center;

            return btn;
        }

        void HighlightLangBtns(bool isZh)
        {
            if (_zhBtn != null)
            {
                var img = _zhBtn.GetComponent<Image>();
                if (img != null) img.color = isZh ? ThreeKingdomsTheme.SpeedActive : ThreeKingdomsTheme.SpeedInactive;
            }
            if (_enBtn != null)
            {
                var img = _enBtn.GetComponent<Image>();
                if (img != null) img.color = !isZh ? ThreeKingdomsTheme.SpeedActive : ThreeKingdomsTheme.SpeedInactive;
            }
        }

        void OnMsgSpeedChanged(int idx)
        {
            HighlightSpeedBtns(_msgSpeedBtns, idx);
            if (GameManager.Instance?.Settings != null)
                GameManager.Instance.Settings.messageSpeed = (SpeedSetting)idx;
        }

        void OnMoveSpeedChanged(int idx)
        {
            HighlightSpeedBtns(_moveSpeedBtns, idx);
            if (GameManager.Instance?.Settings != null)
                GameManager.Instance.Settings.moveSpeed = (SpeedSetting)idx;
        }

        void HighlightSpeedBtns(Button[] btns, int activeIdx)
        {
            for (int i = 0; i < btns.Length; i++)
            {
                if (btns[i] == null) continue;
                var img = btns[i].GetComponent<Image>();
                if (img != null) img.color = i == activeIdx
                    ? ThreeKingdomsTheme.SpeedActive
                    : ThreeKingdomsTheme.SpeedInactive;
            }
        }

        void SyncSettingsUI()
        {
            var settings = GameManager.Instance?.Settings;
            if (settings != null)
            {
                HighlightSpeedBtns(_msgSpeedBtns, (int)settings.messageSpeed);
                HighlightSpeedBtns(_moveSpeedBtns, (int)settings.moveSpeed);

                // Sync autoplay toggle
                if (_autoPlayToggle != null)
                    _autoPlayToggle.SetIsOnWithoutNotify(settings.autoPlay);
            }

            // Audio toggles default to on (no persistent storage yet)
            if (_bgmToggle != null) _bgmToggle.SetIsOnWithoutNotify(true);
            if (_sfxToggle != null) _sfxToggle.SetIsOnWithoutNotify(true);

            // Display toggles default to on
            if (_showHpToggle != null) _showHpToggle.SetIsOnWithoutNotify(true);
            if (_longPressToggle != null) _longPressToggle.SetIsOnWithoutNotify(true);
            if (_autoMinimapToggle != null) _autoMinimapToggle.SetIsOnWithoutNotify(true);
            if (_dialogHoldToggle != null) _dialogHoldToggle.SetIsOnWithoutNotify(false);
            if (_statusChangeToggle != null) _statusChangeToggle.SetIsOnWithoutNotify(true);
            if (_critLineToggle != null) _critLineToggle.SetIsOnWithoutNotify(true);

            bool isZh = GameManager.Instance?.CurrentLanguage != "en";
            HighlightLangBtns(isZh);
        }

        // ================================================================
        // Confirmation Dialog
        // ================================================================

        void BuildConfirmDialog(Transform root)
        {
            _confirmDialog = new GameObject("ConfirmDialog");
            _confirmDialog.transform.SetParent(root, false);
            var drt = _confirmDialog.AddComponent<RectTransform>();
            drt.anchorMin = Vector2.zero;
            drt.anchorMax = Vector2.one;
            drt.offsetMin = Vector2.zero;
            drt.offsetMax = Vector2.zero;
            var overlay = _confirmDialog.AddComponent<Image>();
            overlay.color = new Color(0, 0, 0, 0.5f);

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

            var confirmBtn = CreatePanelButton(box.transform, "确认",
                new Vector2(0.1f, 0.05f), new Vector2(0.45f, 0.35f));
            confirmBtn.onClick.AddListener(OnConfirmYes);

            var cancelBtn = CreatePanelButton(box.transform, "取消",
                new Vector2(0.55f, 0.05f), new Vector2(0.9f, 0.35f));
            cancelBtn.onClick.AddListener(OnConfirmNo);

            _confirmDialog.SetActive(false);
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

            if (_gsm == null) _gsm = ServiceLocator.Get<GameStateManager>();
            if (_gsm == null)
            {
                _statusText.text = "存档系统未就绪";
                return;
            }

            if (_page == MenuPage.Save)
            {
                _gsm.State.currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                _gsm.SaveToSlot(slot);
                _statusText.text = $"已保存到存档位 {slot}";
                RefreshSlotList();
            }
            else if (_page == MenuPage.Load)
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
        // Shared helpers
        // ================================================================

        Button CreatePanelButton(Transform parent, string text, Vector2 anchorMin, Vector2 anchorMax)
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
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f);
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

        // ================================================================
        // Page Navigation
        // ================================================================

        void ShowPage(MenuPage page)
        {
            _page = page;
            _mainPanel.SetActive(page == MenuPage.Main);
            _slotPanel.SetActive(page == MenuPage.Save || page == MenuPage.Load);
            _settingsPanel.SetActive(page == MenuPage.Settings);
            _confirmDialog.SetActive(false);

            if (page == MenuPage.Save || page == MenuPage.Load)
            {
                _slotTitle.text = page == MenuPage.Save ? "存档记录" : "读取记录";
                if (_statusText != null) _statusText.text = "";
                RefreshSlotList();
            }
            else if (page == MenuPage.Settings)
            {
                SyncSettingsUI();
            }
        }

        // ================================================================
        // Show / Hide
        // ================================================================

        public void Show()
        {
            if (_canvas == null) return;
            EnsureServices(); // Re-check in case services were registered after Build()
            _canvas.gameObject.SetActive(true);
            ShowPage(MenuPage.Main);
        }

        public void Hide()
        {
            if (_canvas == null) return;
            _canvas.gameObject.SetActive(false);
        }

        public void Toggle()
        {
            if (IsOpen) Hide();
            else Show();
        }
    }
}
