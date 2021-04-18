using Eidetic.PointClouds;
using System;
using UnityEngine.VFX;
using UnityEngine.VFX.Utility;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Eidetic.URack
{
    public abstract partial class UModule : MonoBehaviour
    {
        public static Dictionary<int, UModule> Instances { get; private set; } = new Dictionary<int, UModule>();

        static List<Type> PatchedTypes = new List<Type>();

        public static UModule Create(string moduleName, int id)
        {
            Debug.Log($"Attempting to create {moduleName}{id}");
            // Load the module's prefab into the scene
            var prefab = GetPrefab(moduleName);
            GameObject gameObject;
            if (prefab != null) gameObject = Instantiate(GetPrefab(moduleName));
            // if there's no custom prefab, create a new object
            else
            {
                UnityEngine.Debug.Log($"Attempting to instantiate module {moduleName} without custom prefab.");
                gameObject = new GameObject();
                // and add the corresponding UModule component
                if (!Application.PluginModules.Keys.Contains(moduleName))
                    UnityEngine.Debug.Log($"Attempt to instantiate backend for {moduleName} module failed. Does {moduleName}.cs exist?");

                Type moduleComponent = Application.PluginModules[moduleName];
                gameObject.AddComponent(moduleComponent);
                if (moduleComponent.IsSubclassOf(typeof(VFXModule)))
                {
                    var visualEffect = gameObject.AddComponent<VisualEffect>();
                    visualEffect.visualEffectAsset = Resources.Load<VisualEffectAsset>(moduleName + "Graph");
                    var propertyBinder = gameObject.AddComponent<VFXPropertyBinder>();
                    propertyBinder.AddPropertyBinder<PointCloudBinder>();
                }
            }
            var instanceName = moduleName + "Instance" + id;
            gameObject.name = instanceName;

            // set properties
            var moduleInstance = Instances[id] = gameObject.GetComponent<UModule>();
            moduleInstance.ModuleType = moduleName;
            moduleInstance.InstanceName = instanceName;
            moduleInstance.Id = id;
            moduleInstance.InstanceAddress = "/" + moduleName + "/" + id;

            Type moduleType = null;
#if UNITY_EDITOR
            // If we're in the editor, try looking for the module in unpackaged code
            Assembly scriptingAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .SingleOrDefault(a => a.FullName.Contains("Assembly-CSharp,"));
            moduleType = scriptingAssembly?.GetTypes()
                .SingleOrDefault(t => t.Name == moduleName);
#endif
            // Otherwise find the module in assemblies loaded in plugins
            if (moduleType == null)
                moduleType = Application.PluginModules[moduleName];

            // Get properties marked with URack attributes
            var inputProperties = moduleType.GetProperties()
                .Where(p => p.GetCustomAttribute<InputAttribute>() != null).ToArray();
            var outputProperties = moduleType.GetProperties()
                .Where(p => p.GetCustomAttribute<OutputAttribute>() != null).ToList();

            // Create a voltage representation of each input
            foreach (var inputProperty in inputProperties)
            {
                if (inputProperty.PropertyType == typeof(PointCloud))
                    continue;

                var inputAttribute = inputProperty.GetCustomAttribute<InputAttribute>();
                var inputInfo = new InputInfo(inputProperty, inputAttribute);
                moduleInstance.Inputs.Add(inputInfo.Property.Name, inputInfo);

                // Instantiate the voltage vector
                moduleInstance.Voltages[inputInfo] = new Vector2(0, 0);
            }

            // Populate a list of outputs to update to
            outputProperties.ForEach(o => moduleInstance.Outputs.Add(new OutputInfo (o)));

            // This list here runs each method added to it on every frame.
            moduleInstance.RuntimeUpdates.Add(() => typeof(UModule).GetMethod("ValueUpdate").Invoke(moduleInstance, new object[] {  }));
            moduleInstance.RuntimeUpdates.Add(() => typeof(UModule).GetMethod("ConnectionUpdate").Invoke(moduleInstance, new object[] {  }));
            moduleInstance.RuntimeUpdates.Add(() => typeof(UModule).GetMethod("OutputUpdate").Invoke(moduleInstance, new object[] {  }));

            // After creating a module, we should query if the VCV patch has
            // stored any connections for it
            Osc.Server.Send<string>("QueryConnections", moduleInstance.InstanceAddress);

            if (!PatchedTypes.Contains(moduleType))
                PatchedTypes.Add(moduleType);

            return moduleInstance;
        }

        public static void Remove(int id)
        {
            if (!Instances.ContainsKey(id)) return;
            Destroy(Instances[id].gameObject);
            Instances.Remove(id);
        }

        public void SetValue(string property, float value)
        {
            if (!Inputs.ContainsKey(property)) return;
            var input = Inputs[property];
            Voltages[input] = Voltages[input].Replace(1, value);
        }

        public void SetValue(string property, bool value)
        {
            if (property == "Active") Active = value;
            else SetValue(property, value ? 10f : 0f);
        } 

        public void ValueUpdate()
        {
            foreach (var input in Inputs.Values)
            {
                if (input.Property.PropertyType == typeof(PointCloud))
                    continue;

                var a = input.Attribute;
                var currentValue = Voltages[input][0];
                var newValue = Voltages[input][1];

                // perform smoothing
                // TODO rn this is tied to frame-rate
                if (Mathf.Abs(currentValue - newValue) > Mathf.Epsilon)
                    currentValue = currentValue + (newValue - currentValue) / a.Smoothing;
                // perform mapping
                float mappedValue = currentValue.Map(a.MinInput, a.MaxInput, a.MinOutput, a.MaxOutput, a.Exponent);
                if (a.Clamp) mappedValue.Clamp(a.MinOutput, a.MaxOutput);
                // run setter
                input.Property.SetValue(this, mappedValue);
                // rewrite the voltage store because we updated the vector by running the setter
                Voltages[input] = new Vector2(currentValue, newValue);
            }
        }
        public void ConnectionUpdate()
        {
            if (!Active) return;
            foreach (var connection in Connections)
            {
                foreach (var target in connection.Value)
                {
                    var module = target.ModuleInstance;
                    if (!module.Active) continue;
                    var value = connection.Key.GetMethod.Invoke(this, new object[0]);
                    target.SetMethod.Invoke(module, new object[] { value });
                }
            }
        }

        float outputTimer;
        public void OutputUpdate()
        {
            if (!Active) return;
            outputTimer += Time.deltaTime;
            if (outputTimer < 0.01f) return;
            foreach (var output in Outputs)
            {
                string address = $"{InstanceAddress}/{output.Property.Name}";
                var update = (float) output.Property.GetValue(this);
                Osc.Server.Send<float>(address, update);
            }
            outputTimer = 0f;
        }
        
        public int Id { get; private set; }
        public string ModuleType { get; private set; }
        public string InstanceName { get; private set; }
        public string InstanceAddress { get; private set; }

        public Dictionary<string, InputInfo> Inputs { get; private set; } = new Dictionary<string, InputInfo>();
        Dictionary<MethodBase, InputInfo> InputsBySetter = new Dictionary<MethodBase, InputInfo>();
        Dictionary<InputInfo, Vector2> Voltages = new Dictionary<InputInfo, Vector2>();

        public List<OutputInfo> Outputs { get; private set; } = new List<OutputInfo>();

        public Dictionary<Getter, List<Setter>> Connections { get; private set; } = new Dictionary<Getter, List<Setter>>();

        public bool Active
        {
            get => gameObject.activeInHierarchy;
            set => gameObject.SetActive(value);
        }

        public List<Action> RuntimeUpdates = new List<Action>();

        public void LateUpdate()
        {
            if (!Active) return;
            RuntimeUpdates.ForEach(update => update());
        }

        public void Update() { }

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

        public struct OutputInfo
        {
            public PropertyInfo Property;
            public OutputInfo(PropertyInfo property)
            {
                Property = property;
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
