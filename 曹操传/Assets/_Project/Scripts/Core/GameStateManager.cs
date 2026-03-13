using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using CaoCao.Common;
using CaoCao.Data;

namespace CaoCao.Core
{
    /// <summary>
    /// Manages the live GameState. Registered in ServiceLocator.
    /// Provides methods to modify heroes, inventory, equipment, and handles save/load.
    /// All mutations go through this class to ensure validation and event firing.
    /// </summary>
    public class GameStateManager
    {
        GameState _state;
        readonly GameDataRegistry _registry;
        readonly SaveSystem _saveSystem;

        const int MaxSaveSlots = 50;

        public GameState State => _state;
        public GameDataRegistry Registry => _registry;

        /// <summary>
        /// Set to true after LoadFromSlot. StoryDirector checks this to skip story replay.
        /// </summary>
        public bool WasLoadedFromSave { get; set; }

        public GameStateManager(GameDataRegistry registry, SaveSystem saveSystem)
        {
            _registry = registry;
            _saveSystem = saveSystem;
            _state = new GameState();
        }

        // ============================================================
        // New Game
        // ============================================================

        /// <summary>
        /// Initialize a fresh game state with starting heroes and items.
        /// </summary>
        public void InitializeNewGame()
        {
            _state = new GameState();

            // Recruit all heroes available at chapter 0
            if (_registry?.allHeroes != null)
            {
                foreach (var heroDef in _registry.allHeroes)
                {
                    if (heroDef != null && heroDef.recruitChapter <= 0)
                    {
                        RecruitHero(heroDef.id);
                    }
                }
            }

            // Give protagonist starting equipment (directly equipped, not in inventory)
            var protagonist = GetHero("zhang_qiang");
            if (protagonist != null)
            {
                protagonist.equippedWeaponId = "crushed_soul_sword";
                protagonist.equippedArmorId = "cotton_clothes";
            }

            // Add some starting consumables to inventory
            AddItem("hp_potion", 3);

            // Starting gold
            _state.gold = 500;

            RecalculateAllStats();
        }

        // ============================================================
        // Hero Management
        // ============================================================

        /// <summary>
        /// Get runtime data for a specific hero.
        /// </summary>
        public HeroRuntimeData GetHero(string heroId)
        {
            return _state.heroes.Find(h => h.heroId == heroId);
        }

        /// <summary>
        /// Recruit a hero into the roster.
        /// </summary>
        public void RecruitHero(string heroId)
        {
            if (GetHero(heroId) != null) return; // already recruited

            var heroDef = _registry?.GetHero(heroId);
            if (heroDef == null)
            {
                Debug.LogWarning($"[GameStateManager] Hero definition not found: {heroId}");
                return;
            }

            var runtime = new HeroRuntimeData
            {
                heroId = heroId,
                level = 1,
                exp = 0,
                isRecruited = true,
                currentUnitTypeId = heroDef.defaultUnitType != null
                    ? heroDef.defaultUnitType.id : ""
            };

            // Set current HP/MP to max
            runtime.RecalculateStats(heroDef, _registry);
            runtime.currentHp = runtime.maxHp;
            runtime.currentMp = runtime.maxMp;

            _state.heroes.Add(runtime);
        }

        /// <summary>
        /// Get all recruited heroes.
        /// </summary>
        public List<HeroRuntimeData> GetRecruitedHeroes()
        {
            return _state.heroes.Where(h => h.isRecruited).ToList();
        }

        /// <summary>
        /// Recalculate derived stats for all heroes (after load, equip change, level up).
        /// </summary>
        public void RecalculateAllStats()
        {
            foreach (var hero in _state.heroes)
            {
                var heroDef = _registry?.GetHero(hero.heroId);
                hero.RecalculateStats(heroDef, _registry);
            }
        }

        // ============================================================
        // Gold
        // ============================================================

        public int Gold => _state.gold;

        public void AddGold(int amount)
        {
            if (amount <= 0) return;
            _state.gold += amount;
        }

        public bool SpendGold(int amount)
        {
            if (amount <= 0) return false;
            if (_state.gold < amount) return false;
            _state.gold -= amount;
            return true;
        }

        // ============================================================
        // Inventory
        // ============================================================

        /// <summary>
        /// Total number of items in inventory (sum of all stacks).
        /// </summary>
        public int TotalItemCount => _state.inventory.Sum(s => s.count);

        /// <summary>
        /// Add items to inventory. Returns false if would exceed capacity.
        /// </summary>
        public bool AddItem(string itemId, int count = 1)
        {
            if (string.IsNullOrEmpty(itemId) || count <= 0) return false;
            if (TotalItemCount + count > _state.inventoryCapacity) return false;

            var stack = _state.inventory.Find(s => s.itemId == itemId);
            if (stack != null)
            {
                stack.count += count;
            }
            else
            {
                _state.inventory.Add(new ItemStack(itemId, count));
            }

            EventBus.ItemAdded(itemId, count);
            return true;
        }

