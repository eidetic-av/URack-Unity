using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Reflection;
using Eidetic.URack.Packaging;
using LightBuzz.Archiver;
using UnityEditor;
using UnityEngine;

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

        // reference UnityEngine libs, mono libs, and package dlls used in current project
        var editorPath = Path.GetDirectoryName(EditorApplication.applicationPath);

        var unityLibsPath = editorPath + "/Data/Managed/UnityEngine/";
        var monoLibsPath = editorPath + "/Data/MonoBleedingEdge/lib/mono/unity/";
        var packageLibPath = Application.dataPath.Replace("Assets", "/Library/ScriptAssemblies/");

        compilerArgs += " -lib:\"" + unityLibsPath  + "\",\"" + packageLibPath + "\"";

        foreach (var unityLib in Directory.GetFiles(unityLibsPath))
            if (unityLib.Contains(".dll"))
                compilerArgs += " -r:\"" + Path.GetFileName(unityLib) + "\"";

        foreach (var packageLib in Directory.GetFiles(packageLibPath))
            if (packageLib.Contains(".dll") && !packageLib.Contains("Editor"))
                compilerArgs += " -r:\"" + Path.GetFileName(packageLib) + "\"";

        // add project source files
        compilerArgs += " -recurse:" + Application.dataPath + "/" + SourceDirectory + "/*.cs ";

        // get mono compiler from Unity install
        var compilerPath = Path.GetDirectoryName(EditorApplication.applicationPath);
        compilerPath += "/Data/MonoBleedingEdge/bin/mcs";
#if UNITY_EDITOR_WIN
        compilerPath += ".bat";
#endif
        var compilerStartInfo = new ProcessStartInfo();
        compilerStartInfo.FileName = compilerPath;
        compilerStartInfo.Arguments = compilerArgs;
        compilerStartInfo.RedirectStandardError = true;
        compilerStartInfo.UseShellExecute = false;

        var compilerProcess = new Process();
        compilerProcess.StartInfo = compilerStartInfo;

        // capture errors
        var standardError = new System.Text.StringBuilder();
        compilerProcess.ErrorDataReceived +=
            (s, args) => standardError.AppendLine(args.Data);

        compilerProcess.Start();
        compilerProcess.BeginErrorReadLine();
        compilerProcess.WaitForExit();

        // compiler tends to generate a lot of empty whitespace
        // at standard error
        var errorString = Regex.Replace(standardError.ToString(),
            @"^\r?\n?$", "", RegexOptions.Multiline);
        if (errorString.Length != 0)
        {
            UnityEngine.Debug.LogError("Plugin compilation failed with:\n" + errorString);
            return;
        }

        UnityEngine.Debug.Log("Plugin .dll compilation succeeded.");

        // Replace custom components on plugin prefabs that reference 
        // with a proxy.
        // We need to do this so that we can use scripts located within the
        // dll loaded at runtime
        var proxiedPrefabs = new List<string>();

        var assetBundleName = pluginName.ToLower() + "assets";
        foreach (var assetPath in AssetDatabase.GetAssetPathsFromAssetBundle(assetBundleName))
            if (assetPath.Contains(".prefab"))
            {
                var prefabContents = PrefabUtility.LoadPrefabContents(assetPath);
                foreach (var component in prefabContents.GetComponents<Component>())
                {
                    var componentType = component.GetType();
                    // if the component is located within the current editor assembly,
                    // swap it for the proxy that loads from the dll
                    if (componentType.Assembly.FullName.Contains("Assembly-CSharp"))
                    {
                        var componentProxy = prefabContents.AddComponent<ComponentProxy>();
                        componentProxy.PluginAssembly = pluginName;
                        componentProxy.TargetType = componentType.FullName;
                        MonoBehaviour.DestroyImmediate(component);
                        proxiedPrefabs.Add(assetPath);
                    }
                }
                PrefabUtility.SaveAsPrefabAsset(prefabContents, assetPath);
            }

        // build asset bundles
        BuildPipeline.BuildAssetBundles(outputDirPath,
            BuildAssetBundleOptions.None, EditorUserBuildSettings.activeBuildTarget);
        
        UnityEngine.Debug.Log("Asset bundle exported.");

        // change proxies back to original components now bundle is exported
        foreach (var prefabPath in proxiedPrefabs)
        {
            var prefabContents = PrefabUtility.LoadPrefabContents(prefabPath);
            foreach (var proxy in prefabContents.GetComponents<ComponentProxy>())
            {
                var componentType = System.Type.GetType(proxy.TargetType + ", Assembly-CSharp");
                prefabContents.AddComponent(componentType);
                Component.DestroyImmediate(proxy);
            }
            PrefabUtility.SaveAsPrefabAsset(prefabContents, prefabPath);
        }

        // compress all files into a URack plugin archive
        var pluginArchive = parentDir + "/" + pluginName + "-" + PluginVersion + ".zip";
        if (File.Exists(pluginArchive)) File.Delete(pluginArchive);
        Archiver.Compress(outputDirPath, pluginArchive);
        
        UnityEngine.Debug.Log("Plugin files packed.");

        // remove temp directory now it's archived
        Directory.Delete(outputDirPath, true);

        EditorUtility.RevealInFinder(pluginArchive);
        
        UnityEngine.Debug.Log("Plugin exported successfully.");

        // store plugin export settings for next export
        PlayerPrefs.SetString("URackExporter_PluginName_" + ProjectName, PluginName);
        PlayerPrefs.SetString("URackExporter_PluginVersion_" + ProjectName, PluginVersion);
        PlayerPrefs.SetString("URackExporter_SourceDirectory_" + ProjectName, SourceDirectory);
        PlayerPrefs.SetString("URackExporter_OutputDirectory_" + ProjectName, parentDir);
    }
}