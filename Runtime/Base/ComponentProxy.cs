using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Eidetic.URack.Packaging
{
    public class ComponentProxy : MonoBehaviour
    {
        public string PluginAssembly;
        public string TargetType;

        public void Awake()
        {
            var componentType = System.Type.GetType(TargetType + ", " + PluginAssembly);
            gameObject.AddComponent(componentType);
            Destroy(this);
        }
    }
}