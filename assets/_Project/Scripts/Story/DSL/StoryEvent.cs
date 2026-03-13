using System.Collections.Generic;

namespace CaoCao.Story
{
    public enum StoryEventType
    {
        Label, Goto, SetPos, Move, Face, Talk, Wait,
        HideDialogue, Background, Location, Morality, Choice
    }

    // DialogueSide enum has been moved to StoryTypes.cs

    public class StoryEvent
    {
        public StoryEventType Type;
    }

    public class LabelEvent : StoryEvent
    {
        public string Name;
        public LabelEvent(string name) { Type = StoryEventType.Label; Name = name; }
    }

    public class GotoEvent : StoryEvent
    {
        public string Label;
        public GotoEvent(string label) { Type = StoryEventType.Goto; Label = label; }
    }

    public class SetPosEvent : StoryEvent
    {
        public string UnitId;
        public int X, Y;
        public SetPosEvent(string unit, int x, int y)
        { Type = StoryEventType.SetPos; UnitId = unit; X = x; Y = y; }
    }

    public class MoveEvent : StoryEvent
    {
        public string UnitId;
        public int X, Y;
        public MoveEvent(string unit, int x, int y)
        { Type = StoryEventType.Move; UnitId = unit; X = x; Y = y; }
    }

    public class FaceEvent : StoryEvent
    {
        public string UnitId;
        public string Direction;
        public FaceEvent(string unit, string dir)
        { Type = StoryEventType.Face; UnitId = unit; Direction = dir; }
    }

    public class TalkEvent : StoryEvent
    {
        public string UnitId;
        public string Text;
        public TalkEvent(string unit, string text)
        { Type = StoryEventType.Talk; UnitId = unit; Text = text; }
    }

    public class WaitEvent : StoryEvent
    {
        public float Seconds;
        public WaitEvent(float sec) { Type = StoryEventType.Wait; Seconds = sec; }
    }

    public class HideDialogueEvent : StoryEvent
    {
        public HideDialogueEvent() { Type = StoryEventType.HideDialogue; }
    }

    public class BackgroundEvent : StoryEvent
    {
        public string Path;
        public BackgroundEvent(string path) { Type = StoryEventType.Background; Path = path; }
    }

    public class LocationEvent : StoryEvent
    {
        public string Text;
        public LocationEvent(string text) { Type = StoryEventType.Location; Text = text; }
    }

    public class MoralityEvent : StoryEvent
    {
        public int Value;
        public MoralityEvent(int val) { Type = StoryEventType.Morality; Value = val; }
    }

    public class ChoiceOption
    {
        public string Text;
        public string GotoLabel;
        public int MoralityDelta;
    }

    public class ChoiceEvent : StoryEvent
    {
        public List<ChoiceOption> Options = new();
        public ChoiceEvent() { Type = StoryEventType.Choice; }
    }
}
