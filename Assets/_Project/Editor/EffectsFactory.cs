using UnityEngine;

namespace Fireflies.EditorTools
{
    public static class EffectsFactory
    {
        public static ParticleSystem CreateBurst(string name, Transform parent, Material material, float maxSpeed)
        {
            ParticleSystem burst = new GameObject(name).AddComponent<ParticleSystem>();
            burst.transform.SetParent(parent, false);
            burst.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = burst.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 1f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.1f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(maxSpeed * 0.4f, maxSpeed);
            main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.28f);
            main.gravityModifier = -0.15f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 400;

            ParticleSystem.EmissionModule emission = burst.emission;
            emission.rateOverTime = 0f;

            ParticleSystem.ShapeModule shape = burst.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.4f;

            ParticleSystem.ColorOverLifetimeModule fade = burst.colorOverLifetime;
            fade.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            fade.color = gradient;

            burst.GetComponent<ParticleSystemRenderer>().sharedMaterial = material;
            return burst;
        }
    }
}
