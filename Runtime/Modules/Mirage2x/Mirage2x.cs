using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.VFX;

namespace Eidetic.URack
{
    public class Mirage2x : VFXModule
    {
        const string Folder = "/Mirage Images";

        static List<Texture2D> images;
        static List<Texture2D> Images {
            get {
                if (images != null) return images;
                images = new List<Texture2D>();

                var userFolder = Application.persistentDataPath + Folder;
                var pngFiles = System.IO.Directory.GetFiles(userFolder, "*.png");
                var jpgFiles = System.IO.Directory.GetFiles(userFolder, "*.jpg");
        
                foreach (var file in pngFiles.Concat(jpgFiles)) {
                    var png = System.IO.File.ReadAllBytes(file);
                    var texture = new Texture2D(1, 1);
                    if (texture.LoadImage(png))
                        images.Add(texture);
                }

                return images;
            }
        }

        [Input]
        public float TextureSelectA
        {
            set
            {
                if (Images.Count == 0) return;
                var sequenceLength = Images.Count - 1;
                var selection = Mathf.RoundToInt(value.Map(0, 10, 0, sequenceLength));
                selection = Mathf.Clamp(selection, 0, sequenceLength);
                var image = Images[selection];
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
                if (Images.Count == 0) return;
                var sequenceLength = Images.Count - 1;
                var selection = Mathf.RoundToInt(value.Map(0, 10, 0, sequenceLength));
                selection = Mathf.Clamp(selection, 0, sequenceLength);
                var image = Images[selection];
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

        [Input] public float PositionX {
            set => transform.position = transform.position.Replace(0, value);
        }
        [Input] public float PositionY {
            set => transform.position = transform.position.Replace(1, value);
        }
        [Input] public float PositionZ {
            set => transform.position = transform.position.Replace(2, value);
        }
        [Input] public float RotationX {
            set {
                var euler = transform.rotation.eulerAngles;
                transform.rotation = Quaternion.Euler(value.Map(-5, 5, -180, 180), euler.y, euler.z);
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
    }
}