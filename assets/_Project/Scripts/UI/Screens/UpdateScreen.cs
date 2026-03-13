using System;
using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using CaoCao.Core;

namespace CaoCao.UI
{
    public class UpdateScreen : BaseScreen
    {
        [SerializeField] float checkDuration = 1.2f;
        [SerializeField] float updateDuration = 2.0f;

        [SerializeField] TMP_Text statusLabel;
        [SerializeField] Image progressFill;
        [SerializeField] TMP_Text progressText;

        public event Action<bool> OnUpdateCompleted;

        public override void Initialize() { }

        public void StartCheck()
        {
            StartCoroutine(CheckRoutine());
        }

        IEnumerator CheckRoutine()
        {
            var loc = ServiceLocator.Get<Localization.LocalizationManager>();
            if (statusLabel != null && loc != null)
                statusLabel.text = loc.Get("update_checking");

            yield return AnimateProgress(0f, 0.5f, checkDuration);

            var save = ServiceLocator.Get<SaveSystem>();
            bool hasUpdate = false;
            if (save != null)
            {
                var vm = new VersionManager(save);
                hasUpdate = vm.CheckForUpdate();
            }

            if (hasUpdate)
            {
                if (statusLabel != null && loc != null)
                    statusLabel.text = loc.Get("update_updating");
                yield return AnimateProgress(0.5f, 1f, updateDuration);
            }
            else
            {
                yield return AnimateProgress(0.5f, 1f, 0.3f);
            }

            OnUpdateCompleted?.Invoke(hasUpdate);
        }

        IEnumerator AnimateProgress(float from, float to, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float val = Mathf.Lerp(from, to, elapsed / duration);
                SetProgress(val);
                yield return null;
            }
            SetProgress(to);
        }

        void SetProgress(float value)
        {
            if (progressFill != null)
            {
                var rt = progressFill.rectTransform;
                rt.anchorMax = new Vector2(value, 1);
            }
            if (progressText != null)
                progressText.text = Mathf.RoundToInt(value * 100) + "%";
        }
    }
}
