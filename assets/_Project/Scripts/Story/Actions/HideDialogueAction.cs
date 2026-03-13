using System;
using System.Collections;

namespace CaoCao.Story
{
    [Serializable]
    public class HideDialogueAction : StoryActionBase
    {
        public override string DisplayName => "Hide Dialogue";

        public override IEnumerator Execute(StoryDirector director)
        {
            director.HideDialogue();
            yield break;
        }
    }
}
