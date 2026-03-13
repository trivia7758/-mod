using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using CaoCao.Core;
using CaoCao.UI;
using System.Collections.Generic;

namespace CaoCao.Editor
{
    public static class UIPrefabGenerator
    {
        const string PrefabFolder = "Assets/_Project/Prefabs/UI";
        const string BgPath = "Assets/_Project/Resources/Backgrounds/bg_home_v1.jpg";

        [MenuItem("CaoCao/Generate UI Prefabs")]
        public static void GenerateAll()
        {
            EnsureFolder(PrefabFolder);

            GenerateLogoScreen();
            GenerateUpdateScreen();
            GenerateMenuScreen();
            GenerateSettingsDialog();
            GenerateAnnouncementScreen();
            GenerateLoadDialog();

            AssetDatabase.Refresh();
            Debug.Log("[UIPrefabGenerator] All 6 UI prefabs generated.");
        }

        // ============================================================
        // LogoScreen
        // ============================================================
        static void GenerateLogoScreen()
        {
            var root = CreateScreenRoot("LogoScreen");

            // Dark background
            var bgImg = root.gameObject.AddComponent<Image>();
            bgImg.color = new Color(0.08f, 0.08f, 0.12f, 1f);
            bgImg.raycastTarget = true;

            // Logo label
            var logoGo = CreateChild(root, "LogoLabel");
            var logoRt = logoGo.GetComponent<RectTransform>();
            logoRt.anchorMin = new Vector2(0, 0.4f);
            logoRt.anchorMax = new Vector2(1, 0.7f);
            logoRt.offsetMin = Vector2.zero;
            logoRt.offsetMax = Vector2.zero;
            var logoTmp = logoGo.AddComponent<TextMeshProUGUI>();
            logoTmp.text = "曹操传";
            logoTmp.fontSize = 72;
            logoTmp.color = Color.white;
            logoTmp.alignment = TextAlignmentOptions.Center;

            // Sub label
            var subGo = CreateChild(root, "SubLabel");
            var subRt = subGo.GetComponent<RectTransform>();
            subRt.anchorMin = new Vector2(0.1f, 0.25f);
            subRt.anchorMax = new Vector2(0.9f, 0.4f);
            subRt.offsetMin = Vector2.zero;
            subRt.offsetMax = Vector2.zero;
            var subTmp = subGo.AddComponent<TextMeshProUGUI>();
            subTmp.text = "MOD Framework";
            subTmp.fontSize = 28;
            subTmp.color = new Color(0.8f, 0.8f, 0.8f, 1f);
            subTmp.alignment = TextAlignmentOptions.Center;

            root.gameObject.AddComponent<LogoScreen>();
            SavePrefab(root.gameObject, "LogoScreen");
        }

        // ============================================================
        // UpdateScreen
        // ============================================================
        static void GenerateUpdateScreen()
        {
            var root = CreateScreenRoot("UpdateScreen");

            // Background
            var bgGo = CreateStretchChild(root, "Background");
            var bgImg = bgGo.AddComponent<RawImage>();
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(BgPath);
            if (tex != null) bgImg.texture = tex;
            else bgImg.color = new Color(0.08f, 0.08f, 0.12f, 1f);

            // Dim overlay
            var dimGo = CreateStretchChild(root, "Dim");
            var dimImg = dimGo.AddComponent<Image>();
            dimImg.color = new Color(0, 0, 0, 0.25f);
            dimImg.raycastTarget = false;

            // Panel
            var panelRt = ThreeKingdomsTheme.CreatePanel(root, "Panel",
                Vector2.zero, new Vector2(720, 320));

            // Title
            ThreeKingdomsTheme.CreateLabel(panelRt, "检查游戏更新", 28,
                new Vector2(0, 110), new Vector2(400, 40),
                TextAlignmentOptions.Center);

            // Status label
            var statusTmp = ThreeKingdomsTheme.CreateLabel(panelRt, "正在检查...", 22,
                new Vector2(0, 50), new Vector2(560, 40),
                TextAlignmentOptions.Center);
            statusTmp.gameObject.name = "StatusLabel";

            // Progress bar background
            var barBgGo = CreateChild(panelRt, "ProgressBg");
            var barBgRt = barBgGo.GetComponent<RectTransform>();
            barBgRt.anchoredPosition = new Vector2(0, -10);
            barBgRt.sizeDelta = new Vector2(520, 30);
            var barBgImage = barBgGo.AddComponent<Image>();
            barBgImage.color = ThreeKingdomsTheme.ProgressBg;

            // Progress fill
            var fillGo = CreateChild(barBgRt, "Fill");
            var fillRt = fillGo.GetComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = new Vector2(0, 1);
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;
            var fillImg = fillGo.AddComponent<Image>();
            fillImg.color = ThreeKingdomsTheme.ProgressFill;

            // Percentage text
            var pctGo = CreateStretchChild(barBgRt, "Percentage");
            var pctTmp = pctGo.AddComponent<TextMeshProUGUI>();
            pctTmp.text = "0%";
            pctTmp.fontSize = 18;
            pctTmp.color = ThreeKingdomsTheme.TextPrimary;
            pctTmp.alignment = TextAlignmentOptions.Center;

            root.gameObject.AddComponent<UpdateScreen>();
            SavePrefab(root.gameObject, "UpdateScreen");
        }

