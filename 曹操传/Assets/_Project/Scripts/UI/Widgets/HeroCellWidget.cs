using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CaoCao.UI
{
    /// <summary>
    /// Reusable hero portrait cell for deployment grid and hero list.
    /// Shows portrait, name, selection highlight, and required border.
    /// </summary>
    public class HeroCellWidget : MonoBehaviour
    {
        [SerializeField] Image portrait;
        [SerializeField] TMP_Text nameLabel;
        [SerializeField] Image selectionHighlight;
        [SerializeField] Image requiredBorder;
        [SerializeField] Button button;

        string _heroId;
        bool _isRequired;

        /// <summary>
        /// Fired when this cell is clicked. Passes the heroId.
        /// </summary>
        public event Action<string> OnClicked;

        void Awake()
        {
            if (button == null)
                button = GetComponent<Button>();
            button?.onClick.AddListener(() => OnClicked?.Invoke(_heroId));
        }

        /// <summary>
        /// Configure this cell with hero data.
        /// </summary>
        public void Setup(string heroId, Sprite portraitSprite, string displayName, bool required = false)
        {
            _heroId = heroId;
            _isRequired = required;

            if (portrait != null)
            {
                portrait.sprite = portraitSprite;
                portrait.enabled = portraitSprite != null;
            }

            if (nameLabel != null)
                nameLabel.text = displayName;

            if (requiredBorder != null)
            {
                requiredBorder.enabled = required;
                requiredBorder.color = new Color(0.86f, 0.28f, 0.22f, 1f); // red
            }

            SetSelected(false);
        }

        /// <summary>
        /// Toggle selection highlight.
        /// </summary>
        public void SetSelected(bool selected)
        {
            if (selectionHighlight != null)
            {
                selectionHighlight.enabled = selected;
                selectionHighlight.color = new Color(0.78f, 0.68f, 0.42f, 0.5f); // gold semi-transparent
            }
        }

        public string HeroId => _heroId;
        public bool IsRequired => _isRequired;
    }
}
