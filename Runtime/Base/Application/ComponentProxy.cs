using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Eidetic.URack.Packaging
{
    public class ComponentProxy : MonoBehaviour
    {
        static Dictionary<(string, string), System.Type> TypeCache =
            new Dictionary<(string, string), System.Type>();

        static System.Type GetComponentType(string typeName, string assembly)
        {
            if (TypeCache.ContainsKey((typeName, assembly)))
                return TypeCache[(typeName, assembly)];
            var type = System.Type.GetType(typeName + ", " + assembly);
            TypeCache.Add((typeName, assembly), type);
            return type;
        }

        public string TypeName;
        public string PluginAssembly;

        public void Awake()
        {
            gameObject.AddComponent(GetComponentType(TypeName, PluginAssembly));
            Destroy(this);
        }
    }
}