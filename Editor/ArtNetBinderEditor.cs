using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace UltraCombos.ArtNet
{
    [CustomEditor(typeof(ArtNetBinder))]
    public class ArtNetBinderEditor : Editor
    {
        SerializedProperty m_Universe;
        SerializedProperty m_Channel;
        SerializedProperty m_Target;
        SerializedProperty m_TypeName;
        SerializedProperty m_MemberName;
        SerializedProperty m_Value1Range;
        SerializedProperty m_Value2Range;
        SerializedProperty m_Value3Range;
        SerializedProperty m_Value4Range;

        ArtNetBinder binder;

        private void OnEnable()
        {
            binder = target as ArtNetBinder;

            m_Universe = serializedObject.FindProperty(nameof(m_Universe));
            m_Channel = serializedObject.FindProperty(nameof(m_Channel));
            m_Target = serializedObject.FindProperty(nameof(m_Target));
            m_TypeName = serializedObject.FindProperty(nameof(m_TypeName));
            m_MemberName = serializedObject.FindProperty(nameof(m_MemberName));
            m_Value1Range = serializedObject.FindProperty(nameof(m_Value1Range));
            m_Value2Range = serializedObject.FindProperty(nameof(m_Value2Range));
            m_Value3Range = serializedObject.FindProperty(nameof(m_Value3Range));
            m_Value4Range = serializedObject.FindProperty(nameof(m_Value4Range));
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(m_Universe);
            EditorGUILayout.PropertyField(m_Channel);
            EditorGUILayout.PropertyField(m_Target);
            DrawTypeName();
            DrawFieldName();
            DrawTypeProperties();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawTypeName()
        {
            if (binder.m_Target == null)
            {
                OnInvalid(m_TypeName, "Object is not assigned.");
            }
            else if (binder.m_Target is GameObject)
            {
                var components = (binder.m_Target as GameObject).GetComponents<Component>();
                if (components.Length == 0)
                {
                    OnInvalid(m_TypeName, "No component is founded.");
                }
                else
                {
                    var selections = from comp in components select comp.GetType().Name;
                    var options = selections.ToList();
                    int selected = options.IndexOf(m_TypeName.stringValue);
                    selected = Mathf.Clamp(selected, 0, options.Count - 1);

                    using (var check = new EditorGUI.ChangeCheckScope())
                    {
                        selected = EditorGUILayout.Popup(m_TypeName.displayName, selected, options.ToArray());

                        if (check.changed)
                        {
                            m_TypeName.stringValue = options[selected];
                        }
                    }
                }
            }
            else
            {
                OnInvalid(m_TypeName, "Object type is not supported.");
            }
        }

        private void DrawFieldName()
        {
            if (string.IsNullOrEmpty(binder.m_TypeName))
            {
                OnInvalid(m_MemberName, "Select member name first.");
            }
            else if (binder.m_Target is GameObject)
            {
                var go = binder.m_Target as GameObject;
                var comp = System.Array.Find(go.GetComponents<Component>(), c => c.GetType().Name == binder.m_TypeName);
                if (comp == null)
                {
                    OnInvalid(m_MemberName, "Component is null.");
                }
                else
                {
                    var members = from member in ArtNetBinder.GetMembers(comp.GetType())
                                  where binder.ValidTypes.Contains(ArtNetBinder.GetType(member))
                                  select member;

                    if (members.Count() == 0)
                    {
                        OnInvalid(m_MemberName, "No valid member is founded.");
                    }
                    else
                    {
                        var selections = from member in members select member.Name;
                        var options = selections.ToList();
                        int selected = options.IndexOf(m_MemberName.stringValue);
                        selected = Mathf.Clamp(selected, 0, options.Count - 1);

                        using (var check = new EditorGUI.ChangeCheckScope())
                        {
                            selected = EditorGUILayout.Popup(m_MemberName.displayName, selected, options.ToArray());

                            if (check.changed)
                            {
                                m_MemberName.stringValue = options[selected];
                            }
                        }
                    }
                }
            }
            else
            {
                OnInvalid(m_MemberName, "Member type is not supported.");
            }
        }

        private void DrawTypeProperties()
        {
            if (binder.MemberType == null)
            {

            }
            else if (binder.MemberType == typeof(float))
            {
                m_Value1Range.vector2Value = EditorGUILayout.Vector2Field("Min Max", m_Value1Range.vector2Value);
            }
            else if (binder.MemberType == typeof(Vector2))
            {
                m_Value1Range.vector2Value = EditorGUILayout.Vector2Field("X Min Max", m_Value1Range.vector2Value);
                m_Value2Range.vector2Value = EditorGUILayout.Vector2Field("Y Min Max", m_Value2Range.vector2Value);
            }
            else if (binder.MemberType == typeof(Vector3))
            {
                m_Value1Range.vector2Value = EditorGUILayout.Vector2Field("X Min Max", m_Value1Range.vector2Value);
                m_Value2Range.vector2Value = EditorGUILayout.Vector2Field("Y Min Max", m_Value2Range.vector2Value);
                m_Value3Range.vector2Value = EditorGUILayout.Vector2Field("Z Min Max", m_Value3Range.vector2Value);
            }
            else if (binder.MemberType == typeof(Vector4))
            {
                m_Value1Range.vector2Value = EditorGUILayout.Vector2Field("X Min Max", m_Value1Range.vector2Value);
                m_Value2Range.vector2Value = EditorGUILayout.Vector2Field("Y Min Max", m_Value2Range.vector2Value);
                m_Value3Range.vector2Value = EditorGUILayout.Vector2Field("Z Min Max", m_Value3Range.vector2Value);
                m_Value4Range.vector2Value = EditorGUILayout.Vector2Field("W Min Max", m_Value4Range.vector2Value);
            }
            else if (binder.MemberType == typeof(Quaternion))
            {
                m_Value1Range.vector2Value = EditorGUILayout.Vector2Field("X Min Max", m_Value1Range.vector2Value);
                m_Value2Range.vector2Value = EditorGUILayout.Vector2Field("Y Min Max", m_Value2Range.vector2Value);
                m_Value3Range.vector2Value = EditorGUILayout.Vector2Field("Z Min Max", m_Value3Range.vector2Value);
            }
        }

        private void OnInvalid(SerializedProperty property, string msg)
        {
            property.stringValue = "";
            using (var scp = new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.LabelField(property.displayName, msg);
            }
        }
    }
}
