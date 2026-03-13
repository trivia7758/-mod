using System.Collections.Generic;
using UnityEngine;

namespace CaoCao.Data
{
    /// <summary>
    /// Central registry for all game data ScriptableObjects.
    /// Loaded from Resources and registered in ServiceLocator by GameManager.
    /// Provides O(1) lookup by ID after Initialize().
    /// </summary>
    [CreateAssetMenu(fileName = "GameDataRegistry", menuName = "CaoCao/Data/Game Data Registry")]
    public class GameDataRegistry : ScriptableObject
    {
        [Header("Heroes")]
        public HeroDefinition[] allHeroes;

        [Header("Items")]
        public ItemDefinition[] allItems;

        [Header("Skills")]
        public SkillDefinition[] allSkills;

        [Header("Unit Types")]
        public UnitTypeDefinition[] allUnitTypes;

        [Header("Battles")]
        public BattleDefinition[] allBattles;

        // Runtime lookup dictionaries (built on Initialize)
        Dictionary<string, HeroDefinition> _heroMap;
        Dictionary<string, ItemDefinition> _itemMap;
        Dictionary<string, SkillDefinition> _skillMap;
        Dictionary<string, UnitTypeDefinition> _unitTypeMap;
        Dictionary<string, BattleDefinition> _battleMap;

        /// <summary>
        /// Build lookup dictionaries from arrays. Must be called once at game start.
        /// </summary>
        public void Initialize()
        {
            _heroMap = new Dictionary<string, HeroDefinition>();
            if (allHeroes != null)
                foreach (var h in allHeroes)
                    if (h != null && !string.IsNullOrEmpty(h.id))
                        _heroMap[h.id] = h;

            _itemMap = new Dictionary<string, ItemDefinition>();
            if (allItems != null)
                foreach (var it in allItems)
                    if (it != null && !string.IsNullOrEmpty(it.id))
                        _itemMap[it.id] = it;

            _skillMap = new Dictionary<string, SkillDefinition>();
            if (allSkills != null)
                foreach (var s in allSkills)
                    if (s != null && !string.IsNullOrEmpty(s.id))
                        _skillMap[s.id] = s;

            _unitTypeMap = new Dictionary<string, UnitTypeDefinition>();
            if (allUnitTypes != null)
                foreach (var ut in allUnitTypes)
                    if (ut != null && !string.IsNullOrEmpty(ut.id))
                        _unitTypeMap[ut.id] = ut;

            _battleMap = new Dictionary<string, BattleDefinition>();
            if (allBattles != null)
                foreach (var b in allBattles)
                    if (b != null)
                        _battleMap[b.name] = b;
        }

        // --- Lookup Methods ---

        public HeroDefinition GetHero(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            return _heroMap != null && _heroMap.TryGetValue(id, out var h) ? h : null;
        }

        public ItemDefinition[] GetAllItems() => allItems;

        public ItemDefinition GetItem(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            return _itemMap != null && _itemMap.TryGetValue(id, out var it) ? it : null;
        }

        public SkillDefinition GetSkill(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            return _skillMap != null && _skillMap.TryGetValue(id, out var s) ? s : null;
        }

        public UnitTypeDefinition GetUnitType(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            return _unitTypeMap != null && _unitTypeMap.TryGetValue(id, out var ut) ? ut : null;
        }

        public BattleDefinition GetBattle(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            return _battleMap != null && _battleMap.TryGetValue(name, out var b) ? b : null;
        }
    }
}
