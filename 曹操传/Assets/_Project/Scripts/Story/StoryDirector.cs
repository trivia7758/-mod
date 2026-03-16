using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CaoCao.Camp;
using CaoCao.Core;
using CaoCao.UI;

namespace CaoCao.Story
{
    /// <summary>
    /// Main runtime executor for the visual story editor system.
    /// Replaces StorySceneController. Reads actions from StorySceneData
    /// and executes them sequentially via coroutines.
    /// </summary>
    public class StoryDirector : MonoBehaviour
    {
        [Header("Story Data")]
        [SerializeField] StorySceneData sceneData;

        [Header("UI")]
        [SerializeField] DialogueBox dialogueBox;
        [SerializeField] ChoicePanel choicePanel;
        [SerializeField] SpriteRenderer background;
        [SerializeField] TMP_Text locationLabel;
        [SerializeField] MoralityBar moralityBar;

        [Header("Scene")]
        [Tooltip("Parent transform for actor GameObjects (e.g., UnitContainer)")]
        [SerializeField] Transform actorParent;

        StoryContext _context;
        int _morality = 50;
        int _actionIndex;
        Dictionary<string, int> _labels = new();
        bool _gotoRequested;
        string _gotoTarget;

        // Skip story support
        Coroutine _runCoroutine;
        bool _skipRequested;
        GameObject _skipButtonGo;

        // Public API for actions to use
        public StoryContext Context => _context;
        public DialogueBox DialogueBox => dialogueBox;
        public ChoicePanel ChoicePanel => choicePanel;

        void Start()
        {
            AutoFindReferences();
            PositionCamera();
            SetupMorality();

            // Check if loaded from save — skip story and go directly to camp
            var gsm = ServiceLocator.Get<GameStateManager>();
            if (gsm != null && gsm.WasLoadedFromSave)
            {
                Debug.Log("[StoryDirector] Loaded from save — skipping story, showing camp");
                gsm.WasLoadedFromSave = false;

                // Restore visuals from story data
                RestoreLastBackground();
                RestoreLastLocation();
                if (sceneData != null)
                {
                    BuildLabelMap();
                    SetupActors();
                    RestoreActorPositions();
                }
                HideDialogue();

                // Ensure CampManager exists
                if (FindAnyObjectByType<CampManager>() == null)
                {
                    var go = new GameObject("CampManager");
                    go.AddComponent<CampManager>();
                }

                EventBus.StoryCompleted();
                return;
            }

            if (sceneData == null)
            {
                Debug.LogWarning("[StoryDirector] No StorySceneData assigned — skipping to camp");
                // Ensure CampManager exists
                if (FindAnyObjectByType<CampManager>() == null)
                {
                    var go = new GameObject("CampManager");
                    go.AddComponent<CampManager>();
                }
                EventBus.StoryCompleted();
                return;
            }

            BuildLabelMap();
            SetupActors();
            CreateSkipButton();
            _runCoroutine = StartCoroutine(RunActions());
        }

        void AutoFindReferences()
        {
            if (dialogueBox == null)
                dialogueBox = FindAnyObjectByType<DialogueBox>();
            if (choicePanel == null)
                choicePanel = FindAnyObjectByType<ChoicePanel>();
            if (background == null)
            {
                var bgGo = GameObject.Find("Background");
                if (bgGo != null) background = bgGo.GetComponent<SpriteRenderer>();
            }
            if (locationLabel == null)
            {
                var topBar = GameObject.Find("TopBar");
                if (topBar != null)
                {
                    var locTf = topBar.transform.Find("LocationLabel");
                    if (locTf != null) locationLabel = locTf.GetComponent<TMP_Text>();
                }
                if (locationLabel == null)
                {
                    var locGo = GameObject.Find("LocationLabel");
                    if (locGo != null) locationLabel = locGo.GetComponent<TMP_Text>();
                }
            }
            if (moralityBar == null)
                moralityBar = FindAnyObjectByType<MoralityBar>();
            if (actorParent == null)
            {
                var container = GameObject.Find("UnitContainer");
                if (container != null) actorParent = container.transform;
            }
        }