        // ============================================================
        // MenuScreen
        // ============================================================
        static void GenerateMenuScreen()
        {
            var root = CreateScreenRoot("MenuScreen");

            // Background
            var bgGo = CreateStretchChild(root, "Background");
            var bgImg = bgGo.AddComponent<RawImage>();
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(BgPath);
            if (tex != null) bgImg.texture = tex;
            else bgImg.color = new Color(0.08f, 0.08f, 0.12f, 1f);

            // Center Buttons
            var centerGo = CreateChild(root, "CenterButtons");
            var centerRt = centerGo.GetComponent<RectTransform>();
            centerRt.anchorMin = new Vector2(0.5f, 0.28f);
            centerRt.anchorMax = new Vector2(0.5f, 0.28f);
            centerRt.pivot = new Vector2(0.5f, 0.5f);
            centerRt.anchoredPosition = Vector2.zero;
            centerRt.sizeDelta = new Vector2(400, 120);
            var vlg = centerGo.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 12;
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childForceExpandWidth = false;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth = false;
            vlg.childControlHeight = false;

            CreateThemedButton(centerGo.transform, "StartButton", "开始游戏", 400, 52, 24f);
            CreateThemedButton(centerGo.transform, "LoadButton", "读取存档", 400, 52, 24f);

            // Right Buttons
            var rightGo = CreateChild(root, "RightButtons");
            var rightRt = rightGo.GetComponent<RectTransform>();
            rightRt.anchorMin = new Vector2(1f, 0.8f);
            rightRt.anchorMax = new Vector2(1f, 0.8f);
            rightRt.pivot = new Vector2(1f, 1f);
            rightRt.anchoredPosition = new Vector2(-20, 0);
            rightRt.sizeDelta = new Vector2(120, 100);
            var rvlg = rightGo.AddComponent<VerticalLayoutGroup>();
            rvlg.spacing = 8;
            rvlg.childAlignment = TextAnchor.UpperCenter;
            rvlg.childForceExpandWidth = false;
            rvlg.childForceExpandHeight = false;
            rvlg.childControlWidth = false;
            rvlg.childControlHeight = false;

            CreateThemedButton(rightGo.transform, "SettingsButton", "设置", 120, 40, 20f);
            CreateThemedButton(rightGo.transform, "NoticeButton", "公告", 120, 40, 20f);

            // Version label
            var verGo = CreateChild(root, "Version");
            var verRt = verGo.GetComponent<RectTransform>();
            verRt.anchorMin = new Vector2(0, 0);
            verRt.anchorMax = new Vector2(0, 0);
            verRt.pivot = new Vector2(0, 0);
            verRt.anchoredPosition = new Vector2(20, 20);
            verRt.sizeDelta = new Vector2(200, 30);
            var verTmp = verGo.AddComponent<TextMeshProUGUI>();
            verTmp.text = "version: 0.1.1";
            verTmp.fontSize = 16;
            verTmp.color = ThreeKingdomsTheme.TextPrimary;
            verTmp.alignment = TextAlignmentOptions.Left;

            root.gameObject.AddComponent<MenuScreen>();
            SavePrefab(root.gameObject, "MenuScreen");
        }

