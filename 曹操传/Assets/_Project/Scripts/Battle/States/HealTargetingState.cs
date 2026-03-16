using UnityEngine;

namespace CaoCao.Battle.States
{
    /// <summary>
    /// State for selecting a friendly target for a heal skill.
    /// Uses diamond range (Manhattan distance <= 4).
    /// </summary>
    public class HealTargetingState : IBattleState
    {
        BattleController _ctrl;

        public void Enter(BattleController controller) { _ctrl = controller; }
        public void Exit() { }

        public void HandleClick(Vector2Int cell)
        {
            var unit = _ctrl.GetUnitAt(cell);

            if (unit != null && unit.team == UnitTeam.Player && _ctrl.PendingSkill != null)
            {
                // Check within heal range (Manhattan distance <= 4)
                int dist = Mathf.Abs(_ctrl.SelectedUnit.cell.x - unit.cell.x)
                         + Mathf.Abs(_ctrl.SelectedUnit.cell.y - unit.cell.y);
                if (dist <= 4)
                {
                    // Don't heal full HP targets
                    if (unit.hp >= unit.maxHp)
                    {
                        Debug.Log($"[Battle] {unit.displayName} HP already full");
                        return; // stay in targeting state
                    }
                    _ctrl.ExecuteHealSkillOn(unit);
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
