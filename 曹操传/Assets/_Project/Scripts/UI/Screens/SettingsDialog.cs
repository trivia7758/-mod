using UnityEngine;
using TMPro;
using UnityEngine.UI;
using CaoCao.Core;

namespace CaoCao.UI
{
    public class SettingsDialog : BaseScreen
    {
        [Header("Audio")]
        [SerializeField] Toggle bgmToggle;
        [SerializeField] Toggle sfxToggle;

        [Header("Display")]
        [SerializeField] Toggle showHpToggle;
        [SerializeField] Toggle longPressToggle;
        [SerializeField] Toggle autoMinimapToggle;
        [SerializeField] Toggle dialogHoldToggle;
        [SerializeField] Toggle statusChangeToggle;
        [SerializeField] Toggle critLineToggle;

        [Header("Speed")]
        [SerializeField] Button[] msgSpeedBtns = new Button[3];
        [SerializeField] Button[] moveSpeedBtns = new Button[3];

        [Header("Language")]
        [SerializeField] Toggle autoPlayToggle;
        [SerializeField] TMP_Dropdown languageDropdown;

        [Header("UI")]
        [SerializeField] Button closeButton;

        int currentMsgSpeed = 0;
        int currentMoveSpeed = 0;

        public override void Initialize()
        {
            if (closeButton != null)
                closeButton.onClick.AddListener(Hide);

            for (int i = 0; i < 3; i++)
            {
                int idx = i;
                if (i < msgSpeedBtns.Length && msgSpeedBtns[i] != null)
                    msgSpeedBtns[i].onClick.AddListener(() => SetMsgSpeed(idx));
                if (i < moveSpeedBtns.Length && moveSpeedBtns[i] != null)
                    moveSpeedBtns[i].onClick.AddListener(() => SetMoveSpeed(idx));
            }

            if (languageDropdown != null)
                languageDropdown.onValueChanged.AddListener(OnLanguageChanged);
            if (autoPlayToggle != null)
                autoPlayToggle.onValueChanged.AddListener(OnAutoPlayChanged);

            Hide();
        }

        public override void Show()
        {
            base.Show();
            SyncFromSettings();
        }

        void SyncFromSettings()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.Settings == null) return;

            if (languageDropdown != null)
                languageDropdown.SetValueWithoutNotify(gm.Settings.language == "en" ? 1 : 0);
            if (autoPlayToggle != null)
                autoPlayToggle.SetIsOnWithoutNotify(gm.Settings.autoPlay);

            SetMsgSpeed((int)gm.Settings.messageSpeed);
            SetMoveSpeed((int)gm.Settings.moveSpeed);
        }

        void SetMsgSpeed(int idx)
        {
            currentMsgSpeed = idx;
            for (int i = 0; i < msgSpeedBtns.Length; i++)
            {
                if (msgSpeedBtns[i] == null) continue;
                var img = msgSpeedBtns[i].GetComponent<Image>();
                if (img != null) img.color = i == idx ?
                    ThreeKingdomsTheme.SpeedActive : ThreeKingdomsTheme.SpeedInactive;
            }
            if (GameManager.Instance?.Settings != null)
                GameManager.Instance.Settings.messageSpeed = (SpeedSetting)idx;
        }

        void SetMoveSpeed(int idx)
        {
            currentMoveSpeed = idx;
            for (int i = 0; i < moveSpeedBtns.Length; i++)
            {
                if (moveSpeedBtns[i] == null) continue;
                var img = moveSpeedBtns[i].GetComponent<Image>();
                if (img != null) img.color = i == idx ?
                    ThreeKingdomsTheme.SpeedActive : ThreeKingdomsTheme.SpeedInactive;
            }
            if (GameManager.Instance?.Settings != null)
                GameManager.Instance.Settings.moveSpeed = (SpeedSetting)idx;
        }

        void OnLanguageChanged(int index)
        {
            GameManager.Instance?.SetLanguage(index == 1 ? "en" : "zh");
        }

        void OnAutoPlayChanged(bool value)
        {
            if (GameManager.Instance?.Settings != null)
                GameManager.Instance.Settings.autoPlay = value;
        }
    }
}
