using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CaoCao.Battle.States;
using CaoCao.Common;
using CaoCao.Core;
using CaoCao.Data;

namespace CaoCao.Battle
{
    public class BattleController : MonoBehaviour
    {
        [SerializeField] Vector2Int tileSize = new(48, 48);
        [SerializeField] BattleGridMap gridMap;
        [SerializeField] TileHighlighter highlighter;
        [SerializeField] Transform unitsRoot;

        [Header("UI")]
        [SerializeField] BattleActionMenu actionMenu;
        [SerializeField] BattleHUD hud;

        BattleUnitInfoPanel _unitInfoPanel;

        public BattleGridMap GridMap => gridMap;
        public TileHighlighter Highlighter => highlighter;
        public BattleUnit SelectedUnit { get; private set; }
        public Dictionary<Vector2Int, int> Reachable { get; private set; } = new();

        IBattleState _currentState;
        BattlePathfinder _pathfinder = new();
        EnemyAI _ai = new();
        Dictionary<Vector2Int, Vector2Int?> _parent = new();
        Vector2Int _selectedOrigin;
        bool _movedThisTurn;
        string _currentTeam = "player";
        bool _battleEnded;
        int _turnCount = 1;

        // Map background
        Sprite _mapBackground;

        // Turn UI
        GameObject _endTurnBtnGo;
        Canvas _turnBannerCanvas;

        // Terrain Info Panel
        GameObject _terrainInfoPanelGo;
        TMP_Text _terrainInfoName;
        TMP_Text _terrainInfoCoord;
        TMP_Text _terrainInfoHeal;

        // System Menu
        BattleSystemMenu _systemMenu;

        void Start()
        {
            EnsureBattleComponents();
            EnsureInputManager();

            // Try deployment-based spawning first, fall back to demo
            if (!SpawnFromDeployment())
                SpawnDemoUnits();

            // Setup background and camera after spawning (battleDef may set background)
            EnsureMapBackground();
            SyncMapSize();
            SetupCamera();

            // Subscribe to unit death for cleanup
            SubscribeUnitDeathEvents();

            if (actionMenu != null)
            {
                actionMenu.OnAttack += OnAttackPressed;
                actionMenu.OnWait += OnWaitPressed;
                actionMenu.Hide();
            }

            // Create unit info panel
            EnsureUnitInfoPanel();

            // Create end turn button
            BuildEndTurnButton();

            // Create system menu (save/load/settings)
            BuildSystemMenu();

            EventBus.BattleStarted();
            Debug.Log($"[BattleController] Battle started! Players={GetUnitsByTeam(UnitTeam.Player).Count}, Enemies={GetUnitsByTeam(UnitTeam.Enemy).Count}");

            // Show initial turn banner then go to idle
            StartCoroutine(StartBattleSequence());
        }

        IEnumerator StartBattleSequence()
        {
            SetState(new AnimatingState());
            SetEndTurnButtonVisible(false);
            yield return StartCoroutine(ShowTurnBanner($"第{_turnCount}回合 - 我方回合"));
            // Heal player units on healing terrain at turn start
            ApplyTerrainHealing(UnitTeam.Player);
            SetEndTurnButtonVisible(true);
            SetState(new IdleState());
        }

        /// <summary>
        /// Auto-create or find missing serialized components.
        /// Searches the whole scene first (FindAnyObjectByType) since scene objects
        /// may not be children of BattleController.
        /// </summary>
        void EnsureBattleComponents()
        {
            // GridMap — search scene first, then create
            if (gridMap == null)
            {
                gridMap = FindAnyObjectByType<BattleGridMap>();
                if (gridMap == null)
                {
                    var go = new GameObject("BattleGridMap");
                    go.transform.SetParent(transform);
                    gridMap = go.AddComponent<BattleGridMap>();
                    Debug.Log("[BattleController] Auto-created BattleGridMap");
                }
            }

            // TileHighlighter — search scene first, then create
            if (highlighter == null)
            {
                highlighter = FindAnyObjectByType<TileHighlighter>();
                if (highlighter == null)
                {
                    var go = new GameObject("TileHighlighter");
                    go.transform.SetParent(transform);
                    highlighter = go.AddComponent<TileHighlighter>();
                    Debug.Log("[BattleController] Auto-created TileHighlighter");
                }
            }

            // UnitsRoot
            if (unitsRoot == null)
            {
                // Look for existing "PlayerUnits" or "Units" in the scene
                var playerUnits = GameObject.Find("PlayerUnits");
                if (playerUnits != null)
                {
                    unitsRoot = playerUnits.transform;
                }
                else
                {
                    var go = new GameObject("Units");
                    go.transform.SetParent(transform);
                    unitsRoot = go.transform;
                    Debug.Log("[BattleController] Auto-created Units root");
                }
            }

            // ActionMenu — destroy any broken scene ActionMenu, then build a proper fallback
            if (actionMenu == null)
            {
                // Destroy scene's broken ActionMenu (serialized refs are all null)
                var sceneMenu = FindAnyObjectByType<BattleActionMenu>(FindObjectsInactive.Include);
                if (sceneMenu != null)
                {
                    Destroy(sceneMenu.gameObject);
                    Debug.Log("[BattleController] Destroyed broken scene ActionMenu");
                }

                BuildActionMenu();
                Debug.Log("[BattleController] Built fallback ActionMenu");
            }

            // HUD — try to find in scene
            if (hud == null)
                hud = FindAnyObjectByType<BattleHUD>(FindObjectsInactive.Include);

            // Clean up scene objects that cause visual artifacts
            CleanupSceneArtifacts();
        }

