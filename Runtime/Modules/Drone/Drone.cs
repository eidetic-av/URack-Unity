
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
		public float Pace { get; set; }

		[Input(-5, 5, -10, 10)]
		public float Strafe { get; set; }

        float distance;
        [Input]
        public float Distance
        {
            set
            {
                distance = value.Clamp(0.01f, 10);
                while (Vector3.Distance(Camera.transform.position, CameraOrigin.transform.position) > distance + 0.01f)
                    Camera.transform.position = Vector3.MoveTowards(Camera.transform.position, ForwardAxis.transform.position, 0.0005f);
                while (Vector3.Distance(Camera.transform.position, CameraOrigin.transform.position) < distance - 0.01f)
                    Camera.transform.position = Vector3.MoveTowards(Camera.transform.position, BackwardAxis.transform.position, 0.0005f);
            }
        }
        [Input(-5, 5, -180, 180, false, 1, 1)]
        public float OrbitX
        {
            set => Orbit = Orbit.Replace(1, value);
        }
        [Input(-5, 5, -180, 180, false, 1, 1)]
        public float OrbitY
        {
            set => Orbit = Orbit.Replace(0, value);
        }
        [Input]
        public float Zoom
        {
            set => Camera.focalLength = value.Clamp(0, 10).Map(0, 10, 8, 180);
        }

        [SerializeField] Camera Camera;
        [SerializeField] GameObject CameraOrigin;
        [SerializeField] GameObject ForwardAxis;
        [SerializeField] GameObject BackwardAxis;
        [SerializeField] GameObject LeftAxis;
        [SerializeField] GameObject RightAxis;
        [SerializeField] GameObject UpAxis;
        [SerializeField] GameObject DownAxis;
        [SerializeField] Volume PostProcessing;

        Vector3 Orbit = Vector3.zero;

        void Start() { }

        public void Update()
        {
			var position = CameraOrigin.transform.position;
			// apply movement
			if (Pace != 0)
				position += CameraOrigin.transform.TransformDirection(Vector3.forward) * Pace * Time.deltaTime;
			if (Strafe != 0)
				position += CameraOrigin.transform.TransformDirection(Vector3.right) * Strafe * Time.deltaTime;

            // perform the orbital camera rotaiton
            CameraOrigin.transform.SetPositionAndRotation(position, Quaternion.Euler(Orbit));
		}
    }

}
