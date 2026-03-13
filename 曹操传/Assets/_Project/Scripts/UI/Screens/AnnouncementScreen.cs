using System;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using CaoCao.Core;

namespace CaoCao.UI
{
    public class AnnouncementScreen : BaseScreen
    {
        [SerializeField] TMP_Text bodyText;
        [SerializeField] Button closeButton;

        public event Action OnClosed;

        public override void Initialize()
        {
            if (closeButton != null)
                closeButton.onClick.AddListener(() => OnClosed?.Invoke());
            Hide();
        }

        public override void Show()
        {
            base.Show();
            LoadContent();
        }

        void LoadContent()
        {
            var loc = ServiceLocator.Get<Localization.LocalizationManager>();
            string lang = loc?.CurrentLanguage ?? "zh";

            string filename = lang == "en" ? "announcement_en" : "announcement";
            var textAsset = Resources.Load<TextAsset>("Data/" + filename);
            if (textAsset != null && bodyText != null)
            {
                bodyText.text = textAsset.text;
            }
            else if (bodyText != null)
            {
                bodyText.text = "暂无公告。\nNo announcements.";
            }
        }
    }
}
