using Eidetic.ColorExtensions;
using Eidetic.URack;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

public class GlowWorm : UModule {

    const int SegmentCount = 20;
    const int VerticesPerSegment = 5;
    const float WidthMultiplier = 0.01f;

    LineRenderer lineRenderer;
    LineRenderer LineRenderer => lineRenderer ?? (lineRenderer = GetComponentsInChildren<LineRenderer>().First());
    List<Vector3> VertexPositions = new List<Vector3>();
    ColorLine VertexColors = new ColorLine();

    Vector3 TipPosition = new Vector3(0, 0, 0);
    [Input(-5f, 5f, -1.5f, 1.5f)] public float TipX { set => TipPosition.x = value; }
    [Input(-5f, 5f, -1.5f, 1.5f)] public float TipY { set => TipPosition.y = value; }
    [Input(-5f, 5f, -1.5f, 1.5f)] public float TipZ { set => TipPosition.z = value; }

    [Input] public float OriginX { set => transform.position =
            new Vector3(value, transform.position.y, transform.position.z); }
    [Input] public float OriginY { set => transform.position =
            new Vector3(transform.position.x, value, transform.position.z); }
    [Input] public float OriginZ { set => transform.position =
            new Vector3(transform.position.x, transform.position.y, value); }

    [Input] public float RotationX {
        set {
            var euler = transform.rotation.eulerAngles;
            transform.rotation = Quaternion.Euler(euler.x, euler.y, value.Map(-5, 5, -180, 180));
        }
    }
    [Input] public float RotationY {
        set {
            var euler = transform.rotation.eulerAngles;
            transform.rotation = Quaternion.Euler(euler.x, value.Map(-5, 5, -180, 180), euler.z);
        }
    }
    [Input] public float RotationZ {
        set {
            var euler = transform.rotation.eulerAngles;
            transform.rotation = Quaternion.Euler(euler.x, euler.y, value.Map(-5, 5, -180, 180));
        }
    }

    Vector3 HeadHSV = new Vector3(0f, 1f, 1f);
    [Input] public float Hue { set => HeadHSV.x = Mathf.Clamp(value, 0, 10) / 10f; }
    [Input] public float Saturation { set => HeadHSV.y = Mathf.Clamp(value, 0, 10) / 10f; }
    [Input] public float Brightness { set => HeadHSV.z = Mathf.Clamp(value, 0, 10) / 10f; }

    [Input] public float Glow { set =>
            LineRenderer.material.SetFloat("_EmissiveExposureWeight", value.Map(0, 10, 1, 0.85f)); }

    int PointCount = 0;
    [Input]
    public float Length {
        set {
            var lengthValue = Mathf.Clamp(value, 0, 10f) / 2f;
            PointCount = VerticesPerSegment * Mathf.RoundToInt((SegmentCount + 1) * lengthValue);
            while (VertexPositions.Count < PointCount) {
                VertexPositions.Insert(0, VertexPositions.Count > 0 ? VertexPositions[0] : TipPosition);
                VertexColors.Insert(0, VertexColors.Count > 0 ? VertexColors[0] : HeadHSV.AsHSVColor());
            }
            while (VertexPositions.Count > PointCount) {
                VertexPositions.RemoveAt(0);
                VertexColors.RemoveAt(0);
            }
        }
    }

    float width = 1f;
    [Input(0, 10, 0, 20, true, 2)] public float Width {
        set {
            width = value * 10;
            LineRenderer.widthCurve = new AnimationCurve(
                new Keyframe(0, width * WidthMultiplier * tail),
                new Keyframe(1, width * WidthMultiplier));
        }
    }

    float tail = 1f;
    [Input] public float Tail {
        set {
            tail = 1 + (value / 5f);
            LineRenderer.widthCurve = new AnimationCurve(
                new Keyframe(0, width * WidthMultiplier * tail),
                new Keyframe(1, width * WidthMultiplier));
        }
    }

    [Input] public float CapType { set => LineRenderer.numCapVertices = value > 0 ? 20 : 0; }

    public void FixedUpdate() {
        // Advance vertices done in fixed update so it isn't bound to the
        // graphics frame-rate
        for (var i = 0; i < PointCount - 1; i++) {
            VertexPositions[i] = VertexPositions[i + 1];
            VertexColors[i] = VertexColors[i + 1];
        }
        if (PointCount == 0) return;

        // Set tip position and color
        VertexPositions[PointCount - 1] = TipPosition;
        VertexColors[PointCount - 1] = HeadHSV.AsHSVColor();

        // Transfer positions to the LineRenderer
        LineRenderer.positionCount = PointCount;
        LineRenderer.SetPositions(VertexPositions.ToArray());

        // Transfer colors to the material's texture
        var colorTexture = VertexColors.ToTexture();
        LineRenderer.material.SetTexture("_BaseColorMap", colorTexture);
        LineRenderer.material.SetTexture("_EmissiveColorMap", colorTexture);
    }
}
