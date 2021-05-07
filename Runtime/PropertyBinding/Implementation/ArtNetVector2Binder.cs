using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UltraCombos.ArtNet
{
    public class ArtNetVector2Binder : ArtNetBinderBase
    {
        public Vector2 m_XMinMax = new Vector2(0, 1);
        public Vector2 m_YMinMax = new Vector2(0, 1);

        public override System.Type FieldType => typeof(Vector2);

        public override void UpdateBinding(byte[] data)
        {
            int index = m_Channel - 1;
            if (index < data.Length - 1)
            {
                float x = Mathf.Lerp(m_XMinMax.x, m_XMinMax.y, (float)data[index + 0] / byte.MaxValue);
                float y = Mathf.Lerp(m_YMinMax.x, m_YMinMax.y, (float)data[index + 1] / byte.MaxValue);
                field.SetValue(obj, new Vector2(x, y));
            }
        }
    }
}