        /// <summary>
        /// Clean up scene objects that cause visual artifacts:
        /// - FadeOverlay: white Image blocking view
        /// - EnemyUnits: empty container (enemies are spawned under unitsRoot)
        /// - BattleCanvas: has broken UI elements with white backgrounds
        /// </summary>
        // --- Unit Info Panel ---
        void EnsureUnitInfoPanel()
        {
            if (_unitInfoPanel != null) return;

            // Create a dedicated overlay canvas for the info panel
            var canvasGo = new GameObject("BattleInfoCanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            canvasGo.AddComponent<GraphicRaycaster>();

            // Create panel as child of canvas (with RectTransform)
            var panelGo = new GameObject("BattleUnitInfoPanel", typeof(RectTransform));
            panelGo.transform.SetParent(canvasGo.transform, false);
            _unitInfoPanel = panelGo.AddComponent<BattleUnitInfoPanel>();
        }

        public void ShowUnitInfo(BattleUnit unit)
        {
            if (_unitInfoPanel == null) EnsureUnitInfoPanel();
            _unitInfoPanel.Show(unit);
        }

        public void HideUnitInfo()
        {
            if (_unitInfoPanel != null) _unitInfoPanel.Hide();
        }

        // --- Terrain Info Panel ---
        void EnsureTerrainInfoPanel()
        {
            if (_terrainInfoPanelGo != null) return;

            var canvasGo = new GameObject("TerrainInfoCanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 18;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 0.5f;

            // Panel anchored bottom-left
            _terrainInfoPanelGo = new GameObject("TerrainInfoPanel", typeof(RectTransform));
            _terrainInfoPanelGo.transform.SetParent(canvasGo.transform, false);
            var rt = _terrainInfoPanelGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(0, 0);
            rt.pivot = new Vector2(0, 0);
            rt.anchoredPosition = new Vector2(16, 16);
            rt.sizeDelta = new Vector2(220, 90);

            var bg = _terrainInfoPanelGo.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.06f, 0.14f, 0.88f);
            var outline = _terrainInfoPanelGo.AddComponent<Outline>();
            outline.effectColor = new Color(0.6f, 0.5f, 0.25f);
            outline.effectDistance = new Vector2(1, -1);

            // Terrain name label
            _terrainInfoName = CreateTerrainLabel(_terrainInfoPanelGo.transform, "TerrainName",
                new Vector2(10, -8), new Vector2(210, 28), 22, new Color(1f, 0.9f, 0.6f));

            // Coordinates label
            _terrainInfoCoord = CreateTerrainLabel(_terrainInfoPanelGo.transform, "TerrainCoord",
                new Vector2(10, -34), new Vector2(210, 22), 16, new Color(0.75f, 0.75f, 0.75f));

            // Heal label (hidden by default)
            _terrainInfoHeal = CreateTerrainLabel(_terrainInfoPanelGo.transform, "TerrainHeal",
                new Vector2(10, -58), new Vector2(210, 22), 16, new Color(0.3f, 1f, 0.4f));

            _terrainInfoPanelGo.SetActive(false);
        }

        TMP_Text CreateTerrainLabel(Transform parent, string name, Vector2 pos, Vector2 size, float fontSize, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Left;
            return tmp;
        }

        public void ShowTerrainInfo(Vector2Int cell)
        {
            EnsureTerrainInfoPanel();

            bool inBounds = gridMap.IsInBounds(cell);
            Debug.Log($"[TerrainInfo] cell={cell}, inBounds={inBounds}, mapSize={gridMap.MapSize}, origin={gridMap.Origin}");

            if (inBounds)
            {
                var info = gridMap.GetTerrainInfo(cell);
                _terrainInfoName.text = info.Name;
                _terrainInfoCoord.text = $"坐标: ({cell.x}, {cell.y})";

                if (info.HealPerTurn > 0)
                {
                    _terrainInfoHeal.text = $"可恢复 (每回合 +{info.HealPerTurn} HP)";
                    _terrainInfoHeal.gameObject.SetActive(true);
                    _terrainInfoPanelGo.GetComponent<RectTransform>().sizeDelta = new Vector2(220, 90);
                }
                else
                {
                    _terrainInfoHeal.gameObject.SetActive(false);
                    _terrainInfoPanelGo.GetComponent<RectTransform>().sizeDelta = new Vector2(220, 66);
                }
            }
            else
            {
                _terrainInfoName.text = "界外";
                _terrainInfoCoord.text = $"坐标: ({cell.x}, {cell.y})";
                _terrainInfoHeal.gameObject.SetActive(false);
                _terrainInfoPanelGo.GetComponent<RectTransform>().sizeDelta = new Vector2(220, 66);
            }

            _terrainInfoPanelGo.SetActive(true);
        }

        public void HideTerrainInfo()
        {
            if (_terrainInfoPanelGo != null)
                _terrainInfoPanelGo.SetActive(false);
        }

        // --- Heal on Turn Start ---
        void ApplyTerrainHealing(UnitTeam team)
        {
            var units = GetUnitsByTeam(team);
            foreach (var unit in units)
            {
                if (unit.hp <= 0) continue;
                var info = gridMap.GetTerrainInfo(unit.cell);
                if (info.HealPerTurn > 0 && unit.hp < unit.maxHp)
                {
                    int healed = Mathf.Min(info.HealPerTurn, unit.maxHp - unit.hp);
                    unit.hp += healed;
                    Debug.Log($"[BattleController] {unit.displayName} healed {healed} HP on {info.Name} (now {unit.hp}/{unit.maxHp})");
                }
            }
        }

        // --- End Turn Button ---
        void BuildEndTurnButton()
        {
            var canvasGo = new GameObject("EndTurnCanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 15;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            _endTurnBtnGo = new GameObject("EndTurnBtn", typeof(RectTransform));
            _endTurnBtnGo.transform.SetParent(canvasGo.transform, false);
            var rt = _endTurnBtnGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1, 0);
            rt.anchorMax = new Vector2(1, 0);
            rt.pivot = new Vector2(1, 0);
            rt.anchoredPosition = new Vector2(-16, 16);
            rt.sizeDelta = new Vector2(160, 48);

            var img = _endTurnBtnGo.AddComponent<Image>();
            img.color = new Color(0.12f, 0.08f, 0.18f, 0.9f);
            var outline = _endTurnBtnGo.AddComponent<Outline>();
            outline.effectColor = new Color(0.8f, 0.65f, 0.3f);
            outline.effectDistance = new Vector2(1, -1);

            var btn = _endTurnBtnGo.AddComponent<Button>();
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.highlightedColor = new Color(0.2f, 0.15f, 0.3f);
            colors.pressedColor = new Color(0.3f, 0.2f, 0.1f);
            btn.colors = colors;
            btn.onClick.AddListener(ForceEndPlayerTurn);

            var textGo = new GameObject("Text", typeof(RectTransform));
            textGo.transform.SetParent(_endTurnBtnGo.transform, false);
            var textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;
            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text = "结束回合";
            tmp.fontSize = 24;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.8f, 0.65f, 0.3f);
        }

        void ForceEndPlayerTurn()
        {
            if (_currentTeam != "player" || _battleEnded) return;
            if (_currentState is EnemyTurnState || _currentState is AnimatingState) return;

            ClearSelection();
            highlighter.ClearAll();

            foreach (var u in GetUnitsByTeam(UnitTeam.Player))
            {
                if (!u.acted) u.Wait();
            }

            StartCoroutine(RunEnemyTurn());
        }

        void SetEndTurnButtonVisible(bool visible)
        {
            if (_endTurnBtnGo != null)
                _endTurnBtnGo.SetActive(visible);
        }

        // --- System Menu (Save/Load/Settings) ---

