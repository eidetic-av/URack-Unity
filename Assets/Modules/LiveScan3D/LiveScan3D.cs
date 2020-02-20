﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Eidetic.URack
{
    public class LiveScan3D : UModule
    {
        PointCloud pointCloudOutput;
        public PointCloud PointCloudOutput => pointCloudOutput ?? (pointCloudOutput = ScriptableObject.CreateInstance<PointCloud>());

        public bool NewFrame { get; set; }

        [Input(-5, 5, -10, 10)] public float MinX { get; set; }

        [Input(-5, 5, -10, 10)]  public float MinY { get; set; }

        [Input(-5, 5, -10, 10)] public float MinZ { get; set; }

        [Input(-5, 5, -10, 10)] public float MaxX { get; set; }

        [Input(-5, 5, -10, 10)]  public float MaxY { get; set; }

        [Input(-5, 5, -10, 10)]  public float MaxZ { get; set; }

        [Input] public float TranslationX { get; set; }

        [Input] public float TranslationY { get; set; }

        [Input] public float TranslationZ { get; set; }

        [Input(-5, 5, -180, 180)] public float RotationX { get; set; }

        [Input(-5, 5, -180, 180)] public float RotationY { get; set; }

        [Input(-5, 5, -180, 180)] public float RotationZ { get; set; }

        [Input] public float ScaleX { get; set; }

        [Input] public float ScaleY { get; set; }

        [Input] public float ScaleZ { get; set; }

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
