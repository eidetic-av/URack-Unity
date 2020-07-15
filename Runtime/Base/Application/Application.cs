using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using LightBuzz.Archiver;
using UnityEngine;

namespace Eidetic.URack
{
    public static class Application
    {
        public static Dictionary<string, Type> PluginModules = new Dictionary<string, Type>();
        public static Dictionary<string, AssetBundle> ModuleAssetBundles = new Dictionary<string, AssetBundle>();
        public static Dictionary<string, string[]> ModuleAssetDirectories = new Dictionary<string, string[]>();

        static string pluginsDirectory;
        public static string PluginsDirectory
        {
            get
            {
                if (pluginsDirectory != null) return pluginsDirectory;
#if (UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN)
                pluginsDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "/URack";
#elif (UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX)
                pluginsDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "/Documents/URack";
#elif (UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX)
                pluginsDirectory = Environment.GetEnvironmentVariable("HOME") + "/.URack";
#endif
                return pluginsDirectory;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void StartupSequence()
        {
            UnpackPlugins();
            LoadPlugins();
            Osc.Server.CreateInstance();
        }

        static void UnpackPlugins()
        {
            foreach (var file in Directory.GetFiles(PluginsDirectory, "*.zip"))
            {
                var unpackFolder = file.Replace(".zip", "/");
                unpackFolder = unpackFolder.Replace("-win64", "");
                unpackFolder = unpackFolder.Replace("-linux", "");
                unpackFolder = unpackFolder.Replace("-macos", "");
                if (Directory.Exists(unpackFolder)) Directory.Delete(unpackFolder, true);
                new DirectoryInfo(unpackFolder).Create();
                Archiver.Decompress(file, unpackFolder);
                File.Delete(file);
            }
        }

        static void LoadPlugins()
        {
            foreach (var pluginPath in Directory.GetDirectories(PluginsDirectory))
            {
                // Load assembly
                var dll = Directory.GetFiles(pluginPath, "*.dll").SingleOrDefault();
                if (dll == null) continue;
                var assembly = Assembly.LoadFrom(dll);
                var pluginName = Path.GetFileNameWithoutExtension(dll);
                // Get each URack module included in the assembly
                foreach (var module in assembly.GetTypes().Where(t => t.IsSubclassOf(typeof(UModule))))
                {
                    var moduleName = module.Name;
                    // Store the module's type in the dictionary
                    PluginModules.Add(moduleName, module);
                    // And load its asset bundles if we find any
                    var assetBundlePath = Directory
                        .GetFiles(pluginPath, moduleName.ToLower() + "assets").FirstOrDefault();
                    if (assetBundlePath != null)
                        ModuleAssetBundles.Add(moduleName, AssetBundle.LoadFromFile(assetBundlePath));
                }
            }
            
            var plugPath = PluginsDirectory + "/URack-Collection-0.0.1/";
            var modName = "Mirage2x";
            var moduleAssetDirectory = plugPath + "/" + modName + "Assets";
            var moduleAssetDirectories = Directory.GetDirectories(moduleAssetDirectory);
            if (moduleAssetDirectories.Count() != 0)
                ModuleAssetDirectories.Add(modName, moduleAssetDirectories);
        }
    }
}