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
    /// Character detail screen (武将).
    /// Shows hero portrait, stat bars, base attributes, equipment, and tabbed info.
    /// Tabs: 能力(ability), 兵种(unit type), 策略(skills), 特技(passives).
    /// </summary>
    public class HeroInfoScreen : BaseScreen
    {
        [Header("Identity")]
        [SerializeField] Image portraitImage;
        [SerializeField] TMP_Text nameLabel;
        [SerializeField] TMP_Text levelLabel;
        [SerializeField] TMP_Text unitTypeLabel;

        [Header("Stat Bars")]
        [SerializeField] StatBar hpBar;
        [SerializeField] StatBar mpBar;
        [SerializeField] StatBar expBar;

        [Header("Combat Stats")]
        [SerializeField] TMP_Text atkText;
        [SerializeField] TMP_Text defText;
        [SerializeField] TMP_Text spdText;
        [SerializeField] TMP_Text movText;

        [Header("Base Attributes (五维)")]
        [SerializeField] TMP_Text forceText;            // 武力
        [SerializeField] TMP_Text intelligenceText;     // 智力
        [SerializeField] TMP_Text commandText;          // 统帅
        [SerializeField] TMP_Text agilityText;          // 敏捷
        [SerializeField] TMP_Text luckText;             // 气运
        [SerializeField] TMP_Text breakthroughText;     // 成长

        [Header("Tabs")]
        [SerializeField] Button abilityTab;             // 能力
        [SerializeField] Button unitTypeTab;            // 兵种
        [SerializeField] Button skillsTab;              // 策略
        [SerializeField] Button passivesTab;            // 特技
        [SerializeField] RectTransform tabContentPanel;
        [SerializeField] TMP_Text tabContentText;

        [Header("Equipment Display")]
        [SerializeField] TMP_Text weaponText;
        [SerializeField] TMP_Text armorText;
        [SerializeField] TMP_Text auxText;

        [Header("Navigation")]
        [SerializeField] Button prevHeroButton;
        [SerializeField] Button nextHeroButton;
        [SerializeField] Button backButton;

        [Header("Title")]
        [SerializeField] TMP_Text titleLabel;

        /// <summary>Fired when back button is pressed.</summary>
        public event Action OnBack;

        GameStateManager _gsm;
        GameDataRegistry _registry;
        List<HeroRuntimeData> _heroList;
        int _currentIndex;
        string _activeTab = "ability";

        public override void Initialize()
        {
            _gsm = ServiceLocator.Get<GameStateManager>();
            _registry = ServiceLocator.Get<GameDataRegistry>();

            prevHeroButton?.onClick.AddListener(() => NavigateHero(-1));
            nextHeroButton?.onClick.AddListener(() => NavigateHero(1));
            backButton?.onClick.AddListener(() => OnBack?.Invoke());

            abilityTab?.onClick.AddListener(() => ShowTab("ability"));
            unitTypeTab?.onClick.AddListener(() => ShowTab("unit_type"));
            skillsTab?.onClick.AddListener(() => ShowTab("skills"));
            passivesTab?.onClick.AddListener(() => ShowTab("passives"));

            if (backButton != null) ThreeKingdomsTheme.StyleButton(backButton);

            if (titleLabel != null)
            {
                titleLabel.text = "武将";
                titleLabel.color = ThreeKingdomsTheme.TextGold;
            }
        }

        public override void Show()
        {
            base.Show();
            _heroList = _gsm?.GetRecruitedHeroes();
            _currentIndex = 0;
            _activeTab = "ability";
            if (_heroList != null && _heroList.Count > 0)
                ShowHero(0);
        }

        void NavigateHero(int delta)
        {
            if (_heroList == null || _heroList.Count == 0) return;
            _currentIndex = (_currentIndex + delta + _heroList.Count) % _heroList.Count;
            ShowHero(_currentIndex);
        }

        void ShowHero(int index)
        {
            if (_heroList == null || index < 0 || index >= _heroList.Count) return;

            _currentIndex = index;
            var runtime = _heroList[index];
            var heroDef = _registry?.GetHero(runtime.heroId);
            if (heroDef == null) return;

            RefreshIdentity(runtime, heroDef);
            RefreshStats(runtime, heroDef);
            RefreshAttributes(runtime, heroDef);
            RefreshEquipment(runtime);
            ShowTab(_activeTab);
        }

        void RefreshIdentity(HeroRuntimeData runtime, HeroDefinition heroDef)
        {
            if (portraitImage != null)
            {
                portraitImage.sprite = heroDef.portrait;
                portraitImage.enabled = heroDef.portrait != null;
            }

            if (nameLabel != null)
                nameLabel.text = heroDef.displayName;

            if (levelLabel != null)
                levelLabel.text = $"Lv.{runtime.level}";

            if (unitTypeLabel != null)
            {
                var unitType = _registry?.GetUnitType(runtime.currentUnitTypeId);
                unitTypeLabel.text = unitType != null ? unitType.displayName : "";
            }
        }

        void RefreshStats(HeroRuntimeData runtime, HeroDefinition heroDef)
        {
            hpBar?.SetValue(runtime.currentHp, runtime.maxHp, "HP");
            mpBar?.SetValue(runtime.currentMp, runtime.maxMp, "MP");

            // Exp bar: current exp / exp to next level (simplified)
            int expToNext = runtime.level * 100;
            expBar?.SetValue(runtime.exp, expToNext, "Exp");

            if (atkText != null) atkText.text = $"攻击: {runtime.atk}";
            if (defText != null) defText.text = $"防御: {runtime.def}";
            if (spdText != null) spdText.text = $"速度: {runtime.speed}";
            if (movText != null) movText.text = $"移动: {runtime.mov}";
        }

        void RefreshAttributes(HeroRuntimeData runtime, HeroDefinition heroDef)
        {
            // Use runtime five dimensions (which grow on level up)
            int f = runtime?.force > 0 ? runtime.force : heroDef.force;
            int intel = runtime?.intelligence > 0 ? runtime.intelligence : heroDef.intelligence;
            int cmd = runtime?.command > 0 ? runtime.command : heroDef.command;
            int agi = runtime?.agility > 0 ? runtime.agility : heroDef.agility;
            int lk = runtime?.luck > 0 ? runtime.luck : heroDef.luck;

            if (forceText != null) forceText.text = $"武力  {f}";
            if (intelligenceText != null) intelligenceText.text = $"智力  {intel}";
            if (commandText != null) commandText.text = $"统帅  {cmd}";
            if (agilityText != null) agilityText.text = $"敏捷  {agi}";
            if (luckText != null) luckText.text = $"气运  {lk}";
            if (breakthroughText != null) breakthroughText.text = "";
        }

        void RefreshEquipment(HeroRuntimeData runtime)
        {
            SetEquipText(weaponText, runtime.equippedWeaponId, "武器");
            SetEquipText(armorText, runtime.equippedArmorId, "防具");
            SetEquipText(auxText, runtime.equippedAuxiliaryId, "辅助");
        }

        void SetEquipText(TMP_Text label, string itemId, string slotName)
        {
            if (label == null) return;
            if (string.IsNullOrEmpty(itemId))
            {
                label.text = $"{slotName}: --";
                label.color = ThreeKingdomsTheme.TextSecondary;
            }
            else
            {
                var itemDef = _registry?.GetItem(itemId);
                label.text = $"{slotName}: {itemDef?.displayName ?? itemId}";
                label.color = ThreeKingdomsTheme.TextPrimary;
            }
        }

        void ShowTab(string tabName)
        {
            _activeTab = tabName;

            // Highlight active tab button
            HighlightTab(abilityTab, tabName == "ability");
            HighlightTab(unitTypeTab, tabName == "unit_type");
            HighlightTab(skillsTab, tabName == "skills");
            HighlightTab(passivesTab, tabName == "passives");

            if (tabContentText == null) return;

            var runtime = _heroList != null && _currentIndex < _heroList.Count
                ? _heroList[_currentIndex] : null;
            var heroDef = runtime != null ? _registry?.GetHero(runtime.heroId) : null;

            switch (tabName)
            {
                case "ability":
                    ShowAbilityTab(runtime, heroDef);
                    break;
                case "unit_type":
                    ShowUnitTypeTab(runtime, heroDef);
                    break;
                case "skills":
                    ShowSkillsTab(heroDef);
                    break;
                case "passives":
                    ShowPassivesTab(heroDef);
                    break;
            }
        }

        void ShowAbilityTab(HeroRuntimeData runtime, HeroDefinition heroDef)
        {
            if (runtime == null || heroDef == null) { tabContentText.text = ""; return; }
            tabContentText.text =
                $"HP: {runtime.currentHp}/{runtime.maxHp}\n" +
                $"MP: {runtime.currentMp}/{runtime.maxMp}\n" +
                $"攻击: {runtime.atk}  防御: {runtime.def}\n" +
                $"速度: {runtime.speed}  移动: {runtime.mov}";
        }

        void ShowUnitTypeTab(HeroRuntimeData runtime, HeroDefinition heroDef)
        {
            if (heroDef == null) { tabContentText.text = ""; return; }
            var unitType = _registry?.GetUnitType(runtime?.currentUnitTypeId ?? "");
            if (unitType != null)
            {
                string upgradeInfo = unitType.upgradeTo != null
                    ? $"\n升级: {unitType.upgradeTo.displayName} (Lv.{unitType.upgradeLevel})"
                    : "\n(已满级)";
                tabContentText.text =
                    $"兵种: {unitType.displayName}\n" +
                    $"攻击修正: {unitType.atkModifier:+#;-#;0}\n" +
                    $"防御修正: {unitType.defModifier:+#;-#;0}\n" +
                    $"移动修正: {unitType.movModifier:+#;-#;0}" +
                    upgradeInfo;
            }
            else
            {
                tabContentText.text = "无兵种信息";
            }
        }

        void ShowSkillsTab(HeroDefinition heroDef)
        {
            if (heroDef?.learnableSkills == null || heroDef.learnableSkills.Length == 0)
            {
                tabContentText.text = "无策略";
                return;
            }

            var sb = new System.Text.StringBuilder();
            foreach (var skill in heroDef.learnableSkills)
            {
                if (skill == null) continue;
                sb.AppendLine($"{skill.displayName} (MP:{skill.mpCost}) Lv.{skill.learnLevel}");
                sb.AppendLine($"  {skill.description}");
            }
            tabContentText.text = sb.ToString();
        }

        void ShowPassivesTab(HeroDefinition heroDef)
        {
            if (heroDef?.passiveAbilityIds == null || heroDef.passiveAbilityIds.Length == 0)
            {
                tabContentText.text = "无特技";
                return;
            }

            var sb = new System.Text.StringBuilder();
            foreach (var passiveId in heroDef.passiveAbilityIds)
            {
                sb.AppendLine($"- {passiveId}");
            }
            tabContentText.text = sb.ToString();
        }

        void HighlightTab(Button tab, bool active)
        {
            if (tab == null) return;
            var img = tab.GetComponent<Image>();
            if (img != null)
            {
                img.color = active
                    ? ThreeKingdomsTheme.SpeedActive
                    : ThreeKingdomsTheme.SpeedInactive;
            }
        }
    }
}
