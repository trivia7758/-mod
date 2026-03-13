using UnityEngine;

namespace CaoCao.Battle.States
{
    public class AttackingState : IBattleState
    {
        BattleController _ctrl;

        public void Enter(BattleController controller) { _ctrl = controller; }
        public void Exit() { }

        public void HandleClick(Vector2Int cell)
        {
            var unit = _ctrl.GetUnitAt(cell);
            if (unit != null && unit.team == UnitTeam.Enemy && _ctrl.InAttackRange(_ctrl.SelectedUnit.cell, unit.cell))
            {
                _ctrl.ExecuteAttack(_ctrl.SelectedUnit, unit);
                return;
            }

            // Click elsewhere -> undo and cancel
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
