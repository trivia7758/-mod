using System.Collections.Generic;
using UnityEngine;

namespace CaoCao.Story
{
    [CreateAssetMenu(fileName = "StoryMapRegistry", menuName = "CaoCao/StoryMapRegistry")]
    public class StoryMapDataRegistry : ScriptableObject
    {
        [SerializeField] List<StoryMapData> maps = new();

        public StoryMapData GetMap(string mapKey)
        {
            foreach (var m in maps)
                if (m != null && m.mapKey == mapKey)
                    return m;
            return null;
        }

        public List<StoryMapData> AllMaps => maps;

        public void AddMap(StoryMapData map)
        {
            if (!maps.Contains(map))
                maps.Add(map);
        }
    }
}
