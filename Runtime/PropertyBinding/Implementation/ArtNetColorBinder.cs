using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UltraCombos.ArtNet
{
    public class ArtNetColorBinder : ArtNetBinderBase
    {
        public enum ColorMode
        {
            RGB,
            CMY,
        }

        public ColorMode mode;

        public override System.Type FieldType => typeof(Color);

        public override void UpdateBinding(byte[] data)
        {
            int index = m_Channel - 1;
            int padding = 0;

            switch (mode)
            {
                case ColorMode.RGB:
                case ColorMode.CMY:
                    padding = 3;
                    break;
            }

            if (index < data.Length - padding)
            {
                var values = new float[padding];
                for (int i = 0; i < values.Length; ++i)
                {
                    values[i] = (float)data[index + i] / byte.MaxValue;
                }

                Color color = Color.black;
                switch (mode)
                {
                    case ColorMode.RGB: color = new Color(values[0], values[1], values[2]); break;
                    case ColorMode.CMY: color = new Color(1 - values[0], 1 - values[1], 1 - values[2]); break;
                }

                field.SetValue(obj, color);
            }
        }
    }
}
