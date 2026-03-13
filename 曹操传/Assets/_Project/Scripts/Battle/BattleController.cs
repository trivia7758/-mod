using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CaoCao.Battle.States;
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

        void Start()
        {
            EnsureBattleComponents();
            SetupCamera();
            EnsureInputManager();

            // Try deployment-based spawning first, fall back to demo
            if (!SpawnFromDeployment())
                SpawnDemoUnits();
            SyncMapSize();

            // Subscribe to unit death for cleanup
            SubscribeUnitDeathEvents();

            if (actionMenu != null)
            {
                actionMenu.OnAttack += OnAttackPressed;
                actionMenu.OnWait += OnWaitPressed;
                actionMenu.Hide();
            }

            EventBus.BattleStarted();
            Debug.Log($"[BattleController] Battle started! Players={GetUnitsByTeam(UnitTeam.Player).Count}, Enemies={GetUnitsByTeam(UnitTeam.Enemy).Count}");
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
        void CleanupSceneArtifacts()
        {
            // FadeOverlay — pure white Image covering screen
            var fadeOverlay = GameObject.Find("FadeOverlay");
            if (fadeOverlay != null)
            {
                fadeOverlay.SetActive(false);
                Debug.Log("[BattleController] Disabled FadeOverlay");
            }

            // Unused EnemyUnits container
            var enemyUnits = GameObject.Find("EnemyUnits");
            if (enemyUnits != null && enemyUnits.transform != unitsRoot)
            {
                Destroy(enemyUnits);
                Debug.Log("[BattleController] Destroyed unused EnemyUnits");
            }
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

        void Update()
        {
            if (_battleEnded) return;
            if (_currentState is EnemyTurnState || _currentState is AnimatingState)
                return;

            var input = CaoCao.Input.InputManager.Instance;
            if (input == null) return;

            if (input.ClickDown)
            {
                var cell = gridMap.WorldToCell(GetMouseWorldPos());
                // Check if click is on action menu (skip grid click)
                if (IsActionMenuShowing())
                {
                    if (actionMenu != null && actionMenu.ContainsScreenPoint(input.ScreenPosition))
                        return;
                    if (_fallbackMenuPanel != null &&
                        RectTransformUtility.RectangleContainsScreenPoint(_fallbackMenuPanel, input.ScreenPosition, null))
                        return;
                }
                _currentState?.HandleClick(cell);
            }

            if (input.CancelDown)
                _currentState?.HandleCancel();

            // Hover
            var hoverCell = gridMap.WorldToCell(GetMouseWorldPos());
            _currentState?.HandleHover(hoverCell);
        }

        // --- Camera Setup ---
        void SetupCamera()
        {
            var cam = Camera.main;
            if (cam == null) return;

            // Battle grid: 12x8 tiles of 48x48 = 576x384 pixel world
            // Center camera on grid, orthographic size = half height in world units
            float mapWorldW = gridMap != null ? gridMap.MapSize.x * tileSize.x : 12 * 48;
            float mapWorldH = gridMap != null ? gridMap.MapSize.y * tileSize.y : 8 * 48;

            cam.orthographic = true;
            cam.orthographicSize = mapWorldH * 0.5f;
            cam.transform.position = new Vector3(mapWorldW * 0.5f, mapWorldH * 0.5f, -10f);

            Debug.Log($"[BattleController] Camera set: orthoSize={cam.orthographicSize}, pos={cam.transform.position}");
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
            SetState(new IdleState());
        }

        // --- Movement ---
        public void MoveSelectedTo(Vector2Int cell)
        {
            SetState(new AnimatingState());
            _movedThisTurn = true;
            SelectedUnit.OnMoved += OnUnitMoved;
            SelectedUnit.MoveTo(cell, true);
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
            StartCoroutine(AfterAttack(attacker));
        }

        IEnumerator AfterAttack(BattleUnit attacker)
        {
            yield return new WaitForSeconds(0.2f);

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
            _currentTeam = "enemy";
            EventBus.TurnChanged("enemy");

            var enemies = GetUnitsByTeam(UnitTeam.Enemy);
            foreach (var e in enemies) e.acted = false;

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
                    yield return new WaitForSeconds(0.3f);
                    if (CheckBattleEnd()) yield break;
                    enemy.acted = true;
                    continue;
                }

                var path = _ai.ChooseMoveTowards(enemy, players, enemies, gridMap, _pathfinder);
                if (path.Count > 0)
                {
                    bool moved = false;
                    enemy.OnMoved += _ => moved = true;
                    enemy.MoveTo(path[^1], true);
                    while (!moved) yield return null;
                    enemy.OnMoved -= _ => moved = true;

                    target = _ai.ChooseAttackTarget(enemy, players);
                    if (target != null)
                    {
                        enemy.Attack(target);
                        yield return new WaitForSeconds(0.3f);
                        if (CheckBattleEnd()) yield break;
                    }
                }
                enemy.acted = true;
            }

            // End enemy turn — start new player turn
            _turnCount++;
            foreach (var p in GetUnitsByTeam(UnitTeam.Player)) p.acted = false;
            _currentTeam = "player";
            EventBus.TurnChanged("player");
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

            // Hide dead unit (keep GO for potential revival/animation)
            var sr = unit.GetComponentInChildren<SpriteRenderer>();
            if (sr != null) sr.enabled = false;

            // Hide HP label
            var canvas = unit.GetComponentInChildren<Canvas>();
            if (canvas != null) canvas.gameObject.SetActive(false);
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
            _pathfinder.GetCost = c => gridMap.GetCost(c);
            _pathfinder.IsPassable = c => gridMap.IsPassable(c);
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

                    // Initialize enemy stats from placement data
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
                highlighter.Origin = gridMap.Origin;
                highlighter.TileSize = tileSize;
            }
        }
    }
}
