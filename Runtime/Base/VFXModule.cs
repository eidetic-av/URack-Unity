using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.VFX.Utility;

namespace Eidetic.URack
{
    public abstract class VFXModule : UModule
    {
        public virtual VisualEffectAsset TemplateAsset { get; set; }

        VisualEffect visualEffect;
        public VisualEffect VisualEffect => visualEffect ?? (visualEffect = GetComponent<VisualEffect>());

        VFXPropertyBinder binder;
        internal VFXPropertyBinder Binder => binder ?? (binder = VisualEffect.gameObject.GetComponent<VFXPropertyBinder>());

        PointCloudBinder pointCloudBinder;
        internal PointCloudBinder PointCloudBinder => pointCloudBinder ??
            (pointCloudBinder = Binder.GetPropertyBinders<PointCloudBinder>().Single());

        public virtual void Start() => VisualEffect?.Play();

        public void Exit() => gameObject?.SetActive(false);

        [Input]
        virtual public PointCloud PointCloudInput
        {
            set
            {
                PointCloudBinder.PointCloud = value;
                if (VisualEffect != null) VisualEffect.SetInt("PointCount", value.PointCount);
            }
        }
    }
}