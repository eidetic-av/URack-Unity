using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Eidetic.URack
{
    public class PointCounter : VFXModule
    {

        [Input]
        public override PointCloud PointCloudInput
        {
            set
            {
                // Map 10000 points per volt
                var countVolts = value.PointCount / 10000f;
                Osc.Server.Send(InstanceAddress + "/" + "PointCount", countVolts);
                PointCloudOutput = value;
            }
        }

        public PointCloud PointCloudOutput { get; set; }
    }
}