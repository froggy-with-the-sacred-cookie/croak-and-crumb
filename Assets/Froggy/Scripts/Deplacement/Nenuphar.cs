using System.Collections.Generic;
using UnityEngine;

namespace Froggy.Deplacement
{
    /// <summary>
    /// Un nénuphar sur lequel on se pose (#16) : il s'enfonce un peu sous le poids, plonge à l'atterrissage puis remonte,
    /// et penche du côté où l'on se tient. Seul le dessin bouge : le collider reste où il est (là où le Bâtisseur l'a
    /// mesuré, là où les sauts ont été testés), et en réseau rien n'est à synchroniser (skill rendu-vivant : un effet est
    /// local et ne pilote aucune règle). La houle et le lacet au vent sont dans le shader (CouleurSommet, _Flotter).
    /// Le contrôleur (DeplacementWow) signale à chaque pas physique le nénuphar sous ses pieds, et y fait suivre son dessin.
    /// Faits appliqués (ReglagesNenuphar, fiche Docs/Recherches/nenuphars-2026-11-18.md) : un nénuphar est souple, l'eau
    /// déplacée le remonte, et il oscille à ω ≈ √(g/d) : la fréquence se dérive de l'enfoncement.
    /// </summary>
    [DisallowMultipleComponent]
    public class Nenuphar : MonoBehaviour
    {
        /// <summary>Le réglage partagé des nénuphars du jeu (posé sur les préfabriqués, et par le kit gris).</summary>
        public const string CheminReglages = "Assets/Froggy/Postes/1_MouvementCamera/ReglagesNenuphar.asset";

        [SerializeField] private ReglagesNenuphar reglages;
        [Tooltip("Le dessin qui s'enfonce et penche. Vide : au lancement, le nénuphar recopie son dessin dans un enfant " +
                 "« Dessin » et cache le sien ; le collider, lui, ne bouge jamais.")]
        [SerializeField] private Transform dessin;

        private struct Porteur { public Vector3 pieds; public float vu; }

        private readonly Dictionary<Object, Porteur> porteurs = new Dictionary<Object, Porteur>();
        private readonly List<Object> partis = new List<Object>();
        private ReglagesNenuphar parDefaut;
        private Vector3 reposPosition;
        private Quaternion reposRotation;
        private float rayon = 1f;
        private bool pret;
        [System.NonSerialized] private float horloge, enfoncement, vitesse;
        // la pente, vers le côté qui descend, à plat dans les axes du monde (x, z), en degrés. Pas dans les axes du modèle :
        // ceux du jardin sont tournés de 90° (Blender a z vers le haut), leur y local est à l'horizontale (mesuré en jeu)
        [System.NonSerialized] private Vector2 penche, vitessePenche;

        // une grenouille compte tant qu'elle a été signalée depuis moins que ça (le contrôleur le fait à chaque pas physique,
        // et un affichage rapide fait plusieurs images entre deux pas)
        private const float Memoire = 0.08f;
        // pas d'intégration du ressort, au plus : stable même à 30 images par seconde
        private const float PasMax = 1f / 120f;

        /// <summary>De combien le dessin est descendu, en m (positif vers le bas).</summary>
        public float Enfoncement => enfoncement;
        /// <summary>L'inclinaison du dessin : vers le côté qui descend, à plat (x et z du monde), longue de l'angle en degrés.</summary>
        public Vector2 Pente => penche;
        public Transform Dessin { get { Preparer(); return dessin; } }
        public int NombreDePorteurs
        {
            get
            {
                int n = 0;
                foreach (var p in porteurs.Values) if (horloge - p.vu <= Memoire) n++;
                return n;
            }
        }
        /// <summary>La pulsation propre, en rad/s : √(g/d) pour d l'enfoncement sous une grenouille.</summary>
        public float Pulsation => Mathf.Sqrt(-Physics.gravity.y / Reglages.enfoncementParGrenouille);

        private ReglagesNenuphar Reglages
        {
            get
            {
                if (reglages != null) return reglages;
                if (parDefaut == null) parDefaut = ScriptableObject.CreateInstance<ReglagesNenuphar>();
                return parDefaut;
            }
        }

        /// <summary>Le nénuphar à qui appartient ce collider, ou null (le sol, un pont…).</summary>
        public static Nenuphar Sous(Collider c)
        {
            if (c == null) return null;
            return c.TryGetComponent(out Nenuphar n) ? n : c.GetComponentInParent<Nenuphar>();
        }

        /// <summary>Pour un test : un réglage (une copie, jamais l'asset partagé).</summary>
        public void Regler(ReglagesNenuphar r) => reglages = r;

        private void Start() => Preparer();
        private void Update() => Pas(Time.deltaTime);

        private void OnDestroy()
        {
            if (parDefaut == null) return;
            if (Application.isPlaying) Destroy(parDefaut);
            else DestroyImmediate(parDefaut);
        }

        private void Preparer()
        {
            if (pret) return;
            pret = true;
            var col = GetComponent<Collider>();
            if (col == null) col = GetComponentInChildren<Collider>();
            if (col != null) rayon = Mathf.Max(0.1f, Mathf.Max(col.bounds.extents.x, col.bounds.extents.z));
            if (dessin == transform) dessin = null;   // bouger la racine bougerait le collider
            if (dessin == null) dessin = Separer();
            if (dessin != null)
            {
                reposPosition = dessin.localPosition;
                reposRotation = dessin.localRotation;
            }
        }

