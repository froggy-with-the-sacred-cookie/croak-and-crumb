using UnityEngine;

namespace Froggy.Jardin
{
    /// <summary>
    /// Pousse les particules (pétales) dans le sens du vent de <see cref="VentJardin"/>, et donne à chaque instance sa
    /// propre graine, tirée de sa position, pour que deux arbres ne lâchent pas leurs pétales en phase.
    /// </summary>
    [RequireComponent(typeof(ParticleSystem))]
    public class VentSurParticules : MonoBehaviour
    {
        [Tooltip("Force du vent sur une particule, en m/s² à force 1.")]
        [SerializeField] private float poussee = 1.2f;

        private ParticleSystem particules;

        private void Awake()
        {
            particules = GetComponent<ParticleSystem>();
            particules.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particules.useAutoRandomSeed = false;
            particules.randomSeed = (uint)ShishiOdoshi.HachagePosition(transform.position);
            particules.Play();
        }

        private void Update()
        {
            var f = particules.forceOverLifetime;
            f.enabled = true;
            f.space = ParticleSystemSimulationSpace.World;
            f.x = VentJardin.Direction.x * VentJardin.Force * poussee;
            f.y = 0f;
            f.z = VentJardin.Direction.y * VentJardin.Force * poussee;
        }
    }
}
