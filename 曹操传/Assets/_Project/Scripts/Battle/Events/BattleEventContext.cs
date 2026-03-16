using System.Collections.Generic;
using UnityEngine;

namespace CaoCao.Battle.Events
{
    /// <summary>
    /// Runtime context for battle events: flags, unit lookup, turn info.
    /// </summary>
    public class BattleEventContext
    {
        readonly HashSet<string> _flags = new();
        readonly BattleController _controller;

        public BattleEventContext(BattleController controller)
        {
            _controller = controller;
        }

        // ── Flags ──

        public void SetFlag(string name) => _flags.Add(name);
        public void ClearFlag(string name) => _flags.Remove(name);
        public bool HasFlag(string name) => _flags.Contains(name);

        // ── Unit Lookup ──

        /// <summary>
        /// Find a BattleUnit by displayName. Returns null if not found or dead.
        /// Also checks unitTypeName for partial matches.
        /// </summary>
        public BattleUnit FindUnit(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            return _controller.FindUnitByName(name);
        }

        /// <summary>
        /// Find a unit, including dead ones (for checking death triggers).
        /// </summary>
        public BattleUnit FindUnitIncludingDead(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            return _controller.FindUnitByName(name, includeDead: true);
        }

        // ── Turn Info ──

        public int TurnCount => _controller.TurnCount;

        // ── Condition Evaluation ──

        public bool EvaluateCondition(BattleEventCondition cond)
        {
            switch (cond.Type)
            {
                case BattleConditionType.Flag:
                    return HasFlag(cond.Param);
                case BattleConditionType.NotFlag:
                    return !HasFlag(cond.Param);
                case BattleConditionType.UnitAlive:
                    var alive = FindUnit(cond.Param);
                    return alive != null && alive.hp > 0;
                case BattleConditionType.UnitDead:
                    var dead = FindUnitIncludingDead(cond.Param);
                    return dead == null || dead.hp <= 0;
                case BattleConditionType.TurnGe:
                    return TurnCount >= cond.IntParam;
                default:
                    return true;
            }
        }

        public bool AllConditionsMet(BattleEventDef evt)
        {
            foreach (var cond in evt.Conditions)
                if (!EvaluateCondition(cond))
                    return false;
            return true;
        }
    }
}
