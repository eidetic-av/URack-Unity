using System;
using System.Linq;
using UnityEngine;
using Unity.Jobs;
using Unity.Burst;
using UnityEngine.Jobs;
using Unity.Collections;
using System.Collections.Generic;

namespace Eidetic.URack
{
    [Serializable]
    public class PointCloud : ScriptableObject
    {
        public Point[] Points = new Point[0];
        public int PointCount => Points.Length;
        public Vector3[] Positions => Points.Select(p => p.Position).ToArray();
        public Color[] Colors => Points.Select(p => p.Color).ToArray();

        [Serializable] public struct Point
        {
            public Vector3 Position;
            public Color Color;
        }

        public void SetPoints(List<Vector3> positions, List<Color32> colors) =>
            SetPoints(positions.ToArray(), colors.ToArray());

        public void SetPoints(Vector3[] positions, Color32[] colors)
        {
            var pointCount = positions.Length;

            var loadPointsJob = new SetPointsJob()
            {
                vertices = new NativeArray<Vector3>(positions, Allocator.TempJob),
                colors = new NativeArray<Color32>(colors, Allocator.TempJob),
                points = new NativeArray<PointCloud.Point>(pointCount, Allocator.TempJob)
            };
            int jobBatchSize = pointCount < 2000 ? pointCount : 2000;
            loadPointsJob.Schedule(jobBatchSize, jobBatchSize).Complete();

            Points = new Point[pointCount];
            loadPointsJob.points.CopyTo(Points);

            loadPointsJob.vertices.Dispose();
            loadPointsJob.colors.Dispose();
            loadPointsJob.points.Dispose();
        }

        [BurstCompile]
        struct SetPointsJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<Vector3> vertices;
            [ReadOnly] public NativeArray<Color32> colors;
            public NativeArray<PointCloud.Point> points;
            public void Execute(int i)
            {
                points[i] = new PointCloud.Point()
                {
                    Position = vertices[i],
                    Color = colors[i]
                };
            }
        }
    }
}