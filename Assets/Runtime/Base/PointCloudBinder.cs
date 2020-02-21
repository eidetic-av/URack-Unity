using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.VFX;
using UnityEngine.VFX.Utility;

namespace Eidetic.URack
{
    [AddComponentMenu("VFX/Property Binders/URack/PointCloud Binder")]
    [VFXBinder("URack/PointCloud Binder")]
    public class PointCloudBinder : VFXBinderBase
    {
        [SerializeField] int MaxTextureSize = 8192;
        [SerializeField] int JobBatchSize = 2000;

        [VFXPropertyBinding("UnityEngine.Texture2D"), FormerlySerializedAs("Positions")]
        public ExposedProperty PositionsProperty = "Positions";

        [VFXPropertyBinding("UnityEngine.Texture2D"), FormerlySerializedAs("Colors")]
        public ExposedProperty ColorsProperty = "Colors";

        PointCloud pointCloud;
        public PointCloud PointCloud
        {
            get => pointCloud;
            set
            {
                pointCloud = value;
                UpdateMaps = true;
            }
        }

        Texture2D PositionMap;
        Texture2D ColorMap;
        bool UpdateMaps;

        public override void UpdateBinding(VisualEffect visualEffect)
        {
            if (!UpdateMaps) return;

            var pointCount = PointCloud.PointCount;
            if (pointCount == 0) return;

            var width = MaxTextureSize;
            var height = pointCount / MaxTextureSize;
            if (pointCount > 0 && pointCount < MaxTextureSize)
            {
                width = pointCount;
                height = 1;
            }

            if (PositionMap != null) Destroy(PositionMap);
            if (ColorMap != null) Destroy(ColorMap);

            PositionMap = new Texture2D(width, height, TextureFormat.RGBAFloat, false)
            {
                name = gameObject.name + "-PositionMap",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Repeat
            };

            ColorMap = new Texture2D(width, height, TextureFormat.RGBAFloat, false)
            {
                name = gameObject.name + "-ColorMap",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Repeat
            };

            var job = new UpdateJob()
            {
                points = new NativeArray<PointCloud.Point>(PointCloud.Points, Allocator.TempJob),
                positionMap = new NativeArray<Color>(pointCount, Allocator.TempJob),
                colorMap = new NativeArray<Color>(pointCount, Allocator.TempJob)
            };

            var jobBatchSize = pointCount < JobBatchSize ? pointCount : JobBatchSize;
            job.Schedule(pointCount, jobBatchSize)
                .Complete();

            PositionMap.SetPixelData(job.positionMap, 0);
            ColorMap.SetPixelData(job.colorMap, 0);

            PositionMap.Apply();
            ColorMap.Apply();

            job.points.Dispose();
            job.positionMap.Dispose();
            job.colorMap.Dispose();

            visualEffect.SetTexture(PositionsProperty, PositionMap);
            visualEffect.SetTexture(ColorsProperty, ColorMap);

            UpdateMaps = false;
        }

        [BurstCompile]
        public struct UpdateJob : IJobParallelFor
        {
            public NativeArray<PointCloud.Point> points;
            public NativeArray<Color> positionMap;
            public NativeArray<Color> colorMap;
            public void Execute(int i)
            {
                var point = points[i];

                positionMap[i] = new Color()
                {
                    r = point.Position.x,
                    g = point.Position.y,
                    b = point.Position.z
                };

                colorMap[i] = new Color()
                {
                    r = point.Color.r,
                    g = point.Color.g,
                    b = point.Color.b
                };
            }
        }

        public override bool IsValid(VisualEffect component) =>
            component.HasTexture(PositionsProperty) && component.HasTexture(ColorsProperty);
    }
}