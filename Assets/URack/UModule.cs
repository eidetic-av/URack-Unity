using System.Collections.Generic;
using UnityEngine;
using System;
using Harmony;
using System.Reflection;
using System.Linq;

namespace Eidetic.URack
{
    public abstract class UModule : MonoBehaviour
    {
        public static Dictionary<int, UModule> Instances { get; private set; } = new Dictionary<int, UModule>();

        static HarmonyInstance Patcher;
        static public Dictionary<string, Vector2> VoltageStore { get; private set; }
            = new Dictionary<string, Vector2>();

        static UModule()
        {
            Patcher = HarmonyInstance.Create("com.eidetic.urack.umodule");
        }

        public static UModule Create(string moduleName, int id)
        {
            // Load the module's prefab into the scene
            var prefab = Resources.Load<GameObject>(moduleName + "Prefab");
            var gameObject = Instantiate(prefab);
            var instanceName = moduleName + "Instance" + id;
            gameObject.name = instanceName;

            // set properties for the script
            var moduleInstance = (UModule)gameObject.GetComponent(moduleName);
            moduleInstance.ModuleType = moduleName;
            moduleInstance.InstanceName = instanceName;
            moduleInstance.Id = id;

            Instances[id] = moduleInstance;

            // automatic smoothing for setters in each module
            var moduleType = Type.GetType("Eidetic.URack." + moduleName);
            var inputProperties = moduleType.GetProperties()
                .Where(p => p.GetCustomAttribute<InputAttribute>() != null).ToArray();

            foreach (var inputProperty in inputProperties)
            {
                var inputAttribute = inputProperty.GetCustomAttribute<InputAttribute>();
                moduleInstance.Inputs.Add(new InputStore(inputProperty, inputAttribute));

                var inputName = instanceName + inputProperty.Name;

                // Instantiate the voltage vector
                VoltageStore[inputName] = new Vector2(0, 0);

                // apply the prefix
                var prefix = new HarmonyMethod(typeof(UModule).GetMethod("Prefix"));
                Patcher.Patch(inputProperty.GetSetMethod(), prefix);
            }

            return moduleInstance;
        }

        // patch the setter so that it adds the new voltage to our VoltageStore array
        public static void Prefix(UModule __instance, MethodBase __originalMethod, float value)
        {
            var inputName = __instance.InstanceName + __originalMethod.Name.Substring(4);
            VoltageStore[inputName] = VoltageStore[inputName].Replace(1, value);
        }

        public void Update()
        {
            foreach (var input in Inputs)
            {
                var inputName = InstanceName + input.Property.Name;
                var minInput = input.Attribute.MinInput;
                var maxInput = input.Attribute.MaxInput;
                var minOutput = input.Attribute.MinOutput;
                var maxOutput = input.Attribute.MaxOutput;
                var exponent = input.Attribute.Exponent;
                var smoothing = input.Attribute.Smoothing;

                // map the incoming value based on the settings on the attribute
                var newValue = VoltageStore[inputName][1];

                var currentValue = VoltageStore[inputName][0];

                // perform smoothing
                if (smoothing != 1 && Mathf.Abs(currentValue - newValue) > Mathf.Epsilon)
                {
                    currentValue = currentValue + (newValue - currentValue) / smoothing;

                    var mappedValue = currentValue
                        .Map(minInput, maxInput, minOutput, maxOutput, exponent);

                    // TODO: this gets muliplied by 2x somewhere?
                    mappedValue = mappedValue / 2f;

                    input.Property.SetValue(this, mappedValue);
                }

                // rewrite the voltage store because we just updated the vector with the 
                // prefix of the patch
                VoltageStore[inputName] = new Vector2(currentValue, newValue);
            }
            // run the module-specific Update code
            Process();
        }

        public virtual void Process() { }

        public static void Remove(int id)
        {
            Destroy(Instances[id].gameObject);
            Instances.Remove(id);
        }

        public int Id { get; private set; }
        public string ModuleType { get; private set; }
        public string InstanceName { get; private set; }

        List<InputStore> Inputs = new List<InputStore>();

        struct InputStore
        {
            public PropertyInfo Property;
            public InputAttribute Attribute;
            public InputStore(PropertyInfo property, InputAttribute attribute)
            {
                Property = property;
                Attribute = attribute;
            }
        }

        [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
        public class InputAttribute : Attribute
        {
            const float DefaultMinInput = -5;
            const float DefaultMaxInput = 5;
            const float DefaultMinOutput = -5;
            const float DefaultMaxOutput = 5;
            const float DefaultExponent = 1;
            const float DefaultSmoothing = 5;

            public float MinInput { get; internal set; }
            public float MaxInput { get; internal set; }
            public float MinOutput { get; internal set; }
            public float MaxOutput { get; internal set; }
            public float Exponent { get; internal set; }
            public float Smoothing { get; internal set; }

            public InputAttribute(float smoothing = DefaultSmoothing)
            {
                MinInput = DefaultMinInput;
                MaxInput = DefaultMaxInput;
                MinOutput = DefaultMinOutput;
                MaxOutput = DefaultMaxOutput;
                Exponent = DefaultExponent;
                Smoothing = smoothing;
            }

            public InputAttribute(float minInput, float maxInput, float minOutput, float maxOutput, float exponent = DefaultExponent, float smoothing = DefaultSmoothing)
            {
                MinInput = minInput;
                MaxInput = maxInput;
                MinOutput = minOutput;
                MaxOutput = maxOutput;
                Exponent = exponent;
                Smoothing = smoothing;
            }

        }
    }
}