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
        [SerializeField] int JobBatchSize = 1024;

        [VFXPropertyBinding("UnityEngine.Texture2D"), FormerlySerializedAs("Positions")]
        public ExposedProperty PositionsProperty = "Positions";

        [VFXPropertyBinding("UnityEngine.Texture2D"), FormerlySerializedAs("Colors")]
        public ExposedProperty ColorsProperty = "Colors";

        PointCloud pointCloud;
        public PointCloud PointCloud
        {
            get => pointCloud;
            set {
                pointCloud = value;
                UpdateMaps = true;
            }
        }

        Texture2D PositionMap;
        Texture2D ColorMap;
        bool UpdateMaps;

        public override void UpdateBinding(VisualEffect visualEffect) {
            if (!UpdateMaps) return;

            int count = PointCloud.PointCount;
            if (count == 0) return;

            int width = MaxTextureSize;
            int height = count / MaxTextureSize;
            if (count < MaxTextureSize) {
                width = count;
                height = Mathf.CeilToInt( count / (float) MaxTextureSize);
            }

            if (PositionMap != null) Destroy(PositionMap);
            if (ColorMap != null) Destroy(ColorMap);

            PositionMap = new Texture2D(width, height, TextureFormat.RGBAFloat, false) {
                name = gameObject.name + "-PositionMap",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Repeat
            };

            ColorMap = new Texture2D(width, height, TextureFormat.RGBAFloat, false) {
                name = gameObject.name + "-ColorMap",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Repeat
            };

            var job = new UpdateJob() {
                points = new NativeArray<PointCloud.Point>(PointCloud.Points, Allocator.TempJob),
                positionMap = new NativeArray<Color>(count, Allocator.TempJob),
                colorMap = new NativeArray<Color>(count, Allocator.TempJob)
            };

            int jobBatchSize = count < JobBatchSize ? count : JobBatchSize;
            job.Schedule(count, jobBatchSize).Complete();

            PositionMap.SetPixelData(job.positionMap, 0);
            PositionMap.Apply();

            ColorMap.SetPixelData(job.colorMap, 0);
            ColorMap.Apply();

            job.points.Dispose();
            job.positionMap.Dispose();
            job.colorMap.Dispose();

            visualEffect.SetTexture(PositionsProperty, PositionMap);
            visualEffect.SetTexture(ColorsProperty, ColorMap);

            UpdateMaps = false;
        }

        [BurstCompile]
        public struct UpdateJob : IJobParallelFor {
            public NativeArray<PointCloud.Point> points;
            public NativeArray<Color> positionMap;
            public NativeArray<Color> colorMap;
            public void Execute(int i) {
                var point = points[i];

                positionMap[i] = new Color() {
                    r = point.Position.x,
                    g = point.Position.y,
                    b = point.Position.z
                };

                colorMap[i] = new Color() {
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