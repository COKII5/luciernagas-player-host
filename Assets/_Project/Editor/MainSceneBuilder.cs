using Fireflies.App;
using Fireflies.App.Input;
using Fireflies.Config;
using Fireflies.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Fireflies.EditorTools
{
    public static class MainSceneBuilder
    {
        private static readonly Color CameraBackground = new Color(0.04f, 0.06f, 0.12f);

        [MenuItem("Fireflies/Rebuild Main Scene")]
        public static void Build()
        {
            // Se cargan después de NewScene: abrir una escena descarga los assets sin referencias.
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            NetworkSettings networkSettings = SettingsAssetFactory.LoadOrCreate<NetworkSettings>(ProjectAssetPaths.NetworkSettings);
            GameplaySettings gameplaySettings = SettingsAssetFactory.LoadOrCreate<GameplaySettings>(ProjectAssetPaths.GameplaySettings);

            ArtImportSetup.ConfigureUiTextures();
            MaterialLibrary materials = new MaterialLibrary(ArtImportSetup.CreateSoftDot());
            ArtImportSetup.RemapModelMaterials(materials);
            PlayerPalette palette = UiBuilder.LoadOrCreatePalette();

            EnvironmentBuilder.ConfigureRenderSettings();
            AltarView[] altars = new EnvironmentBuilder(materials).Build(gameplaySettings);
            EnvironmentBuilder.BuildPostProcessing();

            SessionRunner runner = BuildAppRoot(networkSettings, gameplaySettings);
            Camera camera = BuildCamera();
            WorldPresenter world = BuildWorld(runner, palette, materials, altars);
            BuildCameraDirector(runner, world, camera);
            UiBuilder.Build(runner, palette, world, camera);

            EditorSceneManager.SaveScene(scene, ProjectAssetPaths.MainScene);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ProjectAssetPaths.MainScene, true) };
            AssetDatabase.SaveAssets();
        }

        private static SessionRunner BuildAppRoot(NetworkSettings networkSettings, GameplaySettings gameplaySettings)
        {
            GameObject root = new GameObject("App");
            KeyboardGamepadInput input = root.AddComponent<KeyboardGamepadInput>();

            SessionRunner runner = root.AddComponent<SessionRunner>();
            SerializedFieldWriter.Set(runner, "_networkSettings", networkSettings);
            SerializedFieldWriter.Set(runner, "_gameplaySettings", gameplaySettings);
            SerializedFieldWriter.Set(runner, "_playerInput", input);

            CommandLineLauncher launcher = root.AddComponent<CommandLineLauncher>();
            SerializedFieldWriter.Set(launcher, "_runner", runner);
            return runner;
        }

        private static Camera BuildCamera()
        {
            GameObject cameraObject = new GameObject("MainCamera") { tag = "MainCamera" };
            cameraObject.transform.SetPositionAndRotation(new Vector3(0f, 27f, -14.5f), Quaternion.Euler(60f, 0f, 0f));
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = CameraBackground;
            camera.fieldOfView = 52f;
            camera.farClipPlane = 140f;

            UniversalAdditionalCameraData cameraData = cameraObject.AddComponent<UniversalAdditionalCameraData>();
            cameraData.renderPostProcessing = true;
            cameraData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            return camera;
        }

        private static WorldPresenter BuildWorld(SessionRunner runner, PlayerPalette palette, MaterialLibrary materials, AltarView[] altars)
        {
            PrefabFactory prefabs = new PrefabFactory(materials);
            Transform normalOrb = prefabs.CreateCarriedOrb("Firefly", "CarriedOrb");
            Transform goldenOrb = prefabs.CreateCarriedOrb("FireflyGold", "CarriedOrbGold");

            GameObject root = new GameObject("World");
            WorldPresenter world = root.AddComponent<WorldPresenter>();
            SerializedFieldWriter.Set(world, "_runner", runner);
            SerializedFieldWriter.Set(world, "_palette", palette);
            SerializedFieldWriter.Set(world, "_playerPrefab", prefabs.CreatePlayer(normalOrb, goldenOrb));
            SerializedFieldWriter.Set(world, "_fireflyPrefab", prefabs.CreateFirefly("Firefly", "FireflyView", 1.4f, 2.6f));
            SerializedFieldWriter.Set(world, "_goldenFireflyPrefab", prefabs.CreateFirefly("FireflyGold", "FireflyGoldView", 3f, 4f));
            SerializedFieldWriter.SetArray(world, "_altars", altars);

            GameEventEffects effects = root.AddComponent<GameEventEffects>();
            SerializedFieldWriter.Set(effects, "_runner", runner);
            SerializedFieldWriter.Set(effects, "_world", world);
            SerializedFieldWriter.Set(effects, "_palette", palette);
            SerializedFieldWriter.Set(effects, "_depositBurst", EffectsFactory.CreateBurst("DepositBurst", root.transform, materials.Glow, 5f));
            SerializedFieldWriter.Set(effects, "_hitBurst", EffectsFactory.CreateBurst("HitBurst", root.transform, materials.Glow, 7f));
            return world;
        }

        private static void BuildCameraDirector(SessionRunner runner, WorldPresenter world, Camera camera)
        {
            CameraDirector director = new GameObject("CameraDirector").AddComponent<CameraDirector>();
            SerializedFieldWriter.Set(director, "_runner", runner);
            SerializedFieldWriter.Set(director, "_world", world);
            SerializedFieldWriter.Set(director, "_cameraTransform", camera.transform);
        }
    }
}
