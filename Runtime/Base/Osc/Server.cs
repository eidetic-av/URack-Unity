using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using UnityEngine;
using UnityEngine.VFX;

namespace Eidetic.URack.Osc
{
    public class Server : MonoBehaviour
    {

        public const int ListenPort = 54321;
        public const int SendPort = 54320;

        public static Server Instance { get; private set; }
        public static void CreateInstance()
        {
            Instance = new GameObject("URackServer").AddComponent<Server>();
        }

        public static string Ip;
        public static void Send<T>(string address, T value) => Instance.SendQueue.Enqueue((address, typeof(T), value));

        public bool LogIncoming = false;

        // receiving
        UdpClient UdpClient;
        IPEndPoint ListenEndPoint;
        Osc.Parser Parser = new Osc.Parser();

        Dictionary<string, PropertyTarget> PropertyTargets = new Dictionary<string, PropertyTarget>();

        Dictionary<string, MethodTarget> MethodTargets = new Dictionary<string, MethodTarget>();

        // sending
        Socket Socket;
        Osc.Encoder Encoder = new Osc.Encoder();

        Queue<(string, Type, object)> SendQueue = new Queue<(string, Type, object)>();

        Dictionary<IPAddress, IPEndPoint> Clients = new Dictionary<IPAddress, IPEndPoint>();

        // Events
        public static event Action<UModule> OnAddModule = (moduleInstance) => { };
        public static event Action OnRemoveModule = () => { };
        public static event Action<UModule, bool> OnModuleSetActive = (moduleInstance, active) => { };
        public static event Action<PropertyTarget, dynamic> OnSetProperty = (target, value) =>
        {
            if (target.Property.Name == "Active") OnModuleSetActive(target.Instance, value);
        };

        void Start()
        {
            // store this machine IP(v4) address
            Server.Ip = Dns.GetHostEntry(Dns.GetHostName()).AddressList
                .First(ip => ip.AddressFamily.ToString() == ProtocolFamily.InterNetwork.ToString())
                .ToString();

            // Set up listen and send UDP sockets
            ListenEndPoint = new IPEndPoint(IPAddress.Any, ListenPort);
            UdpClient = new UdpClient(ListenEndPoint);
            Socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            // get stored ip addresses for the last connected clients
            // and try to connect to them on startup
            int i = 0;
            while (PlayerPrefs.HasKey("Client" + i))
            {
                var lastClientIp = PlayerPrefs.GetString("Client" + i);
                PlayerPrefs.DeleteKey("Client" + i);
                i++;
                if (lastClientIp == "0.0.0.0") continue;
                var sendEndPoint = new IPEndPoint(IPAddress.Parse(lastClientIp), SendPort);
                Clients.Add(sendEndPoint.Address, sendEndPoint);
                // Sending "Initialise" causes modules to return all current values
                // to this IP address
                Encoder.Clear();
                Encoder.Append("/Initialise");
                Encoder.Append(",s");
                Encoder.Append(Server.Ip);
                Socket.SendTo(Encoder.Buffer, 0, Encoder.Length, SocketFlags.None, sendEndPoint);
            }
        }

        void OnDestroy()
        {
            // Save the connected clients to system on exit
            // so we can see if they're active when we
            // start the scene up again, and retreive the patch state
            // if they are
            for (int i = 0; i < Clients.Keys.Count(); i++)
                PlayerPrefs.SetString("Client" + i, Clients.Keys.ElementAt(i).ToString());
            PlayerPrefs.Save();
        }

