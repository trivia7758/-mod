using System;
using System.Collections;

namespace CaoCao.Story
{
    [Serializable]
    public class SetMoralityAction : StoryActionBase
    {
        public int value = 50;

        public override string DisplayName => $"Morality = {value}";

        public override IEnumerator Execute(StoryDirector director)
        {
            director.SetMorality(value);
            yield break;
        }
    }
}
