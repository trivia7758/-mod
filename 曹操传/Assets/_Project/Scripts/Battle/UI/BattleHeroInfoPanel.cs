using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CaoCao.Common;
using CaoCao.Core;
using CaoCao.Camp;
using CaoCao.Data;

namespace CaoCao.Battle
{
    /// <summary>
    /// Full-screen hero info overlay shown on long-press during battle.
    /// Works for both player units (with HeroDefinition) and enemy units (basic stats only).
    /// Layout mirrors the camp hero screen: left panel (portrait + equip), right panel (tabs + stats).
    /// </summary>
    public class BattleHeroInfoPanel : MonoBehaviour
    {
        public event Action OnClosed;

        Canvas _canvas;
        GameObject _root;

        // Data
        BattleUnit _unit;
        HeroDefinition _heroDef;
        HeroRuntimeData _heroRuntime;
        GameDataRegistry _registry;

        // Left panel
        Image _portraitImage;
        TMP_Text _nameLabel;
        TMP_Text _levelLabel;
        TMP_Text _classLabel;
        Transform _equipListParent;
        List<GameObject> _equipSlotGos = new();

        // Right panel
        Transform _tabContentParent;
        Button[] _tabButtons = new Button[4];
        int _selectedTab;

        // Ability tab refs
        List<BarRef> _abilityBars = new();
        List<TMP_Text> _leftStatTexts = new();
        TMP_Text _movText;

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

        public bool IsOpen => _root != null && _root.activeSelf;

        /// <summary>
        /// Show the info panel for a battle unit.
        /// </summary>
        public void Show(BattleUnit unit)
        {
            _unit = unit;
            _registry = ServiceLocator.Get<GameDataRegistry>();

            // Try to find HeroDefinition and runtime data for player units
            _heroDef = null;
            _heroRuntime = null;
            if (unit.team == UnitTeam.Player)
            {
                var gsm = ServiceLocator.Get<GameStateManager>();
                if (_registry != null)
                {
                    // Try matching by displayName since BattleUnit doesn't store heroId directly
                    if (_registry.allHeroes != null)
                    {
                        foreach (var h in _registry.allHeroes)
                        {
                            if (h != null && h.displayName == unit.displayName)
                            {
                                _heroDef = h;
                                if (gsm != null) _heroRuntime = gsm.GetHero(h.id);
                                break;
                            }
                        }
                    }
                }
            }

            BuildUI();
            _root.SetActive(true);
        }

        public void Hide()
        {
            if (_root != null)
                _root.SetActive(false);
            OnClosed?.Invoke();
        }

        void BuildUI()
        {
            // Destroy previous if exists
            if (_root != null) Destroy(_root);
            _equipSlotGos.Clear();
            _abilityBars.Clear();
            _leftStatTexts.Clear();
            _movText = null;

            // Ensure canvas
            EnsureCanvas();

            // Root panel (full screen dark overlay)
            _root = new GameObject("BattleHeroInfoRoot");
            _root.transform.SetParent(_canvas.transform, false);
            var rootRt = _root.AddComponent<RectTransform>();
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.offsetMin = Vector2.zero;
            rootRt.offsetMax = Vector2.zero;

            // Dark background (click to close)
            var bgImg = _root.AddComponent<Image>();
            bgImg.color = new Color(0, 0, 0, 0.85f);
            var bgBtn = _root.AddComponent<Button>();
            bgBtn.targetGraphic = bgImg;
            bgBtn.transition = Selectable.Transition.None;
            bgBtn.onClick.AddListener(Hide);

            // Main content area (doesn't close on click)
            var content = new GameObject("Content");
            content.transform.SetParent(_root.transform, false);
            var cRt = content.AddComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0.02f, 0.02f);
            cRt.anchorMax = new Vector2(0.98f, 0.98f);
            cRt.offsetMin = Vector2.zero;
            cRt.offsetMax = Vector2.zero;
            // Block click-through to background
            var contentImg = content.AddComponent<Image>();
            contentImg.color = new Color(0, 0, 0, 0.01f); // nearly invisible but catches clicks
            contentImg.raycastTarget = true;

