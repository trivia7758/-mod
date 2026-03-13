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
    /// Warehouse/inventory screen (仓库).
    /// Tabs for item categories, item list, detail panel.
    /// </summary>
    public class CampWarehouseScreen : MonoBehaviour
    {
        public event Action OnBack;

        GameStateManager _gsm;
        GameDataRegistry _registry;

        // State
        ItemType _currentTab = ItemType.Weapon;

        // UI references
        Button _tabWeapon, _tabArmor, _tabAux, _tabConsumable;
        TMP_Text _capacityLabel;
        RectTransform _itemListContent;
        TMP_Text _detailText;
        List<Button> _itemRows = new();

        public void Build(GameStateManager gsm, GameDataRegistry registry)
        {
            _gsm = gsm;
            _registry = registry;
            BuildLayout();
        }

        public void Refresh()
        {
            UpdateCapacity();
            RefreshItemList();
        }

        void BuildLayout()
        {
            // Title + capacity
            CampUIHelper.CreateAnchoredLabel(transform, "仓库", 28f,
                new Vector2(0, 0.92f), new Vector2(0.5f, 1),
                new Vector2(20, 0), Vector2.zero,
                TextAlignmentOptions.MidlineLeft);

            var capGo = new GameObject("Capacity");
            capGo.transform.SetParent(transform, false);
            var capRt = capGo.AddComponent<RectTransform>();
            capRt.anchorMin = new Vector2(0.5f, 0.92f);
            capRt.anchorMax = new Vector2(1, 1);
            capRt.offsetMin = Vector2.zero;
            capRt.offsetMax = new Vector2(-20, 0);
            _capacityLabel = capGo.AddComponent<TextMeshProUGUI>();
            _capacityLabel.fontSize = 20f;
            _capacityLabel.color = ThreeKingdomsTheme.TextSecondary;
            _capacityLabel.alignment = TextAlignmentOptions.MidlineRight;

            // Tab bar
            var tabBar = CampUIHelper.CreateSubPanel(transform, "TabBar",
                new Vector2(0, 0.83f), new Vector2(1, 0.91f),
                new Vector2(10, 0), new Vector2(-10, 0));

            var tabHlg = tabBar.gameObject.AddComponent<HorizontalLayoutGroup>();
            tabHlg.spacing = 6;
            tabHlg.childAlignment = TextAnchor.MiddleCenter;
            tabHlg.padding = new RectOffset(8, 8, 4, 4);
            tabHlg.childForceExpandWidth = true;
            tabHlg.childForceExpandHeight = true;

            _tabWeapon = CreateTab(tabBar, "武器", ItemType.Weapon);
            _tabArmor = CreateTab(tabBar, "防具", ItemType.Armor);
            _tabAux = CreateTab(tabBar, "辅助", ItemType.Auxiliary);
            _tabConsumable = CreateTab(tabBar, "道具", ItemType.Consumable);

            // Item list (left)
            var listPanel = CampUIHelper.CreateSubPanel(transform, "ItemList",
                new Vector2(0, 0.08f), new Vector2(0.55f, 0.81f),
                new Vector2(10, 0), new Vector2(0, 0));

            var (scroll, content) = CampUIHelper.CreateScrollView(listPanel,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            _itemListContent = content;

            // Detail panel (right)
            var detailPanel = CampUIHelper.CreateSubPanel(transform, "DetailPanel",
                new Vector2(0.57f, 0.08f), new Vector2(1, 0.81f),
                new Vector2(0, 0), new Vector2(-10, 0));

            var detailGo = new GameObject("DetailText");
            detailGo.transform.SetParent(detailPanel, false);
            var drt = detailGo.AddComponent<RectTransform>();
            drt.anchorMin = Vector2.zero;
            drt.anchorMax = Vector2.one;
            drt.offsetMin = new Vector2(12, 10);
            drt.offsetMax = new Vector2(-12, -10);
            _detailText = detailGo.AddComponent<TextMeshProUGUI>();
            _detailText.fontSize = 17f;
            _detailText.color = ThreeKingdomsTheme.TextPrimary;
            _detailText.alignment = TextAlignmentOptions.TopLeft;
            _detailText.text = "选择物品查看详情";

            // Back button
            var backBtn = CampUIHelper.CreateAnchoredButton(transform, "返回",
                new Vector2(0.35f, 0), new Vector2(0.65f, 0.07f),
                new Vector2(0, 3), new Vector2(0, 0));
            backBtn.onClick.AddListener(() => OnBack?.Invoke());
        }

        Button CreateTab(Transform parent, string label, ItemType type)
        {
            var go = new GameObject("Tab_" + label);
            go.transform.SetParent(parent, false);
            go.AddComponent<LayoutElement>();

            var img = go.AddComponent<Image>();
            img.color = ThreeKingdomsTheme.ButtonNormal;

            var btn = go.AddComponent<Button>();
            ThreeKingdomsTheme.StyleButton(btn, 18f);
            btn.targetGraphic = img;

            var lblGo = new GameObject("Label");
            lblGo.transform.SetParent(go.transform, false);
            var lrt = lblGo.AddComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero;
            lrt.offsetMax = Vector2.zero;
            var tmp = lblGo.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 18f;
            tmp.color = ThreeKingdomsTheme.TextPrimary;
            tmp.alignment = TextAlignmentOptions.Center;

            btn.onClick.AddListener(() => SwitchTab(type));
            return btn;
        }

        void SwitchTab(ItemType type)
        {
            _currentTab = type;
            HighlightTabs();
            RefreshItemList();
            _detailText.text = "选择物品查看详情";
        }

        void HighlightTabs()
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

        void UpdateCapacity()
        {
            if (_capacityLabel == null || _gsm == null) return;
            int total = _gsm.TotalItemCount;
            int cap = _gsm.State.inventoryCapacity;
            _capacityLabel.text = $"容量: {total}/{cap}";
        }

        void RefreshItemList()
        {
            ClearItemRows();
            HighlightTabs();

            var items = _gsm?.GetItemsByType(_currentTab);
            if (items == null || items.Count == 0)
            {
                var emptyRow = CreateWarehouseRow("空", null, null);
                emptyRow.interactable = false;
                _itemRows.Add(emptyRow);
                return;
            }

            foreach (var stack in items)
            {
                var itemDef = _registry?.GetItem(stack.itemId);
                if (itemDef == null) continue;

                string label = $"{itemDef.displayName}  x{stack.count}";
                Sprite icon = CampUIHelper.GetItemIcon(itemDef);
                var row = CreateWarehouseRow(label, icon, stack.itemId);

                _itemRows.Add(row);
            }
        }

        /// <summary>
        /// Create a clearly visible warehouse item row with icon support.
        /// </summary>
        Button CreateWarehouseRow(string text, Sprite icon, string itemId)
        {
            var go = new GameObject("Row_" + text);
            go.transform.SetParent(_itemListContent, false);

            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 48;
            le.minHeight = 48;

            // Bright background — distinct from dark panel
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

            float textLeft = 10f;

            // Icon (if available)
            if (icon != null)
            {
                var iconGo = new GameObject("Icon");
                iconGo.transform.SetParent(go.transform, false);
                var irt = iconGo.AddComponent<RectTransform>();
                irt.anchorMin = new Vector2(0, 0.05f);
                irt.anchorMax = new Vector2(0, 0.95f);
                irt.offsetMin = new Vector2(4, 0);
                irt.offsetMax = new Vector2(44, 0);
                irt.sizeDelta = new Vector2(40, 0);
                var iconImg = iconGo.AddComponent<Image>();
                iconImg.sprite = icon;
                iconImg.preserveAspect = true;
                textLeft = 50f;
            }

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(go.transform, false);
            var lrt = labelGo.AddComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = new Vector2(textLeft, 0);
            lrt.offsetMax = new Vector2(-10, 0);

            var tmp = labelGo.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 16f;
            tmp.color = ThreeKingdomsTheme.TextPrimary;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;

            if (!string.IsNullOrEmpty(itemId))
            {
                string id = itemId;
                btn.onClick.AddListener(() => ShowItemDetail(id));
            }

            return btn;
        }

        void ShowItemDetail(string itemId)
        {
            if (_detailText == null) return;

            var item = _registry?.GetItem(itemId);
            if (item == null)
            {
                _detailText.text = "未找到物品信息";
                return;
            }

            int count = _gsm?.GetItemCount(itemId) ?? 0;

            string text = $"<color=#BF9858><size=22>{item.displayName}</size></color>\n" +
                          $"数量: {count}\n\n";

            if (!string.IsNullOrEmpty(item.description))
                text += $"{item.description}\n\n";

            if (item.IsEquipment)
            {
                text += "<color=#BF9858>装备属性:</color>\n";
                if (item.atkBonus != 0) text += $"  攻击 +{item.atkBonus}\n";
                if (item.defBonus != 0) text += $"  防御 +{item.defBonus}\n";
                if (item.speedBonus != 0) text += $"  速度 +{item.speedBonus}\n";
                if (item.hpBonus != 0) text += $"  生命 +{item.hpBonus}\n";
                if (item.mpBonus != 0) text += $"  真气 +{item.mpBonus}\n";
            }

            if (item.itemType == ItemType.Consumable && item.healAmount > 0)
                text += $"回复: HP +{item.healAmount}\n";

            if (item.buyPrice > 0)
                text += $"\n价格: {item.buyPrice} 金";

            _detailText.text = text;
        }

        void ClearItemRows()
        {
            // Destroy ALL children of scroll content to avoid orphan rows
            if (_itemListContent != null)
            {
                for (int i = _itemListContent.childCount - 1; i >= 0; i--)
                    Destroy(_itemListContent.GetChild(i).gameObject);
            }
            _itemRows.Clear();
        }
    }
}
