using System;
using UnityEngine;

namespace Eidetic.URack
{
    public abstract partial class UModule : MonoBehaviour
    {
        [AttributeUsage(AttributeTargets.Property)]
        public class InputAttribute : Attribute
        {
            const float DefaultMinInput = 0;
            const float DefaultMaxInput = 10;
            const float DefaultMinOutput = 0;
            const float DefaultMaxOutput = 10;
            const float DefaultExponent = 1;
            const bool DefaultClamp = false;
            const float DefaultSmoothing = 3;

            public float MinInput { get; internal set; }
            public float MaxInput { get; internal set; }
            public float MinOutput { get; internal set; }
            public float MaxOutput { get; internal set; }
            public bool Clamp {get; internal set;}
            public float Exponent { get; internal set; }
            public float Smoothing { get; internal set; }

            public InputAttribute(float smoothing = DefaultSmoothing)
            {
                MinInput = DefaultMinInput;
                MaxInput = DefaultMaxInput;
                MinOutput = DefaultMinOutput;
                MaxOutput = DefaultMaxOutput;
                Clamp = DefaultClamp;
                Exponent = DefaultExponent;
                Smoothing = smoothing;
            }
            public InputAttribute(float minInput, float maxInput, float minOutput, float maxOutput, bool clamp = DefaultClamp, float exponent = DefaultExponent, float smoothing = DefaultSmoothing)
            {
                MinInput = minInput;
                MaxInput = maxInput;
                MinOutput = minOutput;
                MaxOutput = maxOutput;
                Clamp = clamp;
                Exponent = exponent;
                Smoothing = smoothing;
            }
        }
    }
}