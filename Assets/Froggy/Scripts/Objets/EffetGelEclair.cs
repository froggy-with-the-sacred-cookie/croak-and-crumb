using System;
using System.Collections.Generic;
using Froggy.Deplacement;
using Froggy.Regles;
using Froggy.Socle;
using UnityEngine;

namespace Froggy.Objets
{
    /// <summary>
    /// L'effet du gel éclair (P4-09), dans une scène, sans réseau. Quand une Froggy utilise l'objet (événement « objet
    /// utilisé » du socle), un projectile part droit devant elle (Notion, page V1). On le voit venir : il avance à
    /// « vitesse » m/s et s'arrête sur un mur. Le premier joueur touché est gelé « duree » secondes : il ne bouge plus
    /// et ne saute plus (Modificateurs à 0), un bloc de glace l'entoure, et il lâche le cookie sacré s'il le porte. Un
    /// coup reçu (torgnole, « coup donné ») brise la glace : il lâche son cookie par la torgnole et repart aussitôt (choix
    /// de Lucas, 18 novembre 2026). Un joueur intouchable (P3-04) est traversé. Les règles sont dans ReglesGelEclair.
    /// Sont visés les mêmes joueurs que le super croassement : toute Froggy, et les mannequins d'essai.
    /// Le projectile et la glace sont PROVISOIRES, en attendant les effets du poste 6 ; pas encore de son.
    /// En réseau, l'état des objets n'est pas encore copié chez les autres joueurs : à faire avec le poste 2.
    /// </summary>
    public class EffetGelEclair : MonoBehaviour
    {
        [SerializeField] private ReglagesGelEclair reglages;
        [Tooltip("Une boule pour le projectile, un bloc de glace autour du joueur gelé. PROVISOIRE.")]
        [SerializeField] private bool visuelProvisoire = true;

        private class Projectile { public GameObject auteur; public Vector3 position, direction; public float parcouru; public Transform visuel; }

        private readonly List<Projectile> projectiles = new List<Projectile>();
        private readonly Dictionary<GameObject, int> numeros = new Dictionary<GameObject, int>();
        private readonly Dictionary<int, (GameObject joueur, object cle, Transform glace)> geles =
            new Dictionary<int, (GameObject, object, Transform)>();
        private readonly List<(int, Vector3, bool)> cibles = new List<(int, Vector3, bool)>();
        private readonly List<GameObject> joueurs = new List<GameObject>();
        private readonly List<int> liberes = new List<int>();
        private readonly RaycastHit[] touches = new RaycastHit[16];
        private IEnumerable<GameObject> joueursImposes;
        private PartieCookieSacre sacre;
        private Material matiereGlace, matiereProjectile;

        public ReglesGelEclair Regles { get; } = new ReglesGelEclair();

        /// <summary>Un joueur vient d'être gelé, ou libéré (fin du gel ou glace brisée) : sons, HUD.</summary>
        public event Action<GameObject> Gele, Libere;

        public int ProjectilesEnVol => projectiles.Count;

        /// <summary>Pour un test : des réglages à soi, sans visuel, et les joueurs visés (sinon ceux de la scène).</summary>
        public void Regler(ReglagesGelEclair r, IEnumerable<GameObject> vises = null, bool visuel = false)
        {
            reglages = r;
            joueursImposes = vises;
            visuelProvisoire = visuel;
        }

        /// <summary>Ce joueur est-il gelé en ce moment ?</summary>
        public bool EstGele(GameObject joueur) => joueur != null && Regles.EstGele(Numero(joueur));

        private void OnEnable() => EvenementsPartie.Emis += Reagir;

        private void OnDisable()
        {
            EvenementsPartie.Emis -= Reagir;
            foreach (var p in projectiles) if (p.visuel != null) Destroy(p.visuel.gameObject);
            projectiles.Clear();
            foreach (int j in new List<int>(geles.Keys)) Liberer(j);
            Regles.Vider();
        }

