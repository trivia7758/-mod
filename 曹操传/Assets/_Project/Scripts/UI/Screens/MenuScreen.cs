using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CaoCao.Core;

namespace CaoCao.UI
{
    public class MenuScreen : BaseScreen
    {
        [SerializeField] Button startButton;
        [SerializeField] Button loadButton;
        [SerializeField] Button settingsButton;
        [SerializeField] Button noticeButton;

        public event Action OnStartGame;
        public event Action OnOpenAnnouncement;

        SettingsDialog settingsDialog;
        LoadDialog loadDialog;

        public override void Initialize()
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                settingsDialog = canvas.GetComponentInChildren<SettingsDialog>(true);
                loadDialog = canvas.GetComponentInChildren<LoadDialog>(true);
            }

            if (startButton != null)
                startButton.onClick.AddListener(() => OnStartGame?.Invoke());
            if (noticeButton != null)
                noticeButton.onClick.AddListener(() => OnOpenAnnouncement?.Invoke());
            if (settingsButton != null)
                settingsButton.onClick.AddListener(() => { if (settingsDialog != null) settingsDialog.Show(); });
            if (loadButton != null)
                loadButton.onClick.AddListener(() => { if (loadDialog != null) loadDialog.Show(); });

            UpdateLabels();
        }

        void UpdateLabels()
        {
            var loc = ServiceLocator.Get<Localization.LocalizationManager>();
            if (loc == null) return;

            SetButtonText(startButton, loc.Get("menu_start"));
            SetButtonText(loadButton, loc.Get("menu_load"));
            SetButtonText(settingsButton, loc.Get("menu_settings"));
            SetButtonText(noticeButton, loc.Get("menu_notice"));
        }

        static void SetButtonText(Button btn, string text)
        {
            if (btn == null) return;
            var tmp = btn.GetComponentInChildren<TMP_Text>();
            if (tmp != null) tmp.text = text;
        }
    }
}
