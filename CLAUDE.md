# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

Originally a starter template; now hosts **Marble Sort** (working title) — a **fully 3D** casual colour-sort puzzle from `docs/TRUCK_BALL_JAM_GAME_SPEC.md`, styled to a candy look. Tap a **candy tile** in the grid → its stack of marbles pours down a chute onto a **straight conveyor belt** → **destination bins** auto-absorb matching colours → win when all delivered, lose if the belt overflows. All first-party code lives under `Assets/_Project/` (namespace `MarbleSort`). Third-party packs (Toony Colors Pro 2, DOTween, Layer Lab GUI Pro) are untouched.

> Design history (matched to user reference mock-ups): v1 = trucks + stadium-loop belt (from spec). v2 = hopper + candy-tile grid + straight belt (candy restyle). v3 = candy dump-trucks + straight belt + stacked bins + candy HUD. **v4 (current) = full 3D re-skin matching the reference mock-up: candy-tile grid inside a rounded teal container tray (+ mystery/numbered tiles) → chute → 3D conveyor belt (rounded slab, cylinder rollers, gold star token) → 4 stacked 3D bins → candy HUD.** Everything renders as real lit 3D geometry (perspective camera + directional key light + URP UniversalRenderer). The deterministic core (`GameState`, delivery, win/lose) is **source-agnostic** and survived all four unchanged — the 3D pass only rewrote the view layer + swapped the truck source for the tile grid.

### Marble Sort — 3D rendering (v4)

- **Everything is 3D geometry**, no 2D physics. Camera is **perspective** (`GameConfig.cameraFov/cameraPitch/cameraSize/cameraCenter`), tilted down `cameraPitch°`, distance derived from `cameraSize` (framed half-height). The camera must opt into the URP **UniversalRenderer** (renderer index **1**, set via `GetUniversalAdditionalCameraData().SetRenderer(1)` in `GameBootstrap`/`GameSceneBuilder`) or 3D-lit materials render unlit/pink.
- **Meshes** are procedural via [MeshFactory](Assets/_Project/Scripts/Utils/MeshFactory.cs): `RoundedBox` (tray / belt / bins / grey cover tiles — 6 subdivided faces projected onto a rounded surface, seamless), **`CandyBubbleTile`** (the signature pop-it tile — a rounded box body + an N×N grid of **real raised bump-domes** merged via `Mesh.CombineMeshes`, so bumps catch specular + self-shade; this is the bubble "texture" done as actual geometry, not a normal map), `StarPrism` (belt token), and cached `Sphere`/`Cylinder`/`Quad` grabbed from throwaway primitives.
- **Materials** via [Mat3D](Assets/_Project/Scripts/Utils/Mat3D.cs) — runtime **URP/Lit** cached by (colour, smoothness, metallic, emission). `Candy`/`Marble` add a subtle emission so colours stay vivid in shadow. Falls back to Standard/Sprites if URP Lit isn't found.
- **Lighting**: one directional key light (`GameBootstrap.EnsureLight`) + flat ambient (`PaletteConfig.ambientColor`). Soft shadows on.
- **Marble motion is fully scripted** (deterministic, no Rigidbody): tile pour → `GameManager.FlyToFunnel` arcs the sphere into the **funnel pool** (pseudo-pile slots, settling lerp) → `DrainFunnel` feeds one marble per `funnelDrainInterval` through the throat (`DropToBelt`) → `ConveyorController.TryIntake` (returns false on overflow = loss) → **stadium-loop belt** (`PosOf` maps arc-length onto two lanes ± `laneOffset` + semicircle end caps; marbles **train-pack** — followers clamp one `Tune.BallSpacing` behind the marble ahead) → `FlyToBox` into a bin.

### Marble Sort — how to run it (first time)

The game is authored by a **one-click Editor builder** (no manual scene setup):
1. Focus the Unity Editor so it imports `Assets/_Project/` and compiles (0 errors expected).
2. Menu **`MarbleSort → Build Scene & Prefabs`** ([GameSceneBuilder.cs](Assets/_Project/Scripts/Editor/GameSceneBuilder.cs)). This creates the config assets, the four prefabs, and `Assets/_Project/Scenes/MarbleSort.unity` (added as the first build scene) with every reference wired.
3. Open `Assets/_Project/Scenes/MarbleSort.unity` and press **Play**.

