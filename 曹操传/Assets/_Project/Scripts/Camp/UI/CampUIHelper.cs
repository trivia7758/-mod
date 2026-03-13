using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CaoCao.Core;
using CaoCao.Data;

namespace CaoCao.Camp
{
    /// <summary>
    /// Helper for building camp screen UI elements programmatically.
    /// </summary>
    public static class CampUIHelper
    {
        static Sprite _defaultPortrait;
        static Sprite _defaultUnitSprite;

        /// <summary>
        /// Create an anchored Image element for displaying portraits/sprites.
        /// </summary>
        public static Image CreateAnchoredImage(Transform parent, string name, Sprite sprite,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;

            var img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.preserveAspect = true;
            img.color = Color.white;
            return img;
        }

        /// <summary>
        /// Get hero portrait sprite, fallback to nanzhu.jpg if null.
        /// </summary>
        public static Sprite GetHeroPortrait(HeroDefinition heroDef)
        {
            if (heroDef != null && heroDef.portrait != null)
                return heroDef.portrait;

            if (_defaultPortrait == null)
                _defaultPortrait = Resources.Load<Sprite>("Portraits/nanzhu");

            return _defaultPortrait;
        }

        /// <summary>
        /// Get hero unit sprite, fallback to test_r1.png if null.
        /// </summary>
        public static Sprite GetHeroUnitSprite(HeroDefinition heroDef)
        {
            if (heroDef != null && heroDef.unitSprite != null)
                return heroDef.unitSprite;

            if (_defaultUnitSprite == null)
                _defaultUnitSprite = Resources.Load<Sprite>("Sprites/Story/test_r1");

            return _defaultUnitSprite;
        }

        /// <summary>
        /// Get item icon sprite. Priority: ItemDefinition.icon > Resources fallback by type+name.
        /// </summary>
        public static Sprite GetItemIcon(ItemDefinition itemDef)
        {
            if (itemDef != null && itemDef.icon != null)
                return itemDef.icon;

            // Fallback: try loading from Resources/Sprites/Items/ by type prefix + displayName
            if (itemDef != null)
            {
                string prefix = itemDef.itemType switch
                {
                    CaoCao.Common.ItemType.Weapon => "weapon",
                    CaoCao.Common.ItemType.Armor => "armor",
                    CaoCao.Common.ItemType.Auxiliary => "aux",
                    _ => "item"
                };
                var sprite = Resources.Load<Sprite>($"Sprites/Items/{prefix}_{itemDef.displayName}");
                if (sprite != null) return sprite;
            }

            return null;
        }

        /// <summary>
        /// Create a clickable list row with an optional icon on the left.
        /// </summary>
        public static Button CreateListRowWithIcon(Transform parent, string text, Sprite icon,
            float height = 44f, float fontSize = 14f)
        {
            var go = new GameObject("Row_" + text);
            go.transform.SetParent(parent, false);

            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = height;
            le.minHeight = height;

            var img = go.AddComponent<Image>();
            img.color = new Color(0.15f, 0.12f, 0.08f, 0.6f);

            var btn = go.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = new Color(0.15f, 0.12f, 0.08f, 0.6f);
            colors.highlightedColor = ThreeKingdomsTheme.ButtonHover;
            colors.pressedColor = ThreeKingdomsTheme.ButtonPressed;
            colors.selectedColor = ThreeKingdomsTheme.SpeedActive;
            btn.colors = colors;
            btn.targetGraphic = img;

            float textLeft = 8f;

            // Icon (if available)
            if (icon != null)
            {
                var iconGo = new GameObject("Icon");
                iconGo.transform.SetParent(go.transform, false);
                var irt = iconGo.AddComponent<RectTransform>();
                irt.anchorMin = new Vector2(0, 0.05f);
                irt.anchorMax = new Vector2(0, 0.95f);
                irt.offsetMin = new Vector2(4, 0);
                irt.offsetMax = new Vector2(4 + height - 6, 0);
                irt.sizeDelta = new Vector2(height - 6, 0);
                var iconImg = iconGo.AddComponent<Image>();
                iconImg.sprite = icon;
                iconImg.preserveAspect = true;
                textLeft = height + 2;
            }

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(go.transform, false);
            var lrt = labelGo.AddComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = new Vector2(textLeft, 0);
            lrt.offsetMax = new Vector2(-8, 0);

            var tmp = labelGo.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = ThreeKingdomsTheme.TextPrimary;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;

            return btn;
        }

