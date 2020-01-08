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
                var diff = cameraDistance - value;
                if (diff > 0)
                    Camera.transform.position = Vector3.MoveTowards(Camera.transform.position, CameraOrigin.transform.position, Mathf.Abs(diff));
                else
                    Camera.transform.position = Vector3.MoveTowards(Camera.transform.position, BackwardAxis.transform.position, Mathf.Abs(diff));
                while (Vector3.Distance(Camera.transform.position, CameraOrigin.transform.position) > 10f)
                    Camera.transform.position = Vector3.MoveTowards(Camera.transform.position, CameraOrigin.transform.position, 0.01f);
                while (Vector3.Distance(Camera.transform.position, CameraOrigin.transform.position) < 0f)
                    Camera.transform.position = Vector3.MoveTowards(Camera.transform.position, BackwardAxis.transform.position, 0.01f);
                cameraDistance = value;
            }
        }

        float cameraHeight;
        public float CameraHeight
        {
            set
            {
                var diff = cameraHeight - value;
                if (diff > 0)
                    Camera.transform.position = Vector3.MoveTowards(Camera.transform.position, DownAxis.transform.position, Mathf.Abs(diff));
                else
                    Camera.transform.position = Vector3.MoveTowards(Camera.transform.position, UpAxis.transform.position, Mathf.Abs(diff));
                if (Camera.transform.position.y > 10f) Camera.transform.position = Camera.transform.position.Replace(1, 10f);
                else if (Camera.transform.position.y < -10f) Camera.transform.position = Camera.transform.position.Replace(1, -10f);
                Camera.transform.LookAt(CameraOrigin.transform);
                cameraHeight = value;
            }
        }

        float cameraOrbit;
        public float CameraOrbit
        {
            set
            {
                var newValue = value.Map(-180, 180);
                Camera.transform.RotateAround(CameraOrigin.transform.position, Vector3.up, newValue - cameraOrbit);
                Camera.transform.LookAt(CameraOrigin.transform);
                cameraOrbit = newValue;
            }
        }

        public float CameraFocalLength
        {
            set => Camera.focalLength = value.Map(6, 42);
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