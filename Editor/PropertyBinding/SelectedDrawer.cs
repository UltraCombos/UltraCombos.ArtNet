using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UltraCombos.ArtNet
{
    [CustomPropertyDrawer(typeof(SelectedAttributeBase))]
    public abstract class SelectedDrawer : PropertyDrawer
    {
        protected void OnInvalid(Rect position, SerializedProperty property, string msg)
        {
            (attribute as SelectedAttributeBase).selected = -1;
            property.stringValue = "";
            using (var scp = new EditorGUI.DisabledScope(true))
            {
                var rect = EditorGUI.PrefixLabel(position, new GUIContent(property.displayName));
                EditorGUI.LabelField(rect, msg);
            }
        }
    }
}
