using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CaoCao.Battle.States;

namespace CaoCao.Battle.Events
{
    /// <summary>
    /// Manages battle event loading, trigger detection, and execution.
    /// Attach to the same GameObject as BattleController, or call Init() manually.
    /// </summary>
    public class BattleEventManager : MonoBehaviour
    {
        BattleController _ctrl;
        BattleEventScript _script;
        BattleEventContext _ctx;
        BattleEventExecutor _executor;
        bool _executing; // true while an event is running

        /// <summary>True if an event is currently executing (battle is paused).</summary>
        public bool IsExecuting => _executing;

        /// <summary>
        /// Initialize with a battle controller and load events from Resources.
        /// Call from BattleController.Start() after units are spawned.
        /// </summary>
        public void Init(BattleController controller, string battleName)
        {
            _ctrl = controller;
            _ctx = new BattleEventContext(controller);

            // Try to load battle event DSL file
            string path = $"Data/BattleEvents/{battleName}";
            var textAsset = Resources.Load<TextAsset>(path);
            if (textAsset != null)
            {
                var parser = new BattleEventDSLParser();
                _script = parser.Parse(textAsset.text);
                Debug.Log($"[BattleEventManager] Loaded {_script.Events.Count} events from '{path}'");
            }
            else
            {
                _script = new BattleEventScript();
                Debug.Log($"[BattleEventManager] No event file found at '{path}', running without events");
            }

            _executor = new BattleEventExecutor(controller, _ctx, _script);
        }

        /// <summary>
        /// Initialize with a pre-parsed script (for testing or custom loading).
        /// </summary>
        public void Init(BattleController controller, BattleEventScript script)
        {
            _ctrl = controller;
            _ctx = new BattleEventContext(controller);
            _script = script ?? new BattleEventScript();
            _executor = new BattleEventExecutor(controller, _ctx, _script);
        }

        // ──────────────────────────────────────────────
        //  Trigger Check Entry Points
        //  (called from BattleController at the right moments)
        // ──────────────────────────────────────────────

        /// <summary>Check and run battle_start events.</summary>
        public IEnumerator CheckBattleStart()
        {
            yield return CheckTriggerType(BattleTriggerType.BattleStart);
        }

        /// <summary>Check and run turn_start events for the given turn number.</summary>
        public IEnumerator CheckTurnStart(int turnNumber)
        {
            yield return CheckTriggerType(BattleTriggerType.TurnStart, turnNumber: turnNumber);
        }

        /// <summary>
        /// Check proximity and area_enter triggers after a unit has moved.
        /// Works for both player and enemy units.
        /// </summary>
        public IEnumerator CheckAfterMove(BattleUnit movedUnit)
        {
            if (_script == null || _executing) yield break;

            // Check proximity triggers
            foreach (var evt in _script.Events)
            {
                if (evt.Fired || evt.Trigger == null) continue;
                if (evt.Trigger.Type != BattleTriggerType.Proximity) continue;
                if (!_ctx.AllConditionsMet(evt)) continue;

                var unitA = _ctx.FindUnit(evt.Trigger.UnitA);
                var unitB = _ctx.FindUnit(evt.Trigger.UnitB);
                if (unitA == null || unitB == null) continue;
                if (unitA.hp <= 0 || unitB.hp <= 0) continue;

                if (IsAdjacent8(unitA.cell, unitB.cell))
                {
                    evt.Fired = true;
                    yield return ExecuteEvent(evt);
                }
            }

            // Check area_enter triggers
            if (movedUnit != null && movedUnit.hp > 0)
            {
                foreach (var evt in _script.Events)
                {
                    if (evt.Fired || evt.Trigger == null) continue;
                    if (evt.Trigger.Type != BattleTriggerType.AreaEnter) continue;
                    if (!_ctx.AllConditionsMet(evt)) continue;

                    // Team filter
                    if (!TeamMatches(movedUnit.team, evt.Trigger.TeamFilter)) continue;

                    // Area check
                    if (InRect(movedUnit.cell, evt.Trigger.AreaMin, evt.Trigger.AreaMax))
                    {
                        evt.Fired = true;
                        yield return ExecuteEvent(evt);
                    }
                }
            }
        }

