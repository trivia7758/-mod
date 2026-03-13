using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CaoCao.Story
{
    public class DialogueBox : MonoBehaviour
    {
        [Header("Text")]
        [SerializeField] TMP_Text textLabel;
        [SerializeField] TMP_Text nameLabel;
        [SerializeField] float charsPerSec = 40f;

        [Header("Name Bar")]
        [SerializeField] GameObject nameBar;

        [Header("Portraits")]
        [SerializeField] GameObject portraitLeftFrame;
        [SerializeField] Image portraitLeftImage;
        [SerializeField] GameObject portraitRightFrame;
        [SerializeField] Image portraitRightImage;

        [Header("Layout")]
        [SerializeField] RectTransform dialogueRect;

        public event Action OnFinished;

        bool _typing;
        bool _confirmRequested;
        CanvasGroup _canvasGroup;

        // Store default layout for restoring from center mode
        Vector2 _defaultAnchorMin;
        Vector2 _defaultAnchorMax;
        Vector2 _defaultOffsetMin;
        Vector2 _defaultOffsetMax;

        void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (dialogueRect == null)
                dialogueRect = GetComponent<RectTransform>();

            _defaultAnchorMin = dialogueRect.anchorMin;
            _defaultAnchorMax = dialogueRect.anchorMax;
            _defaultOffsetMin = dialogueRect.offsetMin;
            _defaultOffsetMax = dialogueRect.offsetMax;

            // Auto-find portrait references if not assigned in Inspector
            if (portraitLeftFrame == null)
                portraitLeftFrame = transform.Find("PortraitLeft")?.gameObject;
            if (portraitLeftImage == null)
            {
                var t = transform.Find("PortraitLeft/PortraitLeftImage");
                if (t != null) portraitLeftImage = t.GetComponent<Image>();
            }
            if (portraitRightFrame == null)
                portraitRightFrame = transform.Find("PortraitRight")?.gameObject;
            if (portraitRightImage == null)
            {
                var t = transform.Find("PortraitRight/PortraitRightImage");
                if (t != null) portraitRightImage = t.GetComponent<Image>();
            }
        }

        public IEnumerator ShowLine(string text, string speakerName, Sprite portrait, DialogueSide side)
        {
            gameObject.SetActive(true);
            _confirmRequested = false;

            SetNameAndPortrait(speakerName, portrait, side);

            if (textLabel == null)
            {
                // Wait for confirm then return
                yield return WaitConfirm();
                OnFinished?.Invoke();
                yield break;
            }

            textLabel.text = text;
            textLabel.maxVisibleCharacters = 0;
            int totalChars = textLabel.textInfo.characterCount;
            // Force mesh update to get accurate count
            textLabel.ForceMeshUpdate();
            totalChars = textLabel.textInfo.characterCount;

            // Typewriter
            _typing = true;
            float accum = 0f;
            int visible = 0;

            while (_typing)
            {
                accum += charsPerSec * Time.deltaTime;
                int add = Mathf.FloorToInt(accum);
                if (add > 0)
                {
                    accum -= add;
                    visible = Mathf.Min(totalChars, visible + add);
                    textLabel.maxVisibleCharacters = visible;
                }
                if (visible >= totalChars)
                    _typing = false;
                yield return null;
            }
            textLabel.maxVisibleCharacters = totalChars;

            // Wait for confirm
            yield return WaitConfirm();

            OnFinished?.Invoke();
        }

        IEnumerator WaitConfirm()
        {
            _confirmRequested = false;
            while (!_confirmRequested)
                yield return null;
            _confirmRequested = false;
        }

        void Update()
        {
            if (!gameObject.activeSelf) return;

            var input = CaoCao.Input.InputManager.Instance;
            if (input == null) return;

            if (input.ConfirmDown || input.CancelDown)
            {
                if (_typing)
                {
                    // Skip to end of text
                    _typing = false;
                }
                else
                {
                    _confirmRequested = true;
                }
            }
        }

        public void RequestConfirm()
        {
            _confirmRequested = true;
        }

        public void SetCharsPerSec(float cps)
        {
            charsPerSec = cps;
        }

        void SetNameAndPortrait(string name, Sprite portrait, DialogueSide side)
        {
            if (side == DialogueSide.Center)
            {
                ApplyCenterLayout(true);
                if (nameLabel != null) { nameLabel.text = ""; nameLabel.gameObject.SetActive(false); }
                if (nameBar != null) nameBar.SetActive(false);
                if (portraitLeftFrame != null) portraitLeftFrame.SetActive(false);
                if (portraitRightFrame != null) portraitRightFrame.SetActive(false);
                if (textLabel != null) textLabel.alignment = TextAlignmentOptions.Center;
                return;
            }

            ApplyCenterLayout(false);
            if (nameLabel != null)
            {
                nameLabel.text = name;
                nameLabel.gameObject.SetActive(true);
            }
            if (nameBar != null) nameBar.SetActive(true);
            if (portraitLeftFrame != null) portraitLeftFrame.SetActive(false);
            if (portraitRightFrame != null) portraitRightFrame.SetActive(false);

            if (portrait != null)
            {
                if (side == DialogueSide.Right)
                {
                    if (portraitRightFrame != null) portraitRightFrame.SetActive(true);
                    if (portraitRightImage != null)
                    {
                        portraitRightImage.sprite = portrait;
                        portraitRightImage.color = Color.white;
                        portraitRightImage.preserveAspect = true;
                    }
                }
                else
                {
                    if (portraitLeftFrame != null) portraitLeftFrame.SetActive(true);
                    if (portraitLeftImage != null)
                    {
                        portraitLeftImage.sprite = portrait;
                        portraitLeftImage.color = Color.white;
                        portraitLeftImage.preserveAspect = true;
                    }
                }
            }

            if (textLabel != null) textLabel.alignment = TextAlignmentOptions.TopLeft;
        }

        void ApplyCenterLayout(bool center)
        {
            if (dialogueRect == null) return;
            if (center)
            {
                dialogueRect.anchorMin = new Vector2(0.5f, 0.5f);
                dialogueRect.anchorMax = new Vector2(0.5f, 0.5f);
                dialogueRect.offsetMin = new Vector2(-420, -60);
                dialogueRect.offsetMax = new Vector2(420, 60);
            }
            else
            {
                dialogueRect.anchorMin = _defaultAnchorMin;
                dialogueRect.anchorMax = _defaultAnchorMax;
                dialogueRect.offsetMin = _defaultOffsetMin;
                dialogueRect.offsetMax = _defaultOffsetMax;
            }
        }
    }
}
