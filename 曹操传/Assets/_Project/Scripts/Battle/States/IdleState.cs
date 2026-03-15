using UnityEngine;

namespace CaoCao.Battle.States
{
    public class IdleState : IBattleState
    {
        BattleController _ctrl;

        public void Enter(BattleController controller) { _ctrl = controller; }
        public void Exit() { }

        public void HandleClick(Vector2Int cell)
        {
            var unit = _ctrl.GetUnitAt(cell);
            if (unit == null)
            {
                _ctrl.HideUnitInfo();
                // Always show terrain info (even out of bounds — shows coordinates)
                _ctrl.ShowTerrainInfo(cell);
                return;
            }

            // Always show info for any unit clicked
            _ctrl.ShowUnitInfo(unit);
            // Also show terrain info for the cell the unit is on
            _ctrl.ShowTerrainInfo(cell);

            // Only select player units for movement/action
            if (unit.team == UnitTeam.Player && !unit.acted)
                _ctrl.SelectUnit(unit);
        }

        public void HandleCancel()
        {
            _ctrl.HideTerrainInfo();
        }

        public void HandleHover(Vector2Int cell) { _ctrl.UpdateHover(cell); }
    }
}
