using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Eidetic.URack
{
    public abstract partial class UModule : MonoBehaviour
    {
        public static GameObject GetPrefab(string moduleName)
        {
            var prefabName = moduleName + "Prefab.prefab";
            GameObject prefab;
#if UNITY_EDITOR
            foreach (var assetPath in AssetDatabase.GetAssetPathsFromAssetBundle(moduleName.ToLower() + "assets"))
            {
                var fileName = Path.GetFileName(assetPath);
                if (fileName == prefabName)
                    return AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            }
#endif
            var assetBundle = Application.ModuleAssets[moduleName];
            foreach (var assetName in assetBundle.GetAllAssetNames())
            {
                if (assetName.Contains(prefabName.ToLower()))
                {
                    var modulePrefab = assetBundle.LoadAsset<GameObject>(assetName);
                    return modulePrefab;
                }
            }
            return null;
        }

        public List<UnityEngine.Object> GetAssets()
        {
            var assets = new List<UnityEngine.Object>();
#if UNITY_EDITOR
            foreach (var assetPath in AssetDatabase.GetAssetPathsFromAssetBundle(ModuleType.ToLower() + "assets"))
                assets.Add(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath));
#else
#endif
            Debug.Log(assets.Count());
            return assets;
        }

        public string[] GetAssetPaths()
        {
#if UNITY_EDITOR
            return AssetDatabase.GetAssetPathsFromAssetBundle(ModuleType.ToLower() + "assets");
#else
#endif
            return new string[] { };
        }

    }
}