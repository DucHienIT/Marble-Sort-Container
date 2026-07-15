using System.Collections.Generic;
using UnityEngine;

namespace MarbleSort.Utils
{
    /// <summary>
    /// A closed "stadium" loop (two horizontal straights + two semicircular caps) parameterised by
    /// arc-length. Balls recirculate by advancing arc-length modulo <see cref="Length"/>.
    /// Local space is centred on the loop; add the belt's world position to place it.
    ///
    /// Travel direction (increasing s): along the BOTTOM straight left->right, up the right cap,
    /// along the TOP straight right->left, down the left cap. Delivery gates live on the bottom
    /// straight so destination boxes sit directly beneath the belt.
    /// </summary>
    public class ConveyorPath
    {
        public readonly float StraightLength; // L
        public readonly float CapRadius;      // R
        public readonly float Length;         // total perimeter

        private readonly float _sBottomEnd;   // s at end of bottom straight
        private readonly float _sRightEnd;    // s at end of right cap
        private readonly float _sTopEnd;      // s at end of top straight

        public ConveyorPath(float straightLength, float capRadius)
        {
            StraightLength = Mathf.Max(0.1f, straightLength);
            CapRadius = Mathf.Max(0.1f, capRadius);
            _sBottomEnd = StraightLength;
            _sRightEnd = _sBottomEnd + Mathf.PI * CapRadius;
            _sTopEnd = _sRightEnd + StraightLength;
            Length = _sTopEnd + Mathf.PI * CapRadius;
        }

        /// <summary>Arc-length of the top-straight midpoint (where the ramp feeds marbles in).</summary>
        public float TopCenterS { get { return _sRightEnd + StraightLength * 0.5f; } }

        /// <summary>Shortest signed loop distance from a to b (in (-Length/2, Length/2]).</summary>
        public float SignedDelta(float from, float to)
        {
            float d = Wrap(to) - Wrap(from);
            if (d > Length * 0.5f) d -= Length;
            if (d < -Length * 0.5f) d += Length;
            return d;
        }

        public float Wrap(float s)
        {
            s %= Length;
            if (s < 0f) s += Length;
            return s;
        }

        public Vector2 Position(float s)
        {
            s = Wrap(s);
            float halfL = StraightLength * 0.5f;
            float R = CapRadius;
            if (s <= _sBottomEnd)
            {
                float x = -halfL + s;
                return new Vector2(x, -R);
            }
            if (s <= _sRightEnd)
            {
                float a = (s - _sBottomEnd) / R;            // 0..pi
                float ang = -Mathf.PI * 0.5f + a;            // -90deg -> +90deg
                return new Vector2(halfL + R * Mathf.Cos(ang), R * Mathf.Sin(ang));
            }
            if (s <= _sTopEnd)
            {
                float x = halfL - (s - _sRightEnd);
                return new Vector2(x, R);
            }
            {
                float a = (s - _sTopEnd) / R;                // 0..pi
                float ang = Mathf.PI * 0.5f + a;             // +90deg -> +270deg
                return new Vector2(-halfL + R * Mathf.Cos(ang), R * Mathf.Sin(ang));
            }
        }

        /// <summary>Evenly spaced gate arc-lengths along the bottom straight (where boxes catch balls).</summary>
        public float[] BottomGates(int count)
        {
            var g = new float[Mathf.Max(1, count)];
            // inset a little from the caps so gates stay on the flat run
            float inset = StraightLength * 0.12f;
            float usable = StraightLength - inset * 2f;
            if (g.Length == 1) { g[0] = inset + usable * 0.5f; return g; }
            for (int i = 0; i < g.Length; i++)
                g[i] = inset + usable * (i / (float)(g.Length - 1));
            return g;
        }

        /// <summary>Sampled outline points for rendering the belt as a line loop.</summary>
        public List<Vector2> Outline(int samples = 96)
        {
            var pts = new List<Vector2>(samples);
            for (int i = 0; i < samples; i++)
                pts.Add(Position(Length * i / samples));
            return pts;
        }
    }
}
