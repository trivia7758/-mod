using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using CaoCao.Story;

namespace CaoCao.Editor
{
    /// <summary>
    /// Custom Inspector for StorySceneData assets.
    /// Provides a ReorderableList of actions with color-coding,
    /// a property inspector for the selected action, and an Add dropdown.
    /// </summary>
    [CustomEditor(typeof(StorySceneData))]
    public class StorySceneDataEditor : UnityEditor.Editor
    {
        SerializedProperty _actorsProp;
        SerializedProperty _actionsProp;
        ReorderableList _actionsList;
        int _selectedIndex = -1;

        // Cache action types for the Add menu
        static readonly (string label, Type type)[] ActionTypes = new[]
        {
            ("Set Position",     typeof(SetPositionAction)),
            ("Move To",          typeof(MoveToAction)),
            ("Face Direction",   typeof(FaceDirectionAction)),
            ("Talk",             typeof(TalkAction)),
            ("Choice",           typeof(ChoiceAction)),
            ("Set Background",   typeof(SetBackgroundAction)),
            ("Set Location",     typeof(SetLocationAction)),
            ("Wait",             typeof(WaitAction)),
            ("Hide Dialogue",    typeof(HideDialogueAction)),
            ("Set Morality",     typeof(SetMoralityAction)),
            ("Label",            typeof(LabelAction)),
            ("Goto",             typeof(GotoAction)),
        };

        // Color coding by category
        static Color GetActionColor(StoryActionBase action)
        {
            return action switch
            {
                SetPositionAction or MoveToAction => new Color(0.4f, 0.6f, 1f, 0.3f),      // Blue - movement
                FaceDirectionAction => new Color(0.4f, 0.6f, 1f, 0.2f),                     // Light blue
                TalkAction => new Color(0.3f, 0.8f, 0.4f, 0.3f),                            // Green - dialogue
                ChoiceAction => new Color(1f, 0.85f, 0.3f, 0.3f),                           // Yellow - branching
                LabelAction or GotoAction => new Color(0.7f, 0.7f, 0.7f, 0.3f),             // Gray - flow control
                SetBackgroundAction or SetLocationAction => new Color(0.8f, 0.5f, 1f, 0.2f), // Purple - scene
                _ => new Color(1f, 1f, 1f, 0.1f)                                            // Default
            };
        }

        void OnEnable()
        {
            _actorsProp = serializedObject.FindProperty("actors");
            _actionsProp = serializedObject.FindProperty("actions");
            SetupActionsList();
        }

        void SetupActionsList()
        {
            _actionsList = new ReorderableList(serializedObject, _actionsProp, true, true, false, false);

            _actionsList.drawHeaderCallback = rect =>
            {
                EditorGUI.LabelField(rect, $"Actions ({_actionsProp.arraySize})");
            };

            _actionsList.elementHeightCallback = index =>
            {
                return EditorGUIUtility.singleLineHeight + 4f;
            };

            _actionsList.drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                if (index >= _actionsProp.arraySize) return;

                var element = _actionsProp.GetArrayElementAtIndex(index);
                var action = element.managedReferenceValue as StoryActionBase;

                // Background color
                var bgRect = new Rect(rect.x - 4, rect.y, rect.width + 8, rect.height);
                if (action != null)
                    EditorGUI.DrawRect(bgRect, GetActionColor(action));

                // Index + display name
                var labelRect = new Rect(rect.x, rect.y + 2, rect.width, EditorGUIUtility.singleLineHeight);
                string label = action != null ? $"[{index}] {action.DisplayName}" : $"[{index}] (null)";
                EditorGUI.LabelField(labelRect, label);
            };

            _actionsList.onSelectCallback = list =>
            {
                _selectedIndex = list.index;
                StorySceneViewEditor.SetSelectedActionIndex(_selectedIndex);
                SceneView.RepaintAll();
            };
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Actors section
            EditorGUILayout.PropertyField(_actorsProp, new GUIContent("Actors"), true);
            EditorGUILayout.Space(10);

            // Action list
            _actionsList.DoLayoutList();

