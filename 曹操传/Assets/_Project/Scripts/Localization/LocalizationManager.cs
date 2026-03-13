using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

namespace CaoCao.Localization
{
    public class LocalizationManager
    {
        Dictionary<string, Dictionary<string, string>> _data = new();
        string _currentLang = "zh";

        public string CurrentLanguage => _currentLang;

        public void LoadFromResources(string resourcePath)
        {
            var textAsset = Resources.Load<TextAsset>(resourcePath);
            if (textAsset == null)
            {
                Debug.LogWarning($"[Localization] Could not load: {resourcePath}");
                return;
            }
            _data = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(textAsset.text);
        }

        public void SetLanguage(string lang)
        {
            _currentLang = lang;
        }

        public string Get(string key)
        {
            if (_data.TryGetValue(_currentLang, out var dict))
            {
                if (dict.TryGetValue(key, out var val))
                    return val;
            }
            // Fallback to zh
            if (_currentLang != "zh" && _data.TryGetValue("zh", out var zhDict))
            {
                if (zhDict.TryGetValue(key, out var val))
                    return val;
            }
            return key;
        }

        public Dictionary<string, string> GetAll()
        {
            if (_data.TryGetValue(_currentLang, out var dict))
                return dict;
            return new Dictionary<string, string>();
        }
    }
}