        /// <summary>Écoute les événements du socle (un test l'appelle directement : hors jeu, OnEnable ne tourne pas).</summary>
        public void Reagir(EvenementPartie e)
        {
            if (reglages == null || e.Auteur == null) return;
            if (e.Type == TypeEvenement.ObjetUtilise)
            {
                var porteur = e.Auteur.GetComponent<PorteurObjet>();
                if (porteur != null && reglages.fiche != null && porteur.DernierUtilise == reglages.fiche) Tirer(e.Auteur);
            }
            else if (e.Type == TypeEvenement.CoupDonne && e.Objet != null)
            {
                // le coup brise la glace : la torgnole a déjà fait lâcher son cookie, il repart
                int n = Numero(e.Objet);
                if (Regles.Briser(n)) Liberer(n);
            }
        }

        /// <summary>Un projectile part droit devant « auteur ».</summary>
        public void Tirer(GameObject auteur)
        {
            if (auteur == null || reglages == null) return;
            Vector3 avant = auteur.transform.forward;
            avant.y = 0f;
            if (avant.sqrMagnitude < 1e-4f) avant = Vector3.forward;
            var p = new Projectile
            {
                auteur = auteur, direction = avant.normalized,
                position = Pieds(auteur) + Vector3.up * reglages.hauteur,
            };
            if (visuelProvisoire) p.visuel = Boule(p.position);
            projectiles.Add(p);
        }

        private void FixedUpdate() => Pas(Time.fixedDeltaTime);

        /// <summary>Un pas, au pas physique (un test l'appelle hors jeu) : les projectiles avancent, les gels passent.</summary>
        public void Pas(float dt)
        {
            if (reglages == null) return;
            if (projectiles.Count > 0) Avancer(dt);
            Regles.Pas(dt, liberes);
            foreach (int j in liberes) Liberer(j);
        }

        private void Avancer(float dt)
        {
            Joueurs();
            for (int i = projectiles.Count - 1; i >= 0; i--)
            {
                var p = projectiles[i];
                float pas = Mathf.Min(reglages.vitesse * dt, reglages.portee - p.parcouru);
                cibles.Clear();
                foreach (var j in joueurs)
                {
                    if (j == null || j == p.auteur || !j.activeInHierarchy) continue;
                    int n = Numero(j);
                    if (Regles.EstGele(n)) continue;
                    cibles.Add((n, j.transform.position, Intouchable(j)));
                }
                int touche = ReglesGelEclair.Toucher(p.position, p.direction, pas, reglages.rayon, cibles, out float aJoueur);
                float aMur = Mur(p, pas);

                bool fini = false;
                if (touche >= 0 && aJoueur <= aMur)
                {
                    Geler(Joueur(touche), p.direction);
                    fini = true;
                }
                else if (aMur <= pas) fini = true;

                p.parcouru += pas;
                p.position += p.direction * Mathf.Min(pas, aMur);
                if (p.parcouru >= reglages.portee - 1e-4f) fini = true;
                if (p.visuel != null) p.visuel.position = p.position;
                if (!fini) continue;
                if (p.visuel != null) Destroy(p.visuel.gameObject);
                projectiles.RemoveAt(i);
            }
        }

        /// <summary>
        /// Gèle ce joueur (un test, ou un autre effet, peut l'appeler directement) : il ne bouge plus, et lâche le cookie
        /// sacré s'il le porte, du côté d'où vient le projectile. Rend false s'il est intouchable ou déjà gelé.
        /// </summary>
        public bool Geler(GameObject joueur, Vector3 direction)
        {
            if (joueur == null || Intouchable(joueur) || reglages == null) return false;
            int n = Numero(joueur);
            if (!Regles.Geler(n, reglages.duree)) return false;
            var modificateurs = joueur.GetComponent<Modificateurs>();
            if (modificateurs == null) modificateurs = joueur.AddComponent<Modificateurs>();
            var cle = new object();
            modificateurs.Poser(cle, 0f, 0f);
            var corps = joueur.GetComponent<Rigidbody>();
            if (corps != null && !corps.isKinematic) corps.linearVelocity = new Vector3(0f, Mathf.Min(0f, corps.linearVelocity.y), 0f);
            geles[n] = (joueur, cle, visuelProvisoire ? Glace(joueur) : null);

            var porteur = joueur.GetComponent<PorteurCookies>();
            if (porteur != null)
            {
                if (sacre == null) sacre = FindAnyObjectByType<PartieCookieSacre>();
                if (sacre != null) sacre.FaireLacher(porteur, direction);
            }
            Gele?.Invoke(joueur);
            return true;
        }

