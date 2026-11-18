using System.Collections.Generic;
using System.Linq;
using Froggy.Arene;
using Froggy.Socle;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Froggy.Regles
{
    /// <summary>
    /// Apparition et réapparition des joueurs dans une scène (P3-04), sans réseau. Au départ, chaque Froggy est posée sur
    /// son repère « Apparition d'un joueur » (posé avec le Bâtisseur) ; une Froggy qui tombe hors de l'arène revient au
    /// point d'apparition le plus loin des autres, regard tourné vers le centre, et reste intouchable un moment
    /// (PorteurCookies.Intouchable : PartieCookies.FaireLacher la refuse). Les règles sont dans ReglesReapparition.
    /// Les joueurs sont ceux de PartieCookies ; seuls ceux qui ont un Rigidbody se déplacent (un mannequin reste où il est).
    /// Le clignotement de l'intouchable est provisoire, en attendant un vrai effet (poste 6) ; il passe par
    /// Renderer.forceRenderingOff et laisse « enabled » au choix du modèle.
    /// </summary>
    public class ApparitionJoueurs : MonoBehaviour
    {
        [SerializeField] private ReglagesReapparition reglages;
        [SerializeField] private PartieCookies partie;
        [Tooltip("Fait clignoter une Froggy intouchable (2 fois par seconde, sous le seuil de 3 éclairs par seconde). PROVISOIRE.")]
        [SerializeField] private bool clignoter = true;

        private const float PeriodeClignotement = 0.5f;

        private readonly List<(int, Vector3)> positions = new List<(int, Vector3)>();
        private readonly List<ReglesReapparition.Reapparu> reapparus = new List<ReglesReapparition.Reapparu>();
        // les Froggy que le clignotement cache en ce moment (vrai) ou a cachées puis remontrées (faux)
        private readonly Dictionary<PorteurCookies, bool> caches = new Dictionary<PorteurCookies, bool>();
        private Vector3? centre;
        private bool aDemarrer = true;
        private float horloge;

        public ReglesReapparition Regles { get; private set; }
        public ReglagesReapparition Reglages => reglages;
        public PartieCookies Partie => partie;

        /// <summary>Pour un test : des réglages et une partie de cookies à soi.</summary>
        public void Regler(ReglagesReapparition r, PartieCookies p)
        {
            reglages = r;
            partie = p;
        }

        private void OnEnable() => SceneManager.sceneLoaded += QuandUneSceneArrive;

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= QuandUneSceneArrive;
            foreach (var p in caches.Keys.ToList()) Montrer(p, true);
        }

        // le décor (et ses repères, ses Froggy) peut arriver après cette scène ; PartieCookies renumérote d'abord les joueurs
        private void QuandUneSceneArrive(Scene scene, LoadSceneMode mode) => aDemarrer = true;

        /// <summary>(Re)lance avec les repères « Apparition d'un joueur » et « Centre » des scènes chargées.</summary>
        public void Demarrer() => Demarrer(FindObjectsByType<RepereArene>());

        private List<RepereArene> derniersReperes = new List<RepereArene>();

        /// <summary>Repose chaque Froggy sur son point de départ, avec les mêmes repères (« Rejouer »).</summary>
        public void Recommencer() => Demarrer(derniersReperes);

        /// <summary>(Re)lance avec ces repères (un test donne les siens) et pose chaque Froggy sur son point de départ.</summary>
        public void Demarrer(IEnumerable<RepereArene> reperes)
        {
            derniersReperes = reperes.Where(r => r != null).ToList();
            reperes = derniersReperes;
            aDemarrer = false;
            if (reglages == null) reglages = ScriptableObject.CreateInstance<ReglagesReapparition>();
            if (partie == null) partie = FindAnyObjectByType<PartieCookies>();
            foreach (var p in caches.Keys.ToList()) Montrer(p, true);
            caches.Clear();

            var tous = reperes.Where(r => r != null).ToList();
            int nombre = partie != null ? partie.Porteurs.Count : 0;
            var apparitions = tous.Where(r => r.sorte == RepereArene.Sorte.Joueur).ToList();
            // une arène à 4 se joue aussi à 2 et à 3 : les repères de cette partie, sinon tous
            if (apparitions.Any(r => r.SertA(nombre))) apparitions = apparitions.Where(r => r.SertA(nombre)).ToList();
            // un ordre qui ne dépend pas de la machine
            var points = apparitions.OrderBy(r => r.joueur).ThenBy(r => r.transform.position.x).ThenBy(r => r.transform.position.z)
                .Select(r => (r.joueur, r.transform.position)).ToList();

            var repereCentre = tous.FirstOrDefault(r => r.sorte == RepereArene.Sorte.Centre);
            centre = repereCentre != null ? repereCentre.transform.position
                : points.Count > 0 ? points.Aggregate(Vector3.zero, (s, p) => s + p.position) / points.Count : (Vector3?)null;

            // sans repère, chaque Froggy garde sa place de départ : elle y reviendra
            var joueurs = Joueurs().ToList();
            if (points.Count == 0)
                foreach (var p in joueurs) points.Add((p.Joueur, p.transform.position - Vector3.up * Hauteur(p)));

            float plusBas = points.Count > 0 ? points.Min(p => p.position.y) : 0f;
            Regles = new ReglesReapparition(points, plusBas - reglages.profondeurChute, reglages.delai, reglages.dureeIntouchable);

            foreach (var p in joueurs)
                if (Regles.Depart(p.Joueur, out Vector3 depart)) Placer(p, depart);
        }

        private void FixedUpdate()
        {
            if (aDemarrer) Demarrer();
            Pas(Time.fixedDeltaTime);
        }

        /// <summary>Un pas, au pas physique (un test l'appelle hors jeu).</summary>
        public void Pas(float dt)
        {
            if (Regles == null || partie == null) return;
            positions.Clear();
            foreach (var p in partie.Porteurs)
                if (p != null && p.isActiveAndEnabled) positions.Add((p.Joueur, p.transform.position));
            reapparus.Clear();
            Regles.Pas(dt, positions, reapparus);

            foreach (var r in reapparus)
            {
                var porteur = Porteur(r.Joueur);
                if (porteur == null || porteur.GetComponent<Rigidbody>() == null) continue;
                Placer(porteur, r.Point);
                EvenementsPartie.Emettre(TypeEvenement.JoueurReapparu, porteur.gameObject, null, porteur.transform.position);
            }

            horloge += dt;
            bool allume = horloge % PeriodeClignotement < PeriodeClignotement * 0.5f;
            foreach (var p in partie.Porteurs)
            {
                if (p == null) continue;
                var etat = Regles.Etat(p.Joueur);
                p.Intouchable = etat != EtatReapparition.EnJeu;
                if (p.GetComponent<Rigidbody>() == null) continue;
                Montrer(p, !clignoter || etat != EtatReapparition.Intouchable || allume);
            }
        }

        private IEnumerable<PorteurCookies> Joueurs() =>
            partie == null ? Enumerable.Empty<PorteurCookies>() : partie.Porteurs.Where(p => p != null && p.GetComponent<Rigidbody>() != null);

        private PorteurCookies Porteur(int joueur) => partie.Porteurs.FirstOrDefault(p => p != null && p.Joueur == joueur);

        /// <summary>Pose une Froggy debout sur ce point du sol, immobile, tournée vers le centre de l'arène.</summary>
        public void Placer(PorteurCookies porteur, Vector3 sol)
        {
            Vector3 position = sol + Vector3.up * (Hauteur(porteur) + 0.02f);
            Quaternion rotation = porteur.transform.rotation;
            if (centre.HasValue)
            {
                Vector3 regard = centre.Value - sol;
                regard.y = 0f;
                if (regard.sqrMagnitude > 0.01f) rotation = Quaternion.LookRotation(regard.normalized, Vector3.up);
            }
            porteur.transform.SetPositionAndRotation(position, rotation);
            var corps = porteur.GetComponent<Rigidbody>();
            if (corps == null) return;
            corps.position = position;
            corps.rotation = rotation;
            if (!corps.isKinematic)
            {
                corps.linearVelocity = Vector3.zero;
                corps.angularVelocity = Vector3.zero;
            }
        }

        // du pivot de la Froggy au bas de sa boîte : la poser les pieds sur le sol
        private static float Hauteur(PorteurCookies porteur)
        {
            // d'après la boîte elle-même : ses « bounds » retardent d'un pas physique après un déplacement
            var boite = porteur.GetComponent<BoxCollider>();
            if (boite != null) return Mathf.Max(0f, -(boite.center.y - boite.size.y * 0.5f) * porteur.transform.lossyScale.y);
            var forme = porteur.GetComponent<Collider>();
            return forme != null ? Mathf.Max(0f, porteur.transform.position.y - forme.bounds.min.y) : 0f;
        }

        /// <summary>
        /// Cache ou remontre une Froggy par « forceRenderingOff », jamais par « enabled » : le choix du modèle
        /// (FroggyApparence) allume et éteint ses rendus par « enabled », et le clignotement ne doit pas le défaire.
        /// </summary>
        private void Montrer(PorteurCookies porteur, bool visible)
        {
            if (porteur == null) return;
            bool cacher = !visible;
            // remontrée : rien à refaire ; cachée : on recommence à chaque pas, pour un modèle choisi pendant le clignotement
            if (caches.TryGetValue(porteur, out bool deja) && deja == cacher && !cacher) return;
            if (!caches.ContainsKey(porteur) && !cacher) return;   // jamais cachée : on n'y touche pas
            foreach (var rendu in porteur.GetComponentsInChildren<Renderer>(true))
                if (rendu != null) rendu.forceRenderingOff = cacher;
            caches[porteur] = cacher;
        }
    }
}
