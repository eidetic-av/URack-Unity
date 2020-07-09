using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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

        var parentDir = EditorUtility.OpenFolderPanel("Select Output Directory",
            Application.dataPath.Replace("/Assets", ""), "Build");

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

        // build and export asset bundles
        BuildPipeline.BuildAssetBundles(outputDirPath,
            BuildAssetBundleOptions.None, EditorUserBuildSettings.activeBuildTarget);

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
    }
}