        void Update()
        {
            while (UdpClient.Available > 0)
            {
                // recieve data and feed to parser
                var data = UdpClient.Receive(ref ListenEndPoint);
                Parser.FeedData(data);
                // if we haven't received from this address before,
                // add it to the client list
                if (!Clients.Keys.Contains(ListenEndPoint.Address))
                {
                    var sendEndPoint = new IPEndPoint(ListenEndPoint.Address, SendPort);
                    Clients.Add(ListenEndPoint.Address, sendEndPoint);
                }
            }

            // Route all incoming messages
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
                        var instance = UModule.Create((string)msg.data[0], (int)msg.data[1]);
                        OnAddModule(instance);
                        break;
                    case "Remove":
                        UModule.Remove((int)msg.data[1]);
                        OnRemoveModule();
                        // remove all references to the properties in the Target list
                        // so that the osc address doesn't point to destroyed references
                        var moduleAddress = msg.data[0] + "/" + msg.data[1];
                        var removePropertyTargets = PropertyTargets.Keys
                            .Where(key => key.Contains(moduleAddress)).ToList();
                        foreach (var removePropertyTarget in removePropertyTargets)
                            PropertyTargets.Remove(removePropertyTarget);
                        break;
                    case "Reset":
                        UModule.Remove((int)msg.data[1]);
                        var resetModuleAddress = msg.data[0] + "/" + msg.data[1];
                        var resetRemoveTargets = PropertyTargets.Keys
                            .Where(key => key.Contains(resetModuleAddress)).ToList();
                        foreach (var removeTarget in resetRemoveTargets)
                            PropertyTargets.Remove(removeTarget);
                        UModule.Create((string)msg.data[0], (int)msg.data[1]);
                        break;
                    case "Instance":
                        dynamic target = null;
                        if (PropertyTargets.ContainsKey(msg.path)) target = PropertyTargets[msg.path];
                        else if (MethodTargets.ContainsKey(msg.path)) target = MethodTargets[msg.path];
                        else
                        {
                            // if the target doesn't exist in the cache,
                            // create the reference to the property or method, and if needed
                            // also create an instance of the module we are targetting
                            int instanceId = int.Parse(address[2]);
                            UModule moduleInstance = UModule.Instances.ContainsKey(instanceId) ?
                                UModule.Instances[instanceId] : UModule.Create(address[1], instanceId);

                            var propertyInfo = moduleInstance.GetType().GetProperty(address[3]);

                            if (propertyInfo != null){
                                var isVFX = moduleInstance.GetType().IsSubclassOf(typeof(VFXModule));
                                if (isVFX)
                                {
                                    var visualEffect = moduleInstance.gameObject.GetComponent<VisualEffect>();
                                    target = new PropertyTarget(moduleInstance, propertyInfo, visualEffect);
                                }
                                else target = new PropertyTarget(moduleInstance, propertyInfo);

                                PropertyTargets[msg.path] = target;
                            } else
                            {
                                // if it's not a property, check if it's a method
                                var methodInfo = moduleInstance.GetType().GetMethod(address[3]);
                                // check if it's a query
                                var query = methodInfo.GetCustomAttribute<UModule.QueryAttribute>();
                                if (query != null)
                                {
                                    target = new MethodTarget(moduleInstance, methodInfo, true);
                                    MethodTargets[msg.path] = target;
                                }
                            }
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
                            if (!moduleInstance.Connections.ContainsKey(outputGetter))
                                moduleInstance.Connections.Add(outputGetter, new List<UModule.Setter>() { inputSetter });
                            else if (!moduleInstance.Connections[outputGetter].Contains(inputSetter))
                                moduleInstance.Connections[outputGetter].Add(inputSetter);
                        }
                        // disconnecting a port
                        else if (address[3] == "Disconnect")
                        {
                            var moduleInstance = UModule.Instances[int.Parse(address[2])];
                            var outputAddress = (string)msg.data[0];
                            var connectionInstance = UModule.Instances[(int)msg.data[1]];
                            var connectionAddress = (string)msg.data[2];
                            var outputGetMethod = moduleInstance.GetType().GetProperty(outputAddress).GetGetMethod();
                            var outputGetter = new UModule.Getter(outputGetMethod);
                            var connectionSetMethod = connectionInstance.GetType().GetProperty(connectionAddress).GetSetMethod();
                            if (moduleInstance.Connections.ContainsKey(outputGetter))
                            {
                                var portConnections = moduleInstance.Connections[outputGetter];
                                for (int i = 0; i < portConnections.Count; i++)
                                    if (portConnections[i].SetMethod == connectionSetMethod)
                                    {
                                        portConnections.RemoveAt(i);
                                        break;
                                    }
                                if (portConnections.Count == 0) moduleInstance.Connections.Remove(outputGetter);
                            }
                        }
                        else if (target is PropertyTarget)
                        {
                            // setting a VFX Blackboard value
                            if (target.IsVFX && target.Property == null)
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
                            else
                            {
                                dynamic newValue = Convert.ChangeType(msg.data[0], target.Property.PropertyType);
                                target.Property.SetValue(target.Instance, newValue);
                                OnSetProperty(target, newValue);
                            }
                        } else if (target is MethodTarget)
                        {
                            // performing a query
                            if (target.IsQuery)
                            {
                                var method = target.Method;
                                var module = target.Instance;
                                Type returnType = method.ReturnType;
                                var result = method.Invoke(module, new object[] {});
                                // add the result to the send queue
                                if (returnType == typeof(string[]))
                                {
                                    Send<string[]>(module.InstanceAddress + "/" + method.Name, result as string[]);
                                }
                            }
                        }
                        break;
                }
            }