        // ── Skip Story ──

        void CreateSkipButton()
        {
            // Always create a dedicated canvas for the skip button
            // (reusing FindAnyObjectByType<Canvas>() can find the FadeOverlay canvas
            //  which has CanvasGroup alpha=0, making the button invisible)
            var canvasGo = new GameObject("SkipButtonCanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            _skipButtonGo = new GameObject("SkipStoryBtn");
            _skipButtonGo.transform.SetParent(canvas.transform, false);

            var rt = _skipButtonGo.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.01f, 0.93f);
            rt.anchorMax = new Vector2(0.14f, 1f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var img = _skipButtonGo.AddComponent<Image>();
            img.color = new Color(0.3f, 0.22f, 0.12f, 0.75f);

            var outline = _skipButtonGo.AddComponent<Outline>();
            outline.effectColor = new Color(0.6f, 0.45f, 0.2f, 0.8f);
            outline.effectDistance = new Vector2(1, -1);

            var btn = _skipButtonGo.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = new Color(0.3f, 0.22f, 0.12f, 0.75f);
            colors.highlightedColor = new Color(0.45f, 0.35f, 0.2f, 0.9f);
            colors.pressedColor = new Color(0.55f, 0.42f, 0.22f, 1f);
            btn.colors = colors;
            btn.targetGraphic = img;
            btn.onClick.AddListener(SkipStory);

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(_skipButtonGo.transform, false);
            var lrt = labelGo.AddComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero;
            lrt.offsetMax = Vector2.zero;
            var tmp = labelGo.AddComponent<TextMeshProUGUI>();
            tmp.text = "跳过剧情";
            tmp.fontSize = 16f;
            tmp.color = new Color(0.95f, 0.9f, 0.78f, 1f);
            tmp.alignment = TextAlignmentOptions.Center;
        }

        /// <summary>
        /// Whether skip has been requested. Actions can check this to fast-forward.
        /// </summary>
        public bool IsSkipping => _skipRequested;

        void SkipStory()
        {
            if (_skipRequested) return;
            _skipRequested = true;

            Debug.Log("[StoryDirector] Story skip requested — fast-forwarding to end");

            // Remove skip button and its canvas immediately
            DestroySkipButton();

            // Stop the running story coroutine
            if (_runCoroutine != null)
                StopCoroutine(_runCoroutine);

            // Hide dialogue and choice panels
            HideDialogue();
            if (choicePanel != null)
                choicePanel.gameObject.SetActive(false);

            // Restore scene to end-of-story state (background, location, actors, morality)
            RestoreLastBackground();
            RestoreLastLocation();
            RestoreActorPositions();

            // Apply all remaining morality changes
            foreach (var action in sceneData.actions)
            {
                if (action is SetMoralityAction sma)
                    SetMorality(sma.value);
            }
            _actionIndex = sceneData.actions.Count;

            // Ensure CampManager exists
            if (FindAnyObjectByType<CampManager>() == null)
            {
                var go = new GameObject("CampManager");
                go.AddComponent<CampManager>();
            }

            EventBus.StoryCompleted();
        }

        void DestroySkipButton()
        {
            if (_skipButtonGo != null)
            {
                // Destroy the parent canvas we created for this button
                var parentCanvas = _skipButtonGo.GetComponentInParent<Canvas>();
                if (parentCanvas != null && parentCanvas.gameObject.name == "SkipButtonCanvas")
                    Destroy(parentCanvas.gameObject);
                else
                    Destroy(_skipButtonGo);
                _skipButtonGo = null;
            }
        }

        void PositionCamera()
        {
            var cam = Camera.main;
            if (cam == null) return;
            cam.transform.position = new Vector3(6.4f, -3.6f, -10f);
        }

        void SetupMorality()
        {
            if (moralityBar != null)
            {
                moralityBar.SetLabel("中立值");
                moralityBar.SetValue(_morality);
            }
        }

        void BuildLabelMap()
        {
            _labels.Clear();
            for (int i = 0; i < sceneData.actions.Count; i++)
            {
                if (sceneData.actions[i] is LabelAction la)
                    _labels[la.labelName] = i;
            }
        }

        void SetupActors()
        {
            _context = new StoryContext();

            foreach (var def in sceneData.actors)
            {
                if (string.IsNullOrEmpty(def.id)) continue;

                StoryUnit unit = FindActorUnit(def.id);
                _context.RegisterActor(def.id, unit, def);
            }
        }

        /// <summary>
        /// Find a StoryUnit in the scene by actor ID.
        /// Searches by GameObject name under actorParent, then globally.
        /// </summary>
        StoryUnit FindActorUnit(string actorId)
        {
            // Try finding under actorParent first
            if (actorParent != null)
            {
                // Search case-insensitively by common naming: "Hero", "Oldman", etc.
                foreach (Transform child in actorParent)
                {
                    if (child.name.Equals(actorId, System.StringComparison.OrdinalIgnoreCase))
                    {
                        var unit = child.GetComponent<StoryUnit>();
                        if (unit != null) return unit;
                    }
                }
            }

            // Fallback: find in the entire scene
            var allUnits = FindObjectsByType<StoryUnit>(FindObjectsSortMode.None);
            foreach (var unit in allUnits)
            {
                if (unit.gameObject.name.Equals(actorId, System.StringComparison.OrdinalIgnoreCase))
                    return unit;
            }

            // Non-visual actors (narrator, system) won't have units — that's fine
            return null;
        }

        IEnumerator RunActions()
        {
            _actionIndex = 0;
            while (_actionIndex < sceneData.actions.Count)
            {
                var action = sceneData.actions[_actionIndex];
                if (action == null)
                {
                    _actionIndex++;
                    continue;
                }

                _gotoRequested = false;
                yield return action.Execute(this);

                if (_gotoRequested)
                {
                    if (_labels.TryGetValue(_gotoTarget, out int targetIdx))
                        _actionIndex = targetIdx;
                    else
                    {
                        Debug.LogWarning($"[StoryDirector] Label not found: {_gotoTarget}");
                        _actionIndex++;
                    }
                }
                else
                {
                    _actionIndex++;
                }
            }

            // Story complete — CampManager listens for this event and shows camp overlay
            Debug.Log("[StoryDirector] Story complete, firing StoryCompleted event");
            HideDialogue();
            DestroySkipButton();

            // Ensure CampManager exists (for when starting directly from StoryScene without Boot)
            if (FindAnyObjectByType<CampManager>() == null)
            {
                var go = new GameObject("CampManager");
                go.AddComponent<CampManager>();
            }

            EventBus.StoryCompleted();
        }

        // ── Public API called by action classes ──

        public void GotoLabel(string label)
        {
            _gotoRequested = true;
            _gotoTarget = label;
        }

        public void SetMorality(int value)
        {
            _morality = Mathf.Clamp(value, 0, 100);
            UpdateMorality();
        }

        public void AddMorality(int delta)
        {
            _morality = Mathf.Clamp(_morality + delta, 0, 100);
            UpdateMorality();
        }

        public void SetBackground(Sprite sprite)
        {
            if (background == null || sprite == null) return;
            background.sprite = sprite;
            background.color = Color.white;
            FitBackgroundToCamera();
        }

        public void SetLocation(string text)
        {
            if (locationLabel != null) locationLabel.text = text;
        }

        public void HideDialogue()
        {
            if (dialogueBox != null) dialogueBox.gameObject.SetActive(false);
        }

        void UpdateMorality()
        {
            if (moralityBar != null)
                moralityBar.SetValue(_morality);
            EventBus.MoralityChanged(_morality);
        }

        /// <summary>
        /// When loading from save, replay the last position and face direction
        /// for each actor so they appear at their end-of-story positions.
        /// </summary>
        void RestoreActorPositions()
        {
            if (sceneData == null || _context == null) return;

            // Collect the last SetPosition and FaceDirection for each actor
            var lastPos = new Dictionary<string, Vector2>();
            var lastFace = new Dictionary<string, FaceDirection>();

            foreach (var action in sceneData.actions)
            {
                if (action is SetPositionAction sp && !string.IsNullOrEmpty(sp.actorId))
                    lastPos[sp.actorId] = sp.worldPosition;
                else if (action is MoveToAction mt && !string.IsNullOrEmpty(mt.actorId)
                         && mt.waypoints != null && mt.waypoints.Count > 0)
                    lastPos[mt.actorId] = mt.waypoints[mt.waypoints.Count - 1].position;
                else if (action is FaceDirectionAction fd && !string.IsNullOrEmpty(fd.actorId))
                    lastFace[fd.actorId] = fd.direction;
            }

            // Apply positions and faces
            foreach (var kvp in lastPos)
            {
                var unit = _context.GetActor(kvp.Key);
                if (unit == null) continue;

                unit.gameObject.SetActive(true);
                unit.transform.position = new Vector3(kvp.Value.x, kvp.Value.y, unit.transform.position.z);
                Debug.Log($"[StoryDirector] Restored actor '{kvp.Key}' at ({kvp.Value.x}, {kvp.Value.y})");

                if (lastFace.TryGetValue(kvp.Key, out var dir))
                {
                    Vector2Int faceVec = dir switch
                    {
                        FaceDirection.Up => Vector2Int.up,
                        FaceDirection.Down => Vector2Int.down,
                        FaceDirection.Left => Vector2Int.left,
                        FaceDirection.Right => Vector2Int.right,
                        _ => Vector2Int.down
                    };
                    unit.FaceDir(faceVec);
                }
            }
        }

        /// <summary>
        /// When loading from save, find the last SetBackgroundAction in story data
        /// and apply its sprite so the screen isn't black behind the camp overlay.
        /// </summary>
        void RestoreLastBackground()
        {
            if (sceneData == null || sceneData.actions == null) return;

            Sprite lastBg = null;
            foreach (var action in sceneData.actions)
            {
                if (action is SetBackgroundAction bgAction && bgAction.backgroundSprite != null)
                    lastBg = bgAction.backgroundSprite;
            }

            if (lastBg != null)
            {
                Debug.Log($"[StoryDirector] Restoring background: {lastBg.name}");
                SetBackground(lastBg);
            }
        }

        /// <summary>
        /// When loading from save, find the last SetLocationAction and restore it.
        /// </summary>
        void RestoreLastLocation()
        {
            if (sceneData == null || sceneData.actions == null) return;

            string lastLoc = null;
            foreach (var action in sceneData.actions)
            {
                if (action is SetLocationAction loc && !string.IsNullOrEmpty(loc.locationText))
                    lastLoc = loc.locationText;
            }

            if (lastLoc != null)
                SetLocation(lastLoc);
        }

        void FitBackgroundToCamera()
        {
            if (background == null || background.sprite == null) return;
            var cam = Camera.main;
            if (cam == null) return;

            float screenH = cam.orthographicSize * 2f;
            float screenW = screenH * cam.aspect;

            float spriteW = background.sprite.bounds.size.x;
            float spriteH = background.sprite.bounds.size.y;

            float scaleX = screenW / spriteW;
            float scaleY = screenH / spriteH;
            float scale = Mathf.Max(scaleX, scaleY);

            background.transform.localScale = new Vector3(scale, scale, 1f);
            background.transform.position = new Vector3(cam.transform.position.x, cam.transform.position.y, 10f);
        }
    }
}
