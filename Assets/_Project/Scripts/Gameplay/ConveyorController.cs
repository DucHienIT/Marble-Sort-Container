using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MarbleSort.Core;
using MarbleSort.Config;
using MarbleSort.Utils;

namespace MarbleSort.Gameplay
{
    /// <summary>Services the conveyor needs from the game host.</summary>
    public interface IConveyorHost
    {
        void ReturnBall(BallView ball);
        void PlayIntake();
        void PlayDeliver();
    }

    /// <summary>
    /// The conveyor as in the reference mock-up: a navy rounded capsule housing containing a
    /// *stadium loop* — marbles ride the top lane right→left, wrap the left end, come back along
    /// the bottom lane and wrap the right end — with a metallic centre rail between the lanes and
    /// a gold star token at the right end. Marbles enter at the top-lane centre (under the spout);
    /// destination bins beneath absorb matching colours as they pass the bottom lane. Gameplay
    /// authority is <see cref="GameState"/>; this is presentation + intake/deliver hand-offs.
    /// On Conveyor.prefab; references DestinationBox.prefab.
    /// </summary>
    public class ConveyorController : MonoBehaviour
    {
        [Header("Loop geometry (world units; loop centre = this transform)")]
        public float beltHalfLength = 2.9f;   // straight-lane half length
        public float laneOffset = 0.33f;      // lanes at ±laneOffset; also the end-cap turn radius
        public float housingPad = 0.42f;      // housing outgrow beyond the marble path
        public float beltDepth = 0.7f;
        public float binDrop = 2.05f;
        public float binPitch = 1.94f;        // horizontal distance between bin centres

        [Header("Child prefab")]
        [SerializeField] private DestinationBoxView boxPrefab;

        private GameState _state;
        private IConveyorHost _host;
        private Vector3 _center;
        private float _length;                // total loop arc length
        private float _straight;              // one straight lane length
        private float _arc;                   // one semicircle length
        private float _intakeS;
        private float[] _gates;

        private readonly List<BallView> _onBelt = new List<BallView>();
        private DestinationBoxView[] _boxes;

        private const float BallZ = -0.05f;

        public Vector2 IntakeWorldPos { get { return new Vector2(_center.x, _center.y + laneOffset); } }

        // ---- geometry helpers -------------------------------------------
        private float Wrap(float s) { s %= _length; if (s < 0f) s += _length; return s; }

        /// <summary>Map loop arc length to world: s=0 at the top-lane right end, increasing in the
        /// direction of travel (top lane right→left, left turn, bottom lane left→right, right turn).</summary>
        private Vector3 PosOf(float s)
        {
            s = Wrap(s);
            float r = laneOffset;
            if (s < _straight)
                return new Vector3(_center.x + beltHalfLength - s, _center.y + r, BallZ);
            s -= _straight;
            if (s < _arc)
            {
                float th = Mathf.PI * 0.5f + s / r;
                return new Vector3(_center.x - beltHalfLength + Mathf.Cos(th) * r, _center.y + Mathf.Sin(th) * r, BallZ);
            }
            s -= _arc;
            if (s < _straight)
                return new Vector3(_center.x - beltHalfLength + s, _center.y - r, BallZ);
            s -= _straight;
            float th2 = -Mathf.PI * 0.5f + s / r;
            return new Vector3(_center.x + beltHalfLength + Mathf.Cos(th2) * r, _center.y + Mathf.Sin(th2) * r, BallZ);
        }

        private float SignedDelta(float from, float to)
        {
            float d = Wrap(to) - Wrap(from);
            if (d > _length * 0.5f) d -= _length;
            if (d < -_length * 0.5f) d += _length;
            return d;
        }

        public void Init(GameState state, IConveyorHost host)
        {
            _state = state;
            _host = host;
            _center = transform.position;
            _straight = beltHalfLength * 2f;
            _arc = Mathf.PI * laneOffset;
            _length = _straight * 2f + _arc * 2f;
            _intakeS = beltHalfLength; // top-lane centre (x = 0)

            RenderBelt();
            BuildGatesAndBoxes();

            _state.BoxActivated += OnBoxActivated;
            _state.BoxCompleted += OnBoxCompleted;
        }

        private void OnDestroy() { Unsubscribe(); }
        private void Unsubscribe()
        {
            if (_state != null)
            {
                _state.BoxActivated -= OnBoxActivated;
                _state.BoxCompleted -= OnBoxCompleted;
            }
        }

