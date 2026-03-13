using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CaoCao.Core
{
    /// <summary>
    /// Visual theme matching the Godot ui_theme.tres.
    /// All colors extracted from the original Three Kingdoms style.
    /// </summary>
    public static class ThreeKingdomsTheme
    {
        // --- Colors from Godot ui_theme.tres ---

        // Panel: bg_color(0,0,0,0.72), border(0.55,0.42,0.22)
        public static readonly Color PanelBg = new(0f, 0f, 0f, 0.72f);
        public static readonly Color PanelBorder = new(0.55f, 0.42f, 0.22f, 1f);

        // Button normal: bg(0.2,0.16,0.12), border(0.55,0.42,0.22)
        public static readonly Color ButtonNormal = new(0.2f, 0.16f, 0.12f, 1f);
        // Button hover: bg(0.3,0.22,0.16)
        public static readonly Color ButtonHover = new(0.3f, 0.22f, 0.16f, 1f);
        // Button pressed: bg(0.18,0.12,0.1), border(0.5,0.38,0.2)
        public static readonly Color ButtonPressed = new(0.18f, 0.12f, 0.1f, 1f);

        // Label font_color: (0.95,0.9,0.78)
        public static readonly Color TextPrimary = new(0.95f, 0.9f, 0.78f, 1f);
        // RichTextLabel default_color: (0.95,0.95,0.95)
        public static readonly Color TextContent = new(0.95f, 0.95f, 0.95f, 1f);
        public static readonly Color TextSecondary = new(0.75f, 0.70f, 0.60f, 1f);
        public static readonly Color TextGold = new(0.75f, 0.60f, 0.35f, 1f);

        // Progress bar gold fill
        public static readonly Color ProgressFill = new(0.78f, 0.68f, 0.42f, 1f);
        public static readonly Color ProgressBg = new(0.15f, 0.12f, 0.08f, 1f);

        // Morality bar
        public static readonly Color MoralityRed = new(0.86f, 0.28f, 0.22f, 1f);
        public static readonly Color MoralityBlue = new(0.25f, 0.45f, 0.90f, 1f);

        // Battle
        public static readonly Color PlayerUnit = new(0.20f, 0.60f, 1.00f, 1f);
        public static readonly Color EnemyUnit = new(0.90f, 0.25f, 0.20f, 1f);

        // Speed button colors
        public static readonly Color SpeedActive = new(0.65f, 0.50f, 0.25f, 1f);
        public static readonly Color SpeedInactive = new(0.3f, 0.25f, 0.18f, 1f);

        // Checkbox active
        public static readonly Color CheckboxActive = new(0.45f, 0.55f, 0.75f, 1f);

        // --- Helper: Load background image from Resources ---
        public static Sprite LoadBackground(string name)
        {
            return Resources.Load<Sprite>("Backgrounds/" + name);
        }

        // --- Helper: Style a Button to match theme ---
        public static void StyleButton(Button btn, float fontSize = 24f)
        {
            var img = btn.GetComponent<Image>();
            if (img != null)
            {
                img.color = ButtonNormal;
            }

            var colors = btn.colors;
            colors.normalColor = ButtonNormal;
            colors.highlightedColor = ButtonHover;
            colors.pressedColor = ButtonPressed;
            colors.selectedColor = ButtonNormal;
            btn.colors = colors;

            var tmp = btn.GetComponentInChildren<TMP_Text>();
            if (tmp != null)
            {
                tmp.color = TextPrimary;
                tmp.fontSize = fontSize;
            }
        }

        // --- Helper: Create a themed panel with border ---
        public static RectTransform CreatePanel(Transform parent, string name,
            Vector2 position, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = position;
            rt.sizeDelta = size;

            // Use Outline component for border effect
            var img = go.AddComponent<Image>();
            img.color = PanelBg;

            var outline = go.AddComponent<Outline>();
            outline.effectColor = PanelBorder;
            outline.effectDistance = new Vector2(2, -2);

            return rt;
        }

        // --- Helper: Create themed label ---
        public static TMP_Text CreateLabel(Transform parent, string text,
            float fontSize, Vector2 pos, Vector2 size,
            TextAlignmentOptions align = TextAlignmentOptions.Left)
        {
            var go = new GameObject("Label_" + text);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = TextPrimary;
            tmp.alignment = align;
            return tmp;
        }

        // --- Helper: Create themed button ---
        public static Button CreateButton(Transform parent, string text,
            Vector2 pos, Vector2 size, float fontSize = 24f)
        {
            var go = new GameObject("Btn_" + text);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            var img = go.AddComponent<Image>();
            img.color = ButtonNormal;

            // Border outline
            var outline = go.AddComponent<Outline>();
            outline.effectColor = PanelBorder;
            outline.effectDistance = new Vector2(1, -1);

            var btn = go.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = ButtonNormal;
            colors.highlightedColor = ButtonHover;
            colors.pressedColor = ButtonPressed;
            colors.selectedColor = ButtonNormal;
            btn.colors = colors;
            btn.targetGraphic = img;

            // Label
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
            tmp.color = TextPrimary;
            tmp.alignment = TextAlignmentOptions.Center;

            return btn;
        }
    }
}
