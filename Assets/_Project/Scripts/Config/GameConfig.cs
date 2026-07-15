using UnityEngine;

namespace MarbleSort.Config
{
    /// <summary>
    /// All gameplay/feel tunables. Designers edit the asset at
    /// Resources/Config/GameConfig.asset; code reads via the <see cref="Tune"/> facade.
    /// <see cref="Active"/> is never null (bootstrap field -> Resources -> in-memory defaults).
    /// </summary>
    [CreateAssetMenu(fileName = "GameConfig", menuName = "MarbleSort/Game Config", order = 0)]
    public class GameConfig : ScriptableObject
    {
        [Header("App")]
        public int targetFrameRate = 60;

        [Header("Camera (perspective 3D, portrait)")]
        public float cameraSize = 8.3f;           // framed half-height (drives perspective distance)
        public Vector2 cameraCenter = new Vector2(0f, 0.6f);
        public float cameraFov = 32f;             // vertical field of view
        public float cameraPitch = 6f;            // downward tilt (degrees) for the 3D look
        public float tileDepth = 0.5f;            // Z-thickness of candy tiles

        [Header("Tile / hopper")]
        public float tileReleaseSpread = 0.5f;    // marbles from one tile spill over this long
        public float tilePopDuration = 0.18f;     // tile clear (shrink) animation
        public float tilePressScale = 0.12f;      // tap press feedback
        public float pourFlyDuration = 0.45f;     // marble arc time from tile into the funnel pool
        public float funnelDrainInterval = 0.2f;  // one marble leaves the funnel throat per interval
        public float funnelDropDuration = 0.16f;  // throat -> belt drop animation

        [Header("Ball physics (Physics2D, cosmetic slide)")]
        public float ballRadius = 0.3f;
        public float ballGravityScale = 2.2f;
        public float ballBounciness = 0.35f;
        public float ballLinearDrag = 0.15f;
        public float ballMaxSpeed = 22f;
        public Vector2 spillJitter = new Vector2(0.22f, 0.15f);
        public float releaseImpulse = 2.2f;       // outward nudge when spilling
        // Safety: a ball sliding longer than this is snapped to intake so the level can't stall.
        public float slideStuckTimeout = 3.5f;

        [Header("Conveyor")]
        public float conveyorSpeed = 3.2f;        // world units / sec along the loop path
        public float ballSpacing = 0.62f;         // min arc-length gap between balls on the belt (≈ touching)
        public float deliverFlyDuration = 0.28f;  // ball -> box animation time

        [Header("Feel / juice")]
        public float boxPunchScale = 0.28f;
        public float boxCompleteScale = 0.5f;
        public float tapCooldown = 0.12f;

        [Header("Audio volumes")]
        [Range(0f, 1f)] public float sfxVolume = 0.8f;
        [Range(0f, 1f)] public float musicVolume = 0.35f;

        // ---- never-null access ------------------------------------------
        private static GameConfig _active;
        public static GameConfig Active
        {
            get
            {
                if (_active == null) _active = Resources.Load<GameConfig>("Config/GameConfig");
                if (_active == null) _active = CreateInstance<GameConfig>(); // defaults above
                return _active;
            }
        }
        public static void SetActive(GameConfig c) { if (c != null) _active = c; }
    }

    /// <summary>Terse static facade so call sites read Tune.ConveyorSpeed, not GameConfig.Active.x.</summary>
    public static class Tune
    {
        public static GameConfig C { get { return GameConfig.Active; } }

        public static int TargetFps { get { return C.targetFrameRate; } }
        public static float CameraSize { get { return C.cameraSize; } }
        public static Vector2 CameraCenter { get { return C.cameraCenter; } }
        public static float CameraFov { get { return C.cameraFov; } }
        public static float CameraPitch { get { return C.cameraPitch; } }
        public static float TileDepth { get { return C.tileDepth; } }

        public static float TileReleaseSpread { get { return C.tileReleaseSpread; } }
        public static float TilePop { get { return C.tilePopDuration; } }
        public static float TilePress { get { return C.tilePressScale; } }
        public static float PourFly { get { return C.pourFlyDuration; } }
        public static float FunnelDrain { get { return C.funnelDrainInterval; } }
        public static float FunnelDrop { get { return C.funnelDropDuration; } }

        public static float BallRadius { get { return C.ballRadius; } }
        public static float BallGravity { get { return C.ballGravityScale; } }
        public static float BallBounce { get { return C.ballBounciness; } }
        public static float BallDrag { get { return C.ballLinearDrag; } }
        public static float BallMaxSpeed { get { return C.ballMaxSpeed; } }
        public static Vector2 SpillJitter { get { return C.spillJitter; } }
        public static float ReleaseImpulse { get { return C.releaseImpulse; } }
        public static float SlideTimeout { get { return C.slideStuckTimeout; } }

        public static float ConveyorSpeed { get { return C.conveyorSpeed; } }
        public static float BallSpacing { get { return C.ballSpacing; } }
        public static float DeliverFly { get { return C.deliverFlyDuration; } }

        public static float BoxPunch { get { return C.boxPunchScale; } }
        public static float BoxComplete { get { return C.boxCompleteScale; } }
        public static float TapCooldown { get { return C.tapCooldown; } }

        public static float SfxVolume { get { return C.sfxVolume; } }
        public static float MusicVolume { get { return C.musicVolume; } }
    }
}