        /// <summary>
        /// Check triggers after combat: unit_died, hp_below, proximity.
        /// </summary>
        public IEnumerator CheckAfterCombat(BattleUnit attacker, BattleUnit target)
        {
            if (_script == null || _executing) yield break;

            // Check unit_died
            if (target != null && target.hp <= 0)
                yield return CheckUnitDied(target.displayName);

            // Check hp_below for both
            if (attacker != null && attacker.hp > 0)
                yield return CheckHpBelow(attacker);
            if (target != null && target.hp > 0)
                yield return CheckHpBelow(target);

            // Proximity may have changed
            if (attacker != null && attacker.hp > 0)
                yield return CheckAfterMove(attacker);
        }

        // ──────────────────────────────────────────────
        //  Internal
        // ──────────────────────────────────────────────

        IEnumerator CheckTriggerType(BattleTriggerType type, int turnNumber = 0)
        {
            if (_script == null || _executing) yield break;

            foreach (var evt in _script.Events)
            {
                if (evt.Fired || evt.Trigger == null) continue;
                if (evt.Trigger.Type != type) continue;

                // Type-specific matching
                if (type == BattleTriggerType.TurnStart && evt.Trigger.TurnNumber != turnNumber)
                    continue;

                if (!_ctx.AllConditionsMet(evt)) continue;

                evt.Fired = true;
                yield return ExecuteEvent(evt);
            }
        }

        IEnumerator CheckUnitDied(string unitName)
        {
            foreach (var evt in _script.Events)
            {
                if (evt.Fired || evt.Trigger == null) continue;
                if (evt.Trigger.Type != BattleTriggerType.UnitDied) continue;
                if (evt.Trigger.UnitName != unitName) continue;
                if (!_ctx.AllConditionsMet(evt)) continue;

                evt.Fired = true;
                yield return ExecuteEvent(evt);
            }
        }

        IEnumerator CheckHpBelow(BattleUnit unit)
        {
            if (unit == null || unit.hp <= 0) yield break;

            float hpPercent = (float)unit.hp / unit.maxHp * 100f;

            foreach (var evt in _script.Events)
            {
                if (evt.Fired || evt.Trigger == null) continue;
                if (evt.Trigger.Type != BattleTriggerType.HpBelow) continue;
                if (evt.Trigger.UnitName != unit.displayName) continue;
                if (hpPercent >= evt.Trigger.HpPercent) continue;
                if (!_ctx.AllConditionsMet(evt)) continue;

                evt.Fired = true;
                yield return ExecuteEvent(evt);
            }
        }

        /// <summary>Execute a single event: pause battle, run actions, resume.</summary>
        IEnumerator ExecuteEvent(BattleEventDef evt)
        {
            _executing = true;
            Debug.Log($"[BattleEventManager] Executing event: {evt.Id}");

            // Enter AnimatingState to block player input
            _ctrl.SetState(new AnimatingState());

            // Run all actions
            yield return _executor.RunActions(evt.Actions);

            _executing = false;
            Debug.Log($"[BattleEventManager] Event '{evt.Id}' completed");

            // Note: the caller (BattleController) is responsible for restoring
            // the correct state after the event check coroutine returns.
        }

        // ── Geometry helpers ──

        static bool IsAdjacent8(Vector2Int a, Vector2Int b)
        {
            if (a == b) return false;
            return Mathf.Abs(a.x - b.x) <= 1 && Mathf.Abs(a.y - b.y) <= 1;
        }

        static bool InRect(Vector2Int cell, Vector2Int min, Vector2Int max)
        {
            return cell.x >= min.x && cell.x <= max.x
                && cell.y >= min.y && cell.y <= max.y;
        }

        static bool TeamMatches(UnitTeam team, string filter)
        {
            if (string.IsNullOrEmpty(filter) || filter == "any") return true;
            if (filter == "player" && team == UnitTeam.Player) return true;
            if (filter == "enemy" && team == UnitTeam.Enemy) return true;
            return false;
        }
    }
}
