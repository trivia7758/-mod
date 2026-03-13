using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using CaoCao.UI;
using CaoCao.Story;

namespace CaoCao.Editor
{
    public static class UIReferenceWirer
    {
        [MenuItem("CaoCao/Wire UI References")]
        public static void WireAll()
        {
            var sm = Object.FindFirstObjectByType<ScreenManager>(FindObjectsInactive.Include);
            if (sm == null)
            {
                Debug.LogError("[UIReferenceWirer] No ScreenManager found in scene.");
                return;
            }

            var so = new SerializedObject(sm);

            // Wire ScreenManager -> screens
            WireField(so, "logoScreen", sm.GetComponentInChildren<LogoScreen>(true));
            WireField(so, "updateScreen", sm.GetComponentInChildren<UpdateScreen>(true));
            WireField(so, "announcementScreen", sm.GetComponentInChildren<AnnouncementScreen>(true));
            WireField(so, "menuScreen", sm.GetComponentInChildren<MenuScreen>(true));
            WireField(so, "settingsDialog", sm.GetComponentInChildren<SettingsDialog>(true));
            WireField(so, "loadDialog", sm.GetComponentInChildren<LoadDialog>(true));
            so.ApplyModifiedProperties();

            // Wire each screen's internal references
            WireLogoScreen(sm.GetComponentInChildren<LogoScreen>(true));
            WireUpdateScreen(sm.GetComponentInChildren<UpdateScreen>(true));
            WireMenuScreen(sm.GetComponentInChildren<MenuScreen>(true));
            WireSettingsDialog(sm.GetComponentInChildren<SettingsDialog>(true));
            WireAnnouncementScreen(sm.GetComponentInChildren<AnnouncementScreen>(true));
            WireLoadDialog(sm.GetComponentInChildren<LoadDialog>(true));

            Debug.Log("[UIReferenceWirer] All UI references wired successfully.");
        }

        static void WireLogoScreen(LogoScreen screen)
        {
            if (screen == null) return;
            var so = new SerializedObject(screen);
            var labels = screen.GetComponentsInChildren<TMP_Text>(true);
            foreach (var l in labels)
            {
                if (l.gameObject.name == "LogoLabel")
                    WireField(so, "logoLabel", l);
                else if (l.gameObject.name == "SubLabel")
                    WireField(so, "subLabel", l);
            }
            so.ApplyModifiedProperties();
        }

        static void WireUpdateScreen(UpdateScreen screen)
        {
            if (screen == null) return;
            var so = new SerializedObject(screen);

            var statusGo = FindDeep(screen.transform, "StatusLabel");
            if (statusGo != null)
                WireField(so, "statusLabel", statusGo.GetComponent<TMP_Text>());

            var fillGo = FindDeep(screen.transform, "Fill");
            if (fillGo != null)
                WireField(so, "progressFill", fillGo.GetComponent<Image>());

            var pctGo = FindDeep(screen.transform, "Percentage");
            if (pctGo != null)
                WireField(so, "progressText", pctGo.GetComponent<TMP_Text>());

            so.ApplyModifiedProperties();
        }

        static void WireMenuScreen(MenuScreen screen)
        {
            if (screen == null) return;
            var so = new SerializedObject(screen);

            WireButton(so, "startButton", screen.transform, "StartButton");
            WireButton(so, "loadButton", screen.transform, "LoadButton");
            WireButton(so, "settingsButton", screen.transform, "SettingsButton");
            WireButton(so, "noticeButton", screen.transform, "NoticeButton");

            so.ApplyModifiedProperties();
        }