Re-running the menu item rebuilds cleanly (idempotent). Unity MCP is configured (`.mcp.json`, port 8084) for programmatic driving after a session restart.

### Marble Sort — architecture

- **Core (pure logic, no MonoBehaviour)** — [Assets/_Project/Scripts/Core](Assets/_Project/Scripts/Core): `BallColor`, `LevelData` (JSON DTOs — a **tile grid** + destination queue — + typed `LevelDefinition` + total-validation), `GameState` (the authority: conveyor counts, box queue, win/lose — source-agnostic, no truck/tile coupling), `LevelLoader`. **Physics is cosmetic only**; every win/lose decision is integer bookkeeping in `GameState` (conservation invariant documented in the file). Bin slots = `activeDestinationCount` from the level (4, like the reference) even with 6 colours: marbles whose colour has no active box keep circulating on the belt until one rotates in; box activation *prefers* currently-uncovered colours; the loss condition is intake overflow (won L2 6-colour/4-bin verified via MCP).
- **Config (ScriptableObject, never hardcoded)** — [Config](Assets/_Project/Scripts/Config): `GameConfig` (feel/timing/camera3D/pour tunables) + `PaletteConfig` (all colours — teal backdrop, candy `ballColors[]` indexed by `BallColor`, container/tile/bin/HUD colours + `lightColor`/`ambientColor`). Access via static facades `Tune.*` / `Palette.*`. `Active` is never null (bootstrap field → `Resources/Config/*.asset` → in-code defaults). **Gotcha:** the builder's `EnsureConfig` does NOT overwrite an existing `.asset`, so after changing a config default in code you must delete `Resources/Config/*.asset` before re-running the builder for the new default to take. (Adding a *new* field is fine — existing assets keep the C# initializer for unseen fields.)
- **Gameplay views (prefab-based, `Init(...)`, all 3D)** — [Gameplay](Assets/_Project/Scripts/Gameplay): `BallView` (3D sphere, scripted Flying→OnConveyor→Delivering states, no physics), `TileView` (candy **pop-it bubble** block via `CandyBubbleTile`, holding a same-colour stack; tap→press→pour→pop. Colour tiles carry **no number** (matches the reference); the two grey decoration styles are `InitCover(...)` — a `?` mystery or a dark-bar **numbered** blocker — both non-tappable, via `TextMesh`. `BoxCollider` for tapping — on `Tile.prefab`), `TileGridView` (reads the level, lays out one tile per stack **interleaved by colour**, wraps them in a rounded teal **container tray** + a mystery top row, builds the chute; references `Tile.prefab`), `ConveyorController` (**3D straight belt**: rounded slab + cylinder end-rollers + scrolling treads + gold `StarPrism`, deterministic arc-length movement w/ wrap, gate delivery, builds bins), `DestinationBoxView` (**3D stacked bin** filling bottom-up with marble spheres; fill refresh in `ConveyorController.FlyToBox`→`PlayReceive`). Visuals are **procedural** (generated in `Init`); prefabs hold only structure/components/tunables. `Conveyor.prefab` references `DestinationBox.prefab` via `[SerializeField] boxPrefab`. (Truck source removed; `IntakeZone`/2D-physics deleted.)
- **Systems** — [Systems](Assets/_Project/Scripts/Systems): `GameManager` (scene-authored coordinator; owns `GameState`, routes taps via new Input System `Pointer`+**`Physics.Raycast`**→`TileView`, arcs poured marbles to the belt via `FlyMarble`, implements `ISourceHost`/`IConveyorHost`), `GameBootstrap` (`[DefaultExecutionOrder(-100)]`, applies config + **perspective camera + key light + URP renderer index 1 + ambient**), `AudioManager` (all SFX/music synthesised at runtime), `BallPool`, `SaveSystem` (PlayerPrefs, prefix `MarbleSort.`).
- **UI (code-first uGUI, legacy `Text` + LegacyRuntime.ttf, no TMP)** — [UI/UIController.cs](Assets/_Project/Scripts/UI/UIController.cs): candy HUD (Shop, coin pill, Level pill, gear→menu, restart, 3 decorative booster buttons — magnet/knot/gumball icons + badges — belt-capacity gauge), MainMenu, LevelSelect, Win, Lose. **Glyph gotcha:** LegacyRuntime.ttf lacks many symbol glyphs (⚙ ⚒ ↻ render blank) — use SpriteFactory icon images for HUD/booster icons, not unicode.
- **Utils** — [Utils](Assets/_Project/Scripts/Utils): `MeshFactory` (procedural 3D meshes — RoundedBox/StarPrism/Sphere/Cylinder, cached), `Mat3D` (runtime URP/Lit material facade — Candy/Marble/Matte/Plastic/Glow, cached), `SpriteFactory` (procedural sprites for the **uGUI HUD** icons — gear/magnet/knot/gumball/star/etc.; still 2D since the HUD is screen-space), `UIFactory` (candy buttons/panels/labels), `ConveyorPath` (unused).
- **Levels** — `Assets/_Project/Resources/Levels/level_00N.json` (4 levels, L2+ use **6 colours** incl. purple/orange at reference density: 16–20 stacks-of-4, boxes capacity 4, 4 active bins), each a set of **trucks** (`trucks[{id,balls[]}]` — just containers; the grid flattens by colour) + round-robin `destinationQueue`. Per-colour marble totals must equal per-colour bin capacity (`LevelDefinition.ValidateTotals`); regenerate/validate with [docs/gen_levels.py](docs/gen_levels.py).

