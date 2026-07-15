using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MarbleSort.Core;
using MarbleSort.Config;
using MarbleSort.Utils;

namespace MarbleSort.Gameplay
{
    /// <summary>Services a tile needs from the game host (marble spawning + pour flight + audio).</summary>
    public interface ISourceHost
    {
        BallView SpawnBall(BallColor color);
        void PourMarble(BallView ball, Vector3 fromWorld);
        void PlaySourceOpen();
        void PlayMarbleRelease(int index, int total);
    }

    /// <summary>
    /// A single candy tile in the grid — a glossy rounded 3D block holding a small stack of same-colour
    /// marbles. Tap it and the stack pours down the chute onto the belt, then the tile pops away.
    /// A "mystery" tile is grey decoration (a "?" cover) that isn't tappable. Numbered tiles show
    /// their remaining count. Visual only; authoritative counts live in <see cref="GameState"/>.
    /// </summary>
    public class TileView : MonoBehaviour
    {
        public int Index { get; private set; }
        public bool IsMystery { get; private set; }
        public bool CanTap { get { return !IsMystery && !_dumped && _load.Count > 0; } }

        private ISourceHost _host;
        private BallColor _color;
        private float _size;
        private bool _dumped;
        private float _bob;

        private Transform _block;      // the tile mesh (tips when tapped)
        private readonly List<BallColor> _load = new List<BallColor>();

        /// <summary>Build a candy tile carrying <paramref name="load"/> marbles of one colour.
        /// <paramref name="bubble"/> picks the pop-it bump face; otherwise a flat glossy candy face
        /// (the reference mixes both styles across the grid).</summary>
        public void InitCandy(int index, BallColor color, List<BallColor> load, ISourceHost host,
            float size, Vector3 pos, bool bubble)
        {
            Index = index;
            _color = color;
            _host = host;
            _size = size;
            IsMystery = false;
            _load.AddRange(load);
            transform.position = pos;
            _bob = index * 1.3f;
            BuildBlock(Palette.Ball(color), bubble ? TileStyle.Bubble : TileStyle.Flat);
        }

        /// <summary>Build a smooth grey "cover" decoration tile (not tappable): a "?" mystery, or a
        /// dark-bar numbered blocker — the two grey tile styles in the reference.</summary>
        public void InitCover(int index, float size, Vector3 pos, string label, bool numbered)
        {
            Index = index;
            _host = null;
            _size = size;
            IsMystery = true;
            transform.position = pos;
            BuildBlock(Palette.TileMystery, TileStyle.Cover);
            if (numbered) { BuildBar(); BuildLabelAt(label, _size * 0.1f, _size * 0.5f, UnityEngine.Color.white); }
            else BuildLabelAt(label, 0f, _size * 0.56f, Palette.TileMysteryMark);
        }

        private enum TileStyle { Flat, Bubble, Cover }

        private void BuildBlock(Color color, TileStyle style)
        {
            float depth = Tune.TileDepth;
            var go = new GameObject("Block");
            go.transform.SetParent(transform, false);
            var mf = go.AddComponent<MeshFilter>();
            var mr = go.AddComponent<MeshRenderer>();
            switch (style)
            {
                case TileStyle.Bubble:
                    mf.sharedMesh = MeshFactory.CandyBubbleTile(_size, depth, 3);
                    mr.sharedMaterial = Mat3D.Candy(color);
                    break;
                case TileStyle.Flat:
                    mf.sharedMesh = MeshFactory.RoundedBox(new Vector3(_size, _size, depth), _size * 0.22f, 7);
                    mr.sharedMaterial = Mat3D.Candy(color);
                    break;
                default:
                    mf.sharedMesh = MeshFactory.RoundedBox(new Vector3(_size, _size, depth), _size * 0.22f, 7);
                    mr.sharedMaterial = Mat3D.Plastic(color);
                    break;
            }
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            _block = go.transform;

            var col = gameObject.GetComponent<BoxCollider>();
            if (col == null) col = gameObject.AddComponent<BoxCollider>();
            col.size = new Vector3(_size, _size, depth + 0.2f);
        }

        private void BuildBar()
        {
            float depth = Tune.TileDepth;
            var bar = new GameObject("Bar");
            bar.transform.SetParent(transform, false);
            bar.transform.localPosition = new Vector3(0f, -_size * 0.32f, -depth * 0.5f - 0.02f);
            var mf = bar.AddComponent<MeshFilter>();
            var mr = bar.AddComponent<MeshRenderer>();
            mf.sharedMesh = MeshFactory.RoundedBox(new Vector3(_size * 0.78f, _size * 0.24f, 0.14f), 0.08f, 4);
            mr.sharedMaterial = Mat3D.Matte(Palette.TileNumberBar);

            // small light handle-line inside the dark bar (matches the reference blocker)
            var notch = new GameObject("Notch");
            notch.transform.SetParent(bar.transform, false);
            notch.transform.localPosition = new Vector3(0f, 0f, -0.09f);
            var nmf = notch.AddComponent<MeshFilter>();
            var nmr = notch.AddComponent<MeshRenderer>();
            nmf.sharedMesh = MeshFactory.RoundedBox(new Vector3(_size * 0.36f, _size * 0.055f, 0.04f), 0.02f, 3);
            nmr.sharedMaterial = Mat3D.Matte(Palette.TileMysteryMark);
        }

        private TextMesh BuildLabelAt(string s, float yOffset, float worldHeight, Color color)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, yOffset, -Tune.TileDepth * 0.5f - 0.18f);
            var tm = go.AddComponent<TextMesh>();
            tm.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            go.GetComponent<MeshRenderer>().sharedMaterial = tm.font.material;
            tm.text = s;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.fontSize = 72;
            tm.characterSize = worldHeight / 8f;
            tm.color = color;
            return tm;
        }

        private void Update()
        {
            if (_block == null) return;
            if (CanTap)
            {
                float t = Time.time * 2.4f + _bob;
                _block.localPosition = new Vector3(0f, Mathf.Sin(t) * 0.03f, 0f);
            }
        }

        public void BeginPour()
        {
            if (!CanTap) return;
            StartCoroutine(PourRoutine());
        }

        private IEnumerator PourRoutine()
        {
            _dumped = true;
            if (_host != null) _host.PlaySourceOpen();

            // quick press-in
            float pt = 0f;
            while (pt < 0.08f) { pt += Time.deltaTime; _block.localScale = Vector3.one * (1f - 0.12f * (pt / 0.08f)); yield return null; }
            _block.localScale = Vector3.one;

            int total = _load.Count;
            for (int i = 0; i < total; i++)
            {
                var color = _load[i];
                var ball = _host.SpawnBall(color);
                Vector3 wp = transform.position + new Vector3(Random.Range(-0.12f, 0.12f), _size * 0.2f, -Tune.TileDepth);
                ball.transform.position = wp;
                _host.PourMarble(ball, wp);
                _host.PlayMarbleRelease(i, total);
                if (total > 1) yield return new WaitForSeconds(Tune.TileReleaseSpread / total);
            }
            _load.Clear();

            // pop away
            float t = 0f;
            Vector3 s0 = transform.localScale;
            while (t < Tune.TilePop)
            {
                t += Time.deltaTime;
                float k = 1f - t / Tune.TilePop;
                transform.localScale = s0 * Mathf.Max(0f, k);
                yield return null;
            }
            gameObject.SetActive(false);
        }
    }
}
