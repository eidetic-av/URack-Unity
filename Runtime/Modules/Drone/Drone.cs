
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Eidetic.URack
{
    public class Drone : UModule
    {
		
	[Input(-5, 5, -10, 10)]
	public float X
	{
		set => CameraOrigin.transform.position = CameraOrigin.transform.position.Replace(0, value);
	}

	[Input(-5, 5, -10, 10)]
	public float Y
	{
		set => CameraOrigin.transform.position = CameraOrigin.transform.position.Replace(1, value);
	}

	[Input(-5, 5, -10, 10)]
	public float Z
	{
		set => CameraOrigin.transform.position = CameraOrigin.transform.position.Replace(2, value);
	}

        Vector3 orbit = Vector3.zero;
        [Input(-5, 5, -180, 180, false, 1, 1)]
        public float Orbit
        {
            set => orbit = orbit.Replace(1, value);
        }

        [Input(-5, 5, -90, 90, false, 1, 1)]
        public float Elevation
        {
            set => orbit = orbit.Replace(0, value);
        }

        float distance;
        [Input]
        public float Distance
        {
	    get => distance;
            set
            {
                distance = value > 0.1f ? value : 0.1f;
            }
        }

	public int Target
	{
		set => OriginTarget.SetActive(value > 0);
	}

        [SerializeField] Camera Camera;
        [SerializeField] GameObject CameraOrigin;
        [SerializeField] GameObject ForwardAxis;
        [SerializeField] GameObject BackwardAxis;
        [SerializeField] GameObject OriginTarget;
	[SerializeField] Volume PostProcessing;

        public void Update()
        {
            // perform the orbital camera rotaiton
            CameraOrigin.transform.SetPositionAndRotation(CameraOrigin.transform.position, Quaternion.Euler(orbit));
	    // distance movement                
	    if (Vector3.Distance(Camera.transform.position, CameraOrigin.transform.position) > distance + 0.01f){
	    while (Vector3.Distance(Camera.transform.position, CameraOrigin.transform.position) > distance + 0.005f)
                Camera.transform.position = Vector3.MoveTowards(Camera.transform.position, ForwardAxis.transform.position, 0.0005f);
} else if (Vector3.Distance(Camera.transform.position, CameraOrigin.transform.position) < distance - 0.01f) {
            while (Vector3.Distance(Camera.transform.position, CameraOrigin.transform.position) < distance - 0.005f)
                Camera.transform.position = Vector3.MoveTowards(Camera.transform.position, BackwardAxis.transform.position, 0.0005f);
}
	}
    }

}
