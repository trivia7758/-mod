using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using CaoCao.Story;
using CaoCao.Common;

namespace CaoCao.Editor
{
    /// <summary>
    /// Editor window that draws an isometric grid overlay on the background image.
    ///
    /// The grid uses per-map editorTileWidth/Height/GridOffset to match each
    /// background's perspective. These are EDITOR-ONLY — runtime uses canonical
    /// 48x48 iso (StoryUnit.TileToWorld) and is unaffected.
    ///
    /// Tile coordinates stored in unitPlacements / blockedTiles are always in
    /// the same tile-index space as DSL (event_setpos / event_move).
    /// </summary>
    public class StoryMapEditor : EditorWindow
    {
        // Editor state
        StoryMapDataRegistry _registry;
        TextAsset _dslFile;
        int _selectedMapIdx;
        bool _showGrid = true;
        bool _showBlocked = true;
        bool _showUnits = true;
        bool _showBackground = true;
        Vector2 _scrollPos;

        // Edit mode
        enum EditMode { Block, PlaceUnit }
        EditMode _editMode = EditMode.Block;
        int _placeUnitIdx = 0;

        // Cached background
        Texture2D _cachedBgTexture;
        string _cachedBgMapKey;
        Material _bgMaterial;
        Material _lineMaterial;

        // Parsed DSL data
        List<MapSegment> _mapSegments = new();
        string[] _mapKeyNames = new string[0];

        // Colors
        static readonly Color GridColor = new(1f, 1f, 1f, 0.18f);
        static readonly Color BlockedColor = new(1f, 0.2f, 0.2f, 0.4f);
        static readonly Color BlockedOutline = new(1f, 0.3f, 0.3f, 0.9f);
        static readonly Color HeroColor = new(0.2f, 0.6f, 1f, 0.9f);
        static readonly Color UnitColor = new(0.2f, 0.9f, 0.4f, 0.9f);

        struct UnitPos { public string unitId; public Vector2Int tile; }
        struct MapSegment { public string mapKey; public string bgPath; public List<UnitPos> unitPositions; }

        [MenuItem("CaoCao/Story Map Editor")]
        static void Open() => GetWindow<StoryMapEditor>("Story Map Editor");

        void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGUI;
            if (_dslFile == null) _dslFile = Resources.Load<TextAsset>("Data/story.dsl");
            FindRegistry();
            if (_dslFile != null) ParseDSL();
        }