            // Add / Delete buttons
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("+ Add Action", GUILayout.Height(24)))
            {
                ShowAddActionMenu();
            }
            GUI.enabled = _selectedIndex >= 0 && _selectedIndex < _actionsProp.arraySize;
            if (GUILayout.Button("Duplicate", GUILayout.Width(80), GUILayout.Height(24)))
            {
                DuplicateAction(_selectedIndex);
            }
            if (GUILayout.Button("Delete", GUILayout.Width(60), GUILayout.Height(24)))
            {
                DeleteAction(_selectedIndex);
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // Property inspector for selected action
            DrawSelectedActionInspector();

            serializedObject.ApplyModifiedProperties();
        }

        void ShowAddActionMenu()
        {
            var menu = new GenericMenu();
            foreach (var (label, type) in ActionTypes)
            {
                menu.AddItem(new GUIContent(label), false, () => AddAction(type));
            }
            menu.ShowAsContext();
        }

        void AddAction(Type actionType)
        {
            Undo.RecordObject(target, "Add Story Action");
            int insertAt = _selectedIndex >= 0 ? _selectedIndex + 1 : _actionsProp.arraySize;
            _actionsProp.InsertArrayElementAtIndex(insertAt);
            var element = _actionsProp.GetArrayElementAtIndex(insertAt);
            element.managedReferenceValue = Activator.CreateInstance(actionType);
            serializedObject.ApplyModifiedProperties();
            _selectedIndex = insertAt;
            _actionsList.index = insertAt;
            EditorUtility.SetDirty(target);
        }

        void DuplicateAction(int index)
        {
            if (index < 0 || index >= _actionsProp.arraySize) return;

            var source = _actionsProp.GetArrayElementAtIndex(index).managedReferenceValue as StoryActionBase;
            if (source == null) return;

            Undo.RecordObject(target, "Duplicate Story Action");

            // Deep copy via JSON
            string json = JsonUtility.ToJson(source);
            var clone = Activator.CreateInstance(source.GetType()) as StoryActionBase;
            JsonUtility.FromJsonOverwrite(json, clone);

            int insertAt = index + 1;
            _actionsProp.InsertArrayElementAtIndex(insertAt);
            var element = _actionsProp.GetArrayElementAtIndex(insertAt);
            element.managedReferenceValue = clone;
            serializedObject.ApplyModifiedProperties();
            _selectedIndex = insertAt;
            _actionsList.index = insertAt;
            EditorUtility.SetDirty(target);
        }

        void DeleteAction(int index)
        {
            if (index < 0 || index >= _actionsProp.arraySize) return;

            Undo.RecordObject(target, "Delete Story Action");
            _actionsProp.DeleteArrayElementAtIndex(index);
            serializedObject.ApplyModifiedProperties();

            if (_selectedIndex >= _actionsProp.arraySize)
                _selectedIndex = _actionsProp.arraySize - 1;
            _actionsList.index = _selectedIndex;
            EditorUtility.SetDirty(target);
        }

        void DrawSelectedActionInspector()
        {
            if (_selectedIndex < 0 || _selectedIndex >= _actionsProp.arraySize) return;

            var element = _actionsProp.GetArrayElementAtIndex(_selectedIndex);
            var action = element.managedReferenceValue as StoryActionBase;
            if (action == null) return;

            EditorGUILayout.LabelField($"Edit: [{_selectedIndex}] {action.DisplayName}", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            // Draw all serialized fields of the selected action
            EditorGUI.indentLevel++;
            var iter = element.Copy();
            var endProp = element.GetEndProperty();
            bool enterChildren = true;
            while (iter.NextVisible(enterChildren))
            {
                if (SerializedProperty.EqualContents(iter, endProp)) break;
                enterChildren = false;
                EditorGUILayout.PropertyField(iter, true);
            }
            EditorGUI.indentLevel--;
        }

        // Handle keyboard shortcuts
        void OnSceneGUI()
        {
            var e = Event.current;
            if (e.type == EventType.KeyDown)
            {
                if (e.keyCode == KeyCode.Delete || e.keyCode == KeyCode.Backspace)
                {
                    if (_selectedIndex >= 0)
                    {
                        DeleteAction(_selectedIndex);
                        e.Use();
                    }
                }
                else if (e.keyCode == KeyCode.D && e.control)
                {
                    if (_selectedIndex >= 0)
                    {
                        DuplicateAction(_selectedIndex);
                        e.Use();
                    }
                }
            }
        }
    }
}