        void BuildSystemMenu()
        {
            var menuGo = new GameObject("BattleSystemMenu");
            menuGo.transform.SetParent(transform);
            _systemMenu = menuGo.AddComponent<BattleSystemMenu>();
            _systemMenu.Build();

            _systemMenu.OnResumed += () =>
            {
                Debug.Log("[BattleController] Resumed from system menu");
            };

            _systemMenu.OnReturnToMainMenu += () =>
            {
                Debug.Log("[BattleController] Returning to main menu");
                var loader = ServiceLocator.Get<SceneLoader>();
                if (loader != null)
                    loader.LoadScene("MainMenu");
                else
                    UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
            };

            // Build the "系统" trigger button on the end-turn canvas
            BuildSystemButton();
        }

        void BuildSystemButton()
        {
            // Find the end-turn canvas to add the system button next to it
            Canvas targetCanvas = null;
            if (_endTurnBtnGo != null)
                targetCanvas = _endTurnBtnGo.GetComponentInParent<Canvas>();

            // If no canvas found, create one
            if (targetCanvas == null)
            {
                var canvasGo = new GameObject("SystemBtnCanvas");
                canvasGo.transform.SetParent(transform);
                targetCanvas = canvasGo.AddComponent<Canvas>();
                targetCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                targetCanvas.sortingOrder = 15;
                var scaler = canvasGo.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1280, 720);
                scaler.matchWidthOrHeight = 0.5f;
                canvasGo.AddComponent<GraphicRaycaster>();
            }

            // System button — top-right corner
            var btnGo = new GameObject("SystemBtn", typeof(RectTransform));
            btnGo.transform.SetParent(targetCanvas.transform, false);
            var rt = btnGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(1, 1);
            rt.anchoredPosition = new Vector2(-16, -16);
            rt.sizeDelta = new Vector2(100, 42);

            var img = btnGo.AddComponent<Image>();
            img.color = new Color(0.12f, 0.08f, 0.18f, 0.9f);
            var outline = btnGo.AddComponent<Outline>();
            outline.effectColor = new Color(0.8f, 0.65f, 0.3f);
            outline.effectDistance = new Vector2(1, -1);

            var btn = btnGo.AddComponent<Button>();
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.highlightedColor = new Color(0.2f, 0.15f, 0.3f);
            colors.pressedColor = new Color(0.3f, 0.2f, 0.1f);
            btn.colors = colors;
            btn.onClick.AddListener(() => _systemMenu?.Toggle());

