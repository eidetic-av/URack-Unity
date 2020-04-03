using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

namespace Eidetic.URack
{
    public class Mirage2x : VFXModule
    {
        public string Folder = "Textures";

        [Input]
        public float TextureSelectA
        {
            set
            {
                var imageIndex = Mathf.RoundToInt(value.Map(-1, 1, 0, Images.Length - 1));
                var image = Images[imageIndex];
                VisualEffect.SetTexture("TextureA", image);
                VisualEffect.SetUInt("WidthA", (uint)image.width);
                VisualEffect.SetUInt("HeightA", (uint)image.height);
            }
        }

        [Input]
        public float TextureSelectB
        {
            set
            {
                var imageIndex = Mathf.RoundToInt(value.Map(-1, 1, 0, Images.Length - 1));
                var image = Images[imageIndex];
                VisualEffect.SetTexture("TextureB", image);
                VisualEffect.SetUInt("WidthB", (uint)image.width);
                VisualEffect.SetUInt("HeightB", (uint)image.height);
            }
        }

        [Input]
        public float SimulationSpeed
        {
            set => VisualEffect.playRate = value;
        }

        Texture2D[] Images;
        new public void Start()
        {
            Images = Resources.LoadAll<Texture2D>(Folder);
            TextureSelectA = 0;
            TextureSelectB = 0;
            base.Start();
        }
    }
}