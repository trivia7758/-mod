using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CaoCao.Core;
using CaoCao.UI;

namespace CaoCao.Story
{
    public class StorySceneController : MonoBehaviour
    {
        [Header("Grid")]
        [SerializeField] Vector2Int gridSize = new(40, 30);

        [Header("Units")]
        [SerializeField] StoryUnit hero;
        [SerializeField] StoryUnit oldman;

        [Header("UI")]
        [SerializeField] DialogueBox dialogueBox;
        [SerializeField] ChoicePanel choicePanel;
        [SerializeField] SpriteRenderer background;
        [SerializeField] TMP_Text locationLabel;
        [SerializeField] MoralityBar moralityBar;

        [Header("Data")]
        [SerializeField] TextAsset dslFile;

        [Header("Portraits")]
        [SerializeField] Sprite heroPortrait;
        [SerializeField] Sprite oldmanPortrait;

        [Header("Map Data")]
        [SerializeField] StoryMapDataRegistry mapDataRegistry;

        int _morality = 50;
        StoryPathfinder _pathfinder = new();
        StoryMapData _currentMapData;
        Dictionary<string, int> _labels = new();

        // Character metadata
        struct CharMeta
        {
            public string name;
            public Sprite portrait;
            public DialogueSide side;
        }
        Dictionary<string, CharMeta> _charMeta;

        void Start()
        {
            // Auto-load portraits from Resources if not assigned in Inspector
            if (heroPortrait == null)
                heroPortrait = Resources.Load<Sprite>("Portraits/nanzhu");
            if (oldmanPortrait == null)
                oldmanPortrait = Resources.Load<Sprite>("Portraits/shenmilaoren");
            _charMeta = new Dictionary<string, CharMeta>
            {
                { "hero", new CharMeta { name = "张强", portrait = heroPortrait, side = DialogueSide.Left } },
                { "oldman", new CharMeta { name = "神秘老人", portrait = oldmanPortrait, side = DialogueSide.Right } },
                { "narrator", new CharMeta { name = "", portrait = null, side = DialogueSide.Center } },
                { "system", new CharMeta { name = "", portrait = null, side = DialogueSide.Center } }
            };

            AutoFindReferences();
            PositionCamera();
            SetupUnits();
            SetupBackground();
            SetupMorality();
            RunScript();
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
        }

        void PositionCamera()
        {
            var cam = Camera.main;
            if (cam == null) return;
            cam.transform.position = new Vector3(6.4f, -3.6f, -10f);
        }

        /// <summary>
        /// Initialize units with default grid params. ApplyMapData will
        /// update these from StoryMapData when a background loads.
        /// </summary>
        void SetupUnits()
        {
            foreach (var unit in new[] { hero, oldman })
            {
                if (unit == null) continue;
                unit.isIsometric = true;
            }
        }

        void SetupMorality()
        {
            if (moralityBar != null)
            {
                moralityBar.SetLabel("中立值");
                moralityBar.SetValue(_morality);
            }
        }

        void RunScript()
        {
            if (dslFile == null)
                dslFile = Resources.Load<TextAsset>("Data/story.dsl");

            if (dslFile == null)
            {
                Debug.LogError("[StoryScene] No DSL file assigned and fallback 'Data/story.dsl' not found!");
                return;
            }

            var parser = new StoryDSLParser();
            var events = parser.Parse(dslFile.text);

            ApplyNavBounds(events);
            _labels = BuildLabelMap(events);
            StartCoroutine(ExecuteEvents(events));
        }

        void ApplyNavBounds(List<StoryEvent> events)
        {
            int minX = 0, maxX = 0, minY = 0, maxY = 0;
            bool first = true;
            foreach (var ev in events)
            {
                int x = 0, y = 0;
                bool hasCoord = false;
                if (ev is SetPosEvent sp) { x = sp.X; y = sp.Y; hasCoord = true; }
                else if (ev is MoveEvent me) { x = me.X; y = me.Y; hasCoord = true; }

                if (hasCoord)
                {
                    if (first) { minX = maxX = x; minY = maxY = y; first = false; }
                    else { minX = Mathf.Min(minX, x); maxX = Mathf.Max(maxX, x); minY = Mathf.Min(minY, y); maxY = Mathf.Max(maxY, y); }
                }
            }
            int pad = 8;
            var origin = new Vector2Int(minX - pad, minY - pad);
            var size = new Vector2Int(maxX - minX + pad * 2 + 1, maxY - minY + pad * 2 + 1);
            _pathfinder.Setup(size, origin);
        }