            var textGo = new GameObject("Text", typeof(RectTransform));
            textGo.transform.SetParent(btnGo.transform, false);
            var textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;
            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text = "系统";
            tmp.fontSize = 22;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.8f, 0.65f, 0.3f);
        }

        // --- Turn Banner ---
        IEnumerator ShowTurnBanner(string text)
        {
            // Create temporary overlay canvas
            var canvasGo = new GameObject("TurnBannerCanvas");
            _turnBannerCanvas = canvasGo.AddComponent<Canvas>();
            _turnBannerCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _turnBannerCanvas.sortingOrder = 100;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 0.5f;

            // Semi-transparent background strip (center of screen)
            var bgGo = new GameObject("BannerBg", typeof(RectTransform));
            bgGo.transform.SetParent(canvasGo.transform, false);
            var bgRt = bgGo.GetComponent<RectTransform>();
            bgRt.anchorMin = new Vector2(0, 0.38f);
            bgRt.anchorMax = new Vector2(1, 0.62f);
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.color = new Color(0, 0, 0, 0.75f);

            // Banner text
            var textGo = new GameObject("BannerText", typeof(RectTransform));
            textGo.transform.SetParent(bgGo.transform, false);
            var textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;
            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 48;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(1f, 0.85f, 0.3f);
            tmp.fontStyle = FontStyles.Bold;

            // Grab the CanvasGroup for fading
            var cg = canvasGo.AddComponent<CanvasGroup>();
            cg.alpha = 0;

            // Fade in
            float t = 0;
            while (t < 0.3f)
            {
                t += Time.deltaTime;
                cg.alpha = Mathf.Clamp01(t / 0.3f);
                yield return null;
            }
            cg.alpha = 1;

            // Hold
            yield return new WaitForSeconds(1.0f);

            // Fade out
            t = 0;
            while (t < 0.3f)
            {
                t += Time.deltaTime;
                cg.alpha = 1f - Mathf.Clamp01(t / 0.3f);
                yield return null;
            }

            Destroy(canvasGo);
            _turnBannerCanvas = null;
        }

        void CleanupSceneArtifacts()
        {
            // FadeOverlay — pure white/dark Image covering screen
            var fadeOverlay = GameObject.Find("FadeOverlay");
            if (fadeOverlay != null)
            {
                Destroy(fadeOverlay);
                Debug.Log("[BattleController] Destroyed FadeOverlay");
            }

            // Unused EnemyUnits container
            var enemyUnits = GameObject.Find("EnemyUnits");
            if (enemyUnits != null && enemyUnits.transform != unitsRoot)
            {
                Destroy(enemyUnits);
                Debug.Log("[BattleController] Destroyed unused EnemyUnits");
            }

            // Destroy any scene-baked BattleCanvas (broken serialized UI)
            var battleCanvas = GameObject.Find("BattleCanvas");
            if (battleCanvas != null)
            {
                Destroy(battleCanvas);
                Debug.Log("[BattleController] Destroyed scene BattleCanvas (using programmatic UI)");
            }
        }

        // --- Map Background ---
        void EnsureMapBackground()
        {
            // Try to get background from BattleDefinition
            if (_mapBackground == null)
            {
                var gsm = ServiceLocator.Has<GameStateManager>() ? ServiceLocator.Get<GameStateManager>() : null;
                var registry = ServiceLocator.Has<GameDataRegistry>() ? ServiceLocator.Get<GameDataRegistry>() : null;
                if (gsm != null && registry != null)
                {
                    var battleDef = registry.GetBattle(gsm.State.currentBattleId);
                    if (battleDef != null)
                        _mapBackground = battleDef.battleBackground;
                }
            }

            // Fallback: try to load from Resources
            if (_mapBackground == null)
                _mapBackground = Resources.Load<Sprite>("Sprites/Battle/level0");

            if (_mapBackground == null)
            {
                Debug.LogWarning("[BattleController] No map background found.");
                return;
            }

            var bgGo = GameObject.Find("MapBackground");
            if (bgGo == null) bgGo = new GameObject("MapBackground");
            var sr = bgGo.GetComponent<SpriteRenderer>();
            if (sr == null) sr = bgGo.AddComponent<SpriteRenderer>();
            sr.sprite = _mapBackground;
            sr.sortingOrder = -100;

            // Position background so its bottom-left corner aligns with the grid Origin.
            // For any pivot, sprite bottom-left in world = pos - pivot * size
            // We want bottom-left = Origin, so pos = Origin + pivot * size
            float ppu = _mapBackground.pixelsPerUnit;
            float sprW = _mapBackground.rect.width / ppu;
            float sprH = _mapBackground.rect.height / ppu;
            float pivotX = _mapBackground.pivot.x / _mapBackground.rect.width; // normalized 0-1
            float pivotY = _mapBackground.pivot.y / _mapBackground.rect.height;

            Vector2 origin = gridMap != null ? gridMap.Origin : Vector2.zero;
            bgGo.transform.position = new Vector3(
                origin.x + pivotX * sprW,
                origin.y + pivotY * sprH,
                0
            );

            Debug.Log($"[BattleController] Map background: {_mapBackground.name} ({sprW}x{sprH} world units, ppu={ppu}), pivot=({pivotX:F2},{pivotY:F2}), pos={bgGo.transform.position}, gridOrigin={origin}");
        }

        // --- Fallback Action Menu (built programmatically) ---
        GameObject _fallbackMenuGo;
        Canvas _fallbackMenuCanvas;
        RectTransform _fallbackMenuPanel;

        void BuildActionMenu()
        {
            // Create a screen-space overlay canvas for the action menu
            var canvasGo = new GameObject("ActionMenuCanvas");
            canvasGo.transform.SetParent(transform);
            _fallbackMenuCanvas = canvasGo.AddComponent<Canvas>();
            _fallbackMenuCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _fallbackMenuCanvas.sortingOrder = 50;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            // Panel
            var panelGo = new GameObject("ActionMenuPanel");
            panelGo.transform.SetParent(canvasGo.transform, false);
            _fallbackMenuPanel = panelGo.AddComponent<RectTransform>();
            _fallbackMenuPanel.sizeDelta = new Vector2(160, 100);
            var panelImg = panelGo.AddComponent<Image>();
            panelImg.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
            var panelOutline = panelGo.AddComponent<Outline>();
            panelOutline.effectColor = new Color(0.8f, 0.7f, 0.3f);
            panelOutline.effectDistance = new Vector2(2, -2);

            var vlg = panelGo.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 4;
            vlg.padding = new RectOffset(8, 8, 8, 8);
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = true;

            // Attack button
            var atkBtn = CreateActionButton(panelGo.transform, "攻击", new Color(0.7f, 0.2f, 0.2f));
            atkBtn.onClick.AddListener(OnAttackPressed);
            // Wait button
            var waitBtn = CreateActionButton(panelGo.transform, "待机", new Color(0.3f, 0.3f, 0.5f));
            waitBtn.onClick.AddListener(OnWaitPressed);

            _fallbackMenuGo = panelGo;
            panelGo.SetActive(false);

            // Set actionMenu to null — we handle show/hide ourselves for fallback
            actionMenu = null;
        }

        Button CreateActionButton(Transform parent, string label, Color bgColor)
        {
            var go = new GameObject($"Btn_{label}");
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = bgColor;
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            var rt = textGo.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 22;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            return btn;
        }

        // Camera drag state
        bool _isDragging;
        bool _pendingClick; // true = mouse went down, waiting to see if it's a click or drag
        Vector3 _dragStartMouse;
        Vector3 _dragStartCamPos;
        const float DragThreshold = 8f; // pixels before drag starts

        void Update()
        {
            if (_battleEnded) return;

            // Block all battle input when system menu is open
            if (_systemMenu != null && _systemMenu.IsOpen) return;

            // Camera drag/scroll — always active
            HandleCameraDrag();

            var scrollMouse = UnityEngine.InputSystem.Mouse.current;
            if (scrollMouse != null)
            {
                float scroll = scrollMouse.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > 0.01f)
                {
                    var cam = Camera.main;
                    if (cam != null)
                    {
                        var pos = cam.transform.position;
                        pos.y += Mathf.Sign(scroll) * gridMap.TileSize.y * 2f;
                        cam.transform.position = pos;
                        ClampCamera();
                    }
                }
            }

            if (_currentState is EnemyTurnState || _currentState is AnimatingState)
                return;

            var input = CaoCao.Input.InputManager.Instance;
            if (input == null) return;

            if (input.CancelDown)
                _currentState?.HandleCancel();

            // Hover
            var hoverCell = gridMap.WorldToCell(GetMouseWorldPos());
            _currentState?.HandleHover(hoverCell);
        }

        void HandleCameraDrag()
        {
            var cam = Camera.main;
            if (cam == null) return;

            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (mouse == null) return;

            var leftBtn = mouse.leftButton;
            Vector3 mousePos = mouse.position.ReadValue();

            // Mouse down — start tracking
            if (leftBtn.wasPressedThisFrame)
            {
                _dragStartMouse = mousePos;
                _dragStartCamPos = cam.transform.position;
                _isDragging = false;
                _pendingClick = true;
            }

            // Mouse held — check for drag or continue dragging
            if (leftBtn.isPressed)
            {
                Vector3 delta = mousePos - _dragStartMouse;

                // Start dragging once past threshold
                if (!_isDragging && _pendingClick && delta.magnitude > DragThreshold)
                {
                    _isDragging = true;
                    _pendingClick = false;
                }

                // Continue dragging
                if (_isDragging)
                {
                    float worldPerPixel = cam.orthographicSize * 2f / Screen.height;
                    Vector3 worldDelta = new Vector3(-delta.x * worldPerPixel, -delta.y * worldPerPixel, 0);
                    cam.transform.position = _dragStartCamPos + worldDelta;
                    ClampCamera();
                }
            }

            // Mouse up — if short tap (no drag), process as click
            if (leftBtn.wasReleasedThisFrame)
            {
                if (_pendingClick && !_isDragging)
                {
                    if (!(_currentState is EnemyTurnState || _currentState is AnimatingState))
                        ProcessClick();
                }
                _pendingClick = false;
                _isDragging = false;
            }
        }

        void ProcessClick()
        {
            var cell = gridMap.WorldToCell(GetMouseWorldPos());
            var input = CaoCao.Input.InputManager.Instance;

            if (IsActionMenuShowing())
            {
                if (input != null)
                {
                    if (actionMenu != null && actionMenu.ContainsScreenPoint(input.ScreenPosition))
                        return;
                    if (_fallbackMenuPanel != null &&
                        RectTransformUtility.RectangleContainsScreenPoint(_fallbackMenuPanel, input.ScreenPosition, null))
                        return;
                }
            }
            _currentState?.HandleClick(cell);
        }

        // --- Camera Setup ---
        // World-space bounds of the viewable map (origin-based)
        float _mapMinX, _mapMinY, _mapWorldW, _mapWorldH;

        void SetupCamera()
        {
            var cam = Camera.main;
            if (cam == null) return;

            // Determine map world bounds (use background sprite size, or grid bounds)
            Vector2 origin = gridMap != null ? gridMap.Origin : Vector2.zero;
            _mapMinX = origin.x;
            _mapMinY = origin.y;

            if (_mapBackground != null)
            {
                float ppu = _mapBackground.pixelsPerUnit;
                _mapWorldW = _mapBackground.rect.width / ppu;
                _mapWorldH = _mapBackground.rect.height / ppu;
            }
            else
            {
                _mapWorldW = gridMap != null ? gridMap.MapSize.x * gridMap.TileSize.x : 12 * 48;
                _mapWorldH = gridMap != null ? gridMap.MapSize.y * gridMap.TileSize.y : 8 * 48;
            }

            cam.orthographic = true;

            // Fit camera width to map width, let height scroll if map is taller
            float screenAspect = (float)Screen.width / Screen.height;
            float orthoSizeByWidth = (_mapWorldW / screenAspect) * 0.5f;
            float orthoSizeByHeight = _mapWorldH * 0.5f;

            // Use the smaller of the two so the map fills the screen (no empty sides)
            cam.orthographicSize = Mathf.Min(orthoSizeByWidth, orthoSizeByHeight);

            // Start camera centered on the bottom portion of the map (where units usually spawn)
            float camHalfH = cam.orthographicSize;
            float centerX = _mapMinX + _mapWorldW * 0.5f;
            float startY = Mathf.Clamp(_mapMinY + camHalfH, _mapMinY + camHalfH, _mapMinY + _mapWorldH - camHalfH);
            cam.transform.position = new Vector3(centerX, startY, -10f);

            Debug.Log($"[BattleController] Camera: orthoSize={cam.orthographicSize}, mapBounds=({_mapMinX},{_mapMinY})+({_mapWorldW}x{_mapWorldH}), pos={cam.transform.position}");
        }

        /// <summary>
        /// Clamp camera within map bounds. Call after any camera movement.
        /// </summary>
        void ClampCamera()
        {
            var cam = Camera.main;
            if (cam == null) return;

            float halfH = cam.orthographicSize;
            float halfW = halfH * cam.aspect;

            float minX = _mapMinX + halfW;
            float maxX = _mapMinX + _mapWorldW - halfW;
            float minY = _mapMinY + halfH;
            float maxY = _mapMinY + _mapWorldH - halfH;

            // If map is smaller than camera view, just center
            if (minX > maxX) minX = maxX = _mapMinX + _mapWorldW * 0.5f;
            if (minY > maxY) minY = maxY = _mapMinY + _mapWorldH * 0.5f;

            var pos = cam.transform.position;
            pos.x = Mathf.Clamp(pos.x, minX, maxX);
            pos.y = Mathf.Clamp(pos.y, minY, maxY);
            cam.transform.position = pos;
        }

        /// <summary>
        /// Smoothly move camera to center on a world position (e.g., selected unit).
        /// </summary>
        public void FocusCameraOn(Vector2 worldPos)
        {
            var cam = Camera.main;
            if (cam == null) return;
            StartCoroutine(SmoothCameraMove(worldPos));
        }

        IEnumerator SmoothCameraMove(Vector2 target)
        {
            var cam = Camera.main;
            float duration = 0.3f;
            float t = 0;
            Vector3 start = cam.transform.position;
            Vector3 end = new Vector3(target.x, target.y, -10f);

            while (t < duration)
            {
                t += Time.deltaTime;
                cam.transform.position = Vector3.Lerp(start, end, t / duration);
                ClampCamera();
                yield return null;
            }
            cam.transform.position = end;
            ClampCamera();
        }

        // --- Ensure InputManager ---
        void EnsureInputManager()
        {
            if (CaoCao.Input.InputManager.Instance != null) return;

            // InputManager is on the Boot scene's GameManager GO (DontDestroyOnLoad).
            // If we loaded Battle directly (e.g., from editor), create a fallback.
            var go = new GameObject("InputManager_Fallback");
            go.AddComponent<CaoCao.Input.InputManager>();
            Debug.Log("[BattleController] Created fallback InputManager");
        }

        // --- State Machine ---
        public void SetState(IBattleState state)
        {
            _currentState?.Exit();
            _currentState = state;
            _currentState.Enter(this);
        }

        // --- Unit Selection ---
        public void SelectUnit(BattleUnit unit)
        {
            SelectedUnit = unit;
            _selectedOrigin = unit.cell;
            _movedThisTurn = false;
            ComputeReachable(unit);
            highlighter.SetMoveCells(new List<Vector2Int>(Reachable.Keys));
            ShowUnitInfo(unit);
            FocusCameraOn(gridMap.CellToWorld(unit.cell));
            SetState(new UnitSelectedState());
        }

        public void ClearSelection()
        {
            SelectedUnit = null;
            Reachable.Clear();
            _parent.Clear();
            _movedThisTurn = false;
            highlighter.ClearAll();
            HideActionMenu();
            HideUnitInfo();
            SetState(new IdleState());
        }

        // --- Movement ---
        public void MoveSelectedTo(Vector2Int cell)
        {
            SetState(new AnimatingState());
            _movedThisTurn = true;

            // Build path from parent chain (endpoint → startpoint, then reverse)
            var path = BuildPath(_selectedOrigin, cell);

            SelectedUnit.OnMoved += OnUnitMoved;
            SelectedUnit.MoveAlongPath(path);
        }

        /// <summary>
        /// Reconstruct path from parent dictionary. Returns list starting from first step (excluding start).
        /// </summary>
        List<Vector2Int> BuildPath(Vector2Int start, Vector2Int end)
        {
            var path = new List<Vector2Int>();
            var current = end;
            while (current != start && _parent.ContainsKey(current) && _parent[current].HasValue)
            {
                path.Add(current);
                current = _parent[current].Value;
            }
            path.Reverse();
            return path;
        }

        void OnUnitMoved(BattleUnit unit)
        {
            unit.OnMoved -= OnUnitMoved;
            highlighter.ClearAll();
            if (unit.team == UnitTeam.Player && _currentTeam == "player")
                ShowActionMenu(unit);
        }

        public void UndoMoveIfNeeded()
        {
            if (_movedThisTurn && SelectedUnit != null)
            {
                SelectedUnit.MoveTo(_selectedOrigin, false);
                _movedThisTurn = false;
            }
        }

        // --- Action Menu ---
        public void ShowActionMenu(BattleUnit unit)
        {
            if (actionMenu != null)
            {
                Vector2 worldPos = gridMap.CellToWorld(unit.cell);
                actionMenu.ShowAt(worldPos + new Vector2(30, -20));
            }
            else if (_fallbackMenuGo != null)
            {
                // Position fallback menu near the unit (screen space)
                var cam = Camera.main;
                if (cam != null)
                {
                    Vector2 worldPos = gridMap.CellToWorld(unit.cell) + new Vector2(30, -20);
                    Vector2 screenPos = cam.WorldToScreenPoint(worldPos);
                    _fallbackMenuPanel.position = screenPos;
                }
                _fallbackMenuGo.SetActive(true);
            }
            SetState(new ActionMenuState());
        }

        public void HideActionMenu()
        {
            if (actionMenu != null) actionMenu.Hide();
            if (_fallbackMenuGo != null) _fallbackMenuGo.SetActive(false);
        }

        bool IsActionMenuShowing()
        {
            if (actionMenu != null) return actionMenu.IsShowing;
            if (_fallbackMenuGo != null) return _fallbackMenuGo.activeSelf;
            return false;
        }

        void OnAttackPressed()
        {
            if (SelectedUnit == null) return;
            HideActionMenu();
            highlighter.SetAttackCells(GetAttackCells(SelectedUnit.cell));
            SetState(new AttackingState());
        }

        void OnWaitPressed()
        {
            HideActionMenu();
            if (SelectedUnit == null) return;
            SelectedUnit.Wait();
            EndPlayerAction();
        }

        // --- Attack ---
        public bool InAttackRange(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) == 1;
        }

        public void ExecuteAttack(BattleUnit attacker, BattleUnit target)
        {
            SetState(new AnimatingState());
            attacker.Attack(target);
            // Show target info during combat so player sees HP change
            ShowUnitInfo(target);
            StartCoroutine(AfterAttack(attacker));
        }

        IEnumerator AfterAttack(BattleUnit attacker)
        {
            yield return new WaitForSeconds(0.5f);

            // Refresh info panel after damage
            if (SelectedUnit != null && SelectedUnit.hp > 0)
                ShowUnitInfo(SelectedUnit);

            // Check win/lose after attack
            if (CheckBattleEnd()) yield break;

            attacker.Wait();
            EndPlayerAction();
        }

        void EndPlayerAction()
        {
            if (_battleEnded) return;
            highlighter.ClearAll();
            ClearSelection();
            if (AllActed(UnitTeam.Player))
                StartCoroutine(RunEnemyTurn());
        }

        // --- Enemy Turn ---
        IEnumerator RunEnemyTurn()
        {
            SetState(new EnemyTurnState());
            SetEndTurnButtonVisible(false);
            _currentTeam = "enemy";
            EventBus.TurnChanged("enemy");

            yield return StartCoroutine(ShowTurnBanner($"第{_turnCount}回合 - 敌方回合"));

            // Heal enemy units on healing terrain
            ApplyTerrainHealing(UnitTeam.Enemy);

            var enemies = GetUnitsByTeam(UnitTeam.Enemy);
            foreach (var e in enemies) e.ResetActed();

            foreach (var enemy in enemies)
            {
                if (_battleEnded) yield break;
                if (enemy.acted || enemy.hp <= 0) continue;

                var players = GetUnitsByTeam(UnitTeam.Player);
                if (players.Count == 0) { CheckBattleEnd(); yield break; }

                var target = _ai.ChooseAttackTarget(enemy, players);
                if (target != null)
                {
                    enemy.Attack(target);
                    yield return new WaitForSeconds(0.5f);
                    if (CheckBattleEnd()) yield break;
                    enemy.acted = true;
                    continue;
                }

                var path = _ai.ChooseMoveTowards(enemy, players, enemies, gridMap, _pathfinder);
                if (path.Count > 0)
                {
                    bool moved = false;
                    enemy.OnMoved += _ => moved = true;
                    enemy.MoveAlongPath(path);
                    while (!moved) yield return null;
                    enemy.OnMoved -= _ => moved = true;

                    target = _ai.ChooseAttackTarget(enemy, players);
                    if (target != null)
                    {
                        enemy.Attack(target);
                        yield return new WaitForSeconds(0.5f);
                        if (CheckBattleEnd()) yield break;
                    }
                }
                enemy.acted = true;
            }

            // End enemy turn — start new player turn
            _turnCount++;
            foreach (var p in GetUnitsByTeam(UnitTeam.Player)) p.ResetActed();
            _currentTeam = "player";
            EventBus.TurnChanged("player");

            yield return StartCoroutine(ShowTurnBanner($"第{_turnCount}回合 - 我方回合"));
            // Heal player units on healing terrain at new turn start
            ApplyTerrainHealing(UnitTeam.Player);
            SetEndTurnButtonVisible(true);
            SetState(new IdleState());
        }

        // ============================================================
        // Win / Lose Detection
        // ============================================================

        void SubscribeUnitDeathEvents()
        {
            foreach (Transform child in unitsRoot)
            {
                var unit = child.GetComponent<BattleUnit>();
                if (unit != null)
                    unit.OnDied += OnUnitDied;
            }
        }

        void OnUnitDied(BattleUnit unit)
        {
            Debug.Log($"[BattleController] Unit died: {unit.gameObject.name} (team={unit.team})");
            // Death animation is handled by BattleUnit.Attack() → BattleUnitAnimator.PlayDeath()
        }

        bool CheckBattleEnd()
        {
            if (_battleEnded) return true;

            var players = GetUnitsByTeam(UnitTeam.Player);
            var enemies = GetUnitsByTeam(UnitTeam.Enemy);

            if (enemies.Count == 0)
            {
                // Victory!
                _battleEnded = true;
                EventBus.BattleEnded(true);
                StartCoroutine(ShowBattleResult(true));
                return true;
            }

            if (players.Count == 0)
            {
                // Defeat
                _battleEnded = true;
                EventBus.BattleEnded(false);
                StartCoroutine(ShowBattleResult(false));
                return true;
            }

            return false;
        }

        IEnumerator ShowBattleResult(bool victory)
        {
            highlighter.ClearAll();
            HideActionMenu();

            yield return new WaitForSeconds(0.5f);

            // Show result overlay
            ShowResultUI(victory);
        }

        void ShowResultUI(bool victory)
        {
            // Create a simple overlay canvas
            var canvasGo = new GameObject("BattleResultCanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            // Dark overlay
            var overlay = new GameObject("Overlay");
            overlay.transform.SetParent(canvasGo.transform, false);
            var overlayRt = overlay.AddComponent<RectTransform>();
            overlayRt.anchorMin = Vector2.zero;
            overlayRt.anchorMax = Vector2.one;
            overlayRt.offsetMin = Vector2.zero;
            overlayRt.offsetMax = Vector2.zero;
            var overlayImg = overlay.AddComponent<Image>();
            overlayImg.color = new Color(0, 0, 0, 0.7f);

            // Result text
            var textGo = new GameObject("ResultText");
            textGo.transform.SetParent(canvasGo.transform, false);
            var textRt = textGo.AddComponent<RectTransform>();
            textRt.anchorMin = new Vector2(0.2f, 0.45f);
            textRt.anchorMax = new Vector2(0.8f, 0.7f);
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;
            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text = victory ? "胜 利" : "败 北";
            tmp.fontSize = 72;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = victory ? new Color(1f, 0.85f, 0.3f) : new Color(0.9f, 0.3f, 0.3f);
            tmp.fontStyle = FontStyles.Bold;

            // Sub text
            var subGo = new GameObject("SubText");
            subGo.transform.SetParent(canvasGo.transform, false);
            var subRt = subGo.AddComponent<RectTransform>();
            subRt.anchorMin = new Vector2(0.2f, 0.35f);
            subRt.anchorMax = new Vector2(0.8f, 0.45f);
            subRt.offsetMin = Vector2.zero;
            subRt.offsetMax = Vector2.zero;
            var subTmp = subGo.AddComponent<TextMeshProUGUI>();
            subTmp.text = victory
                ? $"回合数: {_turnCount}"
                : "全军覆没...";
            subTmp.fontSize = 24;
            subTmp.alignment = TextAlignmentOptions.Center;
            subTmp.color = new Color(0.8f, 0.8f, 0.8f);

            // Continue button
            var btnGo = new GameObject("ContinueBtn");
            btnGo.transform.SetParent(canvasGo.transform, false);
            var btnRt = btnGo.AddComponent<RectTransform>();
            btnRt.anchorMin = new Vector2(0.35f, 0.18f);
            btnRt.anchorMax = new Vector2(0.65f, 0.3f);
            btnRt.offsetMin = Vector2.zero;
            btnRt.offsetMax = Vector2.zero;
            var btnImg = btnGo.AddComponent<Image>();
            btnImg.color = victory ? new Color(0.15f, 0.4f, 0.15f) : new Color(0.4f, 0.15f, 0.15f);
            var btnOutline = btnGo.AddComponent<Outline>();
            btnOutline.effectColor = new Color(0.8f, 0.7f, 0.3f);
            btnOutline.effectDistance = new Vector2(2, -2);

            var btnTextGo = new GameObject("BtnText");
            btnTextGo.transform.SetParent(btnGo.transform, false);
            var btnTextRt = btnTextGo.AddComponent<RectTransform>();
            btnTextRt.anchorMin = Vector2.zero;
            btnTextRt.anchorMax = Vector2.one;
            btnTextRt.offsetMin = Vector2.zero;
            btnTextRt.offsetMax = Vector2.zero;
            var btnTmp = btnTextGo.AddComponent<TextMeshProUGUI>();
            btnTmp.text = victory ? "返回营地" : "重新开始";
            btnTmp.fontSize = 28;
            btnTmp.alignment = TextAlignmentOptions.Center;
            btnTmp.color = Color.white;

            var btn = btnGo.AddComponent<Button>();
            btn.targetGraphic = btnImg;

            if (victory)
            {
                btn.onClick.AddListener(() =>
                {
                    Destroy(canvasGo);
                    ReturnToCamp();
                });
            }
            else
            {
                btn.onClick.AddListener(() =>
                {
                    Destroy(canvasGo);
                    RetryBattle();
                });
            }
        }

        void ReturnToCamp()
        {
            Debug.Log("[BattleController] Returning to camp (StoryScene)...");

            // Sync hero HP back to save state
            SyncHeroHpToState();

            // Load StoryScene — CampManager will re-show via OnStoryCompleted
            var loader = ServiceLocator.Has<SceneLoader>() ? ServiceLocator.Get<SceneLoader>() : null;
            if (loader != null)
                loader.LoadScene("StoryScene");
            else
                UnityEngine.SceneManagement.SceneManager.LoadScene("StoryScene");
        }

        void RetryBattle()
        {
            Debug.Log("[BattleController] Retrying battle...");

            // Reload the battle scene
            var sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            var loader = ServiceLocator.Has<SceneLoader>() ? ServiceLocator.Get<SceneLoader>() : null;
            if (loader != null)
                loader.LoadScene(sceneName);
            else
                UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
        }

        void SyncHeroHpToState()
        {
            var gsm = ServiceLocator.Has<GameStateManager>() ? ServiceLocator.Get<GameStateManager>() : null;
            if (gsm == null) return;

            foreach (Transform child in unitsRoot)
            {
                var unit = child.GetComponent<BattleUnit>();
                if (unit == null || unit.team != UnitTeam.Player) continue;

                // Match by name pattern "Hero_XXX"
                string goName = unit.gameObject.name;
                // Find hero runtime data to update
                foreach (var hero in gsm.GetRecruitedHeroes())
                {
                    var heroDef = gsm.Registry?.GetHero(hero.heroId);
                    if (heroDef != null && goName.Contains(heroDef.displayName))
                    {
                        hero.currentHp = Mathf.Max(1, unit.hp); // Don't let HP stay at 0 after victory
                        break;
                    }
                }
            }
        }

        // --- Pathfinding ---
        void ComputeReachable(BattleUnit unit)
        {
            _pathfinder.MapSize = gridMap.MapSize;
            _pathfinder.GetCost = c => gridMap.GetCost(c, unit.movementType);
            _pathfinder.IsPassable = c => gridMap.IsPassable(c, unit.movementType);
            _pathfinder.IsBlocked = c => IsOccupied(c, unit);
            var result = _pathfinder.ComputeReachable(unit.cell, unit.mov);
            Reachable = result.Reachable;
            _parent = result.Parent;
        }

        // --- Helpers ---
        public BattleUnit GetUnitAt(Vector2Int cell)
        {
            foreach (Transform child in unitsRoot)
            {
                var u = child.GetComponent<BattleUnit>();
                if (u != null && u.cell == cell && u.hp > 0)
                    return u;
            }
            return null;
        }

        bool IsOccupied(Vector2Int cell, BattleUnit ignore)
        {
            foreach (Transform child in unitsRoot)
            {
                var u = child.GetComponent<BattleUnit>();
                if (u != null && u != ignore && u.cell == cell && u.hp > 0)
                    return true;
            }
            return false;
        }

        List<BattleUnit> GetUnitsByTeam(UnitTeam team)
        {
            var list = new List<BattleUnit>();
            foreach (Transform child in unitsRoot)
            {
                var u = child.GetComponent<BattleUnit>();
                if (u != null && u.team == team && u.hp > 0)
                    list.Add(u);
            }
            return list;
        }

        bool AllActed(UnitTeam team)
        {
            foreach (var u in GetUnitsByTeam(team))
                if (!u.acted) return false;
            return true;
        }

        List<Vector2Int> GetAttackCells(Vector2Int cell)
        {
            return new List<Vector2Int>
            {
                cell + Vector2Int.up, cell + Vector2Int.down,
                cell + Vector2Int.left, cell + Vector2Int.right
            };
        }

        public void UpdateHover(Vector2Int cell)
        {
            highlighter.SetHoverCell(cell);
            if (hud != null)
            {
                var info = gridMap.GetTerrainInfo(cell);
                hud.UpdateTerrain(info);
            }
        }

        Vector2 GetMouseWorldPos()
        {
            var input = CaoCao.Input.InputManager.Instance;
            if (input == null) return Vector2.zero;
            return input.GetWorldPosition();
        }

        /// <summary>
        /// Spawn units from GameStateManager deployment data.
        /// Returns true if deployment data was available, false to fall back to demo.
        /// </summary>
        bool SpawnFromDeployment()
        {
            var gsm = ServiceLocator.Has<GameStateManager>() ? ServiceLocator.Get<GameStateManager>() : null;
            var registry = ServiceLocator.Has<GameDataRegistry>() ? ServiceLocator.Get<GameDataRegistry>() : null;
            if (gsm == null || registry == null) return false;

            var deployedIds = gsm.GetDeployment();
            if (deployedIds == null || deployedIds.Count == 0) return false;

            // Get battle definition
            var battleDef = registry.GetBattle(gsm.State.currentBattleId);
            Debug.Log($"[BattleController] SpawnFromDeployment: battleId={gsm.State.currentBattleId}, battleDef={(battleDef != null ? battleDef.battleName : "NULL")}, deployed={deployedIds.Count}");

            // Spawn player heroes
            int optionalIndex = 0;
            foreach (var heroId in deployedIds)
            {
                var heroData = gsm.GetHero(heroId);
                var heroDef = registry.GetHero(heroId);
                if (heroData == null || heroDef == null) continue;

                // Determine spawn cell
                Vector2Int spawnCell = new(2 + optionalIndex, 2);
                bool isRequired = false;

                // Check required heroes for specific spawn positions
                if (battleDef?.requiredHeroes != null)
                {
                    foreach (var req in battleDef.requiredHeroes)
                    {
                        if (req.unitId == heroId)
                        {
                            spawnCell = req.startCell;
                            isRequired = true;
                            break;
                        }
                    }
                }

                // Use optional spawn points for non-required heroes
                if (!isRequired && battleDef?.playerSpawnPoints != null && optionalIndex < battleDef.playerSpawnPoints.Length)
                {
                    spawnCell = battleDef.playerSpawnPoints[optionalIndex];
                    optionalIndex++;
                }
                else if (isRequired)
                {
                    // Don't consume optional spawn index for required heroes
                }
                else
                {
                    optionalIndex++;
                }

                var go = new GameObject($"Hero_{heroDef.displayName}");
                go.transform.SetParent(unitsRoot);
                var unit = go.AddComponent<BattleUnit>();
                unit.tileSize = tileSize;
                unit.cell = spawnCell;
                unit.InitFromHero(heroData, heroDef);

                Debug.Log($"[BattleController] Spawned player: {heroDef.displayName} at {spawnCell}, HP={unit.hp}/{unit.maxHp}, ATK={unit.atk}, DEF={unit.def}, MOV={unit.mov}");
            }

            // Spawn enemy units from battle definition with stats
            if (battleDef?.enemyUnits != null)
            {
                foreach (var enemy in battleDef.enemyUnits)
                {
                    string displayName = !string.IsNullOrEmpty(enemy.displayName) ? enemy.displayName : enemy.unitId;
                    var go = new GameObject($"Enemy_{displayName}");
                    go.transform.SetParent(unitsRoot);
                    var unit = go.AddComponent<BattleUnit>();
                    unit.team = UnitTeam.Enemy;
                    unit.tileSize = tileSize;
                    unit.cell = enemy.startCell;

                    // Initialize enemy identity and stats from placement data
                    unit.displayName = displayName;
                    unit.unitTypeName = "敌兵";
                    unit.level = 1;
                    unit.maxHp = enemy.maxHp;
                    unit.hp = enemy.maxHp;
                    unit.atk = enemy.atk;
                    unit.def = enemy.def;
                    unit.mov = enemy.mov;

                    Debug.Log($"[BattleController] Spawned enemy: {displayName} at {enemy.startCell}, HP={unit.maxHp}, ATK={unit.atk}, DEF={unit.def}, MOV={unit.mov}");
                }
            }

            return true;
        }

        void SpawnDemoUnits()
        {
            if (unitsRoot.childCount > 0) return;
            SpawnUnit(new Vector2Int(2, 2), UnitTeam.Player);
            SpawnUnit(new Vector2Int(4, 3), UnitTeam.Player);
            SpawnUnit(new Vector2Int(8, 5), UnitTeam.Enemy);
            SpawnUnit(new Vector2Int(9, 2), UnitTeam.Enemy);
        }

        void SpawnUnit(Vector2Int cell, UnitTeam team)
        {
            var go = new GameObject($"Unit_{team}_{cell}");
            go.transform.SetParent(unitsRoot);
            var unit = go.AddComponent<BattleUnit>();
            unit.team = team;
            unit.tileSize = tileSize;
            unit.cell = cell;
        }

        void SyncMapSize()
        {
            if (gridMap != null)
            {
                // If no tilemap is present but we have a background, derive grid size from background
                if (_mapBackground != null && gridMap.MapSize.x * gridMap.TileSize.x < _mapBackground.rect.width / _mapBackground.pixelsPerUnit * 0.9f)
                {
                    float ppu = _mapBackground.pixelsPerUnit;
                    int gridW = Mathf.FloorToInt(_mapBackground.rect.width / ppu / gridMap.TileSize.x);
                    int gridH = Mathf.FloorToInt(_mapBackground.rect.height / ppu / gridMap.TileSize.y);
                    Debug.Log($"[BattleController] Resizing grid to match background: {gridW}x{gridH}");
                    gridMap.Rebuild(new Vector2Int(gridW, gridH));
                }

                // Sync tileSize from gridMap (which reads from Grid component)
                tileSize = gridMap.TileSize;
                highlighter.Origin = gridMap.Origin;
                highlighter.TileSize = tileSize;
            }
        }
    }
}
