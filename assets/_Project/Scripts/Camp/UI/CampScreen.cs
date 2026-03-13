using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CaoCao.Core;
using CaoCao.UI;

namespace CaoCao.Camp
{
    /// <summary>
    /// Main camp hub screen (曹操主营).
    /// Shows camp background and 5 menu buttons: 出兵/武将/装备/仓库/系统.
    /// </summary>
    public class CampScreen : BaseScreen
    {
        [Header("Background")]
        [SerializeField] Image backgroundImage;

        [Header("Menu Buttons")]
        [SerializeField] Button deployButton;       // 出兵
        [SerializeField] Button heroesButton;       // 武将
        [SerializeField] Button equipButton;        // 装备
        [SerializeField] Button warehouseButton;    // 仓库
        [SerializeField] Button systemButton;       // 系统

        [Header("Labels")]
        [SerializeField] TMP_Text titleLabel;

        /// <summary>
        /// Fired when a menu button is clicked. Passes menu ID string.
        /// </summary>
        public event Action<string> OnMenuSelected;

        public override void Initialize()
        {
            // Wire button click events
            deployButton?.onClick.AddListener(() => OnMenuSelected?.Invoke("deploy"));
            heroesButton?.onClick.AddListener(() => OnMenuSelected?.Invoke("heroes"));
            equipButton?.onClick.AddListener(() => OnMenuSelected?.Invoke("equip"));
            warehouseButton?.onClick.AddListener(() => OnMenuSelected?.Invoke("warehouse"));
            systemButton?.onClick.AddListener(() => OnMenuSelected?.Invoke("system"));

            // Apply theme styling
            if (deployButton != null) ThreeKingdomsTheme.StyleButton(deployButton);
            if (heroesButton != null) ThreeKingdomsTheme.StyleButton(heroesButton);
            if (equipButton != null) ThreeKingdomsTheme.StyleButton(equipButton);
            if (warehouseButton != null) ThreeKingdomsTheme.StyleButton(warehouseButton);
            if (systemButton != null) ThreeKingdomsTheme.StyleButton(systemButton);

            if (titleLabel != null)
            {
                titleLabel.text = "曹操主营";
                titleLabel.color = ThreeKingdomsTheme.TextGold;
            }
        }

        public override void Show()
        {
            base.Show();
            // Refresh any dynamic content when returning to camp
        }
    }
}
