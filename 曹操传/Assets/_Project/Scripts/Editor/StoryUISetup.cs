using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using CaoCao.Core;
using CaoCao.Story;
using CaoCao.UI;

namespace CaoCao.Editor
{
    /// <summary>
    /// Sets up Story scene UI elements to match Godot StoryScene.tscn styling.
    /// Run via menu: CaoCao/Setup Story UI
    /// </summary>
    public static class StoryUISetup
    {
        // Godot colors from StoryScene.tscn
        static readonly Color BoxBorderColor = new(0.5f, 0.38f, 0.2f, 1f);
        static readonly Color BoxBgColor = new(0.18f, 0.12f, 0.1f, 0.95f);
        static readonly Color TextColor = new(0.95f, 0.95f, 0.95f, 1f);
        static readonly Color NameTextColor = new(0.95f, 0.9f, 0.78f, 1f);
        static readonly Color MoralityBgColor = new(0.2f, 0.18f, 0.18f, 1f);

        const string ChoiceBtnPrefabPath = "Assets/_Project/Prefabs/UI/ChoiceButton.prefab";

        [MenuItem("CaoCao/Setup Story UI")]
        public static void SetupAll()
        {
            // Find the Canvas
            var canvas = Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
            if (canvas == null)
            {
                Debug.LogError("[StoryUISetup] No Canvas found in scene.");
                return;
            }

            // 1. Style DialogueBox
            var db = Object.FindFirstObjectByType<DialogueBox>(FindObjectsInactive.Include);
            if (db != null)
                SetupDialogueBox(db);
            else
                Debug.LogWarning("[StoryUISetup] DialogueBox not found.");

            // 2. Style ChoicePanel
            var cp = Object.FindFirstObjectByType<ChoicePanel>(FindObjectsInactive.Include);
            if (cp != null)
                SetupChoicePanel(cp);
            else
                Debug.LogWarning("[StoryUISetup] ChoicePanel not found.");

            // 3. Create/setup TopBar with MoralityBar and LocationLabel
            SetupTopBar(canvas.transform);

            // 4. Fix Background SpriteRenderer - make transparent until runtime loads sprite
            SetupWorldBackground();

            // 5. Create ChoiceButton prefab
            CreateChoiceButtonPrefab();

            // 6. Wire choiceButtonPrefab
            if (cp != null)
                WireChoiceButtonPrefab(cp);

            // 7. Add BackgroundFallback (dark rect behind everything, like Godot's BackgroundFallback)
            SetupFallbackBackground(canvas.transform);

            // 8. Copy missing assets from Godot project
            CopyMissingAssets();

            // 9. Setup sprite sheets for story characters
            SetupSpriteSheets();

            // 10. Create Hero and Oldman GameObjects
            SetupStoryUnits();

            // 11. Ensure InputManager exists (needed for dialogue click handling)
            EnsureInputManager();

            // 12. Create story map assets from DSL (for grid editor)
            CreateStoryMapAssets();

            Debug.Log("[StoryUISetup] Story UI setup complete.");
        }

        // ============================================================
        // DialogueBox: bottom-anchored, dark bg + brown border
        // Godot: anchor(0,1,1,1), offset_top=-210
        // All children use Godot absolute coords (top-left origin).
        // In Unity, DialogueBox anchor is bottom-stretch, so child positions
        // use anchor(0,1) with negative Y = downward from top of 210px box.
        // ============================================================
        static void SetupDialogueBox(DialogueBox db)
        {
            var rt = db.GetComponent<RectTransform>();
            if (rt == null) return;

            // Bottom-anchored, full width, 210px tall (matching Godot)
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, 0);
            rt.pivot = new Vector2(0.5f, 0);
            rt.offsetMin = new Vector2(0, 0);
            rt.offsetMax = new Vector2(0, 210);

            // Main Image: transparent (children handle visuals)
            var mainImg = db.GetComponent<Image>();
            if (mainImg != null)
            {
                mainImg.color = new Color(0, 0, 0, 0);
                mainImg.raycastTarget = true;
            }

            // --- Background child (BoxBorder + BoxBG combined) ---
            // Godot BoxBorder: anchors(0,0,1,1), offsets(120,28,-120,-18)
            // Godot BoxBG:     anchors(0,0,1,1), offsets(122,30,-122,-20)
            // In Unity: offsetMin=(left,bottom), offsetMax=(right,top)
            // Godot bottom offset -18 means 18px from bottom -> Unity bottom=18
            // Godot top offset 28 means 28px from top -> Unity top=-28
            var bgTf = FindDeep(db.transform, "Background");
            if (bgTf != null)
            {
                var bgRt = bgTf.GetComponent<RectTransform>();
                bgRt.anchorMin = Vector2.zero;
                bgRt.anchorMax = Vector2.one;
                bgRt.offsetMin = new Vector2(122, 20);   // Godot BoxBG: left=122, bottom=20
                bgRt.offsetMax = new Vector2(-122, -30);  // Godot BoxBG: right=-122, top=30→-30

                var bgImg = bgTf.GetComponent<Image>();
                if (bgImg != null)
                    bgImg.color = BoxBgColor;

                var outline = bgTf.GetComponent<Outline>();
                if (outline == null)
                    outline = bgTf.gameObject.AddComponent<Outline>();
                outline.effectColor = BoxBorderColor;
                outline.effectDistance = new Vector2(2, -2);
            }

