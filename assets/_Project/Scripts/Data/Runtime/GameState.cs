using System;
using System.Collections.Generic;

namespace CaoCao.Data
{
    /// <summary>
    /// Master save data structure. Serialized to JSON for save/load.
    /// Contains all mutable game state: heroes, inventory, progress, etc.
    /// </summary>
    [Serializable]
    public class GameState
    {
        [UnityEngine.Header("Progress")]
        public int currentChapter = 0;
        public string currentBattleId = "";
        public int morality = 50;
        public int gold = 0;

        [UnityEngine.Header("Heroes")]
        public List<HeroRuntimeData> heroes = new();

        [UnityEngine.Header("Inventory")]
        public List<ItemStack> inventory = new();
        public int inventoryCapacity = 200;

        [UnityEngine.Header("Story")]
        public List<string> storyFlags = new();

        [UnityEngine.Header("Deployment")]
        public List<string> deployedHeroIds = new();

        [UnityEngine.Header("Meta")]
        public string timestamp = "";
        public string currentScene = "StoryScene";
    }
}
