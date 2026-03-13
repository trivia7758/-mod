using UnityEngine;
using UnityEditor;
using CaoCao.Story;

namespace CaoCao.Editor
{
    /// <summary>
    /// One-time setup: Disables old StorySceneController on StoryController,
    /// adds StoryDirector, and wires up references + Prologue.asset.
    /// Menu: CaoCao/Setup StoryDirector in Scene
    /// </summary>
    public static class SetupStoryDirector
    {
        [MenuItem("CaoCao/Setup StoryDirector in Scene")]
        public static void Setup()
        {
            // Find StoryController GameObject
            var controllerGo = GameObject.Find("StoryController");
            if (controllerGo == null)
            {
                Debug.LogError("[Setup] StoryController not found in scene!");
                return;
            }

            // Disable old StorySceneController
            var oldController = controllerGo.GetComponent<StorySceneController>();
            if (oldController != null)
            {
                Undo.RecordObject(oldController, "Disable StorySceneController");
                oldController.enabled = false;
                Debug.Log("[Setup] Disabled StorySceneController");
            }

            // Add StoryDirector if not already present
            var director = controllerGo.GetComponent<StoryDirector>();
            if (director == null)
            {
                director = Undo.AddComponent<StoryDirector>(controllerGo);
                Debug.Log("[Setup] Added StoryDirector component");
            }

            // Load Prologue.asset
            var sceneData = AssetDatabase.LoadAssetAtPath<StorySceneData>(
                "Assets/_Project/ScriptableObjects/StoryScenes/Prologue.asset");
            if (sceneData == null)
            {
                Debug.LogError("[Setup] Prologue.asset not found! Run CaoCao/Convert DSL to StorySceneData first.");
                return;
            }

            // Wire up references via SerializedObject
            var so = new SerializedObject(director);

            // sceneData
            so.FindProperty("sceneData").objectReferenceValue = sceneData;

            // Copy references from old controller if available
            if (oldController != null)
            {
                var oldSo = new SerializedObject(oldController);

                // dialogueBox
                var dialogueBox = oldSo.FindProperty("dialogueBox").objectReferenceValue;
                if (dialogueBox != null)
                    so.FindProperty("dialogueBox").objectReferenceValue = dialogueBox;

                // choicePanel
                var choicePanel = oldSo.FindProperty("choicePanel").objectReferenceValue;
                if (choicePanel != null)
                    so.FindProperty("choicePanel").objectReferenceValue = choicePanel;

                // background
                var background = oldSo.FindProperty("background").objectReferenceValue;
                if (background != null)
                    so.FindProperty("background").objectReferenceValue = background;

                // locationLabel
                var locationLabel = oldSo.FindProperty("locationLabel").objectReferenceValue;
                if (locationLabel != null)
                    so.FindProperty("locationLabel").objectReferenceValue = locationLabel;

                // moralityBar
                var moralityBar = oldSo.FindProperty("moralityBar").objectReferenceValue;
                if (moralityBar != null)
                    so.FindProperty("moralityBar").objectReferenceValue = moralityBar;
            }

            // actorParent = UnitContainer
            var unitContainer = GameObject.Find("UnitContainer");
            if (unitContainer != null)
                so.FindProperty("actorParent").objectReferenceValue = unitContainer.transform;

            so.ApplyModifiedProperties();

            // Mark scene dirty
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene());

            Debug.Log($"[Setup] StoryDirector configured with Prologue.asset ({sceneData.actions.Count} actions). Save the scene!");
        }
    }
}