            // --- NameBar (border + inner) ---
            // Godot: NameBar(150,10,335,42) = border color
            //        NameBarInner(152,12,333,40) = bg color
            // Both use no anchors (default 0,0,0,0) -> absolute pixel from top-left
            // Unity: position from top-left of 210px DialogueBox
            // Width = 335-150 = 185, Height = 42-10 = 32
            SetupNameBar(db.transform);

            // --- NameLabel ---
            // Godot: offsets (168,12,430,42) -> absolute from DialogueBox top-left
            // Width = 430-168 = 262, Height = 42-12 = 30
            var nameLabelTf = FindDeep(db.transform, "NameLabel");
            if (nameLabelTf != null)
            {
                var nlRt = nameLabelTf.GetComponent<RectTransform>();
                // Use top-left anchor, position relative to top of box
                nlRt.anchorMin = new Vector2(0, 1);
                nlRt.anchorMax = new Vector2(0, 1);
                nlRt.pivot = new Vector2(0, 1);
                nlRt.anchoredPosition = new Vector2(168, -12); // Godot top=12 -> -12 from top
                nlRt.sizeDelta = new Vector2(262, 30);

                var nlTmp = nameLabelTf.GetComponent<TMP_Text>();
                if (nlTmp != null)
                {
                    nlTmp.fontSize = 22;
                    nlTmp.color = NameTextColor;
                    nlTmp.alignment = TextAlignmentOptions.Left;
                    nlTmp.verticalAlignment = VerticalAlignmentOptions.Middle;
                }
            }

            // --- ContentText ---
            // Godot: anchors(0,0,1,1), offsets(168,50,-120,-30)
            // Unity: offsetMin=(left,bottom), offsetMax=(right,top)
            var contentTf = FindDeep(db.transform, "ContentText");
            if (contentTf != null)
            {
                var ctRt = contentTf.GetComponent<RectTransform>();
                ctRt.anchorMin = Vector2.zero;
                ctRt.anchorMax = Vector2.one;
                ctRt.offsetMin = new Vector2(168, 30);   // left=168, bottom=30 (Godot bottom=-30)
                ctRt.offsetMax = new Vector2(-120, -50);  // right=-120, top=-50 (Godot top=50)

                var ctTmp = contentTf.GetComponent<TMP_Text>();
                if (ctTmp != null)
                {
                    ctTmp.fontSize = 24;
                    ctTmp.color = TextColor;
                    ctTmp.alignment = TextAlignmentOptions.TopLeft;
                    ctTmp.overflowMode = TextOverflowModes.Overflow;
                }
            }

            // --- PortraitLeft ---
            // Godot: PortraitLeftFrame anchors(0,0,0,1), offsets(24,6,156,-16)
            // Width = 156-24 = 132, stretches full height minus 6+16=22px
            // PortraitLeftTex inside: anchors(0,0,1,1), offsets(6,6,-6,-6) = 6px padding
            var pLeftTf = FindDeep(db.transform, "PortraitLeft");
            if (pLeftTf != null)
                SetupPortrait(pLeftTf, isRight: false);

            // --- PortraitRight ---
            // Godot: PortraitRightFrame anchors(1,0,1,1), offsets(-156,6,-24,-16)
            var pRightTf = FindDeep(db.transform, "PortraitRight");
            if (pRightTf != null)
                SetupPortrait(pRightTf, isRight: true);

            // --- ContinueIndicator ---
            var contTf = FindDeep(db.transform, "ContinueIndicator");
            if (contTf != null)
            {
                var ciRt = contTf.GetComponent<RectTransform>();
                ciRt.anchorMin = new Vector2(1, 0);
                ciRt.anchorMax = new Vector2(1, 0);
                ciRt.pivot = new Vector2(1, 0);
                ciRt.anchoredPosition = new Vector2(-130, 22);
                ciRt.sizeDelta = new Vector2(20, 20);
                var ciImg = contTf.GetComponent<Image>();
                if (ciImg != null)
                    ciImg.color = new Color(0.95f, 0.9f, 0.78f, 0.8f);
                contTf.gameObject.SetActive(false);
            }

            EditorUtility.SetDirty(db.gameObject);
            Debug.Log("[StoryUISetup] DialogueBox styled.");
        }

