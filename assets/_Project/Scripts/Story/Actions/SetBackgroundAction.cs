using System;
using System.Collections;
using UnityEngine;

namespace CaoCao.Story
{
    [Serializable]
    public class SetBackgroundAction : StoryActionBase
    {
        public Sprite backgroundSprite;

        public override string DisplayName =>
            backgroundSprite != null ? $"BG: {backgroundSprite.name}" : "BG: (none)";

        public override IEnumerator Execute(StoryDirector director)
        {
            director.SetBackground(backgroundSprite);
            yield break;
        }
    }
}
