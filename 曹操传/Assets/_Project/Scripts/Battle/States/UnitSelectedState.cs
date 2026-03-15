using UnityEngine;

namespace CaoCao.Battle.States
{
    public class UnitSelectedState : IBattleState
    {
        BattleController _ctrl;

        public void Enter(BattleController controller) { _ctrl = controller; }
        public void Exit() { }

        public void HandleClick(Vector2Int cell)
        {
            _ctrl.HideTerrainInfo();

            if (!_ctrl.GridMap.IsInBounds(cell))
            {
                _ctrl.ClearSelection();
                return;
            }

            var unit = _ctrl.GetUnitAt(cell);

            // Click on selected unit -> show action menu without moving
            if (unit != null && unit == _ctrl.SelectedUnit)
            {
                _ctrl.Highlighter.ClearAll();
                _ctrl.ShowActionMenu(_ctrl.SelectedUnit);
                return;
            }

            // Click on reachable cell -> move
            if (_ctrl.Reachable.ContainsKey(cell))
            {
                _ctrl.MoveSelectedTo(cell);
                return;
            }

            // Click on another player unit -> re-select
            if (unit != null && unit.team == UnitTeam.Player && !unit.acted)
            {
                _ctrl.SelectUnit(unit);
                return;
            }

            _ctrl.ClearSelection();
        }

        public void HandleCancel()
        {
            _ctrl.ClearSelection();
        }

        public void HandleHover(Vector2Int cell) { _ctrl.UpdateHover(cell); }
    }
}