            // Title bar
            string teamStr = _unit.team == UnitTeam.Player ? "[我军]" : "[敌军]";
            CreateSectionHeader(content.transform, $"武 将  {teamStr}",
                new Vector2(0.15f, 0.93f), new Vector2(0.85f, 1f));

            // Close button
            var closeBtn = CampUIHelper.CreateAnchoredButton(content.transform, "X",
                new Vector2(0.92f, 0.93f), new Vector2(0.99f, 1f),
                Vector2.zero, Vector2.zero, 20f);
            closeBtn.onClick.AddListener(Hide);

            // Left panel
            BuildLeftPanel(content.transform);

            // Right panel
            BuildRightPanel(content.transform);

            // Populate data
            RefreshLeftPanel();
            SelectTab(0);
        }

        void EnsureCanvas()
        {
            if (_canvas != null) return;

            var go = new GameObject("BattleHeroInfoCanvas");
            go.transform.SetParent(transform);
            _canvas = go.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 200; // above everything

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 0.5f;

            go.AddComponent<GraphicRaycaster>();
        }

        // ── Left Panel ──

        void BuildLeftPanel(Transform parent)
        {
            var leftPanel = CampUIHelper.CreateSubPanel(parent, "LeftPanel",
                new Vector2(0.04f, 0.06f), new Vector2(0.46f, 0.92f),
                Vector2.zero, Vector2.zero);

            // Portrait
            var portraitBorder = CampUIHelper.CreateSubPanel(leftPanel, "PortraitBorder",
                new Vector2(0.03f, 0.6f), new Vector2(0.42f, 0.97f),
                Vector2.zero, Vector2.zero);
            var borderOutline = portraitBorder.GetComponent<Outline>();
            if (borderOutline != null)
                borderOutline.effectColor = _unit.team == UnitTeam.Player
                    ? new Color(0.6f, 0.15f, 0.15f, 1f)
                    : new Color(0.15f, 0.15f, 0.6f, 1f);
            var borderImg = portraitBorder.GetComponent<Image>();
            if (borderImg != null) borderImg.color = new Color(0.1f, 0.08f, 0.06f, 1f);

            _portraitImage = CampUIHelper.CreateAnchoredImage(portraitBorder, "Portrait", null,
                new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.96f),
                Vector2.zero, Vector2.zero);

            _nameLabel = CampUIHelper.CreateAnchoredLabel(leftPanel, "", 22f,
                new Vector2(0.45f, 0.87f), new Vector2(0.98f, 0.97f),
                new Vector2(5, 0), Vector2.zero,
                TextAlignmentOptions.MidlineLeft);
            _nameLabel.color = GoldTitle;
            _nameLabel.fontStyle = FontStyles.Bold | FontStyles.Underline;

            _levelLabel = CampUIHelper.CreateAnchoredLabel(leftPanel, "", 15f,
                new Vector2(0.45f, 0.78f), new Vector2(0.98f, 0.87f),
                new Vector2(5, 0), Vector2.zero,
                TextAlignmentOptions.MidlineLeft);

            _classLabel = CampUIHelper.CreateAnchoredLabel(leftPanel, "", 15f,
                new Vector2(0.45f, 0.69f), new Vector2(0.98f, 0.78f),
                new Vector2(5, 0), Vector2.zero,
                TextAlignmentOptions.MidlineLeft);

            // Equipment section
            CreateSectionHeader(leftPanel, "装 备",
                new Vector2(0.03f, 0.54f), new Vector2(0.97f, 0.61f));

            var equipMask = new GameObject("EquipMask");
            equipMask.transform.SetParent(leftPanel, false);
            var emrt = equipMask.AddComponent<RectTransform>();
            emrt.anchorMin = new Vector2(0.03f, 0.02f);
            emrt.anchorMax = new Vector2(0.97f, 0.54f);
            emrt.offsetMin = Vector2.zero;
            emrt.offsetMax = Vector2.zero;
            equipMask.AddComponent<RectMask2D>();

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

