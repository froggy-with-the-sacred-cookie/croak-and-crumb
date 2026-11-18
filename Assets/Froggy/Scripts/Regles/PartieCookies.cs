using System.Collections.Generic;
using System.Linq;
using Froggy.Arene;
using Froggy.Deplacement;
using Froggy.Socle;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Froggy.Regles
{
    /// <summary>
    /// Fait tourner les règles des cookies dans une scène (P3-01), sans réseau : un cookie à chaque repère « Apparition
    /// de cookies » (RepereArene, posé avec le Bâtisseur), ramassé par la Froggy qui passe à portée. Les règles sont dans
    /// ReglesCookies ; ce composant leur donne les positions, montre les cookies, tient PorteurCookies à jour et émet
    /// les événements « cookie ramassé » et « cookie lâché » du socle.
    /// FaireLacher() fait tomber un cookie d'un joueur (P3-02) : c'est ce que la torgnole (P4-10) appellera.
    /// Le cookie affiché est provisoire (un disque) tant que « visuel » est vide : P7-06 fournit le vrai.
    /// Le score à l'écran est provisoire aussi : le HUD est P6-02.
    /// En réseau (P2-04, PartieReseau) : l'hôte le fait tourner normalement ; chez les autres joueurs il est en
    /// « Miroir » : il ne décide rien et montre l'état reçu de l'hôte (Refleter).
    /// </summary>
    public class PartieCookies : MonoBehaviour
    {
        [SerializeField] private ReglagesCookies reglages;
        [Tooltip("Le préfabriqué du cookie (P7-06). Vide : un disque provisoire.")]
        [SerializeField] private GameObject visuel;
        [Tooltip("Affiche le score en haut à gauche, en attendant le HUD (P6-02).")]
        [SerializeField] private bool scoreProvisoire = true;
        [Tooltip("Démarre seul au début de la scène et à chaque scène chargée. Décoché : quelqu'un appelle Demarrer " +
                 "(en réseau, l'hôte, avec les numéros du salon).")]
        [SerializeField] private bool demarrerSeul = true;

        private static readonly Color CouleurProvisoire = new Color(.86f, .58f, .28f);

        private readonly List<PorteurCookies> porteurs = new List<PorteurCookies>();
        private readonly List<GameObject> cookies = new List<GameObject>();
        private readonly Dictionary<int, GameObject> auSol = new Dictionary<int, GameObject>();   // les cookies lâchés, par numéro
        private readonly List<(int, Vector3)> positions = new List<(int, Vector3)>();
        private readonly List<ReglesCookies.Prise> prises = new List<ReglesCookies.Prise>();
        private GUIStyle style;

        public ReglesCookies Regles { get; private set; }

        /// <summary>En pause, rien ne se ramasse ni ne réapparaît (avant le départ, après la fin : DeroulementPartie).</summary>
        public bool EnPause { get; set; }
        public IReadOnlyList<PorteurCookies> Porteurs => porteurs;

        /// <summary>
        /// En réseau, chez un joueur qui n'est pas l'hôte (P2-04) : rien ne se décide ici (pas de Pas en jeu), l'état
        /// vient de l'hôte par Refleter.
        /// </summary>
        public bool Miroir { get; set; }

        /// <summary>Pour un test : des réglages à soi (une copie, jamais l'asset partagé).</summary>
        public void Regler(ReglagesCookies r) => reglages = r;

        private void OnEnable() => SceneManager.sceneLoaded += QuandUneSceneArrive;
        private void OnDisable() => SceneManager.sceneLoaded -= QuandUneSceneArrive;

        private void Start()
        {
            if (demarrerSeul) Demarrer();
        }

        // le décor (et ses repères, ses Froggy) peut arriver après cette scène : on repart de ce qu'il apporte
        private void QuandUneSceneArrive(Scene scene, LoadSceneMode mode)
        {
            if (demarrerSeul) Demarrer();
        }

        /// <summary>(Re)lance la partie avec les repères de cookies et les Froggy des scènes chargées.</summary>
        public void Demarrer()
        {
            var reperes = FindObjectsByType<RepereArene>().Where(r => r.sorte == RepereArene.Sorte.Cookie);
            // les Froggy, et tout ce qui porte déjà un PorteurCookies (un mannequin de test, plus tard un bot)
            var froggys = FindObjectsByType<DeplacementWow>().Select(f => f.gameObject)
                .Concat(FindObjectsByType<PorteurCookies>().Select(p => p.gameObject)).Distinct();
            Demarrer(reperes, froggys);
        }

        /// <summary>(Re)lance la partie avec ces repères et ces Froggy (un test donne les siens).</summary>
        public void Demarrer(IEnumerable<RepereArene> reperes, IEnumerable<GameObject> froggys)
        {
            Reinitialiser(reperes);
            foreach (var froggy in froggys.OrderBy(f => f.transform.position.x).ThenBy(f => f.transform.position.z))
            {
                var porteur = froggy.GetComponent<PorteurCookies>();
                if (porteur == null) porteur = froggy.AddComponent<PorteurCookies>();
                porteur.Numeroter(porteurs.Count + 1);
                Inscrire(porteur);
            }
        }

        /// <summary>
        /// (Re)lance la partie avec des joueurs déjà numérotés (en réseau : les numéros du salon, P2-04). Les numéros
        /// peuvent avoir des trous (joueurs 1 et 3).
        /// </summary>
        public void DemarrerNumerotes(IEnumerable<RepereArene> reperes, IEnumerable<PorteurCookies> joueurs)
        {
            Reinitialiser(reperes);
            foreach (var porteur in joueurs.Where(p => p != null).OrderBy(p => p.Joueur)) Inscrire(porteur);
        }

        /// <summary>Ajoute un joueur déjà numéroté à la partie en cours (sans effet s'il y est déjà).</summary>
        public void Inscrire(PorteurCookies porteur)
        {
            if (porteur == null || porteurs.Contains(porteur)) return;
            porteurs.Add(porteur);
            porteur.Fixer(Regles != null ? Regles.Portes(porteur.Joueur) : 0, 1f);
        }

        private void Reinitialiser(IEnumerable<RepereArene> reperes)
        {
            if (reglages == null) reglages = ScriptableObject.CreateInstance<ReglagesCookies>();
            foreach (var c in cookies) if (c != null) Detruire(c);
            foreach (var c in auSol.Values) if (c != null) Detruire(c);
            cookies.Clear();
            auSol.Clear();
            porteurs.Clear();

            // un ordre qui ne dépend pas de la machine : les numéros de points et de joueurs sont les mêmes partout
            var points = reperes.Select(r => r.transform.position + Vector3.up * reglages.hauteur)
                .OrderBy(p => p.x).ThenBy(p => p.z).ThenBy(p => p.y).ToList();
            Regles = new ReglesCookies(points, reglages.rayonRamassage, reglages.delaiReapparition, reglages.maxPortes,
                reglages.delaiRepriseLacheur);
            foreach (var p in points) cookies.Add(Montrer(p));
        }

        /// <summary>Le joueur de ce numéro, ou null.</summary>
        public PorteurCookies Trouver(int joueur)
        {
            foreach (var p in porteurs)
                if (p != null && p.Joueur == joueur) return p;
            return null;
        }

        /// <summary>
        /// En Miroir (P2-04) : montre l'état décidé par l'hôte. « presents » : un booléen par point d'apparition ;
        /// « tombes » : les cookies au sol (numéro, position) ; « scores » : les cookies portés par chaque joueur.
        /// </summary>
        public void Refleter(IReadOnlyList<bool> presents, IReadOnlyList<(int numero, Vector3 position)> tombes,
            IReadOnlyList<(int joueur, int cookies)> scores)
        {
            if (Regles == null) return;
            for (int i = 0; i < cookies.Count && i < presents.Count; i++)
                if (cookies[i] != null && cookies[i].activeSelf != presents[i]) cookies[i].SetActive(presents[i]);

            // cookies au sol : retirer ceux qui ne sont plus là, montrer les nouveaux
            var encore = new HashSet<int>();
            foreach (var (numero, position) in tombes)
            {
                encore.Add(numero);
                if (!auSol.ContainsKey(numero)) auSol[numero] = Montrer(position);
            }
            foreach (int numero in auSol.Keys.Where(n => !encore.Contains(n)).ToList())
            {
                if (auSol[numero] != null) Detruire(auSol[numero]);
                auSol.Remove(numero);
            }

            foreach (var (joueur, nombre) in scores)
            {
                var porteur = Trouver(joueur);
                if (porteur != null) porteur.Fixer(nombre, reglages.FacteurVitesse(nombre));
            }
        }

        private void FixedUpdate() => Pas(Time.fixedDeltaTime);

        /// <summary>Un pas de la partie, au pas physique (un test l'appelle hors jeu).</summary>
        public void Pas(float dt)
        {
            if (Regles == null || EnPause || Miroir) return;
            positions.Clear();
            foreach (var p in porteurs)
                if (p != null && p.isActiveAndEnabled) positions.Add((p.Joueur, p.transform.position));
            prises.Clear();
            Regles.Pas(dt, positions, prises);

            foreach (var prise in prises)
            {
                var porteur = Trouver(prise.Joueur);
                if (porteur == null) continue;
                MettreAJour(porteur);
                if (prise.Lache < 0)
                {
                    EvenementsPartie.Emettre(TypeEvenement.CookieRamasse, porteur.gameObject, cookies[prise.Point], Regles.Point(prise.Point));
                    continue;
                }
                // un cookie lâché : il disparaît du sol
                auSol.TryGetValue(prise.Lache, out GameObject tombe);
                Vector3 ou = tombe != null ? tombe.transform.position : porteur.transform.position;
                EvenementsPartie.Emettre(TypeEvenement.CookieRamasse, porteur.gameObject, tombe, ou);
                if (tombe != null) Detruire(tombe);
                auSol.Remove(prise.Lache);
            }
            for (int i = 0; i < cookies.Count; i++)
                if (cookies[i] != null && cookies[i].activeSelf != Regles.Present(i)) cookies[i].SetActive(Regles.Present(i));
        }

        /// <summary>
        /// Fait lâcher un cookie à un joueur (P3-02). Le cookie tombe au sol à « distanceLacher » dans la direction donnée
        /// (celle du coup ; nulle : derrière lui), sans traverser un mur, et va au premier qui le ramasse.
        /// Rend false si le joueur ne porte rien ou n'est pas de la partie.
        /// </summary>
        public bool FaireLacher(PorteurCookies porteur, Vector3 direction)
        {
            if (Regles == null || porteur == null || !porteurs.Contains(porteur)) return false;
            Vector3 chute = PointDeChute(porteur.transform, direction);
            int numero = Regles.Lacher(porteur.Joueur, chute);
            if (numero < 0) return false;
            GameObject cookie = Montrer(chute);
            auSol[numero] = cookie;
            MettreAJour(porteur);
            EvenementsPartie.Emettre(TypeEvenement.CookieLache, porteur.gameObject, cookie, chute);
            return true;
        }

        private void MettreAJour(PorteurCookies porteur)
        {
            int portes = Regles.Portes(porteur.Joueur);
            porteur.Fixer(portes, reglages.FacteurVitesse(portes));
        }

        /// <summary>Où le cookie lâché se pose : devant un mur s'il y en a un, puis au sol sous ce point.</summary>
        private Vector3 PointDeChute(Transform joueur, Vector3 direction)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude < 1e-4f) direction = -joueur.forward;
            direction.y = 0f;
            direction = direction.sqrMagnitude < 1e-4f ? Vector3.back : direction.normalized;

            var physique = joueur.gameObject.scene.GetPhysicsScene();
            Vector3 depart = joueur.position;
            float distance = reglages.distanceLacher;
            // le premier obstacle qui n'est pas le joueur lui-même raccourcit la chute
            int n = physique.Raycast(depart, direction, touches, distance, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
                if (!touches[i].transform.IsChildOf(joueur)) distance = Mathf.Min(distance, Mathf.Max(0f, touches[i].distance - 0.3f));
            Vector3 dessus = depart + direction * distance;

            float sol = dessus.y - 0.5f;   // sans sol trouvé : à peu près aux pieds du joueur
            float plusProche = float.MaxValue;
            n = physique.Raycast(dessus + Vector3.up * 0.5f, Vector3.down, touches, 30f, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
                if (!touches[i].transform.IsChildOf(joueur) && touches[i].distance < plusProche) { plusProche = touches[i].distance; sol = touches[i].point.y; }
            return new Vector3(dessus.x, sol + reglages.hauteur, dessus.z);
        }

        private readonly RaycastHit[] touches = new RaycastHit[16];

        private GameObject Montrer(Vector3 position)
        {
            GameObject cookie;
            if (visuel != null) cookie = Instantiate(visuel, position, Quaternion.identity, transform);
            else
            {
                cookie = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                cookie.transform.SetParent(transform, false);
                cookie.transform.SetPositionAndRotation(position, Quaternion.Euler(90f, 0f, 0f));   // debout, face visible de loin
                cookie.transform.localScale = new Vector3(0.6f, 0.06f, 0.6f);
                Detruire(cookie.GetComponent<Collider>());   // on le traverse : c'est la distance qui ramasse
                var couleur = new MaterialPropertyBlock();
                couleur.SetColor("_BaseColor", CouleurProvisoire);
                cookie.GetComponent<Renderer>().SetPropertyBlock(couleur);
            }
            cookie.name = "Cookie";
            return cookie;
        }

        private static void Detruire(Object o)
        {
            if (Application.isPlaying) Destroy(o);
            else DestroyImmediate(o);
        }

        private void OnGUI()
        {
            if (!scoreProvisoire || Regles == null) return;
            if (style == null) style = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold };
            float y = 12f;
            foreach (var p in porteurs)
            {
                if (p == null) continue;
                var zone = new Rect(14f, y, 420f, 30f);
                string texte = $"Joueur {p.Joueur} : {p.Nombre} cookie{(p.Nombre > 1 ? "s" : "")}";
                style.normal.textColor = Color.black;   // contour sombre : lisible sur le ciel comme sur l'herbe
                GUI.Label(new Rect(zone.x + 2f, zone.y + 2f, zone.width, zone.height), texte, style);
                style.normal.textColor = Color.white;
                GUI.Label(zone, texte, style);
                y += 30f;
            }
        }
    }
}
