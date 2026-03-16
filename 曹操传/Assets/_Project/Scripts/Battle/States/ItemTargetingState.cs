using UnityEngine;

namespace CaoCao.Battle.States
{
    /// <summary>
    /// State for selecting a target to use an item on.
    /// Shows 3x3 (九宫格) range around the unit; can target self or adjacent allies.
    /// </summary>
    public class ItemTargetingState : IBattleState
    {
        BattleController _ctrl;

        public void Enter(BattleController controller) { _ctrl = controller; }
        public void Exit() { }

        public void HandleClick(Vector2Int cell)
        {
            if (_ctrl.SelectedUnit == null || _ctrl.PendingItem == null)
            {
                HandleCancel();
                return;
            }

            // Check if clicked cell is within 3x3 range
            var unitCell = _ctrl.SelectedUnit.cell;
            int dx = Mathf.Abs(cell.x - unitCell.x);
            int dy = Mathf.Abs(cell.y - unitCell.y);

            if (dx <= 1 && dy <= 1)
            {
                // Valid range — try to use item on target
                var target = _ctrl.GetUnitAt(cell);
                if (target != null && target.team == _ctrl.SelectedUnit.team)
                {
                    _ctrl.ExecuteItemOn(cell);
                    return;
                }
            }

            // Clicked outside range or no valid target — cancel
            HandleCancel();
        }

        public void HandleCancel()
        {
            _ctrl.Highlighter.ClearAll();
            _ctrl.ShowActionMenu(_ctrl.SelectedUnit);
        }

        public void HandleHover(Vector2Int cell) { _ctrl.UpdateHover(cell); }
    }
}
