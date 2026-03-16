using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CaoCao.Battle.States;
using CaoCao.Battle.Events;
using CaoCao.Common;
using CaoCao.Core;
using CaoCao.Data;
using UnityEngine.EventSystems;

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
        BattleHeroInfoPanel _heroInfoPanel;

        public BattleGridMap GridMap => gridMap;
        public TileHighlighter Highlighter => highlighter;
        public BattleUnit SelectedUnit { get; private set; }
        public Dictionary<Vector2Int, int> Reachable { get; private set; } = new();
        public int TurnCount => _turnCount;

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

        // Camera follow
        Coroutine _cameraFollowCo;
        Transform _cameraFollowTarget;

        // Battle event system
        BattleEventManager _eventManager;
        string _battleDefName;

        // Skill/Item selection panel
        GameObject _selectionPanelGo;
        Canvas _selectionCanvas;
        RectTransform _selectionContent;

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

            // Initialize battle event system
            InitEventManager();

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

            // Battle start events (opening dialogue, etc.)
            if (_eventManager != null)
                yield return _eventManager.CheckBattleStart();

            // Heal player units on healing terrain at turn start
            ApplyTerrainHealing(UnitTeam.Player);

            // Turn start events
            if (_eventManager != null)
                yield return _eventManager.CheckTurnStart(_turnCount);

            SetEndTurnButtonVisible(true);
            SetState(new IdleState());
        }

        void InitEventManager()
        {
            _eventManager = GetComponent<BattleEventManager>();
            if (_eventManager == null)
                _eventManager = gameObject.AddComponent<BattleEventManager>();

            // Determine battle name for loading event file
            // Use the battle definition name if available, otherwise "demo"
            string battleName = _battleDefName ?? "demo";
            _eventManager.Init(this, battleName);
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
                var info = gridMap.GetTerrainInfo(unit.cell, unit.unitClass);
                // Use DB heal% (e.g. 村庄15%, 兵营20%), fallback to legacy HealPerTurn
                int healAmt = info.HealPercent > 0
                    ? Mathf.FloorToInt(unit.maxHp * info.HealPercent / 100f)
                    : info.HealPerTurn;
                if (healAmt > 0 && unit.hp < unit.maxHp)
                {
                    int healed = Mathf.Min(healAmt, unit.maxHp - unit.hp);
                    unit.hp += healed;
                    Debug.Log($"[BattleController] {unit.displayName} 在{info.Name}恢复{healed}HP (now {unit.hp}/{unit.maxHp})");
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
        Button _fallbackAtkBtn;
        Button _fallbackSkillBtn;
        Button _fallbackItemBtn;
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
            _fallbackMenuPanel.sizeDelta = new Vector2(160, 192);
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
            _fallbackAtkBtn = atkBtn;
            // Skill button
            var skillBtn = CreateActionButton(panelGo.transform, "技能", new Color(0.2f, 0.45f, 0.7f));
            skillBtn.onClick.AddListener(OnSkillPressed);
            _fallbackSkillBtn = skillBtn;
            // Item button
            var itemBtn = CreateActionButton(panelGo.transform, "道具", new Color(0.2f, 0.6f, 0.3f));
            itemBtn.onClick.AddListener(OnItemPressed);
            _fallbackItemBtn = itemBtn;
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

        // Long-press state (for hero info panel)
        float _pressStartTime;
        bool _longPressTriggered;
        const float LongPressDuration = 0.5f; // seconds

        void Update()
        {
            if (_battleEnded) return;

            // Block all battle input when system menu or hero info panel is open
            if (_systemMenu != null && _systemMenu.IsOpen) return;
            if (_heroInfoPanel != null && _heroInfoPanel.IsOpen) return;

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
                _pressStartTime = Time.unscaledTime;
                _longPressTriggered = false;
            }

            // Mouse held — check for drag, long press, or continue dragging
            if (leftBtn.isPressed)
            {
                Vector3 delta = mousePos - _dragStartMouse;

                // Start dragging once past threshold
                if (!_isDragging && _pendingClick && delta.magnitude > DragThreshold)
                {
                    _isDragging = true;
                    _pendingClick = false;
                }

                // Long press detection (only if not dragging and not already triggered)
                if (!_isDragging && !_longPressTriggered && _pendingClick
                    && Time.unscaledTime - _pressStartTime >= LongPressDuration)
                {
                    _longPressTriggered = true;
                    _pendingClick = false; // consume the click
                    OnLongPress();
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
            // Block click-through when clicking on any UI element (buttons, menus, etc.)
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            var cell = gridMap.WorldToCell(GetMouseWorldPos());
            _currentState?.HandleClick(cell);
        }

        void OnLongPress()
        {
            // Block when clicking on UI
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            var cell = gridMap.WorldToCell(GetMouseWorldPos());
            var unit = GetUnitAt(cell);
            if (unit == null) return;

            ShowHeroInfoPanel(unit);
        }

        void ShowHeroInfoPanel(BattleUnit unit)
        {
            if (_heroInfoPanel == null)
            {
                var go = new GameObject("BattleHeroInfoPanel");
                go.transform.SetParent(transform);
                _heroInfoPanel = go.AddComponent<BattleHeroInfoPanel>();
                _heroInfoPanel.OnClosed += OnHeroInfoPanelClosed;
            }

            _heroInfoPanel.Show(unit);
            Debug.Log($"[BattleController] Showing hero info for: {unit.displayName}");
        }

        void OnHeroInfoPanelClosed()
        {
            // Nothing special needed — battle state continues as before
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

        // --- Camera Follow ---

        /// <summary>
        /// Start smoothly following a unit's transform each frame (e.g., during movement).
        /// Call StopCameraFollow() when the action is done.
        /// </summary>
        public void StartCameraFollow(Transform target)
        {
            StopCameraFollow();
            if (target == null) return;
            _cameraFollowTarget = target;
            _cameraFollowCo = StartCoroutine(CameraFollowCoroutine());
        }

        public void StopCameraFollow()
        {
            if (_cameraFollowCo != null)
            {
                StopCoroutine(_cameraFollowCo);
                _cameraFollowCo = null;
            }
            _cameraFollowTarget = null;
        }

        IEnumerator CameraFollowCoroutine()
        {
            var cam = Camera.main;
            if (cam == null) yield break;

            float smoothSpeed = 8f;
            while (_cameraFollowTarget != null)
            {
                Vector3 targetPos = new Vector3(
                    _cameraFollowTarget.position.x,
                    _cameraFollowTarget.position.y,
                    cam.transform.position.z);
                cam.transform.position = Vector3.Lerp(cam.transform.position, targetPos, smoothSpeed * Time.deltaTime);
                ClampCamera();
                yield return null;
            }
        }

        /// <summary>
        /// Focus camera on a unit, following during movement, then snap when done.
        /// Used for enemy AI actions.
        /// </summary>
        IEnumerator FocusCameraOnUnit(BattleUnit unit)
        {
            if (unit == null) yield break;
            var cam = Camera.main;
            if (cam == null) yield break;

            Vector3 target = new Vector3(unit.transform.position.x, unit.transform.position.y, -10f);
            float duration = 0.25f;
            float t = 0;
            Vector3 start = cam.transform.position;
            while (t < duration)
            {
                t += Time.deltaTime;
                cam.transform.position = Vector3.Lerp(start, target, t / duration);
                ClampCamera();
                yield return null;
            }
            cam.transform.position = target;
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
            ShowRangeHighlights(unit, Reachable);
            ShowUnitInfo(unit);
            FocusCameraOn(gridMap.CellToWorld(unit.cell));
            SetState(new UnitSelectedState());
        }

        /// <summary>
        /// Show movement + attack range + terrain effects for a unit.
        /// Used for both player selection and enemy preview.
        /// </summary>
        void ShowRangeHighlights(BattleUnit unit, Dictionary<Vector2Int, int> reachable)
        {
            // Build effect% dictionary for move cells
            var moveEffects = new Dictionary<Vector2Int, int>();
            foreach (var cell in reachable.Keys)
                moveEffects[cell] = gridMap.GetEffect(cell, unit.unitClass);

            highlighter.SetMoveCells(new List<Vector2Int>(reachable.Keys), moveEffects);

            // Show red attack range around unit's CURRENT position (no effect labels)
            var atkCells = GetAttackCells(unit.cell, unit.atkRange);
            highlighter.SetAttackCells(atkCells);
        }

        /// <summary>Preview enemy unit's movement + attack range (read-only, no selection).</summary>
        public void PreviewEnemyRange(BattleUnit enemy)
        {
            // Compute reachable for enemy
            _pathfinder.MapSize = gridMap.MapSize;
            _pathfinder.GetCost = c => gridMap.GetCost(c, enemy.unitClass);
            _pathfinder.IsPassable = c => gridMap.IsPassable(c, enemy.unitClass);
            _pathfinder.IsBlocked = c => IsOccupied(c, enemy);
            var result = _pathfinder.ComputeReachable(enemy.cell, enemy.mov);

            // Show blue movement range with effects
            ShowRangeHighlights(enemy, result.Reachable);

            // Show red attack range around enemy's CURRENT position (no effect labels)
            var atkCells = GetAttackCells(enemy.cell, enemy.atkRange);
            highlighter.SetAttackCells(atkCells);
        }

        /// <summary>Clear enemy range preview.</summary>
        public void ClearEnemyPreview()
        {
            highlighter.ClearAll();
        }

        public void ClearSelection()
        {
            SelectedUnit = null;
            Reachable.Clear();
            _parent.Clear();
            _movedThisTurn = false;
            _pendingSkill = null;
            _pendingItem = null;
            highlighter.ClearAll();
            HideActionMenu();
            CloseSelectionPanel();
            HideUnitInfo();
            SetState(new IdleState());
        }

        // --- Movement ---
        public void MoveSelectedTo(Vector2Int cell)
        {
            SetState(new AnimatingState());
            _movedThisTurn = true;

            // Clear range highlights while unit is walking
            highlighter.ClearAll();

            // Build path from parent chain (endpoint → startpoint, then reverse)
            var path = BuildPath(_selectedOrigin, cell);

            // Camera follows the moving unit
            StartCameraFollow(SelectedUnit.transform);

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
            StopCameraFollow();
            highlighter.ClearAll();
            StartCoroutine(OnUnitMovedWithEvents(unit));
        }

        IEnumerator OnUnitMovedWithEvents(BattleUnit unit)
        {
            // Check battle events triggered by movement (proximity, area_enter)
            if (_eventManager != null)
                yield return _eventManager.CheckAfterMove(unit);

            if (unit.team == UnitTeam.Player && _currentTeam == "player" && !_battleEnded)
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
            // Check if any enemy is within attack range
            bool hasTarget = HasEnemyInRange(unit);

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

                // Gray out attack button if no enemies in range
                if (_fallbackAtkBtn != null)
                {
                    _fallbackAtkBtn.interactable = hasTarget;
                    var colors = _fallbackAtkBtn.colors;
                    colors.disabledColor = new Color(0.3f, 0.3f, 0.3f, 0.6f);
                    _fallbackAtkBtn.colors = colors;
                }

                // Skill: gray out if unit has no usable skills or insufficient MP
                if (_fallbackSkillBtn != null)
                {
                    bool hasSkills = false;
                    foreach (var sk in unit.skills)
                    {
                        if (sk != null && sk.usableInBattle && unit.mp >= sk.mpCost)
                        { hasSkills = true; break; }
                    }
                    _fallbackSkillBtn.interactable = hasSkills;
                    var sc = _fallbackSkillBtn.colors;
                    sc.disabledColor = new Color(0.3f, 0.3f, 0.3f, 0.6f);
                    _fallbackSkillBtn.colors = sc;
                }

                // Item: gray out if shared inventory has no usable consumable items
                if (_fallbackItemBtn != null)
                {
                    bool hasItems = false;
                    var sharedItems = GetSharedInventory();
                    foreach (var stack in sharedItems)
                    {
                        if (stack != null && stack.count > 0)
                        {
                            var itemDef = FindItemDef(stack.itemId);
                            if (itemDef != null && itemDef.usableInBattle &&
                                itemDef.itemType == Common.ItemType.Consumable)
                            { hasItems = true; break; }
                        }
                    }
                    _fallbackItemBtn.interactable = hasItems;
                    var ic = _fallbackItemBtn.colors;
                    ic.disabledColor = new Color(0.3f, 0.3f, 0.3f, 0.6f);
                    _fallbackItemBtn.colors = ic;
                }

                _fallbackMenuGo.SetActive(true);
            }
            SetState(new ActionMenuState());
        }

        /// <summary>Check if any enemy unit is within attack range of the given unit.</summary>
        bool HasEnemyInRange(BattleUnit unit)
        {
            var enemies = GetUnitsByTeam(unit.team == UnitTeam.Player ? UnitTeam.Enemy : UnitTeam.Player);
            foreach (var e in enemies)
            {
                if (e.hp <= 0) continue;
                int dist = Mathf.Abs(unit.cell.x - e.cell.x) + Mathf.Abs(unit.cell.y - e.cell.y);
                if (dist >= 1 && dist <= unit.atkRange)
                    return true;
            }
            return false;
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
            // Show red attack range cells around unit's current position (no effect labels)
            var atkCells = GetAttackCells(SelectedUnit.cell, SelectedUnit.atkRange);
            highlighter.SetAttackCells(atkCells);
            SetState(new AttackingState());
        }

        void OnSkillPressed()
        {
            if (SelectedUnit == null) return;
            HideActionMenu();
            ShowSkillPanel(SelectedUnit);
        }

        void OnItemPressed()
        {
            if (SelectedUnit == null) return;
            HideActionMenu();
            ShowItemPanel(SelectedUnit);
        }

        // ── Skill Selection Panel ──

        void ShowSkillPanel(BattleUnit unit)
        {
            CloseSelectionPanel();

            if (unit.skills.Count == 0)
            {
                ShowActionMenu(unit);
                return;
            }

            // Build card data
            var cards = new List<CardData>();
            foreach (var sk in unit.skills)
            {
                if (sk == null || !sk.usableInBattle) continue;
                bool canUse = unit.mp >= sk.mpCost;
                string effectText = sk.effectType switch
                {
                    Common.SkillEffectType.Damage => "攻击策略",
                    Common.SkillEffectType.Heal   => "恢复策略",
                    Common.SkillEffectType.Buff    => "辅助策略",
                    Common.SkillEffectType.Debuff  => "妨碍策略",
                    _ => "策略"
                };
                Color effectColor = sk.effectType switch
                {
                    Common.SkillEffectType.Damage => new Color(1f, 0.4f, 0.3f),
                    Common.SkillEffectType.Heal   => new Color(0.3f, 1f, 0.5f),
                    Common.SkillEffectType.Buff    => new Color(0.4f, 0.7f, 1f),
                    Common.SkillEffectType.Debuff  => new Color(0.9f, 0.6f, 1f),
                    _ => Color.white
                };
                string costText = $"MP-{sk.mpCost}";
                var captured = sk;
                cards.Add(new CardData
                {
                    name = sk.displayName,
                    effectText = effectText,
                    effectColor = effectColor,
                    bottomText = costText,
                    icon = null, // TODO: sk.icon
                    enabled = canUse,
                    onClick = () => OnSkillSelected(unit, captured)
                });
            }

            BuildCardPanel(unit.displayName, unit.mp, unit.maxMp, cards,
                () => { CloseSelectionPanel(); ShowActionMenu(unit); });
        }

        void OnSkillSelected(BattleUnit unit, Data.SkillDefinition skill)
        {
            CloseSelectionPanel();

            if (skill.effectType == Common.SkillEffectType.Heal)
            {
                // Heal: show diamond range and enter heal targeting state
                var healCells = GetHealRangeCells(unit.cell, 4);
                highlighter.SetHealCells(healCells);
                _pendingSkill = skill;
                SetState(new HealTargetingState());
            }
            else if (skill.effectType == Common.SkillEffectType.Damage)
            {
                // Damage skill: show attack range and enter targeting state
                var atkCells = GetAttackCells(unit.cell, skill.range);
                highlighter.SetAttackCells(atkCells);
                _pendingSkill = skill;
                SetState(new SkillTargetingState());
            }
            else
            {
                // Buff/debuff: apply to self for now
                unit.UseSkill(skill, unit);
                unit.Wait();
                EndPlayerAction();
            }
        }

        Data.SkillDefinition _pendingSkill;

        /// <summary>Execute pending skill on a target (called from SkillTargetingState).</summary>
        public void ExecuteSkillOn(BattleUnit target)
        {
            if (SelectedUnit == null || _pendingSkill == null) return;
            SetState(new AnimatingState());
            // Focus camera on skill target
            FocusCameraOn(gridMap.CellToWorld(target.cell));
            SelectedUnit.UseSkill(_pendingSkill, target);
            ShowUnitInfo(target);
            _pendingSkill = null;
            StartCoroutine(AfterSkill(SelectedUnit, target));
        }

        IEnumerator AfterSkill(BattleUnit caster, BattleUnit target)
        {
            yield return new WaitForSeconds(0.5f);
            if (CheckBattleEnd()) yield break;

            // Check battle events after skill use
            if (_eventManager != null)
                yield return _eventManager.CheckAfterCombat(caster, target);

            if (CheckBattleEnd()) yield break;

            caster.Wait();
            EndPlayerAction();
        }

        public Data.SkillDefinition PendingSkill => _pendingSkill;

        /// <summary>
        /// Execute a heal skill on a friendly target (called from HealTargetingState).
        /// </summary>
        public void ExecuteHealSkillOn(BattleUnit target)
        {
            if (SelectedUnit == null || _pendingSkill == null) return;
            SetState(new AnimatingState());
            FocusCameraOn(gridMap.CellToWorld(target.cell));
            SelectedUnit.UseSkill(_pendingSkill, target);
            ShowUnitInfo(target);
            _pendingSkill = null;
            highlighter.ClearAll();
            StartCoroutine(AfterHealSkill(SelectedUnit));
        }

        IEnumerator AfterHealSkill(BattleUnit caster)
        {
            yield return new WaitForSeconds(0.5f);
            caster.Wait();
            EndPlayerAction();
        }

        /// <summary>
        /// Get heal range cells: diamond shape (Manhattan distance ≤ radius).
        /// radius=4 gives the traditional 曹操传 heal range:
        ///     dy=0: 9 wide (4 left + self + 4 right)
        ///     dy=±1: 7 wide
        ///     dy=±2: 5 wide
        ///     dy=±3: 3 wide
        ///     dy=±4: 1 wide
        /// </summary>
        List<Vector2Int> GetHealRangeCells(Vector2Int center, int radius)
        {
            var cells = new List<Vector2Int>();
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    int dist = Mathf.Abs(dx) + Mathf.Abs(dy);
                    if (dist <= radius)
                    {
                        var c = center + new Vector2Int(dx, dy);
                        if (gridMap.IsInBounds(c))
                            cells.Add(c);
                    }
                }
            }
            return cells;
        }

        // ── Item Selection Panel ──

        void ShowItemPanel(BattleUnit unit)
        {
            CloseSelectionPanel();

            var cards = new List<CardData>();
            // Read from shared party inventory (all heroes share items)
            var sharedItems = GetSharedInventory();
            foreach (var stack in sharedItems)
            {
                if (stack == null || stack.count <= 0) continue;
                var itemDef = FindItemDef(stack.itemId);
                if (itemDef == null || !itemDef.usableInBattle) continue;
                if (itemDef.itemType != Common.ItemType.Consumable) continue;

                // Items are always selectable — target validation happens in ItemTargetingState
                bool canUse = true;

                // Effect text & color
                string effectText = "";
                Color effectColor = new Color(0.3f, 1f, 0.5f);
                if (itemDef.healAmount > 0)
                {
                    effectText = "恢复生命";
                    effectColor = new Color(0.3f, 1f, 0.5f);
                }
                else if (itemDef.mpBonus > 0)
                {
                    effectText = "恢复法力";
                    effectColor = new Color(0.4f, 0.7f, 1f);
                }

                var capturedDef = itemDef;
                cards.Add(new CardData
                {
                    name = itemDef.displayName,
                    effectText = effectText,
                    effectColor = effectColor,
                    bottomText = stack.count.ToString(),
                    icon = itemDef.icon,
                    enabled = canUse,
                    onClick = () => OnItemSelected(unit, capturedDef)
                });
            }

            if (cards.Count == 0)
            {
                ShowActionMenu(unit);
                return;
            }

            BuildCardPanel(unit.displayName, unit.mp, unit.maxMp, cards,
                () => { CloseSelectionPanel(); ShowActionMenu(unit); });
        }

        void OnItemSelected(BattleUnit unit, Data.ItemDefinition itemDef)
        {
            CloseSelectionPanel();

            // Show 3x3 item usage range (九宫格) around unit
            var itemCells = GetItemRangeCells(unit.cell);
            highlighter.SetAttackCells(itemCells); // red highlight for item range
            _pendingItem = itemDef;
            SetState(new ItemTargetingState());
        }

        Data.ItemDefinition _pendingItem;

        /// <summary>
        /// Get the shared party inventory (all heroes share the same item pool).
        /// Falls back to a demo inventory if GameStateManager is not available.
        /// </summary>
        List<Data.ItemStack> GetSharedInventory()
        {
            var gsm = ServiceLocator.Has<GameStateManager>()
                ? ServiceLocator.Get<GameStateManager>() : null;
            if (gsm != null)
                return gsm.State.inventory;

            // Fallback: use _demoSharedInventory for demo mode
            return _demoSharedInventory;
        }

        /// <summary>Demo fallback shared inventory (when no GameStateManager).</summary>
        List<Data.ItemStack> _demoSharedInventory = new();

        /// <summary>
        /// Consume 1 item from shared inventory.
        /// Uses GameStateManager.RemoveItem if available, otherwise manual removal.
        /// </summary>
        bool ConsumeSharedItem(string itemId)
        {
            var gsm = ServiceLocator.Has<GameStateManager>()
                ? ServiceLocator.Get<GameStateManager>() : null;
            if (gsm != null)
                return gsm.RemoveItem(itemId, 1);

            // Fallback: manual removal from demo inventory
            var stack = _demoSharedInventory.Find(s => s.itemId == itemId && s.count > 0);
            if (stack == null) return false;
            stack.count--;
            if (stack.count <= 0) _demoSharedInventory.Remove(stack);
            return true;
        }

        /// <summary>Get 3x3 grid cells around center (九宫格, including self).</summary>
        List<Vector2Int> GetItemRangeCells(Vector2Int center)
        {
            var cells = new List<Vector2Int>();
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    var c = new Vector2Int(center.x + dx, center.y + dy);
                    if (gridMap.IsInBounds(c))
                        cells.Add(c);
                }
            }
            return cells;
        }

        /// <summary>Execute pending item on a target cell (called from ItemTargetingState).</summary>
        public void ExecuteItemOn(Vector2Int targetCell)
        {
            if (SelectedUnit == null || _pendingItem == null) return;

            // Check if there's a friendly unit at that cell (or self)
            var target = GetUnitAt(targetCell);
            if (target == null || target.team != SelectedUnit.team)
                return; // no valid target on that cell

            // Validate: HP potion on full HP target is wasted, don't allow
            if (_pendingItem.healAmount > 0 && _pendingItem.mpBonus <= 0 && target.hp >= target.maxHp)
            {
                Debug.Log($"[Battle] {target.displayName} HP已满，无法使用 {_pendingItem.displayName}");
                return; // stay in targeting state, let player pick another target
            }
            if (_pendingItem.mpBonus > 0 && _pendingItem.healAmount <= 0 && target.mp >= target.maxMp)
            {
                Debug.Log($"[Battle] {target.displayName} MP已满，无法使用 {_pendingItem.displayName}");
                return;
            }

            // Consume 1 from shared party inventory
            if (!ConsumeSharedItem(_pendingItem.id)) { _pendingItem = null; return; }

            // Focus camera on item target
            FocusCameraOn(gridMap.CellToWorld(targetCell));

            // Apply effect to target
            if (_pendingItem.healAmount > 0)
            {
                int oldHp = target.hp;
                target.hp = Mathf.Min(target.maxHp, target.hp + _pendingItem.healAmount);
                Debug.Log($"[Battle] {SelectedUnit.displayName} 对 {target.displayName} 使用 {_pendingItem.displayName}: HP {oldHp} → {target.hp}");
            }
            if (_pendingItem.mpBonus > 0)
            {
                int oldMp = target.mp;
                target.mp = Mathf.Min(target.maxMp, target.mp + _pendingItem.mpBonus);
                Debug.Log($"[Battle] {SelectedUnit.displayName} 对 {target.displayName} 使用 {_pendingItem.displayName}: MP {oldMp} → {target.mp}");
            }

            // Update target's HP bar immediately after item use
            target.UpdateHpBar();

            ShowUnitInfo(target);
            highlighter.ClearAll();
            _pendingItem = null;
            SelectedUnit.Wait();
            EndPlayerAction();
        }

        public Data.ItemDefinition PendingItem => _pendingItem;

        /// <summary>Find ItemDefinition by id from registry, Resources, or demo fallback.</summary>
        readonly Dictionary<string, Data.ItemDefinition> _itemDefCache = new();

        Data.ItemDefinition FindItemDef(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return null;
            if (_itemDefCache.TryGetValue(itemId, out var cached)) return cached;

            // Try GameDataRegistry first
            var registry = CaoCao.Core.ServiceLocator.Get<Data.GameDataRegistry>();
            if (registry != null)
            {
                var def = registry.GetItem(itemId);
                if (def != null) { _itemDefCache[itemId] = def; return def; }
            }
            // Fallback: try Resources
            var allItems = Resources.LoadAll<Data.ItemDefinition>("Data/Items");
            foreach (var it in allItems)
            {
                if (it.id == itemId) { _itemDefCache[itemId] = it; return it; }
            }

            // Demo fallback: create runtime items for testing
            var demo = CreateDemoItem(itemId);
            if (demo != null) _itemDefCache[itemId] = demo;
            return demo;
        }

        static Data.ItemDefinition CreateDemoItem(string itemId)
        {
            var item = ScriptableObject.CreateInstance<Data.ItemDefinition>();
            item.id = itemId;
            item.itemType = Common.ItemType.Consumable;
            item.usableInBattle = true;
            item.usableInCamp = true;

            switch (itemId)
            {
                case "hp_potion":
                    item.displayName = "回复药";
                    item.description = "恢复少量HP";
                    item.healAmount = 50;
                    break;
                case "hp_potion_large":
                    item.displayName = "大回复药";
                    item.description = "恢复大量HP";
                    item.healAmount = 120;
                    break;
                case "mp_potion":
                    item.displayName = "策略恢复药";
                    item.description = "恢复少量MP";
                    item.mpBonus = 30;
                    break;
                default:
                    return null;
            }
            return item;
        }

        // ── Card Data & Card Panel Builder (曹操传 style) ──

        struct CardData
        {
            public string name;
            public string effectText;   // e.g. "恢复生命", "攻击策略"
            public Color effectColor;
            public string bottomText;   // quantity for items, "MP-X" for skills
            public Sprite icon;
            public bool enabled;
            public System.Action onClick;
        }

        /// <summary>
        /// Build a card-grid panel like the original 曹操传.
        /// Top: hero name + MP bar.  Body: grid of cards (icon + name + effect + count/cost).
        /// </summary>
        void BuildCardPanel(string heroName, int mp, int maxMp,
            List<CardData> cards, System.Action onCancel)
        {
            // ── Canvas ──
            var canvasGo = new GameObject("SelectionPanelCanvas");
            _selectionCanvas = canvasGo.AddComponent<Canvas>();
            _selectionCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _selectionCanvas.sortingOrder = 55;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            // ── Semi-transparent full-screen backdrop ──
            var backdropGo = new GameObject("Backdrop", typeof(RectTransform));
            backdropGo.transform.SetParent(canvasGo.transform, false);
            var bdRt = backdropGo.GetComponent<RectTransform>();
            bdRt.anchorMin = Vector2.zero;
            bdRt.anchorMax = Vector2.one;
            bdRt.offsetMin = Vector2.zero;
            bdRt.offsetMax = Vector2.zero;
            var bdImg = backdropGo.AddComponent<Image>();
            bdImg.color = new Color(0, 0, 0, 0.5f);
            var bdBtn = backdropGo.AddComponent<Button>();
            bdBtn.targetGraphic = bdImg;
            bdBtn.onClick.AddListener(() => onCancel?.Invoke());

            // ── Main panel (centered, large) ──
            int cols = Mathf.Min(cards.Count, 6);
            if (cols < 3) cols = 3;
            int rows = Mathf.CeilToInt((float)cards.Count / cols);
            float cardW = 145f;
            float cardH = 155f;
            float gap = 8f;
            float panelW = cols * cardW + (cols - 1) * gap + 32;
            float panelH = 60 + rows * cardH + (rows - 1) * gap + 60; // header + cards + footer
            panelW = Mathf.Min(panelW, 960);
            panelH = Mathf.Min(panelH, 560);

            _selectionPanelGo = new GameObject("CardPanel", typeof(RectTransform));
            _selectionPanelGo.transform.SetParent(canvasGo.transform, false);
            var panelRt = _selectionPanelGo.GetComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0.5f, 0.5f);
            panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.pivot = new Vector2(0.5f, 0.5f);
            panelRt.sizeDelta = new Vector2(panelW, panelH);
            var panelImg = _selectionPanelGo.AddComponent<Image>();
            panelImg.color = new Color(0.1f, 0.1f, 0.12f, 0.95f);
            // Gold border
            var outline = _selectionPanelGo.AddComponent<Outline>();
            outline.effectColor = new Color(0.65f, 0.55f, 0.25f);
            outline.effectDistance = new Vector2(2, -2);

            // ── Header: hero name + MP bar ──
            var headerGo = new GameObject("Header", typeof(RectTransform));
            headerGo.transform.SetParent(_selectionPanelGo.transform, false);
            var headerRt = headerGo.GetComponent<RectTransform>();
            headerRt.anchorMin = new Vector2(0, 1);
            headerRt.anchorMax = new Vector2(1, 1);
            headerRt.pivot = new Vector2(0.5f, 1);
            headerRt.anchoredPosition = Vector2.zero;
            headerRt.sizeDelta = new Vector2(0, 50);

            // Hero name
            var nameGo = CreateTmpChild(headerGo, "HeroName", heroName, 24,
                new Color(1f, 0.95f, 0.8f), TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
            var nameRt = nameGo.GetComponent<RectTransform>();
            nameRt.anchorMin = new Vector2(0, 0);
            nameRt.anchorMax = new Vector2(0.4f, 1);
            nameRt.offsetMin = new Vector2(16, 8);
            nameRt.offsetMax = new Vector2(0, -4);

            // "MP" label
            var mpLabelGo = CreateTmpChild(headerGo, "MPLabel", "MP", 20,
                Color.white, TextAlignmentOptions.MidlineRight, FontStyles.Normal);
            var mpLabelRt = mpLabelGo.GetComponent<RectTransform>();
            mpLabelRt.anchorMin = new Vector2(0.4f, 0);
            mpLabelRt.anchorMax = new Vector2(0.48f, 1);
            mpLabelRt.offsetMin = new Vector2(0, 8);
            mpLabelRt.offsetMax = new Vector2(0, -4);

            // MP bar background
            var mpBarBgGo = new GameObject("MpBarBg", typeof(RectTransform));
            mpBarBgGo.transform.SetParent(headerGo.transform, false);
            var mpBarBgRt = mpBarBgGo.GetComponent<RectTransform>();
            mpBarBgRt.anchorMin = new Vector2(0.49f, 0.35f);
            mpBarBgRt.anchorMax = new Vector2(0.82f, 0.65f);
            mpBarBgRt.offsetMin = Vector2.zero;
            mpBarBgRt.offsetMax = Vector2.zero;
            var mpBarBgImg = mpBarBgGo.AddComponent<Image>();
            mpBarBgImg.color = new Color(0.15f, 0.15f, 0.2f);

            // MP bar fill
            var mpBarFillGo = new GameObject("MpBarFill", typeof(RectTransform));
            mpBarFillGo.transform.SetParent(mpBarBgGo.transform, false);
            var mpBarFillRt = mpBarFillGo.GetComponent<RectTransform>();
            float mpRatio = maxMp > 0 ? (float)mp / maxMp : 0;
            mpBarFillRt.anchorMin = Vector2.zero;
            mpBarFillRt.anchorMax = new Vector2(mpRatio, 1);
            mpBarFillRt.offsetMin = Vector2.zero;
            mpBarFillRt.offsetMax = Vector2.zero;
            var mpBarFillImg = mpBarFillGo.AddComponent<Image>();
            mpBarFillImg.color = new Color(0.2f, 0.65f, 0.95f);

            // MP text (e.g. "48/48")
            var mpTextGo = CreateTmpChild(headerGo, "MPText", $"{mp}/{maxMp}", 20,
                new Color(0.6f, 0.9f, 1f), TextAlignmentOptions.MidlineLeft, FontStyles.Normal);
            var mpTextRt = mpTextGo.GetComponent<RectTransform>();
            mpTextRt.anchorMin = new Vector2(0.83f, 0);
            mpTextRt.anchorMax = new Vector2(1f, 1);
            mpTextRt.offsetMin = new Vector2(4, 8);
            mpTextRt.offsetMax = new Vector2(-8, -4);

            // ── Scroll area for cards ──
            var scrollGo = new GameObject("ScrollView", typeof(RectTransform));
            scrollGo.transform.SetParent(_selectionPanelGo.transform, false);
            var scrollRt = scrollGo.GetComponent<RectTransform>();
            scrollRt.anchorMin = new Vector2(0, 0);
            scrollRt.anchorMax = new Vector2(1, 1);
            scrollRt.offsetMin = new Vector2(8, 48);   // bottom: cancel button
            scrollRt.offsetMax = new Vector2(-8, -50);  // top: header
            var scrollRect = scrollGo.AddComponent<ScrollRect>();
            scrollGo.AddComponent<RectMask2D>();
            var scrollImg = scrollGo.AddComponent<Image>();
            scrollImg.color = new Color(0, 0, 0, 0);  // transparent, needed for drag
            scrollImg.raycastTarget = true;

            var contentGo = new GameObject("Content", typeof(RectTransform));
            contentGo.transform.SetParent(scrollGo.transform, false);
            _selectionContent = contentGo.GetComponent<RectTransform>();
            _selectionContent.anchorMin = new Vector2(0, 1);
            _selectionContent.anchorMax = new Vector2(1, 1);
            _selectionContent.pivot = new Vector2(0.5f, 1);
            _selectionContent.anchoredPosition = Vector2.zero;
            scrollRect.content = _selectionContent;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;

            // Grid layout
            var grid = contentGo.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(cardW, cardH);
            grid.spacing = new Vector2(gap, gap);
            grid.padding = new RectOffset(8, 8, 8, 8);
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.UpperCenter;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = cols;

            var csf = contentGo.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // ── Create cards ──
            foreach (var card in cards)
            {
                CreateCard(contentGo.transform, card);
            }

            // ── Cancel / 返回 button at bottom ──
            var cancelGo = new GameObject("CancelBtn", typeof(RectTransform));
            cancelGo.transform.SetParent(_selectionPanelGo.transform, false);
            var cancelRt = cancelGo.GetComponent<RectTransform>();
            cancelRt.anchorMin = new Vector2(0.3f, 0);
            cancelRt.anchorMax = new Vector2(0.7f, 0);
            cancelRt.pivot = new Vector2(0.5f, 0);
            cancelRt.anchoredPosition = new Vector2(0, 8);
            cancelRt.sizeDelta = new Vector2(0, 36);
            var cancelImg = cancelGo.AddComponent<Image>();
            cancelImg.color = new Color(0.35f, 0.2f, 0.15f, 0.95f);
            var cancelOutline = cancelGo.AddComponent<Outline>();
            cancelOutline.effectColor = new Color(0.6f, 0.5f, 0.25f);
            cancelOutline.effectDistance = new Vector2(1, -1);
            var cancelBtn = cancelGo.AddComponent<Button>();
            cancelBtn.targetGraphic = cancelImg;
            cancelBtn.onClick.AddListener(() => onCancel?.Invoke());
            CreateTmpChild(cancelGo, "Text", "返回", 20, Color.white,
                TextAlignmentOptions.Center, FontStyles.Normal, true);
        }

        /// <summary>Create a single card in the grid (曹操传 style).</summary>
        void CreateCard(Transform parent, CardData card)
        {
            // Card root
            var cardGo = new GameObject("Card", typeof(RectTransform));
            cardGo.transform.SetParent(parent, false);
            var cardImg = cardGo.AddComponent<Image>();
            cardImg.color = card.enabled ? new Color(0.14f, 0.13f, 0.16f, 0.95f)
                                          : new Color(0.1f, 0.1f, 0.1f, 0.7f);
            // Gold border
            var cardOutline = cardGo.AddComponent<Outline>();
            cardOutline.effectColor = card.enabled ? new Color(0.6f, 0.5f, 0.25f)
                                                    : new Color(0.3f, 0.28f, 0.2f);
            cardOutline.effectDistance = new Vector2(2, -2);

            var btn = cardGo.AddComponent<Button>();
            btn.targetGraphic = cardImg;
            btn.interactable = card.enabled;
            var btnColors = btn.colors;
            btnColors.normalColor = Color.white;
            btnColors.highlightedColor = new Color(1.2f, 1.15f, 1f);
            btnColors.pressedColor = new Color(0.8f, 0.75f, 0.65f);
            btnColors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.6f);
            btn.colors = btnColors;

            var captured = card.onClick;
            btn.onClick.AddListener(() => captured?.Invoke());

            // ── Icon area (top portion of card) ──
            var iconGo = new GameObject("Icon", typeof(RectTransform));
            iconGo.transform.SetParent(cardGo.transform, false);
            var iconRt = iconGo.GetComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0.2f, 0.5f);
            iconRt.anchorMax = new Vector2(0.8f, 0.95f);
            iconRt.offsetMin = Vector2.zero;
            iconRt.offsetMax = Vector2.zero;
            var iconImg = iconGo.AddComponent<Image>();
            if (card.icon != null)
            {
                iconImg.sprite = card.icon;
                iconImg.preserveAspect = true;
            }
            else
            {
                // Placeholder: dark square with subtle icon hint
                iconImg.color = new Color(0.2f, 0.18f, 0.22f, 0.8f);
            }
            iconImg.raycastTarget = false;

            // ── Name (below icon) ──
            var nameGo = CreateTmpChild(cardGo, "Name", card.name, 18,
                card.enabled ? Color.white : new Color(0.5f, 0.5f, 0.5f),
                TextAlignmentOptions.Center, FontStyles.Bold);
            var nameRt = nameGo.GetComponent<RectTransform>();
            nameRt.anchorMin = new Vector2(0, 0.28f);
            nameRt.anchorMax = new Vector2(1, 0.48f);
            nameRt.offsetMin = new Vector2(4, 0);
            nameRt.offsetMax = new Vector2(-4, 0);

            // ── Effect text (colored, e.g. "恢复生命") ──
            var effectGo = CreateTmpChild(cardGo, "Effect", card.effectText, 14,
                card.enabled ? card.effectColor : new Color(0.4f, 0.4f, 0.4f),
                TextAlignmentOptions.Center, FontStyles.Bold);
            var effectRt = effectGo.GetComponent<RectTransform>();
            effectRt.anchorMin = new Vector2(0, 0.12f);
            effectRt.anchorMax = new Vector2(1, 0.3f);
            effectRt.offsetMin = new Vector2(4, 0);
            effectRt.offsetMax = new Vector2(-4, 0);

            // ── Bottom text (quantity or MP cost) ──
            var bottomGo = CreateTmpChild(cardGo, "Bottom", card.bottomText, 16,
                card.enabled ? new Color(0.9f, 0.85f, 0.7f) : new Color(0.4f, 0.4f, 0.4f),
                TextAlignmentOptions.Center, FontStyles.Normal);
            var bottomRt = bottomGo.GetComponent<RectTransform>();
            bottomRt.anchorMin = new Vector2(0, 0);
            bottomRt.anchorMax = new Vector2(1, 0.14f);
            bottomRt.offsetMin = new Vector2(4, 2);
            bottomRt.offsetMax = new Vector2(-4, 0);
        }

        /// <summary>Helper: create a TMP text child with full RectTransform stretch.</summary>
        GameObject CreateTmpChild(GameObject parent, string goName, string text, float fontSize,
            Color color, TextAlignmentOptions align, FontStyles style, bool stretch = false)
        {
            var go = new GameObject(goName, typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);
            if (stretch)
            {
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = align;
            tmp.fontStyle = style;
            tmp.raycastTarget = false;
            return go;
        }

        void CloseSelectionPanel()
        {
            if (_selectionCanvas != null)
            {
                Destroy(_selectionCanvas.gameObject);
                _selectionCanvas = null;
                _selectionPanelGo = null;
                _selectionContent = null;
            }
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
            int dist = Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
            int range = SelectedUnit != null ? SelectedUnit.atkRange : 1;
            return dist >= 1 && dist <= range;
        }

        public void ExecuteAttack(BattleUnit attacker, BattleUnit target)
        {
            SetState(new AnimatingState());
            // Focus camera on the combat area (midpoint between attacker and target)
            Vector2 mid = (gridMap.CellToWorld(attacker.cell) + gridMap.CellToWorld(target.cell)) * 0.5f;
            FocusCameraOn(mid);
            attacker.Attack(target);
            // Show target info during combat so player sees HP change
            ShowUnitInfo(target);
            StartCoroutine(AfterAttack(attacker, target));
        }

        IEnumerator AfterAttack(BattleUnit attacker, BattleUnit target)
        {
            yield return new WaitForSeconds(0.5f);

            // Refresh info panel after damage
            if (SelectedUnit != null && SelectedUnit.hp > 0)
                ShowUnitInfo(SelectedUnit);

            // Check win/lose after attack
            if (CheckBattleEnd()) yield break;

            // Check battle events (unit death, HP threshold, proximity)
            if (_eventManager != null)
                yield return _eventManager.CheckAfterCombat(attacker, target);

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

                // Focus camera on the active enemy before its action
                yield return StartCoroutine(FocusCameraOnUnit(enemy));

                var target = _ai.ChooseAttackTarget(enemy, players);
                if (target != null)
                {
                    // Focus on combat midpoint
                    Vector2 mid = (gridMap.CellToWorld(enemy.cell) + gridMap.CellToWorld(target.cell)) * 0.5f;
                    FocusCameraOn(mid);
                    enemy.Attack(target);
                    yield return new WaitForSeconds(0.5f);

                    // Check events after enemy attack
                    if (_eventManager != null)
                        yield return _eventManager.CheckAfterCombat(enemy, target);

                    if (CheckBattleEnd()) yield break;
                    enemy.acted = true;
                    continue;
                }

                var path = _ai.ChooseMoveTowards(enemy, players, enemies, gridMap, _pathfinder);
                if (path.Count > 0)
                {
                    // Camera follows enemy during movement
                    StartCameraFollow(enemy.transform);
                    bool moved = false;
                    enemy.OnMoved += _ => moved = true;
                    enemy.MoveAlongPath(path);
                    while (!moved) yield return null;
                    enemy.OnMoved -= _ => moved = true;
                    StopCameraFollow();

                    // Check events after enemy move (proximity, area_enter)
                    if (_eventManager != null)
                        yield return _eventManager.CheckAfterMove(enemy);

                    if (_battleEnded) yield break;

                    target = _ai.ChooseAttackTarget(enemy, players);
                    if (target != null)
                    {
                        // Focus on combat midpoint
                        Vector2 mid = (gridMap.CellToWorld(enemy.cell) + gridMap.CellToWorld(target.cell)) * 0.5f;
                        FocusCameraOn(mid);
                        enemy.Attack(target);
                        yield return new WaitForSeconds(0.5f);

                        // Check events after enemy attack
                        if (_eventManager != null)
                            yield return _eventManager.CheckAfterCombat(enemy, target);

                        if (CheckBattleEnd()) yield break;
                    }
                }
                enemy.acted = true;
                yield return new WaitForSeconds(0.2f); // Brief pause between enemy actions
            }

            // End enemy turn — start new player turn
            _turnCount++;
            foreach (var p in GetUnitsByTeam(UnitTeam.Player)) p.ResetActed();
            _currentTeam = "player";
            EventBus.TurnChanged("player");

            yield return StartCoroutine(ShowTurnBanner($"第{_turnCount}回合 - 我方回合"));

            // Turn start events for new player turn
            if (_eventManager != null)
                yield return _eventManager.CheckTurnStart(_turnCount);

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
            _pathfinder.GetCost = c => gridMap.GetCost(c, unit.unitClass);
            _pathfinder.IsPassable = c => gridMap.IsPassable(c, unit.unitClass);
            _pathfinder.IsBlocked = c => IsOccupied(c, unit);
            var result = _pathfinder.ComputeReachable(unit.cell, unit.mov);
            Reachable = result.Reachable;
            _parent = result.Parent;

            // Debug: trace movement range calculation
            Debug.Log($"[ComputeReachable] {unit.displayName}: cell={unit.cell}, MOV={unit.mov}, unitClass={unit.unitClass}, mapSize={gridMap.MapSize}, reachable={Reachable.Count} cells");
            // Log costs of adjacent cells for debugging
            var dirs = new Vector2Int[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            foreach (var d in dirs)
            {
                var n = unit.cell + d;
                bool inBounds = gridMap.IsInBounds(n);
                int cost = inBounds ? gridMap.GetCost(n, unit.unitClass) : -99;
                bool passable = inBounds && gridMap.IsPassable(n, unit.unitClass);
                bool blocked = IsOccupied(n, unit);
                var tt = inBounds ? gridMap.GetTerrainType(n) : null;
                string ttInfo = tt != null ? $"id={tt.terrainId}, db={(tt.DbData != null ? "OK" : "NULL")}" : "null";
                Debug.Log($"  neighbor {n}: inBounds={inBounds}, cost={cost}, passable={passable}, blocked={blocked}, terrain=[{ttInfo}]");
            }
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

        public List<BattleUnit> GetUnitsByTeam(UnitTeam team)
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

        // ── Battle Event API ──

        /// <summary>
        /// Find a BattleUnit by displayName. Returns first match.
        /// </summary>
        public BattleUnit FindUnitByName(string name, bool includeDead = false)
        {
            if (string.IsNullOrEmpty(name) || unitsRoot == null) return null;
            foreach (Transform child in unitsRoot)
            {
                var u = child.GetComponent<BattleUnit>();
                if (u == null) continue;
                if (!includeDead && u.hp <= 0) continue;
                if (u.displayName == name) return u;
            }
            // Fallback: partial match on unitTypeName
            foreach (Transform child in unitsRoot)
            {
                var u = child.GetComponent<BattleUnit>();
                if (u == null) continue;
                if (!includeDead && u.hp <= 0) continue;
                if (!string.IsNullOrEmpty(u.unitTypeName) && u.unitTypeName == name) return u;
            }
            return null;
        }

        /// <summary>
        /// Spawn a new unit at runtime (for battle events: reinforcements, ambushes).
        /// </summary>
        public BattleUnit SpawnEventUnit(string displayName, UnitTeam team, Vector2Int cell,
            int hp, int atk, int def, int mov)
        {
            var go = new GameObject($"EventUnit_{displayName}");
            go.transform.SetParent(unitsRoot, false);
            var unit = go.AddComponent<BattleUnit>();
            unit.displayName = displayName;
            unit.team = team;
            unit.cell = cell;
            unit.maxHp = hp;
            unit.hp = hp;
            unit.atk = atk;
            unit.def = def;
            unit.mov = mov;
            unit.atkRange = 1;
            unit.tileSize = tileSize;
            unit.unitClass = Common.UnitClass.Infantry;

            // Force immediate visual initialization (normally happens in Start)
            // Position first so the unit appears in the right place
            unit.UpdatePosition();
            // EnsureVisual is called by BattleUnit.Start() on next frame,
            // which creates sprite, animator, HP bar, and team tint.

            // Subscribe death event
            unit.OnDied += OnUnitDied;

            Debug.Log($"[BattleController] Event spawned: {displayName} ({team}) at {cell}, HP={hp}");
            return unit;
        }

        /// <summary>Get all cells within attack range from a position (Manhattan distance 1..atkRange).</summary>
        List<Vector2Int> GetAttackCells(Vector2Int center, int atkRange = 1)
        {
            var cells = new List<Vector2Int>();
            for (int dx = -atkRange; dx <= atkRange; dx++)
            {
                for (int dy = -atkRange; dy <= atkRange; dy++)
                {
                    int dist = Mathf.Abs(dx) + Mathf.Abs(dy);
                    if (dist < 1 || dist > atkRange) continue;
                    var c = center + new Vector2Int(dx, dy);
                    if (gridMap.IsInBounds(c))
                        cells.Add(c);
                }
            }
            return cells;
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
            if (battleDef != null) _battleDefName = battleDef.battleName;
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

                Debug.Log($"[BattleController] Spawned player: {heroDef.displayName} at {spawnCell}, HP={unit.hp}/{unit.maxHp}, ATK={unit.atk}, DEF={unit.def}, MOV={unit.mov}, unitClass={unit.unitClass}, moveType={unit.movementType}");
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
            SpawnUnit(new Vector2Int(2, 2), UnitTeam.Player, "曹操");
            SpawnUnit(new Vector2Int(4, 3), UnitTeam.Player, "夏侯惇");
            SpawnUnit(new Vector2Int(8, 5), UnitTeam.Enemy, "黄巾头目");
            SpawnUnit(new Vector2Int(9, 2), UnitTeam.Enemy, "黄巾兵");
        }

        void SpawnUnit(Vector2Int cell, UnitTeam team, string name = null)
        {
            var go = new GameObject($"Unit_{team}_{cell}");
            go.transform.SetParent(unitsRoot);
            var unit = go.AddComponent<BattleUnit>();
            unit.team = team;
            unit.tileSize = tileSize;
            unit.cell = cell;
            // Demo: default to cavalry for testing terrain effects
            unit.unitClass = Common.UnitClass.Cavalry;
            unit.movementType = Common.MovementType.Cavalry;
            unit.displayName = name ?? $"{(team == UnitTeam.Player ? "我军" : "敌军")}骑兵";
            unit.unitTypeName = "骑兵";
            unit.mov = 7;
            unit.atkRange = 1;
            unit.maxMp = 20;
            unit.mp = 20;

            // Give player units demo skills for testing
            if (team == UnitTeam.Player)
            {
                // Demo items go into shared party inventory (not per-unit)
                // Only add once (when first player unit is spawned)
                if (_demoSharedInventory.Count == 0)
                {
                    _demoSharedInventory.Add(new Data.ItemStack("hp_potion", 5));
                    _demoSharedInventory.Add(new Data.ItemStack("hp_potion_large", 2));
                }

                // Demo skills: create runtime skill definitions
                unit.skills.Add(CreateDemoSkill("fire_attack", "火攻", Common.SkillEffectType.Damage, 8, 2, 15));
                unit.skills.Add(CreateDemoSkill("heal", "治疗", Common.SkillEffectType.Heal, 6, 1, 20));
            }

            Debug.Log($"[SpawnUnit] {unit.displayName} at {cell}, class={unit.unitClass}, atkRange={unit.atkRange}, mov={unit.mov}, skills={unit.skills.Count}");
        }

        /// <summary>Create a runtime SkillDefinition for demo/testing.</summary>
        static Data.SkillDefinition CreateDemoSkill(string id, string name,
            Common.SkillEffectType effectType, int mpCost, int range, int power)
        {
            var sk = ScriptableObject.CreateInstance<Data.SkillDefinition>();
            sk.id = id;
            sk.displayName = name;
            sk.effectType = effectType;
            sk.mpCost = mpCost;
            sk.range = range;
            sk.power = power;
            sk.usableInBattle = true;
            return sk;
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
