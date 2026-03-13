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
            if (unit != null && unit.team == UnitTeam.Player && !unit.acted)
                _ctrl.SelectUnit(unit);
        }

        public void HandleCancel() { }
        public void HandleHover(Vector2Int cell) { _ctrl.UpdateHover(cell); }
    }
}
