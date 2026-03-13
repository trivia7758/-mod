using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CaoCao.Common;
using CaoCao.Core;
using CaoCao.Data;
using CaoCao.UI;

namespace CaoCao.Camp
{
    /// <summary>
    /// Inventory/warehouse browser screen (仓库).
    /// Tab bar: 武器/防具/辅助/道具. Grid of items. Capacity display.
    /// </summary>
    public class WarehouseScreen : BaseScreen
    {
        [Header("Tabs")]
        [SerializeField] Button weaponTab;          // 武器
        [SerializeField] Button armorTab;           // 防具
        [SerializeField] Button auxTab;             // 辅助
        [SerializeField] Button consumableTab;      // 道具

        [Header("Item Grid")]
        [SerializeField] Transform itemGridContainer;
        [SerializeField] GameObject itemCellPrefab;

        [Header("Detail Panel")]
        [SerializeField] RectTransform detailPanel;
        [SerializeField] TMP_Text detailName;
        [SerializeField] TMP_Text detailDesc;
        [SerializeField] TMP_Text detailStats;

        [Header("Capacity")]
        [SerializeField] TMP_Text capacityLabel;

        [Header("Navigation")]
        [SerializeField] Button backButton;

        [Header("Title")]
        [SerializeField] TMP_Text titleLabel;

        /// <summary>Fired when back button is pressed.</summary>
        public event Action OnBack;

        GameStateManager _gsm;
        GameDataRegistry _registry;
        ItemType _activeTab = ItemType.Weapon;

        readonly List<GameObject> _itemCells = new();

        public override void Initialize()
        {
            _gsm = ServiceLocator.Get<GameStateManager>();
            _registry = ServiceLocator.Get<GameDataRegistry>();

            weaponTab?.onClick.AddListener(() => SelectTab(ItemType.Weapon));
            armorTab?.onClick.AddListener(() => SelectTab(ItemType.Armor));
            auxTab?.onClick.AddListener(() => SelectTab(ItemType.Auxiliary));
            consumableTab?.onClick.AddListener(() => SelectTab(ItemType.Consumable));

            backButton?.onClick.AddListener(() => OnBack?.Invoke());
            if (backButton != null) ThreeKingdomsTheme.StyleButton(backButton);

            if (titleLabel != null)
            {
                titleLabel.text = "仓库";
                titleLabel.color = ThreeKingdomsTheme.TextGold;
            }
        }

        public override void Show()
        {
            base.Show();
            _activeTab = ItemType.Weapon;
            SelectTab(_activeTab);
            UpdateCapacity();
            ClearDetail();
        }

        void SelectTab(ItemType type)
        {
            _activeTab = type;

            HighlightTab(weaponTab, type == ItemType.Weapon);
            HighlightTab(armorTab, type == ItemType.Armor);
            HighlightTab(auxTab, type == ItemType.Auxiliary);
            HighlightTab(consumableTab, type == ItemType.Consumable);

            PopulateGrid();
            ClearDetail();
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

        void PopulateGrid()
        {
            // Clear existing cells
            foreach (var go in _itemCells)
                if (go != null) Destroy(go);
            _itemCells.Clear();

            if (itemGridContainer == null || itemCellPrefab == null || _gsm == null) return;

            var items = _gsm.GetItemsByType(_activeTab);

            foreach (var stack in items)
            {
                var itemDef = _registry?.GetItem(stack.itemId);
                if (itemDef == null) continue;

                var go = Instantiate(itemCellPrefab, itemGridContainer);
                _itemCells.Add(go);

                var cell = go.GetComponent<ItemCellWidget>();
                if (cell != null)
                {
                    cell.Setup(stack.itemId, itemDef.icon, itemDef.displayName, stack.count);
                    cell.OnClicked += OnItemCellClicked;
                }
            }
        }

        void OnItemCellClicked(string itemId)
        {
            var itemDef = _registry?.GetItem(itemId);
            if (itemDef == null) return;

            int count = _gsm?.GetItemCount(itemId) ?? 0;
            ShowItemDetail(itemDef, count);
        }

        void ShowItemDetail(ItemDefinition itemDef, int count)
        {
            if (detailPanel != null)
                detailPanel.gameObject.SetActive(true);

            if (detailName != null)
            {
                detailName.text = itemDef.displayName;
                detailName.color = ThreeKingdomsTheme.TextGold;
            }

            if (detailDesc != null)
                detailDesc.text = itemDef.description;

            if (detailStats != null)
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine($"数量: {count}");

                if (itemDef.IsEquipment)
                {
                    if (itemDef.atkBonus != 0) sb.AppendLine($"攻击 {itemDef.atkBonus:+#;-#}");
                    if (itemDef.defBonus != 0) sb.AppendLine($"防御 {itemDef.defBonus:+#;-#}");
                    if (itemDef.speedBonus != 0) sb.AppendLine($"速度 {itemDef.speedBonus:+#;-#}");
                    if (itemDef.hpBonus != 0) sb.AppendLine($"HP {itemDef.hpBonus:+#;-#}");
                    if (itemDef.mpBonus != 0) sb.AppendLine($"MP {itemDef.mpBonus:+#;-#}");
                }
                else if (itemDef.healAmount > 0)
                {
                    sb.AppendLine($"回复 HP {itemDef.healAmount}");
                }

                if (itemDef.buyPrice > 0)
                    sb.AppendLine($"价格: {itemDef.buyPrice}金");

                detailStats.text = sb.ToString();
            }
        }

        void ClearDetail()
        {
            if (detailPanel != null)
                detailPanel.gameObject.SetActive(false);
        }

        void UpdateCapacity()
        {
            if (capacityLabel == null || _gsm == null) return;
            int total = _gsm.TotalItemCount;
            int capacity = _gsm.State.inventoryCapacity;
            capacityLabel.text = $"当前: {total} / 最大: {capacity}";
        }
    }
}
