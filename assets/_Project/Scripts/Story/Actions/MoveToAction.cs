using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CaoCao.Story
{
    [Serializable]
    public class MoveToAction : StoryActionBase
    {
        public string actorId;
        public List<Waypoint> waypoints = new();
        public float speed = 2f;

        public override string DisplayName =>
            $"Move {actorId} ({waypoints.Count} pts)";

        public override IEnumerator Execute(StoryDirector director)
        {
            var unit = director.Context.GetActor(actorId);
            if (unit == null || waypoints.Count == 0) yield break;
            unit.gameObject.SetActive(true);
            yield return unit.MoveAlongWorld(waypoints, speed);
        }
    }
}
