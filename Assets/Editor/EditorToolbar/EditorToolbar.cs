#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Klrohias.NFast.PhiGamePlay;
using Klrohias.NFast.Utilities;
using Newtonsoft.Json;
using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental;
using UnityEditor.SceneManagement;
using UnityEditor.SearchService;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Klrohias.UnityEditorToolbox
{

    [InitializeOnLoad]
    public static class EditorToolbar
    {
        private static Type toolbarType = null;
        private static ScriptableObject lastToolbar = null;
        static EditorToolbar()
        {
            EditorApplication.playModeStateChanged += playModeStateChanged;
            EditorApplication.update += OnUpdate;
            toolbarType = typeof(Editor).Assembly.GetType("UnityEditor.Toolbar");
        }
        private static void playModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
            }

            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                // EditorSceneManager.LoadSceneInPlayMode("Scenes/EntryScene",
                //     new LoadSceneParameters(LoadSceneMode.Single));
                if (EditorSceneManager.GetActiveScene().buildIndex != 0) EditorSceneManager.LoadScene(0);
                Debug.ClearDeveloperConsole();
            }
        }


        private static void OnUpdate()
        {
            var toolbar = LocateToolbar();
            if (toolbar == lastToolbar) return;
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
            if (GUILayout.Button("Switch 3D/2D"))
            {
                var view = SceneView.sceneViews[0] as SceneView;
                view.in2DMode = !view.in2DMode;
                view.orthographic = view.in2DMode;
            }
            if (GUILayout.Button("Edit GUI"))
            {
                var view = SceneView.sceneViews[0] as SceneView;
                view.in2DMode = true;
                view.orthographic = true;
                Selection.activeGameObject = GameObject.Find("Canvas");
                SceneView.FrameLastActiveSceneView();
            }

            if (GUILayout.Button("Toolbox"))
            {
                EditorWindow.GetWindow<EditorToolboxWindow>();
            }
            GUILayout.EndHorizontal();
        }
    }

    public class EditorToolboxWindow : EditorWindow
    {
        private void DumpObject(object obj) => JsonConvert.SerializeObject(obj, Formatting.Indented,
            new JsonSerializerSettings()
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            }).Log();
        private void OnGUI()
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("0.5x"))
                Time.timeScale = 0.5f;
            if (GUILayout.Button("1x"))
                Time.timeScale = 1f;
            if (GUILayout.Button("2x"))
                Time.timeScale = 2f;
            if (GUILayout.Button("4x"))
                Time.timeScale = 4f;
            if (GUILayout.Button("10x"))
                Time.timeScale = 10f;
            GUILayout.EndHorizontal();
            GUILayout.Label("Current: " + Time.timeScale);

            if (GUILayout.Button("Dump selected Note"))
            {
                var selectedObject =
                    Selection.activeTransform.gameObject;
                DumpObject(selectedObject.GetComponent<IPhiNoteWrapper>().Note);
            }
        }
    }
}

#endif