        Dictionary<string, int> BuildLabelMap(List<StoryEvent> events)
        {
            var map = new Dictionary<string, int>();
            for (int i = 0; i < events.Count; i++)
            {
                if (events[i] is LabelEvent le)
                    map[le.Name] = i;
            }
            return map;
        }

        IEnumerator ExecuteEvents(List<StoryEvent> events)
        {
            int idx = 0;
            while (idx < events.Count)
            {
                var ev = events[idx];
                switch (ev)
                {
                    case LabelEvent _:
                        break;
                    case GotoEvent ge:
                        if (_labels.TryGetValue(ge.Label, out int target))
                        { idx = target; continue; }
                        break;
                    case SetPosEvent sp:
                        SetUnitPos(sp);
                        break;
                    case MoveEvent me:
                        yield return MoveUnit(me);
                        break;
                    case FaceEvent fe:
                        FaceUnit(fe);
                        break;
                    case TalkEvent te:
                        yield return Talk(te);
                        break;
                    case HideDialogueEvent _:
                        if (dialogueBox != null) dialogueBox.gameObject.SetActive(false);
                        break;
                    case WaitEvent we:
                        yield return new WaitForSeconds(we.Seconds);
                        break;
                    case BackgroundEvent be:
                        SetBackground(be.Path);
                        break;
                    case LocationEvent le:
                        if (locationLabel != null) locationLabel.text = le.Text;
                        break;
                    case MoralityEvent me:
                        _morality = me.Value;
                        UpdateMorality();
                        break;
                    case ChoiceEvent ce:
                        if (choicePanel == null) break;
                        // Convert old ChoiceOption to new StoryChoiceOption for updated ChoicePanel
                        var storyOpts = new List<StoryChoiceOption>();
                        foreach (var o in ce.Options)
                            storyOpts.Add(new StoryChoiceOption { text = o.Text, gotoLabel = o.GotoLabel, moralityDelta = o.MoralityDelta });
                        StoryChoiceOption chosen = null;
                        yield return choicePanel.ShowChoices(storyOpts, opt => chosen = opt);
                        if (chosen != null)
                        {
                            _morality += chosen.moralityDelta;
                            UpdateMorality();
                            if (!string.IsNullOrEmpty(chosen.gotoLabel) && _labels.TryGetValue(chosen.gotoLabel, out int lbl))
                            { idx = lbl; continue; }
                        }
                        break;
                }
                idx++;
            }

            var loader = ServiceLocator.Get<SceneLoader>();
            if (loader != null)
                loader.LoadScene("Battle");
        }

        void SetUnitPos(SetPosEvent ev)
        {
            var unit = GetUnit(ev.UnitId);
            if (unit == null) return;
            unit.gameObject.SetActive(true);

            // DSL event_setpos always takes priority — it's the script's explicit command
            unit.tile = new Vector2Int(ev.X, ev.Y);
            unit.transform.position = unit.TileToWorld(unit.tile);
            Debug.Log($"[DEBUG] SetUnitPos {ev.UnitId} tile=({ev.X},{ev.Y}) hw={unit.tileHalfWidth} hh={unit.tileHalfHeight} off={unit.gridOffset} → world={unit.transform.position}");
        }

        IEnumerator MoveUnit(MoveEvent ev)
        {
            var unit = GetUnit(ev.UnitId);
            if (unit == null) yield break;
            unit.gameObject.SetActive(true);
            var target = new Vector2Int(ev.X, ev.Y);
            var path = _pathfinder.FindPath(unit.tile, target);
            if (path.Count == 0) yield break;
            bool reached = false;
            unit.OnReachedTile += () => reached = true;
            unit.MoveAlong(path);
            while (!reached) yield return null;
        }

        void FaceUnit(FaceEvent ev)
        {
            var unit = GetUnit(ev.UnitId);
            if (unit == null) return;
            unit.FaceDir(ParseDir(ev.Direction));
        }

