using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace UltraCombos.ArtNet
{
    [CustomPropertyDrawer(typeof(BindFieldAttribute))]
    public class BindFieldDrawer : SelectedDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            using (new EditorGUI.PropertyScope(position, label, property))
            {
                BindFieldAttribute attribute = (BindFieldAttribute)base.attribute;

                var binder = property.serializedObject.targetObject as ArtNetFloatBinder;
                if (string.IsNullOrEmpty(binder.m_TypeName))
                {
                    OnInvalid(position, property, "Select object name first.");
                }
                else if (binder.m_Target is GameObject)
                {
                    var go = binder.m_Target as GameObject;
                    var comp = System.Array.Find(go.GetComponents<Component>(), c => c.GetType().Name == binder.m_TypeName);
                    if (comp == null)
                    {
                        OnInvalid(position, property, "Component is null.");
                    }
                    else
                    {
                        var flag = BindingFlags.Instance | BindingFlags.Public;
                        var fields = comp.GetType().GetFields(flag);

                        if (fields.Length == 0)
                        {
                            OnInvalid(position, property, "No valid field is founded.");
                        }
                        else
                        {
                            var selections = from field in fields select field.Name;
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
                }
                else
                {
                    OnInvalid(position, property, "Object type is not supported.");
                }
            }
        }
    }
}
