using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CaoCao.Common;
using CaoCao.Core;
using CaoCao.Data;

namespace CaoCao.Camp
{
    /// <summary>
    /// Equipment management screen (装备).
    /// Left = hero info + equipped items (click to unequip).
    /// Right = slot tabs + available items list / item detail with equip button.
    /// </summary>
    public class CampEquipScreen : MonoBehaviour
    {
        public event Action OnBack;

        GameStateManager _gsm;
        GameDataRegistry _registry;

        // Navigation state
        int _heroIndex;
        List<HeroRuntimeData> _heroList = new();
        EquipSlot _selectedSlot = EquipSlot.Weapon;

        // UI — left panel hero info
        Image _portraitImage;
        TMP_Text _nameLabel;
        TMP_Text _statsLeftLabel;   // 生命/攻击/防御/士气
        TMP_Text _statsRightLabel;  // 策略/谋略/灵敏/移动
        TMP_Text _levelClassLabel;  // 等级 X  兵种

        // UI — left panel equipped items
        Transform _equipListParent;
        List<GameObject> _equipSlotGos = new();

        // UI — right panel tabs
        Button _tabWeaponBtn;
        Button _tabArmorBtn;
        Button _tabAuxBtn;

        // UI — right panel item list (scroll)
        ScrollRect _scrollRect;
        RectTransform _itemListContent;
        GameObject _scrollViewGo;
        List<Button> _itemRows = new();

        // UI — right panel item detail overlay
        GameObject _detailPanel;
        Image _detailIcon;
        TMP_Text _detailName;
        TMP_Text _detailDesc;
        TMP_Text _detailStats;
        Button _detailEquipBtn;
        Button _detailCloseBtn;
        string _detailItemId;

        // UI — bottom
        TMP_Text _pageLabel;

        // Colors
        static readonly Color GoldTitle = new(0.75f, 0.6f, 0.3f, 1f);
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

            HideDetail();
            RefreshAll();
        }

        // ============================================================
        // Layout
        // ============================================================

        void BuildLayout()
        {
            // ── Title bar ──
            CreateSectionHeader(transform, "装 备",
                new Vector2(0.15f, 0.93f), new Vector2(0.85f, 1f));

            // ── LEFT PANEL ──
            BuildLeftPanel();

            // ── RIGHT PANEL ──
            BuildRightPanel();

            // ── Page indicator (bottom-right) ──
            _pageLabel = CampUIHelper.CreateAnchoredLabel(transform, "1/1", 16f,
                new Vector2(0.7f, 0.01f), new Vector2(0.9f, 0.07f),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.Center);
        }

        void BuildLeftPanel()
        {
            var leftPanel = CampUIHelper.CreateSubPanel(transform, "LeftPanel",
                new Vector2(0.02f, 0.07f), new Vector2(0.42f, 0.92f),
                Vector2.zero, Vector2.zero);

            // ── Portrait with border (top-left) ──
            var portraitBorder = CampUIHelper.CreateSubPanel(leftPanel, "PortraitBorder",
                new Vector2(0.03f, 0.7f), new Vector2(0.32f, 0.97f),
                Vector2.zero, Vector2.zero);
            var borderOutline = portraitBorder.GetComponent<Outline>();
            if (borderOutline != null) borderOutline.effectColor = new Color(0.6f, 0.5f, 0.3f, 1f);
            var borderImg = portraitBorder.GetComponent<Image>();
            if (borderImg != null) borderImg.color = new Color(0.1f, 0.08f, 0.06f, 1f);

            _portraitImage = CampUIHelper.CreateAnchoredImage(portraitBorder, "Portrait", null,
                new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.96f),
                Vector2.zero, Vector2.zero);

            // ── Level + Class (right of portrait, top) ──
            _levelClassLabel = CampUIHelper.CreateAnchoredLabel(leftPanel, "", 14f,
                new Vector2(0.34f, 0.9f), new Vector2(0.98f, 0.97f),
                new Vector2(5, 0), Vector2.zero,
                TextAlignmentOptions.MidlineLeft);

