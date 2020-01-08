using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Eidetic.URack
{
    public class Arena : UModule
    {
        [SerializeField] Camera Camera;
        [SerializeField] GameObject CameraOrigin;
        [SerializeField] GameObject ForwardAxis;
        [SerializeField] GameObject BackwardAxis;
        [SerializeField] GameObject UpAxis;
        [SerializeField] GameObject DownAxis;
        [SerializeField] Volume PostProcessing;
        [SerializeField] GameObject OriginMarker;

        Exposure ExposureComponent;
        void Start()
        {
            Exposure exposure;
            if (PostProcessing.profile.TryGet<Exposure>(out exposure))
                ExposureComponent = exposure;
        }

        public float CameraOriginX
        {
            set => CameraOrigin.transform.position = CameraOrigin.transform.position.Replace(0, value.Map(-10, 10));
        }
        public float CameraOriginY
        {
            set => CameraOrigin.transform.position = CameraOrigin.transform.position.Replace(1, value.Map(-10, 10));
        }
        public float CameraOriginZ
        {
            set => CameraOrigin.transform.position = CameraOrigin.transform.position.Replace(2, value.Map(-10, 10));
        }

        float cameraDistance;
        public float CameraDistance
        {
            set
            {
                var cameraDistance = value.Clamp(0, 10);
                while (Vector3.Distance(Camera.transform.position, CameraOrigin.transform.position) > cameraDistance + 0.005f)
                    Camera.transform.position = Vector3.MoveTowards(Camera.transform.position, ForwardAxis.transform.position, 0.001f);
                while (Vector3.Distance(Camera.transform.position, CameraOrigin.transform.position) < cameraDistance - 0.005f)
                    Camera.transform.position = Vector3.MoveTowards(Camera.transform.position, BackwardAxis.transform.position, 0.001f);
            }
        }

        float cameraOrbitX;
        public float CameraOrbitX
        {
            set
            {
                var newValue = value.Map(-180f, 180f);
                var diff = cameraOrbitX - newValue;
                CameraOrigin.transform.Rotate(0, diff, -CameraOrigin.transform.eulerAngles.z);
                cameraOrbitX = newValue;
            }
        }

        float cameraOrbitY;
        public float CameraOrbitY
        {
            set
            {
                var newValue = value.Map(-180f, 180f);
                var diff = cameraOrbitY - newValue;
                CameraOrigin.transform.Rotate(diff, 0, -CameraOrigin.transform.eulerAngles.z);
                cameraOrbitY = newValue;
            }
        }

        public float CameraFocalLength
        {
            set => Camera.focalLength = value.Clamp(0, 10).Map(0, 10, 8, 180);
        }
        public float Exposure
        {
            set => ExposureComponent.fixedExposure.value = value.Map(0, 20);
        }
        public bool MarkerEnable
        {
            set => OriginMarker.SetActive(value);
        }
    }

}