        public void Teardown()
        {
            Unsubscribe();
            StopAllCoroutines();
            for (int i = transform.childCount - 1; i >= 0; i--) Destroy(transform.GetChild(i).gameObject);
            _onBelt.Clear();
            _boxes = null;
            _state = null;
        }

        // ---- build -------------------------------------------------------

        private void RenderBelt()
        {
            float ballR = Tune.BallRadius;
            float w = 2f * (beltHalfLength + laneOffset + ballR) + housingPad * 2f;
            float h = 2f * (laneOffset + ballR) + housingPad * 0.8f;

            // lighter outline → navy capsule housing → slightly darker inner well
            AddBox("HousingRim", new Vector3(_center.x, _center.y, 0.52f), new Vector3(w + 0.16f, h + 0.16f, beltDepth * 0.9f),
                (h + 0.16f) * 0.42f, Mat3D.Plastic(Color.Lerp(Palette.ConveyorFrame, Color.white, 0.22f)));
            AddBox("Housing", new Vector3(_center.x, _center.y, 0.42f), new Vector3(w, h, beltDepth), h * 0.42f, Mat3D.Plastic(Palette.ConveyorFrame));
            AddBox("Well", new Vector3(_center.x, _center.y, 0.30f), new Vector3(w - 0.26f, h - 0.26f, beltDepth * 0.7f), (h - 0.26f) * 0.42f, Mat3D.Matte(Palette.ConveyorBelt));

            // light centre rail between the two lanes, with rounded end knobs
            float railLen = beltHalfLength * 2f + laneOffset;
            var railMat = Mat3D.Get(Palette.BeltRail, 0.8f, 0.25f, 0.18f);
            AddBox("Rail", new Vector3(_center.x, _center.y, -0.02f), new Vector3(railLen, 0.13f, 0.16f), 0.06f, railMat);
            for (int sgn = -1; sgn <= 1; sgn += 2)
            {
                var knob = new GameObject(sgn < 0 ? "RailKnobL" : "RailKnobR");
                knob.transform.SetParent(transform, false);
                knob.transform.position = new Vector3(_center.x + sgn * railLen * 0.5f, _center.y, -0.03f);
                knob.transform.localScale = Vector3.one * 0.2f;
                var kmf = knob.AddComponent<MeshFilter>();
                var kmr = knob.AddComponent<MeshRenderer>();
                kmf.sharedMesh = MeshFactory.Sphere();
                kmr.sharedMaterial = railMat;
            }

            // gold star token at the right end (+ two white sparkles)
            var star = new GameObject("Star");
            star.transform.SetParent(transform, false);
            star.transform.position = new Vector3(_center.x + beltHalfLength + laneOffset * 0.6f, _center.y, -0.16f);
            var smf = star.AddComponent<MeshFilter>();
            var smr = star.AddComponent<MeshRenderer>();
            smf.sharedMesh = MeshFactory.StarPrism(5, 0.36f, 0.17f, 0.14f);
            smr.sharedMaterial = Mat3D.Glow(Palette.Ball(BallColor.Yellow));

            for (int sIdx = 0; sIdx < 2; sIdx++)
            {
                var spark = new GameObject("Sparkle" + sIdx);
                spark.transform.SetParent(transform, false);
                spark.transform.position = star.transform.position +
                    (sIdx == 0 ? new Vector3(0.28f, 0.22f, -0.06f) : new Vector3(0.34f, -0.12f, -0.06f));
                var pmf = spark.AddComponent<MeshFilter>();
                var pmr = spark.AddComponent<MeshRenderer>();
                pmf.sharedMesh = MeshFactory.StarPrism(4, sIdx == 0 ? 0.14f : 0.09f, sIdx == 0 ? 0.05f : 0.032f, 0.08f);
                pmr.sharedMaterial = Mat3D.Glow(Color.white);
            }
        }

        private MeshRenderer AddBox(string name, Vector3 pos, Vector3 size, float radius, Material mat)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.position = pos;
            var mf = go.AddComponent<MeshFilter>();
            var mr = go.AddComponent<MeshRenderer>();
            mf.sharedMesh = MeshFactory.RoundedBox(size, radius, 6);
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            return mr;
        }

