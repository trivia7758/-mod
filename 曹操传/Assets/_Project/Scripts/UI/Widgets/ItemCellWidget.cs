using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CaoCao.UI
{
    /// <summary>
    /// Reusable item cell for warehouse grid and equipment item list.
    /// Shows icon, name, and quantity count.
    /// </summary>
    public class ItemCellWidget : MonoBehaviour
    {
        [SerializeField] Image icon;
        [SerializeField] TMP_Text nameLabel;
        [SerializeField] TMP_Text countLabel;
        [SerializeField] Image selectionHighlight;
        [SerializeField] Button button;

        string _itemId;

        /// <summary>
        /// Fired when this cell is clicked. Passes the itemId.
        /// </summary>
        public event Action<string> OnClicked;

        void Awake()
        {
            if (button == null)
                button = GetComponent<Button>();
            button?.onClick.AddListener(() => OnClicked?.Invoke(_itemId));
        }

        /// <summary>
        /// Configure this cell with item data.
        /// </summary>
        public void Setup(string itemId, Sprite iconSprite, string displayName, int count = 1)
        {
            _itemId = itemId;

            if (icon != null)
            {
                icon.sprite = iconSprite;
                icon.enabled = iconSprite != null;
            }

            if (nameLabel != null)
                nameLabel.text = displayName;

            if (countLabel != null)
            {
                countLabel.text = count > 1 ? $"x{count}" : "";
                countLabel.enabled = count > 1;
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
                selectionHighlight.color = new Color(0.78f, 0.68f, 0.42f, 0.5f);
            }
        }

        public string ItemId => _itemId;
    }
}
