using UnityEngine;
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
                switch (address[0].ToLower())
                {
                    case "add":
                        UModule.Create((string)msg.data[0], (int)msg.data[1]);
                        break;
                    case "remove":
                        UModule.Remove((int)msg.data[1]);
                        break;
                    case "reset":
                        UModule.Remove((int)msg.data[1]);
                        // remove all references to the properties in the Target list
                        // so that the osc address doesn't point to destroyed references
                        var removeTargets = Targets.Keys
                            .Where(key => key.Contains(msg.data[0] + "/" + msg.data[1]));
                        foreach (var key in removeTargets) Targets.Remove(key);
                        UModule.Create((string)msg.data[0], (int)msg.data[1]);
                        break;
                    case "instance":
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
                            Targets[msg.path] = target = new TargetProperty(moduleInstance, targetProperty);
                        }
                        // connecting a port
                        if (address[3] == "connect")
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
                        else if (address[3] == "disconnect")
                        {
                            var moduleInstance = UModule.Instances[int.Parse(address[2])];
                            var outputGetMethod = moduleInstance.GetType().GetProperty((string)msg.data[0]).GetGetMethod();
                            var outputGetter = new UModule.Getter(outputGetMethod);
                            moduleInstance.Connections.Remove(outputGetter);
                        }
                        // seting a property value
                        else if (target.Property.PropertyType == typeof(float))
                            target.Property.SetValue(target.Instance, (float)msg.data[0]);
                        else if (target.Property.PropertyType == typeof(int))
                            target.Property.SetValue(target.Instance, Mathf.RoundToInt((float)msg.data[0]));
                        else if (target.Property.PropertyType == typeof(bool))
                            target.Property.SetValue(target.Instance, (float)msg.data[0] >= 0 ? true : false);
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
            public TargetProperty(UModule instance, PropertyInfo property)
            {
                Instance = instance;
                Property = property;
            }
        }

    }
}