using System.Collections.Generic;
using UnityEngine;

namespace Eidetic.URack
{
    public class Arena : MonoBehaviour
    {
        public static Dictionary<int, MonoBehaviour> Instance = new Dictionary<int, MonoBehaviour>();

        public int InstanceNumber { get; private set; }

        Camera Camera;

        public static Arena Create(int number) => new Arena(number);

        private Arena(int number)
        {
            Instance[InstanceNumber = number] = this;
        }
        private Arena()
        {
            Instance[InstanceNumber = 23] = this;
        }

        void Start()
        {
            Camera = GetComponentInChildren<Camera>();
        }

        public float CameraPositionX
        {
            set => Camera.transform.position = Camera.transform.position.Replace(0, value);
        }
        public float CameraPositionY
        {
            set => Camera.transform.position = Camera.transform.position.Replace(1, value);
        }
        public float CameraPositionZ
        {
            set => Camera.transform.position = Camera.transform.position.Replace(2, value);
        }
    }

    public static class URackExtensions
    {

        public static Vector3 Replace(this Vector3 vector, int index, float value)
        {
            var newVector = vector;
            newVector[index] = value;
            return newVector;
        }

    }

}