        /// <summary>
        /// Creates the double-layer name bar matching Godot:
        /// NameBar (border color) + NameBarInner (bg color)
        /// </summary>
        static void SetupNameBar(Transform dbTransform)
        {
            // Always destroy and recreate to avoid stale reference issues
            var oldBars = new System.Collections.Generic.List<GameObject>();
            for (int i = dbTransform.childCount - 1; i >= 0; i--)
            {
                try
                {
                    var child = dbTransform.GetChild(i);
                    if (child != null && child.gameObject != null && child.gameObject.name == "NameBar")
                        oldBars.Add(child.gameObject);
                }
                catch { /* skip destroyed children */ }
            }
            foreach (var old in oldBars)
                Object.DestroyImmediate(old);

            // Create fresh NameBar (outer = border color like Godot NameBar)
            var nbGo = new GameObject("NameBar");
            nbGo.transform.SetParent(dbTransform, false);
            nbGo.transform.SetSiblingIndex(1); // after Background
            var nbRt = nbGo.AddComponent<RectTransform>();
            nbRt.anchorMin = new Vector2(0, 1);
            nbRt.anchorMax = new Vector2(0, 1);
            nbRt.pivot = new Vector2(0, 1);
            nbRt.anchoredPosition = new Vector2(150, -10);
            nbRt.sizeDelta = new Vector2(185, 32);
            var nbImg = nbGo.AddComponent<Image>();
            nbImg.color = BoxBorderColor;

            // Create NameBarInner (bg color like Godot NameBarInner, 2px inset)
            var innerGo = new GameObject("NameBarInner");
            innerGo.transform.SetParent(nbGo.transform, false);
            var innerRt = innerGo.AddComponent<RectTransform>();
            innerRt.anchorMin = Vector2.zero;
            innerRt.anchorMax = Vector2.one;
            innerRt.offsetMin = new Vector2(2, 2);
            innerRt.offsetMax = new Vector2(-2, -2);
            var innerImg = innerGo.AddComponent<Image>();
            innerImg.color = BoxBgColor;
        }

        /// <summary>
        /// Setup portrait frame matching Godot's Panel + TextureRect structure.
        /// Godot: Frame is a Panel (border container), TextureRect child shows portrait with 6px padding.
        /// Unity: Frame Image is the border (BoxBorderColor), child PortraitImage shows the portrait.
        /// DialogueBox wires: portraitLeftFrame = frame GO, portraitLeftImage = child Image.
        /// </summary>
        static void SetupPortrait(Transform portraitTf, bool isRight)
        {
            var pRt = portraitTf.GetComponent<RectTransform>();
            if (pRt == null) return;

            if (isRight)
            {
                // Godot: anchors(1,0,1,1), offsets(-156,6,-24,-16)
                pRt.anchorMin = new Vector2(1, 0);
                pRt.anchorMax = new Vector2(1, 1);
                pRt.pivot = new Vector2(1, 0.5f);
                pRt.offsetMin = new Vector2(-156, 16);
                pRt.offsetMax = new Vector2(-24, -6);
            }
            else
            {
                // Godot: anchors(0,0,0,1), offsets(24,6,156,-16)
                pRt.anchorMin = new Vector2(0, 0);
                pRt.anchorMax = new Vector2(0, 1);
                pRt.pivot = new Vector2(0, 0.5f);
                pRt.offsetMin = new Vector2(24, 16);
                pRt.offsetMax = new Vector2(156, -6);
            }

            // Frame Image = border color (like Godot Panel)
            var frameImg = portraitTf.GetComponent<Image>();
            if (frameImg != null)
            {
                frameImg.color = BoxBorderColor;
                frameImg.type = Image.Type.Simple;
            }

            // Remove old Outline (we use the frame Image color instead)
            var pOutline = portraitTf.GetComponent<Outline>();
            if (pOutline != null) Object.DestroyImmediate(pOutline);

            // Remove old portrait children that may be stale from previous runs
            string childName = isRight ? "PortraitRightImage" : "PortraitLeftImage";
            for (int i = portraitTf.childCount - 1; i >= 0; i--)
            {
                try
                {
                    var c = portraitTf.GetChild(i);
                    if (c != null && c.gameObject != null)
                        Object.DestroyImmediate(c.gameObject);
                }
                catch { /* skip stale */ }
            }

            // Create fresh child PortraitImage (like Godot TextureRect with padding)
            var imgGo = new GameObject(childName);
            imgGo.transform.SetParent(portraitTf, false);

            var imgRt = imgGo.AddComponent<RectTransform>();
            imgRt.anchorMin = Vector2.zero;
            imgRt.anchorMax = Vector2.one;
            imgRt.offsetMin = new Vector2(4, 4);
            imgRt.offsetMax = new Vector2(-4, -4);

            var childImg = imgGo.AddComponent<Image>();
            childImg.color = Color.white;
            childImg.preserveAspect = true;
            childImg.type = Image.Type.Simple;
            childImg.raycastTarget = false;

            portraitTf.gameObject.SetActive(false);
        }

        // ============================================================
        // ChoicePanel: centered, 720x180
        // ============================================================
        static void SetupChoicePanel(ChoicePanel cp)
        {
            var rt = cp.GetComponent<RectTransform>();
            if (rt == null) return;

            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(720, 180);

            var mainImg = cp.GetComponent<Image>();
            if (mainImg == null)
                mainImg = cp.gameObject.AddComponent<Image>();
            mainImg.color = BoxBgColor;
            mainImg.raycastTarget = true;

            var outline = cp.GetComponent<Outline>();
            if (outline == null)
                outline = cp.gameObject.AddComponent<Outline>();
            outline.effectColor = BoxBorderColor;
            outline.effectDistance = new Vector2(2, -2);

            var vlg = cp.GetComponent<VerticalLayoutGroup>();
            if (vlg != null)
            {
                vlg.padding = new RectOffset(20, 20, 16, 16);
                vlg.spacing = 8;
                vlg.childAlignment = TextAnchor.MiddleCenter;
                vlg.childControlWidth = true;
                vlg.childControlHeight = false;
                vlg.childForceExpandWidth = true;
                vlg.childForceExpandHeight = false;
            }

            // Remove child "Background" since ChoicePanel itself has the bg now
            var bgChild = FindDeep(cp.transform, "Background");
            if (bgChild != null && bgChild != cp.transform)
                Object.DestroyImmediate(bgChild.gameObject);

            cp.gameObject.SetActive(false);
            EditorUtility.SetDirty(cp.gameObject);
            Debug.Log("[StoryUISetup] ChoicePanel styled.");
        }

