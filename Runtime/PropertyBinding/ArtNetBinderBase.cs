using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UltraCombos.ArtNet
{
    [ExecuteAlways]
    [RequireComponent(typeof(ArtNetPropertyBinder))]
    public abstract class ArtNetBinderBase : MonoBehaviour
    {
        [Range(1, 512)]
        public short m_Universe = 1;
        [Range(1, 512)]
        public short m_Channel = 1;

        protected ArtNetPropertyBinder binder;

        protected virtual void Awake()
        {
            binder = GetComponent<ArtNetPropertyBinder>();
        }

        protected virtual void OnEnable()
        {
            if (!binder.m_PropertyBindings.Contains(this))
                binder.m_PropertyBindings.Add(this);

            //hideFlags = HideFlags.HideInInspector; // Comment to debug
        }

        protected virtual void OnDisable()
        {
            if (binder.m_PropertyBindings.Contains(this))
                binder.m_PropertyBindings.Remove(this);
        }

        public abstract bool IsValid();

        public abstract void UpdateBinding(byte[] data);
    }
}
