using UnityEngine;

namespace CaoCao.Battle.States
{
    /// <summary>
    /// State for selecting a target for a damage/debuff skill.
    /// Similar to AttackingState but uses skill range.
    /// </summary>
    public class SkillTargetingState : IBattleState
    {
        BattleController _ctrl;

        public void Enter(BattleController controller) { _ctrl = controller; }
        public void Exit() { }

        public void HandleClick(Vector2Int cell)
        {
            var unit = _ctrl.GetUnitAt(cell);

            if (unit != null && unit.team == UnitTeam.Enemy && _ctrl.PendingSkill != null)
            {
                int dist = Mathf.Abs(_ctrl.SelectedUnit.cell.x - unit.cell.x)
                         + Mathf.Abs(_ctrl.SelectedUnit.cell.y - unit.cell.y);
                if (dist >= 1 && dist <= _ctrl.PendingSkill.range)
                {
                    _ctrl.ExecuteSkillOn(unit);
                    return;
                }
            }

            // Click elsewhere -> cancel skill, undo move, return to idle
            _ctrl.UndoMoveIfNeeded();
            _ctrl.Highlighter.ClearAll();
            _ctrl.ClearSelection();
        }

        public void HandleCancel()
        {
            _ctrl.Highlighter.ClearAll();
            _ctrl.ShowActionMenu(_ctrl.SelectedUnit);
        }

        public void HandleHover(Vector2Int cell) { _ctrl.UpdateHover(cell); }
    }
}
