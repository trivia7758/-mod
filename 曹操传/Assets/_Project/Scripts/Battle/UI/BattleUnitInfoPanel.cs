using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CaoCao.Battle
{
    /// <summary>
    /// Displays selected unit info panel in battle (bottom-left).
    /// Layout matches 曹操传 style:
    ///   [Portrait] | 名字   XXX
    ///   [  等级  ] | 职业   XXX
    ///              | 生命 ███████  72/ 72
    ///              | 魔力 ███████  36/ 75
    ///              | 经验值        0
    /// </summary>
    public class BattleUnitInfoPanel : MonoBehaviour
    {
        Image _portraitImage;
        TMP_Text _nameLabel, _nameValue;
        TMP_Text _classLabel, _classValue;
        TMP_Text _levelLabel, _levelValue;
        Image _hpBarFill;
        TMP_Text _hpLabel, _hpValue;
        Image _mpBarFill;
        TMP_Text _mpLabel, _mpValue;
        GameObject _mpRow;
        TMP_Text _expLabel, _expValue;

        RectTransform _panel;
        bool _built;

        // Sizes
        const float PanelW = 320f;
        const float PanelH = 140f;
        const float PortraitSize = 72f;
        const float BarW = 100f;
        const float BarH = 12f;
        const float RowH = 22f;

        static readonly Color GoldLabel = new(0.8f, 0.65f, 0.3f);
        static readonly Color DarkBg = new(0.07f, 0.05f, 0.1f, 0.93f);
        static readonly Color BarBg = new(0.2f, 0.15f, 0.12f, 1f);
        static readonly Color BorderGold = new(0.55f, 0.42f, 0.18f, 0.9f);

        void Awake()
        {
            EnsureBuilt();
            Hide();
        }

        public void Show(BattleUnit unit)
        {
            if (unit == null) { Hide(); return; }
            EnsureBuilt();
            _panel.gameObject.SetActive(true);

            // Name
            _nameValue.text = !string.IsNullOrEmpty(unit.displayName) ? unit.displayName : unit.gameObject.name;
            // Class
            _classValue.text = !string.IsNullOrEmpty(unit.unitTypeName) ? unit.unitTypeName : "---";
            // Level
            _levelValue.text = unit.level.ToString();

            // Portrait — use assigned portrait, or fall back to idle-down battle sprite
            if (unit.portrait != null)
            {
                _portraitImage.sprite = unit.portrait;
                _portraitImage.color = Color.white;
            }
            else
            {
                // Use the idle-down sprite from battle sheet as temporary portrait
                var idleSprite = BattleSpriteSheet.IdleDown;
                if (idleSprite != null)
                {
                    _portraitImage.sprite = idleSprite;
                    _portraitImage.color = unit.team == UnitTeam.Player
                        ? Color.white : new Color(1f, 0.75f, 0.75f);
                }
                else
                {
                    _portraitImage.sprite = null;
                    _portraitImage.color = unit.team == UnitTeam.Player
                        ? new Color(0.15f, 0.3f, 0.55f) : new Color(0.55f, 0.15f, 0.15f);
                }
            }

            // HP
            float hpRatio = unit.maxHp > 0 ? (float)unit.hp / unit.maxHp : 0;
            _hpBarFill.fillAmount = hpRatio;
            _hpBarFill.color = hpRatio > 0.5f ? new Color(0.85f, 0.15f, 0.1f)
                             : hpRatio > 0.25f ? new Color(0.9f, 0.7f, 0.1f)
                             : new Color(0.4f, 0.1f, 0.1f);
            _hpValue.text = $"<color=#FFFFFF>{unit.hp}</color><color=#AAAAAA>/</color> {unit.maxHp}";

            // MP — always show
            _mpRow.SetActive(true);
            float mpRatio = unit.maxMp > 0 ? (float)unit.mp / unit.maxMp : 0f;
            _mpBarFill.fillAmount = mpRatio;
            _mpValue.text = $"<color=#FFFFFF>{unit.mp}</color><color=#AAAAAA>/</color> {unit.maxMp}";

            // EXP
            _expValue.text = unit.exp.ToString();
        }

        public void Hide()
        {
            if (_panel != null)
                _panel.gameObject.SetActive(false);
        }

        // ============================================================
        void EnsureBuilt()
        {
            if (_built) return;
            _built = true;

            _panel = GetComponent<RectTransform>();
            if (_panel == null)
                _panel = gameObject.AddComponent<RectTransform>();

            // Panel background
            var bg = gameObject.AddComponent<Image>();
            bg.color = DarkBg;

            // Anchor bottom-left
            _panel.anchorMin = Vector2.zero;
            _panel.anchorMax = Vector2.zero;
            _panel.pivot = Vector2.zero;
            _panel.anchoredPosition = new Vector2(8, 8);
            _panel.sizeDelta = new Vector2(PanelW, PanelH);

            // Gold border
            var outline = gameObject.AddComponent<Outline>();
            outline.effectColor = BorderGold;
            outline.effectDistance = new Vector2(2, 2);

            // === LEFT: Portrait ===
            float px = 8, py = PanelH - 8;
            var portraitGo = MakeChild("Portrait", _panel);
            SetRect(portraitGo, px, py - PortraitSize, PortraitSize, PortraitSize);
            _portraitImage = portraitGo.AddComponent<Image>();
            _portraitImage.preserveAspect = true;
            var pOutline = portraitGo.AddComponent<Outline>();
            pOutline.effectColor = BorderGold;
            pOutline.effectDistance = new Vector2(1, 1);

            // Level badge below portrait
            float badgeW = PortraitSize;
            float badgeH = 22;
            var badgeGo = MakeChild("LvlBadge", _panel);
            SetRect(badgeGo, px, py - PortraitSize - 2 - badgeH, badgeW, badgeH);
            badgeGo.AddComponent<Image>().color = new Color(0.12f, 0.1f, 0.07f, 0.95f);

            var lvlIcon = MakeText("等级", badgeGo.GetComponent<RectTransform>(), 11, GoldLabel, TextAlignmentOptions.Left);
            SetRect(lvlIcon.gameObject, 3, 0, badgeW * 0.55f, badgeH);
            _levelValue = MakeText("1", badgeGo.GetComponent<RectTransform>(), 15, Color.white, TextAlignmentOptions.Center);
            _levelValue.fontStyle = FontStyles.Bold;
            SetRect(_levelValue.gameObject, badgeW * 0.5f, 0, badgeW * 0.5f - 2, badgeH);

            // === RIGHT: Info rows ===
            float rx = px + PortraitSize + 10; // right column x
            float rw = PanelW - rx - 8;        // right column width
            float labelW = 42;                  // "名字" label width
            float valX = rx + labelW + 4;       // value start x
            float valW = rw - labelW - 4;       // value width
            float curY = py;                    // current y (top)

            // Row: 名字
            MakeLabel("名字", rx, curY, labelW);
            _nameValue = MakeVal("", valX, curY, valW, 16);
            curY -= RowH;

            // Row: 职业
            MakeLabel("职业", rx, curY, labelW);
            _classValue = MakeVal("", valX, curY, valW, 16);
            curY -= RowH + 2;

            // Row: 生命 bar + value
            float barLabelW = 36;
            float barValW = 60;
            float barX = rx + barLabelW + 4;
            float actualBarW = rw - barLabelW - barValW - 12;
            if (actualBarW < 60) actualBarW = 60;

            MakeLabel("生命", rx, curY, barLabelW);
            _hpBarFill = MakeBar("HP", barX, curY, actualBarW, BarH,
                new Color(0.85f, 0.15f, 0.1f));
            _hpValue = MakeVal("", barX + actualBarW + 6, curY, barValW, 13);
            _hpValue.alignment = TextAlignmentOptions.Right;
            _hpValue.richText = true;
            curY -= RowH;

            // Row: 魔力 bar + value
            var mpRowGo = MakeChild("MPRow", _panel);
            SetRect(mpRowGo, 0, 0, PanelW, PanelH); // full size, just a grouping container
            mpRowGo.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            mpRowGo.GetComponent<RectTransform>().anchorMax = Vector2.one;
            mpRowGo.GetComponent<RectTransform>().offsetMin = Vector2.zero;
            mpRowGo.GetComponent<RectTransform>().offsetMax = Vector2.zero;
            _mpRow = mpRowGo;

            MakeLabel("魔力", rx, curY, barLabelW, mpRowGo.GetComponent<RectTransform>());
            _mpBarFill = MakeBar("MP", barX, curY, actualBarW, BarH,
                new Color(0.2f, 0.4f, 0.85f), mpRowGo.GetComponent<RectTransform>());
            _mpValue = MakeVal("", barX + actualBarW + 6, curY, barValW, 13,
                mpRowGo.GetComponent<RectTransform>());
            _mpValue.alignment = TextAlignmentOptions.Right;
            _mpValue.richText = true;
            curY -= RowH;

            // Row: 经验值
            MakeLabel("经验值", rx, curY, 50);
            _expValue = MakeVal("0", rx + 54, curY, valW - 50, 14);
        }

        // === Helpers ===

        TMP_Text MakeLabel(string text, float x, float y, float w, RectTransform parent = null)
        {
            var go = MakeChild($"L_{text}", parent ?? _panel);
            var t = go.AddComponent<TextMeshProUGUI>();
            t.text = text;
            t.fontSize = 14;
            t.color = GoldLabel;
            t.alignment = TextAlignmentOptions.Left;
            t.enableWordWrapping = false;
            t.overflowMode = TextOverflowModes.Overflow;
            SetAnchorTopLeft(go.GetComponent<RectTransform>(), x, PanelH - y, w, RowH);
            return t;
        }

        TMP_Text MakeVal(string text, float x, float y, float w, float fontSize, RectTransform parent = null)
        {
            var go = MakeChild($"V_{text}", parent ?? _panel);
            var t = go.AddComponent<TextMeshProUGUI>();
            t.text = text;
            t.fontSize = fontSize;
            t.fontStyle = FontStyles.Bold;
            t.color = Color.white;
            t.alignment = TextAlignmentOptions.Left;
            t.enableWordWrapping = false;
            t.overflowMode = TextOverflowModes.Overflow;
            SetAnchorTopLeft(go.GetComponent<RectTransform>(), x, PanelH - y, w, RowH);
            return t;
        }

        Image MakeBar(string name, float x, float y, float w, float h, Color fillColor, RectTransform parent = null)
        {
            // Background
            var barGo = MakeChild($"Bar_{name}", parent ?? _panel);
            barGo.AddComponent<Image>().color = BarBg;
            SetAnchorTopLeft(barGo.GetComponent<RectTransform>(), x, PanelH - y + (RowH - h) * 0.5f, w, h);

            // Fill
            var fillGo = MakeChild("Fill", barGo.GetComponent<RectTransform>());
            var fill = fillGo.AddComponent<Image>();
            fill.color = fillColor;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillAmount = 1f;
            var fr = fillGo.GetComponent<RectTransform>();
            fr.anchorMin = Vector2.zero;
            fr.anchorMax = Vector2.one;
            fr.offsetMin = Vector2.zero;
            fr.offsetMax = Vector2.zero;

            return fill;
        }

        static void SetAnchorTopLeft(RectTransform rt, float x, float fromTop, float w, float h)
        {
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(x, -fromTop);
            rt.sizeDelta = new Vector2(w, h);
        }

        static void SetRect(GameObject go, float x, float y, float w, float h)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(0, 0);
            rt.pivot = new Vector2(0, 0);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(w, h);
        }

        static TMP_Text MakeText(string text, RectTransform parent, float size, Color color, TextAlignmentOptions align)
        {
            var go = new GameObject(text, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<TextMeshProUGUI>();
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.enableWordWrapping = false;
            return t;
        }

        static GameObject MakeChild(string name, RectTransform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }
    }
}
