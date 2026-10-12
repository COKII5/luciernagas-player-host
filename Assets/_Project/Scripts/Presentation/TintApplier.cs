using UnityEngine;

namespace Fireflies.Presentation
{
    // Solo tiñe los materiales llamados "PlayerTint" (acordado con los modelos de Blender). Con
    // MaterialPropertyBlock todos los jugadores comparten el mismo material.
    public static class TintApplier
    {
        public const string TintMaterialName = "PlayerTint";

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        public static void Apply(Renderer[] renderers, Color color, float emissionIntensity)
        {
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            foreach (Renderer target in renderers)
            {
                Material[] materials = target.sharedMaterials;
                for (int index = 0; index < materials.Length; index++)
                {
                    if (materials[index] == null || !materials[index].name.StartsWith(TintMaterialName))
                    {
                        continue;
                    }

                    target.GetPropertyBlock(block, index);
                    block.SetColor(BaseColorId, color);
                    block.SetColor(EmissionColorId, color * emissionIntensity);
                    target.SetPropertyBlock(block, index);
                }
            }
        }
    }
}
