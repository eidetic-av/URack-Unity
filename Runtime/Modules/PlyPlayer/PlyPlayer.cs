using UnityEngine;
using UnityEngine.Jobs;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Eidetic.URack
{
    public class PlyPlayer : UModule
    {
        public string FolderName = "Emanouella";

        [Input] public float Run { get; set; }
        [Input] public float Reset { get; set; }

        [Input] public float Speed { get; set; }
        float FrameTime => Speed.Map(-1, 1, 0.5f, 0.0001f);

        [Input(-5, 5, -180, 180)] public float RotationX {get; set;}
        [Input(-5, 5, -180, 180)] public float RotationY {get; set;}
        [Input(-5, 5, -180, 180)] public float RotationZ {get; set;}
        Vector3 Rotation => new Vector3(RotationX, RotationY, RotationZ);

        [Input] public float PositionX {get; set;}
        [Input] public float PositionY {get; set;}
        [Input] public float PositionZ {get; set;}
        Vector3 Position => new Vector3(PositionX, PositionY, PositionZ);

        [Input] public float Scale {get; set;} = 1;

        public float RGBGain = 0;

        PointCloud pointCloudOutput;
        public PointCloud PointCloudOutput => pointCloudOutput ?? (pointCloudOutput = ScriptableObject.CreateInstance<PointCloud>());

        List<PointCloud> Frames;

        float lastFrameTime;
        int currentFrameNumber;
        PointCloud CurrentFrame
        {
            get
            {
                var delta = Mathf.Abs(Time.time - lastFrameTime);
                if (delta > FrameTime)
                {
                    currentFrameNumber = (currentFrameNumber + 1) % Frames.Count();
                    lastFrameTime = Time.time;
                }
                return Frames[currentFrameNumber];
            }
        }

        public void Start()
        {
            Frames = new List<PointCloud>();
            foreach (var ply in Resources.LoadAll<PointCloud>(FolderName))
            {
                var frame = ScriptableObject.CreateInstance<PointCloud>();
                frame.Points = new PointCloud.Point[ply.PointCount];
                ply.Points.CopyTo(frame.Points, 0);
                Frames.Add(frame);
            }
            Debug.Log($"Loaded {Frames.Count()} frames into PlyPlayer instance");
        }

        public void Update() {
            if (Frames == null) return;
            var framePoints = CurrentFrame.Points;

            var transformJob = new TransformPointsJob() {
                rotation = Rotation,
                translation = Position,
                scale = Vector3.one * Scale,
                rgbGain = RGBGain,
                points = new NativeArray<PointCloud.Point>(framePoints, Allocator.TempJob)
            };

            var jobBatchSize = framePoints.Length > 2000 ? 2000 : framePoints.Length;

            transformJob.Schedule(framePoints.Length, jobBatchSize).Complete();

            PointCloudOutput.Points = new PointCloud.Point[transformJob.points.Length];
            transformJob.points.CopyTo(PointCloudOutput.Points);

            transformJob.points.Dispose();
        }

        [BurstCompile]
        struct TransformPointsJob : IJobParallelFor
        {
            public Vector3 rotation;
            public Vector3 translation;
            public Vector3 scale;
            public float rgbGain;
            public NativeArray<PointCloud.Point> points;
            public void Execute(int i)
            {
                var point = points[i];
                point.Position = point.Position
                    .RotateBy(rotation)
                    .TranslateBy(translation)
                    .ScaleBy(scale);
                point.Color = new Color(point.Color.r + rgbGain, point.Color.g + rgbGain, point.Color.b + rgbGain);
                points[i] = point;
            }
        }

    }
}