using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UltraCombos.ArtNet
{
    [ExecuteAlways]
    [RequireComponent(typeof(ArtNetReceiver))]
    public class ArtNetPropertyBinder : MonoBehaviour
    {
        ArtNetReceiver receiver;
        public List<ArtNetBinderBase> m_PropertyBindings = new List<ArtNetBinderBase>();

        private void OnEnable()
        {
            Reload();
        }

        private void OnValidate()
        {
            Reload();
        }

        private void Reload()
        {
            receiver = GetComponent<ArtNetReceiver>();

            m_PropertyBindings = new List<ArtNetBinderBase>();
            m_PropertyBindings.AddRange(gameObject.GetComponents<ArtNetBinderBase>());
        }

        private void Update()
        {
            foreach (var bind in m_PropertyBindings)
            {
                if (receiver.Data.ContainsKey(bind.m_Universe))
                {
                    bind.UpdateBinding(receiver.Data[bind.m_Universe].data);
                }
            }
        }

    }
}
