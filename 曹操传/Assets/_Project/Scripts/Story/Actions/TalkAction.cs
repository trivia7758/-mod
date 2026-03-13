using System;
using System.Collections;

namespace CaoCao.Story
{
    [Serializable]
    public class TalkAction : StoryActionBase
    {
        public string actorId;
        [UnityEngine.TextArea(2, 6)]
        public string text;

        public override string DisplayName
        {
            get
            {
                string preview = string.IsNullOrEmpty(text) ? "" : text;
                if (preview.Length > 20) preview = preview.Substring(0, 20) + "...";
                return $"{actorId}: {preview}";
            }
        }

        public override IEnumerator Execute(StoryDirector director)
        {
            if (director.DialogueBox == null) yield break;

            var def = director.Context.GetActorDef(actorId);
            var unit = director.Context.GetActor(actorId);

            string displayName = def != null ? def.displayName : "";
            UnityEngine.Sprite portrait = def?.portrait;
            DialogueSide side = def?.defaultSide ?? DialogueSide.Left;

            if (unit != null) unit.PlayTalk();
            yield return director.DialogueBox.ShowLine(text, displayName, portrait, side);
            if (unit != null) unit.StopTalk();
        }
    }
}
