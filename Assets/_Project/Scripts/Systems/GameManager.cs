using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using MarbleSort.Core;
using MarbleSort.Config;
using MarbleSort.Gameplay;
using MarbleSort.UI;

namespace MarbleSort.Systems
{
    /// <summary>
    /// Central coordinator. Owns the <see cref="GameState"/> authority, builds the hopper (tile
    /// grid) + conveyor, routes taps, and drives win/lose/flow. Authored in the scene with all
    /// references serialized. Implements the hopper/conveyor host interfaces so views stay decoupled.
    /// </summary>
    public class GameManager : MonoBehaviour, ISourceHost, IConveyorHost
    {
        [Header("Scene references")]
        [SerializeField] private UIController ui;
        [SerializeField] private ConveyorController conveyor;
        [SerializeField] private TileGridView tileGrid;
        [SerializeField] private AudioManager audioManager;
        [SerializeField] private BallPool ballPool;
        [SerializeField] private Transform ballRoot;
        [SerializeField] private Camera gameCamera;

        [Header("Layout (world units)")]
        [SerializeField] private Vector2 conveyorCenter = new Vector2(0f, -4.6f);
        [SerializeField] private float spoutHeightAboveBelt = 1.15f;

        public GameState State { get; private set; }
        public int CurrentLevel { get; private set; }
        public int LevelCount { get; private set; }
        public bool IsPaused { get; private set; }

        private readonly List<BallView> _activeBalls = new List<BallView>();
        private bool _playing;

        // funnel pool: poured marbles pile up here, then drain single-file onto the belt
        private readonly List<BallView> _funnel = new List<BallView>();
        private readonly HashSet<BallView> _settled = new HashSet<BallView>();
        private Vector2 _spout;
        private Coroutine _drain;

        private void Start()
        {
            if (gameCamera == null) gameCamera = Camera.main;
            if (ballRoot == null) ballRoot = new GameObject("BallRoot").transform;
            if (ballPool != null) ballPool.Init();
            LevelCount = Mathf.Max(1, LevelLoader.CountLevels());

            if (ui != null) ui.Init(this);
            if (audioManager != null) audioManager.StartMusic();
            ShowMenu();
        }

        // ---- flow --------------------------------------------------------

        public void ShowMenu()
        {
            _playing = false;
            Teardown();
            if (ui != null) ui.ShowMainMenu(SaveSystem.MaxUnlockedLevel, LevelCount);
        }

        public void StartLevel(int levelNumber)
        {
            levelNumber = Mathf.Clamp(levelNumber, 1, Mathf.Max(1, LevelCount));
            var def = LevelLoader.Load(levelNumber);
            if (def == null)
            {
                Debug.LogError("[MarbleSort] Cannot start level " + levelNumber + " (missing).");
                ShowMenu();
                return;
            }

            Teardown();
            CurrentLevel = levelNumber;
            IsPaused = false;
            Time.timeScale = 1f;

            State = new GameState(def);
            State.Won += OnWon;
            State.LostOverflow += OnLost;

            conveyor.transform.position = conveyorCenter;
            conveyor.Init(State, this);

            _spout = new Vector2(conveyorCenter.x, conveyorCenter.y + spoutHeightAboveBelt);
            if (tileGrid != null) tileGrid.Build(def, this, _spout);

            _funnel.Clear();
            _settled.Clear();
            if (_drain != null) StopCoroutine(_drain);
            _drain = StartCoroutine(DrainFunnel());

            _playing = true;
            if (ui != null) ui.ShowHud(this);
            if (audioManager != null) audioManager.StartMusic();
        }

        public void RestartLevel() { StartLevel(CurrentLevel); }

        public void NextLevel()
        {
            if (CurrentLevel < LevelCount) StartLevel(CurrentLevel + 1);
            else ShowMenu();
        }

        private void Teardown()
        {
            if (_drain != null) { StopCoroutine(_drain); _drain = null; }
            _funnel.Clear();
            _settled.Clear();
            if (ballPool != null) ballPool.ReturnAll(_activeBalls);
            else _activeBalls.Clear();
            if (conveyor != null) conveyor.Teardown();
            if (tileGrid != null)
                for (int i = tileGrid.transform.childCount - 1; i >= 0; i--)
                    Destroy(tileGrid.transform.GetChild(i).gameObject);
            State = null;
            _playing = false;
        }

        // ---- input -------------------------------------------------------

        private void Update()
        {
            if (!_playing || IsPaused || State == null) return;
            HandleTapInput();
            SettleFunnel();
        }

        /// <summary>Settled marbles slide toward their (shifting) pile slot as the pool drains.</summary>
        private void SettleFunnel()
        {
            for (int i = 0; i < _funnel.Count; i++)
            {
                var b = _funnel[i];
                if (b == null || !_settled.Contains(b)) continue;
                Vector3 target = PoolPos(i);
                b.transform.position = Vector3.Lerp(b.transform.position, target, Time.deltaTime * 9f);
            }
        }

        private void HandleTapInput()
        {
            var pointer = Pointer.current;
            if (pointer == null) return;
            if (!pointer.press.wasPressedThisFrame) return;
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

            Vector2 screen = pointer.position.ReadValue();
            Ray ray = gameCamera.ScreenPointToRay(new Vector3(screen.x, screen.y, 0f));
            RaycastHit hit;
            if (!Physics.Raycast(ray, out hit, 200f)) return;
            var tile = hit.collider.GetComponentInParent<TileView>();
            if (tile != null && tile.CanTap) tile.BeginPour();
        }

        // ---- ISourceHost -------------------------------------------------

