using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using CaoCao.Core;

namespace CaoCao.Editor
{
    public static class SetupHelper
    {
        [MenuItem("CaoCao/Create GameSettings Asset")]
        public static void CreateGameSettings()
        {
            var asset = ScriptableObject.CreateInstance<GameSettingsData>();
            asset.language = "zh";
            asset.messageSpeed = SpeedSetting.Mid;
            asset.moveSpeed = SpeedSetting.Mid;
            asset.autoPlay = true;

            string path = "Assets/_Project/ScriptableObjects/Settings/GameSettings.asset";
            string dir = System.IO.Path.GetDirectoryName(path);
            if (!System.IO.Directory.Exists(dir))
                System.IO.Directory.CreateDirectory(dir);

            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[CaoCao] Created GameSettings at {path}");

            // Try to assign to GameManager in scene
            var gm = Object.FindFirstObjectByType<GameManager>();
            if (gm != null)
            {
                var so = new SerializedObject(gm);
                var prop = so.FindProperty("gameSettings");
                if (prop != null)
                {
                    prop.objectReferenceValue = asset;
                    so.ApplyModifiedProperties();
                    Debug.Log("[CaoCao] Assigned GameSettings to GameManager");
                }
            }
        }

        [MenuItem("CaoCao/Setup Boot Scene")]
        public static void SetupBootScene()
        {
            // Ensure GameManager exists
            var gm = Object.FindFirstObjectByType<GameManager>();
            if (gm == null)
            {
                var go = new GameObject("GameManager");
                gm = go.AddComponent<GameManager>();
                go.AddComponent<SceneLoader>();
                go.AddComponent<CaoCao.Input.InputManager>();
                Debug.Log("[CaoCao] Created GameManager");
            }

            // Load and assign GameSettings
            var settings = AssetDatabase.LoadAssetAtPath<GameSettingsData>(
                "Assets/_Project/ScriptableObjects/Settings/GameSettings.asset");
            if (settings != null)
            {
                var so = new SerializedObject(gm);
                var prop = so.FindProperty("gameSettings");
                if (prop != null)
                {
                    prop.objectReferenceValue = settings;
                    so.ApplyModifiedProperties();
                }
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            Debug.Log("[CaoCao] Boot scene setup complete");
        }

        [MenuItem("CaoCao/Setup Build Settings")]
        public static void SetupBuildSettings()
        {
            var scenes = new[]
            {
                new EditorBuildSettingsScene("Assets/_Project/Scenes/Boot.unity", true),
                new EditorBuildSettingsScene("Assets/_Project/Scenes/MainMenu.unity", true),
                new EditorBuildSettingsScene("Assets/_Project/Scenes/StoryScene.unity", true),
                new EditorBuildSettingsScene("Assets/_Project/Scenes/Battle.unity", true),
            };
            EditorBuildSettings.scenes = scenes;
            Debug.Log("[CaoCao] Build Settings updated: Boot(0), MainMenu(1), StoryScene(2), Battle(3)");
        }

        [MenuItem("CaoCao/Fix All EventSystems (Use New Input System)")]
        public static void FixAllEventSystems()
        {
            string[] scenePaths = new[]
            {
                "Assets/_Project/Scenes/MainMenu.unity",
                "Assets/_Project/Scenes/StoryScene.unity",
                "Assets/_Project/Scenes/Battle.unity",
            };

            var currentScene = EditorSceneManager.GetActiveScene().path;
            int fixCount = 0;

            foreach (var path in scenePaths)
            {
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                var eventSystems = Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None);

                foreach (var es in eventSystems)
                {
                    // Remove old StandaloneInputModule if present
                    var oldModule = es.GetComponent<StandaloneInputModule>();
                    if (oldModule != null)
                    {
                        Object.DestroyImmediate(oldModule);
                        fixCount++;
                    }

                    // Add InputSystemUIInputModule if not present
                    if (es.GetComponent<InputSystemUIInputModule>() == null)
                    {
                        es.gameObject.AddComponent<InputSystemUIInputModule>();
                    }
                }

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            // Re-open original scene
            if (!string.IsNullOrEmpty(currentScene))
                EditorSceneManager.OpenScene(currentScene, OpenSceneMode.Single);

            Debug.Log($"[CaoCao] Fixed {fixCount} EventSystem(s) across {scenePaths.Length} scenes. Replaced StandaloneInputModule with InputSystemUIInputModule.");
        }

        [MenuItem("CaoCao/Setup CJK Font")]
        public static void SetupCJKFont()
        {
            string[] candidateFonts = new[]
            {
                "C:/Windows/Fonts/msyh.ttc",
                "C:/Windows/Fonts/msyhbd.ttc",
                "C:/Windows/Fonts/simsun.ttc",
                "C:/Windows/Fonts/simhei.ttf",
            };

            string foundFont = null;
            foreach (var path in candidateFonts)
            {
                if (System.IO.File.Exists(path))
                {
                    foundFont = path;
                    break;
                }
            }

            if (foundFont == null)
            {
                Debug.LogError("[CaoCao] No CJK font found on system.");
                return;
            }

            Debug.Log("[CaoCao] Found CJK font: " + foundFont);

            string destDir = "Assets/_Project/Fonts";
            if (!System.IO.Directory.Exists(destDir))
                System.IO.Directory.CreateDirectory(destDir);

            string fileName = System.IO.Path.GetFileName(foundFont);
            string destPath = destDir + "/" + fileName;

            if (!System.IO.File.Exists(destPath))
            {
                System.IO.File.Copy(foundFont, destPath);
                AssetDatabase.Refresh();
                Debug.Log("[CaoCao] Copied font to " + destPath);
            }

            var font = AssetDatabase.LoadAssetAtPath<Font>(destPath);
            if (font == null)
            {
                Debug.LogError("[CaoCao] Failed to load font at " + destPath);
                return;
            }

            string tmpFontPath = destDir + "/CJKFont SDF.asset";

            // Delete old broken asset if exists
            var oldAsset = AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>(tmpFontPath);
            if (oldAsset != null)
            {
                // Remove from fallback list first
                var defFont = TMPro.TMP_Settings.defaultFontAsset;
                if (defFont != null && defFont.fallbackFontAssetTable != null)
                {
                    defFont.fallbackFontAssetTable.Remove(oldAsset);
                    EditorUtility.SetDirty(defFont);
                }
                AssetDatabase.DeleteAsset(tmpFontPath);
                AssetDatabase.Refresh();
            }

            // Create font asset with explicit atlas texture that persists
            var tmpFont = TMPro.TMP_FontAsset.CreateFontAsset(
                font, 36, 4,
                UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,
                4096, 4096,
                TMPro.AtlasPopulationMode.Dynamic);

            if (tmpFont == null)
            {
                Debug.LogError("[CaoCao] Failed to create TMP_FontAsset");
                return;
            }

            // Save the font asset
            AssetDatabase.CreateAsset(tmpFont, tmpFontPath);

            // Save the atlas texture as sub-asset so it persists
            if (tmpFont.atlasTexture != null)
            {
                tmpFont.atlasTexture.name = "CJKFont SDF Atlas";
                AssetDatabase.AddObjectToAsset(tmpFont.atlasTexture, tmpFont);
            }

            // Save material as sub-asset
            if (tmpFont.material != null)
            {
                tmpFont.material.name = "CJKFont SDF Material";
                AssetDatabase.AddObjectToAsset(tmpFont.material, tmpFont);
            }

            AssetDatabase.SaveAssets();

            // Re-load to get the persisted version
            tmpFont = AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>(tmpFontPath);

            // Add as fallback to default font
            var defaultFont = TMPro.TMP_Settings.defaultFontAsset;
            if (defaultFont != null && tmpFont != null)
            {
                if (defaultFont.fallbackFontAssetTable == null)
                    defaultFont.fallbackFontAssetTable = new System.Collections.Generic.List<TMPro.TMP_FontAsset>();

                if (!defaultFont.fallbackFontAssetTable.Contains(tmpFont))
                {
                    defaultFont.fallbackFontAssetTable.Add(tmpFont);
                    EditorUtility.SetDirty(defaultFont);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[CaoCao] CJK font setup complete: " + tmpFontPath);
        }

        [MenuItem("CaoCao/Refresh Assets")]
        public static void RefreshAssets()
        {
            AssetDatabase.Refresh();
            Debug.Log("[CaoCao] Assets refreshed.");
        }

        [MenuItem("CaoCao/Copy Godot Assets")]
        public static void CopyGodotAssets()
        {
            string godotRoot = "C:/Users/123/Desktop/曹操传";
            string unityRes = "Assets/_Project/Resources";

            // Copy backgrounds
            CopyAsset(godotRoot + "/assets/ui/backgrounds/bg_home_v1.jpg",
                      unityRes + "/Backgrounds/bg_home_v1.jpg");

            // Copy story backgrounds
            CopyAsset(godotRoot + "/assets/ui/backgrounds/story/home.png",
                      unityRes + "/Backgrounds/Story/home.png");
            CopyAsset(godotRoot + "/assets/ui/backgrounds/story/jiaoqu.jpg",
                      unityRes + "/Backgrounds/Story/jiaoqu.jpg");
            CopyAsset(godotRoot + "/assets/ui/backgrounds/story/jiaoqu2.png",
                      unityRes + "/Backgrounds/Story/jiaoqu2.png");

            // Copy portraits
            CopyAsset(godotRoot + "/assets/ui/portraits/shenmilaoren.jpg",
                      unityRes + "/Portraits/shenmilaoren.jpg");
            CopyAsset(godotRoot + "/assets/ui/portraits/nanzhu.jpg",
                      unityRes + "/Portraits/nanzhu.jpg");
            CopyAsset(godotRoot + "/assets/ui/portraits/pangbai.jpg",
                      unityRes + "/Portraits/pangbai.jpg");

            // Copy story sprites
            CopyAsset(godotRoot + "/assets/sprites/story/test_r1.png",
                      unityRes + "/Sprites/Story/test_r1.png");
            CopyAsset(godotRoot + "/assets/sprites/story/test_r2.png",
                      unityRes + "/Sprites/Story/test_r2.png");

            // Copy battle sprites
            CopyAsset(godotRoot + "/assets/sprites/battle/test_s.bmp",
                      unityRes + "/Sprites/Battle/test_s.bmp");
            CopyAsset(godotRoot + "/assets/sprites/battle/test_m.bmp",
                      unityRes + "/Sprites/Battle/test_m.bmp");

            // Copy battle background
            CopyAsset(godotRoot + "/assets/ui/backgrounds/battle/level0.JPG",
                      unityRes + "/Backgrounds/Battle/level0.jpg");

            AssetDatabase.Refresh();
            Debug.Log("[CaoCao] Godot assets copied to Unity Resources.");
        }

        static void CopyAsset(string src, string dest)
        {
            if (!System.IO.File.Exists(src))
            {
                Debug.LogWarning("[CaoCao] Source not found: " + src);
                return;
            }
            string dir = System.IO.Path.GetDirectoryName(dest);
            if (!System.IO.Directory.Exists(dir))
                System.IO.Directory.CreateDirectory(dir);
            if (!System.IO.File.Exists(dest))
                System.IO.File.Copy(src, dest);
        }
    }
}
