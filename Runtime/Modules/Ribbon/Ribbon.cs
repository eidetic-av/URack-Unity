using UnityEngine;
using Unity.Mathematics;
using UnityEngine.Rendering.HighDefinition;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Linq;
using Eidetic.URack;
using Eidetic.ColorExtensions;

public class Ribbon : UModule
{
    static readonly int SegmentCount = 20;
    static readonly int VerticesPerSegment = 5;
    static readonly float WidthMultiplier = 0.01f;

    LineRenderer lineRenderer;
    LineRenderer LineRenderer => lineRenderer ?? (lineRenderer = GetComponentsInChildren<LineRenderer>().First());
    List<Vector3> VertexPositions = new List<Vector3>();
    ColorLine VertexColors = new ColorLine();

    Vector3 HeadPosition = new Vector3(0, 0, 0);
    [Input] public float X { set => HeadPosition.x = value; }
    [Input] public float Y { set => HeadPosition.y = value; }
    [Input] public float Z { set => HeadPosition.z = value; }

    [Input] public float OriginX { set => transform.position = new Vector3(value, transform.position.y, transform.position.z); }
    [Input] public float OriginY { set => transform.position = new Vector3(transform.position.x, value, transform.position.z); }
    [Input] public float OriginZ { set => transform.position = new Vector3(transform.position.x, transform.position.y, value); }

    [Input]
    public float RotateX
    {
        set
        {
            var euler = transform.rotation.eulerAngles;
            transform.rotation = Quaternion.Euler(euler.x, euler.y, value.Map(-5, 5, -180, 180));
        }
    }
    [Input]
    public float RotateY
    {
        set
        {
            var euler = transform.rotation.eulerAngles;
            transform.rotation = Quaternion.Euler(euler.x, value.Map(-5, 5, -180, 180), euler.z);
        }
    }

    Vector3 HeadHSV = new Vector3(0f, 1f, 1f);
    [Input] public float Hue { set => HeadHSV.x = value / 10; }
    [Input] public float Saturation { set => HeadHSV.y = value / 10; }
    [Input] public float Brightness { set => HeadHSV.z = value / 10; }

    [Input]
    public float Glow
    {
        set => LineRenderer.material.SetFloat("_EmissiveExposureWeight", value.Map(0, 10, 1, 0.85f));
    }

    float width = 1f;
    [Input(0, 10, 0, 20, true, 2)]
    public float Width
    {
        set
        {
            width = value * 10;
            LineRenderer.widthCurve = new AnimationCurve(
                new Keyframe(0, width * WidthMultiplier * tail),
                new Keyframe(1, width * WidthMultiplier));
        }
    }
    float tail = 1f;
    [Input]
    public float Tail
    {
        set
        {
            tail = 1 + (value / 5);
            LineRenderer.widthCurve = new AnimationCurve(
                new Keyframe(0, width * WidthMultiplier * tail),
                new Keyframe(1, width * WidthMultiplier));
        }
    }

    int PointCount = 0;
    [Input]
    public float Length
    {
        set
        {
            if (value < 0) return;
            PointCount = VerticesPerSegment * Mathf.RoundToInt((SegmentCount + 1) * (value / 2));
            while (VertexPositions.Count < PointCount)
            {
                VertexPositions.Insert(0, VertexPositions.Count > 0 ? VertexPositions[0] : HeadPosition);
                VertexColors.Insert(0, VertexColors.Count > 0 ? VertexColors[0] : HeadHSV.AsHSVColor());
            }

            while (VertexPositions.Count > PointCount)
            {
                VertexPositions.RemoveAt(0);
                VertexColors.RemoveAt(0);
            }

            LineRenderer.positionCount = PointCount;
        }
    }

    public void Update()
    {
        if (PointCount == 0) return;

        // Advance vertices
        for (var i = 0; i < PointCount - 1; i++)
        {
            VertexPositions[i] = VertexPositions[i + 1];
            VertexColors[i] = VertexColors[i + 1];
        }

        // Set "head" position and color
        VertexPositions[PointCount - 1] = HeadPosition;
        VertexColors[PointCount - 1] = HeadHSV.AsHSVColor();

        // Transfer positions to the LineRenderer
        LineRenderer.SetPositions(VertexPositions.ToArray());

        // Transfer colors to the material
        var colorTexture = VertexColors.ToTexture();
        LineRenderer.material.SetTexture("_BaseColorMap", colorTexture);
        LineRenderer.material.SetTexture("_EmissiveColorMap", colorTexture);
    }
}
