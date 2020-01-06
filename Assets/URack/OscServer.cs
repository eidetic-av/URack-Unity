using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Reflection;

public class OscServer : MonoBehaviour
{
    public int ListenPort = 54321;

    UdpClient UdpClient;
    IPEndPoint EndPoint;
    Osc.Parser Parser = new Osc.Parser();

    Dictionary<string, (MonoBehaviour, PropertyInfo)> Targets = new Dictionary<string, (MonoBehaviour, PropertyInfo)>();

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
            var address = msg.path.Split('/');
            switch (address[0].ToLower())
            {
                case "instance":
                    (MonoBehaviour, PropertyInfo) target;
                    if (Targets.ContainsKey(msg.path)) target = Targets[msg.path];
                    else
                    {
                        // the type of module comes in at address[1],
                        // so first get the list of instances of this object
                        var targetType = Type.GetType("Eidetic.URack." + address[1]);
                        var instanceList = targetType.GetField("Instance", BindingFlags.Public
                                | BindingFlags.Static
                                | BindingFlags.FlattenHierarchy).GetValue(null);

                        // the instance number comes in at address[2],
                        // so we can get the object instance from the list
                        var targetObject = ((Dictionary<int, MonoBehaviour>)instanceList)[int.Parse(address[2])];

                        // the property we are trying to change comes in at address[3]
                        var targetProperty = targetObject.GetType().GetProperty(address[3]);

                        // bundle the object instance and the property info and put it in the
                        // address dictionary:w
                        Targets[msg.path] = target = (targetObject, targetProperty);
                    }
                    if (target.Item2.PropertyType == typeof(float))
                        target.Item2.SetValue(target.Item1, (float)msg.data[0]);
                    break;
            }
        }
    }
}