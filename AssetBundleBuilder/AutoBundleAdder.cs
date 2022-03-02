using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Editor.EditorTool;
using Script;
using UnityEditor;
using UnityEngine;

namespace Editor.AssetBundleBuilder
{
    public class AutoBundleAdder
    {
        [MenuItem("Tools/Assets/Generate Bundle Name", false, 1)]
        public static void BundleNameGenerator()
        {
            EditorHelper.Log("Adding bundle name to each resource");
            Stack<string> directories = new Stack<string>();
            directories.Push(Constant.FilePath.BundleFilePath);
            while (directories.Any())
            {
                string currentPath = directories.Pop();
                string bundleName = currentPath.Replace(Constant.FilePath.BundleFilePath, "");
                if (bundleName.EndsWith("/"))
                    bundleName = bundleName.Substring(0, bundleName.Length - 2);
                if (bundleName == "")
                    bundleName = "Root";
                foreach (var file in Directory.GetFiles(currentPath))
                {
                    if (!Regex.IsMatch(file, "^(?!.*\\.~).*(.meta)$"))
                    {
                        string path = file.Replace(Application.dataPath, "Assets");
                        AssetImporter ai = AssetImporter.GetAtPath(path);
                        ai.assetBundleName = bundleName;
                    }
                }

                foreach (var directory in Directory.GetDirectories(currentPath))
                {
                    directories.Push(directory);
                }
            }
            EditorHelper.Log("BundleName is added to all resources");
        }
    }
}