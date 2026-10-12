using Fireflies.Config;
using Fireflies.Game.Simulation;
using Fireflies.Presentation;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Fireflies.EditorTools
{
    public sealed class EnvironmentBuilder
    {
        private const int RandomSeed = 20261012;
        private const string VolumeProfilePath = "Assets/_Project/Settings/NightVolumeProfile.asset";
        private const float GroundScale = 1.15f;
        private const float TreeInnerRadius = 18f;
        private const float TreeOuterRadius = 30f;
        private const int TreeCount = 70;
        private const int DecorCount = 46;
        private const float DecorInnerRadius = 15.6f;
        private const float DecorOuterRadius = 19f;
        private const float CameraSideHalfAngle = 45f;
        private const float CameraSideAngle = 270f;

        private static readonly string[] Trees = { "TreePine", "TreePine", "TreeRound", "TreeDead" };
        private static readonly string[] Decor = { "RockSmall", "RockLarge", "Bush", "MushroomCluster", "Stump", "Bush", "MushroomCluster" };

        private readonly MaterialLibrary _materials;
        private readonly System.Random _random = new(RandomSeed);

        public EnvironmentBuilder(MaterialLibrary materials)
        {
            _materials = materials;
        }

        public AltarView[] Build(GameplaySettings settings)
        {
            Transform root = new GameObject("Environment").transform;
            BuildGround(root);
            BuildTrees(root);
            BuildDecor(root);
            BuildAmbientMotes(root);
            BuildLighting(root);
            return BuildAltars(root, new ArenaLayout(settings));
        }

        public static void ConfigureRenderSettings()
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.34f, 0.4f, 0.62f);
            RenderSettings.ambientEquatorColor = new Color(0.22f, 0.27f, 0.4f);
            RenderSettings.ambientGroundColor = new Color(0.1f, 0.12f, 0.18f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogColor = new Color(0.05f, 0.07f, 0.13f);
            RenderSettings.fogDensity = 0.011f;
            RenderSettings.skybox = null;
        }

        public static Volume BuildPostProcessing()
        {
            VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, VolumeProfilePath);
                AddOverride<Bloom>(profile, bloom =>
                {
                    bloom.threshold.Override(0.85f);
                    bloom.intensity.Override(1.6f);
                    bloom.scatter.Override(0.72f);
                });
                AddOverride<Tonemapping>(profile, tonemapping => tonemapping.mode.Override(TonemappingMode.ACES));
                AddOverride<Vignette>(profile, vignette => vignette.intensity.Override(0.32f));
                AddOverride<ColorAdjustments>(profile, adjustments =>
                {
                    adjustments.postExposure.Override(0.6f);
                    adjustments.saturation.Override(12f);
                });
                AssetDatabase.SaveAssets();
            }

            Volume volume = new GameObject("PostProcessVolume").AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = profile;
            return volume;
        }

        private static void AddOverride<T>(VolumeProfile profile, System.Action<T> configure) where T : VolumeComponent
        {
            T component = profile.Add<T>(true);
            component.name = typeof(T).Name;
            configure(component);
            AssetDatabase.AddObjectToAsset(component, profile);
        }

        private void BuildGround(Transform root)
        {
            GameObject clearing = Place("ClearingGround", root, Vector3.zero, 0f, new Vector3(GroundScale, 1f, GroundScale));
            clearing.name = "Clearing";

            GameObject outer = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            outer.name = "OuterGround";
            Object.DestroyImmediate(outer.GetComponent<Collider>());
            outer.transform.SetParent(root, false);
            outer.transform.localPosition = new Vector3(0f, -0.06f, 0f);
            outer.transform.localScale = new Vector3(90f, 0.05f, 90f);
            outer.GetComponent<Renderer>().sharedMaterial = _materials.Get("OuterGround");
        }

        private void BuildTrees(Transform root)
        {
            Transform parent = Group("Trees", root);
            for (int i = 0; i < TreeCount; i++)
            {
                float angle = Range(0f, 360f);
                if (IsInCameraSide(angle))
                {
                    continue;
                }

                float radius = Range(TreeInnerRadius, TreeOuterRadius);
                Place(Trees[_random.Next(Trees.Length)], parent, Polar(angle, radius), Range(0f, 360f), Vector3.one * Range(0.8f, 1.35f));
            }
        }

        private void BuildDecor(Transform root)
        {
            Transform parent = Group("Decor", root);
            for (int i = 0; i < DecorCount; i++)
            {
                float angle = Range(0f, 360f);
                float radius = Range(DecorInnerRadius, DecorOuterRadius);
                Place(Decor[_random.Next(Decor.Length)], parent, Polar(angle, radius), Range(0f, 360f), Vector3.one * Range(0.8f, 1.4f));
            }
        }

        private void BuildAmbientMotes(Transform root)
        {
            ParticleSystem motes = new GameObject("AmbientMotes").AddComponent<ParticleSystem>();
            motes.transform.SetParent(root, false);
            motes.transform.localPosition = new Vector3(0f, 1.5f, 0f);
            motes.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);

            ParticleSystem.MainModule main = motes.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(5f, 9f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.25f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.2f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.95f, 0.5f), new Color(0.7f, 1f, 0.6f));
            main.maxParticles = 180;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            ParticleSystem.EmissionModule emission = motes.emission;
            emission.rateOverTime = 22f;

            ParticleSystem.ShapeModule shape = motes.shape;
            shape.shapeType = ParticleSystemShapeType.Donut;
            shape.radius = 20f;
            shape.donutRadius = 5f;

            ParticleSystem.NoiseModule noise = motes.noise;
            noise.enabled = true;
            noise.strength = 0.4f;
            noise.frequency = 0.3f;

            ParticleSystem.ColorOverLifetimeModule fade = motes.colorOverLifetime;
            fade.enabled = true;
            fade.color = FadeInOut();

            motes.GetComponent<ParticleSystemRenderer>().sharedMaterial = _materials.Glow;
        }

        private static void BuildLighting(Transform root)
        {
            Light moon = new GameObject("Moonlight").AddComponent<Light>();
            moon.transform.SetParent(root, false);
            moon.transform.rotation = Quaternion.Euler(52f, -35f, 0f);
            moon.type = LightType.Directional;
            moon.color = new Color(0.66f, 0.74f, 1f);
            moon.intensity = 1.1f;
            moon.shadows = LightShadows.Soft;
            moon.shadowStrength = 0.6f;
        }

        private AltarView[] BuildAltars(Transform root, ArenaLayout layout)
        {
            Transform parent = Group("Altars", root);
            AltarView[] altars = new AltarView[ArenaLayout.SlotCount];
            for (int slot = 0; slot < altars.Length; slot++)
            {
                Vector2 position = layout.GetAltarPosition(slot);
                Vector2 facing = layout.GetSpawnFacing(slot);
                float yaw = Mathf.Atan2(facing.x, facing.y) * Mathf.Rad2Deg;
                GameObject altar = Place("Altar", parent, new Vector3(position.x, 0f, position.y), yaw, Vector3.one);
                altar.name = $"Altar_{slot}";
                Light light = PrefabFactory.AddPointLight(altar.transform, Color.white, 3f, 7f, new Vector3(0f, 1.6f, 0f));

                AltarView view = altar.AddComponent<AltarView>();
                SerializedFieldWriter.SetArray(view, "_tintedRenderers", altar.GetComponentsInChildren<Renderer>());
                SerializedFieldWriter.Set(view, "_altarLight", light);
                altars[slot] = view;
            }

            return altars;
        }

        private static GameObject Place(string modelName, Transform parent, Vector3 position, float yaw, Vector3 scale)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(PrefabFactory.LoadModel(modelName), parent);
            instance.transform.localPosition = position;
            instance.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            instance.transform.localScale = scale;
            return instance;
        }

        private static Transform Group(string name, Transform parent)
        {
            Transform group = new GameObject(name).transform;
            group.SetParent(parent, false);
            return group;
        }

        private static bool IsInCameraSide(float angle)
        {
            return Mathf.Abs(Mathf.DeltaAngle(angle, CameraSideAngle)) < CameraSideHalfAngle;
        }

        private static Vector3 Polar(float angleDegrees, float radius)
        {
            float radians = angleDegrees * Mathf.Deg2Rad;
            return new Vector3(Mathf.Cos(radians) * radius, 0f, Mathf.Sin(radians) * radius);
        }

        private static Gradient FadeInOut()
        {
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.3f), new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) });
            return gradient;
        }

        private float Range(float min, float max)
        {
            return Mathf.Lerp(min, max, (float)_random.NextDouble());
        }
    }
}
