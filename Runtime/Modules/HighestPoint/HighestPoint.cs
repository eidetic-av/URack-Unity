using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Eidetic.URack
{
    public class HighestPoint : VFXModule
    {

        [Input]
        public override PointCloud PointCloudInput
        {
            set
            {
                // Map 1 metre per volt
                var height = -99f;
                foreach (var point in value.Points) {
                    if (point.Position.z > height) height = point.Position.z;
                }
                Osc.Server.Send(InstanceAddress + "/" + "Height", height);
                PointCloudOutput = value;
            }
        }

        public PointCloud PointCloudOutput { get; set; }
    }
}