        static void WireSettingsDialog(SettingsDialog screen)
        {
            if (screen == null) return;
            var so = new SerializedObject(screen);

            // Close button
            WireButton(so, "closeButton", screen.transform, "CloseButton");

            // Toggles
            WireToggle(so, "bgmToggle", screen.transform, "CB_BGM");
            WireToggle(so, "sfxToggle", screen.transform, "CB_SFX");
            WireToggle(so, "showHpToggle", screen.transform, "CB_ShowHP");
            WireToggle(so, "longPressToggle", screen.transform, "CB_LongPress");
            WireToggle(so, "autoMinimapToggle", screen.transform, "CB_AutoMinimap");
            WireToggle(so, "dialogHoldToggle", screen.transform, "CB_DialogHold");
            WireToggle(so, "statusChangeToggle", screen.transform, "CB_StatusChange");
            WireToggle(so, "critLineToggle", screen.transform, "CB_CritLine");
            WireToggle(so, "autoPlayToggle", screen.transform, "CB_AutoPlay");

            // Speed buttons (arrays)
            WireButtonArray(so, "msgSpeedBtns", screen.transform,
                new[] { "MsgSpeed_Fast", "MsgSpeed_Med", "MsgSpeed_Slow" });
            WireButtonArray(so, "moveSpeedBtns", screen.transform,
                new[] { "MoveSpeed_Fast", "MoveSpeed_Med", "MoveSpeed_Slow" });

            // Language dropdown
            var ddGo = FindDeep(screen.transform, "LanguageDropdown");
            if (ddGo != null)
                WireField(so, "languageDropdown", ddGo.GetComponent<TMP_Dropdown>());

            so.ApplyModifiedProperties();
        }

        static void WireAnnouncementScreen(AnnouncementScreen screen)
        {
            if (screen == null) return;
            var so = new SerializedObject(screen);

            var bodyGo = FindDeep(screen.transform, "Body");
            if (bodyGo != null)
                WireField(so, "bodyText", bodyGo.GetComponent<TMP_Text>());

            WireButton(so, "closeButton", screen.transform, "CloseButton");

            so.ApplyModifiedProperties();
        }

        static void WireLoadDialog(LoadDialog screen)
        {
            if (screen == null) return;
            var so = new SerializedObject(screen);

            WireButton(so, "closeButton", screen.transform, "CloseButton");
            WireButton(so, "bottomButton", screen.transform, "BottomButton");

            var contentGo = FindDeep(screen.transform, "Content");
            if (contentGo != null)
                WireField(so, "rowsContainer", contentGo.transform);

            so.ApplyModifiedProperties();
        }

        // ============================================================
        // StoryScene wiring
        // ============================================================
        [MenuItem("CaoCao/Wire Story References")]
        public static void WireStoryScene()
        {
            // StorySceneController
            var ctrl = Object.FindFirstObjectByType<StorySceneController>(FindObjectsInactive.Include);
            if (ctrl == null)
            {
                Debug.LogError("[UIReferenceWirer] No StorySceneController found in scene.");
                return;
            }

            var so = new SerializedObject(ctrl);

            // DialogueBox
            var db = Object.FindFirstObjectByType<DialogueBox>(FindObjectsInactive.Include);
            WireField(so, "dialogueBox", db);

            // ChoicePanel
            var cp = Object.FindFirstObjectByType<ChoicePanel>(FindObjectsInactive.Include);
            WireField(so, "choicePanel", cp);

            // Background SpriteRenderer (world space, not UI)
            var allSR = Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var sr in allSR)
            {
                if (sr.gameObject.name == "Background")
                {
                    WireField(so, "background", sr);
                    break;
                }
            }

            // Hero and Oldman StoryUnits
            var allUnits = Object.FindObjectsByType<StoryUnit>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var unit in allUnits)
            {
                if (unit.gameObject.name == "Hero")
                    WireField(so, "hero", unit);
                else if (unit.gameObject.name == "Oldman")
                    WireField(so, "oldman", unit);
            }

            // MoralityBar
            var mb = Object.FindFirstObjectByType<MoralityBar>(FindObjectsInactive.Include);
            WireField(so, "moralityBar", mb);