        // ============================================================
        // SettingsDialog
        // ============================================================
        static void GenerateSettingsDialog()
        {
            var root = CreateScreenRoot("SettingsDialog");

            // Dim overlay — raycastTarget=true to block clicks outside panel
            var dimGo = CreateStretchChild(root, "Dim");
            var dimImg = dimGo.AddComponent<Image>();
            dimImg.color = new Color(0, 0, 0, 0.5f);
            dimImg.raycastTarget = true; // blocks outside clicks, panel is on top

            // Panel
            var panel = ThreeKingdomsTheme.CreatePanel(root, "Panel",
                Vector2.zero, new Vector2(1120, 640));

            // Title
            ThreeKingdomsTheme.CreateLabel(panel, "设置", 26,
                new Vector2(0, 286), new Vector2(400, 38),
                TextAlignmentOptions.Center);

            // Close button
            var closeBtn = ThreeKingdomsTheme.CreateButton(panel, "关闭",
                new Vector2(490, 288), new Vector2(100, 40), 20f);
            closeBtn.gameObject.name = "CloseButton";

            // Content area (no scroll - matches Godot offset 24/70/-24/-24)
            var contentGo = CreateChild(panel, "Content");
            var contentRt = contentGo.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0, 0);
            contentRt.anchorMax = new Vector2(1, 1);
            contentRt.offsetMin = new Vector2(24, 24);
            contentRt.offsetMax = new Vector2(-24, -70);
            var contentVlg = contentGo.AddComponent<VerticalLayoutGroup>();
            contentVlg.spacing = 0; // Godot VBoxContainer default = 0
            contentVlg.childAlignment = TextAnchor.UpperLeft;
            contentVlg.childForceExpandWidth = true;
            contentVlg.childForceExpandHeight = false;
            contentVlg.childControlWidth = true;
            contentVlg.childControlHeight = true; // let VLG control heights

            var content = contentGo.transform;

            // Audio section
            CreateSectionLabel(content, "音频设置");
            CreateCheckboxGrid(content, new[] {
                ("CB_BGM", "背景音乐", true),
                ("CB_SFX", "游戏音效", true)
            });

            // Display section
            CreateSectionLabel(content, "显示设置");
            CreateCheckboxGrid(content, new[] {
                ("CB_ShowHP", "战斗时显示血条", true),
                ("CB_LongPress", "长按可使剧情加快", true),
                ("CB_AutoMinimap", "自动显示战场缩小图", true),
                ("CB_DialogHold", "会话窗口不自动关闭", true),
                ("CB_StatusChange", "显示状态条变更", true),
                ("CB_CritLine", "开启暴击台词", true)
            });

            // Speed section
            CreateSectionLabel(content, "速度设置");
            var msgRow = CreateRow(content, 28);
            CreateRowLabel(msgRow, "信息显示速度", 140);
            CreateSpeedButton(msgRow, "MsgSpeed_Fast", "快", true);
            CreateSpeedButton(msgRow, "MsgSpeed_Med", "中", false);
            CreateSpeedButton(msgRow, "MsgSpeed_Slow", "慢", false);

            var moveRow = CreateRow(content, 28);
            CreateRowLabel(moveRow, "武将移动速度", 140);
            CreateSpeedButton(moveRow, "MoveSpeed_Fast", "快", true);
            CreateSpeedButton(moveRow, "MoveSpeed_Med", "中", false);
            CreateSpeedButton(moveRow, "MoveSpeed_Slow", "慢", false);

            // Language section
            CreateSectionLabel(content, "语言设置");
            var autoRow = CreateRow(content, 28);
            CreateCheckbox(autoRow, "CB_AutoPlay", "剧情自动播放", true);

            var langRow = CreateRow(content, 28);
            CreateRowLabel(langRow, "语言", 140);
            CreateLanguageDropdown(langRow);

            root.gameObject.AddComponent<SettingsDialog>();
            SavePrefab(root.gameObject, "SettingsDialog");
        }

