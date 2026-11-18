using System.Collections.Generic;
using UnityEngine;

namespace Froggy.Regles
{
    /// <summary>
    /// Le gobelin du cookie sacré (P5-08, modèle et animations : P7-07) : il jaillit (« Apparition »), fuit les joueurs en
    /// courant (« Course », en boucle), et une fois attrapé se débat puis boude (« Attrape ») avant de disparaître.
    /// Ce n'est pas lui qui décide qu'il est attrapé : ReglesCookieSacre le fait (PartieCookieSacre appelle Attraper).
    /// Fuite : s'éloigner des joueurs proches, revenir vers son départ s'il s'en écarte trop, contourner les obstacles par
    /// des rayons, ne jamais quitter le sol. Sans joueur proche, il trottine. Réglages PROVISOIRES (ReglagesCookieSacre).
    /// « mains » est le point entre ses mains (enfant de l'os Spine1) : le cookie sacré s'y accroche.
    /// </summary>
    public class FuiteGobelin : MonoBehaviour
    {
        public enum Etat { Apparition, Fuite, Attrape, Parti }

        [Tooltip("Le point entre ses mains, où le cookie sacré se tient (enfant de l'os Spine1).")]
        [SerializeField] private Transform mains;
        [Tooltip("Durées des clips, en s : on change d'état à la fin (Apparition 1,3 ; Attrape 2,0).")]
        [SerializeField] private float dureeApparition = 1.3f, dureeAttrape = 2.0f;

        private static readonly int ParamVitesse = Animator.StringToHash("Vitesse");
        private static readonly int ParamAttrape = Animator.StringToHash("Attrape");
        private static readonly float[] Detours = { 0f, 30f, -30f, 60f, -60f, 90f, -90f, 125f, -125f, 180f };
        private readonly RaycastHit[] touches = new RaycastHit[8];

        private Animator animateur;
        private ReglagesCookieSacre reglages;
        private Vector3 depart, direction;
        private float temps, graine;

        public Etat EtatActuel { get; private set; } = Etat.Apparition;
        /// <summary>Le point entre ses mains (lui-même s'il n'en a pas).</summary>
        public Transform Mains
        {
            get => mains != null ? mains : transform;
            set => mains = value;
        }

        /// <summary>Le fait apparaître à « depart » (le centre de sa zone de fuite) : il joue « Apparition », puis fuit.</summary>
        public void Apparaitre(Vector3 depart, ReglagesCookieSacre reglages)
        {
            this.depart = depart;
            this.reglages = reglages != null ? reglages : ScriptableObject.CreateInstance<ReglagesCookieSacre>();
            animateur = GetComponentInChildren<Animator>();
            transform.position = AuSol(depart, out _);
            direction = transform.forward;
            graine = (depart.x * 12.9898f + depart.z * 78.233f) % 100f;   // même trajet partout, à entrées égales
            temps = 0f;
            EtatActuel = Etat.Apparition;
        }

        /// <summary>Il est attrapé : il s'arrête, se débat, boude, puis disparaît.</summary>
        public void Attraper()
        {
            if (EtatActuel == Etat.Attrape || EtatActuel == Etat.Parti) return;
            EtatActuel = Etat.Attrape;
            temps = 0f;
            if (animateur != null) animateur.SetTrigger(ParamAttrape);
        }

        /// <summary>Un pas, au pas physique : « menaces » sont les positions des joueurs. « bouger » faux : il reste sur place.</summary>
        public void Pas(float dt, IReadOnlyList<Vector3> menaces, bool bouger = true)
        {
            temps += dt;
            switch (EtatActuel)
            {
                case Etat.Apparition:
                    if (temps >= dureeApparition) { EtatActuel = Etat.Fuite; temps = 0f; }
                    Vitesse(0f);
                    break;
                case Etat.Fuite:
                    if (bouger) Courir(dt, menaces); else Vitesse(0f);
                    break;
                case Etat.Attrape:
                    if (temps >= dureeAttrape)
                    {
                        float u = Mathf.Clamp01((temps - dureeAttrape) / 0.3f);   // il rapetisse et s'en va
                        transform.localScale = Vector3.one * (1f - u);
                        if (u >= 1f) { EtatActuel = Etat.Parti; gameObject.SetActive(false); }
                    }
                    break;
            }
        }

