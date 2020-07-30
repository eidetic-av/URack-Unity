// Ply file reading based on https://github.com/keijiro/Pcx

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.AssetImporters;
using UnityEngine;
using UnityEngine.Rendering;

namespace Eidetic.URack.Importers
{
    [ScriptedImporter(1, "ply")]
    class PlyEditorImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext context)
        {
            var pointCloud = PlyImporter.ImportAsPointCloud(context.assetPath);
            context.AddObjectToAsset("PointCloud", pointCloud);
            context.AddObjectToAsset("PositionMap", pointCloud.PositionMap);
            context.AddObjectToAsset("ColorMap", pointCloud.ColorMap);
            context.SetMainObject(pointCloud);
        }
    }
}