        // ============================================================
        // TopBar: right-anchored, contains MoralityBar + LocationLabel
        // Godot: TopBar anchors (1,0,1,0) offset (-360,10,0,54) -> 360x44
        // Children: MoralityBar, MoralityLabel, LocationLabel
        // ============================================================
        static void SetupTopBar(Transform canvasRoot)
        {
            // Find or create TopBar
            var topBarTf = canvasRoot.Find("TopBar");
            if (topBarTf == null)
            {
                var tbGo = new GameObject("TopBar");
                tbGo.transform.SetParent(canvasRoot, false);
                var tbRt = tbGo.AddComponent<RectTransform>();
                // Godot: anchor right(1,0), offset (-360,10,0,54)
                tbRt.anchorMin = new Vector2(1, 1);
                tbRt.anchorMax = new Vector2(1, 1);
                tbRt.pivot = new Vector2(1, 1);
                tbRt.anchoredPosition = new Vector2(0, -10);
                tbRt.sizeDelta = new Vector2(360, 54);
                topBarTf = tbGo.transform;
            }
            else
            {
                var tbRt = topBarTf.GetComponent<RectTransform>();
                if (tbRt != null)
                {
                    tbRt.anchorMin = new Vector2(1, 1);
                    tbRt.anchorMax = new Vector2(1, 1);
                    tbRt.pivot = new Vector2(1, 1);
                    tbRt.anchoredPosition = new Vector2(0, -10);
                    tbRt.sizeDelta = new Vector2(360, 54);
                }
            }

            // --- Rebuild MoralityBar under TopBar ---
            // The old MoralityBar may use Transform (not RectTransform).
            // Delete old one and recreate it properly.
            // First, check if MoralityBar already exists under TopBar
            var existingMb = topBarTf.Find("MoralityBar");
            if (existingMb != null)
            {
                var mbComp = existingMb.GetComponent<MoralityBar>();
                if (mbComp != null && existingMb.GetComponent<RectTransform>() != null)
                {
                    // Already properly set up, just restyle it
                    SetupMoralityBarRects(mbComp);
                }
                else
                {
                    // Broken - destroy and recreate
                    Object.DestroyImmediate(existingMb.gameObject);
                    CreateMoralityBar(topBarTf);
                }
            }
            else
            {
                // No MoralityBar under TopBar. Check if there's one elsewhere (old setup)
                var oldMb = Object.FindFirstObjectByType<MoralityBar>(FindObjectsInactive.Include);
                if (oldMb != null)
                    Object.DestroyImmediate(oldMb.gameObject);
                CreateMoralityBar(topBarTf);
            }

            // --- MoralityLabel "中立值" ---
            // Godot: MoralityLabel anchors left(0,0) offset (0,2,120,20) -> 120x18
            var morLabelTf = topBarTf.Find("MoralityLabel");
            if (morLabelTf == null)
            {
                var mlGo = new GameObject("MoralityLabel");
                mlGo.transform.SetParent(topBarTf, false);
                var mlRt = mlGo.AddComponent<RectTransform>();
                mlRt.anchorMin = new Vector2(0, 1);
                mlRt.anchorMax = new Vector2(0, 1);
                mlRt.pivot = new Vector2(0, 1);
                mlRt.anchoredPosition = new Vector2(0, 0);
                mlRt.sizeDelta = new Vector2(120, 20);
                var mlTmp = mlGo.AddComponent<TextMeshProUGUI>();
                mlTmp.text = "中立值";
                mlTmp.fontSize = 18;
                mlTmp.color = NameTextColor;
                mlTmp.alignment = TextAlignmentOptions.Left;
            }
            else
            {
                var mlTmp = morLabelTf.GetComponent<TMP_Text>();
                if (mlTmp != null)
                {
                    mlTmp.text = "中立值";
                    mlTmp.fontSize = 18;
                    mlTmp.color = NameTextColor;
                }
            }

            // --- LocationLabel ---
            // Godot: LocationLabel anchors bottom-right (0,1,1,1) offset (0,28,0,54)
            var locTf = topBarTf.Find("LocationLabel");
            if (locTf == null)
            {
                var llGo = new GameObject("LocationLabel");
                llGo.transform.SetParent(topBarTf, false);
                locTf = llGo.transform;
            }

            if (locTf == null)
            {
                Debug.LogWarning("[StoryUISetup] Could not create LocationLabel.");
                return;
            }

            var llRt = locTf.GetComponent<RectTransform>();
            if (llRt == null) llRt = locTf.gameObject.AddComponent<RectTransform>();
            llRt.anchorMin = new Vector2(0, 0);
            llRt.anchorMax = new Vector2(1, 0);
            llRt.pivot = new Vector2(1, 0);
            llRt.anchoredPosition = new Vector2(0, 0);
            llRt.sizeDelta = new Vector2(0, 26);

            var llTmp = locTf.GetComponent<TMP_Text>();
            if (llTmp == null) llTmp = locTf.gameObject.AddComponent<TextMeshProUGUI>();
            llTmp.text = "";
            llTmp.fontSize = 18;
            llTmp.color = NameTextColor;
            llTmp.alignment = TextAlignmentOptions.Right;

            EditorUtility.SetDirty(topBarTf.gameObject);
            Debug.Log("[StoryUISetup] TopBar created with MoralityBar + LocationLabel.");
        }

