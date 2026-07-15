using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using MarbleSort.Config;

namespace MarbleSort.Systems
{
    /// <summary>
    /// Applies config assets and frames the 3D scene before anything else runs: perspective camera,
    /// a key directional light, ambient fill, and the URP 3D renderer. Does NOT construct gameplay
    /// objects — those are authored in the scene / prefabs. Author on a scene object.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private GameConfig gameConfig;
        [SerializeField] private PaletteConfig paletteConfig;
        [SerializeField] private Camera targetCamera;
        [SerializeField] private Light keyLight;

        private void Awake()
        {
            GameConfig.SetActive(gameConfig);
            PaletteConfig.SetActive(paletteConfig);

            Application.targetFrameRate = Tune.TargetFps;

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = Palette.AmbientColor;

            EnsureLight();
            if (targetCamera == null) targetCamera = Camera.main;
            if (targetCamera != null) SetupCamera(targetCamera);
        }

        private void SetupCamera(Camera cam)
        {
            cam.orthographic = false;
            cam.fieldOfView = Tune.CameraFov;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 120f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Palette.BackgroundBottom;

            float half = Tune.CameraSize;
            float fovRad = Tune.CameraFov * Mathf.Deg2Rad;
            float dist = half / Mathf.Tan(fovRad * 0.5f);
            float pitch = Tune.CameraPitch;
            float pr = pitch * Mathf.Deg2Rad;

            Vector3 target = new Vector3(Tune.CameraCenter.x, Tune.CameraCenter.y, 0f);
            cam.transform.position = target + new Vector3(0f, dist * Mathf.Sin(pr), -dist * Mathf.Cos(pr));
            cam.transform.rotation = Quaternion.Euler(pitch, 0f, 0f);

            // opt this camera into the URP 3D renderer (index 1 = UniversalRenderer)
            var data = cam.GetUniversalAdditionalCameraData();
            if (data != null) { try { data.SetRenderer(1); } catch { } }
        }

        private void EnsureLight()
        {
            if (keyLight == null)
            {
                foreach (var l in FindObjectsOfType<Light>())
                    if (l.type == LightType.Directional) { keyLight = l; break; }
            }
            if (keyLight == null)
            {
                keyLight = new GameObject("KeyLight", typeof(Light)).GetComponent<Light>();
                keyLight.type = LightType.Directional;
            }
            keyLight.color = Palette.LightColor;
            keyLight.intensity = 1.15f;
            keyLight.shadows = LightShadows.Soft;
            keyLight.transform.rotation = Quaternion.Euler(52f, -28f, 0f);
        }
    }
}
