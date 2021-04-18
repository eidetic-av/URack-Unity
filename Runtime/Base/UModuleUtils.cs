using Eidetic.PointClouds;
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
#if UNITY_EDITOR
            foreach (var assetPath in AssetDatabase.GetAssetPathsFromAssetBundle(moduleName.ToLower() + "assets"))
            {
                var fileName = Path.GetFileName(assetPath);
                if (fileName == prefabName)
                    return AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            }
#endif
            if (!Application.ModuleAssetBundles.ContainsKey(moduleName))
                return null;

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

        public List<string> GetUserAssetDirectoryNames()
        {
            var assetDirectories = Application.ModuleAssetDirectories[ModuleType]
                .Select(p => Path.GetFileName(Path.GetDirectoryName(p + "/")));
            var baseDirectory = assetDirectories.Where(d => d == ModuleType + "UserAssets");
            return assetDirectories.Except(baseDirectory).ToList();
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
                    MainThreadDispatcher.Enqueue(() =>
                    {
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
                        MainThreadDispatcher.Enqueue(() =>
                        {
                            if (texture == null) return;
                            var data = System.IO.File.ReadAllBytes(path);
                            texture.LoadImage(data);
                        });
                    }
            }

            return textures;
        }

        public List<PointCloud> GetPointCloudAssets(string searchFilter = "")
        {
            var pointClouds = new List<PointCloud>();
            
            if (ModuleType == null || ModuleType == "") return pointClouds;

            // Check in resources
            var resourceResults = Resources.LoadAll<PointCloud>(searchFilter).ToList();
            if (resourceResults?.Count != 0)
                resourceResults.ForEach(pc => pointClouds.Add(pc));

            // Check in asset bundles
#if UNITY_EDITOR
            foreach (var path in AssetDatabase.GetAssetPathsFromAssetBundle(ModuleType.ToLower() + "assets"))
            {
                if (searchFilter != "" && !path.ToLower().Contains(searchFilter.ToLower())) continue;
                if (!path.IsPointCloudPath()) continue;
                var pointCloud = PlyImporter.ImportAsPointCloud(path);
                pointClouds.Add(pointCloud);
            }
#endif
            if (Application.ModuleAssetBundles.ContainsKey(ModuleType))
            {
                var moduleAssetBundles = Application.ModuleAssetBundles[ModuleType];
                foreach (var name in moduleAssetBundles.GetAllAssetNames())
                {
                    if (searchFilter != "" && !name.ToLower().Contains(searchFilter.ToLower())) continue;
                    if (!name.IsPointCloudPath()) continue;
                    // TODO not yet implemented
                }
            }
            // Check in user asset directories
            if (Application.ModuleAssetDirectories.ContainsKey(ModuleType))
            {
                foreach (var directory in Application.ModuleAssetDirectories[ModuleType])
                    foreach (var path in Directory.GetFiles(directory))
                    {
                        if (path != "" && !path.ToLower().Contains(searchFilter.ToLower())) continue;
                        if (!path.IsPointCloudPath()) continue;
                        var pointCloud = PlyImporter.ImportAsPointCloud(path);
                        pointClouds.Add(pointCloud);
                    }
            }
            return pointClouds;
        }

        public T GetAsset<T>(string assetName) where T : class
        {
            // Check in Resources
            var resourceResults = Resources.LoadAll(assetName);
            if (resourceResults?.Count() != 0)
                return resourceResults.SingleOrDefault() as T;
            // Check in asset bundles
#if UNITY_EDITOR
            foreach (var path in AssetDatabase.GetAssetPathsFromAssetBundle(ModuleType.ToLower() + "assets"))
            {
                if (!path.ToLower().EndsWith(assetName.ToLower())) continue;
                return AssetDatabase.LoadAssetAtPath(path, typeof(T)) as T;
            }
#endif
            if (Application.ModuleAssetBundles.ContainsKey(ModuleType))
            {
                var moduleAssetBundle = Application.ModuleAssetBundles[ModuleType];
                foreach (var name in moduleAssetBundle.GetAllAssetNames())
                {
                    if (!name.ToLower().EndsWith(assetName.ToLower())) continue;
                    return moduleAssetBundle.LoadAsset(name, typeof(T)) as T;
                }
            }
            // Check in user asset directories
            if (Application.ModuleAssetDirectories.ContainsKey(ModuleType))
            {
                foreach (var directory in Application.ModuleAssetDirectories[ModuleType])
                    foreach (var path in Directory.GetFiles(directory))
                    {
                        if (!path.ToLower().EndsWith(assetName.ToLower())) continue;
                        // TODO not yet implemented
                    }
            }
            return default(T);
        }

    }
}
