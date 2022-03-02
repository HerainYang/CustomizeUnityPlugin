using UnityEditor.SceneManagement;
using UnityEngine;
using Editor.EditorTool;

namespace Editor.AutoSaveScene
{
    public class Saver
    {
        //Just for accidental crash, please remember save by yourself
        public static void SaveOpenScene()
        { 
            EditorHelper.Log("Auto-saving");
            bool ret = EditorSceneManager.SaveOpenScenes();
            EditorHelper.Log("Save process complete, status: " + ret);
        }
    }
}