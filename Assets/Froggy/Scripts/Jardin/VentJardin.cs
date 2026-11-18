using UnityEngine;

namespace Froggy.Jardin
{
    /// <summary>
    /// La seule source du vent (skill tech-art) : direction, force et horloge, posées chaque image dans la variable
    /// globale de shader <c>_FroggyVent</c> (xy : direction sur le sol, z : force 0-1, w : horloge) et lisibles en C#.
    /// Tout ce qui bouge au vent la lit (eau, nénuphars, nuages, pétales) : jamais de direction en dur.
    /// L'horloge du vent est la sienne : elle avance plus vite par grand vent. Purement visuel, non synchronisé en réseau.
    /// </summary>
    [ExecuteAlways]
    public class VentJardin : MonoBehaviour
    {
        [Tooltip("D'où souffle le vent, en degrés autour de la verticale (0 : vers +z).")]
        [Range(0f, 360f)] [SerializeField] private float direction = 60f;

        [Range(0f, 1f)] [SerializeField] private float force = 0.35f;

        [Tooltip("Part de la force qui varie en rafales.")]
        [Range(0f, 1f)] [SerializeField] private float rafales = 0.35f;

        private static readonly int IdVent = Shader.PropertyToID("_FroggyVent");
        private float horloge;

        public static Vector2 Direction { get; private set; } = Vector2.right;
        public static float Force { get; private set; }

        private void OnEnable() => Poser(0f);

        private void Update() => Poser(Application.isPlaying ? Time.deltaTime : 0f);

        private void Poser(float dt)
        {
            float t = Application.isPlaying ? Time.time : 0f;
            // rafales et petites sautes de direction, lentes et bornées (bruit de Perlin, pas d'à-coups)
            float f = force * (1f + rafales * (Mathf.PerlinNoise(t * 0.15f, 0.3f) * 2f - 1f));
            float a = (direction + (Mathf.PerlinNoise(0.7f, t * 0.05f) * 2f - 1f) * 15f) * Mathf.Deg2Rad;
            Force = Mathf.Clamp01(f);
            Direction = new Vector2(Mathf.Sin(a), Mathf.Cos(a));
            horloge += dt * (0.4f + Force);
            Shader.SetGlobalVector(IdVent, new Vector4(Direction.x, Direction.y, Force, horloge));
        }
    }
}
