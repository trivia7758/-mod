#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using CaoCao.Camp;
using CaoCao.UI;
using CaoCao.Core;

namespace CaoCao.Editor
{
    /// <summary>
    /// Editor tool to create the CampScene with all required UI screens.
    /// Menu: CaoCao/Setup Camp Scene
    /// </summary>
    public static class SetupCampScene
    {
        [MenuItem("CaoCao/Setup Camp Scene")]
        public static void Execute()
        {
            // Create new scene
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Camera
            var camGo = new GameObject("Main Camera");
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 3.6f;
            cam.backgroundColor = new Color(0.1f, 0.08f, 0.06f);
            camGo.transform.position = new Vector3(6.4f, -3.6f, -10f);
            camGo.tag = "MainCamera";

            // EventSystem
            var esGo = new GameObject("EventSystem");
            esGo.AddComponent<EventSystem>();
            esGo.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();

            // Canvas
            var canvasGo = new GameObject("CampCanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            // Create screens
            var campScreen = CreateScreen<CampScreen>(canvasGo.transform, "CampScreen");
            var deployScreen = CreateScreen<DeploymentScreen>(canvasGo.transform, "DeploymentScreen");
            var heroInfoScreen = CreateScreen<HeroInfoScreen>(canvasGo.transform, "HeroInfoScreen");
            var equipScreen = CreateScreen<EquipmentScreen>(canvasGo.transform, "EquipmentScreen");
            var warehouseScreen = CreateScreen<WarehouseScreen>(canvasGo.transform, "WarehouseScreen");
            var saveLoadScreen = CreateScreen<SaveLoadScreen>(canvasGo.transform, "SaveLoadScreen");

            // Create CampManager
            var managerGo = new GameObject("CampManager");
            var manager = managerGo.AddComponent<CampManager>();

            // Wire references via SerializedObject
            var so = new SerializedObject(manager);
            so.FindProperty("campScreen").objectReferenceValue = campScreen;
            so.FindProperty("deploymentScreen").objectReferenceValue = deployScreen;
            so.FindProperty("heroInfoScreen").objectReferenceValue = heroInfoScreen;
            so.FindProperty("equipmentScreen").objectReferenceValue = equipScreen;
            so.FindProperty("warehouseScreen").objectReferenceValue = warehouseScreen;
            so.FindProperty("saveLoadScreen").objectReferenceValue = saveLoadScreen;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Add placeholder content to CampScreen
            SetupCampScreenContent(campScreen);

            // Save scene
            string scenePath = "Assets/_Project/Scenes/CampScene.unity";
            EditorSceneManager.SaveScene(scene, scenePath);

            // Add to Build Settings
            AddToBuildSettings(scenePath);

            Debug.Log("[SetupCampScene] CampScene created and saved at: " + scenePath);
            EditorUtility.DisplayDialog("Camp Scene Setup",
                "CampScene created successfully!\n\n" +
                "Screens created:\n" +
                "- CampScreen (main menu)\n" +
                "- DeploymentScreen\n" +
                "- HeroInfoScreen\n" +
                "- EquipmentScreen\n" +
                "- WarehouseScreen\n" +
                "- SaveLoadScreen\n\n" +
                "Scene saved at: " + scenePath,
                "OK");
        }

        static T CreateScreen<T>(Transform parent, string name) where T : BaseScreen
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            go.AddComponent<CanvasGroup>();

            var screen = go.AddComponent<T>();

            // Start hidden
            var cg = go.GetComponent<CanvasGroup>();
            cg.alpha = 0;
            cg.blocksRaycasts = false;
            cg.interactable = false;

            // Add background panel
            var bg = go.AddComponent<Image>();
            bg.color = ThreeKingdomsTheme.PanelBg;

            return screen;
        }

        static void SetupCampScreenContent(CampScreen campScreen)
        {
            var parent = campScreen.transform;

            // Title
            var titleGo = new GameObject("TitleLabel");
            titleGo.transform.SetParent(parent, false);
            var titleRt = titleGo.AddComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0.5f, 0.9f);
            titleRt.anchorMax = new Vector2(0.5f, 0.9f);
            titleRt.anchoredPosition = Vector2.zero;
            titleRt.sizeDelta = new Vector2(400, 50);
            var titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
            titleTmp.text = "曹操主营";
            titleTmp.fontSize = 36;
            titleTmp.color = ThreeKingdomsTheme.TextGold;
            titleTmp.alignment = TextAlignmentOptions.Center;

            // Menu buttons (vertical stack on the right)
            string[] labels = { "出兵", "武将", "装备", "仓库", "系统" };
            string[] ids = { "deploy", "heroes", "equip", "warehouse", "system" };

            for (int i = 0; i < labels.Length; i++)
            {
                var btn = ThreeKingdomsTheme.CreateButton(parent, labels[i],
                    new Vector2(450, 150 - i * 60), new Vector2(160, 45));
                btn.name = $"Btn_{ids[i]}";

                // Wire to CampScreen via SerializedObject
                var so = new SerializedObject(campScreen);
                string fieldName = ids[i] switch
                {
                    "deploy" => "deployButton",
                    "heroes" => "heroesButton",
                    "equip" => "equipButton",
                    "warehouse" => "warehouseButton",
                    "system" => "systemButton",
                    _ => ""
                };
                if (!string.IsNullOrEmpty(fieldName))
                {
                    var prop = so.FindProperty(fieldName);
                    if (prop != null)
                    {
                        prop.objectReferenceValue = btn;
                        so.ApplyModifiedPropertiesWithoutUndo();
                    }
                }
            }

            // Wire title label
            var campSo = new SerializedObject(campScreen);
            var titleProp = campSo.FindProperty("titleLabel");
            if (titleProp != null)
            {
                titleProp.objectReferenceValue = titleTmp;
                campSo.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        static void AddToBuildSettings(string scenePath)
        {
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(
                EditorBuildSettings.scenes);

            // Check if already in build settings
            foreach (var s in scenes)
            {
                if (s.path == scenePath) return;
            }

            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log($"[SetupCampScene] Added {scenePath} to Build Settings.");
        }
    }
}
#endif
