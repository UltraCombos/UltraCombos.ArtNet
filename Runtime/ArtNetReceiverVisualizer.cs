using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UltraCombos.ArtNet
{
    [RequireComponent(typeof(ArtNetReceiver))]
    public class ArtNetReceiverVisualizer : MonoBehaviour
    {
        ArtNetReceiver receiver;

        public bool m_DrawDebug = false;
        [Range(0, 5)]
        public float m_DebugScale = 1;
        GUIStyle barStyle;
        GUIStyle buttonStyle;
        Color baseColor;
        Color deactiveColor;
        Color activeColor;
        Texture2D tex;
        short displayUniverse = -1;

        private void Awake()
        {
            receiver = GetComponent<ArtNetReceiver>();

            baseColor = new Color32(230, 158, 160, 200);
            deactiveColor = new Color32(128, 128, 128, 200);
            activeColor = new Color32(141, 162, 216, 255);
        }

        private void OnGUI()
        {
            if (m_DrawDebug == false || m_DebugScale < Mathf.Epsilon)
                return;

            if (displayUniverse < 0)
            {
                if (receiver.Data.Count > 0)
                {
                    displayUniverse = receiver.Data.Keys.First();
                }
                else
                {
                    return;
                }
            }

            if (tex == null)
            {
                tex = Texture2D.whiteTexture;
            }

            if (barStyle == null)
            {
                barStyle = new GUIStyle();
                barStyle.normal.textColor = Color.black;
                barStyle.alignment = TextAnchor.MiddleCenter;
            }

            if (buttonStyle == null)
            {
                buttonStyle = new GUIStyle(GUI.skin.button);
                buttonStyle.fontStyle = FontStyle.Bold;
                buttonStyle.normal.background = tex;
                buttonStyle.hover.background = tex;
                buttonStyle.active.background = tex;
            }

            var mtx = GUI.matrix;

            GUI.matrix = Matrix4x4.TRS(new Vector3(30, 30, 0), Quaternion.identity, Vector3.one * m_DebugScale);

            using (var vScp = new GUILayout.VerticalScope())
            {
                using (var hScp = new GUILayout.HorizontalScope())
                {
                    foreach (var universe in receiver.Data.Keys)
                    {
                        GUI.backgroundColor = displayUniverse == universe ? baseColor : deactiveColor;
                        if (GUILayout.Button($"Universe {universe}", buttonStyle, GUILayout.Width(100), GUILayout.Height(40)))
                        {
                            displayUniverse = universe;
                        }
                    }
                }

                GUI.matrix = Matrix4x4.Translate(new Vector3(0, 50 * m_DebugScale, 0)) * GUI.matrix;


                const int column = 32;
                const int channels = 512;
                var size = new Vector2(30, 30);
                float gap = 1.1f;
                for (int i = 0; i < channels; ++i)
                {
                    float x = i % column * size.x * gap;
                    float y = i / column * size.y * gap;
                    var position = new Rect(x, y, size.x, size.y);
                    float width = size.x;
                    float height = size.y * receiver.Data[displayUniverse].data[i] / 255.0f;
                    y += size.y - height;
                    var bar = new Rect(x, y, width, height);
                    GUI.DrawTexture(position, tex, ScaleMode.StretchToFill, true, 1, baseColor, 0, 0);
                    GUI.DrawTexture(bar, tex, ScaleMode.StretchToFill, false, 1, activeColor, 0, 0);
                    GUI.Label(position, $"{i:D3}", barStyle);
                }
            }

            GUI.matrix = mtx;


        }
    }

}