        // ============================================================
        // AnnouncementScreen
        // ============================================================
        static void GenerateAnnouncementScreen()
        {
            var root = CreateScreenRoot("AnnouncementScreen");

            // Dim overlay
            var dimGo = CreateStretchChild(root, "Dim");
            dimGo.AddComponent<Image>().color = new Color(0, 0, 0, 0.15f);

            // Panel
            var panelRt = ThreeKingdomsTheme.CreatePanel(root, "Panel",
                Vector2.zero, new Vector2(1040, 560));
            var panelImg = panelRt.GetComponent<Image>();
            panelImg.color = new Color(0.18f, 0.12f, 0.1f, 1f);
            var panelOutline = panelRt.GetComponent<Outline>();
            if (panelOutline != null)
            {
                panelOutline.effectColor = new Color(0.5f, 0.38f, 0.2f, 1f);
                panelOutline.effectDistance = new Vector2(2, -2);
            }

            // Title
            var titleTmp = ThreeKingdomsTheme.CreateLabel(panelRt, "游戏公告", 26f,
                new Vector2(0, 245), new Vector2(400, 34),
                TextAlignmentOptions.Center);
            titleTmp.color = new Color(0.98f, 0.92f, 0.8f, 1f);

            // Close button
            var closeBtn = ThreeKingdomsTheme.CreateButton(panelRt, "关闭",
                new Vector2(450, 245), new Vector2(100, 40), 20f);
            closeBtn.gameObject.name = "CloseButton";
            var closeBtnImg = closeBtn.GetComponent<Image>();
            closeBtnImg.color = new Color(0.24f, 0.16f, 0.12f, 1f);
            var btnColors = closeBtn.colors;
            btnColors.normalColor = new Color(0.24f, 0.16f, 0.12f, 1f);
            btnColors.highlightedColor = new Color(0.32f, 0.2f, 0.16f, 1f);
            btnColors.pressedColor = new Color(0.18f, 0.12f, 0.1f, 1f);
            btnColors.selectedColor = new Color(0.24f, 0.16f, 0.12f, 1f);
            closeBtn.colors = btnColors;
            var closeLabelTmp = closeBtn.GetComponentInChildren<TMP_Text>();
            if (closeLabelTmp != null) closeLabelTmp.color = new Color(0.9f, 0.82f, 0.65f, 1f);

            // Body text
            var bodyGo = CreateChild(panelRt, "Body");
            var bodyRt = bodyGo.GetComponent<RectTransform>();
            bodyRt.anchorMin = new Vector2(0, 0);
            bodyRt.anchorMax = new Vector2(1, 1);
            bodyRt.offsetMin = new Vector2(24, 70);
            bodyRt.offsetMax = new Vector2(-24, -70);
            var bodyTmp = bodyGo.AddComponent<TextMeshProUGUI>();
            bodyTmp.text = "";
            bodyTmp.fontSize = 20;
            bodyTmp.color = ThreeKingdomsTheme.TextContent;
            bodyTmp.alignment = TextAlignmentOptions.TopLeft;
            bodyTmp.textWrappingMode = TextWrappingModes.Normal;
            bodyTmp.overflowMode = TextOverflowModes.ScrollRect;

            root.gameObject.AddComponent<AnnouncementScreen>();
            SavePrefab(root.gameObject, "AnnouncementScreen");
        }

