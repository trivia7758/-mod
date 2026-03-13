using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CaoCao.UI
{
    public class MoralityBar : MonoBehaviour
    {
        [SerializeField] RectTransform barRect;
        [SerializeField] Image redFill;
        [SerializeField] Image blueFill;
        [SerializeField] TMP_Text label;

        public void SetValue(int morality)
        {
            if (barRect == null || redFill == null || blueFill == null) return;

            int v = Mathf.Clamp(morality, 0, 100);
            float t = v / 100f;

            redFill.rectTransform.anchorMin = new Vector2(0, 0);
            redFill.rectTransform.anchorMax = new Vector2(t, 1);
            redFill.rectTransform.offsetMin = Vector2.zero;
            redFill.rectTransform.offsetMax = Vector2.zero;

            blueFill.rectTransform.anchorMin = new Vector2(t, 0);
            blueFill.rectTransform.anchorMax = new Vector2(1, 1);
            blueFill.rectTransform.offsetMin = Vector2.zero;
            blueFill.rectTransform.offsetMax = Vector2.zero;
        }

        public void SetLabel(string text)
        {
            if (label != null) label.text = text;
        }
    }
}
