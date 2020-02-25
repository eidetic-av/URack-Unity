using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Eidetic.URack
{
    public class LiveScan3D : UModule
    {
        PointCloud pointCloudOutput;
        public PointCloud PointCloudOutput => pointCloudOutput ?? (pointCloudOutput = ScriptableObject.CreateInstance<PointCloud>());

        public bool NewFrame { get; set; }

        [Input(-5, 5, -10, 10)] public float MinLopX { get; set; }

        [Input(-5, 5, -10, 10)]  public float MinLopY { get; set; }

        [Input(-5, 5, -10, 10)] public float MinLopZ { get; set; }

        [Input(-5, 5, -10, 10)] public float MaxLopX { get; set; }

        [Input(-5, 5, -10, 10)]  public float MaxLopY { get; set; }

        [Input(-5, 5, -10, 10)]  public float MaxLopZ { get; set; }

        [Input] public float LocationX { get; set; }

        [Input] public float LocationY { get; set; }

        [Input] public float LocationZ { get; set; }

        [Input(-5, 5, -180, 180)] public float RotationX { get; set; }

        [Input(-5, 5, -180, 180)] public float RotationY { get; set; }

        [Input(-5, 5, -180, 180)] public float RotationZ { get; set; }

        [Input] public float ScalingX { get; set; }

        [Input] public float ScalingY { get; set; }

        [Input] public float ScalingZ { get; set; }

        LiveScanReceiver Receiver;

        string Address = "127.0.0.1";
        int Port = 48002;

        public void Start() => Connect();

        void Connect()
        {
            var objectName = "LiveScanReceiver" + "-" + Address + ":" + Port;

            Receiver = GameObject.Find(objectName)?.GetComponent<LiveScanReceiver>() ??
                new GameObject(objectName).AddComponent<LiveScanReceiver>();
            Receiver.gameObject.SetActive(true);
            Receiver.Connect(Address, Port);

            if (!Receiver.StreamingModules.Contains(this)) Receiver.StreamingModules.Add(this);
        }

        public void OnDestroy()
        {
            if (Receiver == null) return;
            if (Receiver.StreamingModules.Contains(this))
                Receiver.StreamingModules.Remove(this);
            if (Receiver.StreamingModules.Count == 0)
                Receiver.gameObject.Destroy();
        }

    }
}
