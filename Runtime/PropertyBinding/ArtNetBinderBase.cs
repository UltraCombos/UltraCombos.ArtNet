using System.Collections;
using System.Collections.Generic;
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

        protected object obj = null;
        protected FieldInfo field = null;

        protected virtual void OnEnable()
        {
            ArtNetReceiver.Instance.onDataUpdated.AddListener(OnDataUpdated);
        }

        protected virtual void OnDisable()
        {
            ArtNetReceiver.Instance.onDataUpdated.RemoveListener(OnDataUpdated);
        }
        
        public abstract bool IsValid();


        public void OnDataUpdated()
        {
            var receiver = ArtNetReceiver.Instance;
            if (receiver.Data.ContainsKey(m_Universe))
            {
                UpdateBinding(receiver.Data[m_Universe].data);
            }
        }

        public abstract void UpdateBinding(byte[] data);
    }
}
