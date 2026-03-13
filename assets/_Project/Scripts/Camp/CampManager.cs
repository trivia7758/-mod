using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CaoCao.Core;
using CaoCao.Data;
using CaoCao.Story;

namespace CaoCao.Camp
{
    /// <summary>
    /// Camp overlay that appears on top of the StoryScene after story ends.
    /// Builds its own UI programmatically. Lives on GameManager (DontDestroyOnLoad).
    /// Listens for EventBus.OnStoryCompleted to show the camp bottom bar.
    /// </summary>
    public class CampManager : MonoBehaviour
    {
        Canvas _canvas;
        GameObject _bottomBar;

        // Sub-screen panels
        GameObject _deployPanelGo;
        GameObject _heroPanelGo;
        GameObject _equipPanelGo;
        GameObject _warehousePanelGo;
        GameObject _systemPanelGo;

        // Screen components
        CampHeroScreen _heroScreen;
        CampEquipScreen _equipScreen;
        CampWarehouseScreen _warehouseScreen;
        CampSystemScreen _systemScreen;

        // Deployment state
        HashSet<string> _selectedHeroes = new();
        HashSet<string> _requiredHeroIds = new();
        int _maxDeploy = 8;
        TMP_Text _deployCountLabel;
        Dictionary<string, Image> _deployCardBorders = new();

        GameStateManager _gsm;
        GameDataRegistry _registry;
        bool _campActive;

        void OnEnable()
        {
            Debug.Log("[CampManager] OnEnable — subscribing to OnStoryCompleted");
            EventBus.OnStoryCompleted += OnStoryCompleted;
        }

        void OnDisable()
        {
            EventBus.OnStoryCompleted -= OnStoryCompleted;
        }

        void OnStoryCompleted()
        {
            Debug.Log("[CampManager] OnStoryCompleted received!");
            EnsureServicesExist();

            _gsm = ServiceLocator.Get<GameStateManager>();
            _registry = ServiceLocator.Get<GameDataRegistry>();

            Debug.Log($"[CampManager] _gsm={(_gsm != null ? "OK" : "NULL")}, _registry={(_registry != null ? "OK" : "NULL")}");

            // Initialize new game if no heroes recruited yet
            if (_gsm != null && _gsm.GetRecruitedHeroes().Count == 0)
                _gsm.InitializeNewGame();

            // Hide dialogue box
            var dialogueBox = FindAnyObjectByType<DialogueBox>();
            if (dialogueBox != null)
                dialogueBox.gameObject.SetActive(false);

            ShowCampMenu();
        }

        /// <summary>
        /// When starting from StoryScene directly (no Boot/GameManager),
        /// essential services may not exist. Create them as fallback.
        /// </summary>
        void EnsureServicesExist()
        {
            // SaveSystem
            if (!ServiceLocator.Has<SaveSystem>())
            {
                Debug.Log("[CampManager] Bootstrapping SaveSystem");
                var ss = new SaveSystem();
                ss.EnsureDataDirectory();
                ServiceLocator.Register(ss);
            }

            // GameDataRegistry
            if (!ServiceLocator.Has<GameDataRegistry>())
            {
                Debug.Log("[CampManager] Bootstrapping GameDataRegistry");
                var reg = Resources.Load<GameDataRegistry>("Data/GameDataRegistry");
                if (reg != null)
                {
                    reg.Initialize();
                    ServiceLocator.Register(reg);
                }
            }

            // GameStateManager
            if (!ServiceLocator.Has<GameStateManager>())
            {
                Debug.Log("[CampManager] Bootstrapping GameStateManager");
                var ss = ServiceLocator.Get<SaveSystem>();
                var reg = ServiceLocator.Get<GameDataRegistry>();
                var gsm = new GameStateManager(reg, ss);
                ServiceLocator.Register(gsm);
            }
        }

        void ShowCampMenu()
        {
            Debug.Log("[CampManager] ShowCampMenu called");
            if (_campActive) return;
            _campActive = true;

            EnsureCanvas();
            BuildBottomBar();
            BuildAllScreens();
            Debug.Log("[CampManager] Bottom bar built and shown");

            HideAllScreens();
            _bottomBar.SetActive(true);

            EventBus.CampEntered();
        }

