using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace UltraCombos.ArtNet
{
    [ExecuteAlways]
    public class ArtNetBinder : MonoBehaviour
    {
        [Range(1, 512)]
        public short m_Universe = 1;
        [Range(1, 512)]
        public short m_Channel = 1;
        public GameObject m_Target = null;

        public string m_TypeName;
        string lastObjectName;
        public string m_MemberName;
        string lastMemberName;

        protected object obj = null;
        protected MemberInfo member = null;

        public System.Type MemberType => GetType(member);
        public int Padding => paddings[MemberType];

        public Vector2 m_Value1Range = new Vector2(0, 1);
        public Vector2 m_Value2Range = new Vector2(0, 1);
        public Vector2 m_Value3Range = new Vector2(0, 1);
        public Vector2 m_Value4Range = new Vector2(0, 1);

        Dictionary<System.Type, int> paddings = new Dictionary<System.Type, int>()
        {
            { typeof(float), 1 },
            { typeof(Vector2), 2 },
            { typeof(Vector3), 3 },
            { typeof(Vector4), 4 },
            { typeof(Color), 3 },
            { typeof(Quaternion), 3 },
        };

        public List<System.Type> ValidTypes => paddings.Keys.ToList();

        protected virtual void OnEnable()
        {
            ArtNetReceiver.Instance.onDataUpdated.AddListener(OnDataUpdated);
        }

        protected virtual void OnDisable()
        {
            ArtNetReceiver.Instance.onDataUpdated.RemoveListener(OnDataUpdated);
        }

        private void Update()
        {
            if (IsValid())
            {
                return;
            }

            if (m_Target is GameObject)
            {
                var go = m_Target as GameObject;

                var comp = System.Array.Find(go.GetComponents<Component>(), c => c.GetType().Name == m_TypeName);
                if (comp != null)
                {
                    var members = GetMembers(comp.GetType());
                    var member = System.Array.Find(members, m => m.Name == m_MemberName);
                    if (member != null)
                    {
                        this.member = member;
                        obj = comp;
                        /*
                        field.GetCustomAttributes(typeof(RangeAttribute)).ToList().ForEach(attr =>
                        {
                            var range = attr as RangeAttribute;
                            m_MinMax.Set(range.min, range.max);
                        });
                        */
                        lastObjectName = m_TypeName;
                        lastMemberName = m_MemberName;
                        
                    }
                }
            }
        }

        public static MemberInfo[] GetMembers(System.Type type)
        {
            var flag = BindingFlags.Instance | BindingFlags.Public;
            var members = new List<MemberInfo>();
            members.AddRange(type.GetProperties(flag));
            members.AddRange(type.GetFields(flag));
            return members.ToArray();
        }

        public static System.Type GetType(MemberInfo info)
        {
            System.Type res = null;
            if (info != null)
            {
                switch (info.MemberType)
                {
                    case MemberTypes.Property: res = (info as PropertyInfo).PropertyType; break;
                    case MemberTypes.Field: res = (info as FieldInfo).FieldType; break;
                }
            }            
            return res;
        }

        public virtual bool IsValid()
        {
            if (string.IsNullOrEmpty(m_TypeName) || lastObjectName != m_TypeName)
            {
                obj = null;
            }
            if (string.IsNullOrEmpty(m_MemberName) || lastMemberName != m_MemberName)
            {
                member = null;
            }
            return obj != null && member != null;
        }


        private void OnDataUpdated()
        {
            var receiver = ArtNetReceiver.Instance;
            if (receiver.Data.ContainsKey(m_Universe) && IsValid())
            {
                UpdateBinding(receiver.Data[m_Universe].data);
            }
        }

        private void UpdateBinding(byte[] data)
        {
            var type = MemberType;
            int index = m_Channel - 1;
            int padding = Padding;
            var values = new float[padding];
            var minmax = new Vector2[4] { m_Value1Range, m_Value2Range, m_Value3Range, m_Value4Range };
            object value = null;

            if (index + padding > data.Length)
                return;

            for (int i = 0; i < padding; ++i)
            {
                if (type == typeof(Color))
                {
                    values[i] = (float)data[index + i] / byte.MaxValue;
                }
                else
                {
                    values[i] = Mathf.Lerp(minmax[i].x, minmax[i].y, (float)data[index + i] / byte.MaxValue);
                }
                    
            }

            if (type == typeof(float))
            {
                value = values[0];
            }
            else if (type == typeof(Vector2))
            {
                value = new Vector2(values[0], values[1]);
            }
            else if (type == typeof(Vector3))
            {
                value = new Vector3(values[0], values[1], values[2]);
            }
            else if (type == typeof(Vector4))
            {
                value = new Vector4(values[0], values[1], values[2], values[3]);
            }
            else if (type == typeof(Color))
            {
                value = new Color(values[0], values[1], values[2]);
            }
            else if (type == typeof(Quaternion))
            {
                value = Quaternion.Euler(values[0], values[1], values[2]);
            }

            if (value != null)
            {
                if (member.MemberType == MemberTypes.Field)
                {
                    (member as FieldInfo).SetValue(obj, value);
                }
                else if (member.MemberType == MemberTypes.Property)
                {
                    (member as PropertyInfo).SetValue(obj, value);
                }
            }
            
        }
    }
}