        static void CreateMoralityBar(Transform topBar)
        {
            // Godot: MoralityBar in TopBar, contains MoralityBG, MoralityBorder, MoralityRed, MoralityBlue
            // Position: right side of TopBar, offset (120, 0, 360, 20) -> 240x20
            var mbGo = new GameObject("MoralityBar");
            mbGo.transform.SetParent(topBar, false);
            var mbRt = mbGo.AddComponent<RectTransform>();
            mbRt.anchorMin = new Vector2(0, 1);
            mbRt.anchorMax = new Vector2(1, 1);
            mbRt.pivot = new Vector2(0.5f, 1);
            mbRt.offsetMin = new Vector2(120, -20);  // left offset, bottom (from top)
            mbRt.offsetMax = new Vector2(0, 0);       // right, top
            // This gives us a bar from x=120 to right edge, 20px tall at top

            // MoralityBG (dark background)
            var bgGo = new GameObject("Background");
            bgGo.transform.SetParent(mbGo.transform, false);
            var bgRt = bgGo.AddComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.color = MoralityBgColor;

            // Red fill (left portion = morality%)
            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(mbGo.transform, false);
            var fillRt = fillGo.AddComponent<RectTransform>();
            fillRt.anchorMin = new Vector2(0, 0);
            fillRt.anchorMax = new Vector2(0.5f, 1); // 50% default
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;
            var fillImg = fillGo.AddComponent<Image>();
            fillImg.color = ThreeKingdomsTheme.MoralityRed;

            // Blue fill (right portion = 100-morality%)
            var blueGo = new GameObject("BlueFill");
            blueGo.transform.SetParent(mbGo.transform, false);
            var blueRt = blueGo.AddComponent<RectTransform>();
            blueRt.anchorMin = new Vector2(0.5f, 0);
            blueRt.anchorMax = new Vector2(1, 1);
            blueRt.offsetMin = Vector2.zero;
            blueRt.offsetMax = Vector2.zero;
            var blueImg = blueGo.AddComponent<Image>();
            blueImg.color = ThreeKingdomsTheme.MoralityBlue;

            // Border outline on MoralityBar itself
            var mbBgImg = mbGo.AddComponent<Image>();
            mbBgImg.color = new Color(0, 0, 0, 0);
            var outline = mbGo.AddComponent<Outline>();
            outline.effectColor = BoxBorderColor;
            outline.effectDistance = new Vector2(1, -1);

            // Add MoralityBar component
            var mb = mbGo.AddComponent<MoralityBar>();

            // Wire references via SerializedObject
            var so = new SerializedObject(mb);
            so.FindProperty("barRect").objectReferenceValue = mbRt;
            so.FindProperty("redFill").objectReferenceValue = fillImg;
            so.FindProperty("blueFill").objectReferenceValue = blueImg;
            so.ApplyModifiedProperties();

            EditorUtility.SetDirty(mbGo);
            Debug.Log("[StoryUISetup] MoralityBar created under TopBar.");
        }

        static void SetupMoralityBarRects(MoralityBar mb)
        {
            // Style existing properly-placed MoralityBar children
            var bgTf = FindDeep(mb.transform, "Background");
            if (bgTf != null)
            {
                var bgImg = bgTf.GetComponent<Image>();
                if (bgImg != null) bgImg.color = MoralityBgColor;
            }

            var fillTf = FindDeep(mb.transform, "Fill");
            if (fillTf != null)
            {
                var fillImg = fillTf.GetComponent<Image>();
                if (fillImg != null) fillImg.color = ThreeKingdomsTheme.MoralityRed;
            }

            var blueTf = FindDeep(mb.transform, "BlueFill");
            if (blueTf != null)
            {
                var blueImg = blueTf.GetComponent<Image>();
                if (blueImg != null) blueImg.color = ThreeKingdomsTheme.MoralityBlue;
            }

            EditorUtility.SetDirty(mb.gameObject);
        }