        void ReturnToMenu()
        {
            HideAllScreens();
            _bottomBar.SetActive(true);
        }

        void HideAllScreens()
        {
            if (_deployPanelGo != null) _deployPanelGo.SetActive(false);
            if (_heroPanelGo != null) _heroPanelGo.SetActive(false);
            if (_equipPanelGo != null) _equipPanelGo.SetActive(false);
            if (_warehousePanelGo != null) _warehousePanelGo.SetActive(false);
            if (_systemPanelGo != null) _systemPanelGo.SetActive(false);
        }

        void ShowScreen(GameObject panel)
        {
            _bottomBar.SetActive(false);
            HideAllScreens();
            if (panel != null) panel.SetActive(true);
        }

        // ============================================================
        // Canvas & Bottom Bar
        // ============================================================

        void EnsureCanvas()
        {
            if (_canvas != null)
            {
                _canvas.gameObject.SetActive(true);
                return;
            }

            var go = new GameObject("CampOverlayCanvas");
            go.transform.SetParent(transform);
            _canvas = go.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 100;

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 0.5f;

            go.AddComponent<GraphicRaycaster>();
        }

        void BuildBottomBar()
        {
            if (_bottomBar != null) return;

            _bottomBar = new GameObject("BottomBar");
            _bottomBar.transform.SetParent(_canvas.transform, false);

            var rt = _bottomBar.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, 0);
            rt.pivot = new Vector2(0.5f, 0);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = new Vector2(0, 80);

            var bg = _bottomBar.AddComponent<Image>();
            bg.color = ThreeKingdomsTheme.PanelBg;

            var outline = _bottomBar.AddComponent<Outline>();
            outline.effectColor = ThreeKingdomsTheme.PanelBorder;
            outline.effectDistance = new Vector2(0, 2);

            var hlg = _bottomBar.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 10;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.padding = new RectOffset(20, 20, 8, 8);
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;

            CreateMenuButton("出兵", "deploy");
            CreateMenuButton("武将", "heroes");
            CreateMenuButton("装备", "equip");
            CreateMenuButton("仓库", "warehouse");
            CreateMenuButton("系统", "system");
        }

        void CreateMenuButton(string label, string menuId)
        {
            var btn = ThreeKingdomsTheme.CreateButton(
                _bottomBar.transform, label, Vector2.zero, new Vector2(120, 64), 22f);
            btn.gameObject.AddComponent<LayoutElement>();

            var btnRt = btn.GetComponent<RectTransform>();
            btnRt.anchoredPosition = Vector2.zero;

            btn.onClick.AddListener(() => OnMenuSelected(menuId));
        }

        void OnMenuSelected(string menuId)
        {
            EventBus.CampMenuSelected(menuId);

            switch (menuId)
            {
                case "deploy":
                    ShowDeploymentScreen();
                    break;
                case "heroes":
                    _heroScreen?.Refresh();
                    ShowScreen(_heroPanelGo);
                    break;
                case "equip":
                    _equipScreen?.Refresh();
                    ShowScreen(_equipPanelGo);
                    break;
                case "warehouse":
                    _warehouseScreen?.Refresh();
                    ShowScreen(_warehousePanelGo);
                    break;
                case "system":
                    _systemScreen?.Refresh();
                    ShowScreen(_systemPanelGo);
                    break;
            }
        }

        // ============================================================
        // Build All Sub-Screens
        // ============================================================

