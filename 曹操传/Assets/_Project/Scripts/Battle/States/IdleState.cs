using UnityEngine;

namespace CaoCao.Battle.States
{
    public class IdleState : IBattleState
    {
        BattleController _ctrl;
        bool _showingEnemyRange;

        public void Enter(BattleController controller)
        {
            _ctrl = controller;
            _showingEnemyRange = false;
        }

        public void Exit()
        {
            if (_showingEnemyRange)
            {
                _ctrl.ClearEnemyPreview();
                _showingEnemyRange = false;
            }
        }

        public void HandleClick(Vector2Int cell)
        {
            // If showing enemy range, first click elsewhere clears it
            if (_showingEnemyRange)
            {
                _ctrl.ClearEnemyPreview();
                _showingEnemyRange = false;
            }

            var unit = _ctrl.GetUnitAt(cell);
            if (unit == null)
            {
                _ctrl.HideUnitInfo();
                _ctrl.ShowTerrainInfo(cell);
                return;
            }

            _ctrl.ShowUnitInfo(unit);
            _ctrl.ShowTerrainInfo(cell);

            if (unit.team == UnitTeam.Player && !unit.acted)
            {
                // Select player unit → enters UnitSelectedState
                _ctrl.SelectUnit(unit);
            }
            else
            {
                // Click enemy or acted ally → preview their move + attack range + terrain effects
                _ctrl.PreviewEnemyRange(unit);
                _showingEnemyRange = true;
            }
        }

        public void HandleCancel()
        {
            if (_showingEnemyRange)
            {
                _ctrl.ClearEnemyPreview();
                _showingEnemyRange = false;
            }
            _ctrl.HideTerrainInfo();
        }

        public void HandleHover(Vector2Int cell) { _ctrl.UpdateHover(cell); }
    }
}
