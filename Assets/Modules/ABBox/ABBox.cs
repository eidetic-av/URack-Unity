using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Serialization;
using System.Collections;
using System.Collections.Generic;

namespace Eidetic.URack
{
    public class ABBox : VFXModule
    {
        const int JobBatchSize = 1000;

        [Input]
        public override PointCloud PointCloud
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
                    positionX = PositionX,
                    positionY = PositionY,
                    positionZ = PositionZ,
                    scaleX = ScaleX,
                    scaleY = ScaleY,
                    scaleZ = ScaleZ
                };
                insideJob.ScheduleAppend(insideIndices, pointCount, JobBatchSize).Complete();

                var outsideIndices = new NativeList<int>(Allocator.TempJob);
                var outsideJob = new OutsideFilter()
                {
                    points = pointArray,
                    positionX = PositionX,
                    positionY = PositionY,
                    positionZ = PositionZ,
                    scaleX = ScaleX,
                    scaleY = ScaleY,
                    scaleZ = ScaleZ
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

        [Input] public float PositionX { get; set; }

        [Input] public float PositionY { get; set; }

        [Input] public float PositionZ { get; set; }

        [Input] public float ScaleX { get; set; }

        [Input] public float ScaleY { get; set; }

        [Input] public float ScaleZ { get; set; }

        PointCloud outside;
        public PointCloud Outside => outside ?? (outside = ScriptableObject.CreateInstance<PointCloud>());

        PointCloud inside;
        public PointCloud Inside => inside ?? (inside = ScriptableObject.CreateInstance<PointCloud>());


        [BurstCompile]
        public struct InsideFilter : IJobParallelForFilter
        {
            public NativeArray<PointCloud.Point> points;
            public float positionX;
            public float positionY;
            public float positionZ;
            public float scaleX;
            public float scaleY;
            public float scaleZ;
            bool IJobParallelForFilter.Execute(int i)
            {
                var position = points[i].Position;

                bool inside = false;

                float minX = positionX - (scaleX / 2);
                float minY = positionY - (scaleY / 2);
                float minZ = positionZ - (scaleZ / 2);
                float maxX = positionX + (scaleX / 2);
                float maxY = positionY + (scaleY / 2);
                float maxZ = positionZ + (scaleZ / 2);
                
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
            public float positionX;
            public float positionY;
            public float positionZ;
            public float scaleX;
            public float scaleY;
            public float scaleZ;
            bool IJobParallelForFilter.Execute(int i)
            {
                var position = points[i].Position;

                float minX = positionX - (scaleX / 2);
                float minY = positionY - (scaleY / 2);
                float minZ = positionZ - (scaleZ / 2);
                float maxX = positionX + (scaleX / 2);
                float maxY = positionY + (scaleY / 2);
                float maxZ = positionZ + (scaleZ / 2);

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