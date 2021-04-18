using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Eidetic.ColorExtensions
{
    public static class ColorExtensions
    {
        public static float h(this Color color)
        {
            Color.RGBToHSV(color, out float hue, out _, out _);
            return hue;
        }
        public static float s(this Color color)
        {
            Color.RGBToHSV(color, out float h, out float saturation, out float v);
            return saturation;
        }
        public static float v(this Color color)
        {
            Color.RGBToHSV(color, out float h, out float s, out float value);
            return value;
        }
        public static Color AsHSVColor(this Vector3 hsv) => Color.HSVToRGB(hsv.x, hsv.y, hsv.z);
        public static Color AsRGBColor(this Vector3 rgb) => new Color(rgb.x, rgb.y, rgb.z);
    }
    public class ColorLine : List<Color>
    {
        public Color Evaluate(float position)
        {
            if (Count == 0) return Color.white;
            if (Count == 1) return this[0];
            
            int startKey = Mathf.FloorToInt(position * Count);
            if (startKey >= Count - 1) return this[Count - 1];
            int nextKey = startKey + 1;
            
            float distanceToNextKey = (position * Count) - startKey;
            return Color.Lerp(this[startKey], this[nextKey], distanceToNextKey);
        }
        
        public Texture2D ToTexture(int width = 256)
        {
            var texture = new Texture2D(width, 1, TextureFormat.ARGB32, false);
            texture.filterMode = FilterMode.Bilinear;
            float inv = 1f / (width - 1);
            for (int x = 0; x < width; x++)
            {
                var t = x * inv;
                Color color = Evaluate(t);
                texture.SetPixel(x, 0, color);
            }
            texture.Apply();
            return texture;
        }
        
        /// <summary>
        /// Sample line to an 8-color Unity Gradient
        /// </summary>
        /// <returns></returns>
        public Gradient ToGradient() => new Gradient()
        {
            // Unity Gradient has a Max of 8 keys
            colorKeys = new GradientColorKey[]
            {
                new GradientColorKey(Evaluate(0 / 7f), 0 / 7f),
                new GradientColorKey(Evaluate(1 / 7f), 1 / 7f),
                new GradientColorKey(Evaluate(2 / 7f), 2 / 7f),
                new GradientColorKey(Evaluate(3 / 7f), 3 / 7f),
                new GradientColorKey(Evaluate(4 / 7f), 4 / 7f),
                new GradientColorKey(Evaluate(5 / 7f), 5 / 7f),
                new GradientColorKey(Evaluate(6 / 7f), 6 / 7f),
                new GradientColorKey(Evaluate(7 / 7f), 7 / 7f)
            }
        };
    }
}
