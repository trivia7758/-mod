using System.Collections.Generic;
using UnityEngine;

namespace CaoCao.Story
{
    /// <summary>
    /// Top-level ScriptableObject that defines a complete story scene.
    /// Replaces the old DSL text file (story.dsl.txt).
    /// One asset per story scene (e.g., Prologue, Chapter1, etc.)
    /// </summary>
    [CreateAssetMenu(fileName = "NewStoryScene", menuName = "CaoCao/Story Scene Data")]
    public class StorySceneData : ScriptableObject
    {
        [Header("Actors")]
        [Tooltip("Characters that can appear in this scene")]
        public List<ActorDefinition> actors = new();

        [Header("Actions")]
        [Tooltip("Sequential list of actions to execute")]
        [SerializeReference]
        public List<StoryActionBase> actions = new();
    }
}
