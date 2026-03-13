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
    /// Equipment management screen (装备).
    /// Left: hero stats + 3 equipment slots. Right: available items for selected slot.
    /// Shows stat diff when selecting equipment.
    /// </summary>
    public class EquipmentScreen : BaseScreen
    {
        [Header("Hero Info (Left Panel)")]
        [SerializeField] Image portraitImage;
        [SerializeField] TMP_Text heroNameLabel;
        [SerializeField] TMP_Text heroStatsText;

        [Header("Equipment Slots")]
        [SerializeField] Button weaponSlotBtn;
        [SerializeField] Button armorSlotBtn;
        [SerializeField] Button auxSlotBtn;
        [SerializeField] TMP_Text weaponSlotLabel;
        [SerializeField] TMP_Text armorSlotLabel;
        [SerializeField] TMP_Text auxSlotLabel;

        [Header("Item List (Right Panel)")]
        [SerializeField] Transform itemListContainer;
        [SerializeField] GameObject itemRowPrefab;
        [SerializeField] TMP_Text itemDetailText;

        [Header("Navigation")]
        [SerializeField] Button prevHeroBtn;
        [SerializeField] Button nextHeroBtn;
        [SerializeField] Button backButton;
        [SerializeField] Button unequipButton;

        [Header("Title")]
        [SerializeField] TMP_Text titleLabel;

        /// <summary>Fired when back button is pressed.</summary>
        public event Action OnBack;

        GameStateManager _gsm;
        GameDataRegistry _registry;
        List<HeroRuntimeData> _heroList;
        int _currentHeroIndex;
        EquipSlot _activeSlot = EquipSlot.Weapon;

        readonly List<GameObject> _itemRows = new();

        public override void Initialize()
        {
            _gsm = ServiceLocator.Get<GameStateManager>();
            _registry = ServiceLocator.Get<GameDataRegistry>();

            prevHeroBtn?.onClick.AddListener(() => NavigateHero(-1));
            nextHeroBtn?.onClick.AddListener(() => NavigateHero(1));
            backButton?.onClick.AddListener(() => OnBack?.Invoke());
            unequipButton?.onClick.AddListener(OnUnequipClicked);

            weaponSlotBtn?.onClick.AddListener(() => SelectSlot(EquipSlot.Weapon));
            armorSlotBtn?.onClick.AddListener(() => SelectSlot(EquipSlot.Armor));
            auxSlotBtn?.onClick.AddListener(() => SelectSlot(EquipSlot.Auxiliary));

            if (backButton != null) ThreeKingdomsTheme.StyleButton(backButton);
            if (unequipButton != null) ThreeKingdomsTheme.StyleButton(unequipButton);

            if (titleLabel != null)
            {
                titleLabel.text = "装备";
                titleLabel.color = ThreeKingdomsTheme.TextGold;
            }
        }

        public override void Show()
        {
            base.Show();
            _heroList = _gsm?.GetRecruitedHeroes();
            _currentHeroIndex = 0;
            _activeSlot = EquipSlot.Weapon;
            if (_heroList != null && _heroList.Count > 0)
                RefreshAll();
        }

        void NavigateHero(int delta)
        {
            if (_heroList == null || _heroList.Count == 0) return;
            _currentHeroIndex = (_currentHeroIndex + delta + _heroList.Count) % _heroList.Count;
            RefreshAll();
        }

        void RefreshAll()
        {
            if (_heroList == null || _currentHeroIndex >= _heroList.Count) return;

            var runtime = _heroList[_currentHeroIndex];
            var heroDef = _registry?.GetHero(runtime.heroId);
            if (heroDef == null) return;

            // Hero info
            if (portraitImage != null)
            {
                portraitImage.sprite = heroDef.portrait;
                portraitImage.enabled = heroDef.portrait != null;
            }

            if (heroNameLabel != null)
                heroNameLabel.text = $"{heroDef.displayName}  Lv.{runtime.level}";

            RefreshHeroStats(runtime);
            RefreshSlotLabels(runtime);
            SelectSlot(_activeSlot);
        }

        void RefreshHeroStats(HeroRuntimeData runtime)
        {
            if (heroStatsText != null)
            {
                heroStatsText.text =
                    $"HP: {runtime.currentHp}/{runtime.maxHp}\n" +
                    $"MP: {runtime.currentMp}/{runtime.maxMp}\n" +
                    $"攻击: {runtime.atk}  防御: {runtime.def}\n" +
                    $"速度: {runtime.speed}  移动: {runtime.mov}";
            }
        }

        void RefreshSlotLabels(HeroRuntimeData runtime)
        {
            SetSlotLabel(weaponSlotLabel, runtime.equippedWeaponId, "武器");
            SetSlotLabel(armorSlotLabel, runtime.equippedArmorId, "防具");
            SetSlotLabel(auxSlotLabel, runtime.equippedAuxiliaryId, "辅助");
        }

        void SetSlotLabel(TMP_Text label, string itemId, string slotName)
        {
            if (label == null) return;
            if (string.IsNullOrEmpty(itemId))
            {
                label.text = $"{slotName}: --";
            }
            else
            {
                var itemDef = _registry?.GetItem(itemId);
                label.text = $"{slotName}: {itemDef?.displayName ?? itemId}";
            }
        }

        void SelectSlot(EquipSlot slot)
        {
            _activeSlot = slot;

            // Highlight active slot button
            HighlightSlotBtn(weaponSlotBtn, slot == EquipSlot.Weapon);
            HighlightSlotBtn(armorSlotBtn, slot == EquipSlot.Armor);
            HighlightSlotBtn(auxSlotBtn, slot == EquipSlot.Auxiliary);

            PopulateItemList(slot);
        }

        void HighlightSlotBtn(Button btn, bool active)
        {
            if (btn == null) return;
            var img = btn.GetComponent<Image>();
            if (img != null)
                img.color = active
                    ? ThreeKingdomsTheme.SpeedActive
                    : ThreeKingdomsTheme.SpeedInactive;
        }

        void PopulateItemList(EquipSlot slot)
        {
            // Clear existing rows
            foreach (var go in _itemRows)
                if (go != null) Destroy(go);
            _itemRows.Clear();

            if (itemDetailText != null)
                itemDetailText.text = "";

            if (itemListContainer == null || itemRowPrefab == null || _gsm == null) return;

            // Get matching item type
            ItemType matchingType = slot switch
            {
                EquipSlot.Weapon => ItemType.Weapon,
                EquipSlot.Armor => ItemType.Armor,
                EquipSlot.Auxiliary => ItemType.Auxiliary,
                _ => ItemType.Weapon
            };

            var runtime = _heroList[_currentHeroIndex];
            var items = _gsm.GetItemsByType(matchingType);

            foreach (var stack in items)
            {
                var itemDef = _registry?.GetItem(stack.itemId);
                if (itemDef == null) continue;
                if (!itemDef.CanHeroEquip(runtime.heroId)) continue;

                var go = Instantiate(itemRowPrefab, itemListContainer);
                _itemRows.Add(go);

                var cell = go.GetComponent<ItemCellWidget>();
                if (cell != null)
                {
                    cell.Setup(stack.itemId, itemDef.icon, itemDef.displayName, stack.count);
                    cell.OnClicked += OnItemClicked;
                }
            }
        }

        void OnItemClicked(string itemId)
        {
            if (_heroList == null || _currentHeroIndex >= _heroList.Count) return;

            var runtime = _heroList[_currentHeroIndex];
            var itemDef = _registry?.GetItem(itemId);

            // Show stat diff
            if (itemDetailText != null && itemDef != null)
            {
                itemDetailText.text =
                    $"{itemDef.displayName}\n{itemDef.description}\n" +
                    FormatBonus("攻击", itemDef.atkBonus) +
                    FormatBonus("防御", itemDef.defBonus) +
                    FormatBonus("速度", itemDef.speedBonus) +
                    FormatBonus("HP", itemDef.hpBonus) +
                    FormatBonus("MP", itemDef.mpBonus);
            }

            // Actually equip the item
            if (_gsm != null)
            {
                _gsm.EquipItem(runtime.heroId, itemId, _activeSlot);

                // Refresh hero list (stats changed)
                _heroList = _gsm.GetRecruitedHeroes();
                RefreshAll();
            }
        }

        void OnUnequipClicked()
        {
            if (_heroList == null || _currentHeroIndex >= _heroList.Count) return;

            var runtime = _heroList[_currentHeroIndex];
            _gsm?.UnequipItem(runtime.heroId, _activeSlot);

            _heroList = _gsm?.GetRecruitedHeroes();
            RefreshAll();
        }

        string FormatBonus(string name, int value)
        {
            if (value == 0) return "";
            string color = value > 0 ? "#4CAF50" : "#F44336";
            return $"<color={color}>{name} {value:+#;-#}</color>  ";
        }
    }
}
