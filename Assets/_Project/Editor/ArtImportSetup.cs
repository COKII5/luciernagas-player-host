using System.IO;
using UnityEditor;
using UnityEngine;

namespace Fireflies.EditorTools
{
    public static class ArtImportSetup
    {
        private const string UiFolder = "Assets/_Project/Art/UI";
        private const string ModelsFolder = "Assets/_Project/Art/Models";
        private const string SoftDotPath = "Assets/_Project/Art/Textures/SoftDot.png";
        private const int SoftDotSize = 64;

        public static void ConfigureUiTextures()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { UiFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
        }

        public static void RemapModelMaterials(MaterialLibrary materials)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { ModelsFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ModelImporter importer = (ModelImporter)AssetImporter.GetAtPath(path);
                importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
                importer.importAnimation = false;
                importer.importCameras = false;
                importer.importLights = false;
                foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
                {
                    if (asset is Material embedded && materials.Get(embedded.name) != null)
                    {
                        importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), embedded.name), materials.Get(embedded.name));
                    }
                }

                importer.SaveAndReimport();
            }
        }

        // Punto suave para las partículas, generado aquí para no depender de texturas externas.
        public static Texture2D CreateSoftDot()
        {
            Texture2D existing = AssetDatabase.LoadAssetAtPath<Texture2D>(SoftDotPath);
            if (existing != null)
            {
                return existing;
            }

            Texture2D texture = new Texture2D(SoftDotSize, SoftDotSize, TextureFormat.RGBA32, false);
            float center = (SoftDotSize - 1) * 0.5f;
            for (int y = 0; y < SoftDotSize; y++)
            {
                for (int x = 0; x < SoftDotSize; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center)) / center;
                    float alpha = Mathf.Pow(Mathf.Clamp01(1f - distance), 2f);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            AssetFolders.Ensure(Path.GetDirectoryName(SoftDotPath).Replace('\\', '/'));
            File.WriteAllBytes(SoftDotPath, texture.EncodeToPNG());
            AssetDatabase.ImportAsset(SoftDotPath);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(SoftDotPath);
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(SoftDotPath);
        }
    }
}
