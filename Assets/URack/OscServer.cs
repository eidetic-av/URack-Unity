using UnityEngine;
using UnityEngine.VFX;
using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Reflection;

namespace Eidetic.URack
{
    public class OscServer : MonoBehaviour
    {
        public int ListenPort = 54321;
        public bool LogIncoming = false;

        UdpClient UdpClient;
        IPEndPoint EndPoint;
        Osc.Parser Parser = new Osc.Parser();

        Dictionary<string, TargetProperty> Targets = new Dictionary<string, TargetProperty>();

        void Start()
        {
            EndPoint = new IPEndPoint(IPAddress.Any, ListenPort);
            UdpClient = new UdpClient(EndPoint);
        }

        void Update()
        {
            while (UdpClient.Available > 0)
            {
                Parser.FeedData(UdpClient.Receive(ref EndPoint));
            }

            while (Parser.MessageCount > 0)
            {
                var msg = Parser.PopMessage();

                if (LogIncoming)
                {
                    string dataString = "";
                    for (int i = 0; i < msg.data.Length; i++)
                    {
                        dataString += msg.data[i].ToString();
                        if (i != msg.data.Length - 1) dataString += ", ";
                    }
                    Debug.Log(msg.path + "\n" + dataString);
                }

                var address = msg.path.Split('/');
                switch (address[0])
                {
                    case "Add":
                        UModule.Create((string)msg.data[0], (int)msg.data[1]);
                        break;
                    case "Remove":
                        UModule.Remove((int)msg.data[1]);
                        // remove all references to the properties in the Target list
                        // so that the osc address doesn't point to destroyed references
                        var moduleAddress = msg.data[0] + "/" + msg.data[1];
                        var removeTargets = Targets.Keys
                            .Where(key => key.Contains(moduleAddress)).ToList();
                        foreach(var removeTarget in removeTargets)
                            Targets.Remove(removeTarget);
                        break;
                    case "Reset":
                        UModule.Remove((int)msg.data[1]);
                        var resetModuleAddress = msg.data[0] + "/" + msg.data[1];
                        var resetRemoveTargets = Targets.Keys
                            .Where(key => key.Contains(resetModuleAddress)).ToList();
                        foreach(var removeTarget in resetRemoveTargets)
                            Targets.Remove(removeTarget);
                        UModule.Create((string)msg.data[0], (int)msg.data[1]);
                        break;
                    case "Instance":
                        TargetProperty target;
                        if (Targets.ContainsKey(msg.path)) target = Targets[msg.path];
                        else
                        {
                            // if the target property doesn't exist in the cache,
                            // create the reference to the property, and if needed
                            // also create an instance of the module we are targetting
                            int instanceId = int.Parse(address[2]);
                            UModule moduleInstance = UModule.Instances.ContainsKey(instanceId) ?
                                UModule.Instances[instanceId] : UModule.Create(address[1], instanceId);
                            var targetProperty = moduleInstance.GetType()
                                .GetProperty(address[3]);

                            var isVFX = moduleInstance.GetType().IsSubclassOf(typeof(VFXModule));
                            if (isVFX)
                            {
                                var visualEffect = moduleInstance.gameObject.GetComponent<VisualEffect>();
                                target = new TargetProperty(moduleInstance, targetProperty, visualEffect);
                            }
                            else target = new TargetProperty(moduleInstance, targetProperty);

                            Targets[msg.path] = target;
                        }
                        // connecting a port
                        if (address[3] == "Connect")
                        {
                            var moduleInstance = UModule.Instances[int.Parse(address[2])];
                            var outputGetMethod = moduleInstance.GetType().GetProperty((string)msg.data[0]).GetGetMethod();
                            var outputGetter = new UModule.Getter(outputGetMethod);
                            var connectionInstance = UModule.Instances[(int)msg.data[1]];
                            var inputSetMethod = connectionInstance.GetType().GetProperty((string)msg.data[2]).GetSetMethod();
                            var inputSetter = new UModule.Setter(connectionInstance, inputSetMethod);
                            moduleInstance.Connections.Add(outputGetter, inputSetter);
                        }
                        // disconnecting a port
                        else if (address[3] == "Disconnect")
                        {
                            var moduleInstance = UModule.Instances[int.Parse(address[2])];
                            var outputGetMethod = moduleInstance.GetType().GetProperty((string)msg.data[0]).GetGetMethod();
                            var outputGetter = new UModule.Getter(outputGetMethod);
                            moduleInstance.Connections.Remove(outputGetter);
                        }
                        // setting a VFX Blackboard value
                        else if (target.IsVFX && target.Property == null)
                        {
                            var visualEffect = target.VisualEffect;
                            if (visualEffect.HasFloat(address[3]))
                                visualEffect.SetFloat(address[3], (float)msg.data[0]);
                            else if (visualEffect.HasInt(address[3]))
                                visualEffect.SetInt(address[3], (int)msg.data[0]);
                            else if (visualEffect.HasBool(address[3]))
                                visualEffect.SetBool(address[3], (float)msg.data[0] > 0);
                        }
                        // setting a property value
                        else if (target.Property.PropertyType == typeof(float))
                            target.Property.SetValue(target.Instance, (float)msg.data[0]);
                        else if (target.Property.PropertyType == typeof(int))
                            target.Property.SetValue(target.Instance, Mathf.RoundToInt((float)msg.data[0]));
                        else if (target.Property.PropertyType == typeof(bool))
                            target.Property.SetValue(target.Instance, (float)msg.data[0] > 0);
                        else if (target.Property.PropertyType == typeof(string))
                            target.Property.SetValue(target.Instance, msg.data[0]);
                        break;
                }
            }
        }

        struct TargetProperty
        {
            public UModule Instance;
            public PropertyInfo Property;
            public bool IsVFX => (VisualEffect != null);
            public VisualEffect VisualEffect;
            public TargetProperty(UModule instance, PropertyInfo property, VisualEffect visualEffect = null)
            {
                Instance = instance;
                Property = property;
                VisualEffect = visualEffect;
            }
        }

    }
}