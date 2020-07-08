using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;

public class ExporterWindow : EditorWindow
{
    string PluginName = "Plugin Name";
    string SourceDirectory = "";

    [MenuItem("URack/Export Plugin")]
    static void Open()
    {
        var window = GetWindowWithRect<ExporterWindow>(new Rect(0, 0, 500, 300));
        window.Show();
        window.minSize = new Vector2(200, 200);
        window.maxSize = new Vector2(700, 700);
        window.titleContent = new GUIContent("Export URack Plugin");
    }

    void OnGUI()
    {
        GUILayout.BeginVertical();
        EditorGUILayout.LabelField("Plugin name");
        PluginName = EditorGUILayout.TextField(PluginName);
        EditorGUILayout.LabelField("Source directory");
        EditorGUILayout.LabelField("(leave blank to use 'Assets' folder)");
        SourceDirectory = EditorGUILayout.TextField(SourceDirectory);
        if (GUILayout.Button("Export"))
            Build();
    }

    void Build()
    {
        var pluginName = PluginName.Replace(" ", "");

        var parentDir = EditorUtility
            .OpenFolderPanel("Select Output Directory", Application.dataPath.Replace("/Assets", ""), "Build");

        var outputDirPath = parentDir + "/" + pluginName;
        var outputDir = new FileInfo(outputDirPath).Directory;
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
        var editorPath = EditorApplication.applicationPath.Replace("Unity.exe", "");
        var unityLibsPath = editorPath + "Data/Managed/UnityEngine/";
        var packageLibPath = Application.dataPath.Replace("Assets", "Library/ScriptAssemblies/");

        compilerArgs += " -lib:\"" + unityLibsPath + "\"" + ",\"" + packageLibPath + "\"";

        foreach (var unityLib in Directory.GetFiles(unityLibsPath))
            if (unityLib.Contains(".dll"))
                compilerArgs += " -r:\"" + Path.GetFileName(unityLib) + "\"";

        foreach (var packageLib in Directory.GetFiles(packageLibPath))
            if (packageLib.Contains(".dll") && !packageLib.Contains("Editor"))
                compilerArgs += " -r:\"" + Path.GetFileName(packageLib) + "\"";

        // add project source files
        compilerArgs += " -recurse:" + Application.dataPath + "/" + SourceDirectory + "/*.cs ";

        // var compilerPath = EditorApplication.applicationPath.Replace("Unity.exe", "");
        // compilerPath += "Data/MonoBleedingEdge/lib/mono/4.5/mcs.exe";

        // compile plugin dll
        Process.Start("mcs", compilerArgs).WaitForExit();

        // build asset bundles in project directory
        var assetBundleDir = "Assets/temp_build/AB/";
        new FileInfo(assetBundleDir).Directory.Create();
        BuildPipeline.BuildAssetBundles(assetBundleDir, BuildAssetBundleOptions.None, BuildTarget.StandaloneWindows);

        // find asset bundle for this plugin and move to build dir
        // var assetBundleDirPath = Application.dataPath.Replace("Assets", assetBundleDir);
        // foreach (var file in Directory.GetFiles(assetBundleDirPath))
        //     if (file.ToLower().Contains(pluginName.ToLower()))
        //     {
        //         var fileName = Path.GetFileName(file);
        //         File.Move(file, outputDirPath + "/" + fileName);
        //     }

        // remove remaining asset bundles from project dir
        // Directory.Delete(assetBundleDir);

        // EditorUtility.RevealInFinder(pluginDll);
    }
}