using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Jobs;

namespace Eidetic.URack
{
    [Serializable]
    public class PointCloud : ScriptableObject
    {
        const int MaxTextureSize = 4096;
        const int JobBatchSize = 4096;

        public Texture2D PositionMap;
        public Texture2D ColorMap;

        public int PointCount => PositionMap.width * PositionMap.height;

        // TODO get rid of this kind of access (or abstract the maps if necessary):
        public Point[] Points = new Point[0];

        [Serializable]
        public struct Point
        {
            public Vector3 Position;
            public Color Color;
        }

        public (Texture2D, Texture2D) SetPoints(List<Vector3> positions, List<Color32> colors) =>
            SetPoints(positions.ToArray(), colors.ToArray());

        public (Texture2D, Texture2D) SetPoints(Vector3[] positions, Color32[] colors)
        {
            var pointCount = positions.Length;

            int width = MaxTextureSize;
            int height = Mathf.CeilToInt(pointCount / (float)MaxTextureSize);
            var pixelCount = width * height;

            PositionMap = new Texture2D(width, height, TextureFormat.RGBAFloat, false)
            {
                name = "PositionMap",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Repeat
            };
            ColorMap = new Texture2D(width, height, TextureFormat.RGBAFloat, false)
            {
                name = "ColorMap",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Repeat
            };

            var createTexturesJob = new CreateTexturesJob()
            {
                vertices = new NativeArray<Vector3>(positions, Allocator.TempJob),
                colors = new NativeArray<Color32>(colors, Allocator.TempJob),
                positionMap = new NativeArray<Color>(pixelCount, Allocator.TempJob),
                colorMap = new NativeArray<Color>(pixelCount, Allocator.TempJob)
            };

            createTexturesJob.Schedule(pixelCount, JobBatchSize).Complete();

            PositionMap.LoadRawTextureData(createTexturesJob.positionMap);
            ColorMap.LoadRawTextureData(createTexturesJob.colorMap);

            createTexturesJob.vertices.Dispose();
            createTexturesJob.colors.Dispose();
            createTexturesJob.positionMap.Dispose();
            createTexturesJob.colorMap.Dispose();

            return (PositionMap, ColorMap);
        }

        public void SetPositionMap(Texture src)
        {
            Destroy(PositionMap);
            PositionMap = new Texture2D(src.width, src.height, TextureFormat.RGBAFloat, false)
            {
                name = "PositionMap",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Repeat
            };
            Graphics.CopyTexture(src, PositionMap);
        }

        public void SetColorMap(Texture src)
        {
            Destroy(ColorMap);
            ColorMap = new Texture2D(src.width, src.height, TextureFormat.RGBAFloat, false)
            {
                name = "ColorMap",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Repeat
            };
            Graphics.CopyTexture(src, ColorMap);
        }

        [BurstCompile]
        struct CreateTexturesJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<Vector3> vertices;
            [ReadOnly] public NativeArray<Color32> colors;

            public NativeArray<Color> positionMap;
            public NativeArray<Color> colorMap;

            public void Execute(int i)
            {
                // transfer the point if it exists
                if (i < vertices.Length)
                {
                    var vertex = vertices[i];
                    positionMap[i] = new Color()
                    {
                        r = vertex.x,
                        g = vertex.y,
                        b = vertex.z
                    };
                    colorMap[i] = colors[i];
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
    }
}