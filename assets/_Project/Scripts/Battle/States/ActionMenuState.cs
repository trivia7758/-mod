using UnityEngine;

namespace CaoCao.Battle.States
{
    public class ActionMenuState : IBattleState
    {
        BattleController _ctrl;

        public void Enter(BattleController controller) { _ctrl = controller; }
        public void Exit() { }

        public void HandleClick(Vector2Int cell)
        {
            // Click outside action menu -> cancel and undo
            _ctrl.UndoMoveIfNeeded();
            _ctrl.HideActionMenu();
            _ctrl.Highlighter.ClearAll();
            _ctrl.ClearSelection();
        }

        public void HandleCancel()
        {
            _ctrl.UndoMoveIfNeeded();
            _ctrl.HideActionMenu();
            // Go back to unit selected with move range shown
            _ctrl.Highlighter.SetMoveCells(new System.Collections.Generic.List<Vector2Int>(_ctrl.Reachable.Keys));
            _ctrl.SetState(new UnitSelectedState());
        }

        public void HandleHover(Vector2Int cell) { }
    }
}
