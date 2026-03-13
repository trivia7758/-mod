using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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

        void Start()
        {
            // Try deployment-based spawning first, fall back to demo
            if (!SpawnFromDeployment())
                SpawnDemoUnits();
            SyncMapSize();

            if (actionMenu != null)
            {
                actionMenu.OnAttack += OnAttackPressed;
                actionMenu.OnWait += OnWaitPressed;
                actionMenu.Hide();
            }

            SetState(new IdleState());
        }

        void Update()
        {
            if (_currentState is EnemyTurnState || _currentState is AnimatingState)
                return;

            var input = CaoCao.Input.InputManager.Instance;
            if (input == null) return;

            if (input.ClickDown)
            {
                var cell = gridMap.WorldToCell(GetMouseWorldPos());
                // Check if click is on action menu (skip grid click)
                if (actionMenu != null && actionMenu.IsShowing && actionMenu.ContainsScreenPoint(input.ScreenPosition))
                    return;
                _currentState?.HandleClick(cell);
            }

            if (input.CancelDown)
                _currentState?.HandleCancel();

            // Hover
            var hoverCell = gridMap.WorldToCell(GetMouseWorldPos());
            _currentState?.HandleHover(hoverCell);
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
            if (actionMenu != null) actionMenu.Hide();
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
            SetState(new ActionMenuState());
        }

        public void HideActionMenu()
        {
            if (actionMenu != null) actionMenu.Hide();
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
            attacker.Wait();
            EndPlayerAction();
        }

        void EndPlayerAction()
        {
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
                if (enemy.acted || enemy.hp <= 0) continue;

                var players = GetUnitsByTeam(UnitTeam.Player);
                var target = _ai.ChooseAttackTarget(enemy, players);
                if (target != null)
                {
                    enemy.Attack(target);
                    yield return new WaitForSeconds(0.3f);
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
                    }
                }
                enemy.acted = true;
            }

            // End enemy turn
            foreach (var p in GetUnitsByTeam(UnitTeam.Player)) p.acted = false;
            _currentTeam = "player";
            EventBus.TurnChanged("player");
            SetState(new IdleState());
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

            // Spawn player heroes
            int spawnIndex = 0;
            foreach (var heroId in deployedIds)
            {
                var heroData = gsm.GetHero(heroId);
                var heroDef = registry.GetHero(heroId);
                if (heroData == null || heroDef == null) continue;

                // Determine spawn cell
                Vector2Int spawnCell = new(2 + spawnIndex, 2);

                // Check required heroes for specific spawn positions
                if (battleDef?.requiredHeroes != null)
                {
                    foreach (var req in battleDef.requiredHeroes)
                    {
                        if (req.unitId == heroId)
                        {
                            spawnCell = req.startCell;
                            break;
                        }
                    }
                }

                // Check optional spawn points
                if (battleDef?.playerSpawnPoints != null && spawnIndex < battleDef.playerSpawnPoints.Length)
                {
                    bool isRequired = false;
                    if (battleDef.requiredHeroes != null)
                    {
                        foreach (var req in battleDef.requiredHeroes)
                        {
                            if (req.unitId == heroId) { isRequired = true; break; }
                        }
                    }
                    if (!isRequired)
                        spawnCell = battleDef.playerSpawnPoints[spawnIndex];
                }

                var go = new GameObject($"Hero_{heroDef.displayName}");
                go.transform.SetParent(unitsRoot);
                var unit = go.AddComponent<BattleUnit>();
                unit.tileSize = tileSize;
                unit.cell = spawnCell;
                unit.InitFromHero(heroData, heroDef);

                spawnIndex++;
            }

            // Spawn enemy units from battle definition
            if (battleDef?.enemyUnits != null)
            {
                foreach (var enemy in battleDef.enemyUnits)
                {
                    var go = new GameObject($"Enemy_{enemy.unitId}");
                    go.transform.SetParent(unitsRoot);
                    var unit = go.AddComponent<BattleUnit>();
                    unit.team = UnitTeam.Enemy;
                    unit.tileSize = tileSize;
                    unit.cell = enemy.startCell;
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
