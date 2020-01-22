using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Jobs;
using System.Threading;
using System.Threading.Tasks;

namespace Eidetic.URack
{

    public class LiveScanReceiver : MonoBehaviour
    {
        static readonly int JobBatchSize = 10000;

        public List<LiveScan3D> StreamingModules = new List<LiveScan3D>();
        public Dictionary<LiveScan3D, float> LastUpdateTime = new Dictionary<LiveScan3D, float>();

        public bool Connected { get; set; }

        public int InputPointCount;
        public int OutputPointCount;

        TcpClient Socket;
        bool ReadyForNextFrame = true;
        bool ReceivedFrame = false;
        bool WaitingForFrame = false;

        Thread NetworkThread;

        public bool Connect(string IP, int port)
        {
            Disconnect();
            try
            {
                Debug.LogFormat("LiveScan: Trying to connect to {0}:{1}", IP, port);
                Socket = new TcpClient(IP, port);
                Connected = true;
                Debug.Log("LiveScan: Success!");

                if (NetworkThread == null)
                {
                    NetworkThread = new Thread(new ThreadStart(ReceiveThread));
                    NetworkThread.IsBackground = true;
                    NetworkThread.Start();
                }
            }
            catch (Exception e)
            {
                Debug.LogError("LiveScan: Could not connect.");
                Debug.LogError("LiveScan: " + e.Message);
            }
            return Connected;
        }
        void Update()
        {
            if (Connected)
            //if (Connected && StreamingModules.Where(m => m.DisableUpdate == 1).Count() == 0)
            {
                if (ReadyForNextFrame)
                {
                    ReadyForNextFrame = false;

                    byte[] byteToSend = new byte[1];
                    byteToSend[0] = 0;

                    Socket.GetStream().Write(byteToSend, 0, 1);

                    WaitingForFrame = true;
                }
                UpdatePoints(Positions, Colors);
            }
        }

        float[] Positions = new float[0];
        byte[] Colors = new byte[0];

        void ReceiveThread()
        {
            while (true)
            {
                while (WaitingForFrame)
                    if (ReceiveFrame())
                    {
                        WaitingForFrame = false;
                        ReadyForNextFrame = true;
                    }
            }
        }

        void UpdatePoints(float[] positions, byte[] colors)
        {
            var pointCount = positions?.Length / 3 ?? 0;
            InputPointCount = pointCount;

            // Job for converting positions and colors arrays into PointCloud.Point[]

            var transferJob = new ConvertToPointsJob()
            {
                positions = new NativeArray<float>(positions, Allocator.TempJob),
                colors = new NativeArray<byte>(colors, Allocator.TempJob),
                points = new NativeArray<PointCloud.Point>(pointCount, Allocator.TempJob)
            };
            transferJob.Schedule(pointCount, JobBatchSize).Complete();

            // then perform processing and set point cloud per active module

            foreach (var module in StreamingModules)
            {
                //if (LastUpdateTime.ContainsKey(module) &&
                    //(Time.time - LastUpdateTime[module] < module.UpdateFrequency)) continue;

                // Job for filtering points based on the module mix/max settings

                var filteredIndices = new NativeList<int>(Allocator.TempJob);
                var filterJob = new BuildPointFilterJob()
                {
                    min = new Vector3(module.MinX, module.MinY, module.MinZ),
                    max = new Vector3(module.MaxX, module.MaxY, module.MaxZ),
                    points = transferJob.points
                };
                filterJob.ScheduleAppend(filteredIndices, pointCount, JobBatchSize).Complete();

                // Job for creating a new PointCloud.Point[] that has only the filtered points

                var buildArrayJob = new BuildFilteredArrayJob()
                {
                    unfilteredPoints = transferJob.points,
                    pointFilter = filteredIndices,
                    filteredPoints = new NativeArray<PointCloud.Point>(filteredIndices.Length, Allocator.TempJob)
                };
                buildArrayJob.Schedule(filteredIndices.Length, JobBatchSize).Complete();

                // Job for transforming each point after the filter 

                var transformJob = new TransformPointsJob()
                {
                    rotation = new Vector3(module.RotationX, module.RotationY, module.RotationZ),
                    translation = new Vector3(module.TranslationX, module.TranslationY, module.TranslationZ),
                    scale = new Vector3(module.ScaleX, module.ScaleY, module.ScaleZ),
                    points = buildArrayJob.filteredPoints
                };
                transformJob.Schedule(buildArrayJob.filteredPoints.Length, JobBatchSize).Complete();

                // Move the processed points to the module's PointCloud instance

                module.PointCloudOutput.Points = new PointCloud.Point[transformJob.points.Length];
                transformJob.points.CopyTo(module.PointCloudOutput.Points);

                OutputPointCount = transformJob.points.Length;

                // Clean up module memory

                filteredIndices.Dispose();
                transformJob.points.Dispose();

                LastUpdateTime[module] = Time.time;
                module.NewFrame = true;
            }

            // Clean up shared memory

            transferJob.positions.Dispose();
            transferJob.colors.Dispose();
            transferJob.points.Dispose();

            ReadyForNextFrame = true;
        }