            // LocationLabel (inside TopBar)
            var canvas = Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
            if (canvas != null)
            {
                var topBar = canvas.transform.Find("TopBar");
                if (topBar != null)
                {
                    var locLabel = topBar.Find("LocationLabel");
                    if (locLabel != null)
                    {
                        var locTmp = locLabel.GetComponent<TMP_Text>();
                        if (locTmp != null) WireField(so, "locationLabel", locTmp);
                    }
                }
            }

            // StoryMapDataRegistry - try direct path first, then search
            StoryMapDataRegistry mapRegistry = null;
            string registryPath = "Assets/_Project/ScriptableObjects/StoryMaps/StoryMapRegistry.asset";
            mapRegistry = AssetDatabase.LoadAssetAtPath<StoryMapDataRegistry>(registryPath);
            if (mapRegistry == null)
            {
                var registryGuids = AssetDatabase.FindAssets("t:StoryMapDataRegistry");
                if (registryGuids.Length > 0)
                    mapRegistry = AssetDatabase.LoadAssetAtPath<StoryMapDataRegistry>(
                        AssetDatabase.GUIDToAssetPath(registryGuids[0]));
            }
            if (mapRegistry != null)
                WireField(so, "mapDataRegistry", mapRegistry);
            else
                Debug.LogWarning("[UIReferenceWirer] StoryMapDataRegistry not found. Run 'CaoCao/Setup Story UI' first.");

            so.ApplyModifiedProperties();

            // Wire DialogueBox internal references
            if (db != null) WireDialogueBox(db);

            // Wire ChoicePanel internal references
            if (cp != null) WireChoicePanel(cp);

            // Wire MoralityBar internal references
            if (mb != null) WireMoralityBar(mb);

