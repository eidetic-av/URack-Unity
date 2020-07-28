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
        [SerializeField] int MaxTextureSize = 4096;
        [SerializeField] int JobBatchSize = 4096;

        [VFXPropertyBinding("UnityEngine.Texture2D"), FormerlySerializedAs("Positions")]
        public ExposedProperty PositionsProperty = "Positions";

        [VFXPropertyBinding("UnityEngine.Texture2D"), FormerlySerializedAs("Colors")]
        public ExposedProperty ColorsProperty = "Colors";

        [VFXPropertyBinding("int"), FormerlySerializedAs("PointCount")]
        public ExposedProperty PointCountProperty = "PointCount";

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

            if (PointCloud.UsingTextureMaps)
            {
                visualEffect.SetTexture(PositionsProperty, PointCloud.PositionMap);
                visualEffect.SetTexture(ColorsProperty, PointCloud.ColorMap);
                var texturePixelCount = PointCloud.PositionMap.width * PointCloud.PositionMap.height;
                visualEffect.SetInt(PointCountProperty, texturePixelCount);
                UpdateMaps = false;
                return;
            }

            int count = PointCloud.PointCount;
            if (count == 0) return;

            int width = MaxTextureSize;
            int height = Mathf.CeilToInt(count / (float) MaxTextureSize);
            var pixelCount = width * height;

            // If the textures are undefined, create them
            // and if they are a different size, resize them
            if (PositionMap == null)
            {
                PositionMap = new Texture2D(width, height, TextureFormat.RGBAFloat, false)
                {
                name = gameObject.name + "-PositionMap",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Repeat
                };
            }
            else if (PositionMap.height != height)
            {
                PositionMap.Resize(width, height, TextureFormat.RGBAFloat, false);
            }

            if (ColorMap == null)
            {
                ColorMap = new Texture2D(width, height, TextureFormat.RGBAFloat, false)
                {
                name = gameObject.name + "-ColorMap",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Repeat
                };
            }
            else if (ColorMap.height != height)
            {
                ColorMap.Resize(width, height, TextureFormat.RGBAFloat, false);
            }

            var job = new UpdateJob()
            {
                points = new NativeArray<PointCloud.Point>(PointCloud.Points, Allocator.TempJob),
                positionMap = new NativeArray<Color>(pixelCount, Allocator.TempJob),
                colorMap = new NativeArray<Color>(pixelCount, Allocator.TempJob)
            };

            job.Schedule(pixelCount, JobBatchSize).Complete();

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
        public struct UpdateJob : IJobParallelFor
        {
            public NativeArray<PointCloud.Point> points;
            public NativeArray<Color> positionMap;
            public NativeArray<Color> colorMap;
            public void Execute(int i)
            {
                // transfer the point if it exists
                if (i < points.Length)
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
                // if it doesn't exist for this index,
                // fill the remaining pixels of the texture
                // with a dummy value
                // transparent and out of the way
                else
                {
                    positionMap[i] = Color.white * 5000f;
                    colorMap[i] = Color.clear;
                }
            }
        }

        public override bool IsValid(VisualEffect component) =>
            component.HasTexture(PositionsProperty) && component.HasTexture(ColorsProperty);
    }
}