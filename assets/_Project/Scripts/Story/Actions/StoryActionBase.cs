using System;
using System.Collections;

namespace CaoCao.Story
{
    /// <summary>
    /// Abstract base class for all story actions.
    /// Stored via [SerializeReference] in StorySceneData.actions.
    /// Each subclass defines its own serialized fields and Execute() logic.
    /// </summary>
    [Serializable]
    public abstract class StoryActionBase
    {
        /// <summary>
        /// Human-readable label shown in the editor action list.
        /// </summary>
        public abstract string DisplayName { get; }

        /// <summary>
        /// Execute this action. Yield for async operations (dialogue, movement, wait).
        /// </summary>
        public abstract IEnumerator Execute(StoryDirector director);
    }
}
