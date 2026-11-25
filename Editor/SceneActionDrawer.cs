using UnityEditor;
using UnityEngine;

namespace Atthree.SceneLoading
{
    /// <summary>Shows only the fields the chosen action uses.</summary>
    [CustomPropertyDrawer(typeof(SceneAction))]
    public class SceneActionDrawer : PropertyDrawer
    {
        static float Line => EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!property.isExpanded) return Line;
            float h = Line * 3; // foldout, action, custom options
            var kind = (SceneActionKind)property.FindPropertyRelative("action").enumValueIndex;
            if (kind == SceneActionKind.LoadScene || kind == SceneActionKind.LoadGroup) h += Line;
            var custom = property.FindPropertyRelative("customOptions");
            if (custom.boolValue) h += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("options"), true) + 2;
            return h;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            var r = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            var action = property.FindPropertyRelative("action");
            var kind = (SceneActionKind)action.enumValueIndex;

            property.isExpanded = EditorGUI.Foldout(r, property.isExpanded, $"{label.text}: {Summary(property, kind)}", true);
            if (!property.isExpanded) { EditorGUI.EndProperty(); return; }

            EditorGUI.indentLevel++;
            r.y += Line;
            EditorGUI.PropertyField(r, action);

            if (kind == SceneActionKind.LoadScene)
            {
                r.y += Line;
                EditorGUI.PropertyField(r, property.FindPropertyRelative("scene"));
            }
            else if (kind == SceneActionKind.LoadGroup)
            {
                r.y += Line;
                EditorGUI.PropertyField(r, property.FindPropertyRelative("group"));
            }

            r.y += Line;
            var custom = property.FindPropertyRelative("customOptions");
            EditorGUI.PropertyField(r, custom, new GUIContent("Custom Options", custom.tooltip));
            if (custom.boolValue)
            {
                var options = property.FindPropertyRelative("options");
                options.isExpanded = true;
                r.y += Line;
                r.height = EditorGUI.GetPropertyHeight(options, true);
                EditorGUI.PropertyField(r, options, new GUIContent("Options"), true);
            }
            EditorGUI.indentLevel--;
            EditorGUI.EndProperty();
        }

        static string Summary(SerializedProperty p, SceneActionKind kind)
        {
            switch (kind)
            {
                case SceneActionKind.LoadScene:
                    var n = p.FindPropertyRelative("scene").FindPropertyRelative("name").stringValue;
                    return string.IsNullOrEmpty(n) ? "Load (no scene)" : $"Load {n}";
                case SceneActionKind.LoadGroup:
                    var g = p.FindPropertyRelative("group").objectReferenceValue;
                    return g != null ? $"Load {g.name}" : "Load (no group)";
                default:
                    return ObjectNames.NicifyVariableName(kind.ToString());
            }
        }
    }
}
