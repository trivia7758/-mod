using System.Collections.Generic;

namespace CaoCao.Story
{
    /// <summary>
    /// Runtime registry mapping actor IDs to their scene units and definitions.
    /// Replaces the hardcoded GetUnit() switch and _charMeta dictionary.
    /// </summary>
    public class StoryContext
    {
        readonly Dictionary<string, StoryUnit> _actors = new();
        readonly Dictionary<string, ActorDefinition> _actorDefs = new();

        public void RegisterActor(string id, StoryUnit unit, ActorDefinition def)
        {
            _actors[id] = unit;
            _actorDefs[id] = def;
        }

        /// <summary>
        /// Get the scene unit for an actor. Returns null for non-visual actors (narrator, system).
        /// </summary>
        public StoryUnit GetActor(string id)
        {
            return _actors.GetValueOrDefault(id);
        }

        /// <summary>
        /// Get the definition (name, portrait, side) for an actor.
        /// </summary>
        public ActorDefinition GetActorDef(string id)
        {
            return _actorDefs.GetValueOrDefault(id);
        }

        public bool HasActor(string id) => _actors.ContainsKey(id);
    }
}
