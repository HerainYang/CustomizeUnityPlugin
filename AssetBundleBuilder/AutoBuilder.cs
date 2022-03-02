using System.Collections;
using System.Collections.Generic;
using System.IO;
using Editor.EditorTool;
using UnityEditor;
using UnityEditor.Experimental;
using UnityEngine;

namespace Editor.AssetBundleBuilder
{
    public class AutoBuilder
    {
        [MenuItem("Tools/Assets/Totally Rebuild AssetBundles", false, 2)]
        public static void TotallyReBuildAssetBundles()
        {
            string assetBundleDirectory = "Assets/StreamingAssets";
            if (Directory.Exists(Application.streamingAssetsPath))
            {
                Directory.Delete(assetBundleDirectory, true);
            }

            if (!Directory.Exists(Application.streamingAssetsPath))
            {
                Directory.CreateDirectory(assetBundleDirectory);
            }

            BuildPipeline.BuildAssetBundles(assetBundleDirectory, BuildAssetBundleOptions.None,
                EditorUserBuildSettings.activeBuildTarget);
        }

        public static void IncrementalBuildBundles()
        {
            EditorHelper.Log("Start building bundle");
            string assetBundleDirectory = "Assets/StreamingAssets";
            if (!Directory.Exists(Application.streamingAssetsPath))
            {
                Directory.CreateDirectory(assetBundleDirectory);
            }

            BuildPipeline.BuildAssetBundles(assetBundleDirectory, BuildAssetBundleOptions.None,
                EditorUserBuildSettings.activeBuildTarget);
            EditorHelper.Log("End build bundle");
        }
    }
}