        // ============================================================
        // LoadDialog
        // ============================================================
        static void GenerateLoadDialog()
        {
            var root = CreateScreenRoot("LoadDialog");

            Color parchmentBg = new(0.92f, 0.9f, 0.86f, 1f);
            Color parchmentBorder = new(0.45f, 0.36f, 0.2f, 1f);
            Color titleColor = new(0.6f, 0.48f, 0.28f, 1f);
            Color headerColor = new(0.55f, 0.45f, 0.28f, 1f);
            Color rowText = new(0.3f, 0.2f, 0.1f, 1f);
            Color closeNormal = new(0.7f, 0.7f, 0.7f, 1f);
            Color closeHover = new(0.82f, 0.82f, 0.82f, 1f);
            Color bottomNormal = new(0.86f, 0.74f, 0.45f, 1f);
            Color bottomHover = new(0.95f, 0.86f, 0.6f, 1f);
            Color borderColor = new(0.5f, 0.38f, 0.2f, 1f);

            // Dim overlay
            var dimGo = CreateStretchChild(root, "Dim");
            dimGo.AddComponent<Image>().color = new Color(0, 0, 0, 0.5f);

            // Panel
            var panelGo = CreateChild(root, "Panel");
            var panelRt = panelGo.GetComponent<RectTransform>();
            panelRt.anchoredPosition = Vector2.zero;
            panelRt.sizeDelta = new Vector2(1200, 640);
            panelGo.AddComponent<Image>().color = parchmentBg;
            var panelOutline = panelGo.AddComponent<Outline>();
            panelOutline.effectColor = parchmentBorder;
            panelOutline.effectDistance = new Vector2(2, -2);

            // Title
            var titleGo = CreateChild(panelRt, "Title");
            var titleRt = titleGo.GetComponent<RectTransform>();
            titleRt.anchoredPosition = new Vector2(0, 288);
            titleRt.sizeDelta = new Vector2(400, 40);
            var titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
            titleTmp.text = "读取记录";
            titleTmp.fontSize = 26;
            titleTmp.color = titleColor;
            titleTmp.alignment = TextAlignmentOptions.Center;

            // Close button "X"
            var closeBtnGo = CreateChild(panelRt, "CloseButton");
            var closeBtnRt = closeBtnGo.GetComponent<RectTransform>();
            closeBtnRt.anchoredPosition = new Vector2(560, 296);
            closeBtnRt.sizeDelta = new Vector2(48, 48);
            closeBtnGo.AddComponent<Image>().color = closeNormal;
            var closeOutline = closeBtnGo.AddComponent<Outline>();
            closeOutline.effectColor = borderColor;
            closeOutline.effectDistance = new Vector2(1, -1);
            var closeBtn = closeBtnGo.AddComponent<Button>();
            var cc = closeBtn.colors;
            cc.normalColor = closeNormal;
            cc.highlightedColor = closeHover;
            cc.pressedColor = closeNormal;
            cc.selectedColor = closeNormal;
            closeBtn.colors = cc;
            closeBtn.targetGraphic = closeBtnGo.GetComponent<Image>();
            var closeLblGo = CreateStretchChild(closeBtnRt, "Label");
            var closeLblTmp = closeLblGo.AddComponent<TextMeshProUGUI>();
            closeLblTmp.text = "X";
            closeLblTmp.fontSize = 22;
            closeLblTmp.color = rowText;
            closeLblTmp.alignment = TextAlignmentOptions.Center;

            // Header row
            var headerGo = CreateChild(panelRt, "Header");
            var headerRt = headerGo.GetComponent<RectTransform>();
            headerRt.anchorMin = new Vector2(0, 1);
            headerRt.anchorMax = new Vector2(1, 1);
            headerRt.pivot = new Vector2(0.5f, 1);
            headerRt.anchoredPosition = new Vector2(0, -66);
            headerRt.sizeDelta = new Vector2(-40, 30);
            var headerHlg = headerGo.AddComponent<HorizontalLayoutGroup>();
            headerHlg.spacing = 0;
            headerHlg.childAlignment = TextAnchor.MiddleLeft;
            headerHlg.childForceExpandWidth = false;
            headerHlg.childForceExpandHeight = true;
            headerHlg.childControlWidth = false;
            headerHlg.childControlHeight = true;

            CreateHeaderCell(headerGo.transform, "编号", 90, headerColor);
            CreateHeaderCell(headerGo.transform, "等级", 90, headerColor);
            CreateHeaderCell(headerGo.transform, "记录", 720, headerColor);
            CreateHeaderCell(headerGo.transform, "时间", 190, headerColor);

            // Scroll area
            var scrollGo = CreateChild(panelRt, "Scroll");
            var scrollRt = scrollGo.GetComponent<RectTransform>();
            scrollRt.anchorMin = new Vector2(0, 0);
            scrollRt.anchorMax = new Vector2(1, 1);
            scrollRt.offsetMin = new Vector2(20, 70);
            scrollRt.offsetMax = new Vector2(-20, -100);
            var scrollRectComp = scrollGo.AddComponent<ScrollRect>();
            scrollRectComp.horizontal = false;
            scrollRectComp.vertical = true;
            scrollRectComp.movementType = ScrollRect.MovementType.Clamped;
            scrollGo.AddComponent<RectMask2D>();

            var sContentGo = CreateChild(scrollRt, "Content");
            var sContentRt = sContentGo.GetComponent<RectTransform>();
            sContentRt.anchorMin = new Vector2(0, 1);
            sContentRt.anchorMax = new Vector2(1, 1);
            sContentRt.pivot = new Vector2(0.5f, 1);
            sContentRt.anchoredPosition = Vector2.zero;
            sContentRt.sizeDelta = Vector2.zero;
            var scsf = sContentGo.AddComponent<ContentSizeFitter>();
            scsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var sVlg = sContentGo.AddComponent<VerticalLayoutGroup>();
            sVlg.spacing = 2;
            sVlg.childAlignment = TextAnchor.UpperCenter;
            sVlg.childForceExpandWidth = true;
            sVlg.childForceExpandHeight = false;
            sVlg.childControlWidth = true;
            sVlg.childControlHeight = false;
            scrollRectComp.content = sContentRt;

            // Bottom button
            var bottomGo = CreateChild(panelRt, "BottomButton");
            var bottomRt = bottomGo.GetComponent<RectTransform>();
            bottomRt.anchoredPosition = new Vector2(0, -283);
            bottomRt.sizeDelta = new Vector2(240, 38);
            bottomGo.AddComponent<Image>().color = bottomNormal;
            var bottomOutline = bottomGo.AddComponent<Outline>();
            bottomOutline.effectColor = borderColor;
            bottomOutline.effectDistance = new Vector2(1, -1);
            var bottomBtn = bottomGo.AddComponent<Button>();
            var bc = bottomBtn.colors;
            bc.normalColor = bottomNormal;
            bc.highlightedColor = bottomHover;
            bc.pressedColor = bottomNormal;
            bc.selectedColor = bottomNormal;
            bottomBtn.colors = bc;
            bottomBtn.targetGraphic = bottomGo.GetComponent<Image>();
            var bottomLblGo = CreateStretchChild(bottomRt, "Label");
            var bottomLblTmp = bottomLblGo.AddComponent<TextMeshProUGUI>();
            bottomLblTmp.text = "读取回合初始";
            bottomLblTmp.fontSize = 20;
            bottomLblTmp.color = rowText;
            bottomLblTmp.alignment = TextAlignmentOptions.Center;

            root.gameObject.AddComponent<LoadDialog>();
            SavePrefab(root.gameObject, "LoadDialog");
        }

