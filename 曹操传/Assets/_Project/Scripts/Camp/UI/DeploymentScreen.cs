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
    /// Deployment screen (出兵/出战).
    /// Left: available hero grid. Right: deployment slots (required=red, optional=empty).
    /// Bottom: selected hero stats preview. Confirm to start battle.
    /// </summary>
    public class DeploymentScreen : BaseScreen
    {
        [Header("Hero Grid (Available)")]
        [SerializeField] Transform heroGridContainer;
        [SerializeField] GameObject heroCellPrefab;

        [Header("Deploy Slots (Right Panel)")]
        [SerializeField] Transform deploySlotContainer;
        [SerializeField] GameObject deploySlotPrefab;

        [Header("Stats Preview (Bottom)")]
        [SerializeField] RectTransform statsPreviewPanel;
        [SerializeField] Image previewPortrait;
        [SerializeField] TMP_Text previewName;
        [SerializeField] TMP_Text previewStats;

        [Header("Buttons")]
        [SerializeField] Button confirmButton;      // 出击
        [SerializeField] Button cancelButton;       // 返回

        [Header("Labels")]
        [SerializeField] TMP_Text titleLabel;
        [SerializeField] TMP_Text deployCountLabel;

        /// <summary>Fired when deployment is confirmed. Passes list of deployed hero IDs.</summary>
        public event Action<List<string>> OnDeployConfirmed;
        /// <summary>Fired when user cancels deployment.</summary>
        public event Action OnCancelled;

        BattleDefinition _battleDef;
        GameStateManager _gsm;
        GameDataRegistry _registry;

        List<string> _deployedIds = new();
        List<string> _requiredIds = new();
        string _selectedHeroId;

        // Track instantiated widgets for cleanup
        readonly List<GameObject> _heroCells = new();
        readonly List<GameObject> _slotCells = new();

        public override void Initialize()
        {
            _gsm = ServiceLocator.Get<GameStateManager>();
            _registry = ServiceLocator.Get<GameDataRegistry>();

            confirmButton?.onClick.AddListener(OnConfirmClicked);
            cancelButton?.onClick.AddListener(() => OnCancelled?.Invoke());

            if (confirmButton != null) ThreeKingdomsTheme.StyleButton(confirmButton);
            if (cancelButton != null) ThreeKingdomsTheme.StyleButton(cancelButton);

            if (titleLabel != null)
            {
                titleLabel.text = "出兵准备";
                titleLabel.color = ThreeKingdomsTheme.TextGold;
            }
        }

        /// <summary>
        /// Set up the deployment screen for a specific battle.
        /// </summary>
        public void Setup(BattleDefinition battleDef)
        {
            _battleDef = battleDef;
            _deployedIds.Clear();
            _requiredIds.Clear();
            _selectedHeroId = null;

            // Collect required hero IDs
            if (_battleDef?.requiredHeroes != null)
            {
                foreach (var req in _battleDef.requiredHeroes)
                {
                    if (!string.IsNullOrEmpty(req.unitId))
                    {
                        _requiredIds.Add(req.unitId);
                        _deployedIds.Add(req.unitId);
                    }
                }
            }

            PopulateHeroGrid();
            UpdateDeploySlots();
            UpdateDeployCount();
            ClearPreview();
        }

        void PopulateHeroGrid()
        {
            // Clear existing cells
            foreach (var go in _heroCells)
                if (go != null) Destroy(go);
            _heroCells.Clear();

            if (heroGridContainer == null || heroCellPrefab == null) return;

            var heroes = _gsm?.GetRecruitedHeroes();
            if (heroes == null) return;

            foreach (var heroData in heroes)
            {
                var heroDef = _registry?.GetHero(heroData.heroId);
                if (heroDef == null) continue;

                var go = Instantiate(heroCellPrefab, heroGridContainer);
                _heroCells.Add(go);

                var cell = go.GetComponent<HeroCellWidget>();
                if (cell != null)
                {
                    bool required = _requiredIds.Contains(heroData.heroId);
                    cell.Setup(heroData.heroId, heroDef.portrait, heroDef.displayName, required);
                    cell.SetSelected(_deployedIds.Contains(heroData.heroId));
                    cell.OnClicked += OnHeroCellClicked;
                }
            }
        }

        void OnHeroCellClicked(string heroId)
        {
            // Select for preview
            _selectedHeroId = heroId;
            UpdateStatsPreview(heroId);

            // Toggle deployment (required heroes can't be removed)
            if (_requiredIds.Contains(heroId)) return;

            if (_deployedIds.Contains(heroId))
            {
                _deployedIds.Remove(heroId);
            }
            else
            {
                int maxDeploy = _battleDef?.maxDeployCount ?? 8;
                if (_deployedIds.Count >= maxDeploy) return;
                _deployedIds.Add(heroId);
            }

            // Update visuals
            foreach (var go in _heroCells)
            {
                var cell = go.GetComponent<HeroCellWidget>();
                if (cell != null)
                    cell.SetSelected(_deployedIds.Contains(cell.HeroId));
            }

            UpdateDeploySlots();
            UpdateDeployCount();
        }

        void UpdateDeploySlots()
        {
            // Clear existing slot visuals
            foreach (var go in _slotCells)
                if (go != null) Destroy(go);
            _slotCells.Clear();

            if (deploySlotContainer == null || deploySlotPrefab == null) return;

            int maxDeploy = _battleDef?.maxDeployCount ?? 8;

            for (int i = 0; i < maxDeploy; i++)
            {
                var go = Instantiate(deploySlotPrefab, deploySlotContainer);
                _slotCells.Add(go);

                var cell = go.GetComponent<HeroCellWidget>();
                if (cell != null)
                {
                    if (i < _deployedIds.Count)
                    {
                        string heroId = _deployedIds[i];
                        var heroDef = _registry?.GetHero(heroId);
                        bool required = _requiredIds.Contains(heroId);
                        cell.Setup(heroId, heroDef?.portrait, heroDef?.displayName ?? heroId, required);
                        cell.SetSelected(true);
                    }
                    else
                    {
                        cell.Setup("", null, "空", false);
                    }
                }
            }
        }

        void UpdateDeployCount()
        {
            if (deployCountLabel != null)
            {
                int maxDeploy = _battleDef?.maxDeployCount ?? 8;
                deployCountLabel.text = $"{_deployedIds.Count}/{maxDeploy}";
            }
        }

        void UpdateStatsPreview(string heroId)
        {
            var heroData = _gsm?.GetHero(heroId);
            var heroDef = _registry?.GetHero(heroId);

            if (heroData == null || heroDef == null)
            {
                ClearPreview();
                return;
            }

            if (statsPreviewPanel != null)
                statsPreviewPanel.gameObject.SetActive(true);

            if (previewPortrait != null)
            {
                previewPortrait.sprite = heroDef.portrait;
                previewPortrait.enabled = heroDef.portrait != null;
            }

            if (previewName != null)
                previewName.text = heroDef.displayName;

            if (previewStats != null)
            {
                previewStats.text =
                    $"Lv.{heroData.level}  HP:{heroData.currentHp}/{heroData.maxHp}  " +
                    $"MP:{heroData.currentMp}/{heroData.maxMp}\n" +
                    $"攻:{heroData.atk}  防:{heroData.def}  移:{heroData.mov}  速:{heroData.speed}";
            }
        }

        void ClearPreview()
        {
            if (statsPreviewPanel != null)
                statsPreviewPanel.gameObject.SetActive(false);
        }

        void OnConfirmClicked()
        {
            if (_deployedIds.Count == 0)
            {
                Debug.LogWarning("[DeploymentScreen] No heroes deployed!");
                return;
            }

            OnDeployConfirmed?.Invoke(new List<string>(_deployedIds));
        }

        public override void Show()
        {
            base.Show();
            // Refresh if data might have changed
        }
    }
}
