using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CaoCao.Story
{
    public class ChoicePanel : MonoBehaviour
    {
        [SerializeField] Transform choiceContainer;
        [SerializeField] GameObject choiceButtonPrefab;

        StoryChoiceOption _selectedOption;

        public IEnumerator ShowChoices(List<StoryChoiceOption> options, Action<StoryChoiceOption> onChosen)
        {
            gameObject.SetActive(true);
            _selectedOption = null;

            if (choiceContainer == null) { Debug.LogWarning("[ChoicePanel] choiceContainer not assigned"); yield break; }

            // Clear existing buttons
            foreach (Transform child in choiceContainer)
                Destroy(child.gameObject);

            // Create buttons
            foreach (var opt in options)
            {
                var btnGo = Instantiate(choiceButtonPrefab, choiceContainer);
                btnGo.SetActive(true);
                var tmp = btnGo.GetComponentInChildren<TMP_Text>();
                if (tmp != null) tmp.text = opt.text;
                var btn = btnGo.GetComponent<Button>();
                var captured = opt;
                btn.onClick.AddListener(() => _selectedOption = captured);
            }

            // Wait for selection
            while (_selectedOption == null)
                yield return null;

            gameObject.SetActive(false);
            onChosen?.Invoke(_selectedOption);
        }
    }
}
