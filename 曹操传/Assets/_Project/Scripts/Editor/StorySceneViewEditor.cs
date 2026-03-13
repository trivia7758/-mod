using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using CaoCao.Story;

namespace CaoCao.Editor
{
    /// <summary>
    /// Scene View integration for StorySceneData.
    /// When a StorySceneData asset is selected, draws:
    /// - Position handles for SetPositionAction
    /// - Path lines + waypoint handles for MoveToAction
    /// - Actor labels at their last known positions
    /// </summary>
    [InitializeOnLoad]
    public static class StorySceneViewEditor
    {
        // Colors
        static readonly Color PathColor = new(0f, 0.9f, 1f, 0.8f);
        static readonly Color WaypointColor = new(1f, 0.6f, 0f, 0.9f);
        static readonly Color PositionColor = new(0.3f, 1f, 0.3f, 0.9f);
        static readonly Color InactivePathColor = new(0.5f, 0.7f, 0.8f, 0.3f);
        static readonly Color LabelBgColor = new(0f, 0f, 0f, 0.6f);

        static StorySceneData _activeData;
        static int _selectedActionIndex = -1;

        static StorySceneViewEditor()
        {
            SceneView.duringSceneGui += OnSceneGUI;
            Selection.selectionChanged += OnSelectionChanged;
        }

        static void OnSelectionChanged()
        {
            _activeData = null;
            _selectedActionIndex = -1;

            if (Selection.activeObject is StorySceneData data)
            {
                _activeData = data;
            }

            SceneView.RepaintAll();
        }

        /// <summary>
        /// Called externally by StorySceneDataEditor to sync selection.
        /// </summary>
        public static void SetSelectedActionIndex(int index)
        {
            _selectedActionIndex = index;
            SceneView.RepaintAll();
        }

        static void OnSceneGUI(SceneView sceneView)
        {
            if (_activeData == null) return;

            // Track positions for label drawing
            var actorPositions = new Dictionary<string, Vector2>();

            for (int i = 0; i < _activeData.actions.Count; i++)
            {
                var action = _activeData.actions[i];
                if (action == null) continue;

                bool isSelected = (i == _selectedActionIndex);

                switch (action)
                {
                    case SetPositionAction spa:
                        DrawSetPositionHandle(spa, i, isSelected);
                        if (!string.IsNullOrEmpty(spa.actorId))
                            actorPositions[spa.actorId] = spa.worldPosition;
                        break;

                    case MoveToAction mta:
                        DrawMoveToHandles(mta, i, isSelected, actorPositions);
                        if (!string.IsNullOrEmpty(mta.actorId) && mta.waypoints.Count > 0)
                            actorPositions[mta.actorId] = mta.waypoints[^1].position;
                        break;
                }
            }

            // Draw actor labels at their last known positions
            DrawActorLabels(actorPositions);
        }

        static void DrawSetPositionHandle(SetPositionAction action, int actionIndex, bool isSelected)
        {
            Vector3 pos = new Vector3(action.worldPosition.x, action.worldPosition.y, 0);

            if (isSelected)
            {
                // Full position handle for the selected action
                EditorGUI.BeginChangeCheck();
                var newPos = Handles.PositionHandle(pos, Quaternion.identity);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(_activeData, "Move SetPosition");
                    action.worldPosition = new Vector2(newPos.x, newPos.y);
                    EditorUtility.SetDirty(_activeData);
                }
            }

            // Always draw a disc marker
            Handles.color = isSelected ? PositionColor : new Color(0.3f, 1f, 0.3f, 0.4f);
            Handles.DrawSolidDisc(pos, Vector3.forward, isSelected ? 0.15f : 0.08f);

            // Label
            if (isSelected)
            {
                Handles.Label(pos + Vector3.up * 0.3f,
                    $"SetPos [{actionIndex}] {action.actorId}",
                    GetLabelStyle());
            }
        }

