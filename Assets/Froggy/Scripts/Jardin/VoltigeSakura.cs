using UnityEngine;

namespace Froggy.Jardin
{
    /// <summary>
    /// Les pétales de sakura en voltige (kit de Codex du 29 octobre 2026, Tools/blender/sakura_voltige.py), en deux
    /// versions à comparer depuis le menu de debug : les pétales au vent (particules sur toute la vitrine, avec les trois
    /// maillages du kit, poussées par VentJardin, qui se posent au sol) et la boucle précalculée de 8 s, posée au-dessus
    /// des cerisiers, qui ne suit ni le vent ni le sol. Les pétales qui tombent sous les cerisiers (Petales_Chute) restent
    /// tels quels. Purement visuel, local, non synchronisé en réseau.
    /// </summary>
    public class VoltigeSakura : MonoBehaviour
    {
        [SerializeField] private ParticleSystem auVent;
        [SerializeField] private GameObject boucles;
        [Tooltip("Surface que couvrent les pétales au vent (m²), posée par VitrineJardin.")]
        [SerializeField] private float surface = 1000f;
        [Tooltip("Pétales lâchés par seconde et par m².")]
        [SerializeField] private float densite = 0.02f;

        public bool AuVent
        {
            get => auVent != null && auVent.gameObject.activeSelf;
            set { if (auVent != null) auVent.gameObject.SetActive(value); }
        }

        public bool Boucle
        {
            get => boucles != null && boucles.activeSelf;
            set
            {
                // rallumer la remettrait à son départ : seulement quand ça change
                if (boucles == null || boucles.activeSelf == value) return;
                boucles.SetActive(value);
                if (value) Desynchroniser();
            }
        }

        public float Densite
        {
            get => densite;
            set { densite = Mathf.Max(0f, value); Appliquer(); }
        }

        /// <summary>Pétales lâchés par seconde sur toute la surface.</summary>
        public float Debit => densite * surface;

        private void Start()
        {
            Appliquer();
            if (Boucle) Desynchroniser();
        }

        private void Appliquer()
        {
            if (auVent == null) return;
            var em = auVent.emission;
            em.rateOverTime = Debit;
        }

        // deux boucles voisines ne tombent jamais en phase : départ et vitesse tirés de leur position (stable)
        private void Desynchroniser()
        {
            foreach (var a in boucles.GetComponentsInChildren<Animator>())
            {
                var p = a.transform.position;
                a.Play("Voltige", 0, Phase(p));
                a.speed = 0.9f + 0.2f * Phase(p + Vector3.right * 7.3f);
            }
        }

        /// <summary>Un nombre de [0, 1[ tiré de la position, toujours le même pour la même place.</summary>
        public static float Phase(Vector3 p) => (ShishiOdoshi.HachagePosition(p) & 0xFFFF) / 65536f;
    }
}