        private void Courir(float dt, IReadOnlyList<Vector3> menaces)
        {
            Vector3 ici = transform.position;
            Vector3 fuite = Vector3.zero;
            foreach (var m in menaces)
            {
                Vector3 ecart = ici - m; ecart.y = 0f;
                float d = ecart.magnitude;
                if (d < 1e-3f || d > reglages.distancePeur) continue;
                fuite += ecart / d * (1f - d / reglages.distancePeur);
            }
            bool apeure = fuite.sqrMagnitude > 1e-4f;
            Vector3 voulue = apeure ? fuite.normalized : Quaternion.Euler(0f, 70f * (Mathf.PerlinNoise(graine, temps * 0.4f) - 0.5f) * 2f, 0f) * direction;

            // la zone : au-delà des trois quarts de son rayon, il est ramené vers son départ
            Vector3 dehors = ici - depart; dehors.y = 0f;
            float r = dehors.magnitude / reglages.rayonArene;
            if (r > 0.75f) voulue += -dehors.normalized * Mathf.Lerp(0f, 2.5f, (r - 0.75f) / 0.25f);
            voulue += Quaternion.Euler(0f, 90f, 0f) * voulue * (0.25f * (Mathf.PerlinNoise(temps * 0.7f, graine) - 0.5f));   // zigzag
            voulue.y = 0f;
            if (voulue.sqrMagnitude < 1e-4f) voulue = direction;
            voulue.Normalize();

            float vitesse = reglages.vitesse * (apeure ? 1f : 0.45f);
            Vector3 libre = Contourner(ici, voulue, Mathf.Max(1.2f, vitesse * 0.3f));
            direction = Vector3.Slerp(direction, libre, 1f - Mathf.Exp(-10f * dt)).normalized;
            Vector3 suivant = AuSol(ici + direction * vitesse * dt, out bool sol);
            if (sol) transform.position = suivant;
            if (direction.sqrMagnitude > 1e-4f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(direction, Vector3.up), 540f * dt);
            Vitesse(vitesse / Mathf.Max(0.01f, reglages.vitesse));
        }

        /// <summary>La première direction libre, en partant de « voulue » et en s'en écartant de plus en plus.</summary>
        private Vector3 Contourner(Vector3 ici, Vector3 voulue, float portee)
        {
            var physique = gameObject.scene.GetPhysicsScene();
            Vector3 hanches = ici + Vector3.up * 0.4f;
            foreach (float angle in Detours)
            {
                Vector3 d = Quaternion.Euler(0f, angle, 0f) * voulue;
                if (Bloque(physique, hanches, d, portee)) continue;
                AuSol(ici + d * portee, out bool sol);
                if (sol) return d;                       // pas de trou ni de vide devant
            }
            return -voulue;
        }

        private bool Bloque(PhysicsScene physique, Vector3 depuis, Vector3 d, float portee)
        {
            int n = physique.SphereCast(depuis, 0.2f, d, touches, portee, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
                if (!touches[i].transform.IsChildOf(transform) && touches[i].distance > 0f) return true;
            return false;
        }

        /// <summary>Le point du sol sous « p » (de 1 m au-dessus à 3 m en dessous) ; « sol » faux s'il n'y en a pas.</summary>
        private Vector3 AuSol(Vector3 p, out bool sol)
        {
            var physique = gameObject.scene.GetPhysicsScene();
            int n = physique.Raycast(p + Vector3.up * 1f, Vector3.down, touches, 4f, ~0, QueryTriggerInteraction.Ignore);
            float meilleur = float.MaxValue; sol = false; Vector3 point = p;
            for (int i = 0; i < n; i++)
            {
                if (touches[i].transform.IsChildOf(transform) || touches[i].distance >= meilleur) continue;
                meilleur = touches[i].distance; point = touches[i].point; sol = true;
            }
            return sol ? point : p;
        }

        private void Vitesse(float v)
        {
            if (animateur != null && animateur.runtimeAnimatorController != null) animateur.SetFloat(ParamVitesse, Mathf.Max(0.5f, v));
        }
    }
}