        static void DrawMoveToHandles(MoveToAction action, int actionIndex, bool isSelected,
            Dictionary<string, Vector2> actorPositions)
        {
            if (action.waypoints == null || action.waypoints.Count == 0) return;

            // Draw starting line from actor's current position (if known)
            Vector3 startPos = Vector3.zero;
            bool hasStart = false;
            if (!string.IsNullOrEmpty(action.actorId) && actorPositions.TryGetValue(action.actorId, out var aPos))
            {
                startPos = new Vector3(aPos.x, aPos.y, 0);
                hasStart = true;
            }

            // Draw path lines
            Handles.color = isSelected ? PathColor : InactivePathColor;
            float lineWidth = isSelected ? 3f : 1.5f;

            if (hasStart && action.waypoints.Count > 0)
            {
                Vector3 firstWp = new Vector3(action.waypoints[0].position.x, action.waypoints[0].position.y, 0);
                Handles.DrawLine(startPos, firstWp, lineWidth);
            }

            for (int w = 0; w < action.waypoints.Count; w++)
            {
                var wp = action.waypoints[w];
                Vector3 wpPos = new Vector3(wp.position.x, wp.position.y, 0);

                // Connect consecutive waypoints
                if (w > 0)
                {
                    var prev = action.waypoints[w - 1];
                    Vector3 prevPos = new Vector3(prev.position.x, prev.position.y, 0);
                    Handles.DrawLine(prevPos, wpPos, lineWidth);
                }

                // Waypoint marker
                if (isSelected)
                {
                    // Full position handle for selected action's waypoints
                    Handles.color = WaypointColor;
                    Handles.DrawSolidDisc(wpPos, Vector3.forward, 0.12f);

                    EditorGUI.BeginChangeCheck();
                    var newWpPos = Handles.PositionHandle(wpPos, Quaternion.identity);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(_activeData, "Move Waypoint");
                        wp.position = new Vector2(newWpPos.x, newWpPos.y);
                        EditorUtility.SetDirty(_activeData);
                    }

                    // Waypoint label: index + face direction
                    string faceInfo = wp.face != FaceDirection.Auto ? $" [{wp.face}]" : "";
                    Handles.Label(wpPos + Vector3.up * 0.2f, $"W{w}{faceInfo}", GetSmallLabelStyle());
                }
                else
                {
                    // Small non-interactive marker
                    Handles.color = InactivePathColor;
                    Handles.DrawSolidDisc(wpPos, Vector3.forward, 0.05f);
                }
            }

            // Action label
            if (isSelected && action.waypoints.Count > 0)
            {
                Vector3 midpoint = new Vector3(action.waypoints[0].position.x, action.waypoints[0].position.y, 0);
                Handles.Label(midpoint + Vector3.up * 0.4f,
                    $"Move [{actionIndex}] {action.actorId} ({action.waypoints.Count} pts)",
                    GetLabelStyle());
            }
        }

        static void DrawActorLabels(Dictionary<string, Vector2> actorPositions)
        {
            foreach (var kvp in actorPositions)
            {
                Vector3 pos = new Vector3(kvp.Value.x, kvp.Value.y, 0);
                Handles.Label(pos + Vector3.down * 0.25f, kvp.Key, GetSmallLabelStyle());
            }
        }

        static GUIStyle _labelStyle;
        static GUIStyle GetLabelStyle()
        {
            if (_labelStyle == null)
            {
                _labelStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    normal = { textColor = Color.white },
                    fontSize = 11
                };
            }
            return _labelStyle;
        }

        static GUIStyle _smallLabelStyle;
        static GUIStyle GetSmallLabelStyle()
        {
            if (_smallLabelStyle == null)
            {
                _smallLabelStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    normal = { textColor = new Color(1f, 1f, 1f, 0.8f) },
                    fontSize = 9
                };
            }
            return _smallLabelStyle;
        }
    }
}