        // ============================================================
        // Shared Helpers
        // ============================================================

        static RectTransform CreateScreenRoot(string name)
        {
            var go = new GameObject(name);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
            go.AddComponent<CanvasGroup>();
            return rt;
        }

        static GameObject CreateChild(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            return go;
        }

        static GameObject CreateStretchChild(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
            return go;
        }

        static Button CreateThemedButton(Transform parent, string goName, string text,
            float width, float height, float fontSize)
        {
            var go = new GameObject(goName);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(width, height);

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
            le.preferredWidth = width;
            le.preferredHeight = height;

            var labelGo = CreateStretchChild(go.transform, "Label");
            var tmp = labelGo.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = ThreeKingdomsTheme.TextPrimary;
            tmp.alignment = TextAlignmentOptions.Center;

            return btn;
        }

        // --- Settings helpers ---

        static Transform CreateRow(Transform parent, float height)
        {
            var go = new GameObject("Row");
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = height;
            var hlg = go.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 16;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            return go.transform;
        }

        static void CreateSectionLabel(Transform parent, string text)
        {
            var go = new GameObject("Section_" + text);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 26;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 20;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = ThreeKingdomsTheme.TextPrimary;
        }

        /// <summary>
        /// Creates a 2-column grid of checkboxes, matching Godot GridContainer columns=2.
        /// Each row is ~28px tall, tightly packed.
        /// </summary>
        static void CreateCheckboxGrid(Transform parent, (string name, string label, bool on)[] items)
        {
            int rows = (items.Length + 1) / 2;
            float cellH = 28f;
            float totalH = rows * cellH;

            var go = new GameObject("CheckboxGrid");
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = totalH;

            var grid = go.AddComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;
            grid.cellSize = new Vector2(480, cellH);
            grid.spacing = new Vector2(8, 0);
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.childAlignment = TextAnchor.UpperLeft;

            foreach (var item in items)
                CreateCheckbox(go.transform, item.name, item.label, item.on);
        }

        static void CreateRowLabel(Transform parent, string text, float width)
        {
            var go = new GameObject("Label_" + text);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(width, 28);
            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth = width;
            le.preferredHeight = 28;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 18;
            tmp.color = ThreeKingdomsTheme.TextPrimary;
            tmp.alignment = TextAlignmentOptions.Left;
        }