            // Send any outgoing messages
            while (SendQueue.Count > 0)
            {
                var item = SendQueue.Dequeue();

                // if its a response to an active connection query
                if (item.Item1 == "QueryConnections")
                {
                    Encoder.Clear();
                    Encoder.Append("/QueryConnections");
                    Encoder.Append(",s");
                    Encoder.Append("Instance" + (string)item.Item3);

                    foreach (var endpoint in Clients.Values)
                        Socket.SendTo(Encoder.Buffer, 0, Encoder.Length, SocketFlags.None, endpoint);

                    continue;
                }
                // if its a response to a query for a list of strings
                else if (item.Item2 == typeof(string[]))
                {
                    var data = item.Item3 as string[];
                    Encoder.Clear();
                    Encoder.Append(item.Item1);
                    Encoder.Append(",i");
                    Encoder.Append(data.Length);
                    foreach (var stringResponse in data)
                    {
                        Encoder.Append(",s");
                        Encoder.Append(stringResponse);
                    }
                    foreach (var endpoint in Clients.Values)
                        Socket.SendTo(Encoder.Buffer, 0, Encoder.Length, SocketFlags.None, endpoint);
                    continue;
                }

                // if it is not a "Query" message, then it is an output value update
                var address = "/Instance" + item.Item1;
                var type = item.Item2;
                var value = item.Item3;

                Encoder.Clear();
                Encoder.Append(address);

                if (type == typeof(float))
                {
                    Encoder.Append(",f");
                    Encoder.Append((float)value);
                }
                else if (type == typeof(int))
                {
                    Encoder.Append(",i");
                    Encoder.Append((int)value);
                }
                else if (type == typeof(string))
                {
                    Encoder.Append(",s");
                    Encoder.Append((string)value);
                }

                foreach (var endpoint in Clients.Values)
                    Socket.SendTo(Encoder.Buffer, 0, Encoder.Length, SocketFlags.None, endpoint);

            }
        }

        public struct PropertyTarget
        {
            public UModule Instance;
            public PropertyInfo Property;
            public bool IsVFX => (VisualEffect != null);
            public VisualEffect VisualEffect;
            public PropertyTarget(UModule instance, PropertyInfo property, VisualEffect visualEffect = null)
            {
                Instance = instance;
                Property = property;
                VisualEffect = visualEffect;
            }
        }

        public struct MethodTarget
        {
            public UModule Instance;
            public MethodInfo Method;
            public bool IsQuery;
            public MethodTarget(UModule instance, MethodInfo method, bool isQuery)
            {
                Instance = instance;
                Method = method;
                IsQuery = isQuery;
            }
        }

    }
}
