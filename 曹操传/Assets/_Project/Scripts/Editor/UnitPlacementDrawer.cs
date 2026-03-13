using UnityEngine;
using UnityEditor;
using CaoCao.Story;

namespace CaoCao.Editor
{
    /// <summary>
    /// Custom PropertyDrawer for UnitPlacement — shows unitId as a dropdown
    /// instead of a free-text field.
    /// </summary>
    [CustomPropertyDrawer(typeof(UnitPlacement))]
    public class UnitPlacementDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            position = EditorGUI.PrefixLabel(position, label);

            float halfW = position.width * 0.45f;
            float spacing = position.width * 0.1f;

            var unitIdProp = property.FindPropertyRelative("unitId");
            var tileProp = property.FindPropertyRelative("tile");

            // Draw unitId as dropdown
            var dropdownRect = new Rect(position.x, position.y, halfW, position.height);
            string[] options = StoryUnitIds.All;
            int currentIdx = System.Array.IndexOf(options, unitIdProp.stringValue);
            if (currentIdx < 0) currentIdx = 0;

            int newIdx = EditorGUI.Popup(dropdownRect, currentIdx, options);
            unitIdProp.stringValue = options[newIdx];

            // Draw tile as Vector2Int
            var tileRect = new Rect(position.x + halfW + spacing, position.y, halfW, position.height);
            EditorGUI.PropertyField(tileRect, tileProp, GUIContent.none);

            EditorGUI.EndProperty();
        }
    }
}
