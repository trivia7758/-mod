using UnityEngine;
using TMPro;
using CaoCao.Core;

namespace CaoCao.Localization
{
    [RequireComponent(typeof(TMP_Text))]
    public class LocalizedText : MonoBehaviour
    {
        [SerializeField] string localizationKey;

        TMP_Text _text;

        void Awake()
        {
            _text = GetComponent<TMP_Text>();
        }

        void OnEnable()
        {
            EventBus.OnLanguageChanged += OnLanguageChanged;
            UpdateText();
        }

        void OnDisable()
        {
            EventBus.OnLanguageChanged -= OnLanguageChanged;
        }

        void OnLanguageChanged(string lang)
        {
            UpdateText();
        }

        void UpdateText()
        {
            var loc = ServiceLocator.Get<LocalizationManager>();
            if (loc != null && !string.IsNullOrEmpty(localizationKey))
                _text.text = loc.Get(localizationKey);
        }

        public void SetKey(string key)
        {
            localizationKey = key;
            UpdateText();
        }
    }
}
