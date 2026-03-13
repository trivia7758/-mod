using System;
using System.Collections;

namespace CaoCao.Story
{
    [Serializable]
    public class LabelAction : StoryActionBase
    {
        public string labelName;

        public override string DisplayName => $"Label: {labelName}";

        public override IEnumerator Execute(StoryDirector director)
        {
            // Labels are no-ops at runtime; they're only used as jump targets.
            yield break;
        }
    }
}