        // ============================================================
        // World Background SpriteRenderer: transparent until runtime
        // ============================================================
        static void SetupWorldBackground()
        {
            // Find Background with SpriteRenderer (world space, not UI)
            var allSR = Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var sr in allSR)
            {
                if (sr.gameObject.name == "Background")
                {
                    if (sr.sprite == null)
                        sr.color = new Color(0, 0, 0, 0);
                    EditorUtility.SetDirty(sr.gameObject);
                    Debug.Log("[StoryUISetup] Background SpriteRenderer set transparent.");
                    break;
                }
            }
        }

        // ============================================================
        // Fallback dark background (matches Godot's BackgroundFallback)
        // ============================================================
        static void SetupFallbackBackground(Transform canvasRoot)
        {
            // Ensure FadeOverlay or a fallback dark bg exists behind everything
            // This ensures black bg instead of skybox when no background image is loaded
            // Already exists as FadeOverlay in scene, just make sure it's behind
            var fadeOvl = canvasRoot.Find("FadeOverlay");
            if (fadeOvl != null)
            {
                fadeOvl.SetAsFirstSibling();
                var foImg = fadeOvl.GetComponent<Image>();
                if (foImg != null)
                {
                    foImg.color = new Color(0, 0, 0, 0); // start transparent
                    foImg.raycastTarget = false;
                }
            }
        }

        // ============================================================
        // ChoiceButton prefab
        // ============================================================
        static void CreateChoiceButtonPrefab()
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Prefabs"))
                AssetDatabase.CreateFolder("Assets/_Project", "Prefabs");
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Prefabs/UI"))
                AssetDatabase.CreateFolder("Assets/_Project/Prefabs", "UI");

            var go = new GameObject("ChoiceButton");
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(680, 40);

            var img = go.AddComponent<Image>();
            img.color = ThreeKingdomsTheme.ButtonNormal;

            var outline = go.AddComponent<Outline>();
            outline.effectColor = ThreeKingdomsTheme.PanelBorder;
            outline.effectDistance = new Vector2(1, -1);

            var btn = go.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = ThreeKingdomsTheme.ButtonNormal;
            colors.highlightedColor = ThreeKingdomsTheme.ButtonHover;
            colors.pressedColor = ThreeKingdomsTheme.ButtonPressed;
            colors.selectedColor = ThreeKingdomsTheme.ButtonNormal;
            btn.colors = colors;
            btn.targetGraphic = img;

            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 40;
            le.minHeight = 36;

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(go.transform, false);
            var labelRt = labelGo.AddComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = new Vector2(16, 0);
            labelRt.offsetMax = new Vector2(-16, 0);
            var tmp = labelGo.AddComponent<TextMeshProUGUI>();
            tmp.text = "Choice";
            tmp.fontSize = 22;
            tmp.color = ThreeKingdomsTheme.TextPrimary;
            tmp.alignment = TextAlignmentOptions.Center;

            PrefabUtility.SaveAsPrefabAsset(go, ChoiceBtnPrefabPath);
            Object.DestroyImmediate(go);
            Debug.Log($"[StoryUISetup] ChoiceButton prefab saved: {ChoiceBtnPrefabPath}");
        }

        static void WireChoiceButtonPrefab(ChoicePanel cp)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ChoiceBtnPrefabPath);
            if (prefab == null) return;

            var so = new SerializedObject(cp);
            var prop = so.FindProperty("choiceButtonPrefab");
            if (prop != null)
            {
                prop.objectReferenceValue = prefab;
                so.ApplyModifiedProperties();
                Debug.Log("[StoryUISetup] ChoicePanel.choiceButtonPrefab wired.");
            }
        }

        // ============================================================
        // Copy missing assets from Godot project to Unity Resources
        // ============================================================
        static void CopyMissingAssets()
        {
            // Godot project root (parent of Unity project)
            string godotRoot = System.IO.Path.GetFullPath(
                System.IO.Path.Combine(Application.dataPath, "../../"));

            var copies = new (string src, string dst)[]
            {
                ("assets/ui/backgrounds/xuanwo.jpg", "Assets/_Project/Resources/Backgrounds/xuanwo.jpg"),
            };

            foreach (var (src, dst) in copies)
            {
                if (System.IO.File.Exists(dst)) continue;
                string srcFull = System.IO.Path.Combine(godotRoot, src);
                if (System.IO.File.Exists(srcFull))
                {
                    string dstDir = System.IO.Path.GetDirectoryName(dst);
                    if (!System.IO.Directory.Exists(dstDir))
                        System.IO.Directory.CreateDirectory(dstDir);
                    System.IO.File.Copy(srcFull, dst);
                    Debug.Log($"[StoryUISetup] Copied missing asset: {src} -> {dst}");
                }
                else
                {
                    Debug.LogWarning($"[StoryUISetup] Source not found: {srcFull}");
                }
            }

            AssetDatabase.Refresh();

            // Fix import settings for newly copied textures
            foreach (var (_, dst) in copies)
            {
                if (!System.IO.File.Exists(dst)) continue;
                var importer = AssetImporter.GetAtPath(dst) as TextureImporter;
                if (importer != null && importer.textureType != TextureImporterType.Sprite)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.SaveAndReimport();
                    Debug.Log($"[StoryUISetup] Set sprite import for: {dst}");
                }
            }
        }

        // ============================================================
        // Sprite sheet import: set test_r1/r2 as Sprite with Multiple mode + slicing
        // Each sheet is 48px wide, with 3 frames of 48x64 stacked vertically (192 total height)
        // Frame 0 = idle, Frame 1-2 = walk
        // ============================================================
        static void SetupSpriteSheets()
        {
            var paths = new[]
            {
                "Assets/_Project/Resources/Sprites/Story/test_r1.png",
                "Assets/_Project/Resources/Sprites/Story/test_r2.png"
            };

            foreach (var path in paths)
            {
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Multiple;

                // Pixel art settings
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;

                // Define sprite rects: only first 3 frames used (idle + 2 walk), each 48x64
                // Godot uses indices 0=idle, 1-2=walk from top of sheet
                // Unity sprite Y starts from bottom, so frame 0 (top) has highest Y
                var fileName = System.IO.Path.GetFileNameWithoutExtension(path);
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                int texH = 192; // 3 * 64
                if (tex != null) texH = tex.height;

                int frameW = 48;
                int frameH = 64;
                int frameCount = 3; // Only use first 3 frames like Godot

                // Set sprite rects using spritesheet property
#pragma warning disable CS0618
                var rects = new SpriteMetaData[frameCount];
                for (int i = 0; i < frameCount; i++)
                {
                    rects[i] = new SpriteMetaData
                    {
                        name = $"{fileName}_{i}",
                        rect = new Rect(0, texH - (i + 1) * frameH, frameW, frameH),
                        alignment = (int)SpriteAlignment.BottomCenter,
                        pivot = new Vector2(0.5f, 0f)
                    };
                }
                importer.spritesheet = rects;
#pragma warning restore CS0618

                importer.SaveAndReimport();
                Debug.Log($"[StoryUISetup] Sprite sheet configured: {path} ({frameCount} frames)");
            }
        }

        // ============================================================
        // Create Hero and Oldman GameObjects with StoryUnit + SpriteRenderer
        // Godot: Hero at position (5,5), Oldman at (8,5) initially
        // ============================================================
        static void SetupStoryUnits()
        {
            var existingHero = FindOrCreateUnit("Hero");
            var existingOldman = FindOrCreateUnit("Oldman");

            SetupUnitComponents(existingHero, "Sprites/Story/test_r1", "Sprites/Story/test_r2");
            SetupUnitComponents(existingOldman, "Sprites/Story/test_r1", "Sprites/Story/test_r2");

            // Both start inactive (DSL set_pos activates them)
            existingHero.SetActive(false);
            existingOldman.SetActive(false);

            EditorUtility.SetDirty(existingHero);
            EditorUtility.SetDirty(existingOldman);
        }

        /// <summary>
        /// Find an existing unit by name (including inactive), or create one.
        /// Also cleans up duplicates from previous runs.
        /// </summary>
        static GameObject FindOrCreateUnit(string name)
        {
            // Find ALL StoryUnits (including inactive) and match by name
            var allUnits = Object.FindObjectsByType<StoryUnit>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            GameObject found = null;
            foreach (var u in allUnits)
            {
                if (u.gameObject.name == name)
                {
                    if (found == null)
                        found = u.gameObject;
                    else
                        Object.DestroyImmediate(u.gameObject); // duplicate
                }
            }

            // Also check plain GameObjects without StoryUnit (from broken creation)
            if (found == null)
            {
                // Search scene roots for inactive objects
                var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                foreach (var root in scene.GetRootGameObjects())
                {
                    if (root.name == name)
                    {
                        if (found == null)
                            found = root;
                        else
                            Object.DestroyImmediate(root);
                    }
                }
            }

            if (found == null)
            {
                found = CreateStoryUnitGO(name);
                Debug.Log($"[StoryUISetup] Created {name} GameObject.");
            }
            return found;
        }

        static GameObject CreateStoryUnitGO(string name)
        {
            var go = new GameObject(name);
            // Add SpriteRenderer as child for proper sorting
            var spriteChild = new GameObject("Sprite");
            spriteChild.transform.SetParent(go.transform, false);
            spriteChild.AddComponent<SpriteRenderer>();
            return go;
        }

        static void SetupUnitComponents(GameObject unitGo, string frontPath, string backPath)
        {
            // Ensure StoryUnit component
            var unit = unitGo.GetComponent<StoryUnit>();
            if (unit == null)
                unit = unitGo.AddComponent<StoryUnit>();

            // Ensure SpriteRenderer on child
            var sr = unitGo.GetComponentInChildren<SpriteRenderer>();
            if (sr == null)
            {
                var spriteChild = unitGo.transform.Find("Sprite");
                if (spriteChild == null)
                {
                    var childGo = new GameObject("Sprite");
                    childGo.transform.SetParent(unitGo.transform, false);
                    sr = childGo.AddComponent<SpriteRenderer>();
                }
                else
                {
                    sr = spriteChild.GetComponent<SpriteRenderer>();
                    if (sr == null) sr = spriteChild.gameObject.AddComponent<SpriteRenderer>();
                }
            }

            // Set sorting order so units appear above background
            sr.sortingOrder = 10;

            // Load sliced sprites from the sprite sheet assets
            var frontSprites = LoadSlicedSprites($"Assets/_Project/Resources/{frontPath}.png");
            var backSprites = LoadSlicedSprites($"Assets/_Project/Resources/{backPath}.png");

            // Wire via SerializedObject
            var so = new SerializedObject(unit);

            // spriteRenderer
            var srProp = so.FindProperty("spriteRenderer");
            if (srProp != null) srProp.objectReferenceValue = sr;

            // frontFrames
            var frontProp = so.FindProperty("frontFrames");
            if (frontProp != null && frontSprites.Length > 0)
            {
                frontProp.arraySize = frontSprites.Length;
                for (int i = 0; i < frontSprites.Length; i++)
                    frontProp.GetArrayElementAtIndex(i).objectReferenceValue = frontSprites[i];
            }

            // backFrames
            var backProp = so.FindProperty("backFrames");
            if (backProp != null && backSprites.Length > 0)
            {
                backProp.arraySize = backSprites.Length;
                for (int i = 0; i < backSprites.Length; i++)
                    backProp.GetArrayElementAtIndex(i).objectReferenceValue = backSprites[i];
            }

            so.ApplyModifiedProperties();
        }

        static Sprite[] LoadSlicedSprites(string assetPath)
        {
            var allAssets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            var sprites = new System.Collections.Generic.List<Sprite>();
            foreach (var asset in allAssets)
            {
                if (asset is Sprite s)
                    sprites.Add(s);
            }
            // Sort by name (test_r1_0, test_r1_1, test_r1_2...)
            sprites.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.Ordinal));
            return sprites.ToArray();
        }

        // ============================================================
        // Ensure InputManager exists in scene (required for dialogue input)
        // Boot scene has it, but when testing StoryScene directly it's missing
        // ============================================================
        static void EnsureInputManager()
        {
            var existing = Object.FindFirstObjectByType<CaoCao.Input.InputManager>(FindObjectsInactive.Include);
            if (existing != null) return;

            var go = new GameObject("InputManager");
            go.AddComponent<CaoCao.Input.InputManager>();
            EditorUtility.SetDirty(go);
            Debug.Log("[StoryUISetup] Created InputManager (required for dialogue clicks).");
        }

        // ============================================================
        // Create Story Map Assets from DSL
        // ============================================================
        [MenuItem("CaoCao/Create Story Map Assets")]
        public static void CreateStoryMapAssets()
        {
            var dsl = Resources.Load<TextAsset>("Data/story.dsl");
            if (dsl == null)
            {
                Debug.LogError("[StoryUISetup] DSL file not found at Resources/Data/story.dsl");
                return;
            }

            var parser = new CaoCao.Story.StoryDSLParser();
            var events = parser.Parse(dsl.text);

            // Extract unique map keys from BackgroundEvents
            var mapKeys = new System.Collections.Generic.List<string>();
            foreach (var ev in events)
            {
                if (ev is CaoCao.Story.BackgroundEvent bg)
                {
                    string key = CaoCao.Story.StoryMapHelper.ExtractMapKey(bg.Path);
                    if (!mapKeys.Contains(key))
                        mapKeys.Add(key);
                }
            }

            if (mapKeys.Count == 0)
            {
                Debug.LogWarning("[StoryUISetup] No background events found in DSL.");
                return;
            }

            // Ensure folder
            if (!AssetDatabase.IsValidFolder("Assets/_Project/ScriptableObjects"))
                AssetDatabase.CreateFolder("Assets/_Project", "ScriptableObjects");
            if (!AssetDatabase.IsValidFolder("Assets/_Project/ScriptableObjects/StoryMaps"))
                AssetDatabase.CreateFolder("Assets/_Project/ScriptableObjects", "StoryMaps");

            // Find or create registry
            CaoCao.Story.StoryMapDataRegistry registry = null;
            var guids = AssetDatabase.FindAssets("t:StoryMapDataRegistry");
            if (guids.Length > 0)
                registry = AssetDatabase.LoadAssetAtPath<CaoCao.Story.StoryMapDataRegistry>(
                    AssetDatabase.GUIDToAssetPath(guids[0]));

            if (registry == null)
            {
                registry = ScriptableObject.CreateInstance<CaoCao.Story.StoryMapDataRegistry>();
                AssetDatabase.CreateAsset(registry,
                    "Assets/_Project/ScriptableObjects/StoryMaps/StoryMapRegistry.asset");
                Debug.Log("[StoryUISetup] Created StoryMapRegistry asset.");
            }

            int created = 0;
            foreach (var key in mapKeys)
            {
                if (registry.GetMap(key) != null) continue;
                var mapData = ScriptableObject.CreateInstance<CaoCao.Story.StoryMapData>();
                mapData.mapKey = key;
                string fileName = key.Replace('/', '_');
                string path = $"Assets/_Project/ScriptableObjects/StoryMaps/{fileName}.asset";
                AssetDatabase.CreateAsset(mapData, path);
                registry.AddMap(mapData);
                created++;
                Debug.Log($"[StoryUISetup] Created StoryMapData: {key} -> {path}");
            }

            EditorUtility.SetDirty(registry);
            AssetDatabase.SaveAssets();
            Debug.Log($"[StoryUISetup] Story map assets done. Created {created}, total {mapKeys.Count} maps.");
        }

        // --- Helper ---
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