        IEnumerator Talk(TalkEvent ev)
        {
            if (dialogueBox == null)
            {
                Debug.LogWarning($"[StoryScene] DialogueBox not found, skipping talk: {ev.Text}");
                yield break;
            }

            var unit = GetUnit(ev.UnitId);
            var meta = _charMeta.GetValueOrDefault(ev.UnitId,
                new CharMeta { name = "", portrait = null, side = DialogueSide.Left });

            if (unit != null) unit.PlayTalk();
            yield return dialogueBox.ShowLine(ev.Text, meta.name, meta.portrait, meta.side);
            if (unit != null) unit.StopTalk();
        }

        void SetupBackground()
        {
            if (background == null) return;
            var homeSprite = Resources.Load<Sprite>("Backgrounds/Story/home");
            if (homeSprite != null)
            {
                background.sprite = homeSprite;
                background.color = Color.white;
                FitBackgroundToCamera();
            }
            ApplyMapData("res://assets/ui/backgrounds/story/home.png");
        }

        void SetBackground(string path)
        {
            if (background == null || string.IsNullOrEmpty(path)) return;
            string resPath = ConvertGodotPath(path);
            var sprite = Resources.Load<Sprite>(resPath);
            if (sprite != null)
            {
                background.sprite = sprite;
                background.color = Color.white;
                FitBackgroundToCamera();
            }
            else
            {
                Debug.LogWarning($"[StoryScene] Background not found: {resPath} (from: {path})");
            }
            ApplyMapData(path);
        }

        void ApplyMapData(string godotPath)
        {
            if (mapDataRegistry == null) return;
            string mapKey = StoryMapHelper.ExtractMapKey(godotPath);
            _currentMapData = mapDataRegistry.GetMap(mapKey);
            if (_currentMapData != null)
            {
                _currentMapData.ApplyTo(_pathfinder);
                // Push grid params to units so TileToWorld matches editor overlay
                ApplyGridToUnits(_currentMapData);
            }
            else
                _pathfinder.ClearBlocked();
        }

        /// <summary>
        /// Copy editor grid overlay params (tileWidth/Height/Offset) to units
        /// so runtime TileToWorld matches editor placement exactly.
        /// </summary>
        void ApplyGridToUnits(StoryMapData mapData)
        {
            Debug.Log($"[DEBUG] ApplyGridToUnits: hw={mapData.editorTileWidth} hh={mapData.editorTileHeight} off={mapData.editorGridOffset}");
            foreach (var unit in new[] { hero, oldman })
            {
                if (unit == null) continue;
                unit.tileHalfWidth = mapData.editorTileWidth;
                unit.tileHalfHeight = mapData.editorTileHeight;
                unit.gridOffset = mapData.editorGridOffset;
            }
        }

        // Editor placements (unitPlacements in StoryMapData) are editor-only.
        // Runtime positions are fully controlled by DSL (event_setpos / event_move).

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

        string ConvertGodotPath(string godotPath)
        {
            string p = godotPath.Replace("res://assets/ui/backgrounds/", "");
            int dotIdx = p.LastIndexOf('.');
            if (dotIdx >= 0) p = p.Substring(0, dotIdx);
            int slashIdx = p.IndexOf('/');
            if (slashIdx > 0)
            {
                string folder = p.Substring(0, 1).ToUpper() + p.Substring(1, slashIdx - 1);
                string rest = p.Substring(slashIdx);
                p = folder + rest;
            }
            return "Backgrounds/" + p;
        }

        void UpdateMorality()
        {
            if (moralityBar != null)
                moralityBar.SetValue(_morality);
            EventBus.MoralityChanged(_morality);
        }

        StoryUnit GetUnit(string id)
        {
            return id switch
            {
                "hero" => hero,
                "oldman" => oldman,
                _ => null
            };
        }

        static Vector2Int ParseDir(string raw)
        {
            return raw.ToLower() switch
            {
                "up" => Vector2Int.up,
                "down" => Vector2Int.down,
                "left" => Vector2Int.left,
                "right" => Vector2Int.right,
                _ => Vector2Int.down
            };
        }
    }
}
