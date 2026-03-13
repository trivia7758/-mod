using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CaoCao.UI
{
    /// <summary>
    /// Reusable stat bar widget for displaying HP, MP, Exp, etc.
    /// Shows a filled bar with current/max text label.
    /// </summary>
    public class StatBar : MonoBehaviour
    {
        [SerializeField] Image fill;
        [SerializeField] TMP_Text valueLabel;
        [SerializeField] TMP_Text nameLabel;
        [SerializeField] Color fillColor = new(0.78f, 0.68f, 0.42f, 1f); // gold

        /// <summary>
        /// Set the stat bar value and update visuals.
        /// </summary>
        public void SetValue(int current, int max, string statName = "")
        {
            if (fill != null)
            {
                fill.fillAmount = max > 0 ? (float)current / max : 0f;
                fill.color = fillColor;
            }

            if (valueLabel != null)
            {
                valueLabel.text = $"{current}/{max}";
            }

            if (nameLabel != null && !string.IsNullOrEmpty(statName))
            {
                nameLabel.text = statName;
            }
        }

        /// <summary>
        /// Set just the fill color.
        /// </summary>
        public void SetColor(Color color)
        {
            fillColor = color;
            if (fill != null)
                fill.color = fillColor;
        }
    }
}
