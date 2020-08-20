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

            var threadGroupsX = Mathf.CeilToInt(width / 8f);
            var threadGroupsY = Mathf.CeilToInt(height / 8f);
            SetPointsShader.Dispatch(SetPointsHandle, threadGroupsX, threadGroupsY, 1);

            SetPositionMap(positionsRt);
            SetColorMap(colorsRt);

            Destroy(positionsRt);
            Destroy(colorsRt);
            positionsBuffer.Release();
            colorsBuffer.Release();

            return (PositionMap, ColorMap);
        }

        public (Texture2D, Texture2D) SetPoints(float[] positions, byte[] colors)
        {
            // TODO process this in parrallel
            var pointCount = positions.Length / 3;
            var positionVectors = new Vector3[pointCount];
            var packedColors = new Color[pointCount];
            for (int i = 0; i < pointCount; i++)
            {
                var index = i * 3;
                var x = positions[index] * -1;
                var y = positions[index + 1] * -1;
                var z = positions[index + 2];
                positionVectors[i] = new Vector3(x, y, z);
                var r = colors[index] / 256f;
                var g = colors[index + 1] / 256f;
                var b = colors[index + 2] / 256f;
                packedColors[i] = new Color(r, g, b);
            }
            return SetPoints(positionVectors, packedColors);
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
