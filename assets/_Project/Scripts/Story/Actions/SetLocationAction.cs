using System;
using System.Collections;

namespace CaoCao.Story
{
    [Serializable]
    public class SetLocationAction : StoryActionBase
    {
        public string locationText;

        public override string DisplayName => $"Location: {locationText}";

        public override IEnumerator Execute(StoryDirector director)
        {
            director.SetLocation(locationText);
            yield break;
        }
    }
}
