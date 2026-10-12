using Fireflies.Presentation;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Fireflies.EditorTools
{
    public sealed class PrefabFactory
    {
        private const string ModelsFolder = "Assets/_Project/Art/Models";
        private const string PrefabsFolder = "Assets/_Project/Prefabs";
        private const float CarriedOrbScale = 0.6f;
        private const float PlayerVisualScale = 1.35f;
        private const float FireflyVisualScale = 2f;

        private readonly MaterialLibrary _materials;

        public PrefabFactory(MaterialLibrary materials)
        {
            _materials = materials;
            AssetFolders.Ensure(PrefabsFolder);
        }

        public static GameObject LoadModel(string modelName)
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelsFolder}/{modelName}.fbx");
        }

        public FireflyView CreateFirefly(string modelName, string prefabName, float lightIntensity, float lightRange)
        {
            GameObject root = new GameObject(prefabName);
            Transform visual = new GameObject("Visual").transform;
            visual.SetParent(root.transform, false);
            Instantiate(modelName, visual).transform.localScale = Vector3.one * FireflyVisualScale;
            AddPointLight(visual, new Color(1f, 0.85f, 0.45f), lightIntensity, lightRange, Vector3.up * 0.3f);

            FireflyView view = root.AddComponent<FireflyView>();
            SerializedFieldWriter.Set(view, "_visual", visual);
            return Save(root).GetComponent<FireflyView>();
        }

        public Transform CreateCarriedOrb(string modelName, string prefabName)
        {
            GameObject root = Instantiate(modelName, null);
            root.name = prefabName;
            root.transform.localScale = Vector3.one * CarriedOrbScale;
            return Save(root).transform;
        }

        public PlayerView CreatePlayer(Transform normalOrb, Transform goldenOrb)
        {
            GameObject root = new GameObject("LanternKeeperView");
            Transform body = new GameObject("Body").transform;
            body.SetParent(root.transform, false);
            body.localScale = Vector3.one * PlayerVisualScale;
            GameObject model = Instantiate("LanternKeeper", body);
            Transform lantern = model.transform.Find("Lantern");
            Light lanternLight = AddPointLight(lantern, Color.white, 4f, 6f, new Vector3(0f, -0.15f, 0f));

            CarriedFirefliesOrbit orbit = new GameObject("CarriedOrbit").AddComponent<CarriedFirefliesOrbit>();
            orbit.transform.SetParent(root.transform, false);
            SerializedFieldWriter.Set(orbit, "_normalOrbPrefab", normalOrb);
            SerializedFieldWriter.Set(orbit, "_goldenOrbPrefab", goldenOrb);

            TrailRenderer trail = root.AddComponent<TrailRenderer>();
            trail.time = 0.25f;
            trail.widthCurve = AnimationCurve.Linear(0f, 0.7f, 1f, 0f);
            trail.sharedMaterial = _materials.Glow;
            trail.shadowCastingMode = ShadowCastingMode.Off;
            trail.emitting = false;

            PlayerView view = root.AddComponent<PlayerView>();
            SerializedFieldWriter.SetArray(view, "_tintedRenderers", model.GetComponentsInChildren<Renderer>());
            SerializedFieldWriter.Set(view, "_body", body);
            SerializedFieldWriter.Set(view, "_lantern", lantern);
            SerializedFieldWriter.Set(view, "_lanternLight", lanternLight);
            SerializedFieldWriter.Set(view, "_carriedOrbit", orbit);
            SerializedFieldWriter.Set(view, "_dashTrail", trail);
            SerializedFieldWriter.Set(view, "_localPlayerMarker", CreateLocalMarker(root.transform));
            return Save(root).GetComponent<PlayerView>();
        }

        public static Light AddPointLight(Transform parent, Color color, float intensity, float range, Vector3 localPosition)
        {
            Light light = new GameObject("Light").AddComponent<Light>();
            light.transform.SetParent(parent, false);
            light.transform.localPosition = localPosition;
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.None;
            return light;
        }

        private GameObject CreateLocalMarker(Transform parent)
        {
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = "LocalPlayerMarker";
            Object.DestroyImmediate(marker.GetComponent<Collider>());
            marker.transform.SetParent(parent, false);
            marker.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            marker.transform.localScale = new Vector3(1.5f, 0.01f, 1.5f);
            Renderer renderer = marker.GetComponent<Renderer>();
            renderer.sharedMaterial = _materials.Marker;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            return marker;
        }

        private static GameObject Instantiate(string modelName, Transform parent)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(LoadModel(modelName), parent);
            PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            return instance;
        }

        private static GameObject Save(GameObject root)
        {
            string path = $"{PrefabsFolder}/{root.name}.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }
    }
}
