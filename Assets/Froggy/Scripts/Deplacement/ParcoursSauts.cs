using System.Collections.Generic;
using Froggy.Cameras;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Froggy.Deplacement
{
    /// <summary>
    /// Parcours de test du contrôleur (#13) : une plate-forme de départ et dix plates-formes de la taille d'un nénuphar,
    /// à enchaîner d'un saut chacune (écarts de 2 à 3,5 m, montées, descentes, virages jusqu'à 60°). « C'est fini quand on
    /// enchaîne 10 sauts sans rater par injustice, à 30 comme à 144 images par seconde » : le compteur à l'écran dit la
    /// série en cours. Une chute compte un raté et ramène au départ ; R ramène au départ et remet la série à zéro.
    /// Outil de test, pas une arène : les plates-formes se déplacent à la main dans l'éditeur, le parcours lit ses enfants
    /// dans l'ordre (Depart, puis Plateforme_01…). Construire() pose la disposition d'origine (menu et tests).
    /// </summary>
    public class ParcoursSauts : MonoBehaviour
    {
        /// <summary>Taille d'une plate-forme (largeur, épaisseur, profondeur), en m : à peu près un grand nénuphar.</summary>
        public static readonly Vector3 TaillePlateforme = new Vector3(1.8f, 0.4f, 1.8f);
        public static readonly Vector3 TailleDepart = new Vector3(3f, 0.4f, 3f);

        /// <summary>
        /// Chaque saut, par rapport à la plate-forme d'avant : écart bord à bord (m), dénivelé (m), virage (degrés). Dans la
        /// portée d'un saut de 1,5 m à 6 m/s sans sprint (≈ 5,9 m vers une plate-forme 0,6 m plus haute) : précis, pas injuste.
        /// </summary>
        public static readonly Vector3[] Sauts =
        {
            new Vector3(2.0f, 0f, 0f),
            new Vector3(2.5f, 0.5f, 30f),
            new Vector3(3.0f, 0f, -45f),
            new Vector3(2.0f, 0.8f, 0f),
            new Vector3(3.5f, -0.5f, 40f),
            new Vector3(2.5f, 0f, -30f),
            new Vector3(3.0f, 0.6f, 0f),
            new Vector3(2.0f, -1.0f, 60f),
            new Vector3(3.2f, 0f, -20f),
            new Vector3(2.8f, 0.5f, 0f),
        };

        [Tooltip("Au lancement, Froggy est posé sur la plate-forme de départ.")]
        [SerializeField] private bool placerAuDepart = true;
        [Tooltip("Ramène au départ et remet la série à zéro.")]
        [SerializeField] private Key toucheDepart = Key.R;
        [Tooltip("Sous la plus basse plate-forme de plus que ça, en m : c'est un raté, retour au départ.")]
        [SerializeField] private float margeChute = 4f;

        private readonly List<Transform> plateformes = new List<Transform>();
        private DeplacementWow deplacement;
        private Rigidbody corps;
        [System.NonSerialized] private bool place;
        [System.NonSerialized] private int courante, serie, meilleure, rates;

        /// <summary>La plate-forme où se tient Froggy (0 = départ), ou -1.</summary>
        public int Courante => courante;
        public int Serie => serie;
        public int Meilleure => meilleure;
        public int Rates => rates;
        public IReadOnlyList<Transform> Plateformes { get { Lister(); return plateformes; } }

        /// <summary>
        /// Pose la disposition d'origine sous racine (vide au préalable) : départ à l'origine locale, face à +z, puis les dix
        /// plates-formes. Des cubes avec BoxCollider, orientés dans le sens du saut (le bord est perpendiculaire à la course).
        /// </summary>
        public static void Construire(Transform racine)
        {
            Vector3 centre = Vector3.zero;
            float cap = 0f, demiAvant = TailleDepart.z * 0.5f;
            Poser(racine, "Depart", centre, cap, TailleDepart);
            for (int i = 0; i < Sauts.Length; i++)
            {
                cap += Sauts[i].z;
                Vector3 dir = Quaternion.Euler(0f, cap, 0f) * Vector3.forward;
                float demi = TaillePlateforme.z * 0.5f;
                // depuis le bord de la précédente (son bord est perpendiculaire à son propre cap : on garde le plus court)
                centre += dir * (demiAvant + Sauts[i].x + demi) + Vector3.up * Sauts[i].y;
                Poser(racine, $"Plateforme_{i + 1:00}", centre, cap, TaillePlateforme);
                demiAvant = demi;
            }
        }

        private static void Poser(Transform racine, string nom, Vector3 centreDessus, float cap, Vector3 taille)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = nom;
            go.transform.SetParent(racine, false);
            go.transform.localPosition = centreDessus + Vector3.down * (taille.y * 0.5f);   // le dessus à la hauteur voulue
            go.transform.localRotation = Quaternion.Euler(0f, cap, 0f);
            go.transform.localScale = taille;
        }

        private void Lister()
        {
            plateformes.Clear();
            foreach (Transform t in transform)
                if (t.GetComponent<Collider>() != null) plateformes.Add(t);
        }

        /// <summary>Le haut d'une plate-forme, au centre, en monde.</summary>
        public Vector3 Dessus(int i)
        {
            Lister();
            var b = plateformes[i].GetComponent<Collider>().bounds;
            return new Vector3(plateformes[i].position.x, b.max.y, plateformes[i].position.z);
        }

        /// <summary>L'index de la plate-forme dont fait partie ce collider, ou -1.</summary>
        public int Indice(Collider c)
        {
            Lister();
            for (int i = 0; i < plateformes.Count; i++)
                if (c != null && c.transform == plateformes[i]) return i;
            return -1;
        }

        private void Update()
        {
            if (deplacement == null) Suivre(FindAnyObjectByType<DeplacementWow>(FindObjectsInactive.Include));   // le décor se charge après nous
            if (corps == null) return;
            if (placerAuDepart && !place) { AuDepart(); return; }

            var clavier = Keyboard.current;
            if (clavier != null && clavier[toucheDepart].wasPressedThisFrame) { AuDepart(); return; }

            Lister();
            float plusBas = float.PositiveInfinity;
            foreach (var p in plateformes) plusBas = Mathf.Min(plusBas, p.GetComponent<Collider>().bounds.max.y);
            if (corps.position.y < plusBas - margeChute)
            {
                rates++;
                AuDepart();
            }
        }

        /// <summary>
        /// Suit ce Froggy : chaque atterrissage annoncé par son contrôleur compte, avec le sol touché. Pas au contact
        /// physique : mesuré en jeu le 18 novembre 2026, un saut enchaîné par le tampon repart quand les pieds passent à
        /// moins de groundCheckDistance du dessus, sans jamais toucher la plate-forme (OnCollisionEnter ne voyait rien).
        /// </summary>
        public void Suivre(DeplacementWow froggy)
        {
            if (deplacement != null) deplacement.Atterrissage -= SurAtterrissage;
            deplacement = froggy;
            corps = froggy != null ? froggy.GetComponent<Rigidbody>() : null;
            if (deplacement != null) deplacement.Atterrissage += SurAtterrissage;
        }

        private void OnDestroy() => Suivre(null);

        private void SurAtterrissage(Collider sol)
        {
            int i = Indice(sol);
            if (i >= 0) Atterrir(i, corps);
        }

        /// <summary>
        /// Un corps s'est posé sur la plate-forme i. Celui de Froggy seulement : la série avance d'une plate-forme à la
        /// suivante ; revenir en arrière ou en sauter une la remet à zéro ; se reposer sur la même ne change rien.
        /// </summary>
        public void Atterrir(int i, Rigidbody qui)
        {
            if (qui == null || qui != corps || i == courante) return;
            serie = i == courante + 1 ? serie + 1 : 0;
            courante = i;
            meilleure = Mathf.Max(meilleure, serie);
        }

        /// <summary>
        /// Pose Froggy sur le départ, face à la première plate-forme, sans élan ; la caméra se recale (un saut net ne doit
        /// pas faire glisser l'image, skill ressenti-mouvement, règle 6). La série repart de zéro.
        /// </summary>
        public void AuDepart()
        {
            if (corps == null) return;
            Lister();
            var col = corps.GetComponent<Collider>();
            float demi = col != null ? corps.position.y - col.bounds.min.y : 0.5f;
            Vector3 p = Dessus(0) + Vector3.up * (demi + 0.02f);
            Vector3 vers = Dessus(1) - Dessus(0);
            float cap = Mathf.Atan2(vers.x, vers.z) * Mathf.Rad2Deg;
            corps.linearVelocity = Vector3.zero;
            corps.position = p;
            corps.rotation = Quaternion.Euler(0f, cap, 0f);
            corps.transform.SetPositionAndRotation(p, corps.rotation);
            var cam = FindAnyObjectByType<CameraOrbitale>();
            if (cam != null && cam.isActiveAndEnabled) cam.Regler(cap, 18f, 6f);
            courante = 0;
            serie = 0;
            place = true;
        }

        private void OnGUI()
        {
            if (!Application.isEditor && !Debug.isDebugBuild) return;
            float e = Mathf.Max(1f, Screen.height / 900f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(e, e, 1f));
            int total = Mathf.Max(0, Plateformes.Count - 1);
            GUI.Label(new Rect(12f, Screen.height / e - 34f, 560f, 24f),
                $"Parcours de sauts : série {serie} / {total} · meilleure {meilleure} · ratés {rates} · R : départ");
        }
    }
}
