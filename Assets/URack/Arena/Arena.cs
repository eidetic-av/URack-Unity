using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Eidetic.URack
{
    public class Arena : UModule
    {

        //----------------------------------
        // Constants
        //----------------------------------
        static readonly Color[] Monochromatic = new Color[] { Color.HSVToRGB(0, .7f, 1), Color.HSVToRGB(0, 1, 1), Color.HSVToRGB(0, .7f, .5f) };
        static readonly Color[] Analogous = new Color[] { Color.HSVToRGB(0.05f, .95f, 1), Color.HSVToRGB(0, 1, 1), Color.HSVToRGB(0.83333f, .95f, 1f) };
        static readonly Color[] Triadic = new Color[] { Color.HSVToRGB(0.16388f, .9f, 1), Color.HSVToRGB(0, 1, 1), Color.HSVToRGB(0.56388f, .95f, 1f) };
        static readonly Color[] Complementary = new Color[] { Color.HSVToRGB(0.33888f, 1f, 1), Color.HSVToRGB(0, 1, 1), Color.HSVToRGB(0.42222f, .95f, 1f) };

        //----------------------------------
        // Prefab objects/components
        //----------------------------------
        [SerializeField] Camera Camera;
        [SerializeField] GameObject CameraOrigin;
        [SerializeField] GameObject ForwardAxis;
        [SerializeField] GameObject BackwardAxis;
        [SerializeField] GameObject UpAxis;
        [SerializeField] GameObject DownAxis;
        [SerializeField] Volume PostProcessing;
        [SerializeField] Volume Sky;
        [SerializeField] GameObject OriginMarker;

        //----------------------------------
        // Private variables
        //----------------------------------
        Exposure ExposureComponent;
        GradientSky GradientSkyComponent;
        Vector3 Orbit = Vector3.zero;

        //----------------------------------
        // Setters for applying voltages
        //----------------------------------
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
                Orbit = Orbit.Replace(1, newValue);
                var diff = cameraOrbitX - newValue;
                // CameraOrigin.transform.Rotate(0, diff, 0);
                cameraOrbitX = newValue;
            }
        }
        float cameraOrbitY;
        public float CameraOrbitY
        {
            set
            {
                var newValue = value.Clamp().Map(89.99f, -89.99f);
                Orbit = Orbit.Replace(0, newValue);
                var diff = cameraOrbitY - newValue;
                // CameraOrigin.transform.Rotate(diff, 0, 0);
                cameraOrbitY = newValue;
            }
        }
        public float CameraFocalLength
        {
            set => Camera.focalLength = value.Clamp(0, 10).Map(0, 10, 8, 180);
        }
        public float CameraExposure
        {
            set => ExposureComponent.fixedExposure.value = value.Map(0, 20);
        }
        public float LightHue
        {
            set {}
        }
        public float LightSaturation
        {
            set {}
        }
        public float LightBrightness
        {
            set {}
        }
        public float SkyColorHarmony { get; set; }
        public float SkyColorPhase { get; set; }
        public float SkyColorDiffusion { get; set; }
        public float SkyExposure { get; set; }
        public bool MarkerEnable
        {
            set => OriginMarker.SetActive(value);
        }

        //----------------------------------
        // Unity magic methods
        //----------------------------------
        void Start()
        {
            Exposure exposure;
            if (PostProcessing.profile.TryGet<Exposure>(out exposure))
                ExposureComponent = exposure;
            GradientSky gradient;
            if (Sky.profile.TryGet<GradientSky>(out gradient))
                GradientSkyComponent = gradient;
        }

        void Update()
        {
            // perform the orbital camera rotaiton
            CameraOrigin.transform.SetPositionAndRotation(CameraOrigin.transform.position, Quaternion.Euler(Orbit));

            // Set the Sky

            // Calculate the base harmonies
            var harmony = SkyColorHarmony.Clamp(0, 10).Map(0, 10, 0, 1);
            Color topColor, midColor, bottomColor;
            if (harmony <= (1f / 3f))
            {
                var lerp = harmony / (1f / 3f);
                topColor = Color.Lerp(Monochromatic[0], Analogous[0], lerp);
                midColor = Color.Lerp(Monochromatic[1], Analogous[1], lerp);
                bottomColor = Color.Lerp(Monochromatic[2], Analogous[2], lerp);
            }
            else if (harmony <= (2f / 3f))
            {
                var lerp = (harmony - (1f / 3f)) / (1f / 3f);
                topColor = Color.Lerp(Analogous[0], Triadic[0], lerp);
                midColor = Color.Lerp(Analogous[1], Triadic[1], lerp);
                bottomColor = Color.Lerp(Analogous[2], Triadic[2], lerp);
            }
            else
            {
                var lerp = (harmony - (2f / 3f)) / (1f / 3f);
                topColor = Color.Lerp(Triadic[0], Complementary[0], lerp);
                midColor = Color.Lerp(Triadic[1], Complementary[1], lerp);
                bottomColor = Color.Lerp(Triadic[2], Complementary[2], lerp);
            }

            // Apply offsets
            Color.RGBToHSV(topColor, out float topHue, out float topSat, out float topVal);
            Color.RGBToHSV(midColor, out float midHue, out float midSat, out float midVal);
            Color.RGBToHSV(bottomColor, out float bottomHue, out float bottomSat, out float bottomVal);

            var phase = SkyColorPhase.Map(0, 1);
            if (phase < 0) phase = Mathf.CeilToInt(Mathf.Abs(phase)) - phase;
            topHue = (topHue + phase) % 1;
            midHue = (midHue + phase) % 1;
            bottomHue = (bottomHue + phase) % 1;

            // Set parameters
            GradientSkyComponent.top.value = Color.HSVToRGB(topHue, topSat, topVal);
            GradientSkyComponent.middle.value = Color.HSVToRGB(midHue, midSat, midVal);
            GradientSkyComponent.bottom.value = Color.HSVToRGB(bottomHue, bottomSat, bottomVal);

            GradientSkyComponent.gradientDiffusion.value = SkyColorDiffusion.Clamp(0, 10).Map(0, 10, 0, 10, 3);
            GradientSkyComponent.exposure.value = SkyExposure.Clamp(0, 10).Map(0, 10, 0, 20);
        }
    }

}