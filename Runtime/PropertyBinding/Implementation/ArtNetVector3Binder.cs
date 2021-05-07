using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UltraCombos.ArtNet
{
    public class ArtNetVector3Binder : ArtNetBinderBase
    {
        public Vector2 m_XMinMax = new Vector2(0, 1);
        public Vector2 m_YMinMax = new Vector2(0, 1);
        public Vector2 m_ZMinMax = new Vector2(0, 1);

        public override System.Type FieldType => typeof(Vector3);

        public override void UpdateBinding(byte[] data)
        {
            int index = m_Channel - 1;
            if (index < data.Length - 2)
            {
                float x = Mathf.Lerp(m_XMinMax.x, m_XMinMax.y, (float)data[index + 0] / byte.MaxValue);
                float y = Mathf.Lerp(m_YMinMax.x, m_YMinMax.y, (float)data[index + 1] / byte.MaxValue);
                float z = Mathf.Lerp(m_ZMinMax.x, m_ZMinMax.y, (float)data[index + 2] / byte.MaxValue);
                field.SetValue(obj, new Vector3(x, y, z));
            }
        }
    }
}