        void BuildAllScreens()
        {
            if (_heroPanelGo != null) return; // already built

            // Hero screen
            _heroPanelGo = CreateScreenPanel("HeroScreen");
            _heroScreen = _heroPanelGo.AddComponent<CampHeroScreen>();
            _heroScreen.Build(_gsm, _registry);
            _heroScreen.OnBack += ReturnToMenu;

            // Equip screen
            _equipPanelGo = CreateScreenPanel("EquipScreen");
            _equipScreen = _equipPanelGo.AddComponent<CampEquipScreen>();
            _equipScreen.Build(_gsm, _registry);
            _equipScreen.OnBack += ReturnToMenu;

            // Warehouse screen
            _warehousePanelGo = CreateScreenPanel("WarehouseScreen");
            _warehouseScreen = _warehousePanelGo.AddComponent<CampWarehouseScreen>();
            _warehouseScreen.Build(_gsm, _registry);
            _warehouseScreen.OnBack += ReturnToMenu;

            // System screen
            _systemPanelGo = CreateScreenPanel("SystemScreen");
            _systemScreen = _systemPanelGo.AddComponent<CampSystemScreen>();
            _systemScreen.Build(_gsm, _registry);
            _systemScreen.OnBack += ReturnToMenu;
            _systemScreen.OnReturnToMainMenu += GoToMainMenu;
        }

        GameObject CreateScreenPanel(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_canvas.transform, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            // Full-screen dark overlay background
            var bg = go.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.8f);

