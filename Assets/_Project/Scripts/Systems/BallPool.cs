using System.Collections.Generic;
using UnityEngine;
using MarbleSort.Core;
using MarbleSort.Gameplay;

namespace MarbleSort.Systems
{
    /// <summary>
    /// Simple pool for BallView instances. Instantiates from Ball.prefab, reuses on return.
    /// Author on a scene object (or created by GameManager); references Ball.prefab.
    /// </summary>
    public class BallPool : MonoBehaviour
    {
        [SerializeField] private BallView ballPrefab;

        private readonly Stack<BallView> _idle = new Stack<BallView>();
        private Transform _root;

        public void Init()
        {
            if (_root == null)
            {
                _root = new GameObject("BallPoolRoot").transform;
                _root.SetParent(transform, false);
            }
        }

        public BallView Get(BallColor color, int sortingOrder)
        {
            BallView b = null;
            while (_idle.Count > 0 && b == null) b = _idle.Pop();
            if (b == null)
            {
                if (ballPrefab != null)
                {
                    b = Instantiate(ballPrefab, _root);
                }
                else
                {
                    var go = new GameObject("Ball", typeof(MeshFilter), typeof(MeshRenderer));
                    go.transform.SetParent(_root, false);
                    b = go.AddComponent<BallView>();
                }
            }
            b.gameObject.SetActive(true);
            b.Init(color, sortingOrder);
            return b;
        }

        public void Return(BallView ball)
        {
            if (ball == null) return;
            ball.transform.SetParent(_root, false);
            ball.gameObject.SetActive(false);
            _idle.Push(ball);
        }

        /// <summary>Return every ball (called on level teardown/restart to avoid leaks).</summary>
        public void ReturnAll(List<BallView> active)
        {
            for (int i = 0; i < active.Count; i++) Return(active[i]);
            active.Clear();
        }
    }
}
