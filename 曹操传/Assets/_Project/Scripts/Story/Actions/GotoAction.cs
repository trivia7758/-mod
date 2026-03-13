using System;
using System.Collections;

namespace CaoCao.Story
{
    [Serializable]
    public class GotoAction : StoryActionBase
    {
        public string targetLabel;

        public override string DisplayName => $"Goto: {targetLabel}";

        public override IEnumerator Execute(StoryDirector director)
        {
            director.GotoLabel(targetLabel);
            yield break;
        }
    }
}
