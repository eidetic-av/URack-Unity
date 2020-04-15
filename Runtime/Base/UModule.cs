using System.Collections.Generic;
using UnityEngine;
using System;
using Harmony;
using System.Reflection;
using System.Linq;

namespace Eidetic.URack {
    public abstract partial class UModule : MonoBehaviour {
        public static Dictionary<int, UModule> Instances { get; private set; } = new Dictionary<int, UModule>();
        static HarmonyInstance Patcher;
        static UModule() => Patcher = HarmonyInstance.Create("Eidetic.URack.UModule");
        static List<Type> PatchedTypes = new List<Type>();

        public static UModule Create(string moduleName, int id) {
            // Load the module's prefab into the scene
            var gameObject = Instantiate(Resources.Load<GameObject>(moduleName + "Prefab"));
            var instanceName = moduleName + "Instance" + id;
            gameObject.name = instanceName;
            // set properties for the script

            var moduleInstance = Instances[id] = gameObject.GetComponent<UModule>();
            moduleInstance.ModuleType = moduleName;
            moduleInstance.InstanceName = instanceName;
            moduleInstance.Id = id;
            moduleInstance.InstanceAddress = "/" + moduleName + "/" + id;

            // Apply patches for automatic input voltage processing
            var moduleType = Type.GetType("Eidetic.URack." + moduleName);
            var inputProperties = moduleType.GetProperties()
                .Where(p => p.GetCustomAttribute<InputAttribute>() != null).ToArray();

            // for each "Input" apply a prefix to the property's set method
            foreach (var inputProperty in inputProperties) {
                if (inputProperty.PropertyType == typeof(PointCloud))
                    continue;

                var inputAttribute = inputProperty.GetCustomAttribute<InputAttribute>();
                var inputInfo = new InputInfo(inputProperty, inputAttribute);
                moduleInstance.Inputs.Add(inputInfo);

                // Instantiate the voltage vector
                moduleInstance.Voltages[inputInfo] = new Vector2(0, 0);

                // apply the prefix
                var prefix = new HarmonyMethod(typeof(UModule).GetMethod("SetterPrefix"));
                Patcher.Patch(inputProperty.GetSetMethod(), prefix);
            }

            if (PatchedTypes.Contains(moduleType)) return moduleInstance;

            // and apply the prefix for the module's Update method
            // to perform processing (mapping + smoothing) without the module
            // needing to call any ValueUpdate method manually
            var updateMethod = moduleType.GetMethod("Update");
            
            var valueUpdate = new HarmonyMethod(typeof(UModule).GetMethod("ValueUpdate"));
            Patcher.Patch(updateMethod, valueUpdate);
             // do the same for ConnectionUpdate
             var connectionUpdate = new HarmonyMethod(typeof(UModule).GetMethod("ConnectionUpdate"));
             Patcher.Patch(updateMethod, connectionUpdate);

            PatchedTypes.Add(moduleType);

            return moduleInstance;
        }

        public static void Remove(int id) {
            Destroy(Instances[id].gameObject);
            Instances.Remove(id);
        }

        // patch the setter so that it adds the new voltage to our Voltages array
        public static void SetterPrefix(UModule __instance, MethodBase __originalMethod, float value) {
            // get the setter from the InputsBySetter dictionary.
            // if it doesn't exist in there yet then add it
            var input = __instance.InputsBySetter.ContainsKey(__originalMethod)
                ? __instance.InputsBySetter[__originalMethod]
                : (__instance.InputsBySetter[__originalMethod] = __instance.Inputs
                    .Find(i => i.Property.GetSetMethod() == __originalMethod));
            // set the voltage.y value as the new value
            __instance.Voltages[input] = __instance.Voltages[input].Replace(1, value);
        }

        public static void ValueUpdate(UModule __instance) {
            if (__instance.Active) {
                foreach (var input in __instance.Inputs) {
                    if (input.Property.PropertyType == typeof(PointCloud))
                        continue;

                    var a = input.Attribute;
                    var currentValue = __instance.Voltages[input][0];
                    var newValue = __instance.Voltages[input][1];

                    // perform smoothing
                    // Todo: rn this is tied to frame-rate
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
        }
        public static void ConnectionUpdate(UModule __instance) {
            if (!__instance.Active) return;
                Debug.Log(__instance.Connections.Count + " connections");
            foreach (var connection in __instance.Connections) {
                    Debug.Log("with " + connection.Value.Count + " targets");
                foreach (var target in connection.Value) {
                    var module = target.ModuleInstance;
                    if (!module.Active) continue;
                    var value = connection.Key.GetMethod.Invoke(__instance, new object[0]);
                    target.SetMethod.Invoke(module, new object[] { value });
                }
            }
        }
        public int Id { get; private set; }
        public string ModuleType { get; private set; }
        public string InstanceName { get; private set; }
        public string InstanceAddress { get; private set; }

        public List<InputInfo> Inputs { get; private set; } = new List<InputInfo>();
        Dictionary<MethodBase, InputInfo> InputsBySetter = new Dictionary<MethodBase, InputInfo>();
        Dictionary<InputInfo, Vector2> Voltages = new Dictionary<InputInfo, Vector2>();

        public Dictionary<Getter, List<Setter>> Connections { get; private set; } = new Dictionary<Getter, List<Setter>>();

        public bool Active
        {
            get => gameObject.activeInHierarchy;
            set => gameObject.SetActive(value);
        }

        public void Update() { /* need this update method for patching in case the child doesn't call it */ }

        public struct InputInfo
        {
            public PropertyInfo Property;
            public InputAttribute Attribute;
            public InputInfo(PropertyInfo property, InputAttribute attribute)
            {
                Property = property;
                Attribute = attribute;
            }
        }

        public struct Getter
        {
            public MethodInfo GetMethod;
            public Getter(MethodInfo getMethod)
            {
                GetMethod = getMethod;
            }
        }
        public struct Setter
        {
            public UModule ModuleInstance;
            public MethodInfo SetMethod;
            public Setter(UModule moduleInstance, MethodInfo setMethod)
            {
                ModuleInstance = moduleInstance;
                SetMethod = setMethod;
            }
        }
    }
}
