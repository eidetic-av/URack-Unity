using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;
using static UnityEngine.Experimental.Rendering.GraphicsFormat;

namespace Eidetic.URack
{
    [Serializable]
    public class PointCloud : ScriptableObject
    {
        const int MaxTextureSize = 4096;

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

        ComputeShader setPointsShader;
        ComputeShader SetPointsShader => setPointsShader ??
            (setPointsShader = Resources.Load<ComputeShader>("PointCloudSetPointsShader"));
        int SetPointsHandle => SetPointsShader.FindKernel("SetPoints");

        public (Texture2D, Texture2D) SetPoints(IEnumerable<Vector3> positions, IEnumerable<Color> colors) =>
            SetPoints(positions.ToArray(), colors.ToArray());

        public (Texture2D, Texture2D) SetPoints(Vector3[] positions, Color[] colors)
        {
            var pointCount = positions.Length;

            int width = MaxTextureSize;
            int height = Mathf.CeilToInt(pointCount / (float)MaxTextureSize);
            var pixelCount = width * height;

            var positionsBuffer = new ComputeBuffer(positions.Length, 12);
            positionsBuffer.SetData(positions);
            var colorsBuffer = new ComputeBuffer(positions.Length, 16);
            colorsBuffer.SetData(colors);

            var positionsRt = new RenderTexture(width, height, 24, R32G32B32A32_SFloat);
            positionsRt.enableRandomWrite = true;
            positionsRt.Create();
            var colorsRt = new RenderTexture(width, height, 24, R32G32B32A32_SFloat);
            colorsRt.enableRandomWrite = true;
            colorsRt.Create();

            SetPointsShader.SetBuffer(SetPointsHandle, "Positions", positionsBuffer);
            SetPointsShader.SetBuffer(SetPointsHandle, "Colors", colorsBuffer);
            SetPointsShader.SetInt("Width", width);
            SetPointsShader.SetInt("PointCount", pointCount);
            SetPointsShader.SetTexture(SetPointsHandle, "PositionMap", positionsRt);
            SetPointsShader.SetTexture(SetPointsHandle, "ColorMap", colorsRt);

            SetPointsShader.Dispatch(SetPointsHandle, width / 8, height / 8, 1);

            SetPositionMap(positionsRt);
            SetColorMap(colorsRt);

            Destroy(positionsRt);
            Destroy(colorsRt);
            positionsBuffer.Release();
            colorsBuffer.Release();

            return (PositionMap, ColorMap);
        }

        public void SetPositionMap(Texture src)
        {
            if (PositionMap == null)
            {
                PositionMap = new Texture2D(src.width, src.height, TextureFormat.RGBAFloat, false)
                {
                    name = "PositionMap",
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Repeat
                };
            }
            else if (PositionMap.height != src.height)
            {
                PositionMap.Resize(src.width, src.height);
                PositionMap.Apply();
            }
            Graphics.CopyTexture(src, PositionMap);
        }

        public void SetColorMap(Texture src)
        {
            if (ColorMap == null)
            {
                ColorMap = new Texture2D(src.width, src.height, TextureFormat.RGBAFloat, false)
                {
                    name = "ColorMap",
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Repeat
                };
            }
            else if (ColorMap.height != src.height)
            {
                ColorMap.Resize(src.width, src.height);
                ColorMap.Apply();
            }
            Graphics.CopyTexture(src, ColorMap);
        }
    }
}
