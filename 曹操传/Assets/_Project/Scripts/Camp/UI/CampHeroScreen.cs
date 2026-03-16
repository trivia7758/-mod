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
    /// Hero info screen (武将). Reference-style layout:
    /// Left: portrait + name/level/class + equipment list (clipped)
    /// Right: tabs (能力/兵种/策略/特技) + stat bars + six dimensions
    /// Navigation arrows on sides, page indicator at bottom.
    /// </summary>
    public class CampHeroScreen : MonoBehaviour
    {
        public event Action OnBack;

        GameStateManager _gsm;
        GameDataRegistry _registry;

        // Navigation
        int _heroIndex;
        List<HeroRuntimeData> _heroList = new();

        // Left panel references
        Image _portraitImage;
        TMP_Text _nameLabel;
        TMP_Text _levelLabel;
        TMP_Text _classLabel;
        Transform _equipListParent;
        List<GameObject> _equipSlotGos = new();

        // Right panel references
        Transform _tabContentParent;
        Button[] _tabButtons = new Button[4];
        int _selectedTab; // 0=能力 1=兵种 2=策略 3=特技

        // Bottom
        TMP_Text _pageLabel;

        // Ability tab references (stored for refresh without rebuild)
        List<BarRef> _abilityBars = new();    // HP/MP/Exp + 攻击/谋略/防御/灵敏/士气 = 8 bars
        List<TMP_Text> _leftStatTexts = new(); // 武力/智力/统帅/敏捷/气运/破敌 = 6 text labels
        TMP_Text _movText;                     // 移动 (no bar)

        struct BarRef
        {
            public RectTransform fillRt;
            public TMP_Text valueText;
        }

        // Colors
        static readonly Color GoldTitle = new(0.75f, 0.6f, 0.3f, 1f);
        static readonly Color BarBg = new(0.15f, 0.12f, 0.08f, 0.8f);
        static readonly Color HpColor = new(0.3f, 0.75f, 0.3f, 1f);
        static readonly Color MpColor = new(0.3f, 0.5f, 0.85f, 1f);
        static readonly Color ExpColor = new(0.6f, 0.3f, 0.7f, 1f);
        static readonly Color AtkColor = new(0.8f, 0.45f, 0.2f, 1f);
        static readonly Color DefColor = new(0.5f, 0.65f, 0.8f, 1f);
        static readonly Color SpdColor = new(0.7f, 0.7f, 0.3f, 1f);
        static readonly Color SectionHeaderBg = new(0.55f, 0.42f, 0.22f, 0.9f);

        public void Build(GameStateManager gsm, GameDataRegistry registry)
        {
            _gsm = gsm;
            _registry = registry;
            BuildLayout();
        }

        public void Refresh()
        {
            _heroList.Clear();
            var heroes = _gsm?.GetRecruitedHeroes();
            if (heroes != null)
                _heroList.AddRange(heroes);

            if (_heroIndex >= _heroList.Count)
                _heroIndex = 0;

            RefreshAll();
        }

        // ============================================================
        // Layout
        // ============================================================

        void BuildLayout()
        {
            // ── Title bar ──
            CreateSectionHeader(transform, "武 将",
                new Vector2(0.15f, 0.93f), new Vector2(0.85f, 1f));

            // ── Close / Back button (top-right) ──
            var backBtn = CampUIHelper.CreateAnchoredButton(transform, "X",
                new Vector2(0.92f, 0.93f), new Vector2(0.99f, 1f),
                Vector2.zero, Vector2.zero, 20f);
            backBtn.onClick.AddListener(() => OnBack?.Invoke());

            // ── Left navigation arrow ──
            var prevBtn = CampUIHelper.CreateAnchoredButton(transform, "<",
                new Vector2(0f, 0.35f), new Vector2(0.035f, 0.55f),
                Vector2.zero, Vector2.zero, 24f);
            prevBtn.onClick.AddListener(OnPrevHero);

            // ── Right navigation arrow ──
            var nextBtn = CampUIHelper.CreateAnchoredButton(transform, ">",
                new Vector2(0.965f, 0.35f), new Vector2(1f, 0.55f),
                Vector2.zero, Vector2.zero, 24f);
            nextBtn.onClick.AddListener(OnNextHero);

            // ── LEFT PANEL ──
            BuildLeftPanel();

            // ── RIGHT PANEL ──
            BuildRightPanel();

            // ── Page indicator ──
            _pageLabel = CampUIHelper.CreateAnchoredLabel(transform, "1/1", 18f,
                new Vector2(0.4f, 0f), new Vector2(0.6f, 0.06f),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.Center);
        }

        void BuildLeftPanel()
        {
            var leftPanel = CampUIHelper.CreateSubPanel(transform, "LeftPanel",
                new Vector2(0.04f, 0.06f), new Vector2(0.46f, 0.92f),
                Vector2.zero, Vector2.zero);

            // Portrait with border (top-left, reduced height)
            var portraitBorder = CampUIHelper.CreateSubPanel(leftPanel, "PortraitBorder",
                new Vector2(0.03f, 0.6f), new Vector2(0.42f, 0.97f),
                Vector2.zero, Vector2.zero);
            var borderOutline = portraitBorder.GetComponent<Outline>();
            if (borderOutline != null) borderOutline.effectColor = new Color(0.6f, 0.15f, 0.15f, 1f);
            var borderImg = portraitBorder.GetComponent<Image>();
            if (borderImg != null) borderImg.color = new Color(0.1f, 0.08f, 0.06f, 1f);

            _portraitImage = CampUIHelper.CreateAnchoredImage(portraitBorder, "Portrait", null,
                new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.96f),
                Vector2.zero, Vector2.zero);

            // Name (right of portrait, gold)
            _nameLabel = CampUIHelper.CreateAnchoredLabel(leftPanel, "", 22f,
                new Vector2(0.45f, 0.87f), new Vector2(0.98f, 0.97f),
                new Vector2(5, 0), Vector2.zero,
                TextAlignmentOptions.MidlineLeft);
            _nameLabel.color = GoldTitle;
            _nameLabel.fontStyle = FontStyles.Bold | FontStyles.Underline;

            // Level
            _levelLabel = CampUIHelper.CreateAnchoredLabel(leftPanel, "", 15f,
                new Vector2(0.45f, 0.78f), new Vector2(0.98f, 0.87f),
                new Vector2(5, 0), Vector2.zero,
                TextAlignmentOptions.MidlineLeft);

            // Class
            _classLabel = CampUIHelper.CreateAnchoredLabel(leftPanel, "", 15f,
                new Vector2(0.45f, 0.69f), new Vector2(0.98f, 0.78f),
                new Vector2(5, 0), Vector2.zero,
                TextAlignmentOptions.MidlineLeft);

            // ── Equipment section ──
            CreateSectionHeader(leftPanel, "装 备",
                new Vector2(0.03f, 0.54f), new Vector2(0.97f, 0.61f));

            // Equipment list with mask to prevent overflow
            var equipMask = new GameObject("EquipMask");
            equipMask.transform.SetParent(leftPanel, false);
            var emrt = equipMask.AddComponent<RectTransform>();
            emrt.anchorMin = new Vector2(0.03f, 0.02f);
            emrt.anchorMax = new Vector2(0.97f, 0.54f);
            emrt.offsetMin = Vector2.zero;
            emrt.offsetMax = Vector2.zero;
            var mask = equipMask.AddComponent<RectMask2D>();

            var equipArea = new GameObject("EquipArea");
            equipArea.transform.SetParent(equipMask.transform, false);
            var eart = equipArea.AddComponent<RectTransform>();
            eart.anchorMin = Vector2.zero;
            eart.anchorMax = Vector2.one;
            eart.offsetMin = Vector2.zero;
            eart.offsetMax = Vector2.zero;

            var vlg = equipArea.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 2;
            vlg.padding = new RectOffset(2, 2, 2, 2);
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;

            _equipListParent = equipArea.transform;
        }

        void BuildRightPanel()
        {
            var rightPanel = CampUIHelper.CreateSubPanel(transform, "RightPanel",
                new Vector2(0.48f, 0.06f), new Vector2(0.96f, 0.92f),
                Vector2.zero, Vector2.zero);

            // ── Tab bar ──
            string[] tabNames = { "能力", "兵种", "策略", "特技" };
            for (int i = 0; i < 4; i++)
            {
                float x0 = 0.02f + i * 0.24f;
                float x1 = x0 + 0.22f;
                var tabBtn = CampUIHelper.CreateAnchoredButton(rightPanel, tabNames[i],
                    new Vector2(x0, 0.92f), new Vector2(x1, 1f),
                    Vector2.zero, Vector2.zero, 17f);
                int tabIdx = i;
                tabBtn.onClick.AddListener(() => SelectTab(tabIdx));
                _tabButtons[i] = tabBtn;
            }

            // ── Tab content area ──
            var contentPanel = new GameObject("TabContent");
            contentPanel.transform.SetParent(rightPanel, false);
            var crt = contentPanel.AddComponent<RectTransform>();
            crt.anchorMin = new Vector2(0.02f, 0.02f);
            crt.anchorMax = new Vector2(0.98f, 0.91f);
            crt.offsetMin = Vector2.zero;
            crt.offsetMax = Vector2.zero;
            _tabContentParent = contentPanel.transform;

            BuildAbilityTab();
        }

        // ============================================================
        // 能力 Tab — HP/MP/Exp bars + six dimensions text + combat bars
        // ============================================================

        void BuildAbilityTab()
        {
            foreach (Transform child in _tabContentParent)
                Destroy(child.gameObject);
            _abilityBars.Clear();
            _leftStatTexts.Clear();
            _movText = null;

            // ── 基本状态 ──
            CreateSectionHeader(_tabContentParent, "基本状态",
                new Vector2(0.02f, 0.85f), new Vector2(0.98f, 0.93f));

            // HP, MP, Exp bars (bar indices 0, 1, 2)
            CreateColorBar(_tabContentParent, "HP", HpColor,
                new Vector2(0.02f, 0.74f), new Vector2(0.98f, 0.84f));
            CreateColorBar(_tabContentParent, "MP", MpColor,
                new Vector2(0.02f, 0.63f), new Vector2(0.98f, 0.73f));
            CreateColorBar(_tabContentParent, "Exp", ExpColor,
                new Vector2(0.02f, 0.52f), new Vector2(0.98f, 0.62f));

            // ── 基本能力 ──
            CreateSectionHeader(_tabContentParent, "基本能力",
                new Vector2(0.02f, 0.42f), new Vector2(0.98f, 0.5f));

            // Layout: Left column = 五维 (text only), Right column = combat stats (bars)
            float startY = 0.35f;
            float rowH = 0.06f;

            // ── Left column: 五维 text labels + growth summary (no bars) ──
            string[] leftNames = { "武力", "智力", "统帅", "敏捷", "气运", "成长" };
            for (int i = 0; i < 6; i++)
            {
                float y = startY - i * rowH;
                var lbl = CampUIHelper.CreateAnchoredLabel(_tabContentParent, "", 14f,
                    new Vector2(0.02f, y), new Vector2(0.46f, y + rowH),
                    new Vector2(8, 0), Vector2.zero,
                    TextAlignmentOptions.MidlineLeft);
                lbl.color = ThreeKingdomsTheme.TextPrimary;
                _leftStatTexts.Add(lbl);
            }

            // ── Right column: combat stat bars ──
            // 攻击, 谋略, 防御, 灵敏, 士气 (with bars, bar indices 3-7)
            string[] rightBarNames = { "攻击", "谋略", "防御", "灵敏", "士气" };
            Color[] rightBarColors = { AtkColor, MpColor, DefColor, SpdColor, ExpColor };
            for (int i = 0; i < 5; i++)
            {
                float y = startY - i * rowH;
                CreateStatBar(_tabContentParent, rightBarNames[i], rightBarColors[i],
                    new Vector2(0.5f, y), new Vector2(0.98f, y + rowH));
            }

            // 移动 (text only, no bar)
            float movY = startY - 5 * rowH;
            _movText = CampUIHelper.CreateAnchoredLabel(_tabContentParent, "", 14f,
                new Vector2(0.5f, movY), new Vector2(0.98f, movY + rowH),
                new Vector2(8, 0), Vector2.zero,
                TextAlignmentOptions.MidlineLeft);
            _movText.color = ThreeKingdomsTheme.TextPrimary;
        }

        /// <summary>
        /// Creates a full-width color bar with label on left (for HP/MP/Exp).
        /// Adds a BarRef to _abilityBars.
        /// </summary>
        void CreateColorBar(Transform parent, string label, Color fillColor,
            Vector2 anchorMin, Vector2 anchorMax)
        {
            // Label (left side)
            CampUIHelper.CreateAnchoredLabel(parent, label, 16f,
                new Vector2(anchorMin.x, anchorMin.y),
                new Vector2(anchorMin.x + 0.12f, anchorMax.y),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.Center);

            // Bar background — use pixel offsets for Y padding
            var barBgGo = new GameObject("BarBg_" + label);
            barBgGo.transform.SetParent(parent, false);
            var bgRt = barBgGo.AddComponent<RectTransform>();
            bgRt.anchorMin = new Vector2(anchorMin.x + 0.14f, anchorMin.y);
            bgRt.anchorMax = new Vector2(anchorMax.x - 0.02f, anchorMax.y);
            bgRt.offsetMin = new Vector2(0, 3);
            bgRt.offsetMax = new Vector2(0, -3);
            barBgGo.AddComponent<Image>().color = BarBg;
            var bgOutline = barBgGo.AddComponent<Outline>();
            bgOutline.effectColor = new Color(0.4f, 0.3f, 0.2f, 0.6f);
            bgOutline.effectDistance = new Vector2(1, -1);

            // Bar fill
            var fillGo = new GameObject("Fill_" + label);
            fillGo.transform.SetParent(barBgGo.transform, false);
            var frt = fillGo.AddComponent<RectTransform>();
            frt.anchorMin = Vector2.zero;
            frt.anchorMax = Vector2.one;
            frt.offsetMin = new Vector2(1, 1);
            frt.offsetMax = new Vector2(-1, -1);
            fillGo.AddComponent<Image>().color = fillColor;

            // Value text
            var valGo = new GameObject("Val_" + label);
            valGo.transform.SetParent(barBgGo.transform, false);
            var vrt = valGo.AddComponent<RectTransform>();
            vrt.anchorMin = Vector2.zero;
            vrt.anchorMax = Vector2.one;
            vrt.offsetMin = Vector2.zero;
            vrt.offsetMax = Vector2.zero;
            var valTmp = valGo.AddComponent<TextMeshProUGUI>();
            valTmp.fontSize = 14f;
            valTmp.color = Color.white;
            valTmp.alignment = TextAlignmentOptions.Center;

            _abilityBars.Add(new BarRef { fillRt = frt, valueText = valTmp });
        }

        /// <summary>
        /// Creates a stat bar in the right column (label + bar with fill).
        /// Adds a BarRef to _abilityBars.
        /// </summary>
        void CreateStatBar(Transform parent, string label, Color fillColor,
            Vector2 anchorMin, Vector2 anchorMax)
        {
            // Label on left side of the right column
            CampUIHelper.CreateAnchoredLabel(parent, label, 14f,
                new Vector2(anchorMin.x, anchorMin.y),
                new Vector2(anchorMin.x + 0.12f, anchorMax.y),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.MidlineRight);

            // Bar background
            var barBgGo = new GameObject("BarBg_" + label);
            barBgGo.transform.SetParent(parent, false);
            var bgRt = barBgGo.AddComponent<RectTransform>();
            bgRt.anchorMin = new Vector2(anchorMin.x + 0.14f, anchorMin.y);
            bgRt.anchorMax = new Vector2(anchorMax.x, anchorMax.y);
            bgRt.offsetMin = new Vector2(0, 2);
            bgRt.offsetMax = new Vector2(0, -2);
            barBgGo.AddComponent<Image>().color = BarBg;
            var bgOutline = barBgGo.AddComponent<Outline>();
            bgOutline.effectColor = new Color(0.4f, 0.3f, 0.2f, 0.5f);
            bgOutline.effectDistance = new Vector2(1, -1);

            // Bar fill
            var fillGo = new GameObject("Fill_" + label);
            fillGo.transform.SetParent(barBgGo.transform, false);
            var frt = fillGo.AddComponent<RectTransform>();
            frt.anchorMin = Vector2.zero;
            frt.anchorMax = Vector2.one;
            frt.offsetMin = new Vector2(1, 1);
            frt.offsetMax = new Vector2(-1, -1);
            fillGo.AddComponent<Image>().color = fillColor;

            // Value text
            var valGo = new GameObject("Val_" + label);
            valGo.transform.SetParent(barBgGo.transform, false);
            var vrt = valGo.AddComponent<RectTransform>();
            vrt.anchorMin = Vector2.zero;
            vrt.anchorMax = Vector2.one;
            vrt.offsetMin = Vector2.zero;
            vrt.offsetMax = Vector2.zero;
            var valTmp = valGo.AddComponent<TextMeshProUGUI>();
            valTmp.fontSize = 13f;
            valTmp.color = Color.white;
            valTmp.alignment = TextAlignmentOptions.Center;

            _abilityBars.Add(new BarRef { fillRt = frt, valueText = valTmp });
        }

        RectTransform CreateSectionHeader(Transform parent, string text,
            Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject("Header_" + text);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var img = go.AddComponent<Image>();
            img.color = SectionHeaderBg;

            // Text child (Image + TMP can't coexist on same GO)
            var textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            var trt = textGo.AddComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;

            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 16f;
            tmp.color = ThreeKingdomsTheme.TextPrimary;
            tmp.alignment = TextAlignmentOptions.Center;

            return rt;
        }

        // ============================================================
        // Equipment slot row (with slot label prefix)
        // ============================================================

        void CreateEquipSlotRow(string slotLabel, string itemId)
        {
            var itemDef = string.IsNullOrEmpty(itemId) ? null : _registry?.GetItem(itemId);
            string itemName = itemDef != null ? itemDef.displayName : "-- 空 --";
            string itemDesc = GetItemBonusDesc(itemId);
            Sprite icon = CampUIHelper.GetItemIcon(itemDef);

            var row = new GameObject("Equip_" + slotLabel);
            row.transform.SetParent(_equipListParent, false);

            var le = row.AddComponent<LayoutElement>();
            le.preferredHeight = 42;
            le.minHeight = 42;

            var rowImg = row.AddComponent<Image>();
            rowImg.color = new Color(0.18f, 0.15f, 0.1f, 0.7f);

            var outline = row.AddComponent<Outline>();
            outline.effectColor = ThreeKingdomsTheme.PanelBorder;
            outline.effectDistance = new Vector2(1, -1);

            // Slot label (武器/防具/辅助) on left
            var slotGo = new GameObject("SlotLabel");
            slotGo.transform.SetParent(row.transform, false);
            var slotRt = slotGo.AddComponent<RectTransform>();
            slotRt.anchorMin = new Vector2(0, 0);
            slotRt.anchorMax = new Vector2(0, 1);
            slotRt.offsetMin = new Vector2(4, 0);
            slotRt.offsetMax = new Vector2(44, 0);
            slotRt.sizeDelta = new Vector2(40, 0);

            // Slot bg
            var slotBg = slotGo.AddComponent<Image>();
            slotBg.color = new Color(0.35f, 0.28f, 0.18f, 0.8f);

            // Slot text child
            var slotTxtGo = new GameObject("SText");
            slotTxtGo.transform.SetParent(slotGo.transform, false);
            var strt = slotTxtGo.AddComponent<RectTransform>();
            strt.anchorMin = Vector2.zero;
            strt.anchorMax = Vector2.one;
            strt.offsetMin = Vector2.zero;
            strt.offsetMax = Vector2.zero;
            var slotTmp = slotTxtGo.AddComponent<TextMeshProUGUI>();
            slotTmp.text = slotLabel;
            slotTmp.fontSize = 12f;
            slotTmp.color = GoldTitle;
            slotTmp.alignment = TextAlignmentOptions.Center;

            float textLeft = 50f;

            // Icon (if available)
            if (icon != null)
            {
                var iconGo = new GameObject("Icon");
                iconGo.transform.SetParent(row.transform, false);
                var irt = iconGo.AddComponent<RectTransform>();
                irt.anchorMin = new Vector2(0, 0.05f);
                irt.anchorMax = new Vector2(0, 0.95f);
                irt.offsetMin = new Vector2(48, 0);
                irt.offsetMax = new Vector2(84, 0);
                irt.sizeDelta = new Vector2(36, 0);
                var iconImg = iconGo.AddComponent<Image>();
                iconImg.sprite = icon;
                iconImg.preserveAspect = true;
                textLeft = 88f;
            }

            // Item name (top-right of icon)
            var nameGo = new GameObject("ItemName");
            nameGo.transform.SetParent(row.transform, false);
            var nrt = nameGo.AddComponent<RectTransform>();
            nrt.anchorMin = new Vector2(0, 0.45f);
            nrt.anchorMax = new Vector2(1, 1);
            nrt.offsetMin = new Vector2(textLeft, 0);
            nrt.offsetMax = new Vector2(-5, 0);
            var nameTmp = nameGo.AddComponent<TextMeshProUGUI>();
            nameTmp.text = itemName;
            nameTmp.fontSize = 14f;
            nameTmp.color = ThreeKingdomsTheme.TextPrimary;
            nameTmp.alignment = TextAlignmentOptions.MidlineLeft;

            // Bonus description (bottom-right)
            var descGo = new GameObject("ItemDesc");
            descGo.transform.SetParent(row.transform, false);
            var drt = descGo.AddComponent<RectTransform>();
            drt.anchorMin = new Vector2(0, 0);
            drt.anchorMax = new Vector2(1, 0.45f);
            drt.offsetMin = new Vector2(textLeft, 0);
            drt.offsetMax = new Vector2(-5, 0);
            var descTmp = descGo.AddComponent<TextMeshProUGUI>();
            descTmp.text = itemDesc;
            descTmp.fontSize = 11f;
            descTmp.color = ThreeKingdomsTheme.TextSecondary;
            descTmp.alignment = TextAlignmentOptions.MidlineLeft;

            _equipSlotGos.Add(row);
        }

        // ============================================================
        // Navigation
        // ============================================================

        void OnPrevHero()
        {
            if (_heroList.Count <= 0) return;
            _heroIndex = (_heroIndex - 1 + _heroList.Count) % _heroList.Count;
            RefreshAll();
        }

        void OnNextHero()
        {
            if (_heroList.Count <= 0) return;
            _heroIndex = (_heroIndex + 1) % _heroList.Count;
            RefreshAll();
        }

        void SelectTab(int tabIdx)
        {
            _selectedTab = tabIdx;
            HighlightTabs();
            RefreshTabContent();
        }

        void HighlightTabs()
        {
            for (int i = 0; i < _tabButtons.Length; i++)
            {
                if (_tabButtons[i] == null) continue;
                var img = _tabButtons[i].GetComponent<Image>();
                if (img != null)
                    img.color = i == _selectedTab
                        ? ThreeKingdomsTheme.SpeedActive
                        : ThreeKingdomsTheme.ButtonNormal;
            }
        }

        // ============================================================
        // Refresh
        // ============================================================

        void RefreshAll()
        {
            if (_heroList.Count == 0)
            {
                _nameLabel.text = "";
                _levelLabel.text = "暂无武将";
                _classLabel.text = "";
                _pageLabel.text = "0/0";
                _portraitImage.sprite = null;
                _portraitImage.color = new Color(1, 1, 1, 0.2f);
                ClearEquipSlots();
                return;
            }

            var hero = _heroList[_heroIndex];
            var heroDef = _registry?.GetHero(hero.heroId);

            RefreshLeftPanel(hero, heroDef);
            HighlightTabs();
            RefreshTabContent();

            _pageLabel.text = $"{_heroIndex + 1}/{_heroList.Count}";
        }

        void RefreshLeftPanel(HeroRuntimeData hero, HeroDefinition heroDef)
        {
            string heroName = heroDef != null ? heroDef.displayName : hero.heroId;

            _portraitImage.sprite = CampUIHelper.GetHeroPortrait(heroDef);
            _portraitImage.color = Color.white;
            _nameLabel.text = heroName;
            _levelLabel.text = $"等级: {hero.level}    [我军]";

            string unitTypeName = heroDef?.defaultUnitType != null
                ? heroDef.defaultUnitType.displayName : "无兵种";
            _classLabel.text = unitTypeName;

            RefreshEquipment(hero);
        }

        void RefreshEquipment(HeroRuntimeData hero)
        {
            ClearEquipSlots();
            CreateEquipSlotRow("武器", hero.equippedWeaponId);
            CreateEquipSlotRow("防具", hero.equippedArmorId);
            CreateEquipSlotRow("辅助", hero.equippedAuxiliaryId);
        }

        void RefreshTabContent()
        {
            if (_heroList.Count == 0) return;
            var hero = _heroList[_heroIndex];
            var heroDef = _registry?.GetHero(hero.heroId);

            switch (_selectedTab)
            {
                case 0: RefreshAbilityTab(hero, heroDef); break;
                case 1: RefreshUnitTypeTab(hero, heroDef); break;
                case 2: RefreshSkillTab(hero, heroDef); break;
                case 3: RefreshPassiveTab(hero, heroDef); break;
            }
        }

        void RefreshAbilityTab(HeroRuntimeData hero, HeroDefinition heroDef)
        {
            if (_abilityBars.Count == 0)
                BuildAbilityTab();

            _tabContentParent.gameObject.SetActive(true);

            int expNeeded = hero.level * 100;

            // ── HP / MP / Exp bars (indices 0, 1, 2) ──
            SetBar(0, hero.currentHp, hero.maxHp);
            SetBar(1, hero.currentMp, hero.maxMp);
            SetBar(2, hero.exp, expNeeded);

            // ── Left column: 五维 text with growth grades (no bars) ──
            // Use runtime values (which grow on level up) instead of static definition values
            int f = hero.force > 0 ? hero.force : (heroDef?.force ?? 0);
            int intel = hero.intelligence > 0 ? hero.intelligence : (heroDef?.intelligence ?? 0);
            int cmd = hero.command > 0 ? hero.command : (heroDef?.command ?? 0);
            int agi = hero.agility > 0 ? hero.agility : (heroDef?.agility ?? 0);
            int lk = hero.luck > 0 ? hero.luck : (heroDef?.luck ?? 0);

            if (_leftStatTexts.Count >= 6 && heroDef != null)
            {
                // Recalculate current grades for display
                hero.RecalculateGrades(heroDef);

                _leftStatTexts[0].text = FormatDimension("武力", f, hero.forceGrade);
                _leftStatTexts[1].text = FormatDimension("智力", intel, hero.intelligenceGrade);
                _leftStatTexts[2].text = FormatDimension("统帅", cmd, hero.commandGrade);
                _leftStatTexts[3].text = FormatDimension("敏捷", agi, hero.agilityGrade);
                _leftStatTexts[4].text = FormatDimension("气运", lk, hero.luckGrade);
                _leftStatTexts[5].text = $"成长  +{CaoCao.Data.AttributeGrowthSystem.GetGrowthValue(hero.forceGrade)}/" +
                    $"+{CaoCao.Data.AttributeGrowthSystem.GetGrowthValue(hero.intelligenceGrade)}/" +
                    $"+{CaoCao.Data.AttributeGrowthSystem.GetGrowthValue(hero.commandGrade)}/" +
                    $"+{CaoCao.Data.AttributeGrowthSystem.GetGrowthValue(hero.agilityGrade)}/" +
                    $"+{CaoCao.Data.AttributeGrowthSystem.GetGrowthValue(hero.luckGrade)}";
            }
            else if (_leftStatTexts.Count >= 6)
            {
                _leftStatTexts[0].text = $"武力  {f}";
                _leftStatTexts[1].text = $"智力  {intel}";
                _leftStatTexts[2].text = $"统帅  {cmd}";
                _leftStatTexts[3].text = $"敏捷  {agi}";
                _leftStatTexts[4].text = $"气运  {lk}";
                _leftStatTexts[5].text = "";
            }

            // ── Right column: combat stat bars (indices 3-7) ──
            // 3=攻击, 4=谋略, 5=防御, 6=灵敏, 7=士气
            SetBarAbs(3, hero.atk, 200, hero.atk.ToString());
            SetBarAbs(4, intel, 100, intel.ToString());          // 谋略 ← intelligence
            SetBarAbs(5, hero.def, 200, hero.def.ToString());
            SetBarAbs(6, hero.speed, 20, hero.speed.ToString()); // 灵敏 ← speed
            SetBarAbs(7, hero.maxMp, 200, hero.maxMp.ToString()); // 士气 ← maxMp

            // ── 移动 (text only) ──
            if (_movText != null)
                _movText.text = $"移动  {hero.mov}";
        }

        void SetBar(int idx, int current, int max)
        {
            if (idx >= _abilityBars.Count) return;
            var bar = _abilityBars[idx];
            float ratio = max > 0 ? Mathf.Clamp01((float)current / max) : 0;
            bar.fillRt.anchorMax = new Vector2(ratio, 1);
            bar.valueText.text = $"{current}/{max}";
        }

        void SetBarAbs(int idx, int value, int maxRef, string text)
        {
            if (idx >= _abilityBars.Count) return;
            var bar = _abilityBars[idx];
            float ratio = maxRef > 0 ? Mathf.Clamp01((float)value / maxRef) : 0;
            bar.fillRt.anchorMax = new Vector2(ratio, 1);
            bar.valueText.text = text;
        }

        string FormatDimension(string name, int value, CaoCao.Common.GrowthGrade grade)
        {
            string gradeName = CaoCao.Data.AttributeGrowthSystem.GetGradeDisplayName(grade);
            string colorHex = CaoCao.Data.AttributeGrowthSystem.GetGradeColorHex(grade);
            return $"{name}  {value}  <color={colorHex}>{gradeName}</color>";
        }

        void RefreshUnitTypeTab(HeroRuntimeData hero, HeroDefinition heroDef)
        {
            foreach (Transform child in _tabContentParent)
                Destroy(child.gameObject);
            _abilityBars.Clear();

            string unitInfo = "";
            if (heroDef?.defaultUnitType != null)
            {
                var ut = heroDef.defaultUnitType;
                unitInfo =
                    $"<color=#BF9858>当前兵种</color>\n" +
                    $"{ut.displayName}\n\n" +
                    $"移动类型: {ut.movementType}\n" +
                    $"攻击修正: {(ut.atkModifier >= 0 ? "+" : "")}{ut.atkModifier}\n" +
                    $"防御修正: {(ut.defModifier >= 0 ? "+" : "")}{ut.defModifier}\n" +
                    $"移动修正: {(ut.movModifier >= 0 ? "+" : "")}{ut.movModifier}\n" +
                    $"速度修正: {(ut.speedModifier >= 0 ? "+" : "")}{ut.speedModifier}";
                if (ut.upgradeTo != null)
                    unitInfo += $"\n\n升级兵种: {ut.upgradeTo.displayName} (Lv.{ut.upgradeLevel})";
            }
            else
                unitInfo = "暂无兵种信息";

            var tmp = CampUIHelper.CreateAnchoredLabel(_tabContentParent, unitInfo, 16f,
                new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.95f),
                new Vector2(5, 5), new Vector2(-5, -5),
                TextAlignmentOptions.TopLeft);
            tmp.richText = true;
        }

        void RefreshSkillTab(HeroRuntimeData hero, HeroDefinition heroDef)
        {
            foreach (Transform child in _tabContentParent)
                Destroy(child.gameObject);
            _abilityBars.Clear();

            string info = "<color=#BF9858>策略列表</color>\n\n";
            if (heroDef?.learnableSkills != null && heroDef.learnableSkills.Length > 0)
            {
                foreach (var skill in heroDef.learnableSkills)
                {
                    if (skill == null) continue;
                    info += $"{skill.displayName}  消耗:{skill.mpCost}  范围:{skill.range}  威力:{skill.power}\n" +
                            $"  <color=#999>{skill.description}</color>\n\n";
                }
            }
            else
                info += "暂无策略";

            var tmp = CampUIHelper.CreateAnchoredLabel(_tabContentParent, info, 15f,
                new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.95f),
                new Vector2(5, 5), new Vector2(-5, -5),
                TextAlignmentOptions.TopLeft);
            tmp.richText = true;
        }

        void RefreshPassiveTab(HeroRuntimeData hero, HeroDefinition heroDef)
        {
            foreach (Transform child in _tabContentParent)
                Destroy(child.gameObject);
            _abilityBars.Clear();

            string info = "<color=#BF9858>特技列表</color>\n\n";
            if (heroDef?.passiveAbilityIds != null && heroDef.passiveAbilityIds.Length > 0)
            {
                foreach (var pid in heroDef.passiveAbilityIds)
                    if (!string.IsNullOrEmpty(pid)) info += $"- {pid}\n";
            }
            else
                info += "暂无特技";

            var tmp = CampUIHelper.CreateAnchoredLabel(_tabContentParent, info, 16f,
                new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.95f),
                new Vector2(5, 5), new Vector2(-5, -5),
                TextAlignmentOptions.TopLeft);
            tmp.richText = true;
        }

        // ============================================================
        // Helpers
        // ============================================================

        string GetItemDisplayName(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return "-- 空 --";
            var item = _registry?.GetItem(itemId);
            return item != null ? item.displayName : itemId;
        }

        string GetItemBonusDesc(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return "";
            var item = _registry?.GetItem(itemId);
            if (item == null) return "";

            var parts = new List<string>();
            if (item.atkBonus != 0) parts.Add($"攻击+{item.atkBonus}");
            if (item.defBonus != 0) parts.Add($"防御+{item.defBonus}");
            if (item.speedBonus != 0) parts.Add($"速度+{item.speedBonus}");
            if (item.hpBonus != 0) parts.Add($"HP+{item.hpBonus}");
            if (item.mpBonus != 0) parts.Add($"MP+{item.mpBonus}");
            return parts.Count > 0 ? string.Join("  ", parts) : "";
        }

        void ClearEquipSlots()
        {
            foreach (var go in _equipSlotGos)
                if (go != null) Destroy(go);
            _equipSlotGos.Clear();
        }
    }
}
