using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Eidetic.URack
{
    public static class URackExtensions
    {
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
        /// Return a copy of the vector with the value at the index replaced.
        ///</summary>
        public static Vector2 Replace(this Vector2 vector, int index, float value)
        {
            var newVector = vector;
            newVector[index] = value;
            return newVector;
        }

        /// <summary>
        /// Return a Vector3 rotated by another Vector3.
        /// </summary>
        public static Vector3 RotateBy(this Vector3 input, Vector3 rotation) =>
            Quaternion.Euler(rotation) * input;

        /// <summary>
        /// Return a a Vector3 translated by another Vector3.
        /// </summary>
        public static Vector3 TranslateBy(this Vector3 input, Vector3 translation) =>
            input + translation;

        /// <summary>
        /// Return a Vector3 scaled by another Vector3.
        /// </summary>
        public static Vector3 ScaleBy(this Vector3 input, Vector3 scale) =>
            new Vector3(input.x * scale.x, input.y * scale.y, input.z * scale.z);

        /// <summary>
        /// Destroy a GameObject.
        /// </summary>
        public static void Destroy(this GameObject gameObject) => GameObject.Destroy(gameObject);

        /// <summary>
        /// Return all types in the domain that derive from a specific base.
        /// </summary>
        public static List<Type> GetAllDerivedTypes(this AppDomain appDomain, Type type) =>
            appDomain.GetAssemblies().SelectMany(a => a.GetTypes()).Where(t => t.IsSubclassOf(type)).ToList();
    }
}