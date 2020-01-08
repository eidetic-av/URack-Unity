using UnityEngine;

namespace Eidetic.URack {
    public static class URackExtensions
    {

        public static Vector3 Replace(this Vector3 vector, int index, float value)
        {
            var newVector = vector;
            newVector[index] = value;
            return newVector;
        }

        public static float Map(this float value, float minOut, float maxOut)
        {
            return Map(value, -5, 5, minOut, maxOut);
        }

        public static float Map(this float value, float minIn, float maxIn, float minOut, float maxOut)
        {
            return ((value - minIn) / (maxIn - minIn)) * (maxOut - minOut) + minOut;
        }

        public static float Clamp(this float value, float min, float max) => Mathf.Clamp(value, min, max);

    }
}