using UnityEngine;
using MarbleSort.Core;
using MarbleSort.Config;
using MarbleSort.Utils;

namespace MarbleSort.Gameplay
{
    public enum BallVisualState { Packed, Flying, OnConveyor, Delivering, Free }

    /// <summary>
    /// One marble — a real 3D sphere with a glossy candy material. Its motion is fully scripted
    /// (deterministic): it arcs from a tile down to the belt (Flying), rides the belt (OnConveyor),
    /// then flies into a bin (Delivering). Authoritative counts live in <see cref="GameState"/>.
    ///
    /// Lives on Ball.prefab (MeshFilter + MeshRenderer + BallView); mesh/material assigned in Init.
    /// </summary>
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class BallView : MonoBehaviour
    {
        public BallColor Color { get; private set; }
        public BallVisualState State { get; private set; }
        public float ArcS;                 // arc-length position on the conveyor loop

        private MeshFilter _mf;
        private MeshRenderer _mr;

        private void EnsureRefs()
        {
            if (_mf == null) _mf = GetComponent<MeshFilter>();
            if (_mr == null) _mr = GetComponent<MeshRenderer>();
        }

        public void Init(BallColor color)
        {
            EnsureRefs();
            Color = color;
            _mf.sharedMesh = MeshFactory.Sphere();
            // marbles read pastel (lighter than the tiles) like the reference
            _mr.sharedMaterial = Mat3D.Marble(UnityEngine.Color.Lerp(Palette.Ball(color), UnityEngine.Color.white, 0.22f));
            _mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            float d = Tune.BallRadius * 2f;
            transform.localScale = new Vector3(d, d, d);
            State = BallVisualState.Packed;
        }

        // Overload kept for pool call-sites that pass a sorting order (ignored in 3D).
        public void Init(BallColor color, int sortingOrder) { Init(color); }

        public void SetFlying(Transform parent)
        {
            EnsureRefs();
            if (parent != null) transform.SetParent(parent, true);
            State = BallVisualState.Flying;
        }

        public void AttachToConveyor(float arcS, Vector3 worldPos)
        {
            State = BallVisualState.OnConveyor;
            ArcS = arcS;
            transform.position = worldPos;
        }

        public void SetConveyorPos(Vector3 worldPos) { transform.position = worldPos; }

        public void SetDelivering() { State = BallVisualState.Delivering; }

        public bool IsFlying { get { return State == BallVisualState.Flying; } }
        public bool IsOnConveyor { get { return State == BallVisualState.OnConveyor; } }
    }
}
