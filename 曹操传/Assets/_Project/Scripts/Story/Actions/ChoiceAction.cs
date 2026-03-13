using System;
using System.Collections;
using System.Collections.Generic;

namespace CaoCao.Story
{
    [Serializable]
    public class ChoiceAction : StoryActionBase
    {
        public List<StoryChoiceOption> options = new();

        public override string DisplayName => $"Choice ({options.Count} opts)";

        public override IEnumerator Execute(StoryDirector director)
        {
            if (director.ChoicePanel == null || options.Count == 0) yield break;

            StoryChoiceOption chosen = null;
            yield return director.ChoicePanel.ShowChoices(options, opt => chosen = opt);

            if (chosen != null)
            {
                director.AddMorality(chosen.moralityDelta);
                if (!string.IsNullOrEmpty(chosen.gotoLabel))
                    director.GotoLabel(chosen.gotoLabel);
            }
        }
    }
}
