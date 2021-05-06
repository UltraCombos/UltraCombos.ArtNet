using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace UltraCombos.ArtNet
{
    public class ArtNetFloatBinder : ArtNetBinderBase
    {
        public Object m_Target = null;
        public string m_ObjectName;
        public string m_FieldName;
        public Vector2 m_MinMax = new Vector2(0, 1);

        object obj = null;
        FieldInfo field = null;

        private void Update()
        {
            if (IsValid())
            {
                return;
            }

            if (m_Target is GameObject)
            {
                var go = m_Target as GameObject;

                var comp = System.Array.Find(go.GetComponents<Component>(), c => c.GetType().Name == m_ObjectName);
                if (comp != null)
                {
                    var flag = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public;
                    var f = System.Array.Find(comp.GetType().GetFields(flag), f => f.Name == m_FieldName);
                    if (f != null)
                    {
                        field = f;
                        obj = comp;

                        foreach (var attr in field.GetCustomAttributes(typeof(RangeAttribute)))
                        {
                            var range = attr as RangeAttribute;
                            m_MinMax.Set(range.min, range.max);
                        }
                    }
                }
            }
        }

        public override bool IsValid()
        {
            return obj != null && field != null;
        }

        public override void UpdateBinding(byte[] data)
        {
            if (IsValid() == false)
            {
                return;
            }
            
            int index = m_Channel - 1;
            if (index < data.Length)
            {
                float value = Mathf.Lerp(m_MinMax.x, m_MinMax.y, (float)data[index] / byte.MaxValue);
                field.SetValue(obj, value);
            }
        }
    }
}
