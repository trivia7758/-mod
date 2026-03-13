using System;
using System.Collections;
using UnityEngine;
using TMPro;

namespace CaoCao.UI
{
    public class LogoScreen : BaseScreen
    {
        [SerializeField] float fadeInTime = 0.7f;
        [SerializeField] float holdTime = 0.9f;
        [SerializeField] float fadeOutTime = 0.6f;

        [SerializeField] TMP_Text logoLabel;
        [SerializeField] TMP_Text subLabel;

        public event Action OnFinished;

        public override void Initialize()
        {
            if (logoLabel != null) logoLabel.alpha = 0f;
            if (subLabel != null) subLabel.alpha = 0f;
        }

        public override void Show()
        {
            base.Show();
            StartCoroutine(PlaySequence());
        }

        IEnumerator PlaySequence()
        {
            var loc = Core.ServiceLocator.Get<Localization.LocalizationManager>();
            if (loc != null)
            {
                if (logoLabel != null) logoLabel.text = loc.Get("logo_name");
                if (subLabel != null) subLabel.text = loc.Get("logo_sub");
            }
            else
            {
                if (logoLabel != null) logoLabel.text = "曹操传";
                if (subLabel != null) subLabel.text = "MOD Framework";
            }

            yield return FadeTexts(0f, 1f, fadeInTime);
            yield return new WaitForSeconds(holdTime);
            yield return FadeTexts(1f, 0f, fadeOutTime);

            OnFinished?.Invoke();
        }

        IEnumerator FadeTexts(float from, float to, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float a = Mathf.Lerp(from, to, t);
                if (logoLabel != null) logoLabel.alpha = a;
                if (subLabel != null) subLabel.alpha = a;
                yield return null;
            }
            if (logoLabel != null) logoLabel.alpha = to;
            if (subLabel != null) subLabel.alpha = to;
        }
    }
}
