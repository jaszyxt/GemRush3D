using UnityEngine;
using UnityEngine.Rendering.Universal;
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

        /// Index of the world currently built (-1 = none yet). Lets callers
        /// skip a redundant rebuild (the menu re-homes to world 0).
        public static int BuiltLevelIndex { get; private set; } = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            // Guard against a double boot (e.g. play mode without domain reload).
            if (Object.FindObjectOfType<GameManager>() != null) return;

            // The scene file ships Unity's default camera ("Main Camera",
            // with a space), AudioListener and "Directional Light". Left
            // alive they render a useless extra view under the game camera,
            // double-listen, and double-light the world — and URP silently
            // ignores a second directional light, dimming everything versus
            // Built-in. The game builds its own camera, listener and sun in
            // BuildWorld, so retire the scene defaults here.
            foreach (Camera sceneCam in Object.FindObjectsOfType<Camera>())
            {
                if (sceneCam.GetComponent<CameraFollow>() == null)
                    Object.Destroy(sceneCam.gameObject);
            }
            foreach (Light sceneLight in Object.FindObjectsOfType<Light>())
                Object.Destroy(sceneLight);

            // Comfort settings for phones/tablets; no effect on desktop.
            Screen.orientation = ScreenOrientation.LandscapeLeft;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Application.targetFrameRate = 60;
            // Bound catch-up steps after an app switch or frame hitch, so
            // physics never teleports Pip through a platform on resume.
            Time.maximumDeltaTime = 0.1f;

            // Desktop window memory (D8): reopen where the player left
            // the window; a no-op on mobile and in fullscreen.
            DesktopWindow.Restore();
            Application.quitting += DesktopWindow.Save;

            SetupRenderSettings();
            CreateManagers();
            SaveSystem.SnapshotVisit();
            // The menu is home: it opens over the start island (level 1),
            // where Pip's shelf keeps the story of everything achieved.
            BuildWorld(0);
        }

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
                ? UnityEngine.ShadowQuality.All : UnityEngine.ShadowQuality.Disable;

            GameObject sunGo = new GameObject("Sun");
            Light sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.96f, 0.88f);
            // The scene's stray default light used to double-light the
            // world under Built-in (~2.15 effective); with it retired and
            // URP honouring only one directional, compensate so levels
            // keep their established brightness.
            sun.intensity = 1.9f;
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
        }

        static void CreateManagers()
        {
            GameObject managers = new GameObject("~Managers");
            managers.AddComponent<AudioManager>();
            managers.AddComponent<VoiceOver>();
            managers.AddComponent<UIManager>();
            managers.AddComponent<GameManager>();
        }

        /// Creates (or re-creates) the playable world for the given level.
        /// Called at boot and on every level change / restart.
        public static void BuildWorld(int levelIndex)
        {
            BuiltLevelIndex = levelIndex;
            if (World != null) Object.Destroy(World);
            World = new GameObject("~World");
            // A new level means the old level's voice clips (loaded on
            // demand from Resources) are done; free them for real.
            if (VoiceOver.Instance != null) VoiceOver.Instance.Stop();
            Resources.UnloadUnusedAssets();

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
            // URP renders post-processing per camera; the additional-data
            // component is NOT auto-added when the camera is built from
            // code, so add it ourselves and switch post on.
            var urpCam = cameraGo.GetComponent<UniversalAdditionalCameraData>();
            if (urpCam == null)
                urpCam = cameraGo.AddComponent<UniversalAdditionalCameraData>();
            urpCam.renderPostProcessing = true;
            cameraGo.AddComponent<AudioListener>();
            CameraRig = cameraGo.AddComponent<CameraFollow>();
            CameraRig.target = Player.transform;
            CameraRig.SnapToTarget();

            Backdrop.Create(cameraGo.transform, level.SkyColor);

            // The mood layer: bloom, vignette, per-realm colour grading.
            // The volume survives level rebuilds; grading follows the level.
            PostFx.Create(level);

            // Post-befriending packs: Gloomfang tags along as weather support.
            // Unless you ARE him, in which case, one of you is enough.
            if (levelIndex >= 9 && !level.BonusFlight && Player != null)
                Gloomfang.Create(World.transform, Player.transform, level.MirrorSkies);

            // Ghost run: your best-time path, replayed as a translucent Pip
            // to race (only once the level has been cleared at least once).
            if (!level.BonusFlight &&
                SaveSystem.BestTime(levelIndex) >= 0f)
            {
                var path = GhostStore.Decode(
                    GhostStore.Load(level.Name));
                if (path != null && path.Count > 4)
                    GhostRunner.Create(World.transform, path);
            }

            // Cosmetic identity: the star-milestone trail rides Pip once
            // you have earned it (15/30/45 stars; see StarTrail).
            if (Player != null && !level.BonusFlight)
                StarTrail.Create(Player.transform);
        }

        /// Builds `levelIndex` only if it is not the world already standing.
        /// The menu's home-island path: repeated ShowMenu calls (boot, level
        /// exits) must not churn a rebuild every time — while PlayLevel
        /// always calls BuildWorld directly, because replaying a level
        /// needs fresh gems and state.
        public static void EnsureWorld(int levelIndex)
        {
            if (BuiltLevelIndex == levelIndex && World != null) return;
            BuildWorld(levelIndex);
        }
    }
}
