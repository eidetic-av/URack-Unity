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
        public static GameObject GetUModulePrefab(string moduleName)
        {
            var prefabName = moduleName + "Prefab.prefab";
            GameObject prefab;
#if UNITY_EDITOR
            foreach (var assetPath in AssetDatabase.GetAssetPathsFromAssetBundle(moduleName.ToLower()))
            {
                var fileName = Path.GetFileName(assetPath);
                if (fileName == prefabName)
                    return AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            }
            return null;
#else
            return null;
#endif
        }

        public List<UnityEngine.Object> GetAssets()
        {
            var assets = new List<UnityEngine.Object>();
#if UNITY_EDITOR
            foreach (var assetPath in AssetDatabase.GetAssetPathsFromAssetBundle(ModuleType.ToLower()))
                assets.Add(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath));
#else
#endif
            return assets;
        }

        public string[] GetAssetPaths()
        {
#if UNITY_EDITOR
            return AssetDatabase.GetAssetPathsFromAssetBundle(ModuleType.ToLower());
#else
#endif
            return new string[] { };
        }

    }
}