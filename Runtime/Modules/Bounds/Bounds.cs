using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Serialization;
using System.Collections;
using System.Collections.Generic;

namespace Eidetic.URack
{
    public class Bounds : VFXModule
    {
        const int JobBatchSize = 1000;

        [Input]
        public override PointCloud PointCloudInput
        {
            set
            {
                if (!Active) return;

                var pointCount = value.PointCount;

                var pointArray = new NativeArray<PointCloud.Point>(value.Points, Allocator.TempJob);

                var insideIndices = new NativeList<int>(Allocator.TempJob);
                var insideJob = new InsideFilter()
                {
                    points = pointArray,
                    minX = MinX,
                    minY = MinY,
                    minZ = MinZ,
                    maxX = MaxX,
                    maxY = MaxY,
                    maxZ = MaxZ
                };
                insideJob.ScheduleAppend(insideIndices, pointCount, JobBatchSize).Complete();

                var outsideIndices = new NativeList<int>(Allocator.TempJob);
                var outsideJob = new OutsideFilter()
                {
                    points = pointArray,
                    minX = MinX,
                    minY = MinY,
                    minZ = MinZ,
                    maxX = MaxX,
                    maxY = MaxY,
                    maxZ = MaxZ
                };
                outsideJob.ScheduleAppend(outsideIndices, pointCount, JobBatchSize).Complete();

                var buildInsideArrayJob = new LiveScanReceiver.BuildFilteredArrayJob()
                {
                    unfilteredPoints = pointArray,
                    pointFilter = insideIndices,
                    filteredPoints = new NativeArray<PointCloud.Point>(insideIndices.Length, Allocator.TempJob)
                };
                buildInsideArrayJob.Schedule(insideIndices.Length, JobBatchSize).Complete();

                var buildOutsideArrayJob = new LiveScanReceiver.BuildFilteredArrayJob()
                {
                    unfilteredPoints = pointArray,
                    pointFilter = outsideIndices,
                    filteredPoints = new NativeArray<PointCloud.Point>(outsideIndices.Length, Allocator.TempJob)
                };
                buildOutsideArrayJob.Schedule(outsideIndices.Length, JobBatchSize).Complete();

                Inside.Points = new PointCloud.Point[insideIndices.Length];
                buildInsideArrayJob.filteredPoints.CopyTo(Inside.Points);

                Outside.Points = new PointCloud.Point[outsideIndices.Length];
                buildOutsideArrayJob.filteredPoints.CopyTo(Outside.Points);

                pointArray.Dispose();
                insideIndices.Dispose();
                outsideIndices.Dispose();
                buildInsideArrayJob.filteredPoints.Dispose();
                buildOutsideArrayJob.filteredPoints.Dispose();
            }
        }

        [Input] public float MinX { get; set; }

        [Input] public float MinY { get; set; }

        [Input] public float MinZ { get; set; }

        [Input] public float MaxX { get; set; }

        [Input] public float MaxY { get; set; }

        [Input] public float MaxZ { get; set; }

        PointCloud outside;
        public PointCloud Outside => outside ?? (outside = ScriptableObject.CreateInstance<PointCloud>());

        PointCloud inside;
        public PointCloud Inside => inside ?? (inside = ScriptableObject.CreateInstance<PointCloud>());


        [BurstCompile]
        public struct InsideFilter : IJobParallelForFilter
        {
            public NativeArray<PointCloud.Point> points;
            public float minX;
            public float minY;
            public float minZ;
            public float maxX;
            public float maxY;
            public float maxZ;
            bool IJobParallelForFilter.Execute(int i)
            {
                var position = points[i].Position;

                bool inside = false;
                if (position.x < maxX && position.x > minX)
                    if (position.y < maxY && position.y > minY)
                        if (position.z < maxZ && position.z > minZ)
                            inside = true;

                return inside;
            }
        }

        [BurstCompile]
        public struct OutsideFilter : IJobParallelForFilter
        {
            public NativeArray<PointCloud.Point> points;
            public float minX;
            public float minY;
            public float minZ;
            public float maxX;
            public float maxY;
            public float maxZ;
            bool IJobParallelForFilter.Execute(int i)
            {
                var position = points[i].Position;

                bool outside = true;
                if (position.x < maxX && position.x > minX)
                    if (position.y < maxY && position.y > minY)
                        if (position.z < maxZ && position.z > minZ)
                            outside = false;

                return outside;
            }
        }
    }
}