        int ReadInt()
        {
            byte[] buffer = new byte[4];
            int nRead = 0;
            while (nRead < 4)
                nRead += Socket.GetStream().Read(buffer, nRead, 4 - nRead);

            return BitConverter.ToInt32(buffer, 0);
        }

        bool ReceiveFrame()
        {
            if (!Socket.GetStream().DataAvailable)
            {
                return false;
            }

            int nPointsToRead = ReadInt();

            Positions = new float[3 * nPointsToRead];
            short[] lShortVertices = new short[3 * nPointsToRead];
            Colors = new byte[3 * nPointsToRead];

            int nBytesToRead = sizeof(short) * 3 * nPointsToRead;
            int nBytesRead = 0;
            byte[] buffer = new byte[nBytesToRead];

            while (nBytesRead < nBytesToRead)
                nBytesRead += Socket.GetStream().Read(buffer, nBytesRead, Math.Min(nBytesToRead - nBytesRead, 64000));

            Buffer.BlockCopy(buffer, 0, lShortVertices, 0, nBytesToRead);

            for (int i = 0; i < lShortVertices.Length; i++)
                Positions[i] = lShortVertices[i] / 1000.0f;

            nBytesToRead = sizeof(byte) * 3 * nPointsToRead;
            nBytesRead = 0;
            buffer = new byte[nBytesToRead];

            while (nBytesRead < nBytesToRead)
                nBytesRead += Socket.GetStream().Read(buffer, nBytesRead, Math.Min(nBytesToRead - nBytesRead, 64000));

            Buffer.BlockCopy(buffer, 0, Colors, 0, nBytesToRead);

            return true;
        }

        public void Disconnect()
        {
            if (!Connected) return;
            Debug.Log("LiveScan: Disconnecting...");
            try
            {
                Socket.Close();
                Connected = false;
                Debug.Log("LiveScan: Disconnected!");
            }
            catch { Debug.LogError("Unable to close socket."); }
        }

        public void OnDestroy()
        {
            Disconnect();
        }

        [BurstCompile]
        struct ConvertToPointsJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<float> positions;
            [ReadOnly] public NativeArray<byte> colors;
            public NativeArray<PointCloud.Point> points;
            public void Execute(int pointNumber)
            {
                var point = points[pointNumber];

                var index = pointNumber * 3;

                point.Position.x = positions[index];
                point.Position.y = positions[index + 1];
                point.Position.z = positions[index + 2];

                point.Color.r = colors[index] / 256f;
                point.Color.g = colors[index + 1] / 256f;
                point.Color.b = colors[index + 2] / 256f;

                points[pointNumber] = point;
            }
        }

        [BurstCompile]
        struct BuildPointFilterJob : IJobParallelForFilter
        {
            public Vector3 min;
            public Vector3 max;
            [ReadOnly] public NativeArray<PointCloud.Point> points;

            bool IJobParallelForFilter.Execute(int i)
            {
                var position = points[i].Position;

                if (position.x < min.x || position.x > max.x) return false;
                else if (position.y < min.y || position.y > max.y) return false;
                else if (position.z < min.z || position.z > max.z) return false;

                else return true;
            }
        }

        [BurstCompile]
        struct BuildFilteredArrayJob : IJobParallelFor
        {

            [ReadOnly] public NativeArray<PointCloud.Point> unfilteredPoints;
            [ReadOnly] public NativeArray<int> pointFilter;
            public NativeArray<PointCloud.Point> filteredPoints;

            public void Execute(int i)
            {
                filteredPoints[i] = unfilteredPoints[pointFilter[i]];
            }
        }

        [BurstCompile]
        struct TransformPointsJob : IJobParallelFor
        {
            public Vector3 rotation;
            public Vector3 translation;
            public Vector3 scale;
            public NativeArray<PointCloud.Point> points;
            public void Execute(int i)
            {
                var point = points[i];
                point.Position = point.Position
                    .RotateBy(rotation)
                    .ScaleBy(scale)
                    .TranslateBy(translation);
                points[i] = point;
            }
        }
    }
}