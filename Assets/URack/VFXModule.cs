using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Linq;
using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.VFX.Utility;
// #if UNITY_EDITOR
// using UnityEditor;
// #endif

namespace Eidetic.URack
{
    public abstract class VFXModule : UModule
    {
        public virtual VisualEffectAsset TemplateAsset { get; set; }

        [SerializeField] VisualEffect VisualEffect;

        VFXPropertyBinder binder;
        internal VFXPropertyBinder Binder => binder ?? (binder = VisualEffect.gameObject.GetComponent<VFXPropertyBinder>());

        PointCloudBinder pointCloudBinder;
        internal PointCloudBinder PointCloudBinder => pointCloudBinder ??
            (pointCloudBinder = Binder.GetParameterBinders<PointCloudBinder>().Single());

        public virtual void Start()
        {
            if (VisualEffect != null)
                VisualEffect.Play();
        }

        public void Exit()
        {
            gameObject?.SetActive(false);
        }

        [Input]
        public PointCloud PointCloud
        {
            set
            {
                PointCloudBinder.PointCloud = value;
                if (VisualEffect != null)
                    VisualEffect.SetInt("PointCount", value.PointCount);
            }
        }

//         public class VFXBlackboardInfo : ScriptableObject
//         {
//             public List<String> Properties;
//         }

// #if UNITY_EDITOR
//         [UnityEditor.Callbacks.DidReloadScripts]
//         private static void OnScriptsReloaded()
//         {
//             var derivedTypes = System.AppDomain.CurrentDomain
//                 .GetAllDerivedTypes(MethodBase.GetCurrentMethod().DeclaringType);

//             foreach (var type in derivedTypes)
//             {
//                 var scriptFile = System.IO.Directory
//                     .GetFiles(Application.dataPath, type.Name + ".cs", SearchOption.AllDirectories)
//                     .FirstOrDefault();

//                 var baseDir = Regex.Split(scriptFile, "Assets/")[1];
//                 baseDir = Regex.Split(baseDir, type.Name + ".cs")[0];
//                 var assetPath = "Assets/" + baseDir + type.Name + "BlackboardInfo.asset";
//                 var blackboardInfo = ScriptableObject.CreateInstance<VFXBlackboardInfo>();

//                 var prefab = Resources.Load<GameObject>(type.Name + "Prefab");
//                 var visualEffect = prefab.GetComponent<VisualEffect>();

//                 blackboardInfo.Properties = new List<string>() { "oi" };

//                 AssetDatabase.CreateAsset(blackboardInfo, assetPath);
//                 AssetDatabase.SaveAssets();
//                 AssetDatabase.Refresh();
//             }
//         }
// #endif
    }
}