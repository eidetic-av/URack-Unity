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
        static bool pipelineChecked = false;
        static bool isUsingUniversalRP = false;
        public static bool IsUsingUniversalRP 
        {
            get 
            {
                if (!pipelineChecked) 
                {
                    isUsingUniversalRP = AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.IsDynamic)
                        .SelectMany(a => a.GetExportedTypes()).ToList()
                        .FirstOrDefault(type => type.FullName.Contains("UnityEngine.Rendering.Universal")) != null;
                    pipelineChecked = true;
                }
                return isUsingUniversalRP;
            }
        }

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
#elif UNITY_ANDROID
                var javaClass = new AndroidJavaClass("android.os.Environment") ;
                var dataPath = javaClass.CallStatic<AndroidJavaObject>("getExternalStorageDirectory").Call<string>("getAbsolutePath");
                pluginsDirectory = dataPath + "/URack";
#endif
                return pluginsDirectory;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void StartupSequence()
        {
            UnpackPlugins();
            LoadPlugins();
            LoadPlayerModules();
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
                if (dll != null)
                {
                    var assembly = Assembly.LoadFrom(dll);
                    var pluginName = Path.GetFileNameWithoutExtension(dll);

                    // Get each URack module included in the assembly
                    foreach (var module in assembly.GetTypes().Where(t => t.IsSubclassOf(typeof(UModule))))
                    {
                        var moduleName = module.Name;
                        // Store the module's type in the dictionary
                        PluginModules[moduleName] = module;
                        // And load its asset bundles if we find any
                        var assetBundlePath = Directory
                            .GetFiles(pluginPath, moduleName.ToLower() + "assets").FirstOrDefault();
                        if (assetBundlePath != null)
                            ModuleAssetBundles[moduleName] = AssetBundle.LoadFromFile(assetBundlePath);
                    }
                }
                // Directories in the plugin path appended with Assets contain
                // collections of runtime-loaded files
                foreach (var subDirectory in Directory.GetDirectories(pluginPath))
                {
                    if (!subDirectory.EndsWith("UserAssets")) continue;
                    var moduleName = Path.GetFileName(subDirectory).Replace("UserAssets", "");
                    // add all sub-directories as well as the root Assets directory
                    var assetDirectories = Directory.GetDirectories(subDirectory)
                        .Append(subDirectory).ToArray();
                    ModuleAssetDirectories[moduleName] = assetDirectories;
                }
            }
        }

        static void LoadPlayerModules()
        {
            var scriptAssembly =  AppDomain.CurrentDomain.GetAssemblies()
                .First(a => a.GetName().Name.Contains("Assembly-CSharp"));
            foreach (var type in scriptAssembly.GetTypes())
                if (type.IsSubclassOf(typeof(UModule)))
                    PluginModules[type.Name] = type;
        }
    }
}
