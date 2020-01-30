using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Eidetic.URack
{
    public class PointCount : VFXModule
    {
        float lastTestSend;
        public float TestSend;

        public void Update()
        {
            if (TestSend != lastTestSend)
            {
                OscServer.Send<float>("/test/one", TestSend);
                lastTestSend = TestSend;
            }
        }
    }

}