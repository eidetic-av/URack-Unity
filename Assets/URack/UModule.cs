using System.Collections.Generic;
using UnityEngine;
using System;
using Harmony;
using System.Reflection;
using System.Linq;

namespace Eidetic.URack
{
    public abstract partial class UModule : MonoBehaviour
    {
        public static Dictionary<int, UModule> Instances { get; private set; } = new Dictionary<int, UModule>();

        static HarmonyInstance Patcher;

        static UModule() => Patcher = HarmonyInstance.Create("com.eidetic.urack.umodule");

        public static UModule Create(string moduleName, int id)
        {
            // Load the module's prefab into the scene
            var gameObject = Instantiate(Resources.Load<GameObject>(moduleName + "Prefab"));
            var instanceName = moduleName + "Instance" + id;
            gameObject.name = instanceName;

            // set properties for the script
            var moduleInstance = Instances[id]
                = (UModule)gameObject.GetComponent(moduleName);
            moduleInstance.ModuleType = moduleName;
            moduleInstance.InstanceName = instanceName;
            moduleInstance.Id = id;

            // Apply patches for automatic input voltage processing

            var moduleType = Type.GetType("Eidetic.URack." + moduleName);
            var inputProperties = moduleType.GetProperties()
                .Where(p => p.GetCustomAttribute<InputAttribute>() != null).ToArray();

            // for each "Input" apply a prefix to the property's set method
            foreach (var inputProperty in inputProperties)
            {
                var inputAttribute = inputProperty.GetCustomAttribute<InputAttribute>();
                var inputInfo = new InputInfo(inputProperty, inputAttribute);
                moduleInstance.Inputs.Add(inputInfo);

                // Instantiate the voltage vector
                moduleInstance.Voltages[inputInfo] = new Vector2(0, 0);

                // apply the prefix
                var prefix = new HarmonyMethod(typeof(UModule).GetMethod("SetterPrefix"));
                Patcher.Patch(inputProperty.GetSetMethod(), prefix);
            }

            // and apply the prefix for the module's Update method
            // to perform processing (mapping + smoothing) without the module
            // needing to call any ValueUpdate method manually
            var updateMethod = moduleType.GetMethod("Update");
            var valueUpdate = new HarmonyMethod(typeof(UModule).GetMethod("ValueUpdate"));
            Patcher.Patch(updateMethod, valueUpdate);

            return moduleInstance;
        }

        public static void Remove(int id)
        {
            Destroy(Instances[id].gameObject);
            Instances.Remove(id);
        }

        // patch the setter so that it adds the new voltage to our Voltages array
        public static void SetterPrefix(UModule __instance, MethodBase __originalMethod, float value)
        {
            // get the setter from the InputsBySetter dictionary.
            // if it doesn't exist in there yet then add it
            var input = __instance.InputsBySetter.ContainsKey(__originalMethod)
                ? __instance.InputsBySetter[__originalMethod]
                : (__instance.InputsBySetter[__originalMethod] = __instance.Inputs
                    .Find(i => i.Property.GetSetMethod() == __originalMethod));
            // set the voltage.y value as the new value
            __instance.Voltages[input] = __instance.Voltages[input].Replace(1, value);
        }

        public static void ValueUpdate(UModule __instance)
        {
            foreach (var input in __instance.Inputs)
            {
                var a = input.Attribute;
                var currentValue = __instance.Voltages[input][0];
                var newValue = __instance.Voltages[input][1];

                // perform smoothing
                if (Mathf.Abs(currentValue - newValue) > Mathf.Epsilon)
                    currentValue = currentValue + (newValue - currentValue) / a.Smoothing;
                // perform mapping
                float mappedValue = currentValue.Map(a.MinInput, a.MaxInput, a.MinOutput, a.MaxOutput, a.Exponent);
                if (a.Clamp) mappedValue.Clamp(a.MinOutput, a.MaxOutput);
                // run setter
                input.Property.SetValue(__instance, mappedValue);
                // rewrite the voltage store because we updated the vector by running the setter
                __instance.Voltages[input] = new Vector2(currentValue, newValue);
            }
        }

        public int Id { get; private set; }
        public string ModuleType { get; private set; }
        public string InstanceName { get; private set; }

        List<InputInfo> Inputs = new List<InputInfo>();
        Dictionary<MethodBase, InputInfo> InputsBySetter = new Dictionary<MethodBase, InputInfo>();
        Dictionary<InputInfo, Vector2> Voltages = new Dictionary<InputInfo, Vector2>();

        void Update() { /* need this update method for patching in case the child doesn't call it */ }

        struct InputInfo
        {
            public PropertyInfo Property;
            public InputAttribute Attribute;
            public InputInfo(PropertyInfo property, InputAttribute attribute)
            {
                Property = property;
                Attribute = attribute;
            }
        }
    }
}