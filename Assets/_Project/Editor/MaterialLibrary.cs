using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Fireflies.EditorTools
{
    // Materiales URP que reemplazan a los de Blender. Los nombres coinciden con los slots de los FBX.
    public sealed class MaterialLibrary
    {
        private const string Folder = "Assets/_Project/Art/Materials";
        private const string LitShader = "Universal Render Pipeline/Lit";
        private const string UnlitShader = "Universal Render Pipeline/Unlit";
        private const string ParticleShader = "Universal Render Pipeline/Particles/Unlit";

        private readonly Dictionary<string, Material> _byName = new();

        public MaterialLibrary(Texture2D softDot)
        {
            AddLit("PlayerTint", Hex("#D9D9D9"), 0.35f, Color.black, 0f);
            AddLit("Skin", Hex("#E8B48A"), 0.3f, Color.black, 0f);
            AddLit("Metal", Hex("#1B2440"), 0.55f, Color.black, 0f);
            AddLit("LanternGlow", Hex("#F9F871"), 0.6f, Hex("#F9F871"), 3.5f);
            AddLit("GoldGlow", Hex("#FFD166"), 0.7f, Hex("#FFC23A"), 5f);
            AddLit("Stone", Hex("#6B7280"), 0.15f, Color.black, 0f);
            AddLit("Wood", Hex("#5A4636"), 0.1f, Color.black, 0f);
            AddLit("Leaf", Hex("#3E7C59"), 0.1f, Color.black, 0f);
            AddLit("Ground", Hex("#33604A"), 0.05f, Color.black, 0f);
            AddLit("OuterGround", Hex("#1E3A2B"), 0.05f, Color.black, 0f);
            AddLit("Mushroom", Hex("#B76BE0"), 0.4f, Hex("#B76BE0"), 1.2f);
            Glow = AddParticle("SoftGlow", softDot);
            Marker = AddTransparentUnlit("LocalMarker", new Color(1f, 0.85f, 0.45f, 0.35f));
        }

        public Material Glow { get; }
        public Material Marker { get; }

        public Material Get(string name)
        {
            return _byName.TryGetValue(name, out Material material) ? material : null;
        }

        private void AddLit(string name, Color baseColor, float smoothness, Color emission, float emissionIntensity)
        {
            Material material = LoadOrCreate(name, LitShader);
            material.SetColor("_BaseColor", baseColor);
            material.SetFloat("_Smoothness", smoothness);
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", emission * emissionIntensity);
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            _byName[name] = material;
        }

        private Material AddParticle(string name, Texture2D texture)
        {
            Material material = LoadOrCreate(name, ParticleShader);
            material.SetTexture("_BaseMap", texture);
            material.SetColor("_BaseColor", Color.white);
            ConfigureTransparent(material, BlendMode.One);
            material.SetFloat("_Blend", 2f);
            _byName[name] = material;
            return material;
        }

        private Material AddTransparentUnlit(string name, Color color)
        {
            Material material = LoadOrCreate(name, UnlitShader);
            material.SetColor("_BaseColor", color);
            ConfigureTransparent(material, BlendMode.OneMinusSrcAlpha);
            _byName[name] = material;
            return material;
        }

        private static void ConfigureTransparent(Material material, BlendMode destinationBlend)
        {
            material.SetFloat("_Surface", 1f);
            material.SetOverrideTag("RenderType", "Transparent");
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)destinationBlend);
            material.SetFloat("_ZWrite", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)RenderQueue.Transparent;
        }

        private static Material LoadOrCreate(string name, string shaderName)
        {
            string path = $"{Folder}/{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
            {
                return material;
            }

            AssetFolders.Ensure(Folder);
            material = new Material(Shader.Find(shaderName)) { name = name };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static Color Hex(string html)
        {
            ColorUtility.TryParseHtmlString(html, out Color color);
            return color;
        }
    }
}
