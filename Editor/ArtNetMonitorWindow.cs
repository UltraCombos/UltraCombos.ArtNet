using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UltraCombos.ArtNet
{
    public class ArtNetMonitorWindow : EditorWindow
    {
        short displayUniverse = -1;
        Texture2D tex;
        GUIStyle barStyle;
        GUIStyle buttonStyle;

        Color baseColor;
        Color deactiveColor;
        Color activeColor;

        [MenuItem("Window/ArtNet Monitor")]
        public static void ShowWindow()
        {
            GetWindow<ArtNetMonitorWindow>("ArtNet Monitor");
            //GetWindowWithRect<ArtNetMonitorWindow>(new Rect(0, 0, 1120, 630), true, "ArtNet Monitor");
        }

        private void OnEnable()
        {
            ArtNetReceiver.Instance.onDataUpdated.AddListener(OnDataUpdated);
        }

        private void OnDisable()
        {
            ArtNetReceiver.Instance.onDataUpdated.RemoveListener(OnDataUpdated);
        }

        private void OnDataUpdated()
        {
            Repaint();
        }


        private void OnGUI()
        {
            var receiver = ArtNetReceiver.Instance;

            if (receiver.Data.ContainsKey(displayUniverse) == false)
            {
                displayUniverse = -1;
            }

            if (displayUniverse < 0)
            {
                if (receiver.Data.Count > 0)
                {
                    displayUniverse = receiver.Data.Keys.First();
                    
                    baseColor = new Color32(217, 137, 119, 255);
                    deactiveColor = new Color32(128, 128, 128, 255);
                    activeColor = new Color32(217, 171, 154, 255);
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
            float dim = position.width / 37;
            barStyle.fontSize = Mathf.FloorToInt(dim * 0.4f);

            GUI.matrix = Matrix4x4.TRS(new Vector3(dim, dim, 0), Quaternion.identity, Vector3.one);

            using (var vScp = new GUILayout.VerticalScope())
            {
                using (var hScp = new GUILayout.HorizontalScope())
                {
                    GUI.backgroundColor = Color.black;
                    if (GUILayout.Button("Reload", buttonStyle, GUILayout.Width(60), GUILayout.Height(40)))
                    {
                        receiver.onDataUpdated.RemoveListener(OnDataUpdated);
                        receiver.onDataUpdated.AddListener(OnDataUpdated);
                    }

                    foreach (var universe in receiver.Data.Keys)
                    {
                        GUI.backgroundColor = displayUniverse == universe ? baseColor : deactiveColor;
                        if (GUILayout.Button($"Universe {universe}", buttonStyle, GUILayout.Width(100), GUILayout.Height(40)))
                        {
                            displayUniverse = universe;
                        }
                    }
                }

                GUI.matrix = Matrix4x4.Translate(new Vector3(0, 50, 0)) * GUI.matrix;

                const int column = 32;
                const int channels = 512;

                var size = new Vector2(dim, dim);
                float gap = 1.1f;
                for (int i = 0; i < channels; ++i)
                {
                    float x = i % column * size.x * gap;
                    float y = i / column * size.y * gap;
                    var channelPos = new Rect(x, y, size.x, size.y);
                    float width = size.x;
                    float height = size.y * receiver.Data[displayUniverse].data[i] / 255.0f;
                    y += size.y - height;
                    var valuePos = new Rect(x, y, width, height);
                    GUI.DrawTexture(channelPos, tex, ScaleMode.StretchToFill, true, 1, baseColor, 0, 0);
                    GUI.DrawTexture(valuePos, tex, ScaleMode.StretchToFill, false, 1, activeColor, 0, 0);
                    GUI.Label(channelPos, $"{i:D3}", barStyle);
                }
            }

            GUI.matrix = mtx;


        }
    }
}