        void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            if (_bgMaterial != null) DestroyImmediate(_bgMaterial);
            if (_lineMaterial != null) DestroyImmediate(_lineMaterial);
        }

        void FindRegistry()
        {
            if (_registry != null) return;
            _registry = AssetDatabase.LoadAssetAtPath<StoryMapDataRegistry>(
                "Assets/_Project/ScriptableObjects/StoryMaps/StoryMapRegistry.asset");
            if (_registry != null) return;
            var guids = AssetDatabase.FindAssets("t:StoryMapDataRegistry");
            if (guids.Length > 0)
                _registry = AssetDatabase.LoadAssetAtPath<StoryMapDataRegistry>(
                    AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        void EnsureMaterials()
        {
            if (_bgMaterial == null)
            {
                _bgMaterial = new Material(Shader.Find("Sprites/Default"));
                _bgMaterial.hideFlags = HideFlags.HideAndDontSave;
            }
            if (_lineMaterial == null)
            {
                _lineMaterial = new Material(Shader.Find("Hidden/Internal-Colored"));
                _lineMaterial.hideFlags = HideFlags.HideAndDontSave;
                _lineMaterial.SetInt("_ZWrite", 0);
                _lineMaterial.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
            }
        }

        // ============================================================
        // GUI
        // ============================================================
        void OnGUI()
        {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Story Map Editor", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            var newDsl = (TextAsset)EditorGUILayout.ObjectField("DSL File", _dslFile, typeof(TextAsset), false);
            if (newDsl != _dslFile) { _dslFile = newDsl; if (_dslFile != null) ParseDSL(); }

            _registry = (StoryMapDataRegistry)EditorGUILayout.ObjectField(
                "Map Registry", _registry, typeof(StoryMapDataRegistry), false);

            EditorGUILayout.Space(4);

            if (_mapKeyNames.Length > 0)
            {
                int newIdx = EditorGUILayout.Popup("Map",
                    Mathf.Clamp(_selectedMapIdx, 0, _mapKeyNames.Length - 1), _mapKeyNames);
                if (newIdx != _selectedMapIdx) { _selectedMapIdx = newIdx; _cachedBgMapKey = null; }
            }
            else EditorGUILayout.LabelField("No maps found. Load a DSL file.");

            EditorGUILayout.Space(4);
            _showBackground = EditorGUILayout.Toggle("Show Background", _showBackground);
            _showGrid = EditorGUILayout.Toggle("Show Grid", _showGrid);
            _showBlocked = EditorGUILayout.Toggle("Show Blocked", _showBlocked);
            _showUnits = EditorGUILayout.Toggle("Show Units", _showUnits);

            EditorGUILayout.Space(8);

            // ---- Per-map settings ----
            var mapData = GetCurrentMapData();
            if (mapData != null)
            {
                EditorGUILayout.LabelField("Map Settings", EditorStyles.boldLabel);
                var so = new SerializedObject(mapData);
                so.Update();
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(so.FindProperty("gridSize"), new GUIContent("Grid Size (cols x rows)"));
                if (EditorGUI.EndChangeCheck()) so.ApplyModifiedProperties();

                // Editor grid overlay controls
                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("Editor Grid Overlay", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(
                    "Adjust tile width/height to match background tile size.\n" +
                    "Grid offset positions tile (0,0).\n" +
                    "These settings are editor-only and do NOT affect runtime.",
                    MessageType.Info);
                EditorGUI.BeginChangeCheck();
                mapData.editorTileWidth = EditorGUILayout.Slider("Tile Half-Width", mapData.editorTileWidth, 0.1f, 2f);
                mapData.editorTileHeight = EditorGUILayout.Slider("Tile Half-Height", mapData.editorTileHeight, 0.05f, 1f);
                mapData.editorGridOffset = EditorGUILayout.Vector2Field("Grid Offset", mapData.editorGridOffset);
                if (EditorGUI.EndChangeCheck()) EditorUtility.SetDirty(mapData);

                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField($"Blocked: {mapData.blockedTiles.Count}  Units: {mapData.unitPlacements.Count}");

                // Edit mode
                EditorGUILayout.Space(4);
                _editMode = (EditMode)EditorGUILayout.EnumPopup("Edit Mode", _editMode);
                if (_editMode == EditMode.PlaceUnit)
                {
                    _placeUnitIdx = EditorGUILayout.Popup("Unit ID", _placeUnitIdx, StoryUnitIds.All);
                    EditorGUILayout.HelpBox("Left-click = place unit\nRight-click = remove unit at tile", MessageType.Info);
                }
                else
                    EditorGUILayout.HelpBox("Left-click = toggle blocked tile", MessageType.Info);

                // Unit list
                if (mapData.unitPlacements.Count > 0)
                {
                    EditorGUILayout.Space(4);
                    EditorGUILayout.LabelField("Placed Units:", EditorStyles.miniLabel);
                    for (int i = mapData.unitPlacements.Count - 1; i >= 0; i--)
                    {
                        var up = mapData.unitPlacements[i];
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField($"  {up.unitId} @ ({up.tile.x},{up.tile.y})");
                        if (GUILayout.Button("x", GUILayout.Width(20)))
                        {
                            mapData.unitPlacements.RemoveAt(i);
                            EditorUtility.SetDirty(mapData);
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                }
            }
            else if (_selectedMapIdx >= 0 && _selectedMapIdx < _mapSegments.Count)
                EditorGUILayout.HelpBox("No StoryMapData asset. Click 'Auto-Create' below.", MessageType.Warning);

            EditorGUILayout.Space(8);
            if (GUILayout.Button("Reload DSL") && _dslFile != null) ParseDSL();
            if (GUILayout.Button("Auto-Create Map Assets")) AutoCreateMapAssets();
            EditorGUILayout.Space(4);
            if (GUILayout.Button("Focus Scene View (2D)")) FocusSceneView();
            if (mapData != null && GUILayout.Button("Import DSL Unit Positions"))
                ImportDSLUnitPositions(mapData);

            EditorGUILayout.EndScrollView();
            if (GUI.changed) SceneView.RepaintAll();
        }

        // ============================================================
        // Scene View
        // ============================================================
        void OnSceneGUI(SceneView sv)
        {
            if (_mapSegments == null || _mapSegments.Count == 0) return;
            if (_selectedMapIdx < 0 || _selectedMapIdx >= _mapSegments.Count) return;

            var seg = _mapSegments[_selectedMapIdx];
            var mapData = GetCurrentMapData();

            Vector2Int gs = mapData != null ? mapData.gridSize : new Vector2Int(15, 15);
            gs.x = Mathf.Clamp(gs.x, 1, 100);
            gs.y = Mathf.Clamp(gs.y, 1, 100);

            // Get editor grid params from mapData (or defaults)
            float hw = mapData != null ? mapData.editorTileWidth : 0.24f;
            float hh = mapData != null ? mapData.editorTileHeight : 0.24f;
            Vector2 off = mapData != null ? mapData.editorGridOffset : new Vector2(0.24f, -0.24f);

            EnsureMaterials();

            if (_showBackground) DrawBackground(seg);
            if (_showGrid) DrawGridGL(gs, hw, hh, off);
            if (_showBlocked && mapData != null) DrawBlockedGL(mapData, hw, hh, off);
            if (_showUnits) DrawUnits(seg, mapData, hw, hh, off);

            HandleClick(mapData, gs, hw, hh, off);
        }

        // ---- Background ----
        void DrawBackground(MapSegment seg)
        {
            if (string.IsNullOrEmpty(seg.bgPath)) return;

            if (_cachedBgMapKey != seg.mapKey || _cachedBgTexture == null)
            {
                _cachedBgTexture = LoadBackgroundTexture(seg.bgPath);
                _cachedBgMapKey = seg.mapKey;
            }
            if (_cachedBgTexture == null || _bgMaterial == null) return;

            _bgMaterial.mainTexture = _cachedBgTexture;
            _bgMaterial.SetPass(0);

            GL.PushMatrix();
            GL.Begin(GL.QUADS);
            GL.Color(Color.white);
            GL.TexCoord2(0, 0); GL.Vertex3(0f,    -7.2f, 1f);
            GL.TexCoord2(1, 0); GL.Vertex3(12.8f, -7.2f, 1f);
            GL.TexCoord2(1, 1); GL.Vertex3(12.8f,  0f,   1f);
            GL.TexCoord2(0, 1); GL.Vertex3(0f,     0f,   1f);
            GL.End();
            GL.PopMatrix();
        }

        // ---- Grid (GL lines) ----
        void DrawGridGL(Vector2Int gs, float hw, float hh, Vector2 off)
        {
            if (_lineMaterial == null) return;
            _lineMaterial.SetPass(0);

            GL.PushMatrix();
            GL.Begin(GL.LINES);
            GL.Color(GridColor);

            for (int tx = 0; tx < gs.x; tx++)
            {
                for (int ty = 0; ty < gs.y; ty++)
                {
                    TileDiamond(new Vector2Int(tx, ty), hw, hh, off,
                        out var t, out var r, out var b, out var l);
                    GLLine(t, r); GLLine(r, b); GLLine(b, l); GLLine(l, t);
                }
            }

            GL.End();
            GL.PopMatrix();
        }

        // ---- Blocked tiles (GL quads) ----
        void DrawBlockedGL(StoryMapData mapData, float hw, float hh, Vector2 off)
        {
            if (mapData.blockedTiles == null || mapData.blockedTiles.Count == 0) return;
            if (_lineMaterial == null) return;

            _lineMaterial.SetPass(0);
            GL.PushMatrix();
            GL.Begin(GL.QUADS);
            GL.Color(BlockedColor);
            foreach (var tile in mapData.blockedTiles)
            {
                TileDiamond(tile, hw, hh, off, out var t, out var r, out var b, out var l);
                GL.Vertex3(t.x, t.y, 0); GL.Vertex3(r.x, r.y, 0);
                GL.Vertex3(b.x, b.y, 0); GL.Vertex3(l.x, l.y, 0);
            }
            GL.End();

            GL.Begin(GL.LINES);
            GL.Color(BlockedOutline);
            foreach (var tile in mapData.blockedTiles)
            {
                TileDiamond(tile, hw, hh, off, out var t, out var r, out var b, out var l);
                GLLine(t, r); GLLine(r, b); GLLine(b, l); GLLine(l, t);
            }
            GL.End();
            GL.PopMatrix();
        }

        static void GLLine(Vector3 a, Vector3 b)
        {
            GL.Vertex3(a.x, a.y, a.z);
            GL.Vertex3(b.x, b.y, b.z);
        }

        // ---- Units ----
        void DrawUnits(MapSegment seg, StoryMapData mapData, float hw, float hh, Vector2 off)
        {
            List<UnitPlacement> placements = null;
            Dictionary<string, Vector2Int> dslPos = null;

            if (mapData != null && mapData.unitPlacements != null && mapData.unitPlacements.Count > 0)
                placements = mapData.unitPlacements;
            else
            {
                dslPos = new Dictionary<string, Vector2Int>();
                if (seg.unitPositions != null)
                    foreach (var u in seg.unitPositions) dslPos[u.unitId] = u.tile;
            }

            if (placements != null)
                foreach (var up in placements)
                    DrawUnitMarker(up.unitId, up.tile, hw, hh, off);
            else if (dslPos != null)
                foreach (var kv in dslPos)
                    DrawUnitMarker(kv.Key, kv.Value, hw, hh, off);
        }

        void DrawUnitMarker(string id, Vector2Int tile, float hw, float hh, Vector2 off)
        {
            var c = TileCenter(tile, hw, hh, off);
            Color col = id == "hero" ? HeroColor : UnitColor;
            float r = hw * 0.3f;
            Handles.color = col;
            Handles.DrawSolidDisc(c, Vector3.forward, Mathf.Max(r, 0.06f));

            var style = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = col },
                alignment = TextAnchor.MiddleCenter,
                fontSize = 10
            };
            Handles.Label(c + Vector3.up * (hh * 0.6f), id, style);
        }

        // ---- Click handling ----
        void HandleClick(StoryMapData mapData, Vector2Int gs, float hw, float hh, Vector2 off)
        {
            if (mapData == null) return;
            var e = Event.current;
            if (e.type != EventType.MouseDown || e.alt) return;

            int cid = GUIUtility.GetControlID(FocusType.Passive);
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            if (Mathf.Abs(ray.direction.z) < 0.0001f) return;
            float t = -ray.origin.z / ray.direction.z;
            Vector3 hit = ray.GetPoint(t);

            Vector2Int tile = WorldToTile(hit, hw, hh, off);
            if (tile.x < 0 || tile.x >= gs.x || tile.y < 0 || tile.y >= gs.y) return;

            if (e.button == 0)
            {
                if (_editMode == EditMode.Block)
                {
                    mapData.SetBlocked(tile, !mapData.IsBlocked(tile));
                    EditorUtility.SetDirty(mapData);
                }
                else if (_editMode == EditMode.PlaceUnit)
                {
                    string uid = StoryUnitIds.All[_placeUnitIdx];
                    mapData.unitPlacements.RemoveAll(u => u.unitId == uid);
                    mapData.unitPlacements.Add(new UnitPlacement { unitId = uid, tile = tile });
                    EditorUtility.SetDirty(mapData);
                }
                e.Use(); GUIUtility.hotControl = cid;
                SceneView.RepaintAll(); Repaint();
            }
            else if (e.button == 1 && _editMode == EditMode.PlaceUnit)
            {
                if (mapData.unitPlacements.RemoveAll(u => u.tile == tile) > 0)
                {
                    EditorUtility.SetDirty(mapData);
                    e.Use(); GUIUtility.hotControl = cid;
                    SceneView.RepaintAll(); Repaint();
                }
            }
        }

        // ============================================================
        // Coordinate helpers
        //
        // Editor grid uses per-map editorTileWidth (hw), editorTileHeight (hh),
        // and editorGridOffset (off) to draw diamonds that match the background.
        //
        // Formula (standard isometric):
        //   center.x = (tile.x - tile.y) * hw + off.x
        //   center.y = (tile.x + tile.y) * (-hh) + off.y
        //
        // Stored tile coords are the same index space as DSL / runtime.
        // ============================================================

        /// <summary>
        /// Tile -> editor world position using per-map grid params.
        /// </summary>
        static Vector3 TileCenter(Vector2Int tile, float hw, float hh, Vector2 off)
        {
            float x = (tile.x - tile.y) * hw + off.x;
            float y = (tile.x + tile.y) * (-hh) + off.y;
            return new Vector3(x, y, 0f);
        }

        /// <summary>
        /// Editor world position -> tile coordinate (inverse of TileCenter).
        /// </summary>
        static Vector2Int WorldToTile(Vector3 wp, float hw, float hh, Vector2 off)
        {
            // Undo offset
            float dx = wp.x - off.x;
            float dy = wp.y - off.y;
            // Undo iso: dx = (tx - ty) * hw, dy = (tx + ty) * (-hh)
            // tx - ty = dx / hw
            // tx + ty = dy / (-hh)
            float sum = dx / hw;     // tx - ty
            float dif = dy / (-hh);  // tx + ty
            float fx = (sum + dif) * 0.5f;
            float fy = (dif - sum) * 0.5f;
            return new Vector2Int(Mathf.RoundToInt(fx), Mathf.RoundToInt(fy));
        }

        /// <summary>
        /// Diamond vertices for a tile in editor world space.
        /// </summary>
        static void TileDiamond(Vector2Int tile, float hw, float hh, Vector2 off,
            out Vector3 top, out Vector3 right, out Vector3 bottom, out Vector3 left)
        {
            var c = TileCenter(tile, hw, hh, off);
            top    = new Vector3(c.x,      c.y + hh, 0);
            right  = new Vector3(c.x + hw, c.y,      0);
            bottom = new Vector3(c.x,      c.y - hh, 0);
            left   = new Vector3(c.x - hw, c.y,      0);
        }

        // ============================================================
        // Focus
        // ============================================================
        void FocusSceneView()
        {
            var sv = SceneView.lastActiveSceneView;
            if (sv == null) return;
            sv.in2DMode = true;
            sv.pivot = new Vector3(6.4f, -3.6f, 0f);
            sv.size = 5f;
            sv.Repaint();
        }

        // ============================================================
        // DSL Parsing
        // ============================================================
        void ParseDSL()
        {
            _mapSegments.Clear();
            if (_dslFile == null) { UpdateMapNames(); return; }

            var parser = new StoryDSLParser();
            var events = parser.Parse(_dslFile.text);

            string curKey = "_default";
            string curBg = "";
            var curUnits = new List<UnitPos>();
            var seen = new HashSet<string>();
            var unitTiles = new Dictionary<string, Vector2Int>();

            foreach (var ev in events)
            {
                if (ev is BackgroundEvent bg)
                {
                    string nk = StoryMapHelper.ExtractMapKey(bg.Path);
                    if (nk != curKey)
                    {
                        if (curUnits.Count > 0 || curKey != "_default")
                            Finalize(curKey, curBg, curUnits, seen);
                        curKey = nk; curBg = bg.Path;
                        curUnits = new List<UnitPos>();
                        foreach (var kv in unitTiles)
                            curUnits.Add(new UnitPos { unitId = kv.Key, tile = kv.Value });
                    }
                }
                else if (ev is SetPosEvent sp)
                {
                    var tl = new Vector2Int(sp.X, sp.Y);
                    unitTiles[sp.UnitId] = tl;
                    curUnits.Add(new UnitPos { unitId = sp.UnitId, tile = tl });
                }
                else if (ev is MoveEvent me)
                {
                    var tl = new Vector2Int(me.X, me.Y);
                    unitTiles[me.UnitId] = tl;
                    curUnits.Add(new UnitPos { unitId = me.UnitId, tile = tl });
                }
            }
            if (curUnits.Count > 0 || curKey != "_default")
                Finalize(curKey, curBg, curUnits, seen);

            if (_mapSegments.Count >= 2 && _mapSegments[0].mapKey == "_default")
            {
                var first = _mapSegments[1];
                first.unitPositions.InsertRange(0, _mapSegments[0].unitPositions);
                _mapSegments[1] = first;
                _mapSegments.RemoveAt(0);
            }
            UpdateMapNames();
            SceneView.RepaintAll();
        }

        void Finalize(string key, string bg, List<UnitPos> units, HashSet<string> seen)
        {
            if (seen.Contains(key))
            {
                for (int i = 0; i < _mapSegments.Count; i++)
                    if (_mapSegments[i].mapKey == key)
                    {
                        var s = _mapSegments[i]; s.unitPositions.AddRange(units); _mapSegments[i] = s;
                        break;
                    }
                return;
            }
            seen.Add(key);
            _mapSegments.Add(new MapSegment { mapKey = key, bgPath = bg, unitPositions = new(units) });
        }

        void UpdateMapNames()
        {
            _mapKeyNames = new string[_mapSegments.Count];
            for (int i = 0; i < _mapSegments.Count; i++) _mapKeyNames[i] = _mapSegments[i].mapKey;
        }

        void ImportDSLUnitPositions(StoryMapData mapData)
        {
            if (_selectedMapIdx < 0 || _selectedMapIdx >= _mapSegments.Count) return;
            var seg = _mapSegments[_selectedMapIdx];
            var last = new Dictionary<string, Vector2Int>();
            if (seg.unitPositions != null)
                foreach (var u in seg.unitPositions) last[u.unitId] = u.tile;
            foreach (var kv in last)
            {
                bool found = false;
                foreach (var up in mapData.unitPlacements)
                    if (up.unitId == kv.Key) { found = true; break; }
                if (!found)
                    mapData.unitPlacements.Add(new UnitPlacement { unitId = kv.Key, tile = kv.Value });
            }
            EditorUtility.SetDirty(mapData);
        }

        // ============================================================
        // Asset helpers
        // ============================================================
        static Texture2D LoadBackgroundTexture(string godotPath)
        {
            string stripped = godotPath.Replace("res://assets/ui/backgrounds/", "");
            int dot = stripped.LastIndexOf('.');
            string noExt = dot >= 0 ? stripped.Substring(0, dot) : stripped;
            int slash = noExt.IndexOf('/');
            string fix = noExt;
            if (slash > 0)
                fix = noExt.Substring(0, 1).ToUpper() + noExt.Substring(1, slash - 1) + noExt.Substring(slash);
            string basePath = $"Assets/_Project/Resources/Backgrounds/{fix}";
            foreach (var ext in new[] { ".png", ".jpg", ".jpeg" })
            {
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(basePath + ext);
                if (tex != null) return tex;
            }
            return null;
        }

        void AutoCreateMapAssets()
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Project/ScriptableObjects"))
                AssetDatabase.CreateFolder("Assets/_Project", "ScriptableObjects");
            if (!AssetDatabase.IsValidFolder("Assets/_Project/ScriptableObjects/StoryMaps"))
                AssetDatabase.CreateFolder("Assets/_Project/ScriptableObjects", "StoryMaps");
            if (_registry == null)
            {
                _registry = CreateInstance<StoryMapDataRegistry>();
                AssetDatabase.CreateAsset(_registry, "Assets/_Project/ScriptableObjects/StoryMaps/StoryMapRegistry.asset");
            }
            int c = 0;
            foreach (var seg in _mapSegments)
            {
                if (seg.mapKey == "_default" || _registry.GetMap(seg.mapKey) != null) continue;
                var md = CreateInstance<StoryMapData>();
                md.mapKey = seg.mapKey;
                string fn = seg.mapKey.Replace('/', '_');
                AssetDatabase.CreateAsset(md, $"Assets/_Project/ScriptableObjects/StoryMaps/{fn}.asset");
                _registry.AddMap(md); c++;
            }
            EditorUtility.SetDirty(_registry);
            AssetDatabase.SaveAssets();
            Repaint();
        }

        StoryMapData GetCurrentMapData()
        {
            if (_registry == null || _selectedMapIdx < 0 || _selectedMapIdx >= _mapSegments.Count) return null;
            return _registry.GetMap(_mapSegments[_selectedMapIdx].mapKey);
        }
    }
}