            // ── Stats left column ──
            _statsLeftLabel = CampUIHelper.CreateAnchoredLabel(leftPanel, "", 13f,
                new Vector2(0.34f, 0.7f), new Vector2(0.64f, 0.9f),
                new Vector2(5, 0), Vector2.zero,
                TextAlignmentOptions.TopLeft);
            _statsLeftLabel.lineSpacing = 8f;

            // ── Stats right column ──
            _statsRightLabel = CampUIHelper.CreateAnchoredLabel(leftPanel, "", 13f,
                new Vector2(0.64f, 0.7f), new Vector2(0.98f, 0.9f),
                new Vector2(5, 0), Vector2.zero,
                TextAlignmentOptions.TopLeft);
            _statsRightLabel.lineSpacing = 8f;

            // ── Hero name (below portrait, underlined gold) ──
            _nameLabel = CampUIHelper.CreateAnchoredLabel(leftPanel, "", 16f,
                new Vector2(0.03f, 0.63f), new Vector2(0.32f, 0.7f),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.Center);
            _nameLabel.color = GoldTitle;
            _nameLabel.fontStyle = FontStyles.Bold | FontStyles.Underline;

            // ── Equipment list ──
            var equipMask = new GameObject("EquipMask");
            equipMask.transform.SetParent(leftPanel, false);
            var emrt = equipMask.AddComponent<RectTransform>();
            emrt.anchorMin = new Vector2(0.02f, 0.12f);
            emrt.anchorMax = new Vector2(0.98f, 0.62f);
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
            vlg.spacing = 4;
            vlg.padding = new RectOffset(2, 2, 2, 2);
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;

            _equipListParent = equipArea.transform;

            // ── Navigation buttons (bottom of left panel) ──
            var prevBtn = CampUIHelper.CreateAnchoredButton(leftPanel, "上一武将",
                new Vector2(0.03f, 0.01f), new Vector2(0.48f, 0.1f),
                Vector2.zero, Vector2.zero, 16f);
            prevBtn.onClick.AddListener(OnPrevHero);

