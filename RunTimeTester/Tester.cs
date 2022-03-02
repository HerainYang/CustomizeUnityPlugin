using System;
using System.Linq;
using Editor.EditorTool;
using Script;
using Script.Helper;
using UnityEditor;
using UnityEngine;

namespace Editor.RunTimeTester
{
    public class Tester : EditorWindow
    {
        private static EditorWindow _thisWindow;
        private GameObject _gameObject;
        private GameObject _oldGameObject;
        private Component[] _components;

        [MenuItem("Tools/RunTimeTester")]
        public static void ShowWindow()
        {
            _thisWindow = EditorWindow.GetWindow(typeof(Tester));
        }
        private void OnGUI()
        {
            _gameObject = (GameObject)EditorGUILayout.ObjectField((UnityEngine.GameObject)_gameObject, typeof(GameObject), true);
            if (_gameObject != _oldGameObject)
            {
                _components = null;
                _oldGameObject = _gameObject;
            }

            if (_gameObject != null)
            {
                if (GUILayout.Button("Parse this object"))
                {
                    _components = _gameObject.GetComponents(typeof(IRunTimeTesterExecutable));
                    Debug.Log(_components.Length);

                }
            }

            if (_components != null)
            {
                foreach (var component in _components)
                {
                    if (Constant.RunTimeTesterComponent.Contains(component.GetType()))
                    {
                        EditorGUILayout.LabelField(component.ToString());
                        if (_thisWindow == null)
                        {
                            EditorHelper.LogError("Please Restart This RunTimeTester");
                        }
                        IRunTimeTesterExecutable executable = (IRunTimeTesterExecutable)component;
                        executable.RunTimeTesterHelpFunc();
                        Rect rect = EditorGUILayout.GetControlRect(false, 1, GUILayout.Width(_thisWindow.position.width));
                        EditorGUI.DrawRect(rect, new Color(1f, 1f, 1f, 1));
                    }
                }
            }
        }
    }
}