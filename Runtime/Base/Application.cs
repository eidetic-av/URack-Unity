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
        public static Dictionary<string, AssetBundle> ModuleAssets = new Dictionary<string, AssetBundle>();

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
                var dll = Directory.GetFiles(pluginPath, "*.dll").Single();
                var assembly = Assembly.LoadFrom(dll);
                var pluginName = Path.GetFileNameWithoutExtension(dll);
                // Get each URack module included in the assembly
                foreach (var uModule in assembly.GetTypes()
                        .Where(t => t.BaseType == typeof(UModule) || t.BaseType == typeof(VFXModule)))
                {
                    var moduleName = uModule.Name;
                    // Store the module's type in the dictionary
                    PluginModules.Add(moduleName, uModule);
                    // And load its assets if we find any
                    var assetBundlePath = Directory
                        .GetFiles(pluginPath, moduleName.ToLower() + "assets").FirstOrDefault();
                    if (assetBundlePath != null)
                        ModuleAssets.Add(moduleName, AssetBundle.LoadFromFile(assetBundlePath));
                }
            }
        }
    }
}