            var nextBtn = CampUIHelper.CreateAnchoredButton(leftPanel, "下一武将",
                new Vector2(0.52f, 0.01f), new Vector2(0.97f, 0.1f),
                Vector2.zero, Vector2.zero, 16f);
            nextBtn.onClick.AddListener(OnNextHero);
        }

        void BuildRightPanel()
        {
            var rightPanel = CampUIHelper.CreateSubPanel(transform, "RightPanel",
                new Vector2(0.44f, 0.07f), new Vector2(0.98f, 0.92f),
                Vector2.zero, Vector2.zero);

            // ── Slot tab buttons (top) ──
            _tabWeaponBtn = CampUIHelper.CreateAnchoredButton(rightPanel, "武器",
                new Vector2(0.02f, 0.92f), new Vector2(0.22f, 1f),
                Vector2.zero, Vector2.zero, 16f);
            _tabWeaponBtn.onClick.AddListener(() => SelectSlot(EquipSlot.Weapon));

            _tabArmorBtn = CampUIHelper.CreateAnchoredButton(rightPanel, "防具",
                new Vector2(0.24f, 0.92f), new Vector2(0.44f, 1f),
                Vector2.zero, Vector2.zero, 16f);
            _tabArmorBtn.onClick.AddListener(() => SelectSlot(EquipSlot.Armor));

            _tabAuxBtn = CampUIHelper.CreateAnchoredButton(rightPanel, "辅助",
                new Vector2(0.46f, 0.92f), new Vector2(0.66f, 1f),
                Vector2.zero, Vector2.zero, 16f);
            _tabAuxBtn.onClick.AddListener(() => SelectSlot(EquipSlot.Auxiliary));

            // ── Back button (top-right) ──
            var backBtn = CampUIHelper.CreateAnchoredButton(rightPanel, "返回",
                new Vector2(0.78f, 0.92f), new Vector2(0.98f, 1f),
                Vector2.zero, Vector2.zero, 16f);
            backBtn.onClick.AddListener(() => OnBack?.Invoke());

            // ── Available items scroll view ──
            var (scroll, content) = CampUIHelper.CreateScrollView(rightPanel,
                new Vector2(0.02f, 0.02f), new Vector2(0.98f, 0.9f),
                new Vector2(4, 0), new Vector2(-4, 0));
            _scrollRect = scroll;
            _itemListContent = content;
            _scrollViewGo = scroll.gameObject;

            // Ensure layout group has explicit child control settings
            var vlg = content.GetComponent<VerticalLayoutGroup>();
            if (vlg != null)
            {
                vlg.childControlWidth = true;
                vlg.childControlHeight = true;
            }

            // ── Item detail panel (hidden by default, overlays the scroll area) ──
            BuildDetailPanel(rightPanel);
        }

        void BuildDetailPanel(Transform rightPanel)
        {
            _detailPanel = new GameObject("DetailPanel");
            _detailPanel.transform.SetParent(rightPanel, false);
            var drt = _detailPanel.AddComponent<RectTransform>();
            drt.anchorMin = new Vector2(0.02f, 0.02f);
            drt.anchorMax = new Vector2(0.98f, 0.9f);
            drt.offsetMin = Vector2.zero;
            drt.offsetMax = Vector2.zero;

            var bg = _detailPanel.AddComponent<Image>();
            bg.color = new Color(0.12f, 0.1f, 0.07f, 0.95f);

            var outline = _detailPanel.AddComponent<Outline>();
            outline.effectColor = new Color(0.6f, 0.45f, 0.2f, 0.8f);
            outline.effectDistance = new Vector2(2, -2);

            // ── Icon (top-left) ──
            var iconBg = new GameObject("IconBg");
            iconBg.transform.SetParent(_detailPanel.transform, false);
            var ibrt = iconBg.AddComponent<RectTransform>();
            ibrt.anchorMin = new Vector2(0.05f, 0.6f);
            ibrt.anchorMax = new Vector2(0.3f, 0.92f);
            ibrt.offsetMin = Vector2.zero;
            ibrt.offsetMax = Vector2.zero;
            iconBg.AddComponent<Image>().color = new Color(0.18f, 0.15f, 0.1f, 0.8f);

            var iconGo = new GameObject("Icon");
            iconGo.transform.SetParent(iconBg.transform, false);
            var irt = iconGo.AddComponent<RectTransform>();
            irt.anchorMin = new Vector2(0.05f, 0.05f);
            irt.anchorMax = new Vector2(0.95f, 0.95f);
            irt.offsetMin = Vector2.zero;
            irt.offsetMax = Vector2.zero;
            _detailIcon = iconGo.AddComponent<Image>();
            _detailIcon.preserveAspect = true;

            // ── Name (top-right of icon) ──
            _detailName = CampUIHelper.CreateAnchoredLabel(_detailPanel.transform, "", 20f,
                new Vector2(0.32f, 0.78f), new Vector2(0.95f, 0.92f),
                new Vector2(5, 0), Vector2.zero,
                TextAlignmentOptions.MidlineLeft);
            _detailName.color = GoldTitle;
            _detailName.fontStyle = FontStyles.Bold;

            // ── Stats (below name) ──
            _detailStats = CampUIHelper.CreateAnchoredLabel(_detailPanel.transform, "", 15f,
                new Vector2(0.32f, 0.6f), new Vector2(0.95f, 0.78f),
                new Vector2(5, 0), Vector2.zero,
                TextAlignmentOptions.TopLeft);
            _detailStats.color = new Color(0.6f, 0.85f, 0.6f, 1f);

            // ── Description ──
            _detailDesc = CampUIHelper.CreateAnchoredLabel(_detailPanel.transform, "", 14f,
                new Vector2(0.05f, 0.35f), new Vector2(0.95f, 0.58f),
                new Vector2(5, 5), new Vector2(-5, -5),
                TextAlignmentOptions.TopLeft);
            _detailDesc.color = ThreeKingdomsTheme.TextSecondary;

            // ── Equip button ──
            _detailEquipBtn = CampUIHelper.CreateAnchoredButton(_detailPanel.transform, "装备此装备",
                new Vector2(0.1f, 0.08f), new Vector2(0.55f, 0.22f),
                Vector2.zero, Vector2.zero, 18f);
            _detailEquipBtn.onClick.AddListener(OnDetailEquip);

            // ── Close button ──
            _detailCloseBtn = CampUIHelper.CreateAnchoredButton(_detailPanel.transform, "返回",
                new Vector2(0.6f, 0.08f), new Vector2(0.9f, 0.22f),
                Vector2.zero, Vector2.zero, 18f);
            _detailCloseBtn.onClick.AddListener(HideDetail);

            _detailPanel.SetActive(false);
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
            go.AddComponent<Image>().color = SectionHeaderBg;

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            var trt = textGo.AddComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;
            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 18f;
            tmp.color = ThreeKingdomsTheme.TextPrimary;
            tmp.alignment = TextAlignmentOptions.Center;

            return rt;
        }

        // ============================================================
        // Equipped item row (left panel — clickable to unequip)
        // ============================================================

        void CreateEquipRow(string slotLabel, string itemId, EquipSlot slot)
        {
            var row = new GameObject("EqRow_" + slotLabel);
            row.transform.SetParent(_equipListParent, false);

            var le = row.AddComponent<LayoutElement>();
            le.preferredHeight = 52;
            le.minHeight = 52;

            var rowImg = row.AddComponent<Image>();
            rowImg.color = new Color(0.18f, 0.15f, 0.1f, 0.7f);

            var outline = row.AddComponent<Outline>();
            outline.effectColor = ThreeKingdomsTheme.PanelBorder;
            outline.effectDistance = new Vector2(1, -1);

            // Make the whole row a button
            var btn = row.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = new Color(0.18f, 0.15f, 0.1f, 0.7f);
            colors.highlightedColor = ThreeKingdomsTheme.ButtonHover;
            colors.pressedColor = ThreeKingdomsTheme.ButtonPressed;
            btn.colors = colors;
            btn.targetGraphic = rowImg;

            if (string.IsNullOrEmpty(itemId))
            {
                // Empty slot — not clickable
                btn.interactable = false;

                var emptyGo = new GameObject("Empty");
                emptyGo.transform.SetParent(row.transform, false);
                var ert = emptyGo.AddComponent<RectTransform>();
                ert.anchorMin = Vector2.zero;
                ert.anchorMax = Vector2.one;
                ert.offsetMin = new Vector2(10, 0);
                ert.offsetMax = new Vector2(-10, 0);
                var etmp = emptyGo.AddComponent<TextMeshProUGUI>();
                etmp.text = $"-- {slotLabel}: 空 --";
                etmp.fontSize = 14f;
                etmp.color = ThreeKingdomsTheme.TextSecondary;
                etmp.alignment = TextAlignmentOptions.MidlineLeft;
            }
            else
            {
                var item = _registry?.GetItem(itemId);
                string itemName = item != null ? item.displayName : itemId;
                Sprite icon = CampUIHelper.GetItemIcon(item);

                float textLeft = 8f;

                // Icon (if available)
                if (icon != null)
                {
                    var iconGo = new GameObject("Icon");
                    iconGo.transform.SetParent(row.transform, false);
                    var irt = iconGo.AddComponent<RectTransform>();
                    irt.anchorMin = new Vector2(0, 0.05f);
                    irt.anchorMax = new Vector2(0, 0.95f);
                    irt.offsetMin = new Vector2(4, 0);
                    irt.offsetMax = new Vector2(48, 0);
                    irt.sizeDelta = new Vector2(44, 0);
                    var iconImg = iconGo.AddComponent<Image>();
                    iconImg.sprite = icon;
                    iconImg.preserveAspect = true;
                    textLeft = 52f;
                }

                // Item name (top line)
                var nameGo = new GameObject("Name");
                nameGo.transform.SetParent(row.transform, false);
                var nrt = nameGo.AddComponent<RectTransform>();
                nrt.anchorMin = new Vector2(0, 0.5f);
                nrt.anchorMax = new Vector2(1, 1);
                nrt.offsetMin = new Vector2(textLeft, 0);
                nrt.offsetMax = new Vector2(-5, -2);
                var ntmp = nameGo.AddComponent<TextMeshProUGUI>();
                ntmp.text = itemName;
                ntmp.fontSize = 14f;
                ntmp.color = ThreeKingdomsTheme.TextPrimary;
                ntmp.alignment = TextAlignmentOptions.MidlineLeft;

                // Bonus desc (bottom line)
                var descGo = new GameObject("Desc");
                descGo.transform.SetParent(row.transform, false);
                var drt = descGo.AddComponent<RectTransform>();
                drt.anchorMin = new Vector2(0, 0);
                drt.anchorMax = new Vector2(1, 0.5f);
                drt.offsetMin = new Vector2(textLeft, 2);
                drt.offsetMax = new Vector2(-5, 0);
                var dtmp = descGo.AddComponent<TextMeshProUGUI>();
                dtmp.text = GetBonusText(item);
                dtmp.fontSize = 11f;
                dtmp.color = ThreeKingdomsTheme.TextSecondary;
                dtmp.alignment = TextAlignmentOptions.MidlineLeft;

                // On click: unequip this slot + switch tab
                EquipSlot capturedSlot = slot;
                btn.onClick.AddListener(() => OnEquipRowClick(capturedSlot));
            }

            _equipSlotGos.Add(row);
        }

        /// <summary>
        /// Called when player clicks an equipped item in the left panel.
        /// Unequips the item and switches to the corresponding tab.
        /// </summary>
        void OnEquipRowClick(EquipSlot slot)
        {
            string heroId = GetCurrentHeroId();
            if (string.IsNullOrEmpty(heroId)) return;

            string removedId = _gsm.UnequipItem(heroId, slot);
            Debug.Log($"[Equip] Unequipped '{removedId}' from slot {slot}. " +
                      $"Inventory count: {_gsm.TotalItemCount}");

            // Refresh hero list data
            _heroList.Clear();
            var heroes = _gsm?.GetRecruitedHeroes();
            if (heroes != null) _heroList.AddRange(heroes);

            // Switch to the corresponding tab
            _selectedSlot = slot;
            HideDetail();
            RefreshAll();
        }

        // ============================================================
        // Hero Navigation
        // ============================================================

        void OnPrevHero()
        {
            if (_heroList.Count <= 0) return;
            _heroIndex = (_heroIndex - 1 + _heroList.Count) % _heroList.Count;
            HideDetail();
            RefreshAll();
        }

        void OnNextHero()
        {
            if (_heroList.Count <= 0) return;
            _heroIndex = (_heroIndex + 1) % _heroList.Count;
            HideDetail();
            RefreshAll();
        }

        // ============================================================
        // Slot Selection
        // ============================================================

        void SelectSlot(EquipSlot slot)
        {
            _selectedSlot = slot;
            HideDetail();
            HighlightTabs();
            RefreshItemList();
        }

        void HighlightTabs()
        {
            SetTabColor(_tabWeaponBtn, _selectedSlot == EquipSlot.Weapon);
            SetTabColor(_tabArmorBtn, _selectedSlot == EquipSlot.Armor);
            SetTabColor(_tabAuxBtn, _selectedSlot == EquipSlot.Auxiliary);
        }

        void SetTabColor(Button btn, bool selected)
        {
            if (btn == null) return;
            var img = btn.GetComponent<Image>();
            if (img != null)
                img.color = selected ? ThreeKingdomsTheme.SpeedActive : ThreeKingdomsTheme.ButtonNormal;
        }

        // ============================================================
        // Refresh
        // ============================================================

        void RefreshAll()
        {
            RefreshHeroInfo();
            RefreshEquipList();
            HighlightTabs();
            RefreshItemList();
            _pageLabel.text = _heroList.Count > 0
                ? $"{_heroIndex + 1}/{_heroList.Count}" : "0/0";
        }

        void RefreshHeroInfo()
        {
            if (_heroList.Count == 0)
            {
                _nameLabel.text = "无武将";
                _levelClassLabel.text = "";
                _statsLeftLabel.text = "";
                _statsRightLabel.text = "";
                _portraitImage.sprite = null;
                _portraitImage.color = new Color(1, 1, 1, 0.2f);
                return;
            }

            var hero = _heroList[_heroIndex];
            var heroDef = _registry?.GetHero(hero.heroId);
            string heroName = heroDef != null ? heroDef.displayName : hero.heroId;
            string className = heroDef?.defaultUnitType != null
                ? heroDef.defaultUnitType.displayName : "";

            _portraitImage.sprite = CampUIHelper.GetHeroPortrait(heroDef);
            _portraitImage.color = Color.white;
            _nameLabel.text = heroName;

            _levelClassLabel.text = $"等级 {hero.level}    {className}";

            int intel = heroDef?.intelligence ?? 0;

            _statsLeftLabel.text =
                $"生命 {hero.maxHp}\n" +
                $"攻击 {hero.atk}\n" +
                $"防御 {hero.def}\n" +
                $"士气 {hero.maxMp}";

            _statsRightLabel.text =
                $"策略 {hero.maxMp}\n" +
                $"谋略 {intel}\n" +
                $"灵敏 {hero.speed}\n" +
                $"移动 {hero.mov}";
        }

        void RefreshEquipList()
        {
            ClearEquipSlots();

            if (_heroList.Count == 0) return;
            var hero = _heroList[_heroIndex];

            CreateEquipRow("武器", hero.equippedWeaponId, EquipSlot.Weapon);
            CreateEquipRow("防具", hero.equippedArmorId, EquipSlot.Armor);
            CreateEquipRow("辅助", hero.equippedAuxiliaryId, EquipSlot.Auxiliary);
        }

        string GetCurrentHeroId()
        {
            if (_heroList.Count == 0 || _heroIndex >= _heroList.Count) return null;
            return _heroList[_heroIndex].heroId;
        }

        // ============================================================
        // Item List (right panel)
        // ============================================================

        void RefreshItemList()
        {
            ClearItemRows();

            string heroId = GetCurrentHeroId();
            var hero = heroId != null ? _gsm?.GetHero(heroId) : null;
            if (hero == null) return;

            // Get inventory items that fit this slot
            ItemType targetType = _selectedSlot switch
            {
                EquipSlot.Weapon => ItemType.Weapon,
                EquipSlot.Armor => ItemType.Armor,
                EquipSlot.Auxiliary => ItemType.Auxiliary,
                _ => ItemType.Weapon
            };

            var items = _gsm?.GetItemsByType(targetType);
            if (items != null)
            {
                foreach (var stack in items)
                {
                    var itemDef = _registry?.GetItem(stack.itemId);
                    if (itemDef == null) continue;
                    if (!itemDef.CanHeroEquip(heroId, hero.currentUnitTypeId)) continue;

                    string id = stack.itemId;
                    var row = CreateItemRow(itemDef, stack.count);
                    row.onClick.AddListener(() => ShowDetail(id));
                    _itemRows.Add(row);
                }
            }

            if (_itemRows.Count == 0)
            {
                _itemRows.Add(CreateEmptyRow("无可用物品"));
            }

            // Reset scroll to top and force layout
            Canvas.ForceUpdateCanvases();
            if (_scrollRect != null)
                _scrollRect.normalizedPosition = new Vector2(0, 1);
        }

        /// <summary>
        /// Creates a clearly visible item row with icon, name, and bonus text.
        /// </summary>
        Button CreateItemRow(ItemDefinition itemDef, int count)
        {
            var go = new GameObject("Item_" + itemDef.displayName);
            go.transform.SetParent(_itemListContent, false);

            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 52;
            le.minHeight = 52;

            // Visible background — distinct from panel
            var img = go.AddComponent<Image>();
            img.color = new Color(0.25f, 0.2f, 0.14f, 0.9f);

            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0.5f, 0.4f, 0.25f, 0.6f);
            outline.effectDistance = new Vector2(1, -1);

            var btn = go.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = new Color(0.25f, 0.2f, 0.14f, 0.9f);
            colors.highlightedColor = new Color(0.35f, 0.28f, 0.18f, 1f);
            colors.pressedColor = new Color(0.45f, 0.35f, 0.2f, 1f);
            colors.selectedColor = ThreeKingdomsTheme.SpeedActive;
            btn.colors = colors;
            btn.targetGraphic = img;

            float textLeft = 8f;

            // Icon
            Sprite icon = CampUIHelper.GetItemIcon(itemDef);
            if (icon != null)
            {
                var iconGo = new GameObject("Icon");
                iconGo.transform.SetParent(go.transform, false);
                var irt = iconGo.AddComponent<RectTransform>();
                irt.anchorMin = new Vector2(0, 0.05f);
                irt.anchorMax = new Vector2(0, 0.95f);
                irt.offsetMin = new Vector2(4, 0);
                irt.offsetMax = new Vector2(48, 0);
                irt.sizeDelta = new Vector2(44, 0);
                var iconImg = iconGo.AddComponent<Image>();
                iconImg.sprite = icon;
                iconImg.preserveAspect = true;
                textLeft = 54f;
            }

            // Item name (top)
            var nameGo = new GameObject("Name");
            nameGo.transform.SetParent(go.transform, false);
            var nrt = nameGo.AddComponent<RectTransform>();
            nrt.anchorMin = new Vector2(0, 0.5f);
            nrt.anchorMax = new Vector2(1, 1);
            nrt.offsetMin = new Vector2(textLeft, 0);
            nrt.offsetMax = new Vector2(-8, -2);
            var ntmp = nameGo.AddComponent<TextMeshProUGUI>();
            ntmp.text = $"{itemDef.displayName} x{count}";
            ntmp.fontSize = 15f;
            ntmp.color = ThreeKingdomsTheme.TextPrimary;
            ntmp.alignment = TextAlignmentOptions.MidlineLeft;

            // Bonus text (bottom)
            string bonus = GetBonusText(itemDef);
            if (!string.IsNullOrEmpty(bonus))
            {
                var descGo = new GameObject("Bonus");
                descGo.transform.SetParent(go.transform, false);
                var drt = descGo.AddComponent<RectTransform>();
                drt.anchorMin = new Vector2(0, 0);
                drt.anchorMax = new Vector2(1, 0.5f);
                drt.offsetMin = new Vector2(textLeft, 2);
                drt.offsetMax = new Vector2(-8, 0);
                var dtmp = descGo.AddComponent<TextMeshProUGUI>();
                dtmp.text = bonus;
                dtmp.fontSize = 12f;
                dtmp.color = new Color(0.6f, 0.85f, 0.6f, 1f);
                dtmp.alignment = TextAlignmentOptions.MidlineLeft;
            }

            return btn;
        }

        Button CreateEmptyRow(string text)
        {
            var go = new GameObject("Empty");
            go.transform.SetParent(_itemListContent, false);

            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 40;
            le.minHeight = 40;

            var img = go.AddComponent<Image>();
            img.color = new Color(0.2f, 0.16f, 0.1f, 0.7f);

            var btn = go.AddComponent<Button>();
            btn.interactable = false;
            btn.targetGraphic = img;

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(go.transform, false);
            var lrt = labelGo.AddComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = new Vector2(10, 0);
            lrt.offsetMax = new Vector2(-10, 0);
            var tmp = labelGo.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 15f;
            tmp.color = ThreeKingdomsTheme.TextSecondary;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;

            return btn;
        }

        // ============================================================
        // Item Detail Panel
        // ============================================================

        void ShowDetail(string itemId)
        {
            var itemDef = _registry?.GetItem(itemId);
            if (itemDef == null) return;

            _detailItemId = itemId;

            Sprite icon = CampUIHelper.GetItemIcon(itemDef);
            _detailIcon.sprite = icon;
            _detailIcon.color = icon != null ? Color.white : new Color(1, 1, 1, 0.2f);

            _detailName.text = itemDef.displayName;
            _detailStats.text = GetFullStatsText(itemDef);
            _detailDesc.text = !string.IsNullOrEmpty(itemDef.description)
                ? itemDef.description : "无描述";

            // Show equip button only if hero can equip
            string heroId = GetCurrentHeroId();
            _detailEquipBtn.gameObject.SetActive(
                !string.IsNullOrEmpty(heroId) && itemDef.CanHeroEquip(heroId,
                    _gsm?.GetHero(heroId)?.currentUnitTypeId));

            _scrollViewGo.SetActive(false);
            _detailPanel.SetActive(true);
        }

        void HideDetail()
        {
            if (_detailPanel != null)
                _detailPanel.SetActive(false);
            if (_scrollViewGo != null)
                _scrollViewGo.SetActive(true);
            _detailItemId = null;
        }

        void OnDetailEquip()
        {
            if (string.IsNullOrEmpty(_detailItemId)) return;

            string heroId = GetCurrentHeroId();
            if (string.IsNullOrEmpty(heroId)) return;

            bool ok = _gsm.EquipItem(heroId, _detailItemId, _selectedSlot);
            if (ok)
            {
                _heroList.Clear();
                var heroes = _gsm?.GetRecruitedHeroes();
                if (heroes != null) _heroList.AddRange(heroes);

                HideDetail();
                RefreshAll();
            }
        }

        // ============================================================
        // Helpers
        // ============================================================

        string GetBonusText(ItemDefinition item)
        {
            if (item == null) return "";
            var parts = new List<string>();
            if (item.atkBonus != 0) parts.Add($"攻击{(item.atkBonus > 0 ? "+" : "")}{item.atkBonus}");
            if (item.defBonus != 0) parts.Add($"防御{(item.defBonus > 0 ? "+" : "")}{item.defBonus}");
            if (item.speedBonus != 0) parts.Add($"灵敏{(item.speedBonus > 0 ? "+" : "")}{item.speedBonus}");
            if (item.hpBonus != 0) parts.Add($"生命{(item.hpBonus > 0 ? "+" : "")}{item.hpBonus}");
            if (item.mpBonus != 0) parts.Add($"士气{(item.mpBonus > 0 ? "+" : "")}{item.mpBonus}");
            return parts.Count > 0 ? string.Join("  ", parts) : "";
        }

        string GetFullStatsText(ItemDefinition item)
        {
            if (item == null) return "";
            var parts = new List<string>();
            if (item.atkBonus != 0) parts.Add($"攻击 {(item.atkBonus > 0 ? "+" : "")}{item.atkBonus}");
            if (item.defBonus != 0) parts.Add($"防御 {(item.defBonus > 0 ? "+" : "")}{item.defBonus}");
            if (item.speedBonus != 0) parts.Add($"灵敏 {(item.speedBonus > 0 ? "+" : "")}{item.speedBonus}");
            if (item.hpBonus != 0) parts.Add($"生命 {(item.hpBonus > 0 ? "+" : "")}{item.hpBonus}");
            if (item.mpBonus != 0) parts.Add($"士气 {(item.mpBonus > 0 ? "+" : "")}{item.mpBonus}");
            if (item.healAmount != 0) parts.Add($"回复 {item.healAmount}HP");

            string typeStr = item.itemType switch
            {
                ItemType.Weapon => "武器",
                ItemType.Armor => "防具",
                ItemType.Auxiliary => "辅助",
                ItemType.Consumable => "消耗品",
                _ => "物品"
            };

            string statsLine = parts.Count > 0 ? string.Join("  ", parts) : "无属性加成";
            return $"类型: {typeStr}\n{statsLine}\n买价: {item.buyPrice}  卖价: {item.sellPrice}";
        }

        // ============================================================
        // Cleanup
        // ============================================================

        void ClearEquipSlots()
        {
            foreach (var go in _equipSlotGos)
                if (go != null) Destroy(go);
            _equipSlotGos.Clear();
        }

        void ClearItemRows()
        {
            // Destroy ALL children of the scroll content to avoid orphan rows
            if (_itemListContent != null)
            {
                for (int i = _itemListContent.childCount - 1; i >= 0; i--)
                    Destroy(_itemListContent.GetChild(i).gameObject);
            }
            _itemRows.Clear();
        }
    }
}
