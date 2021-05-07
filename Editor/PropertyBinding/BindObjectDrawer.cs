using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UltraCombos.ArtNet
{
    [CustomPropertyDrawer(typeof(BindObjectAttribute))]
    public class BindObjectDrawer : SelectedDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            using (new EditorGUI.PropertyScope(position, label, property))
            {
                BindObjectAttribute attribute = (BindObjectAttribute)base.attribute;

                var binder = property.serializedObject.targetObject as ArtNetFloatBinder;
                if (binder.m_Target == null)
                {
                    OnInvalid(position, property, "Object is not assigned.");
                }
                else if (binder.m_Target is GameObject)
                {
                    var components = (binder.m_Target as GameObject).GetComponents<Component>();
                    if (components.Length == 0)
                    {
                        OnInvalid(position, property, "No component is founded.");
                    }
                    else
                    {
                        var selections = from comp in components select comp.GetType().Name;
                        var options = selections.ToList();
                        attribute.selected = options.IndexOf(property.stringValue);
                        attribute.selected = Mathf.Clamp(attribute.selected, 0, options.Count - 1);

                        using (var check = new EditorGUI.ChangeCheckScope())
                        {
                            attribute.selected = EditorGUI.Popup(position, label.text, attribute.selected, options.ToArray());

                            if (check.changed)
                            {
                                property.stringValue = options[attribute.selected];
                            }
                        }
                    }
                }
                else
                {
                    OnInvalid(position, property, "Object type is not supported.");
                }
            }
        }
    }
}
