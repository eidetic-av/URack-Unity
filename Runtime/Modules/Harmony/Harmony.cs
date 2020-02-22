using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Eidetic.URack
{
    public class Harmony : UModule
    {
        static readonly Color[] Monochromatic = new Color[] { Color.HSVToRGB(0, .7f, 1), Color.HSVToRGB(0, 1, 1), Color.HSVToRGB(0, .7f, .5f) };
        static readonly Color[] Analogous = new Color[] { Color.HSVToRGB(0.05f, .95f, 1), Color.HSVToRGB(0, 1, 1), Color.HSVToRGB(0.83333f, .95f, 1f) };
        static readonly Color[] Triadic = new Color[] { Color.HSVToRGB(0.16388f, .9f, 1), Color.HSVToRGB(0, 1, 1), Color.HSVToRGB(0.56388f, .95f, 1f) };
        static readonly Color[] Complementary = new Color[] { Color.HSVToRGB(0.33888f, 1f, 1), Color.HSVToRGB(0, 1, 1), Color.HSVToRGB(0.42222f, .95f, 1f) };
 
		[Input]
        public float ColorHarmony { get; set; }
        [Input]
        public float Phase { get; set; }
        [Input]
        public float Diffusion { get; set; }
        [Input]
        public float Glow { get; set; }

		[SerializeField] Volume Sky;
        GradientSky GradientSkyComponent;

		public void Start()
        {
            GradientSky gradient;
            if (Sky.profile.TryGet<GradientSky>(out gradient))
                GradientSkyComponent = gradient;
        }

        public void Update()
        {
            // Calculate the base harmonies
            var harmony = ColorHarmony.Clamp(0, 10).Map(0, 10, 0, 1);
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

            var phase = Phase.Map(0, 1);
            if (phase < 0) phase = Mathf.CeilToInt(Mathf.Abs(phase)) - phase;
            topHue = (topHue + phase) % 1;
            midHue = (midHue + phase) % 1;
            bottomHue = (bottomHue + phase) % 1;

            // Set parameters
            GradientSkyComponent.top.value = Color.HSVToRGB(topHue, topSat, topVal);
            GradientSkyComponent.middle.value = Color.HSVToRGB(midHue, midSat, midVal);
            GradientSkyComponent.bottom.value = Color.HSVToRGB(bottomHue, bottomSat, bottomVal);

            GradientSkyComponent.gradientDiffusion.value = Diffusion.Clamp(0, 10).Map(0, 10, 0, 10, 3);
            GradientSkyComponent.exposure.value = Glow.Clamp(0, 10).Map(0, 10, 0, 20);
        }
    }

}
