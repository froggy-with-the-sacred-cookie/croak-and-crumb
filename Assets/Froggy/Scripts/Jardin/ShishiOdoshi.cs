using System;
using UnityEngine;
using UnityEngine.Events;

namespace Froggy.Jardin
{
    /// <summary>
    /// Le bambou basculant du jardin. Un filet d'eau remplit le tube, qui reste sur sa pierre, frémit, puis bascule
    /// d'un coup (pivot décentré), se vide dans l'étang, retombe en accélérant et claque sur la pierre avec un petit rebond.
    /// Un vrai cycle dure de 1 à 4 minutes (débit et volume du tube) ; il est resserré ici pour qu'on le voie en jeu.
    /// Chaque instance a son rythme, tiré d'un hachage stable de sa position, et deux remplissages de suite ne durent
    /// jamais pareil (skill rendu-vivant). Purement visuel et local : il ne pilote aucune règle, rien n'est synchronisé.
    /// <see cref="claquement"/> annonce le choc (pour brancher le son, skill son-integration).
    /// </summary>
    public class ShishiOdoshi : MonoBehaviour
    {
        [SerializeField] private Transform bascule;
        [Tooltip("Particules qui sortent de la bouche quand le tube se vide.")]
        [SerializeField] private ParticleSystem verse;

        [Header("Rythme")]
        [Tooltip("Durée du remplissage, en secondes (tirée à chaque cycle entre ces bornes).")]
        [SerializeField] private Vector2 remplissage = new Vector2(12f, 18f);
        [SerializeField] private float angleBascule = 52f;
        [SerializeField] private float dureeBascule = 0.45f;
        [SerializeField] private float dureeVidage = 0.6f;
        [SerializeField] private float dureeRetour = 0.22f;
        [Tooltip("Hauteur du rebond sur la pierre, en degrés.")]
        [SerializeField] private float rebond = 4f;
        [SerializeField] private float dureeRebond = 0.2f;
        [Tooltip("0 : graine tirée de la position (chaque bambou a son rythme).")]
        [SerializeField] private int graine;

        [Tooltip("Le tube frappe la pierre : brancher ici le son du claquement.")]
        public UnityEvent claquement = new UnityEvent();

        private enum Phase { Remplissage, Bascule, Vidage, Retour, Rebond }

        private Quaternion repos;
        private System.Random alea;
        private Phase phase;
        private float duree, temps, dernierRemplissage, rebondCycle, versees;
        private bool pret;

        public float Angle { get; private set; }

        private void Awake() => Preparer();

        /// <summary>Trouve le tube et tire la première durée ; appelable hors du mode Play (tests, contrôles).</summary>
        public void Preparer()
        {
            if (pret) return;
            if (bascule == null) bascule = Trouver(transform, "ShishiOdoshi_Bascule");
            repos = bascule.localRotation;
            alea = new System.Random(graine != 0 ? graine : HachagePosition(transform.position));
            pret = true;
            Commencer(Phase.Remplissage);
        }

        private void Update()
        {
            temps += Time.deltaTime;
            float k = Mathf.Clamp01(temps / duree);
            switch (phase)
            {
                case Phase.Remplissage:   // reste sur la pierre ; à la fin, frémit et se soulève de quelques degrés
                    PoserAngle(k > 0.85f ? Mathf.SmoothStep(0f, 4f, (k - 0.85f) / 0.15f) : 0f);
                    if (k >= 1f) Commencer(Phase.Bascule);
                    break;
                case Phase.Bascule:       // le centre de gravité passe le pivot : la chute accélère
                    PoserAngle(Mathf.Lerp(4f, angleBascule, k * k));
                    if (k >= 1f) Commencer(Phase.Vidage);
                    break;
                case Phase.Vidage:        // l'eau sort, de moins en moins
                    PoserAngle(angleBascule + Mathf.Sin(k * Mathf.PI) * 2f);
                    versees += Time.deltaTime * 90f * (1f - k);
                    if (verse != null && versees >= 1f) { verse.Emit((int)versees); versees -= (int)versees; }
                    if (k >= 1f) Commencer(Phase.Retour);
                    break;
                case Phase.Retour:        // le bout lourd retombe en accélérant
                    PoserAngle(Mathf.Lerp(angleBascule, 0f, k * k));
                    if (k >= 1f) { claquement.Invoke(); Commencer(Phase.Rebond); }
                    break;
                case Phase.Rebond:        // petit rebond amorti
                    PoserAngle(Mathf.Sin(k * Mathf.PI) * rebondCycle * (1f - k));
                    if (k >= 1f) Commencer(Phase.Remplissage);
                    break;
            }
        }

        private void Commencer(Phase suivante)
        {
            phase = suivante;
            temps = 0f;
            switch (phase)
            {
                case Phase.Remplissage:
                    duree = dernierRemplissage = TirerRemplissage(alea, remplissage, dernierRemplissage);
                    break;
                case Phase.Bascule: duree = dureeBascule; break;
                case Phase.Vidage: duree = dureeVidage; versees = 0f; break;
                case Phase.Retour: duree = dureeRetour; break;
                case Phase.Rebond:
                    duree = dureeRebond;
                    rebondCycle = rebond * (0.8f + 0.4f * (float)alea.NextDouble());
                    break;
            }
        }

        /// <summary>Tourne le tube autour de son pivot ; positif : la bouche descend.</summary>
        public void PoserAngle(float degres)
        {
            Angle = degres;
            // l'axe du pivot est l'axe x du modèle (celui de Blender), exprimé dans l'espace de la racine
            bascule.localRotation = Quaternion.AngleAxis(degres, Vector3.right) * repos;
        }

        /// <summary>Une durée de remplissage dans les bornes, jamais à moins de 10 % de la plage de la précédente.</summary>
        public static float TirerRemplissage(System.Random alea, Vector2 bornes, float precedente)
        {
            float v = bornes.x;
            for (int essai = 0; essai < 8; essai++)
            {
                v = Mathf.Lerp(bornes.x, bornes.y, (float)alea.NextDouble());
                if (Mathf.Abs(v - precedente) >= (bornes.y - bornes.x) * 0.1f) break;
            }
            return v;
        }

        /// <summary>Hachage stable d'une position (au centimètre), pour désynchroniser les instances.</summary>
        public static int HachagePosition(Vector3 p) =>
            unchecked(Mathf.RoundToInt(p.x * 100f) * 73856093 ^ Mathf.RoundToInt(p.z * 100f) * 19349663) | 1;

        private static Transform Trouver(Transform racine, string nom)
        {
            if (racine.name == nom) return racine;
            foreach (Transform enfant in racine)
            {
                var t = Trouver(enfant, nom);
                if (t != null) return t;
            }
            throw new InvalidOperationException($"{racine.name} : pas de « {nom} » dans le modèle.");
        }
    }
}
