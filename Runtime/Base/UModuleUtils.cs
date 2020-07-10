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

        public List<Texture2D> GetTextureAssets(string searchFilter = "")
        {
            var textures = new List<Texture2D>();
#if UNITY_EDITOR
            foreach (var path in AssetDatabase.GetAssetPathsFromAssetBundle(ModuleType.ToLower() + "assets"))
            {
                if (searchFilter != "" && !path.Contains(searchFilter)) continue;
                if (!path.IsTexturePath()) continue;
                var data = System.IO.File.ReadAllBytes(path);
                var texture = new Texture2D(1, 1);
                if (texture.LoadImage(data))
                    textures.Add(texture);
            }
#endif
            if (!Application.ModuleAssets.ContainsKey(ModuleType)) return textures;
            var moduleAssets = Application.ModuleAssets[ModuleType];
            foreach (var name in moduleAssets.GetAllAssetNames())
            {
                if (searchFilter != "" && !name.Contains(searchFilter.ToLower())) continue;
                if (!name.IsTexturePath()) continue;
                var texture = moduleAssets.LoadAsset<Texture2D>(name);
                if (texture != null)
                    textures.Add(texture);
            }
            return textures;
        }

        

    }
}