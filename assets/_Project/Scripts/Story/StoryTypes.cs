using System;
using UnityEngine;

namespace CaoCao.Story
{
    /// <summary>
    /// Which side of the screen the dialogue appears on.
    /// Shared by DialogueBox, TalkAction, and ActorDefinition.
    /// </summary>
    public enum DialogueSide { Left, Right, Center }

    /// <summary>
    /// Cardinal facing directions for story characters.
    /// Auto = determine from movement delta.
    /// </summary>
    public enum FaceDirection { Auto, Up, Down, Left, Right }

    /// <summary>
    /// A single waypoint in a MoveToAction path.
    /// Each point has a world-space position and an optional facing override.
    /// </summary>
    [Serializable]
    public class Waypoint
    {
        public Vector2 position;
        public FaceDirection face = FaceDirection.Auto;

        public Waypoint() { }
        public Waypoint(Vector2 pos) { position = pos; face = FaceDirection.Auto; }
        public Waypoint(Vector2 pos, FaceDirection f) { position = pos; face = f; }
    }

    /// <summary>
    /// A single choice option presented to the player during story scenes.
    /// Replaces the old ChoiceOption class from StoryEvent.cs.
    /// </summary>
    [Serializable]
    public class StoryChoiceOption
    {
        public string text;
        public string gotoLabel;
        public int moralityDelta;
    }

    /// <summary>
    /// Defines an actor (character) that can appear in a story scene.
    /// Stored in StorySceneData.actors list.
    /// </summary>
    [Serializable]
    public class ActorDefinition
    {
        public string id;               // "hero", "oldman", "narrator"
        public string displayName;      // "张强", "神秘老人"
        public Sprite portrait;         // direct sprite reference
        public DialogueSide defaultSide = DialogueSide.Left;
    }
}
