using System;
using System.Collections.Generic;
using UnityEngine;
using CaoCao.Common;

namespace CaoCao.Core
{
    public static class EventBus
    {
        // App lifecycle
        public static event Action<string> OnLanguageChanged;
        public static event Action<string> OnScreenTransition;

        // Story
        public static event Action OnStoryCompleted;
        public static event Action<int> OnMoralityChanged;
        public static event Action<string> OnChoiceMade;

        // Battle
        public static event Action OnBattleStarted;
        public static event Action<string> OnTurnChanged;

        // Camp
        public static event Action OnCampEntered;
        public static event Action<string> OnCampMenuSelected;

        // Deployment
        public static event Action<List<string>> OnDeploymentConfirmed;
        public static event Action OnDeploymentCancelled;

        // Inventory
        public static event Action<string, int> OnItemAdded;
        public static event Action<string, int> OnItemRemoved;
        public static event Action<string, string, EquipSlot> OnEquipmentChanged;

        // Save/Load
        public static event Action<int> OnGameSaved;
        public static event Action<int> OnGameLoaded;

        // Invoke helpers — App
        public static void LanguageChanged(string lang) => OnLanguageChanged?.Invoke(lang);
        public static void ScreenTransition(string screen) => OnScreenTransition?.Invoke(screen);

        // Invoke helpers — Story
        public static void StoryCompleted() => OnStoryCompleted?.Invoke();
        public static void MoralityChanged(int value) => OnMoralityChanged?.Invoke(value);
        public static void ChoiceMade(string choiceId) => OnChoiceMade?.Invoke(choiceId);

        // Invoke helpers — Battle
        public static void BattleStarted() => OnBattleStarted?.Invoke();
        public static void TurnChanged(string team) => OnTurnChanged?.Invoke(team);

        // Invoke helpers — Camp
        public static void CampEntered() => OnCampEntered?.Invoke();
        public static void CampMenuSelected(string menu) => OnCampMenuSelected?.Invoke(menu);

        // Invoke helpers — Deployment
        public static void DeploymentConfirmed(List<string> heroIds) => OnDeploymentConfirmed?.Invoke(heroIds);
        public static void DeploymentCancelled() => OnDeploymentCancelled?.Invoke();

        // Invoke helpers — Inventory
        public static void ItemAdded(string itemId, int count) => OnItemAdded?.Invoke(itemId, count);
        public static void ItemRemoved(string itemId, int count) => OnItemRemoved?.Invoke(itemId, count);
        public static void EquipmentChanged(string heroId, string itemId, EquipSlot slot) =>
            OnEquipmentChanged?.Invoke(heroId, itemId, slot);

        // Invoke helpers — Save/Load
        public static void GameSaved(int slot) => OnGameSaved?.Invoke(slot);
        public static void GameLoaded(int slot) => OnGameLoaded?.Invoke(slot);

        public static void Clear()
        {
            OnLanguageChanged = null;
            OnScreenTransition = null;
            OnStoryCompleted = null;
            OnMoralityChanged = null;
            OnChoiceMade = null;
            OnBattleStarted = null;
            OnTurnChanged = null;
            OnCampEntered = null;
            OnCampMenuSelected = null;
            OnDeploymentConfirmed = null;
            OnDeploymentCancelled = null;
            OnItemAdded = null;
            OnItemRemoved = null;
            OnEquipmentChanged = null;
            OnGameSaved = null;
            OnGameLoaded = null;
        }
    }
}
