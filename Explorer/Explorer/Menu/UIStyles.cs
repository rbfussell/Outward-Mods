﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Explorer
{
    public class UIStyles
    {
        public static GUISkin WindowSkin
        {
            get
            {
                if (_customSkin == null)
                {
                    try
                    {
                        _customSkin = CreateWindowSkin();
                    }
                    catch
                    {
                        _customSkin = GUI.skin;
                    }
                }

                return _customSkin;
            }
        }

        public static void HorizontalLine(Color color)
        {
            var c = GUI.color;
            GUI.color = color;
            GUILayout.Box(GUIContent.none, HorizontalBar);
            GUI.color = c;
        }

        private static GUISkin _customSkin;

        public static Texture2D m_nofocusTex;
        public static Texture2D m_focusTex;

        private static GUIStyle _horizBarStyle;

        private static GUIStyle HorizontalBar
        {
            get
            {
                if (_horizBarStyle == null)
                {
                    _horizBarStyle = new GUIStyle();
                    _horizBarStyle.normal.background = Texture2D.whiteTexture;
                    _horizBarStyle.margin = new RectOffset(0, 0, 4, 4);
                    _horizBarStyle.fixedHeight = 2;
                }

                return _horizBarStyle;
            }
        }

        private static GUISkin CreateWindowSkin()
        {
            var newSkin = Object.Instantiate(GUI.skin);
            Object.DontDestroyOnLoad(newSkin);

            m_nofocusTex = MakeTex(550, 700, new Color(0.1f, 0.1f, 0.1f, 0.7f));
            m_focusTex = MakeTex(550, 700, new Color(0.3f, 0.3f, 0.3f, 1f));

            newSkin.window.normal.background = m_nofocusTex;
            newSkin.window.onNormal.background = m_focusTex;

            newSkin.box.normal.textColor = Color.white;
            newSkin.window.normal.textColor = Color.white;
            newSkin.button.normal.textColor = Color.white;
            newSkin.textField.normal.textColor = Color.white;
            newSkin.label.normal.textColor = Color.white;

            return newSkin;
        }

        public static Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; ++i)
            {
                pix[i] = col;
            }
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }

        // *********************************** METHODS FOR DRAWING VALUES IN GUI ************************************

        // helper for "Instantiate" button on UnityEngine.Objects
        public static void InstantiateButton(Object obj, float width = 100)
        {
            if (GUILayout.Button("Instantiate", GUILayout.Width(width)))
            {
                var newobj = Object.Instantiate(obj);

                WindowManager.InspectObject(newobj, out _);
            }
        }

        // helper for drawing a styled button for a GameObject or Transform
        public static void GameobjButton(GameObject obj, Action<GameObject> specialInspectMethod = null, bool showSmallInspectBtn = true, float width = 380)
        {
            if (obj == null)
            {
                GUILayout.Label("<i><color=red>null</color></i>");
                return;
            }

            bool enabled = obj.activeSelf;
            bool children = obj.transform.childCount > 0;

            GUILayout.BeginHorizontal();
            GUI.skin.button.alignment = TextAnchor.UpperLeft;

            // ------ build name ------

            string label = children ? "[" + obj.transform.childCount + " children] " : "";
            label += obj.name;

            // ------ Color -------

            if (enabled)
            {
                if (children)
                {
                    GUI.color = Color.green;
                }
                else
                {
                    GUI.color = Global.LIGHT_GREEN;
                }
            }
            else
            {
                GUI.color = Global.LIGHT_RED;
            }

            // ------ toggle active button ------

            enabled = GUILayout.Toggle(enabled, "", GUILayout.Width(18));
            if (obj.activeSelf != enabled)
            {
                obj.SetActive(enabled);
            }

            // ------- actual button ---------

            if (GUILayout.Button(label, new GUILayoutOption[] { GUILayout.Height(22), GUILayout.Width(width) }))
            {
                if (specialInspectMethod != null)
                {
                    specialInspectMethod(obj);
                }
                else
                {
                    WindowManager.InspectObject(obj, out bool _);
                }
            }

            // ------ small "Inspect" button on the right ------

            GUI.skin.button.alignment = TextAnchor.MiddleCenter;
            GUI.color = Color.white;

            if (showSmallInspectBtn)
            {
                if (GUILayout.Button("Inspect"))
                {
                    WindowManager.InspectObject(obj, out bool _);
                }
            }

            GUILayout.EndHorizontal();
        }

        public static void DrawMember(ref object value, string valueType, string memberName, Rect rect, object setTarget = null, Action<object> setAction = null, float labelWidth = 180, bool autoSet = false)
        {
            GUILayout.Label("<color=cyan>" + memberName + ":</color>", GUILayout.Width(labelWidth));

            DrawValue(ref value, rect, valueType, memberName, setTarget, setAction, autoSet);
        }

        public static void DrawValue(ref object value, Rect rect, string nullValueType = null, string memberName = null, object setTarget = null, Action<object> setAction = null, bool autoSet = false)
        {
            if (value == null)
            {
                GUILayout.Label("<i>null (" + nullValueType + ")</i>");
            }
            else
            {
                var valueType = value.GetType();
                if (valueType.IsPrimitive || value.GetType() == typeof(string))
                {
                    DrawPrimitive(ref value, rect, setTarget, setAction);
                }
                else if (valueType == typeof(GameObject) || valueType == typeof(Transform))
                {
                    GameObject go;
                    if (value.GetType() == typeof(Transform))
                    {
                        go = (value as Transform).gameObject;
                    }
                    else
                    {
                        go = (value as GameObject);
                    }

                    UIStyles.GameobjButton(go, null, false, rect.width - 250);
                }
                else if (valueType.IsEnum)
                {
                    if (setAction != null)
                    {
                        if (GUILayout.Button("<", GUILayout.Width(25)))
                        {
                            SetEnum(ref value, -1);
                            setAction.Invoke(setTarget);
                        }
                        if (GUILayout.Button(">", GUILayout.Width(25)))
                        {
                            SetEnum(ref value, 1);
                            setAction.Invoke(setTarget);
                        }
                    }

                    GUILayout.Label(value.ToString());

                }
                else if (valueType.IsArray || ReflectionWindow.IsList(valueType))
                {
                    object[] m_array;
                    if (valueType.IsArray)
                    {
                        m_array = (value as Array).Cast<object>().ToArray();
                    }
                    else
                    {
                        m_array = (value as IEnumerable).Cast<object>().ToArray();
                    }

                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                    if (GUILayout.Button("<color=yellow>[" + m_array.Length + "] " + valueType + "</color>", GUILayout.MaxWidth(rect.width - 230)))
                    {
                        WindowManager.InspectObject(value, out bool _);
                    }
                    GUI.skin.button.alignment = TextAnchor.MiddleCenter;

                    for (int i = 0; i < m_array.Length; i++)
                    {
                        var obj = m_array[i];

                        // collapsing the BeginHorizontal called from ReflectionWindow.WindowFunction or previous array entry
                        GUILayout.EndHorizontal();
                        GUILayout.BeginHorizontal();
                        GUILayout.Space(190);

                        if (i > Explorer.ArrayLimit)
                        {
                            GUILayout.Label($"<i><color=red>{m_array.Length - Explorer.ArrayLimit} results omitted, array is too long!</color></i>");
                            break;
                        }

                        if (obj == null)
                        {
                            GUILayout.Label("<i><color=grey>null</color></i>");
                        }
                        else
                        {
                            var type = obj.GetType();
                            DrawMember(ref obj, type.ToString(), i.ToString(), rect, setTarget, setAction, 25, true);
                        }
                    }
                }
                else
                {
                    var label = value.ToString();
                    if (valueType == typeof(AnimatorControllerParameter))
                    {
                        var param = value as AnimatorControllerParameter;

                        label = param.name + " (" + param.type + ")";
                    }
                    GUI.skin.button.alignment = TextAnchor.MiddleLeft;
                    if (GUILayout.Button("<color=yellow>" + label + "</color>", GUILayout.MaxWidth(rect.width - 230)))
                    {
                        WindowManager.InspectObject(value, out bool _);
                    }
                    GUI.skin.button.alignment = TextAnchor.MiddleCenter;
                }
            }
        }

        // Helper for drawing primitive values (with Apply button)

        public static void DrawPrimitive(ref object value, Rect m_rect, object setTarget = null, Action<object> setAction = null, bool autoSet = false)
        {
            bool allowSet = setAction != null;

            if (value.GetType() == typeof(bool))
            {
                bool b = (bool)value;
                var color = "<color=" + (b ? "lime>" : "red>");

                if (allowSet)
                {
                    value = GUILayout.Toggle((bool)value, color + value.ToString() + "</color>");

                    if (b != (bool)value)
                    {
                        setAction.Invoke(setTarget);
                    }
                }
            }
            else
            {
                if (value.ToString().Length > 37)
                {
                    value = GUILayout.TextArea(value.ToString(), GUILayout.MaxWidth(m_rect.width - 260));
                }
                else
                {
                    value = GUILayout.TextField(value.ToString(), GUILayout.MaxWidth(m_rect.width - 260));
                }

                if (autoSet || (allowSet && GUILayout.Button("<color=#00FF00>Apply</color>", GUILayout.Width(60))))
                {
                    setAction.Invoke(setTarget);
                }
            }
        }

        // Helper for setting an enum

        public static void SetEnum(ref object value, int change)
        {
            var type = value.GetType();
            var names = Enum.GetNames(type).ToList();

            int newindex = names.IndexOf(value.ToString()) + change;

            if ((change < 0 && newindex >= 0) || (change > 0 && newindex < names.Count))
            {
                value = Enum.Parse(type, names[newindex]);
            }
        }
    }
}
