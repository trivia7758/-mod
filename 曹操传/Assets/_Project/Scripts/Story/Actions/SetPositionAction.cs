using System;
using System.Collections;
using UnityEngine;

namespace CaoCao.Story
{
    [Serializable]
    public class SetPositionAction : StoryActionBase
    {
        public string actorId;
        public Vector2 worldPosition;

        public override string DisplayName => $"SetPos {actorId}";

        public override IEnumerator Execute(StoryDirector director)
        {
            var unit = director.Context.GetActor(actorId);
            if (unit != null)
            {
                unit.gameObject.SetActive(true);
                unit.transform.position = new Vector3(worldPosition.x, worldPosition.y, unit.transform.position.z);
            }
            yield break;
        }
    }
}