        /// <summary>
        /// Remove items from inventory. Returns false if not enough.
        /// </summary>
        public bool RemoveItem(string itemId, int count = 1)
        {
            if (string.IsNullOrEmpty(itemId) || count <= 0) return false;

            var stack = _state.inventory.Find(s => s.itemId == itemId);
            if (stack == null || stack.count < count) return false;

            stack.count -= count;
            if (stack.count <= 0)
            {
                _state.inventory.Remove(stack);
            }

            EventBus.ItemRemoved(itemId, count);
            return true;
        }

        /// <summary>
        /// Get count of a specific item in inventory.
        /// </summary>
        public int GetItemCount(string itemId)
        {
            var stack = _state.inventory.Find(s => s.itemId == itemId);
            return stack?.count ?? 0;
        }

        /// <summary>
        /// Get all items of a specific type.
        /// </summary>
        public List<ItemStack> GetItemsByType(ItemType type)
        {
            var result = new List<ItemStack>();
            foreach (var stack in _state.inventory)
            {
                var itemDef = _registry?.GetItem(stack.itemId);
                if (itemDef != null && itemDef.itemType == type && stack.count > 0)
                {
                    result.Add(stack);
                }
            }
            return result;
        }

        // ============================================================
        // Equipment
        // ============================================================

        /// <summary>
        /// Equip an item from inventory to a hero's slot.
        /// The previously equipped item (if any) is returned to inventory.
        /// </summary>
        public bool EquipItem(string heroId, string itemId, EquipSlot slot)
        {
            var hero = GetHero(heroId);
            if (hero == null) return false;

            var itemDef = _registry?.GetItem(itemId);
            if (itemDef == null || !itemDef.IsEquipment) return false;
            if (!itemDef.CanHeroEquip(heroId, hero.currentUnitTypeId)) return false;

            // Unequip current item first
            UnequipItem(heroId, slot);

            // Remove from inventory
            if (!RemoveItem(itemId, 1)) return false;

            // Equip
            hero.SetEquippedItemId(slot, itemId);

            // Recalculate stats
            var heroDef = _registry?.GetHero(heroId);
            hero.RecalculateStats(heroDef, _registry);

            EventBus.EquipmentChanged(heroId, itemId, slot);
            return true;
        }

        /// <summary>
        /// Unequip an item from a hero's slot back to inventory.
        /// Returns the unequipped item ID, or empty string if slot was empty.
        /// </summary>
        public string UnequipItem(string heroId, EquipSlot slot)
        {
            var hero = GetHero(heroId);
            if (hero == null) return "";

            string currentItemId = hero.GetEquippedItemId(slot);
            if (string.IsNullOrEmpty(currentItemId)) return "";

            // Return to inventory
            hero.SetEquippedItemId(slot, "");
            AddItem(currentItemId, 1);

            // Recalculate stats
            var heroDef = _registry?.GetHero(heroId);
            hero.RecalculateStats(heroDef, _registry);

            EventBus.EquipmentChanged(heroId, "", slot);
            return currentItemId;
        }

        // ============================================================
        // Deployment
        // ============================================================

        /// <summary>
        /// Set the list of hero IDs to deploy in the next battle.
        /// </summary>
        public void SetDeployment(List<string> heroIds)
        {
            _state.deployedHeroIds = new List<string>(heroIds);
        }

        /// <summary>
        /// Get the current deployment list.
        /// </summary>
        public List<string> GetDeployment()
        {
            return _state.deployedHeroIds;
        }

        // ============================================================
        // Save / Load
        // ============================================================

        string GetSlotFileName(int slot)
        {
            return $"saves/slot_{slot:D2}.json";
        }

        /// <summary>
        /// Save current game state to a slot.
        /// </summary>
        public void SaveToSlot(int slot)
        {
            if (slot < 1 || slot > MaxSaveSlots) return;

            _state.timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            _saveSystem.SaveJson(GetSlotFileName(slot), _state);

            EventBus.GameSaved(slot);
            Debug.Log($"[GameStateManager] Game saved to slot {slot}");
        }

        /// <summary>
        /// Load game state from a slot. Returns false if slot doesn't exist.
        /// </summary>
        public bool LoadFromSlot(int slot)
        {
            if (slot < 1 || slot > MaxSaveSlots) return false;

            string fileName = GetSlotFileName(slot);
            if (!_saveSystem.FileExists(fileName)) return false;

            _state = _saveSystem.LoadJson<GameState>(fileName);
            RecalculateAllStats();
            WasLoadedFromSave = true;

            EventBus.GameLoaded(slot);
            Debug.Log($"[GameStateManager] Game loaded from slot {slot}");
            return true;
        }

        /// <summary>
        /// Get metadata for a save slot (for UI display).
        /// </summary>
        public SaveSlotInfo GetSlotInfo(int slot)
        {
            var info = new SaveSlotInfo { slot = slot };
            string fileName = GetSlotFileName(slot);

            if (_saveSystem.FileExists(fileName))
            {
                info.exists = true;
                var state = _saveSystem.LoadJson<GameState>(fileName);
                info.chapterName = $"Chapter {state.currentChapter}";
                info.timestamp = state.timestamp;
            }

            return info;
        }

        /// <summary>
        /// Check if a save slot has data.
        /// </summary>
        public bool SlotExists(int slot)
        {
            return _saveSystem.FileExists(GetSlotFileName(slot));
        }
    }
}
