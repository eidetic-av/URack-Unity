using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using LightBuzz.Archiver;

public class ExporterWindow : EditorWindow
{
    static string projectName;
    static string ProjectName => projectName ??
        (projectName = new FileInfo(Application.dataPath).Directory.Name);
    static string PluginName = "Plugin Name";
    static string PluginVersion = "1.0.0";
    static string SourceDirectory = "";
    static string OutputDirectory = "";

    [MenuItem("URack/Export Plugin")]
    static void Open()
    {
        var window = GetWindowWithRect<ExporterWindow>(new Rect(0, 0, 220, 200));
        window.Show();
        window.minSize = new Vector2(220, 200);
        window.titleContent = new GUIContent("Export URack Plugin");
        if (PlayerPrefs.HasKey("URackExporter_PluginName_" + ProjectName))
        {
            PluginName = PlayerPrefs.GetString("URackExporter_PluginName_" + ProjectName);
            PluginVersion = PlayerPrefs.GetString("URackExporter_PluginVersion_" + ProjectName);
            SourceDirectory = PlayerPrefs.GetString("URackExporter_SourceDirectory_" + ProjectName);
            OutputDirectory = PlayerPrefs.GetString("URackExporter_OutputDirectory_" + ProjectName);
        }
    }

    void OnGUI()
    {
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        GUILayout.BeginVertical();
        EditorGUILayout.LabelField("Plugin name");
        PluginName = EditorGUILayout.TextField(PluginName);
        EditorGUILayout.LabelField("Plugin version");
        PluginVersion = EditorGUILayout.TextField(PluginVersion);
        EditorGUILayout.LabelField("Source directory");
        EditorGUILayout.LabelField("(leave blank to use 'Assets' folder)");
        SourceDirectory = EditorGUILayout.TextField(SourceDirectory);
        EditorGUILayout.Space();
        if (GUILayout.Button("Export"))
            Build();
        GUILayout.EndVertical();
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
    }

    void Build()
    {
        var pluginName = PluginName.Replace(" ", "");

        string parentDir = "";
        if (OutputDirectory == "") parentDir = EditorUtility.OpenFolderPanel("Select Output Directory",
            Application.dataPath.Replace("/Assets", ""), "Build");
        else parentDir = EditorUtility.OpenFolderPanel("Select Output Directory",
            Path.GetDirectoryName(OutputDirectory), new DirectoryInfo(OutputDirectory).Name);

        // exit if dialog cancelled
        if (parentDir == "") return;

        // create temporary output directory
        var outputDirPath = parentDir + "/" + pluginName;
        var outputDir = new DirectoryInfo(outputDirPath);
        outputDir.Create();
        var createDirectoryTimeout = 0;
        while (!outputDir.Exists && createDirectoryTimeout < 100)
        {
            System.Threading.Thread.Sleep(50);
            createDirectoryTimeout++;
        }
        if (createDirectoryTimeout >= 100)
        {
            UnityEngine.Debug.LogError("Failed to create plugin directory.");
            return;
        }

        var pluginDll = outputDirPath + "/" + pluginName + ".dll";

        var compilerArgs = "-t:library -out:" + pluginDll;

        // reference UnityEngine libs and package dlls used in current project
        var editorPath = Path.GetDirectoryName(EditorApplication.applicationPath);

        var unityLibsPath = editorPath + "/Data/Managed/UnityEngine/";
        var packageLibPath = Application.dataPath.Replace("Assets", "/Library/ScriptAssemblies/");

        compilerArgs += " -lib:\"" + unityLibsPath + "\"" + ",\"" + packageLibPath + "\"";

        foreach (var unityLib in Directory.GetFiles(unityLibsPath))
            if (unityLib.Contains(".dll"))
                compilerArgs += " -r:\"" + Path.GetFileName(unityLib) + "\"";

        foreach (var packageLib in Directory.GetFiles(packageLibPath))
            if (packageLib.Contains(".dll") && !packageLib.Contains("Editor"))
                compilerArgs += " -r:\"" + Path.GetFileName(packageLib) + "\"";

        // add project source files
        compilerArgs += " -recurse:" + Application.dataPath + "/" + SourceDirectory + "/*.cs ";

        // TODO since mono comes bundled with unity, figure out how
        // to use this version instead of the one on Path
        // var compilerPath = EditorApplication.applicationPath.Replace("Unity.exe", "");
        // compilerPath += "Data/MonoBleedingEdge/lib/mono/4.5/mcs.exe";

        // compile plugin dll
        Process.Start("mcs", compilerArgs).WaitForExit();


        // Update script references on the plugin prefab so they
        // reference the new dll assembly instead of the editor scripting assembly
        var newAssembly = Assembly.LoadFrom(pluginDll);
        var newAssemblyTypes = newAssembly.GetTypes().ToList();
        var updatedComponents = new List<(string, System.Type, System.Type)>();

        var assetBundleName = pluginName.ToLower() + "assets";
        foreach (var assetPath in AssetDatabase.GetAssetPathsFromAssetBundle(assetBundleName))
            if (assetPath.Contains(".prefab"))
            {
                var prefabContents = PrefabUtility.LoadPrefabContents(assetPath);
                foreach (var component in prefabContents.GetComponents<Component>())
                {
                    var componentType = component.GetType();
                    // if the component is located within the current editor assembly,
                    // swap it for the one in the dll
                    if (componentType.Assembly.FullName.Contains("Assembly-CSharp"))
                    {
                        var typeName = componentType.Name;
                        var newType = newAssemblyTypes.First(t => t.Name == typeName);
                        Component.DestroyImmediate(component);
                        prefabContents.AddComponent(newType);
                        updatedComponents.Add((assetPath, componentType, newType));
                    }
                }
                PrefabUtility.SaveAsPrefabAsset(prefabContents, assetPath);
            }

        // build asset bundles
        BuildPipeline.BuildAssetBundles(outputDirPath,
            BuildAssetBundleOptions.None, EditorUserBuildSettings.activeBuildTarget);

        // change prefabs back to original script now bundle is exported
        foreach (var updatedComponent in updatedComponents)
        {
            var prefabContents = PrefabUtility.LoadPrefabContents(updatedComponent.Item1);
            var component = prefabContents.GetComponent(updatedComponent.Item3);
            Component.DestroyImmediate(component);
            prefabContents.AddComponent(updatedComponent.Item2);
            PrefabUtility.SaveAsPrefabAsset(prefabContents, updatedComponent.Item1);
        }

        // compress all files into a URack plugin archive
        var pluginArchive = parentDir + "/" + pluginName + "-" + PluginVersion + ".zip";
        if (File.Exists(pluginArchive)) File.Delete(pluginArchive);
        Archiver.Compress(outputDirPath, pluginArchive);

        // remove temp directory now it's archived
        Directory.Delete(outputDirPath, true);

        EditorUtility.RevealInFinder(pluginArchive);

        // store plugin export settings for next export
        PlayerPrefs.SetString("URackExporter_PluginName_" + ProjectName, PluginName);
        PlayerPrefs.SetString("URackExporter_PluginVersion_" + ProjectName, PluginVersion);
        PlayerPrefs.SetString("URackExporter_SourceDirectory_" + ProjectName, SourceDirectory);
        PlayerPrefs.SetString("URackExporter_OutputDirectory_" + ProjectName, parentDir);
    }
}