        /// <summary>
        /// Create a full-screen overlay panel with semi-transparent background.
        /// </summary>
        public static RectTransform CreateOverlayPanel(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var bg = go.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.8f);

            return rt;
        }

        /// <summary>
        /// Create a vertical scroll view.
        /// Returns (scrollRect, contentTransform).
        /// </summary>
        public static (ScrollRect scroll, RectTransform content) CreateScrollView(
            Transform parent, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject("ScrollView");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;

            var sr = go.AddComponent<ScrollRect>();

            // Viewport — use RectMask2D (no Image needed, clips by rect bounds)
            var viewport = new GameObject("Viewport");
            viewport.transform.SetParent(go.transform, false);
            var vpRt = viewport.AddComponent<RectTransform>();
            vpRt.anchorMin = Vector2.zero;
            vpRt.anchorMax = Vector2.one;
            vpRt.offsetMin = Vector2.zero;
            vpRt.offsetMax = Vector2.zero;
            viewport.AddComponent<RectMask2D>();

            // Content
            var content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            var cRt = content.AddComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0, 1);
            cRt.anchorMax = new Vector2(1, 1);
            cRt.pivot = new Vector2(0.5f, 1);
            cRt.offsetMin = Vector2.zero;
            cRt.offsetMax = Vector2.zero;

            var vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 4;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.padding = new RectOffset(4, 4, 4, 4);

            var csf = content.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            sr.viewport = vpRt;
            sr.content = cRt;
            sr.horizontal = false;
            sr.vertical = true;
            sr.scrollSensitivity = 20f;

            return (sr, cRt);
        }

        /// <summary>
        /// Create a clickable list row with text and optional highlight.
        /// </summary>
        public static Button CreateListRow(Transform parent, string text, float height = 36f, float fontSize = 18f)
        {
            var go = new GameObject("Row_" + text);
            go.transform.SetParent(parent, false);

            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = height;
            le.minHeight = height;

            var img = go.AddComponent<Image>();
            img.color = new Color(0.15f, 0.12f, 0.08f, 0.6f);

            var btn = go.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = new Color(0.15f, 0.12f, 0.08f, 0.6f);
            colors.highlightedColor = ThreeKingdomsTheme.ButtonHover;
            colors.pressedColor = ThreeKingdomsTheme.ButtonPressed;
            colors.selectedColor = ThreeKingdomsTheme.SpeedActive;
            btn.colors = colors;
            btn.targetGraphic = img;

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(go.transform, false);
            var lrt = labelGo.AddComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = new Vector2(10, 0);
            lrt.offsetMax = new Vector2(-10, 0);

            var tmp = labelGo.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = ThreeKingdomsTheme.TextPrimary;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;

            return btn;
        }

        /// <summary>
        /// Highlight a row by changing its Image color.
        /// </summary>
        public static void SetRowHighlight(Button row, bool selected)
        {
            var img = row.GetComponent<Image>();
            if (img != null)
                img.color = selected
                    ? ThreeKingdomsTheme.SpeedActive
                    : new Color(0.15f, 0.12f, 0.08f, 0.6f);
        }

        /// <summary>
        /// Create a sub-panel with border.
        /// </summary>
        public static RectTransform CreateSubPanel(Transform parent, string name,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;

            var img = go.AddComponent<Image>();
            img.color = ThreeKingdomsTheme.PanelBg;

            var outline = go.AddComponent<Outline>();
            outline.effectColor = ThreeKingdomsTheme.PanelBorder;
            outline.effectDistance = new Vector2(1, -1);

            return rt;
        }

        /// <summary>
        /// Create an anchored label.
        /// </summary>
        public static TMP_Text CreateAnchoredLabel(Transform parent, string text, float fontSize,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax,
            TextAlignmentOptions align = TextAlignmentOptions.MidlineLeft)
        {
            var go = new GameObject("Lbl_" + text);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = ThreeKingdomsTheme.TextPrimary;
            tmp.alignment = align;
            return tmp;
        }

        /// <summary>
        /// Create an anchored button.
        /// </summary>
        public static Button CreateAnchoredButton(Transform parent, string text,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax,
            float fontSize = 22f)
        {
            var go = new GameObject("Btn_" + text);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;

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

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(go.transform, false);
            var lrt = labelGo.AddComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero;
            lrt.offsetMax = Vector2.zero;
            var tmp = labelGo.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = ThreeKingdomsTheme.TextPrimary;
            tmp.alignment = TextAlignmentOptions.Center;

            return btn;
        }
    }
}