            _equipListParent = equipArea.transform;
        }

        void RefreshLeftPanel()
        {
            // Portrait
            Sprite portrait = _unit.portrait;
            if (portrait == null && _heroDef != null)
                portrait = CampUIHelper.GetHeroPortrait(_heroDef);
            _portraitImage.sprite = portrait;
            _portraitImage.color = portrait != null ? Color.white : new Color(1, 1, 1, 0.2f);

            // Name
            _nameLabel.text = _unit.displayName;

            // Level + team
            string teamTag = _unit.team == UnitTeam.Player ? "[我军]" : "[敌军]";
            _levelLabel.text = $"等级: {_unit.level}    {teamTag}";

            // Class
            _classLabel.text = !string.IsNullOrEmpty(_unit.unitTypeName) ? _unit.unitTypeName : _unit.unitClass.ToString();

            // Equipment
            RefreshEquipment();
        }

        void RefreshEquipment()
        {
            ClearEquipSlots();

            if (_heroRuntime != null)
            {
                CreateEquipSlotRow("武器", _heroRuntime.equippedWeaponId);
                CreateEquipSlotRow("防具", _heroRuntime.equippedArmorId);
                CreateEquipSlotRow("辅助", _heroRuntime.equippedAuxiliaryId);
            }
            else
            {
                CreateEquipSlotRow("武器", "");
                CreateEquipSlotRow("防具", "");
                CreateEquipSlotRow("辅助", "");
            }
        }

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

            // Slot label
            var slotGo = new GameObject("SlotLabel");
            slotGo.transform.SetParent(row.transform, false);
            var slotRt = slotGo.AddComponent<RectTransform>();
            slotRt.anchorMin = new Vector2(0, 0);
            slotRt.anchorMax = new Vector2(0, 1);
            slotRt.offsetMin = new Vector2(4, 0);
            slotRt.offsetMax = new Vector2(44, 0);
            slotRt.sizeDelta = new Vector2(40, 0);

            var slotBg = slotGo.AddComponent<Image>();
            slotBg.color = new Color(0.35f, 0.28f, 0.18f, 0.8f);

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

        // ── Right Panel ──

        void BuildRightPanel(Transform parent)
        {
            var rightPanel = CampUIHelper.CreateSubPanel(parent, "RightPanel",
                new Vector2(0.48f, 0.06f), new Vector2(0.96f, 0.92f),
                Vector2.zero, Vector2.zero);

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

            var contentPanel = new GameObject("TabContent");
            contentPanel.transform.SetParent(rightPanel, false);
            var crt = contentPanel.AddComponent<RectTransform>();
            crt.anchorMin = new Vector2(0.02f, 0.02f);
            crt.anchorMax = new Vector2(0.98f, 0.91f);
            crt.offsetMin = Vector2.zero;
            crt.offsetMax = Vector2.zero;
            _tabContentParent = contentPanel.transform;
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

        void RefreshTabContent()
        {
            switch (_selectedTab)
            {
                case 0: RefreshAbilityTab(); break;
                case 1: RefreshUnitTypeTab(); break;
                case 2: RefreshSkillTab(); break;
                case 3: RefreshPassiveTab(); break;
            }
        }

        // ── 能力 Tab ──

        void RefreshAbilityTab()
        {
            foreach (Transform child in _tabContentParent)
                Destroy(child.gameObject);
            _abilityBars.Clear();
            _leftStatTexts.Clear();
            _movText = null;

            // 基本状态
            CreateSectionHeader(_tabContentParent, "基本状态",
                new Vector2(0.02f, 0.85f), new Vector2(0.98f, 0.93f));

            CreateColorBar(_tabContentParent, "HP", HpColor,
                new Vector2(0.02f, 0.74f), new Vector2(0.98f, 0.84f));
            CreateColorBar(_tabContentParent, "MP", MpColor,
                new Vector2(0.02f, 0.63f), new Vector2(0.98f, 0.73f));
            CreateColorBar(_tabContentParent, "Exp", ExpColor,
                new Vector2(0.02f, 0.52f), new Vector2(0.98f, 0.62f));

            // 基本能力
            CreateSectionHeader(_tabContentParent, "基本能力",
                new Vector2(0.02f, 0.42f), new Vector2(0.98f, 0.5f));

            float startY = 0.35f;
            float rowH = 0.06f;

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

            string[] rightBarNames = { "攻击", "谋略", "防御", "灵敏", "士气" };
            Color[] rightBarColors = { AtkColor, MpColor, DefColor, SpdColor, ExpColor };
            for (int i = 0; i < 5; i++)
            {
                float y = startY - i * rowH;
                CreateStatBar(_tabContentParent, rightBarNames[i], rightBarColors[i],
                    new Vector2(0.5f, y), new Vector2(0.98f, y + rowH));
            }

            float movY = startY - 5 * rowH;
            _movText = CampUIHelper.CreateAnchoredLabel(_tabContentParent, "", 14f,
                new Vector2(0.5f, movY), new Vector2(0.98f, movY + rowH),
                new Vector2(8, 0), Vector2.zero,
                TextAlignmentOptions.MidlineLeft);
            _movText.color = ThreeKingdomsTheme.TextPrimary;

            // Fill data
            int hp = _unit.hp, maxHp = _unit.maxHp;
            int mp = _unit.mp, maxMp = _unit.maxMp;
            int expVal = _unit.exp, expNeeded = _unit.level * 100;

            SetBar(0, hp, maxHp);
            SetBar(1, mp, maxMp);
            SetBar(2, expVal, expNeeded);

            // Use runtime values (which grow) if available, fallback to definition
            int f = _heroRuntime?.force > 0 ? _heroRuntime.force : (_heroDef?.force ?? 0);
            int intel = _heroRuntime?.intelligence > 0 ? _heroRuntime.intelligence : (_heroDef?.intelligence ?? 0);
            int cmd = _heroRuntime?.command > 0 ? _heroRuntime.command : (_heroDef?.command ?? 0);
            int agi = _heroRuntime?.agility > 0 ? _heroRuntime.agility : (_heroDef?.agility ?? 0);
            int lk = _heroRuntime?.luck > 0 ? _heroRuntime.luck : (_heroDef?.luck ?? 0);

            if (_leftStatTexts.Count >= 6 && _heroDef != null && _heroRuntime != null)
            {
                _heroRuntime.RecalculateGrades(_heroDef);
                _leftStatTexts[0].text = FormatDimension("武力", f, _heroRuntime.forceGrade);
                _leftStatTexts[1].text = FormatDimension("智力", intel, _heroRuntime.intelligenceGrade);
                _leftStatTexts[2].text = FormatDimension("统帅", cmd, _heroRuntime.commandGrade);
                _leftStatTexts[3].text = FormatDimension("敏捷", agi, _heroRuntime.agilityGrade);
                _leftStatTexts[4].text = FormatDimension("气运", lk, _heroRuntime.luckGrade);
                _leftStatTexts[5].text = $"成长  +{AttributeGrowthSystem.GetGrowthValue(_heroRuntime.forceGrade)}/" +
                    $"+{AttributeGrowthSystem.GetGrowthValue(_heroRuntime.intelligenceGrade)}/" +
                    $"+{AttributeGrowthSystem.GetGrowthValue(_heroRuntime.commandGrade)}/" +
                    $"+{AttributeGrowthSystem.GetGrowthValue(_heroRuntime.agilityGrade)}/" +
                    $"+{AttributeGrowthSystem.GetGrowthValue(_heroRuntime.luckGrade)}";
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

            SetBarAbs(3, _unit.atk, 200, _unit.atk.ToString());
            SetBarAbs(4, intel, 100, intel.ToString());
            SetBarAbs(5, _unit.def, 200, _unit.def.ToString());
            int speed = _heroRuntime?.speed ?? 0;
            SetBarAbs(6, speed, 20, speed.ToString());
            SetBarAbs(7, maxMp, 200, maxMp.ToString());

            if (_movText != null)
                _movText.text = $"移动  {_unit.mov}";
        }

        // ── 兵种 Tab ──

        void RefreshUnitTypeTab()
        {
            foreach (Transform child in _tabContentParent)
                Destroy(child.gameObject);
            _abilityBars.Clear();

            string unitInfo;
            if (_heroDef?.defaultUnitType != null)
            {
                var ut = _heroDef.defaultUnitType;
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
            {
                unitInfo = $"<color=#BF9858>兵种信息</color>\n" +
                    $"{_unit.unitTypeName}\n" +
                    $"移动类型: {_unit.unitClass}";
            }

            var tmp = CampUIHelper.CreateAnchoredLabel(_tabContentParent, unitInfo, 16f,
                new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.95f),
                new Vector2(5, 5), new Vector2(-5, -5),
                TextAlignmentOptions.TopLeft);
            tmp.richText = true;
        }

        // ── 策略 Tab ──

        void RefreshSkillTab()
        {
            foreach (Transform child in _tabContentParent)
                Destroy(child.gameObject);
            _abilityBars.Clear();

            string info = "<color=#BF9858>策略列表</color>\n\n";
            bool hasSkills = false;

            // From BattleUnit's runtime skills
            if (_unit.skills != null && _unit.skills.Count > 0)
            {
                foreach (var skill in _unit.skills)
                {
                    if (skill == null) continue;
                    hasSkills = true;
                    info += $"{skill.displayName}  消耗:{skill.mpCost}  范围:{skill.range}  威力:{skill.power}\n" +
                            $"  <color=#999>{skill.description}</color>\n\n";
                }
            }

            // Fallback to HeroDefinition
            if (!hasSkills && _heroDef?.learnableSkills != null)
            {
                foreach (var skill in _heroDef.learnableSkills)
                {
                    if (skill == null) continue;
                    hasSkills = true;
                    info += $"{skill.displayName}  消耗:{skill.mpCost}  范围:{skill.range}  威力:{skill.power}\n" +
                            $"  <color=#999>{skill.description}</color>\n\n";
                }
            }

            if (!hasSkills)
                info += "暂无策略";

            var tmp = CampUIHelper.CreateAnchoredLabel(_tabContentParent, info, 15f,
                new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.95f),
                new Vector2(5, 5), new Vector2(-5, -5),
                TextAlignmentOptions.TopLeft);
            tmp.richText = true;
        }

        // ── 特技 Tab ──

        void RefreshPassiveTab()
        {
            foreach (Transform child in _tabContentParent)
                Destroy(child.gameObject);
            _abilityBars.Clear();

            string info = "<color=#BF9858>特技列表</color>\n\n";
            if (_heroDef?.passiveAbilityIds != null && _heroDef.passiveAbilityIds.Length > 0)
            {
                foreach (var pid in _heroDef.passiveAbilityIds)
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

        // ── Bar helpers ──

        void CreateColorBar(Transform parent, string label, Color fillColor,
            Vector2 anchorMin, Vector2 anchorMax)
        {
            CampUIHelper.CreateAnchoredLabel(parent, label, 16f,
                new Vector2(anchorMin.x, anchorMin.y),
                new Vector2(anchorMin.x + 0.12f, anchorMax.y),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.Center);

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

            var fillGo = new GameObject("Fill_" + label);
            fillGo.transform.SetParent(barBgGo.transform, false);
            var frt = fillGo.AddComponent<RectTransform>();
            frt.anchorMin = Vector2.zero;
            frt.anchorMax = Vector2.one;
            frt.offsetMin = new Vector2(1, 1);
            frt.offsetMax = new Vector2(-1, -1);
            fillGo.AddComponent<Image>().color = fillColor;

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

        void CreateStatBar(Transform parent, string label, Color fillColor,
            Vector2 anchorMin, Vector2 anchorMax)
        {
            CampUIHelper.CreateAnchoredLabel(parent, label, 14f,
                new Vector2(anchorMin.x, anchorMin.y),
                new Vector2(anchorMin.x + 0.12f, anchorMax.y),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.MidlineRight);

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

            var fillGo = new GameObject("Fill_" + label);
            fillGo.transform.SetParent(barBgGo.transform, false);
            var frt = fillGo.AddComponent<RectTransform>();
            frt.anchorMin = Vector2.zero;
            frt.anchorMax = Vector2.one;
            frt.offsetMin = new Vector2(1, 1);
            frt.offsetMax = new Vector2(-1, -1);
            fillGo.AddComponent<Image>().color = fillColor;

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
            string gradeName = AttributeGrowthSystem.GetGradeDisplayName(grade);
            string colorHex = AttributeGrowthSystem.GetGradeColorHex(grade);
            return $"{name}  {value}  <color={colorHex}>{gradeName}</color>";
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

        void OnDestroy()
        {
            if (_canvas != null)
                Destroy(_canvas.gameObject);
        }
    }
}
