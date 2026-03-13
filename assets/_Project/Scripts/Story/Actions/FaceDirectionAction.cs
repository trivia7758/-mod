using System;
using System.Collections;
using UnityEngine;

namespace CaoCao.Story
{
    [Serializable]
    public class FaceDirectionAction : StoryActionBase
    {
        public string actorId;
        public FaceDirection direction = FaceDirection.Down;

        public override string DisplayName => $"Face {actorId} {direction}";

        public override IEnumerator Execute(StoryDirector director)
        {
            var unit = director.Context.GetActor(actorId);
            if (unit != null)
                unit.FaceDir(DirectionToVector(direction));
            yield break;
        }

        static Vector2Int DirectionToVector(FaceDirection dir)
        {
            return dir switch
            {
                FaceDirection.Up => Vector2Int.up,
                FaceDirection.Down => Vector2Int.down,
                FaceDirection.Left => Vector2Int.left,
                FaceDirection.Right => Vector2Int.right,
                _ => Vector2Int.down
            };
        }
    }
}
