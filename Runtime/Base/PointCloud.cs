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

        Point[] points;
        public Point[] Points
        {
            get
            {
                return points ?? (points = new Point[0]);
            }
            set
            {
                if (points != value)
                {
                    points = value;
                    positions = points.Select(p => p.Position).ToArray();
                    colors = points.Select(p => p.Color).ToArray();
                }
            }
        }

        public int PointCount => Positions.Length;

        Vector3[] positions;
        public Vector3[] Positions
        {
            get
            {
                if (positions != null) return positions;
                if (!UsingTextureMaps)
                    return (positions = Points.Select(p => p.Position).ToArray());
                else return (positions = PositionMap.GetPixels().Select(c => new Vector3(c.r, c.g, c.b)).ToArray());
            }
        }

        Color[] colors;
        public Color[] Colors
        {
            get
            {
                if (colors != null) return colors;
                if (!UsingTextureMaps)
                    return (colors = Points.Select(p => p.Color).ToArray());
                else return (colors = ColorMap.GetPixels());
            }
        }

        public bool UsingTextureMaps;
        public Texture2D PositionMap;
        public Texture2D ColorMap;

        [Serializable] public struct Point
        {
            public Vector3 Position;
            public Color Color;
        }

        public(Texture2D, Texture2D) SetPoints(List<Vector3> positions, List<Color32> colors, bool useTextureMaps = false) =>
            SetPoints(positions.ToArray(), colors.ToArray(), useTextureMaps);

        public(Texture2D, Texture2D) SetPoints(Vector3[] positions, Color32[] colors, bool useTextureMaps = false)
        {
            var pointCount = positions.Length;
            UsingTextureMaps = useTextureMaps;

            if (!UsingTextureMaps)
            {
                var setPointsJob = new SetPointsJob()
                {
                    vertices = new NativeArray<Vector3>(positions, Allocator.TempJob),
                    colors = new NativeArray<Color32>(colors, Allocator.TempJob),
                    points = new NativeArray<PointCloud.Point>(pointCount, Allocator.TempJob)
                };
                setPointsJob.Schedule(pointCount, JobBatchSize).Complete();

                Points = new Point[pointCount];
                setPointsJob.points.CopyTo(Points);

                setPointsJob.vertices.Dispose();
                setPointsJob.colors.Dispose();
                setPointsJob.points.Dispose();

                return (null, null);
            }
            else
            {
                int width = MaxTextureSize;
                int height = Mathf.CeilToInt(pointCount / (float) MaxTextureSize);
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
        }

        public void SetPositionMap(Texture2D src)
        {
            PositionMap = new Texture2D(src.width, src.height, TextureFormat.RGBAFloat, false)
            {
                name = "PositionMap",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Repeat
            };
            Graphics.CopyTexture(src, PositionMap);
        }

        public void SetColorMap(Texture2D src)
        {
            ColorMap = new Texture2D(src.width, src.height, TextureFormat.RGBAFloat, false)
            {
                name = "ColorMap",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Repeat
            };
            Graphics.CopyTexture(src, ColorMap);
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