            Debug.Log("[UIReferenceWirer] Story scene references wired successfully.");
        }

        static void WireDialogueBox(DialogueBox db)
        {
            var so = new SerializedObject(db);

            // textLabel -> ContentText
            var contentText = FindDeep(db.transform, "ContentText");
            if (contentText != null)
                WireField(so, "textLabel", contentText.GetComponent<TMP_Text>());

            // nameLabel -> NameLabel
            var nameLabel = FindDeep(db.transform, "NameLabel");
            if (nameLabel != null)
                WireField(so, "nameLabel", nameLabel.GetComponent<TMP_Text>());

            // nameBar -> prefer "NameBar" child, fallback to "Background"
            var nameBarTf = FindDeep(db.transform, "NameBar");
            if (nameBarTf != null && nameBarTf != db.transform)
                WireField(so, "nameBar", nameBarTf.gameObject);
            else
            {
                var nameBg = FindDeep(db.transform, "Background");
                if (nameBg != null)
                    WireField(so, "nameBar", nameBg.gameObject);
            }

            // portraitLeftFrame / portraitLeftImage (Image is on child PortraitLeftImage)
            var pLeft = FindDeep(db.transform, "PortraitLeft");
            if (pLeft != null)
            {
                WireField(so, "portraitLeftFrame", pLeft.gameObject);
                var pLeftImg = FindDeep(pLeft, "PortraitLeftImage");
                if (pLeftImg != null)
                    WireField(so, "portraitLeftImage", pLeftImg.GetComponent<Image>());
                else
                    WireField(so, "portraitLeftImage", pLeft.GetComponent<Image>());
            }

            // portraitRightFrame / portraitRightImage (Image is on child PortraitRightImage)
            var pRight = FindDeep(db.transform, "PortraitRight");
            if (pRight != null)
            {
                WireField(so, "portraitRightFrame", pRight.gameObject);
                var pRightImg = FindDeep(pRight, "PortraitRightImage");
                if (pRightImg != null)
                    WireField(so, "portraitRightImage", pRightImg.GetComponent<Image>());
                else
                    WireField(so, "portraitRightImage", pRight.GetComponent<Image>());
            }

            // dialogueRect -> the DialogueBox's own RectTransform
            WireField(so, "dialogueRect", db.GetComponent<RectTransform>());

            so.ApplyModifiedProperties();
        }

        static void WireChoicePanel(ChoicePanel cp)
        {
            var so = new SerializedObject(cp);

            // choiceContainer -> the panel itself (has VerticalLayoutGroup)
            WireField(so, "choiceContainer", cp.transform);

            // choiceButtonPrefab -> load from Assets
            var btnPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/UI/ChoiceButton.prefab");
            if (btnPrefab != null)
                WireField(so, "choiceButtonPrefab", btnPrefab);

            so.ApplyModifiedProperties();
        }

        static void WireMoralityBar(MoralityBar mb)
        {
            var so = new SerializedObject(mb);

            // barRect -> the MoralityBar's own RectTransform
            var rt = mb.GetComponent<RectTransform>();
            if (rt != null) WireField(so, "barRect", rt);

            // redFill -> "Fill" child
            var fill = FindDeep(mb.transform, "Fill");
            if (fill != null)
                WireField(so, "redFill", fill.GetComponent<Image>());

            // blueFill -> prefer "BlueFill", fallback to "Background"
            var blue = FindDeep(mb.transform, "BlueFill");
            if (blue != null)
                WireField(so, "blueFill", blue.GetComponent<Image>());
            else
            {
                var bg = FindDeep(mb.transform, "Background");
                if (bg != null)
                    WireField(so, "blueFill", bg.GetComponent<Image>());
            }

            // label -> try to find "MoralityLabel" sibling in parent (TopBar)
            if (mb.transform.parent != null)
            {
                var labelTf = mb.transform.parent.Find("MoralityLabel");
                if (labelTf != null)
                {
                    var labelTmp = labelTf.GetComponent<TMP_Text>();
                    if (labelTmp != null) WireField(so, "label", labelTmp);
                }
            }

            so.ApplyModifiedProperties();
        }

        // --- Helpers ---

        static void WireField(SerializedObject so, string fieldName, Object value)
        {
            if (value == null) return;
            var prop = so.FindProperty(fieldName);
            if (prop != null)
                prop.objectReferenceValue = value;
            else
                Debug.LogWarning($"[UIReferenceWirer] Field '{fieldName}' not found on {so.targetObject.GetType().Name}");
        }

        static void WireButton(SerializedObject so, string fieldName, Transform root, string goName)
        {
            var go = FindDeep(root, goName);
            if (go != null)
                WireField(so, fieldName, go.GetComponent<Button>());
        }

        static void WireToggle(SerializedObject so, string fieldName, Transform root, string goName)
        {
            var go = FindDeep(root, goName);
            if (go != null)
                WireField(so, fieldName, go.GetComponent<Toggle>());
        }

        static void WireButtonArray(SerializedObject so, string fieldName, Transform root, string[] goNames)
        {
            var prop = so.FindProperty(fieldName);
            if (prop == null) return;
            prop.arraySize = goNames.Length;
            for (int i = 0; i < goNames.Length; i++)
            {
                var go = FindDeep(root, goNames[i]);
                if (go != null)
                    prop.GetArrayElementAtIndex(i).objectReferenceValue = go.GetComponent<Button>();
            }
        }

        static Transform FindDeep(Transform parent, string name)
        {
            if (parent == null) return null;
            try
            {
                if (parent.gameObject == null) return null;
                if (parent.gameObject.name == name) return parent;
            }
            catch (System.Exception) { return null; }

            int count;
            try { count = parent.childCount; }
            catch (System.Exception) { return null; }

            for (int i = 0; i < count; i++)
            {
                Transform child;
                try { child = parent.GetChild(i); }
                catch (System.Exception) { continue; }
                if (child == null) continue;

                var result = FindDeep(child, name);
                if (result != null) return result;
            }
            return null;
        }
    }
}
