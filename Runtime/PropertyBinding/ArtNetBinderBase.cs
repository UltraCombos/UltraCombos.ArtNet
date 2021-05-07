using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace UltraCombos.ArtNet
{
    [ExecuteAlways]
    public abstract class ArtNetBinderBase : MonoBehaviour
    {
        [Range(1, 512)]
        public short m_Universe = 1;
        [Range(1, 512)]
        public short m_Channel = 1;
        public GameObject m_Target = null;
        [BindObject]
        public string m_TypeName;
        string lastObjectName;
        [BindField]
        public string m_FieldName;
        string lastFieldName;

        protected object obj = null;
        protected FieldInfo field = null;

        public abstract System.Type FieldType { get; }

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
                    var flag = BindingFlags.Instance | BindingFlags.Public;
                    var f = System.Array.Find(comp.GetType().GetFields(flag), f => f.Name == m_FieldName);
                    if (f != null)
                    {
                        field = f;
                        obj = comp;
                        /*
                        field.GetCustomAttributes(typeof(RangeAttribute)).ToList().ForEach(attr =>
                        {
                            var range = attr as RangeAttribute;
                            m_MinMax.Set(range.min, range.max);
                        });
                        */
                        lastObjectName = m_TypeName;
                        lastFieldName = m_FieldName;
                    }
                }
            }
        }

        public virtual bool IsValid()
        {
            if (string.IsNullOrEmpty(m_TypeName) || lastObjectName != m_TypeName)
            {
                obj = null;
            }
            if (string.IsNullOrEmpty(m_FieldName) || lastFieldName != m_FieldName)
            {
                field = null;
            }
            return obj != null && field != null;
        }


        private void OnDataUpdated()
        {
            var receiver = ArtNetReceiver.Instance;
            if (receiver.Data.ContainsKey(m_Universe) && IsValid())
            {
                UpdateBinding(receiver.Data[m_Universe].data);
            }
        }

        public abstract void UpdateBinding(byte[] data);
    }
}
