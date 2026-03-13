using UnityEngine;

namespace CaoCao.Battle.States
{
    public interface IBattleState
    {
        void Enter(BattleController controller);
        void Exit();
        void HandleClick(Vector2Int cell);
        void HandleCancel();
        void HandleHover(Vector2Int cell);
    }
}
