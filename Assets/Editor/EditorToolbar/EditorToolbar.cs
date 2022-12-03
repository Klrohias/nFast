#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental;
using UnityEngine.UIElements;

[InitializeOnLoad]
public static class EditorToolbar
{
    private static Type toolbarType = null;
    private static ScriptableObject lastToolbar = null;
    static EditorToolbar()
    {
        EditorApplication.update += OnUpdate;
         
        toolbarType = typeof(Editor).Assembly.GetType("UnityEditor.Toolbar");
    }

    private static void OnUpdate()
    {
        var toolbar = LocateToolbar();
        if(toolbar == lastToolbar) return;
        if (toolbar != null)
            InjectToolbarEvent(toolbar);
        lastToolbar = toolbar;
    }

    private static void InjectToolbarEvent(ScriptableObject toolbar)
    {
        FieldInfo root = toolbarType.GetField("m_Root", BindingFlags.NonPublic | BindingFlags.Instance);
        VisualElement concreteRoot = root.GetValue(toolbar) as VisualElement;

        VisualElement toolbarZone = concreteRoot.Q("ToolbarZoneRightAlign");
        VisualElement parent = new VisualElement()
        {
            style =
            {
                flexGrow = 1,
                flexDirection = FlexDirection.Row,
            }
        };
        IMGUIContainer container = new IMGUIContainer();
        container.onGUIHandler += OnGUI;
        parent.Add(container);
        toolbarZone.Add(parent);
    }
    private static ScriptableObject LocateToolbar()
    {
        UnityEngine.Object[] toolbars = Resources.FindObjectsOfTypeAll(toolbarType);
        return toolbars.Length > 0 ? (ScriptableObject)toolbars[0] : null;
    }

    private static void OnGUI()
    {
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("切换3D/2D"))
        {
            var view = SceneView.sceneViews[0] as SceneView;
            view.in2DMode = !view.in2DMode;
            view.orthographic = view.in2DMode;
        }
        if (GUILayout.Button("编辑GUI"))
        {
            var view = SceneView.sceneViews[0] as SceneView;
            view.in2DMode = true;
            view.orthographic = true;
            Selection.activeGameObject = GameObject.Find("Cavan♂s");
            SceneView.FrameLastActiveSceneView();
        }

        GUILayout.TextArea("夹带私货：");
        if (GUILayout.Button("嘿嘿...狗带..."))
        {
            Debug.Log("狗带tql！");
        }
        if (GUILayout.Button("关注inokana喵"))
        {
            Application.OpenURL("https://space.bilibili.com/506856077");
        }
        GUILayout.EndHorizontal();
    }
}


#endif