        private void Liberer(int numero)
        {
            if (!geles.TryGetValue(numero, out var g)) return;
            geles.Remove(numero);
            Regles.Briser(numero);
            if (g.joueur != null)
            {
                var modificateurs = g.joueur.GetComponent<Modificateurs>();
                if (modificateurs != null) modificateurs.Retirer(g.cle);
            }
            if (g.glace != null) Destroy(g.glace.gameObject);
            Libere?.Invoke(g.joueur);
        }

        private void Update()
        {
            // la glace tremble la dernière seconde : on voit venir la fin du gel
            foreach (var g in geles)
            {
                if (g.Value.glace == null) continue;
                float tremble = Regles.Reste(g.Key) < 1f ? Mathf.Sin(Time.time * 40f) * 0.03f : 0f;
                g.Value.glace.localScale = new Vector3(1.2f + tremble, 1.3f, 1.2f + tremble);
            }
        }

        // un numéro par joueur, propre à cet effet (un mannequin d'essai n'a pas de numéro de partie)
        private int Numero(GameObject joueur)
        {
            if (!numeros.TryGetValue(joueur, out int n)) numeros[joueur] = n = numeros.Count + 1;
            return n;
        }

        private GameObject Joueur(int numero)
        {
            foreach (var kv in numeros) if (kv.Value == numero) return kv.Key;
            return null;
        }

        private static bool Intouchable(GameObject joueur)
        {
            var p = joueur.GetComponent<PorteurCookies>();
            return p != null && p.Intouchable;
        }

        private void Joueurs()
        {
            joueurs.Clear();
            if (joueursImposes != null) { joueurs.AddRange(joueursImposes); return; }
            foreach (var c in EffetSuperCroassement.Cibles()) joueurs.Add(c.gameObject);
        }

        // la distance au premier obstacle (pas un joueur, pas un déclencheur), ou l'infini
        private float Mur(Projectile p, float pas)
        {
            int n = gameObject.scene.GetPhysicsScene().SphereCast(p.position, reglages.rayon, p.direction, touches, pas, ~0,
                QueryTriggerInteraction.Ignore);
            float plusPres = float.PositiveInfinity;
            for (int i = 0; i < n; i++)
            {
                var c = touches[i].collider;
                if (c == null || c.attachedRigidbody != null && joueurs.Contains(c.attachedRigidbody.gameObject)) continue;
                if (c.GetComponentInParent<DeplacementWow>() != null || c.GetComponentInParent<PorteurCookies>() != null) continue;
                if (touches[i].distance <= 0f && touches[i].point == Vector3.zero) continue;   // départ dans un collider : ignoré
                plusPres = Mathf.Min(plusPres, touches[i].distance);
            }
            return plusPres;
        }

        private static Vector3 Pieds(GameObject go)
        {
            var c = go.GetComponent<Collider>();
            return c != null ? new Vector3(go.transform.position.x, c.bounds.min.y, go.transform.position.z) : go.transform.position;
        }

        private Transform Boule(Vector3 position)
        {
            var boule = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            DestroyImmediate(boule.GetComponent<Collider>());   // jamais un obstacle
            boule.name = "Gel éclair (provisoire)";
            boule.transform.position = position;
            boule.transform.localScale = Vector3.one * reglages.rayon * 2f;
            boule.GetComponent<Renderer>().sharedMaterial = Matiere(ref matiereProjectile, new Color(0.85f, 0.97f, 1f, 1f));
            return boule.transform;
        }

        private Transform Glace(GameObject joueur)
        {
            var bloc = GameObject.CreatePrimitive(PrimitiveType.Cube);
            DestroyImmediate(bloc.GetComponent<Collider>());
            bloc.name = "Glace (provisoire)";
            var boite = joueur.GetComponent<BoxCollider>();
            bloc.transform.SetParent(joueur.transform, false);
            bloc.transform.localPosition = boite != null ? boite.center : Vector3.zero;
            bloc.transform.localScale = new Vector3(1.2f, 1.3f, 1.2f);
            bloc.GetComponent<Renderer>().sharedMaterial = Matiere(ref matiereGlace, new Color(0.7f, 0.9f, 1f, 0.45f));
            return bloc.transform;
        }

        private static Material Matiere(ref Material m, Color couleur)
        {
            if (m != null) return m;
            var shader = Shader.Find("Sprites/Default");
            m = shader != null ? new Material(shader) { color = couleur } : null;
            return m;
        }
    }
}
