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
            AutoFindButtons();

            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                settingsDialog = canvas.GetComponentInChildren<SettingsDialog>(true);
                loadDialog = canvas.GetComponentInChildren<LoadDialog>(true);
            }

            if (startButton != null)
                startButton.onClick.AddListener(() =>
                {
                    Debug.Log("[MenuScreen] Start button clicked");
                    if (OnStartGame != null)
                        OnStartGame.Invoke();
                    else
                        Debug.LogError("[MenuScreen] OnStartGame has no subscribers!");
                });
            if (noticeButton != null)
                noticeButton.onClick.AddListener(() => OnOpenAnnouncement?.Invoke());
            if (settingsButton != null)
                settingsButton.onClick.AddListener(() => { if (settingsDialog != null) settingsDialog.Show(); });
            if (loadButton != null)
                loadButton.onClick.AddListener(() => { if (loadDialog != null) loadDialog.Show(); });

            UpdateLabels();
        }

        void AutoFindButtons()
        {
            if (startButton == null || loadButton == null || settingsButton == null || noticeButton == null)
            {
                var buttons = GetComponentsInChildren<Button>(true);
                foreach (var btn in buttons)
                {
                    var tmp = btn.GetComponentInChildren<TMP_Text>();
                    if (tmp == null) continue;
                    string text = tmp.text.Trim();
                    if (startButton == null && (text.Contains("开始") || text.Contains("Start") || text.Contains("start")))
                        startButton = btn;
                    else if (loadButton == null && (text.Contains("读取") || text.Contains("Load") || text.Contains("load")))
                        loadButton = btn;
                    else if (settingsButton == null && (text.Contains("设置") || text.Contains("Settings") || text.Contains("settings")))
                        settingsButton = btn;
                    else if (noticeButton == null && (text.Contains("公告") || text.Contains("Notice") || text.Contains("notice")))
                        noticeButton = btn;
                }
                if (startButton != null)
                    Debug.Log("[MenuScreen] Auto-found startButton");
            }
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
