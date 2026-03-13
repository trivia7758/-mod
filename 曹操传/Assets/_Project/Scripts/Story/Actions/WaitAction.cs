using System;
using System.Collections;
using UnityEngine;

namespace CaoCao.Story
{
    [Serializable]
    public class WaitAction : StoryActionBase
    {
        public float seconds = 0.5f;

        public override string DisplayName => $"Wait {seconds}s";

        public override IEnumerator Execute(StoryDirector director)
        {
            yield return new WaitForSeconds(seconds);
        }
    }
}
