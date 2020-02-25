using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Eidetic.URack
{
    [CustomEditor(typeof(UModule))]
    public abstract class UModuleEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var module = (UModule) target;

            EditorGUI.BeginDisabledGroup(true);

            foreach(var input in module.Inputs)
            {
                if (input.Property.GetMethod != null)
                    EditorGUILayout.FloatField(input.Property.Name, (float)input.Property.GetValue(module));
            }

            EditorGUI.EndDisabledGroup();

            DrawDefaultInspector();
        }
    }

    [CustomEditor(typeof(LiveScan3D))] public class LiveScan3DEditor : UModuleEditor { }
    [CustomEditor(typeof(Drone))] public class DroneEditor : UModuleEditor { }
    [CustomEditor(typeof(Harmony))] public class HarmonyEditor : UModuleEditor { }
    // [CustomEditor(typeof(Billboard))] public class BillboardEditor : UModuleEditor { }
}