        private void BuildGatesAndBoxes()
        {
            int n = _state.ActiveSlotCount;
            _gates = new float[n];
            _boxes = new DestinationBoxView[n];
            for (int k = 0; k < n; k++)
            {
                float binX = (k - (n - 1) * 0.5f) * binPitch;
                // gate on the bottom lane above this bin (bottom lane runs left→right)
                _gates[k] = Wrap(_straight + _arc + (binX + beltHalfLength));
                Vector3 boxPos = new Vector3(_center.x + binX, _center.y - binDrop, 0f);
                DestinationBoxView box;
                if (boxPrefab != null) box = Instantiate(boxPrefab, transform);
                else { var go = new GameObject("Bin" + k); go.transform.SetParent(transform, false); box = go.AddComponent<DestinationBoxView>(); }
                box.Init(k, boxPos);
                box.SetBox(_state.ActiveBox(k));
                _boxes[k] = box;
            }
        }

        // ---- intake & delivery ------------------------------------------

        /// <summary>Hand a marble that has arrived at the belt onto the loop. Returns false on overflow
        /// (belt full → the level is lost inside GameState).</summary>
        public bool TryIntake(BallView ball)
        {
            if (ball == null) return false;
            if (!_state.TryIntake(ball.Color)) return false; // overflow -> LostOverflow event
            // drop in at the spout, but never closer than one spacing behind the train's tail
            float s = _intakeS;
            if (_onBelt.Count > 0)
            {
                var tail = _onBelt[_onBelt.Count - 1];
                float toTail = SignedDelta(s, tail.ArcS);
                if (toTail > -1.5f && toTail < Tune.BallSpacing) s = Wrap(tail.ArcS - Tune.BallSpacing);
            }
            ball.AttachToConveyor(s, PosOf(s));
            _onBelt.Add(ball);
            _host.PlayIntake();
            return true;
        }

        private void Update()
        {
            if (_state == null) return;
            float advance = Tune.ConveyorSpeed * Time.deltaTime;

            // train packing: the front marble runs free; each follower may not close within one
            // spacing of the marble ahead (the front also can't rear-end the tail after a lap)
            for (int i = 0; i < _onBelt.Count; i++)
            {
                var b = _onBelt[i];
                if (b == null) continue;
                float free = Wrap(b.ArcS + advance);
                BallView ahead = i > 0 ? _onBelt[i - 1] : (_onBelt.Count > 1 ? _onBelt[_onBelt.Count - 1] : null);
                if (ahead != null && ahead != b)
                {
                    float maxS = Wrap(ahead.ArcS - Tune.BallSpacing);
                    float over = SignedDelta(free, maxS);
                    if (over < 0f && over > -1.5f) free = maxS;   // just crossed the limit -> clamp
                }
                b.ArcS = free;
                b.SetConveyorPos(PosOf(b.ArcS));
            }

            if (_state.Status != LevelStatus.Playing) return;

            float window = Tune.BallRadius + 0.16f;
            for (int k = 0; k < _boxes.Length; k++)
            {
                var box = _state.ActiveBox(k);
                if (box == null || box.IsFull) continue;
                for (int i = 0; i < _onBelt.Count; i++)
                {
                    var b = _onBelt[i];
                    if (b == null || b.Color != box.Color) continue;
                    if (Mathf.Abs(SignedDelta(b.ArcS, _gates[k])) > window) continue;
                    if (_state.TryDeliverToSlot(k, b.Color))
                    {
                        _onBelt.RemoveAt(i);
                        b.SetDelivering();
                        StartCoroutine(FlyToBox(b, _boxes[k]));
                        _host.PlayDeliver();
                    }
                    break;
                }
            }
        }

        private IEnumerator FlyToBox(BallView ball, DestinationBoxView box)
        {
            Vector3 from = ball.transform.position;
            Vector3 target = box.CatchPoint;
            float t = 0f;
            float dur = Tune.DeliverFly;
            while (t < dur)
            {
                t += Time.deltaTime;
                float k = t / dur;
                Vector3 p = Vector3.Lerp(from, target, k);
                p.y += Mathf.Sin(k * Mathf.PI) * 0.4f; // little hop
                ball.transform.position = p;
                yield return null;
            }
            box.PlayReceive();
            _host.ReturnBall(ball);
        }

        private void OnBoxActivated(int slot, BoxModel box)
        {
            if (_boxes != null && slot >= 0 && slot < _boxes.Length) _boxes[slot].SetBox(box);
        }

        private void OnBoxCompleted(int slot, BoxModel box)
        {
            if (_boxes != null && slot >= 0 && slot < _boxes.Length) _boxes[slot].PlayComplete();
        }
    }
}
