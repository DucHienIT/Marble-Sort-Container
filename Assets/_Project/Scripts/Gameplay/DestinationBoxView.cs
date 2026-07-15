using System.Collections;
using UnityEngine;
using MarbleSort.Core;
using MarbleSort.Config;
using MarbleSort.Utils;

namespace MarbleSort.Gameplay
{
    /// <summary>
    /// A destination bin as in the reference mock-up: a tall green container with lighter
    /// horizontal front slats and an open top where delivered marbles collect (rows of three,
    /// filling top-down from the opening). The interior well is tinted with the required marble
    /// colour so the player can read the target. When its box completes it flashes/pops and the
    /// next queued box slides in. Built procedurally on DestinationBox.prefab.
    /// </summary>
    public class DestinationBoxView : MonoBehaviour
    {
        private const float BinWidth = 1.86f;
        private const float BinHeight = 2.6f;
        private const float MarbleDia = 0.42f;
        private const int PerRow = 4;

        public int Slot { get; private set; }
        private BoxModel _box;

        private Transform _scaleRoot;
        private Transform _partRoot;
        private MeshRenderer _frame;
        private MeshRenderer[] _slots;

        public void Init(int slot, Vector3 worldPos)
        {
            Slot = slot;
            transform.position = worldPos;
            _scaleRoot = new GameObject("Scale").transform;
            _scaleRoot.SetParent(transform, false);
        }

        public void SetBox(BoxModel box)
        {
            _box = box;
            if (_partRoot != null) Destroy(_partRoot.gameObject);
            _partRoot = new GameObject("Parts").transform;
            _partRoot.SetParent(_scaleRoot, false);

            int cap = box != null ? box.Capacity : 4;

            _frame = AddBox("Frame", new Vector3(0, 0, 0.32f), new Vector3(BinWidth, BinHeight, 0.5f), 0.2f, Mat3D.Matte(Palette.BinFrame));

            // dark open mouth at the top where delivered marbles collect
            AddBox("Mouth", new Vector3(0, BinHeight * 0.5f - 0.5f, 0.18f),
                new Vector3(BinWidth - 0.22f, 0.82f, 0.34f), 0.14f, Mat3D.Matte(Palette.BinInner));

            // lighter green front slats over the lower body, dark well showing in the gaps
            AddBox("Well", new Vector3(0, -0.5f, 0.2f), new Vector3(BinWidth - 0.2f, BinHeight - 1.1f, 0.36f), 0.14f,
                Mat3D.Matte(Palette.BinInner));
            for (int i = 0; i < 3; i++)
            {
                AddBox("Slat" + i, new Vector3(0, -BinHeight * 0.5f + 0.38f + i * 0.66f, -0.14f + i * 0.004f),
                    new Vector3(BinWidth, 0.5f, 0.26f), 0.13f, Mat3D.Matte(Palette.BinSlat));
            }

            // marble slots fill the mouth left→right, extra rows continue below
            _slots = new MeshRenderer[cap];
            for (int i = 0; i < cap; i++)
            {
                int row = i / PerRow, col = i % PerRow;
                var go = new GameObject("SlotBall" + i);
                go.transform.SetParent(_partRoot, false);
                go.transform.localPosition = new Vector3((col - (PerRow - 1) * 0.5f) * (MarbleDia + 0.02f),
                    BinHeight * 0.5f - 0.5f - row * (MarbleDia + 0.04f), -0.05f);
                go.transform.localScale = Vector3.one * MarbleDia;
                var mf = go.AddComponent<MeshFilter>();
                var mr = go.AddComponent<MeshRenderer>();
                mf.sharedMesh = MeshFactory.Sphere();
                _slots[i] = mr;
            }
            RefreshFill();
        }

        private MeshRenderer AddBox(string name, Vector3 pos, Vector3 size, float radius, Material mat)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_scaleRoot, false);
            go.transform.localPosition = pos;
            var mf = go.AddComponent<MeshFilter>();
            var mr = go.AddComponent<MeshRenderer>();
            mf.sharedMesh = MeshFactory.RoundedBox(size, radius, 4);
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            return mr;
        }

        public void RefreshFill()
        {
            if (_box == null || _slots == null) return;
            Color c = Palette.Ball(_box.Color);
            Color ghost = Color.Lerp(c, Color.black, 0.42f);
            Color pastel = Color.Lerp(c, Color.white, 0.22f);   // delivered marbles read pastel like the reference
            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i] == null) continue;
                bool filled = i < _box.Filled;
                // unfilled slots stay visible as small dim "ghost" marbles so the required colour reads
                _slots[i].sharedMaterial = filled ? Mat3D.Marble(pastel) : Mat3D.Matte(ghost);
                _slots[i].transform.localScale = Vector3.one * (filled ? MarbleDia : MarbleDia * 0.62f);
            }
        }

        public Vector3 CatchPoint { get { return transform.position + new Vector3(0f, BinHeight * 0.5f - 0.5f, -0.05f); } }

        public void PlayReceive()
        {
            RefreshFill();
            StopAllCoroutines();
            StartCoroutine(Punch(Tune.BoxPunch, 0.16f));
        }

        public void PlayComplete()
        {
            StopAllCoroutines();
            StartCoroutine(CompleteRoutine());
        }

        private IEnumerator CompleteRoutine()
        {
            Material glow = Mat3D.Glow(Palette.BinCompleted);
            if (_frame != null) _frame.sharedMaterial = glow;
            float t = 0f;
            while (t < 0.24f)
            {
                t += Time.deltaTime;
                _scaleRoot.localScale = Vector3.one * (1f + Tune.BoxComplete * Mathf.Sin(t / 0.24f * Mathf.PI));
                yield return null;
            }
            _scaleRoot.localScale = Vector3.one;
        }

        private IEnumerator Punch(float amount, float dur)
        {
            float t = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                _scaleRoot.localScale = Vector3.one * (1f + amount * Mathf.Sin(t / dur * Mathf.PI));
                yield return null;
            }
            _scaleRoot.localScale = Vector3.one;
        }
    }
}