### Marble Sort — key conventions & gotchas

- **Serialize trap**: every MonoBehaviour used on a prefab/scene is in a `.cs` file named after the class.
- MonoBehaviours/visuals that are `AddComponent`-ed at runtime (tray boxes, tiles, treads, bins) don't need serialization and are fine built in code under a prefab/scene root.
- **Layout (world units)**: belt centre `(0, -4.6)`; **diamond** tile grid (rows 2→4→6→7, 7 cols) centred at `gridCenterY 0.65` inside the stepped tray; funnel spouts `1.15` above the belt; 4 bins at `binDrop 2.05` beneath the belt centre. **Perspective** camera: `cameraSize 7.4` (framed half-height), `cameraCenter (0, 0.35)`, `cameraFov 32`, `cameraPitch 6°` — top ~26% stays clear for the screen-space HUD. Grid/tray/funnel geometry serialized on `TileGridView`; belt/bin geometry on `ConveyorController`; funnel-pool pacing in `GameConfig` (`funnelDrainInterval`/`funnelDropDuration`).
- **Depth/Z convention**: scene is laid out in the XY plane (camera on −Z looking +Z); "closer to camera" = **more negative Z**. Tiles poke out toward −Z from the tray; belt slab sits behind the marbles; labels/star nudged to −Z to stay visible.
- **Pacing / loss**: delivery is deterministic — dumping the whole board at once overflows the belt (a real loss, verified: 6 tiles at once → `LostOverflow`); paced tapping wins. Tune `conveyorSpeed` / `conveyorCapacity` (GameConfig / level JSON) for difficulty. Smart box activation keeps every live colour covered so the only loss is genuine overflow.
- **Compile-check without opening Unity** (works while the editor is open) — see command block under "Working in this repo".

The original template starter notes follow.

A **starter template** for casual mobile games in Unity. Third-party asset packs live under `Assets/` (Toony Colors Pro 2, DOTween/DOTweenPro, Layer Lab GUI Pro). `Assets/Scenes/SampleScene.unity` was the original sole build scene (Marble Sort adds its own).

## Unity version (important)

- Editor version is **`2022.3.62f3`** (`ProjectSettings/ProjectVersion.txt`, branch `unity_2022`). The project was intentionally downgraded from Unity 6 — see commit `1f3bbff "down version"`. Open with this exact version to avoid a forced upgrade.
- If `ProjectVersion.txt` and `README.md` ever disagree on the version, `ProjectVersion.txt` is authoritative.

## Key stack

- **URP 14.0** — supports **both 2D and 3D**. The active pipeline `Assets/Settings/UniversalRP.asset` (guid `681886c5...`, referenced by every quality tier) holds two renderers:
  - index **0 = `Renderer2D.asset`** — the default; 2D lit/sprite scenes render with this out of the box.
  - index **1 = `UniversalRenderer.asset`** — the standard 3D forward renderer, added for 3D scenes.
  - Pick per scene/camera: leave a camera on the default for 2D, or set its **Camera → Rendering → Renderer** to `UniversalRenderer (1)` for 3D. `m_DefaultRendererIndex` stays 0, so nothing changes unless a camera opts in. Add renderers by appending to `m_RendererDataList` in `UniversalRP.asset`.
  - Global settings: `Assets/UniversalRenderPipelineGlobalSettings.asset`.
