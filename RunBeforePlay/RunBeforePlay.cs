using Editor.AssetBundleBuilder;
using Editor.EditorTool;
using UnityEditor;
using UnityEngine;


namespace Editor.RunBeforePlay
{
    
    public class RunBeforePlay
    {
        [InitializeOnEnterPlayMode]
        static void AddExcuteFunction()
        {
            EditorHelper.Log("Start prepare for play");
            AutoSaveScene.Saver.SaveOpenScene();
            AutoBundleAdder.BundleNameGenerator();
            AutoBuilder.IncrementalBuildBundles();
            EditorHelper.Log("Preparation is done");
        }
    }
}