using UnityEngine;

namespace Eidetic.URack {
    public static class URackExtensions
    {

        ///<summary>
        /// Return a copy of the vector with the value at the index replaced.
        ///</summary>
        public static Vector3 Replace(this Vector3 vector, int index, float value)
        {
            var newVector = vector;
            newVector[index] = value;
            return newVector;
        }

        ///<summary>
        /// Map a bi-directional voltage (+-5) to a new range.
        ///</summary>
        public static float Map(this float value, float minOut, float maxOut)
        {
            return Map(value, -5, 5, minOut, maxOut);
        }

        ///<summary>
        /// Map a float to a new range.
        ///</summary>
        public static float Map(this float value, float minIn, float maxIn, float minOut, float maxOut)
        {
            return ((value - minIn) / (maxIn - minIn)) * (maxOut - minOut) + minOut;
        }

        ///<summary>
        /// Map a float to a new range exponentially.
        ///</summary>
        public static float Map(this float value, float minIn, float maxIn, float minOut, float maxOut, float exponent)
        {
            var raised = Mathf.Pow(value.Map(minIn, maxIn, minOut, maxOut), exponent);
            return raised.Map(minOut, Mathf.Pow(maxOut, exponent), minOut, maxOut);
        }

        ///<summary>
        /// Clamp a voltage within -5 and +5.
        ///</summary>
        public static float Clamp(this float value) => value.Clamp(-5, 5);

        ///<summary>
        /// Clamp a float within a range.
        ///</summary>
        public static float Clamp(this float value, float min, float max) => Mathf.Clamp(value, min, max);

    }
}