        static Toggle CreateCheckbox(Transform parent, string goName, string text, bool defaultOn)
        {
            var go = new GameObject(goName);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(280, 28);
            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth = 280;
            le.preferredHeight = 28;

            var toggle = go.AddComponent<Toggle>();
            toggle.isOn = defaultOn;

            // Box (20x20) - light background with dark border for visibility
            var boxGo = new GameObject("Box");
            boxGo.transform.SetParent(go.transform, false);
            var boxRt = boxGo.AddComponent<RectTransform>();
            boxRt.anchorMin = new Vector2(0, 0.5f);
            boxRt.anchorMax = new Vector2(0, 0.5f);
            boxRt.pivot = new Vector2(0, 0.5f);
            boxRt.anchoredPosition = Vector2.zero;
            boxRt.sizeDelta = new Vector2(20, 20);
            var boxImg = boxGo.AddComponent<Image>();
            boxImg.color = new Color(0.85f, 0.82f, 0.76f, 1f); // light parchment bg
            var boxOutline = boxGo.AddComponent<Outline>();
            boxOutline.effectColor = new Color(0.4f, 0.32f, 0.2f, 1f); // dark brown border
            boxOutline.effectDistance = new Vector2(1, -1);

            // Checkmark - use TMP "✓" for clear visibility
            var checkGo = new GameObject("Checkmark");
            checkGo.transform.SetParent(boxGo.transform, false);
            var checkRt = checkGo.AddComponent<RectTransform>();
            checkRt.anchorMin = Vector2.zero;
            checkRt.anchorMax = Vector2.one;
            checkRt.sizeDelta = Vector2.zero;
            checkRt.anchoredPosition = Vector2.zero;
            // Use an Image fill for the checked state - bright gold/brown fill
            var checkImg = checkGo.AddComponent<Image>();
            checkImg.color = new Color(0.72f, 0.56f, 0.2f, 1f); // gold fill when checked

            // Label
            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(go.transform, false);
            var labelRt = labelGo.AddComponent<RectTransform>();
            labelRt.anchorMin = new Vector2(0, 0);
            labelRt.anchorMax = new Vector2(1, 1);
            labelRt.offsetMin = new Vector2(26, 0);
            labelRt.offsetMax = Vector2.zero;
            var tmp = labelGo.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 18;
            tmp.color = ThreeKingdomsTheme.TextPrimary;
            tmp.alignment = TextAlignmentOptions.Left;
            tmp.overflowMode = TextOverflowModes.Ellipsis;

            toggle.graphic = checkImg;
            toggle.targetGraphic = boxImg;

            return toggle;
        }

        static Button CreateSpeedButton(Transform parent, string goName, string text, bool active)
        {
            var go = new GameObject(goName);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(44, 26);
            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth = 44;
            le.preferredHeight = 26;

            var img = go.AddComponent<Image>();
            img.color = active ? ThreeKingdomsTheme.SpeedActive : ThreeKingdomsTheme.SpeedInactive;

            var outline = go.AddComponent<Outline>();
            outline.effectColor = ThreeKingdomsTheme.PanelBorder;
            outline.effectDistance = new Vector2(1, -1);

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;

            var labelGo = CreateStretchChild(go.transform, "Label");
            var tmp = labelGo.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 18;
            tmp.color = ThreeKingdomsTheme.TextPrimary;
            tmp.alignment = TextAlignmentOptions.Center;

            return btn;
        }

