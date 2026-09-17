using UnityEngine;
using UnityEngine.Rendering;

namespace GemRush
{
    /// Entry point. The project's scene file is intentionally empty: everything
    /// here (lighting, managers, level, player, camera, UI, audio) is built from
    /// code when Play starts, so the game works with zero manual editor setup.
    public static class GameBootstrap
    {
        public static GameObject World { get; private set; }
        public static PlayerController Player { get; private set; }
        public static CameraFollow CameraRig { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            // Guard against a double boot (e.g. play mode without domain reload).
            if (Object.FindObjectOfType<GameManager>() != null) return;

            // Comfort settings for phones/tablets; no effect on desktop.
            Screen.orientation = ScreenOrientation.LandscapeLeft;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Application.targetFrameRate = 60;
            // Bound catch-up steps after an app switch or frame hitch, so
            // physics never teleports Pip through a platform on resume.
            Time.maximumDeltaTime = 0.1f;

            SetupRenderSettings();
            CreateManagers();
            SaveSystem.SnapshotVisit();
            BuildWorld(Mathf.Clamp(SaveSystem.UnlockedLevel,
                0, LevelLibrary.Levels.Length - 1));
        }

        // NOTE: the post-processing experiment (bloom/grading/vignette via
        // com.unity.postprocessing) is parked. Its Init() requires the
        // package's PostProcessResources asset, which cannot reach a player
        // build without shipping imported assets — against this project's
        // zero-asset rule. The package stays embedded in Packages/ (patched
        // for Unity 6) in case the approach is revisited.

        static void SetupRenderSettings()
        {
            // Solid-color sky + matched fog: guaranteed identical on every
            // device (the procedural skybox rendered dark on some GPUs).
            Color sky = new Color(0.47f, 0.72f, 0.98f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogDensity = 0.008f;
            RenderSettings.fogColor = sky;

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.55f, 0.65f, 0.85f);
            RenderSettings.ambientEquatorColor = new Color(0.45f, 0.50f, 0.60f);
            RenderSettings.ambientGroundColor = new Color(0.35f, 0.35f, 0.35f);

            QualitySettings.antiAliasing = 4;
            QualitySettings.shadows = SaveSystem.ShadowsOn
                ? ShadowQuality.All : ShadowQuality.Disable;

            GameObject sunGo = new GameObject("Sun");
            Light sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.96f, 0.88f);
            sun.intensity = 1.15f;
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
        }

        static void CreateManagers()
        {
            GameObject managers = new GameObject("~Managers");
            managers.AddComponent<AudioManager>();
            managers.AddComponent<UIManager>();
            managers.AddComponent<GameManager>();
        }

        /// Creates (or re-creates) the playable world for the given level.
        /// Called at boot and on every level change / restart.
        public static void BuildWorld(int levelIndex)
        {
            if (World != null) Object.Destroy(World);
            World = new GameObject("~World");

            levelIndex = Mathf.Clamp(levelIndex, 0, LevelLibrary.Levels.Length - 1);
            LevelDefinition level = LevelLibrary.Levels[levelIndex];
            DailyGem.ActiveLevelIndex = levelIndex;
            LevelBuilder.Build(level, World.transform);

            Player = PlayerController.Create(World.transform,
                GameManager.Instance.SpawnPoint, level.BonusFlight);

            GameObject cameraGo = new GameObject("MainCamera");
            cameraGo.tag = "MainCamera";
            cameraGo.transform.SetParent(World.transform);
            Camera cam = cameraGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = level.SkyColor;
            cam.farClipPlane = 600f;
            cameraGo.AddComponent<AudioListener>();
            CameraRig = cameraGo.AddComponent<CameraFollow>();
            CameraRig.target = Player.transform;
            CameraRig.SnapToTarget();

            Backdrop.Create(cameraGo.transform, level.SkyColor);

            // Post-befriending packs: Gloomfang tags along as weather support.
            // Unless you ARE him, in which case, one of you is enough.
            if (levelIndex >= 9 && !level.BonusFlight && Player != null)
                Gloomfang.Create(World.transform, Player.transform);
        }
    }
}
