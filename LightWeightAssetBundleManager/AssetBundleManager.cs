using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Script.SystemManager
{
    public class AssetBundleManager : MonoBehaviour
    {
        class RefAsset
        {
            public Object Asset;
            public int RefCount;

            public RefAsset(Object asset)
            {
                Asset = asset;
            }
        }

        class RefBundle
        {
            public AssetBundle Bundle;
            public int RefCount;
            public Dictionary<string, RefAsset> RefAssets;

            public RefBundle(AssetBundle bundle)
            {
                Bundle = bundle;
                RefAssets = new Dictionary<string, RefAsset>();
            }

            public void OnDestroy()
            {
                foreach (var pair in RefAssets)
                {
                    RefAssets[pair.Key] = null;
                }
                RefAssets.Clear();
                Bundle.Unload(true);
            }
        }

        private Dictionary<string, RefBundle> _loadedBundles;
        private static AssetBundleManager _instance;

        private void Awake()
        {
            _instance = this;
            _loadedBundles = new Dictionary<string, RefBundle>();
            Launcher.Instance.ManagerSetReady(Constant.ManagerName.BundleManager);
        }

        public static AssetBundleManager GetInstance()
        {
            return _instance;
        }


        //当且仅当你需要用bundle做什么的时候（虽然目前我不知道还能拿来做什么，但资源申请相关还是通过这个class来执行比较好），或者你确定bundle很小，asset很小你急着加载的时候，比如配置文件
        public AssetBundle LoadBundleSync(string bundleName)
        {
            AssetBundle localAssetBundle;
            if (_loadedBundles.ContainsKey(bundleName))
            {
                localAssetBundle = _loadedBundles[bundleName].Bundle;
            }
            else
            {
                localAssetBundle =
                    AssetBundle.LoadFromFile(Path.Combine(Application.streamingAssetsPath, bundleName));
                if (localAssetBundle == null)
                {
                    Debug.LogError("Failed to load AssetBundle!");
                    return null;
                }

                _loadedBundles.Add(bundleName, new RefBundle(localAssetBundle));
            }

            return localAssetBundle;
        }

        IEnumerator LoadBundleAsync(string bundleName)
        {
            AssetBundle localAssetBundle;
            if (_loadedBundles.ContainsKey(bundleName))
            {
                yield break;
            }
            else
            {
                var request = AssetBundle.LoadFromFileAsync(Path.Combine(Application.streamingAssetsPath, bundleName));
                yield return new WaitUntil(() => request.isDone);
                if (request.assetBundle == null)
                {
                    Debug.LogError("Failed to load AssetBundle!");
                    yield break;
                }

                _loadedBundles.Add(bundleName, new RefBundle(request.assetBundle));
            }
        }

        public Object LoadAssetSync(string bundleName, string assetName)
        {
            AssetBundle localAssetBundle = null;
            if (!_loadedBundles.ContainsKey(bundleName))
            {
                localAssetBundle = LoadBundleSync(bundleName);
                if (localAssetBundle == null)
                {
                    return null;
                }
            }
            else
            {
                localAssetBundle = _loadedBundles[bundleName].Bundle;
            }

            if (_loadedBundles[bundleName].RefAssets.ContainsKey(assetName))
            {
                _loadedBundles[bundleName].RefAssets[assetName].RefCount++;
                return _loadedBundles[bundleName].RefAssets[assetName].Asset;
            }
            
            Object result = localAssetBundle.LoadAsset(assetName);
            if (result == null)
            {
                Debug.LogError("No such resource in this bundle");
                return null;
            }

            _loadedBundles[bundleName].RefAssets.Add(assetName, new RefAsset(result));
            _loadedBundles[bundleName].RefAssets[assetName].RefCount++;
            _loadedBundles[bundleName].RefCount++;
            return result;
        }
        
        public void LoadAssetAsync(string bundleName, string assetName, Delegate function, params object[] args)
        {
            IEnumerator func()
            {
                if (!_loadedBundles.ContainsKey(bundleName))
                {
                    yield return LoadBundleAsync(bundleName);
                    if(!_loadedBundles.ContainsKey(bundleName))
                        yield break;
                }
                AssetBundle localAssetBundle = _loadedBundles[bundleName].Bundle;
                if (_loadedBundles[bundleName].RefAssets.ContainsKey(assetName))
                {
                    _loadedBundles[bundleName].RefAssets[assetName].RefCount++;
                }
                else
                {
                    var request = localAssetBundle.LoadAssetAsync<GameObject>(assetName);
                    yield return new WaitUntil(() => { return request.isDone; });
                    if (request.asset == null)
                    {
                        Debug.LogError("No such resource in this bundle");
                        yield break;
                    }
                    _loadedBundles[bundleName].RefAssets.Add(assetName, new RefAsset(request.asset));
                    _loadedBundles[bundleName].RefAssets[assetName].RefCount++;
                }
                if (function != null)
                {
                    List<object> paramList = new List<object>(args);
                    paramList.Add(_loadedBundles[bundleName].RefAssets[assetName].Asset);
                    function.DynamicInvoke(paramList.ToArray());
                }
            }
            StartCoroutine(func());
        }

        public void UnloadAsset(string bundleName, string assetName)
        {
            if (!_loadedBundles.ContainsKey(bundleName))
            {
                Debug.LogError("Wrong bundle name, nothing can be unloaded");
            }
            else
            {
                if (!_loadedBundles[bundleName].RefAssets.ContainsKey(assetName))
                {
                    Debug.LogError("Wrong asset name, nothing can be unloaded");
                    return;
                }

                if ((--_loadedBundles[bundleName].RefAssets[assetName].RefCount) == 0)
                {
                    _loadedBundles[bundleName].RefAssets.Remove(assetName);
                    if ((--_loadedBundles[bundleName].RefCount) == 0)
                    {
                        _loadedBundles[bundleName].OnDestroy();
                        _loadedBundles.Remove(bundleName);
                    }
                }
            }

            Debug.Log("success");
        }
    }
}