- **New Input System 1.18.0** — actions asset wired via `ProjectSettings/EditorBuildSettings.asset` (`com.unity.input.settings.actions`). Do not use the legacy `Input.*` API.
- **2D tooling**: Animation, Aseprite, PSD Importer, Sprite Shape, Tilemap (+ Extras).
- **DOTween / DOTweenPro** (`Assets/Plugins/Demigiant/`) — tweening. Settings: `Assets/Resources/DOTweenSettings.asset`.
- **Layer Lab GUI Pro-CasualGame** (`Assets/Layer Lab/`) — prefab-based casual UI kit (buttons, popups, frames, sliders). Prefer composing these prefabs for UI.
- **Toony Colors Pro 2** (`Assets/JMO Assets/`) — stylized shading; ships its own asmdefs (`ToonyColorsPro.*`).

## Unity MCP

`com.coplaydev.unity-mcp` (MCP for Unity) is a package dependency, so the editor can be driven programmatically over MCP. If `mcp__UnityMCP__*` tools are missing or on the wrong port, use the `unity-mcp-connect` skill — each open editor/project binds its own port. A `.mcp.json` exists (port **8084** as of this writing); MCP tools only load at **session start**, so after writing/updating it you must restart the Claude session. Re-detect the port with the `unity-mcp-connect` skill if the editor was restarted.

## Working in this repo

- **No build/lint/test CLI is set up.** Building and playmode happen inside the Unity Editor. The Test Framework (`com.unity.test-framework`) is installed but there are no test assemblies yet.
- To run tests headlessly once test asmdefs exist:
  ```bash
  Unity -batchmode -runTests -projectPath . -testPlatform EditMode -testResults results.xml
  ```
  (swap `EditMode`/`PlayMode`; use the 2022.3.62f3 editor binary).
- The `Assembly-CSharp*.csproj` and `.sln` at the repo root are Unity-generated and gitignored — never hand-edit them; they regenerate on import.
- Marble Sort code currently compiles into `Assembly-CSharp` (no dedicated asmdef yet). Consider adding `Assets/_Project/MarbleSort.asmdef` later for faster iteration.
- **Compile-check without opening Unity** (works even while the editor is open) — uses Unity's own Roslyn + reference list from the generated csproj:
  ```bash
  UNITY="/c/Program Files/Unity/Hub/Editor/2022.3.62f3/Editor/Data"
  grep -oE '<HintPath>[^<]+</HintPath>' Assembly-CSharp.csproj | sed 's/<[^>]*>//g' > /tmp/refs.txt
  REFS=""; while IFS= read -r p; do REFS="$REFS -r:\"$p\""; done < /tmp/refs.txt
  SRC=$(find Assets/_Project/Scripts -name "*.cs" ! -path "*/Editor/*" | sed 's/.*/"&"/' | tr '\n' ' ')
  eval "\"$UNITY/NetCoreRuntime/dotnet.exe\" \"$UNITY/DotNetSdkRoslyn/csc.dll\" -nologo -target:library \
    -nostdlib -noconfig -langversion:9.0 -out:/tmp/Runtime.dll $REFS $SRC"
  # Editor scripts: add -define:UNITY_EDITOR -r:"$UNITY/Managed/UnityEngine/UnityEditor.CoreModule.dll" -r:/tmp/Runtime.dll
  #   and point SRC at Assets/_Project/Scripts/Editor. (Reference ONLY CoreModule, not UnityEditor.dll, to avoid CS0433.)
  ```

## Conventions for new game code

The available skills encode the intended patterns for this template — follow them when the task matches:
- **`unity-game-clone`** — scene-authored + prefab architecture, tunables/colors in ScriptableObject configs (never hardcoded), procedural sprites/audio, solver-generated levels. Use when building a game from a spec.
- **`unity-ui-refactor`** — code-first uGUI (UIFactory/SpriteFactory), procedural "candy" UI, no image assets.
- **`texture-override`** — pick compression format by target device (TV/desktop → DXT, mobile/TikTok → ASTC) when building WebGL.
- **`tiktok-minigame-sdk`** / **`tv-input-kit`** — TikTok Mini Game (WebGL) and TV-remote input integration.

## Git

- Default branch is `main`; active work is on `unity_2022`.
- Auto-generated folders (`Library/`, `Temp/`, `Logs/`, `obj/`, IDE/`.sln`/`.csproj` files) are gitignored.