        /// <summary>
        /// Recopie le dessin de la racine dans un enfant, qu'on peut bouger sans bouger le collider. Au lancement seulement
        /// (Start, ou le premier pas d'un test) : le préfabriqué et la scène ne changent pas.
        /// </summary>
        private Transform Separer()
        {
            var rendu = GetComponent<MeshRenderer>();
            var filtre = GetComponent<MeshFilter>();
            if (rendu == null || filtre == null) return null;
            var go = new GameObject("Dessin") { layer = gameObject.layer };
            go.transform.SetParent(transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = filtre.sharedMesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterials = rendu.sharedMaterials;
            r.shadowCastingMode = rendu.shadowCastingMode;
            r.receiveShadows = rendu.receiveShadows;
            r.lightProbeUsage = rendu.lightProbeUsage;
            r.reflectionProbeUsage = rendu.reflectionProbeUsage;
            r.renderingLayerMask = rendu.renderingLayerMask;
            r.motionVectorGenerationMode = rendu.motionVectorGenerationMode;
            r.allowOcclusionWhenDynamic = rendu.allowOcclusionWhenDynamic;
            r.enabled = rendu.enabled;
            rendu.enabled = false;
            return go.transform;
        }

        /// <summary>
        /// Une grenouille se tient sur ce nénuphar (appelé à chaque pas physique où elle y est). vitesseImpact, en m/s
        /// vers le bas, au seul pas où elle s'y pose : le nénuphar plonge d'autant, puis remonte.
        /// </summary>
        public void Porter(Object qui, Vector3 pieds, float vitesseImpact)
        {
            Preparer();
            porteurs[qui] = new Porteur { pieds = pieds, vu = horloge };
            if (vitesseImpact > 0f)
                vitesse += Mathf.Min(Reglages.impact * vitesseImpact, Reglages.impactMax);
        }

        /// <summary>Un choc sans poids, en m/s vers le bas (une éclaboussure, un objet qui tombe à côté ; un test).</summary>
        public void Pousser(float vitesseVersLeBas)
        {
            Preparer();
            vitesse += vitesseVersLeBas;
        }

        /// <summary>
        /// Un pas du dessin : la charge (qui est dessus, et où), puis un ressort amorti vers l'enfoncement et la pente
        /// voulus, intégré par petits pas. Purement visuel : rien d'autre ne le lit que le dessin de Froggy.
        /// </summary>
        public void Pas(float dt)
        {
            Preparer();
            var r = Reglages;
            int n = 0;
            Vector2 decalage = Vector2.zero;
            partis.Clear();
            foreach (var kv in porteurs)
            {
                if (horloge - kv.Value.vu > Memoire) { partis.Add(kv.Key); continue; }
                n++;
                Vector3 d = kv.Value.pieds - transform.position;   // à plat, dans les axes du monde
                decalage += new Vector2(d.x, d.z);
            }
            foreach (var k in partis) porteurs.Remove(k);
            horloge += dt;

            float cible = Mathf.Min(n * r.enfoncementParGrenouille, r.enfoncementMax);
            // le côté chargé descend : la pente pointe vers le centre de la charge, d'autant plus qu'elle est loin du milieu
            Vector2 penteCible = n > 0 ? Vector2.ClampMagnitude(decalage / n / rayon, 1f) * r.penteAuBord : Vector2.zero;

            float w = Pulsation, z = r.amortissement;
            int pas = Mathf.Max(1, Mathf.CeilToInt(dt / PasMax));
            float h = dt / pas;
            for (int i = 0; i < pas; i++)
            {
                vitesse += (w * w * (cible - enfoncement) - 2f * z * w * vitesse) * h;
                enfoncement += vitesse * h;
                vitessePenche += (w * w * (penteCible - penche) - 2f * z * w * vitessePenche) * h;
                penche += vitessePenche * h;
            }

            if (dessin != null)
            {
                // en monde : descendre, c'est vers le bas du monde, quelle que soit la rotation du modèle
                var parent = dessin.parent;
                Vector3 repos = parent != null ? parent.TransformPoint(reposPosition) : reposPosition;
                Quaternion reposRot = parent != null ? parent.rotation * reposRotation : reposRotation;
                dessin.SetPositionAndRotation(repos + Vector3.down * enfoncement, Inclinaison() * reposRot);
            }
        }

        /// <summary>La rotation qui fait descendre le côté où pointe la pente (autour de l'axe à plat qui lui est perpendiculaire).</summary>
        private Quaternion Inclinaison()
        {
            float angle = penche.magnitude;
            if (angle < 1e-4f) return Quaternion.identity;
            // dans Unity, tourner autour de « haut × côté » fait descendre ce côté
            return Quaternion.AngleAxis(angle, Vector3.Cross(Vector3.up, new Vector3(penche.x, 0f, penche.y) / angle));
        }

        /// <summary>
        /// De combien le dessus du nénuphar est descendu en ce point (vu de dessus), en m, vers le bas : l'enfoncement,
        /// plus la pente. Le dessin de Froggy s'en sert pour rester posé sur le dessin du nénuphar.
        /// </summary>
        public float DecalageEn(Vector3 point)
        {
            float angle = penche.magnitude;
            if (angle < 1e-4f) return enfoncement;
            Vector3 d = point - transform.position;
            Vector2 cote = penche / angle;
            return enfoncement + Mathf.Sin(angle * Mathf.Deg2Rad) * (d.x * cote.x + d.z * cote.y);
        }
    }
}
