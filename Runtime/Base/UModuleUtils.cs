using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
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
            var assetBundle = Application.ModuleAssetBundles[moduleName];
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
                if (searchFilter != "" && !path.ToLower().Contains(searchFilter.ToLower())) continue;
                if (!path.IsTexturePath()) continue;
                var texture = new Texture2D(1, 1);
                textures.Add(texture);
                // load textures without blocking
                MainThreadDispatcher.Enqueue(() => {
                    if (texture == null) return;
                    var data = System.IO.File.ReadAllBytes(path);
                    texture.LoadImage(data);
                });
            }
#endif
            // Check in asset bundles
            if (Application.ModuleAssetBundles.ContainsKey(ModuleType))
            {
                var moduleAssetBundles = Application.ModuleAssetBundles[ModuleType];
                foreach (var name in moduleAssetBundles.GetAllAssetNames())
                {
                    if (searchFilter != "" && !name.ToLower().Contains(searchFilter.ToLower())) continue;
                    if (!name.IsTexturePath()) continue;
                    var texture = new Texture2D(1, 1);
                    textures.Add(texture);
                    // load textures without blocking
                    MainThreadDispatcher.Enqueue(() => {
                        if (texture == null) return;
                        texture = moduleAssetBundles.LoadAsset<Texture2D>(name);
                    });
                }
            }
            // Check in user asset directories
            if (Application.ModuleAssetDirectories.ContainsKey(ModuleType))
            {
                foreach (var directory in Application.ModuleAssetDirectories[ModuleType])
                    foreach (var path in Directory.GetFiles(directory))
                    {
                        if (path != "" && !path.ToLower().Contains(searchFilter.ToLower())) continue;
                        if (!path.IsTexturePath()) continue;
                        var texture = new Texture2D(1, 1);
                        textures.Add(texture);
                        // load textures without blocking
                        MainThreadDispatcher.Enqueue(() => {
                            if (texture == null) return;
                            var data = System.IO.File.ReadAllBytes(path);
                            texture.LoadImage(data);
                        });
                    }
            }

            return textures;
        }

    }
}