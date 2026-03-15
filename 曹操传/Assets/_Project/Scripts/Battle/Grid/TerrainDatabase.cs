using System;
using System.Collections.Generic;
using UnityEngine;
using CaoCao.Common;

namespace CaoCao.Battle
{
    /// <summary>
    /// Runtime data for a single terrain type, loaded from terrain_data.json.
    /// </summary>
    public class TerrainData
    {
        public string id;
        public string name;           // Chinese display name
        public int healPercent;        // HP% recovered per turn (floor), 0 = none

        // Indexed by (int)UnitClass
        public int[] effects;          // 发挥效果 %  (100=normal, 120=★, 110=◎, 90=△, 80=×, -1=impassable)
        public int[] moveCosts;        // 消耗移动力    (1-3 = cost, -1 = impassable)

        /// <summary>Get effect% for a unit class. Returns -1 if impassable.</summary>
        public int GetEffect(UnitClass uc)
        {
            int idx = (int)uc;
            return (effects != null && idx >= 0 && idx < effects.Length) ? effects[idx] : 100;
        }

        /// <summary>Get movement cost for a unit class. Returns -1 if impassable.</summary>
        public int GetMoveCost(UnitClass uc)
        {
            int idx = (int)uc;
            return (moveCosts != null && idx >= 0 && idx < moveCosts.Length) ? moveCosts[idx] : 1;
        }

        /// <summary>Check if a unit class can walk on this terrain.</summary>
        public bool IsPassable(UnitClass uc) => GetMoveCost(uc) > 0;

        /// <summary>Calculate actual HP healed this turn (floor, no decimals).</summary>
        public int CalcHeal(int maxHp)
        {
            if (healPercent <= 0) return 0;
            return Mathf.FloorToInt(maxHp * healPercent / 100f);
        }
    }

    /// <summary>
    /// Loads and provides access to all terrain data from terrain_data.json.
    /// Singleton — call TerrainDatabase.Instance to access.
    /// </summary>
    public class TerrainDatabase
    {
        static TerrainDatabase _instance;
        public static TerrainDatabase Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new TerrainDatabase();
                    _instance.Load();
                }
                return _instance;
            }
        }

        readonly Dictionary<string, TerrainData> _byId = new(StringComparer.OrdinalIgnoreCase);
        readonly Dictionary<string, TerrainData> _byName = new();
        readonly List<TerrainData> _all = new();

        // Map UnitClass enum names to their index in the JSON arrays
        Dictionary<string, int> _classNameToJsonIndex;

        public IReadOnlyList<TerrainData> All => _all;

        /// <summary>Look up terrain by English id (e.g. "plain", "forest").</summary>
        public TerrainData GetById(string id)
        {
            if (id != null && _byId.TryGetValue(id, out var td)) return td;
            return null;
        }

        /// <summary>Look up terrain by Chinese name (e.g. "平原", "树林").</summary>
        public TerrainData GetByName(string name)
        {
            if (name != null && _byName.TryGetValue(name, out var td)) return td;
            return null;
        }

        /// <summary>Look up by either id or name.</summary>
        public TerrainData Get(string key)
        {
            return GetById(key) ?? GetByName(key);
        }

        void Load()
        {
            var asset = Resources.Load<TextAsset>("Data/terrain_data");
            if (asset == null)
            {
                Debug.LogError("[TerrainDatabase] terrain_data.json not found in Resources/Data/");
                return;
            }

            var root = JsonUtility.FromJson<JsonRoot>(asset.text);
            if (root == null || root.terrains == null)
            {
                Debug.LogError("[TerrainDatabase] Failed to parse terrain_data.json");
                return;
            }

            // Build class-name → JSON-index map
            _classNameToJsonIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            if (root.unitClassOrder != null)
            {
                for (int i = 0; i < root.unitClassOrder.Length; i++)
                    _classNameToJsonIndex[root.unitClassOrder[i]] = i;
            }

            // Parse each terrain entry
            foreach (var entry in root.terrains)
            {
                var td = new TerrainData
                {
                    id = entry.id,
                    name = entry.name,
                    healPercent = entry.healPercent
                };

                // Map JSON array (ordered by unitClassOrder) to UnitClass enum indices
                int enumCount = Enum.GetValues(typeof(UnitClass)).Length;
                td.effects = new int[enumCount];
                td.moveCosts = new int[enumCount];

                // Default: effect=100, cost=1
                for (int i = 0; i < enumCount; i++)
                {
                    td.effects[i] = 100;
                    td.moveCosts[i] = 1;
                }

                // Fill from JSON
                foreach (UnitClass uc in Enum.GetValues(typeof(UnitClass)))
                {
                    string ucName = uc.ToString();
                    if (_classNameToJsonIndex.TryGetValue(ucName, out int jsonIdx))
                    {
                        if (entry.effects != null && jsonIdx < entry.effects.Length)
                            td.effects[(int)uc] = entry.effects[jsonIdx];
                        if (entry.moveCosts != null && jsonIdx < entry.moveCosts.Length)
                            td.moveCosts[(int)uc] = entry.moveCosts[jsonIdx];
                    }
                }

                _byId[td.id] = td;
                _byName[td.name] = td;
                _all.Add(td);
            }

            Debug.Log($"[TerrainDatabase] Loaded {_all.Count} terrain types, {_classNameToJsonIndex.Count} unit classes");
        }

        // ── JSON deserialization classes ──
        [Serializable]
        class JsonRoot
        {
            public string[] unitClassOrder;
            public JsonTerrain[] terrains;
        }

        [Serializable]
        class JsonTerrain
        {
            public string id;
            public string name;
            public int healPercent;
            public int[] effects;
            public int[] moveCosts;
        }
    }
}