        static void CreateLanguageDropdown(Transform parent)
        {
            var go = new GameObject("LanguageDropdown");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(120, 30);
            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth = 120;
            le.preferredHeight = 30;

            go.AddComponent<Image>().color = ThreeKingdomsTheme.ButtonNormal;
            var dd = go.AddComponent<TMP_Dropdown>();
            dd.ClearOptions();
            dd.AddOptions(new List<string> { "中文", "English" });

            var captionGo = new GameObject("Caption");
            captionGo.transform.SetParent(go.transform, false);
            var crt = captionGo.AddComponent<RectTransform>();
            crt.anchorMin = Vector2.zero;
            crt.anchorMax = Vector2.one;
            crt.offsetMin = new Vector2(8, 0);
            crt.offsetMax = new Vector2(-20, 0);
            var captionTmp = captionGo.AddComponent<TextMeshProUGUI>();
            captionTmp.fontSize = 18;
            captionTmp.color = ThreeKingdomsTheme.TextPrimary;
            dd.captionText = captionTmp;

            var arrowGo = new GameObject("Arrow");
            arrowGo.transform.SetParent(go.transform, false);
            var art = arrowGo.AddComponent<RectTransform>();
            art.anchorMin = new Vector2(1, 0);
            art.anchorMax = new Vector2(1, 1);
            art.offsetMin = new Vector2(-20, 4);
            art.offsetMax = new Vector2(-4, -4);
            var arrowTmp = arrowGo.AddComponent<TextMeshProUGUI>();
            arrowTmp.text = "▼";
            arrowTmp.fontSize = 12;
            arrowTmp.color = ThreeKingdomsTheme.TextPrimary;
            arrowTmp.alignment = TextAlignmentOptions.Center;

            var templateGo = new GameObject("Template");
            templateGo.transform.SetParent(go.transform, false);
            var trt = templateGo.AddComponent<RectTransform>();
            trt.anchoredPosition = new Vector2(0, -30);
            trt.sizeDelta = new Vector2(120, 60);
            trt.pivot = new Vector2(0.5f, 1f);
            templateGo.AddComponent<Image>().color = new Color(0.15f, 0.12f, 0.1f, 1f);
            var scroll = templateGo.AddComponent<ScrollRect>();

            var viewGo = new GameObject("Viewport");
            viewGo.transform.SetParent(templateGo.transform, false);
            var vrt = viewGo.AddComponent<RectTransform>();
            vrt.anchorMin = Vector2.zero;
            vrt.anchorMax = Vector2.one;
            vrt.sizeDelta = Vector2.zero;
            viewGo.AddComponent<Image>().color = new Color(0.15f, 0.12f, 0.1f, 1f);
            viewGo.AddComponent<Mask>().showMaskGraphic = false;
            scroll.viewport = vrt;

            var contentGo = new GameObject("Content");
            contentGo.transform.SetParent(viewGo.transform, false);
            var crrt = contentGo.AddComponent<RectTransform>();
            crrt.anchorMin = new Vector2(0, 1);
            crrt.anchorMax = Vector2.one;
            crrt.pivot = new Vector2(0.5f, 1f);
            crrt.sizeDelta = new Vector2(0, 60);
            scroll.content = crrt;

            var itemGo = new GameObject("Item");
            itemGo.transform.SetParent(contentGo.transform, false);
            var irt = itemGo.AddComponent<RectTransform>();
            irt.sizeDelta = new Vector2(120, 30);
            itemGo.AddComponent<Toggle>();

            var itemLabelGo = new GameObject("Item Label");
            itemLabelGo.transform.SetParent(itemGo.transform, false);
            var ilrt = itemLabelGo.AddComponent<RectTransform>();
            ilrt.anchorMin = Vector2.zero;
            ilrt.anchorMax = Vector2.one;
            ilrt.offsetMin = new Vector2(8, 0);
            ilrt.offsetMax = Vector2.zero;
            var itemTmp = itemLabelGo.AddComponent<TextMeshProUGUI>();
            itemTmp.fontSize = 18;
            itemTmp.color = ThreeKingdomsTheme.TextPrimary;
            dd.itemText = itemTmp;

            templateGo.SetActive(false);
            dd.RefreshShownValue();
        }

        static void CreateHeaderCell(Transform parent, string text, float width, Color color)
        {
            var go = new GameObject("H_" + text);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(width, 30);
            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth = width;
            le.preferredHeight = 30;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 20;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Left;
            tmp.margin = new Vector4(4, 0, 4, 0);
        }

        // ============================================================
        // Save Prefab
        // ============================================================
        static void SavePrefab(GameObject go, string name)
        {
            string path = $"{PrefabFolder}/{name}.prefab";
            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            Debug.Log($"[UIPrefabGenerator] Saved: {path}");
        }

        static void EnsureFolder(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                var parts = path.Split('/');
                string current = parts[0];
                for (int i = 1; i < parts.Length; i++)
                {
                    string next = current + "/" + parts[i];
                    if (!AssetDatabase.IsValidFolder(next))
                        AssetDatabase.CreateFolder(current, parts[i]);
                    current = next;
                }
            }
        }
    }
}
