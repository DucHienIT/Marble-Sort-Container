using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using UnityEngine.InputSystem.UI;
using MarbleSort.Config;
using MarbleSort.Gameplay;
using MarbleSort.Systems;
using MarbleSort.UI;

namespace MarbleSort.EditorTools
{
    /// <summary>
    /// One-click authoring of the whole 3D game: config assets, prefabs (Ball/Tile/DestinationBox/
    /// Conveyor), and the playable scene with a perspective camera, a key light, and every reference
    /// wired. Run once from the menu after scripts compile. Idempotent.
    /// </summary>
    public static class GameSceneBuilder
    {
        private const string PrefabDir = "Assets/_Project/Prefabs";
        private const string ConfigDir = "Assets/_Project/Resources/Config";
        private const string SceneDir = "Assets/_Project/Scenes";
        private const string ScenePath = SceneDir + "/MarbleSort.unity";

        [MenuItem("MarbleSort/Build Scene & Prefabs")]
        public static void Build()
        {
            EnsureFolder("Assets/_Project", "Prefabs");
            EnsureFolder("Assets/_Project", "Scenes");
            EnsureFolder("Assets/_Project", "Resources");
            EnsureFolder("Assets/_Project/Resources", "Config");

            var gameConfig = EnsureConfig<GameConfig>(ConfigDir + "/GameConfig.asset");
            var palette = EnsureConfig<PaletteConfig>(ConfigDir + "/PaletteConfig.asset");

            // ---- prefabs (written to disk) ----
            BuildBallPrefab();
            BuildTilePrefab();
            BuildBoxPrefab();
            BuildConveyorPrefab();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // ---- scene ----
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var ballPrefab = AssetDatabase.LoadAssetAtPath<BallView>(PrefabDir + "/Ball.prefab");
            var tilePrefab = AssetDatabase.LoadAssetAtPath<TileView>(PrefabDir + "/Tile.prefab");
            var conveyorPrefab = AssetDatabase.LoadAssetAtPath<ConveyorController>(PrefabDir + "/Conveyor.prefab");

            var cam = Object.FindObjectOfType<Camera>();
            if (cam != null)
            {
                cam.orthographic = false;
                cam.fieldOfView = gameConfig.cameraFov;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = palette.backgroundBottom;
                cam.nearClipPlane = 0.3f;
                cam.farClipPlane = 120f;
                float half = gameConfig.cameraSize;
                float dist = half / Mathf.Tan(gameConfig.cameraFov * Mathf.Deg2Rad * 0.5f);
                float pr = gameConfig.cameraPitch * Mathf.Deg2Rad;
                Vector3 target = new Vector3(gameConfig.cameraCenter.x, gameConfig.cameraCenter.y, 0f);
                cam.transform.position = target + new Vector3(0f, dist * Mathf.Sin(pr), -dist * Mathf.Cos(pr));
                cam.transform.rotation = Quaternion.Euler(gameConfig.cameraPitch, 0f, 0f);
                var data = cam.GetUniversalAdditionalCameraData();
                if (data != null) { try { data.SetRenderer(1); } catch { } }
            }

            // key directional light
            var lightGo = new GameObject("KeyLight", typeof(Light));
            var keyLight = lightGo.GetComponent<Light>();
            keyLight.type = LightType.Directional;
            keyLight.color = palette.lightColor;
            keyLight.intensity = 1.15f;
            keyLight.shadows = LightShadows.Soft;
            lightGo.transform.rotation = Quaternion.Euler(52f, -28f, 0f);

            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            var audio = new GameObject("AudioManager", typeof(AudioManager)).GetComponent<AudioManager>();

            var pool = new GameObject("BallPool", typeof(BallPool)).GetComponent<BallPool>();
            SetRef(pool, "ballPrefab", ballPrefab);

            // UI canvas
            var canvasGo = new GameObject("UICanvas", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(UIController));
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            var ui = canvasGo.GetComponent<UIController>();

            // Conveyor (belt centre)
            var conveyorGo = (GameObject)PrefabUtility.InstantiatePrefab(conveyorPrefab.gameObject);
            conveyorGo.transform.position = new Vector3(0, -4.2f, 0);
            var conveyor = conveyorGo.GetComponent<ConveyorController>();

            // Tile grid (holds the candy tiles) — scene object referencing Tile.prefab
            var gridGo = new GameObject("TileGrid", typeof(TileGridView));
            var tileGrid = gridGo.GetComponent<TileGridView>();
            SetRef(tileGrid, "tilePrefab", tilePrefab);

            var ballRoot = new GameObject("BallRoot").transform;

            // Bootstrap
            var boot = new GameObject("GameBootstrap", typeof(GameBootstrap)).GetComponent<GameBootstrap>();
            SetRef(boot, "gameConfig", gameConfig);
            SetRef(boot, "paletteConfig", palette);
            SetRef(boot, "targetCamera", cam);
            SetRef(boot, "keyLight", keyLight);

            // GameManager (wire everything)
            var gm = new GameObject("GameManager", typeof(GameManager)).GetComponent<GameManager>();
            SetRef(gm, "ui", ui);
            SetRef(gm, "conveyor", conveyor);
            SetRef(gm, "tileGrid", tileGrid);
            SetRef(gm, "audioManager", audio);
            SetRef(gm, "ballPool", pool);
            SetRef(gm, "ballRoot", ballRoot);
            SetRef(gm, "gameCamera", cam);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuild(ScenePath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            VerifyRefs(gm);
            Debug.Log("[MarbleSort] Build complete. Open " + ScenePath + " and press Play.");
        }

        // ---- prefab builders ---------------------------------------------

        private static void BuildTilePrefab()
        {
            var go = new GameObject("Tile", typeof(BoxCollider), typeof(TileView));
            SavePrefab(go, PrefabDir + "/Tile.prefab");
        }

        private static void BuildBallPrefab()
        {
            var go = new GameObject("Ball", typeof(MeshFilter), typeof(MeshRenderer), typeof(BallView));
            SavePrefab(go, PrefabDir + "/Ball.prefab");
        }

        private static void BuildBoxPrefab()
        {
            var go = new GameObject("DestinationBox", typeof(DestinationBoxView));
            SavePrefab(go, PrefabDir + "/DestinationBox.prefab");
        }

        private static void BuildConveyorPrefab()
        {
            var boxPrefab = AssetDatabase.LoadAssetAtPath<DestinationBoxView>(PrefabDir + "/DestinationBox.prefab");
            var go = new GameObject("Conveyor", typeof(ConveyorController));
            var cc = go.GetComponent<ConveyorController>();
            if (boxPrefab != null) SetRef(cc, "boxPrefab", boxPrefab);
            else Debug.LogError("[MarbleSort] DestinationBox.prefab not found when wiring Conveyor.");
            SavePrefab(go, PrefabDir + "/Conveyor.prefab");
        }

        // ---- helpers -----------------------------------------------------

        private static void SavePrefab(GameObject go, string path)
        {
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            if (prefab == null) Debug.LogError("[MarbleSort] Failed to save prefab " + path);
        }

        private static T EnsureConfig<T>(string path) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;
            var asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void SetRef(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            if (p == null) { Debug.LogError("[MarbleSort] No field '" + field + "' on " + target.GetType().Name); return; }
            p.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureFolder(string parent, string child)
        {
            if (!AssetDatabase.IsValidFolder(parent + "/" + child))
                AssetDatabase.CreateFolder(parent, child);
        }

        private static void AddSceneToBuild(string path)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            scenes.RemoveAll(s => s.path == path);
            scenes.Insert(0, new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void VerifyRefs(GameManager gm)
        {
            var so = new SerializedObject(gm);
            string[] fields = { "ui", "conveyor", "tileGrid", "audioManager", "ballPool", "ballRoot", "gameCamera" };
            foreach (var f in fields)
            {
                var p = so.FindProperty(f);
                if (p == null || p.objectReferenceValue == null)
                    Debug.LogError("[MarbleSort] GameManager." + f + " is NOT wired!");
            }
        }
    }
}