        public BallView SpawnBall(BallColor color)
        {
            BallView b = ballPool != null ? ballPool.Get(color, 0) : null;
            if (b == null)
            {
                var go = new GameObject("Ball", typeof(MeshFilter), typeof(MeshRenderer));
                b = go.AddComponent<BallView>();
                b.Init(color);
            }
            _activeBalls.Add(b);
            return b;
        }

        /// <summary>Arc a poured marble from the tile into the funnel pool. It piles up there and
        /// the drain coroutine later feeds it single-file through the throat onto the belt.</summary>
        public void PourMarble(BallView ball, Vector3 fromWorld)
        {
            if (State != null) State.ReleaseBall();
            ball.SetFlying(ballRoot);
            StartCoroutine(FlyToFunnel(ball, fromWorld));
        }

        /// <summary>Pseudo-pile slot inside the funnel: rows of 3,4,5… so the pool reads as a
        /// spread-out heap (like the reference) rather than a thin column.</summary>
        private Vector3 PoolPos(int index)
        {
            int r = 0, start = 0, w = 3;
            while (index >= start + w) { start += w; r++; w++; }
            int k = index - start;
            float d = Tune.BallRadius * 2f * 0.94f;
            float jitter = ((index * 73) % 7 - 3) * 0.02f;
            float x = (k - (w - 1) * 0.5f) * d + jitter;
            float y = _spout.y + 0.7f + r * d * 0.84f;
            return new Vector3(_spout.x + x, y, -0.05f);
        }

        private System.Collections.IEnumerator FlyToFunnel(BallView ball, Vector3 from)
        {
            int slot = _funnel.Count;
            _funnel.Add(ball);   // reserve the slot; settling in Update keeps it packed
            Vector3 to = PoolPos(slot);
            float dur = Mathf.Max(0.05f, Tune.PourFly);
            float t = 0f;
            while (t < dur)
            {
                if (ball == null || !ball.gameObject.activeSelf || !ball.IsFlying) yield break;
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / dur);
                float e = k * k;                       // accelerate (gravity feel)
                int idx = _funnel.IndexOf(ball);
                if (idx < 0) yield break;              // already drained mid-flight
                to = PoolPos(idx);
                Vector3 p = Vector3.Lerp(from, to, e);
                p.x = Mathf.Lerp(from.x, to.x, k);     // ease horizontal linearly toward the funnel
                ball.transform.position = p;
                yield return null;
            }
            if (ball != null) _settled.Add(ball);      // now the pile-settling in Update owns it
        }

        /// <summary>Feed the funnel pool one marble at a time through the throat onto the belt.</summary>
        private System.Collections.IEnumerator DrainFunnel()
        {
            var wait = new WaitForSeconds(Tune.FunnelDrain);
            while (true)
            {
                if (_funnel.Count > 0 && State != null && State.Status == LevelStatus.Playing)
                {
                    var ball = _funnel[0];
                    _funnel.RemoveAt(0);
                    _settled.Remove(ball);
                    if (ball != null && ball.gameObject.activeSelf) StartCoroutine(DropToBelt(ball));
                    yield return wait;
                }
                else yield return null;
            }
        }

        private System.Collections.IEnumerator DropToBelt(BallView ball)
        {
            Vector3 from = ball.transform.position;
            Vector3 throat = new Vector3(_spout.x, _spout.y, -0.05f);
            Vector3 to = new Vector3(conveyor.IntakeWorldPos.x, conveyor.IntakeWorldPos.y, -0.05f);
            float dur = Mathf.Max(0.05f, Tune.FunnelDrop);
            float t = 0f;
            while (t < dur)
            {
                if (ball == null || !ball.gameObject.activeSelf) yield break;
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / dur);
                // slide to the throat first, then fall straight to the belt
                Vector3 p = k < 0.5f
                    ? Vector3.Lerp(from, throat, k * 2f)
                    : Vector3.Lerp(throat, to, (k - 0.5f) * 2f * (k - 0.5f) * 2f);
                ball.transform.position = p;
                yield return null;
            }
            if (ball == null || !ball.gameObject.activeSelf) yield break;
            if (!conveyor.TryIntake(ball)) ReturnBall(ball); // overflow -> loss; recycle the marble
        }

        public void PlaySourceOpen() { if (audioManager != null) { audioManager.PlayTap(); audioManager.PlayRattle(); } }

        public void PlayMarbleRelease(int index, int total) { if (audioManager != null) audioManager.PlayRelease(index, total); }

        // ---- IConveyorHost -----------------------------------------------

        public void ReturnBall(BallView ball)
        {
            _activeBalls.Remove(ball);
            if (ballPool != null) ballPool.Return(ball);
            else if (ball != null) Destroy(ball.gameObject);
        }

        public void PlayIntake() { if (audioManager != null) audioManager.PlayIntake(); }
        public void PlayDeliver() { if (audioManager != null) audioManager.PlayDeliver(); }

        // ---- win / lose --------------------------------------------------

        private void OnWon()
        {
            _playing = false;
            SaveSystem.UnlockLevel(CurrentLevel + 1);
            if (audioManager != null) audioManager.PlayWin();
            if (ui != null) ui.ShowWin(this);
        }

        private void OnLost()
        {
            _playing = false;
            if (audioManager != null) audioManager.PlayLose();
            if (ui != null) ui.ShowLose(this);
        }

        // ---- pause -------------------------------------------------------

        public void SetPaused(bool paused)
        {
            IsPaused = paused;
            Time.timeScale = paused ? 0f : 1f;
        }

        public void TogglePause() { SetPaused(!IsPaused); }

        public bool HasNextLevel { get { return CurrentLevel < LevelCount; } }
    }
}
