using System.Collections.Generic;
using System.Linq;
using Froggy.Arene;
using Froggy.Deplacement;
using Froggy.Objets;
using Froggy.Regles;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Froggy.Bots
{
    /// <summary>
    /// Remplit une partie avec des bots (P3-05), sans réseau. Dès que la Froggy du joueur est là (elle arrive avec le
    /// décor), il en fait « nombreDeBots » copies, leur retire ce qui ne sert qu'à un humain (caméra, PlayerInput) et
    /// leur donne un CerveauBot ; puis il relance la partie (cookies, objets, apparitions) pour qu'elle les compte comme
    /// des joueurs. Posez assez de repères « Apparition d'un joueur » (Bâtisseur) : un par joueur, bots compris.
    /// Il construit aussi la NavMesh, la carte des endroits où l'on marche, à partir des colliders du décor chargé : les
    /// bots y trouvent leur chemin. Elle est faite au lancement, rien n'est enregistré dans la scène du décor.
    /// Les bots du salon en ligne viendront dans une autre carte (l'hôte les fera tourner).
    /// </summary>
    public class BotsPartie : MonoBehaviour
    {
        [Tooltip("Nombre de bots ajoutés au joueur (0 à 3 : 4 Froggy au plus).")]
        [Range(0, 3)] public int nombreDeBots = 3;
        [SerializeField] private ReglagesBot reglages;
        [Tooltip("Avec au moins un bot, éteindre les mannequins d'essai (immobiles), que la partie compterait comme des joueurs.")]
        [SerializeField] private bool couperMannequins = true;

        [Header("Carte des chemins (NavMesh)")]
        [Tooltip("Taille de la zone couverte autour du joueur, en m.")]
        [SerializeField] private Vector3 taille = new Vector3(120f, 40f, 120f);
        [Tooltip("Rayon d'une Froggy pour la carte, en m : plus grand, les bots restent loin des murs.")]
        [SerializeField] private float rayonAgent = 0.4f;
        [Tooltip("Hauteur qu'une Froggy monte en marchant, en m ; au-delà, c'est un saut.")]
        [SerializeField] private float marche = 0.4f;

        private readonly List<CerveauBot> bots = new List<CerveauBot>();
        private NavMeshDataInstance carte;

        public IReadOnlyList<CerveauBot> Bots => bots;

        private void OnEnable() => SceneManager.sceneLoaded += QuandUneSceneArrive;

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= QuandUneSceneArrive;
            if (carte.valid) carte.Remove();
        }

        private void Start() => Peupler();
        private void QuandUneSceneArrive(Scene scene, LoadSceneMode mode) => Peupler();
        private void FixedUpdate() { if (bots.Count < nombreDeBots) Peupler(); }

        /// <summary>Le joueur humain : la Froggy qui a un PlayerInput allumé.</summary>
        public static DeplacementWow Humain() =>
            FindObjectsByType<DeplacementWow>().FirstOrDefault(f =>
            {
                var entree = f.GetComponent<PlayerInput>();
                return entree != null && entree.enabled && f.GetComponent<CerveauBot>() == null;
            });

        /// <summary>Crée les bots qui manquent, s'il y a un joueur à copier, puis relance la partie.</summary>
        public void Peupler()
        {
            bots.RemoveAll(b => b == null);
            if (bots.Count >= nombreDeBots) return;
            var humain = Humain();
            if (humain == null) return;   // le décor n'est pas encore là

            var reperes = FindObjectsByType<RepereArene>().Where(r => r.sorte == RepereArene.Sorte.Joueur)
                .OrderBy(r => r.joueur).Select(r => r.transform.position).ToList();
            while (bots.Count < nombreDeBots)
            {
                int n = bots.Count + 1;
                Vector3 place = n < reperes.Count ? reperes[n] + Vector3.up * 0.6f : humain.transform.position + Vector3.right * 2f * n;
                bots.Add(Creer(humain.gameObject, n, place));
            }

            // les mannequins d'essai (un PorteurCookies qui ne bouge pas) compteraient comme des joueurs : coupés
            if (couperMannequins)
                foreach (var m in FindObjectsByType<PorteurCookies>())
                    if (m.GetComponent<DeplacementWow>() == null) m.gameObject.SetActive(false);

            // la partie les compte maintenant : cookies, objets, puis les places de départ
            var cookies = FindAnyObjectByType<PartieCookies>();
            if (cookies != null) cookies.Demarrer();
            var objets = FindAnyObjectByType<PartieObjets>();
            if (objets != null) objets.Demarrer();
            var apparitions = FindAnyObjectByType<ApparitionJoueurs>();
            if (apparitions != null) apparitions.Demarrer();

            ConstruireCarte(humain.transform.position);
        }

        /// <summary>Une copie de la Froggy du joueur, jouée par l'ordinateur.</summary>
        public CerveauBot Creer(GameObject modele, int numero, Vector3 place)
        {
            // copiée sous un parent éteint : rien ne s'éveille (ni caméra ni PlayerInput) avant le tri
            var attente = new GameObject("(bot en préparation)");
            attente.SetActive(false);
            var bot = Instantiate(modele, place, modele.transform.rotation, attente.transform);
            bot.name = $"Bot {numero}";

            foreach (var c in bot.GetComponentsInChildren<Camera>(true))
            {
                // la caméra et son support (CameraOrbitale) : le plus haut enfant qui la porte
                var t = c.transform;
                while (t.parent != null && t.parent != bot.transform) t = t.parent;
                if (t != bot.transform) DestroyImmediate(t.gameObject);
            }
            foreach (var l in bot.GetComponentsInChildren<AudioListener>(true)) DestroyImmediate(l);
            // le modèle affiché a été copié sans son lien vers FroggyApparence : il resterait figé. On le retire,
            // FroggyApparence en pose un neuf, animé, à son démarrage.
            foreach (var a in bot.GetComponentsInChildren<Animator>(true))
                if (a != null && a.gameObject != bot) DestroyImmediate(a.gameObject);
            foreach (var c in bot.GetComponents<Froggy.Template.TemplatePlayerController>()) DestroyImmediate(c);
            // FroggyApparence exige un PlayerInput : il reste, éteint (aucune manette ni clavier ne lui est donné).
            // Le joueur local se reconnaît donc à un PlayerInput allumé (PartieTorgnoles, PartieObjets, Humain).
            foreach (var c in bot.GetComponents<PlayerInput>()) c.enabled = false;
            // la partie leur donne un numéro neuf
            foreach (var c in bot.GetComponents<PorteurCookies>()) DestroyImmediate(c);
            foreach (var c in bot.GetComponents<PorteurObjet>()) DestroyImmediate(c);

            bot.GetComponent<DeplacementWow>().enabled = true;
            var cerveau = bot.AddComponent<CerveauBot>();
            cerveau.Regler(reglages, numero * 7919);

            bot.transform.SetParent(null, true);
            SceneManager.MoveGameObjectToScene(bot, modele.scene);
            Destroy(attente);
            return cerveau;
        }

        /// <summary>
        /// La NavMesh des bots, construite à partir des colliders du décor chargé (sans les Froggy, sans les zones
        /// d'eau ni les autres déclencheurs). Elle remplace la précédente.
        /// </summary>
        public void ConstruireCarte(Vector3 centre)
        {
            var zone = new Bounds(centre, taille);
            var sources = new List<NavMeshBuildSource>();
            NavMeshBuilder.CollectSources(zone, ~0, NavMeshCollectGeometry.PhysicsColliders, 0, new List<NavMeshBuildMarkup>(), sources);
            sources.RemoveAll(s =>
            {
                var c = s.component as Collider;
                if (c == null) return false;
                return c.isTrigger || c.GetComponentInParent<DeplacementWow>() != null || c.GetComponentInParent<PorteurCookies>() != null;
            });
            // les modèles importés sans « Read/Write » ne laissent pas lire leur forme en jeu (erreurs dans l'éditeur,
            // obstacles absents dans un build) : chacun devient une boîte de sa taille. Un arbre bloque ainsi tout le sol
            // sous ses branches ; c'est le prix pour ne pas toucher aux imports de l'équipe.
            for (int i = 0; i < sources.Count; i++)
            {
                var s = sources[i];
                var maillage = s.sourceObject as Mesh;
                var c = s.component as Collider;
                if (s.shape != NavMeshBuildSourceShape.Mesh || maillage == null || maillage.isReadable || c == null) continue;
                sources[i] = new NavMeshBuildSource
                {
                    shape = NavMeshBuildSourceShape.Box, size = c.bounds.size, area = s.area,
                    transform = Matrix4x4.TRS(c.bounds.center, Quaternion.identity, Vector3.one), component = c,
                };
            }

            var reglagesCarte = NavMesh.GetSettingsByID(0);
            reglagesCarte.agentRadius = rayonAgent;
            reglagesCarte.agentHeight = 1f;
            reglagesCarte.agentClimb = marche;
            reglagesCarte.agentSlope = 45f;
            var donnees = NavMeshBuilder.BuildNavMeshData(reglagesCarte, sources, zone, Vector3.zero, Quaternion.identity);
            if (carte.valid) carte.Remove();
            if (donnees != null) carte = NavMesh.AddNavMeshData(donnees);
        }
    }
}
