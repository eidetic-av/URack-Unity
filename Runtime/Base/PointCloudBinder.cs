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

            if (PointCloud.PositionMap == null) return;
            visualEffect.SetTexture(PositionsProperty, PointCloud.PositionMap);
            visualEffect.SetTexture(ColorsProperty, PointCloud.ColorMap);
            var texturePixelCount = PointCloud.PositionMap.width * PointCloud.PositionMap.height;
            visualEffect.SetInt(PointCountProperty, texturePixelCount);
            UpdateMaps = false;
            return;
        }

        public override bool IsValid(VisualEffect component) =>
            component.HasTexture(PositionsProperty) && component.HasTexture(ColorsProperty);
    }
}