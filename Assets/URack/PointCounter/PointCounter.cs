using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Eidetic.URack
{
    public class PointCounter : VFXModule
    {

        [Input]
        public override PointCloud PointCloud
        {
            set
            {
                // Map 10000 points per volt
                var countVolts = value.PointCount / 10000f;
                OscServer.Send(InstanceAddress + "/" + "PointCount", countVolts);
                PointCloudThru = value;
            }
        }

        public PointCloud PointCloudThru { get; set; }
    }
}