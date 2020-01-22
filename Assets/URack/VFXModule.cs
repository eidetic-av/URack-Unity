using System.Linq;
using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.VFX.Utility;

namespace Eidetic.URack
{
    public abstract class VFXModule : UModule
    {
        public virtual VisualEffectAsset TemplateAsset { get; set; }

        [SerializeField] VisualEffect VisualEffect;

        VFXPropertyBinder binder;
        internal VFXPropertyBinder Binder => binder ?? (binder = VisualEffect.gameObject.GetComponent<VFXPropertyBinder>());

        PointCloudBinder pointCloudBinder;
        internal PointCloudBinder PointCloudBinder => pointCloudBinder ??
            (pointCloudBinder = Binder.GetParameterBinders<PointCloudBinder>().Single());

        public virtual void Start()
        {
            VisualEffect.Play();
        }

        public void Exit()
        {
            gameObject?.SetActive(false);
        }

        [Input]
        public PointCloud PointCloudInput
        {
            set
            {
                PointCloudBinder.PointCloud = value;
                VisualEffect.SetInt("PointCount", value.PointCount);
            }
        }
    }
}