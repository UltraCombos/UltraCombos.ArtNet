using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace UltraCombos.ArtNet
{
    public class ArtNetFloatBinder : ArtNetBinderBase
    {
        
        public Vector2 m_MinMax = new Vector2(0, 1);

        public override Type FieldType => typeof(float);

        public override void UpdateBinding(byte[] data)
        {            
            int index = m_Channel - 1;
            if (index < data.Length)
            {
                float value = Mathf.Lerp(m_MinMax.x, m_MinMax.y, (float)data[index] / byte.MaxValue);
                field.SetValue(obj, value);
            }
        }
    }
}
