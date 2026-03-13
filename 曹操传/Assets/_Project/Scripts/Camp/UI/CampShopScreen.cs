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
    /// Shop screen (商店). Buy items from registry, sell items from inventory.
    /// </summary>
    public class CampShopScreen : MonoBehaviour
    {
        public event Action OnBack;

        GameStateManager _gsm;
        GameDataRegistry _registry;

        // State
        bool _buyMode = true; // true=购买, false=出售
        ItemType _currentTab = ItemType.Weapon;

        // UI references
        Button _tabBuy, _tabSell;
        Button _tabWeapon, _tabArmor, _tabAux, _tabConsumable;
        TMP_Text _goldLabel;
        TMP_Text _capacityLabel;
        ScrollRect _scrollRect;
        RectTransform _itemListContent;
        List<Button> _itemRows = new();

        // Detail panel
        GameObject _detailPanel;
        Image _detailIcon;
        TMP_Text _detailName;
        TMP_Text _detailStats;
        TMP_Text _detailDesc;
        TMP_Text _detailPrice;
        Button _detailActionBtn;
        TMP_Text _detailActionLabel;
        Button _detailCloseBtn;
        string _detailItemId;

        // Scroll view container (to hide when showing detail)
        GameObject _scrollViewGo;

        // Colors
        static readonly Color GoldTitle = new(0.75f, 0.6f, 0.3f, 1f);
        static readonly Color BuyModeColor = new(0.2f, 0.45f, 0.2f, 0.9f);
        static readonly Color SellModeColor = new(0.5f, 0.25f, 0.15f, 0.9f);

        public void Build(GameStateManager gsm, GameDataRegistry registry)
        {
            _gsm = gsm;
            _registry = registry;
            BuildLayout();
        }

        public void Refresh()
        {
            HideDetail();
            UpdateGold();
            UpdateCapacity();
            HighlightModeTabs();
            HighlightTypeTabs();
            RefreshItemList();
        }

        // ============================================================
        // Layout
        // ============================================================

        void BuildLayout()
        {
            // ── Title ──
            CampUIHelper.CreateAnchoredLabel(transform, "商 店", 28f,
                new Vector2(0, 0.92f), new Vector2(0.35f, 1),
                new Vector2(20, 0), Vector2.zero,
                TextAlignmentOptions.MidlineLeft);

            // ── Gold display ──
            _goldLabel = CampUIHelper.CreateAnchoredLabel(transform, "金币: 0", 20f,
                new Vector2(0.5f, 0.92f), new Vector2(0.78f, 1),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.MidlineRight);
            _goldLabel.color = GoldTitle;

            // ── Capacity ──
            _capacityLabel = CampUIHelper.CreateAnchoredLabel(transform, "", 16f,
                new Vector2(0.78f, 0.92f), new Vector2(1, 1),
                Vector2.zero, new Vector2(-10, 0),
                TextAlignmentOptions.MidlineRight);
            _capacityLabel.color = ThreeKingdomsTheme.TextSecondary;

            // ── Buy/Sell mode tabs ──
            _tabBuy = CampUIHelper.CreateAnchoredButton(transform, "购买",
                new Vector2(0.02f, 0.85f), new Vector2(0.18f, 0.92f),
                Vector2.zero, Vector2.zero, 18f);
            _tabBuy.onClick.AddListener(() => SwitchMode(true));

            _tabSell = CampUIHelper.CreateAnchoredButton(transform, "出售",
                new Vector2(0.20f, 0.85f), new Vector2(0.36f, 0.92f),
                Vector2.zero, Vector2.zero, 18f);
            _tabSell.onClick.AddListener(() => SwitchMode(false));

            // ── Category tabs ──
            _tabWeapon = CampUIHelper.CreateAnchoredButton(transform, "武器",
                new Vector2(0.40f, 0.85f), new Vector2(0.54f, 0.92f),
                Vector2.zero, Vector2.zero, 16f);
            _tabWeapon.onClick.AddListener(() => SwitchTab(ItemType.Weapon));

            _tabArmor = CampUIHelper.CreateAnchoredButton(transform, "防具",
                new Vector2(0.56f, 0.85f), new Vector2(0.70f, 0.92f),
                Vector2.zero, Vector2.zero, 16f);
            _tabArmor.onClick.AddListener(() => SwitchTab(ItemType.Armor));

            _tabAux = CampUIHelper.CreateAnchoredButton(transform, "辅助",
                new Vector2(0.72f, 0.85f), new Vector2(0.86f, 0.92f),
                Vector2.zero, Vector2.zero, 16f);
            _tabAux.onClick.AddListener(() => SwitchTab(ItemType.Auxiliary));

            _tabConsumable = CampUIHelper.CreateAnchoredButton(transform, "道具",
                new Vector2(0.88f, 0.85f), new Vector2(0.99f, 0.92f),
                Vector2.zero, Vector2.zero, 16f);
            _tabConsumable.onClick.AddListener(() => SwitchTab(ItemType.Consumable));

            // ── Item list panel (left) ──
            var listPanel = CampUIHelper.CreateSubPanel(transform, "ListPanel",
                new Vector2(0.02f, 0.08f), new Vector2(0.52f, 0.83f),
                Vector2.zero, Vector2.zero);

            var (scroll, content) = CampUIHelper.CreateScrollView(listPanel,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            _scrollRect = scroll;
            _itemListContent = content;
            _scrollViewGo = scroll.gameObject;

            // ── Detail panel (right) ──
            BuildDetailPanel();

            // ── Back button ──
            var backBtn = CampUIHelper.CreateAnchoredButton(transform, "返回",
                new Vector2(0.35f, 0.01f), new Vector2(0.65f, 0.07f),
                Vector2.zero, Vector2.zero);
            backBtn.onClick.AddListener(() => OnBack?.Invoke());
        }

        void BuildDetailPanel()
        {
            _detailPanel = CampUIHelper.CreateSubPanel(transform, "DetailPanel",
                new Vector2(0.54f, 0.08f), new Vector2(0.98f, 0.83f),
                Vector2.zero, Vector2.zero).gameObject;

            var dp = _detailPanel.transform;

            // Default text
            var defaultLabel = CampUIHelper.CreateAnchoredLabel(dp, "选择物品查看详情", 17f,
                new Vector2(0, 0), new Vector2(1, 1),
                new Vector2(12, 10), new Vector2(-12, -10),
                TextAlignmentOptions.Center);
            defaultLabel.color = ThreeKingdomsTheme.TextSecondary;
            defaultLabel.gameObject.name = "DefaultText";

            // ── Icon background ──
            var iconBg = new GameObject("IconBg");
            iconBg.transform.SetParent(dp, false);
            var ibrt = iconBg.AddComponent<RectTransform>();
            ibrt.anchorMin = new Vector2(0.05f, 0.62f);
            ibrt.anchorMax = new Vector2(0.35f, 0.93f);
            ibrt.offsetMin = Vector2.zero;
            ibrt.offsetMax = Vector2.zero;
            iconBg.AddComponent<Image>().color = new Color(0.18f, 0.15f, 0.1f, 0.8f);
            iconBg.SetActive(false);

            var iconGo = new GameObject("Icon");
            iconGo.transform.SetParent(iconBg.transform, false);
            var irt = iconGo.AddComponent<RectTransform>();
            irt.anchorMin = new Vector2(0.05f, 0.05f);
            irt.anchorMax = new Vector2(0.95f, 0.95f);
            irt.offsetMin = Vector2.zero;
            irt.offsetMax = Vector2.zero;
            _detailIcon = iconGo.AddComponent<Image>();
            _detailIcon.preserveAspect = true;

            // ── Name ──
            _detailName = CampUIHelper.CreateAnchoredLabel(dp, "", 20f,
                new Vector2(0.38f, 0.8f), new Vector2(0.95f, 0.93f),
                new Vector2(5, 0), Vector2.zero,
                TextAlignmentOptions.MidlineLeft);
            _detailName.color = GoldTitle;
            _detailName.fontStyle = FontStyles.Bold;
            _detailName.gameObject.SetActive(false);

            // ── Stats ──
            _detailStats = CampUIHelper.CreateAnchoredLabel(dp, "", 15f,
                new Vector2(0.38f, 0.62f), new Vector2(0.95f, 0.8f),
                new Vector2(5, 0), Vector2.zero,
                TextAlignmentOptions.TopLeft);
            _detailStats.color = new Color(0.6f, 0.85f, 0.6f, 1f);
            _detailStats.gameObject.SetActive(false);

            // ── Description ──
            _detailDesc = CampUIHelper.CreateAnchoredLabel(dp, "", 14f,
                new Vector2(0.05f, 0.38f), new Vector2(0.95f, 0.6f),
                new Vector2(5, 5), new Vector2(-5, -5),
                TextAlignmentOptions.TopLeft);
            _detailDesc.color = ThreeKingdomsTheme.TextSecondary;
            _detailDesc.gameObject.SetActive(false);

            // ── Price ──
            _detailPrice = CampUIHelper.CreateAnchoredLabel(dp, "", 18f,
                new Vector2(0.05f, 0.26f), new Vector2(0.95f, 0.38f),
                new Vector2(5, 0), Vector2.zero,
                TextAlignmentOptions.MidlineLeft);
            _detailPrice.color = GoldTitle;
            _detailPrice.gameObject.SetActive(false);

            // ── Action button (buy/sell) ──
            _detailActionBtn = CampUIHelper.CreateAnchoredButton(dp, "购买",
                new Vector2(0.08f, 0.06f), new Vector2(0.52f, 0.22f),
                Vector2.zero, Vector2.zero, 18f);
            _detailActionBtn.onClick.AddListener(OnActionClicked);
            _detailActionLabel = _detailActionBtn.GetComponentInChildren<TMP_Text>();
            _detailActionBtn.gameObject.SetActive(false);

            // ── Close button ──
            _detailCloseBtn = CampUIHelper.CreateAnchoredButton(dp, "关闭",
                new Vector2(0.56f, 0.06f), new Vector2(0.92f, 0.22f),
                Vector2.zero, Vector2.zero, 18f);
            _detailCloseBtn.onClick.AddListener(HideDetail);
            _detailCloseBtn.gameObject.SetActive(false);
        }

        // ============================================================
        // Mode & Tab Switching
        // ============================================================

        void SwitchMode(bool buyMode)
        {
            _buyMode = buyMode;
            HideDetail();
            HighlightModeTabs();
            RefreshItemList();
        }

        void SwitchTab(ItemType type)
        {
            _currentTab = type;
            HideDetail();
            HighlightTypeTabs();
            RefreshItemList();
        }

        void HighlightModeTabs()
        {
            SetTabColor(_tabBuy, _buyMode);
            SetTabColor(_tabSell, !_buyMode);
        }

        void HighlightTypeTabs()
        {
            SetTabColor(_tabWeapon, _currentTab == ItemType.Weapon);
            SetTabColor(_tabArmor, _currentTab == ItemType.Armor);
            SetTabColor(_tabAux, _currentTab == ItemType.Auxiliary);
            SetTabColor(_tabConsumable, _currentTab == ItemType.Consumable);
        }

        void SetTabColor(Button tab, bool active)
        {
            if (tab == null) return;
            var img = tab.GetComponent<Image>();
            if (img != null)
                img.color = active ? ThreeKingdomsTheme.SpeedActive : ThreeKingdomsTheme.ButtonNormal;
        }

        void UpdateGold()
        {
            if (_goldLabel != null && _gsm != null)
                _goldLabel.text = $"金币: {_gsm.Gold}";
        }

        void UpdateCapacity()
        {
            if (_capacityLabel != null && _gsm != null)
                _capacityLabel.text = $"背包 {_gsm.TotalItemCount}/{_gsm.State.inventoryCapacity}";
        }

        // ============================================================
        // Item List
        // ============================================================

        void RefreshItemList()
        {
            ClearItemRows();

            if (_buyMode)
                RefreshBuyList();
            else
                RefreshSellList();

            // Reset scroll
            Canvas.ForceUpdateCanvases();
            if (_scrollRect != null)
                _scrollRect.normalizedPosition = new Vector2(0, 1);
        }

        void RefreshBuyList()
        {
            var allItems = _registry?.GetAllItems();
            if (allItems == null) return;

            foreach (var itemDef in allItems)
            {
                if (itemDef == null) continue;
                if (itemDef.itemType != _currentTab) continue;
                if (itemDef.buyPrice <= 0) continue;

                string id = itemDef.id;
                var row = CreateShopRow(itemDef, -1, itemDef.buyPrice, "金");
                row.onClick.AddListener(() => ShowDetail(id));
                _itemRows.Add(row);
            }

            if (_itemRows.Count == 0)
                _itemRows.Add(CreateEmptyRow("无可购买物品"));
        }

        void RefreshSellList()
        {
            var items = _gsm?.GetItemsByType(_currentTab);
            if (items == null || items.Count == 0)
            {
                _itemRows.Add(CreateEmptyRow("无可出售物品"));
                return;
            }

            foreach (var stack in items)
            {
                var itemDef = _registry?.GetItem(stack.itemId);
                if (itemDef == null) continue;
                if (itemDef.sellPrice <= 0) continue;

                string id = stack.itemId;
                var row = CreateShopRow(itemDef, stack.count, itemDef.sellPrice, "金");
                row.onClick.AddListener(() => ShowDetail(id));
                _itemRows.Add(row);
            }

            if (_itemRows.Count == 0)
                _itemRows.Add(CreateEmptyRow("无可出售物品"));
        }

        /// <summary>
        /// Create a shop item row with icon, name, count (if selling), and price.
        /// </summary>
        Button CreateShopRow(ItemDefinition itemDef, int count, int price, string currency)
        {
            string countStr = count > 0 ? $" x{count}" : "";
            var go = new GameObject("Shop_" + itemDef.displayName);
            go.transform.SetParent(_itemListContent, false);

            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 52;
            le.minHeight = 52;

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

            // Name (top)
            var nameGo = new GameObject("Name");
            nameGo.transform.SetParent(go.transform, false);
            var nrt = nameGo.AddComponent<RectTransform>();
            nrt.anchorMin = new Vector2(0, 0.5f);
            nrt.anchorMax = new Vector2(0.65f, 1);
            nrt.offsetMin = new Vector2(textLeft, 0);
            nrt.offsetMax = new Vector2(0, -2);
            var ntmp = nameGo.AddComponent<TextMeshProUGUI>();
            ntmp.text = $"{itemDef.displayName}{countStr}";
            ntmp.fontSize = 15f;
            ntmp.color = ThreeKingdomsTheme.TextPrimary;
            ntmp.alignment = TextAlignmentOptions.MidlineLeft;

            // Bonus (bottom-left)
            string bonus = GetBonusText(itemDef);
            if (!string.IsNullOrEmpty(bonus))
            {
                var descGo = new GameObject("Bonus");
                descGo.transform.SetParent(go.transform, false);
                var drt = descGo.AddComponent<RectTransform>();
                drt.anchorMin = new Vector2(0, 0);
                drt.anchorMax = new Vector2(0.65f, 0.5f);
                drt.offsetMin = new Vector2(textLeft, 2);
                drt.offsetMax = Vector2.zero;
                var dtmp = descGo.AddComponent<TextMeshProUGUI>();
                dtmp.text = bonus;
                dtmp.fontSize = 12f;
                dtmp.color = new Color(0.6f, 0.85f, 0.6f, 1f);
                dtmp.alignment = TextAlignmentOptions.MidlineLeft;
            }

            // Price (right side)
            var priceGo = new GameObject("Price");
            priceGo.transform.SetParent(go.transform, false);
            var prt = priceGo.AddComponent<RectTransform>();
            prt.anchorMin = new Vector2(0.65f, 0);
            prt.anchorMax = new Vector2(1, 1);
            prt.offsetMin = Vector2.zero;
            prt.offsetMax = new Vector2(-8, 0);
            var ptmp = priceGo.AddComponent<TextMeshProUGUI>();
            ptmp.text = $"{price}{currency}";
            ptmp.fontSize = 16f;
            ptmp.color = GoldTitle;
            ptmp.alignment = TextAlignmentOptions.MidlineRight;

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
        // Detail Panel
        // ============================================================

        void ShowDetail(string itemId)
        {
            var itemDef = _registry?.GetItem(itemId);
            if (itemDef == null) return;

            _detailItemId = itemId;

            // Hide default text, show detail elements
            var defaultText = _detailPanel.transform.Find("DefaultText");
            if (defaultText != null) defaultText.gameObject.SetActive(false);

            // Icon
            var iconBg = _detailPanel.transform.Find("IconBg");
            if (iconBg != null) iconBg.gameObject.SetActive(true);
            Sprite icon = CampUIHelper.GetItemIcon(itemDef);
            _detailIcon.sprite = icon;
            _detailIcon.color = icon != null ? Color.white : new Color(1, 1, 1, 0.2f);

            // Name
            _detailName.gameObject.SetActive(true);
            _detailName.text = itemDef.displayName;

            // Stats
            _detailStats.gameObject.SetActive(true);
            _detailStats.text = GetFullStatsText(itemDef);

            // Description
            _detailDesc.gameObject.SetActive(true);
            _detailDesc.text = !string.IsNullOrEmpty(itemDef.description) ? itemDef.description : "";

            // Price
            _detailPrice.gameObject.SetActive(true);
            int price = _buyMode ? itemDef.buyPrice : itemDef.sellPrice;
            string action = _buyMode ? "购买价格" : "出售价格";
            _detailPrice.text = $"{action}: {price} 金";

            // Action button
            _detailActionBtn.gameObject.SetActive(true);
            if (_detailActionLabel != null)
                _detailActionLabel.text = _buyMode ? "购买" : "出售";

            // Color the action button based on mode
            var actionImg = _detailActionBtn.GetComponent<Image>();
            if (actionImg != null)
                actionImg.color = _buyMode ? BuyModeColor : SellModeColor;

            // Check if action is possible
            if (_buyMode)
            {
                bool canAfford = _gsm.Gold >= itemDef.buyPrice;
                bool hasSpace = _gsm.TotalItemCount < _gsm.State.inventoryCapacity;
                _detailActionBtn.interactable = canAfford && hasSpace;
            }
            else
            {
                _detailActionBtn.interactable = _gsm.GetItemCount(itemId) > 0;
            }

            _detailCloseBtn.gameObject.SetActive(true);
        }

        void HideDetail()
        {
            _detailItemId = null;

            // Show default text, hide detail elements
            var defaultText = _detailPanel.transform.Find("DefaultText");
            if (defaultText != null) defaultText.gameObject.SetActive(true);

            var iconBg = _detailPanel.transform.Find("IconBg");
            if (iconBg != null) iconBg.gameObject.SetActive(false);
            _detailName.gameObject.SetActive(false);
            _detailStats.gameObject.SetActive(false);
            _detailDesc.gameObject.SetActive(false);
            _detailPrice.gameObject.SetActive(false);
            _detailActionBtn.gameObject.SetActive(false);
            _detailCloseBtn.gameObject.SetActive(false);
        }

        void OnActionClicked()
        {
            if (string.IsNullOrEmpty(_detailItemId)) return;

            var itemDef = _registry?.GetItem(_detailItemId);
            if (itemDef == null) return;

            if (_buyMode)
            {
                if (!_gsm.SpendGold(itemDef.buyPrice))
                {
                    Debug.Log("[Shop] Not enough gold");
                    return;
                }
                if (!_gsm.AddItem(_detailItemId))
                {
                    // Refund gold if inventory full
                    _gsm.AddGold(itemDef.buyPrice);
                    Debug.Log("[Shop] Inventory full");
                    return;
                }
                Debug.Log($"[Shop] Bought {itemDef.displayName} for {itemDef.buyPrice} gold");
            }
            else
            {
                if (!_gsm.RemoveItem(_detailItemId))
                {
                    Debug.Log("[Shop] Item not in inventory");
                    return;
                }
                _gsm.AddGold(itemDef.sellPrice);
                Debug.Log($"[Shop] Sold {itemDef.displayName} for {itemDef.sellPrice} gold");
            }

            UpdateGold();
            UpdateCapacity();
            RefreshItemList();

            // Re-show detail if item still relevant
            if (_buyMode)
                ShowDetail(_detailItemId);
            else if (_gsm.GetItemCount(_detailItemId) > 0)
                ShowDetail(_detailItemId);
            else
                HideDetail();
        }

        // ============================================================
        // Helpers
        // ============================================================

        string GetBonusText(ItemDefinition item)
        {
            if (item == null) return "";
            var parts = new List<string>();
            if (item.atkBonus != 0) parts.Add($"攻+{item.atkBonus}");
            if (item.defBonus != 0) parts.Add($"防+{item.defBonus}");
            if (item.speedBonus != 0) parts.Add($"速+{item.speedBonus}");
            if (item.hpBonus != 0) parts.Add($"HP+{item.hpBonus}");
            if (item.mpBonus != 0) parts.Add($"MP+{item.mpBonus}");
            if (item.healAmount != 0) parts.Add($"回复{item.healAmount}");
            return parts.Count > 0 ? string.Join(" ", parts) : "";
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
            return $"类型: {typeStr}\n{statsLine}";
        }

        void ClearItemRows()
        {
            if (_itemListContent != null)
            {
                for (int i = _itemListContent.childCount - 1; i >= 0; i--)
                    Destroy(_itemListContent.GetChild(i).gameObject);
            }
            _itemRows.Clear();
        }
    }
}