            go.SetActive(false);
            return go;
        }

        // ============================================================
        // Deployment Screen — with required/optional heroes + sprites
        // ============================================================

        static readonly Color DeployRequiredBorder = new(0.8f, 0.2f, 0.2f, 1f);
        static readonly Color DeploySelectedBorder = new(0.85f, 0.7f, 0.3f, 1f);
        static readonly Color DeployNormalBorder = new(0.3f, 0.3f, 0.3f, 0.8f);
        static readonly Color DeployCardBg = new(0.12f, 0.1f, 0.08f, 0.8f);

        void ShowDeploymentScreen()
        {
            _bottomBar.SetActive(false);
            HideAllScreens();

            if (_deployPanelGo != null)
                Destroy(_deployPanelGo);

            _selectedHeroes.Clear();
            _requiredHeroIds.Clear();
            _deployCardBorders.Clear();

            BuildDeploymentPanel();
        }

        void BuildDeploymentPanel()
        {
            _deployPanelGo = CreateScreenPanel("DeployPanel");
            _deployPanelGo.SetActive(true);

            // Get battle definition for deploy limits
            var battleDef = _registry?.GetBattle("Battle_01");
            _maxDeploy = battleDef != null ? battleDef.maxDeployCount : 8;

            // Gather required hero IDs
            if (battleDef?.requiredHeroes != null)
            {
                foreach (var rh in battleDef.requiredHeroes)
                {
                    if (!string.IsNullOrEmpty(rh.unitId))
                    {
                        _requiredHeroIds.Add(rh.unitId);
                        _selectedHeroes.Add(rh.unitId);
                    }
                }
            }

            // If no explicit required heroes, make isRequired heroes from HeroDefinition required
            var allHeroes = _gsm?.GetRecruitedHeroes();
            if (allHeroes != null)
            {
                foreach (var h in allHeroes)
                {
                    var hDef = _registry?.GetHero(h.heroId);
                    if (hDef != null && hDef.isRequired && !_requiredHeroIds.Contains(h.heroId))
                    {
                        _requiredHeroIds.Add(h.heroId);
                        _selectedHeroes.Add(h.heroId);
                    }
                }
            }

            // Title + deploy count
            _deployCountLabel = CampUIHelper.CreateAnchoredLabel(_deployPanelGo.transform,
                $"出兵准备 — 出战: {_selectedHeroes.Count}/{_maxDeploy}", 26f,
                new Vector2(0, 0.91f), new Vector2(1, 1),
                Vector2.zero, Vector2.zero,
                TextAlignmentOptions.Center);

            // Scroll area for hero cards
            var scrollPanel = CampUIHelper.CreateSubPanel(_deployPanelGo.transform, "DeployArea",
                new Vector2(0.03f, 0.15f), new Vector2(0.97f, 0.9f),
                Vector2.zero, Vector2.zero);

            var (scroll, content) = CampUIHelper.CreateScrollView(scrollPanel,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            // Required heroes section
            if (_requiredHeroIds.Count > 0)
            {
                AddSectionLabel(content, $"── 必须出战 ({_requiredHeroIds.Count}) ──");
                var requiredGrid = CreateHeroCardGrid(content, "RequiredGrid");

                foreach (var heroId in _requiredHeroIds)
                {
                    var hero = _gsm?.GetHero(heroId);
                    var heroDef = _registry?.GetHero(heroId);
                    if (hero == null) continue;
                    CreateHeroCard(requiredGrid, hero, heroDef, true);
                }
            }

            // Optional heroes section
            var optionalHeroes = new List<HeroRuntimeData>();
            if (allHeroes != null)
            {
                foreach (var h in allHeroes)
                {
                    if (!_requiredHeroIds.Contains(h.heroId))
                        optionalHeroes.Add(h);
                }
            }

            if (optionalHeroes.Count > 0)
            {
                AddSectionLabel(content, $"── 可选出战 ──");
                var optionalGrid = CreateHeroCardGrid(content, "OptionalGrid");

                foreach (var hero in optionalHeroes)
                {
                    var heroDef = _registry?.GetHero(hero.heroId);
                    CreateHeroCard(optionalGrid, hero, heroDef, false);
                }
            }

            if (_requiredHeroIds.Count == 0 && optionalHeroes.Count == 0)
            {
                AddSectionLabel(content, "暂无可用武将");
            }

            // Buttons
            var confirmBtn = CampUIHelper.CreateAnchoredButton(_deployPanelGo.transform, "出击",
                new Vector2(0.55f, 0.03f), new Vector2(0.75f, 0.13f),
                Vector2.zero, Vector2.zero);
            confirmBtn.onClick.AddListener(OnDeployConfirmed);

            var cancelBtn = CampUIHelper.CreateAnchoredButton(_deployPanelGo.transform, "返回",
                new Vector2(0.25f, 0.03f), new Vector2(0.45f, 0.13f),
                Vector2.zero, Vector2.zero);
            cancelBtn.onClick.AddListener(() => ReturnToMenu());
        }

        void AddSectionLabel(RectTransform content, string text)
        {
            var go = new GameObject("SectionLabel");
            go.transform.SetParent(content, false);
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 32;
            le.minHeight = 32;

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 18f;
            tmp.color = ThreeKingdomsTheme.TextGold;
            tmp.alignment = TextAlignmentOptions.Center;
        }

        RectTransform CreateHeroCardGrid(RectTransform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var le = go.AddComponent<LayoutElement>();
            le.minHeight = 100;
            le.preferredHeight = 100;

            var rt = go.GetComponent<RectTransform>();

            // Use GridLayoutGroup for cards
            var glg = go.AddComponent<GridLayoutGroup>();
            glg.cellSize = new Vector2(80, 95);
            glg.spacing = new Vector2(8, 8);
            glg.padding = new RectOffset(10, 10, 5, 5);
            glg.childAlignment = TextAnchor.UpperLeft;
            glg.constraint = GridLayoutGroup.Constraint.Flexible;

            // Auto-size height
            var csf = go.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            return rt;
        }

        void CreateHeroCard(RectTransform parent, HeroRuntimeData hero, HeroDefinition heroDef, bool isRequired)
        {
            string heroName = heroDef != null ? heroDef.displayName : hero.heroId;

            // Card container
            var card = new GameObject("Card_" + heroName);
            card.transform.SetParent(parent, false);

            var cardImg = card.AddComponent<Image>();
            cardImg.color = DeployCardBg;

            // Border outline
            var outline = card.AddComponent<Outline>();
            outline.effectDistance = new Vector2(2, -2);

            if (isRequired)
                outline.effectColor = DeployRequiredBorder;
            else if (_selectedHeroes.Contains(hero.heroId))
                outline.effectColor = DeploySelectedBorder;
            else
                outline.effectColor = DeployNormalBorder;

            _deployCardBorders[hero.heroId] = cardImg;

            // Sprite image (top portion)
            var spriteGo = new GameObject("Sprite");
            spriteGo.transform.SetParent(card.transform, false);
            var srt = spriteGo.AddComponent<RectTransform>();
            srt.anchorMin = new Vector2(0.1f, 0.3f);
            srt.anchorMax = new Vector2(0.9f, 0.95f);
            srt.offsetMin = Vector2.zero;
            srt.offsetMax = Vector2.zero;

            var spriteImg = spriteGo.AddComponent<Image>();
            spriteImg.sprite = CampUIHelper.GetHeroUnitSprite(heroDef);
            spriteImg.preserveAspect = true;
            spriteImg.color = Color.white;

            // Name label (bottom)
            var nameGo = new GameObject("Name");
            nameGo.transform.SetParent(card.transform, false);
            var nrt = nameGo.AddComponent<RectTransform>();
            nrt.anchorMin = new Vector2(0, 0);
            nrt.anchorMax = new Vector2(1, 0.3f);
            nrt.offsetMin = Vector2.zero;
            nrt.offsetMax = Vector2.zero;

            var nameTmp = nameGo.AddComponent<TextMeshProUGUI>();
            nameTmp.text = heroName;
            nameTmp.fontSize = 13f;
            nameTmp.color = isRequired ? new Color(1f, 0.6f, 0.6f) : ThreeKingdomsTheme.TextPrimary;
            nameTmp.alignment = TextAlignmentOptions.Center;
            nameTmp.textWrappingMode = TextWrappingModes.NoWrap;
            nameTmp.overflowMode = TextOverflowModes.Ellipsis;

            // Click handler for optional heroes
            if (!isRequired)
            {
                var btn = card.AddComponent<Button>();
                btn.targetGraphic = cardImg;
                var colors = btn.colors;
                colors.normalColor = DeployCardBg;
                colors.highlightedColor = new Color(0.2f, 0.18f, 0.14f, 0.9f);
                colors.pressedColor = new Color(0.25f, 0.22f, 0.16f, 1f);
                btn.colors = colors;

                string id = hero.heroId;
                btn.onClick.AddListener(() => ToggleDeployHero(id));
            }
        }

        void ToggleDeployHero(string heroId)
        {
            if (_selectedHeroes.Contains(heroId))
            {
                _selectedHeroes.Remove(heroId);
            }
            else
            {
                // Check if we've reached max
                if (_selectedHeroes.Count >= _maxDeploy)
                {
                    Debug.Log($"[CampManager] Max deploy count reached: {_maxDeploy}");
                    return;
                }
                _selectedHeroes.Add(heroId);
            }

            UpdateDeployVisuals(heroId);
            UpdateDeployCount();
        }

        void UpdateDeployVisuals(string heroId)
        {
            // Update card border color
            if (_deployCardBorders.TryGetValue(heroId, out var cardImg))
            {
                var outline = cardImg.GetComponent<Outline>();
                if (outline != null)
                {
                    outline.effectColor = _selectedHeroes.Contains(heroId)
                        ? DeploySelectedBorder
                        : DeployNormalBorder;
                }
            }
        }

        void UpdateDeployCount()
        {
            if (_deployCountLabel != null)
                _deployCountLabel.text = $"出兵准备 — 出战: {_selectedHeroes.Count}/{_maxDeploy}";
        }

        void OnDeployConfirmed()
        {
            if (_selectedHeroes.Count == 0)
            {
                Debug.Log("[CampManager] No heroes selected for deployment");
                return;
            }

            if (_gsm != null)
            {
                var ids = new List<string>(_selectedHeroes);
                _gsm.SetDeployment(ids);
                EventBus.DeploymentConfirmed(ids);
            }

            HideCamp();

            var loader = ServiceLocator.Get<SceneLoader>();
            var battleDef = _registry?.GetBattle("Battle_01");
            string sceneName = battleDef != null && !string.IsNullOrEmpty(battleDef.sceneName)
                ? battleDef.sceneName : "Battle";

            if (loader != null)
                loader.LoadScene(sceneName);
            else
                UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
        }

        void HideCamp()
        {
            _campActive = false;
            if (_canvas != null)
                _canvas.gameObject.SetActive(false);
        }

        void GoToMainMenu()
        {
            HideCamp();
            var loader = ServiceLocator.Get<SceneLoader>();
            if (loader != null)
            {
                loader.LoadScene("MainMenu");
            }
            else
            {
                // Fallback: when starting from StoryScene directly, SceneLoader may not exist
                Debug.Log("[CampManager] SceneLoader not found, using SceneManager directly");
                UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
            }
        }
    }
}
