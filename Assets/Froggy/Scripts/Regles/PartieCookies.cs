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
    /// l'événement « cookie ramassé » du socle.
    /// Le cookie affiché est provisoire (un disque) tant que « visuel » est vide : P7-06 fournit le vrai.
    /// Le score à l'écran est provisoire aussi : le HUD est P6-02.
    /// </summary>
    public class PartieCookies : MonoBehaviour
    {
        [SerializeField] private ReglagesCookies reglages;
        [Tooltip("Le préfabriqué du cookie (P7-06). Vide : un disque provisoire.")]
        [SerializeField] private GameObject visuel;
        [Tooltip("Affiche le score en haut à gauche, en attendant le HUD (P6-02).")]
        [SerializeField] private bool scoreProvisoire = true;

        private static readonly Color CouleurProvisoire = new Color(.86f, .58f, .28f);

        private readonly List<PorteurCookies> porteurs = new List<PorteurCookies>();
        private readonly List<GameObject> cookies = new List<GameObject>();
        private readonly List<(int, Vector3)> positions = new List<(int, Vector3)>();
        private readonly List<ReglesCookies.Prise> prises = new List<ReglesCookies.Prise>();
        private GUIStyle style;

        public ReglesCookies Regles { get; private set; }
        public IReadOnlyList<PorteurCookies> Porteurs => porteurs;

        /// <summary>Pour un test : des réglages à soi (une copie, jamais l'asset partagé).</summary>
        public void Regler(ReglagesCookies r) => reglages = r;

        private void OnEnable() => SceneManager.sceneLoaded += QuandUneSceneArrive;
        private void OnDisable() => SceneManager.sceneLoaded -= QuandUneSceneArrive;

        private void Start() => Demarrer();

        // le décor (et ses repères, ses Froggy) peut arriver après cette scène : on repart de ce qu'il apporte
        private void QuandUneSceneArrive(Scene scene, LoadSceneMode mode) => Demarrer();

        /// <summary>(Re)lance la partie avec les repères de cookies et les Froggy des scènes chargées.</summary>
        public void Demarrer()
        {
            var reperes = FindObjectsByType<RepereArene>().Where(r => r.sorte == RepereArene.Sorte.Cookie);
            var froggys = FindObjectsByType<DeplacementWow>().Select(f => f.gameObject);
            Demarrer(reperes, froggys);
        }

        /// <summary>(Re)lance la partie avec ces repères et ces Froggy (un test donne les siens).</summary>
        public void Demarrer(IEnumerable<RepereArene> reperes, IEnumerable<GameObject> froggys)
        {
            if (reglages == null) reglages = ScriptableObject.CreateInstance<ReglagesCookies>();
            foreach (var c in cookies) if (c != null) Detruire(c);
            cookies.Clear();
            porteurs.Clear();

            // un ordre qui ne dépend pas de la machine : les numéros de points et de joueurs sont les mêmes partout
            var points = reperes.Select(r => r.transform.position + Vector3.up * reglages.hauteur)
                .OrderBy(p => p.x).ThenBy(p => p.z).ThenBy(p => p.y).ToList();
            Regles = new ReglesCookies(points, reglages.rayonRamassage, reglages.delaiReapparition, reglages.maxPortes);
            foreach (var p in points) cookies.Add(Montrer(p));

            foreach (var froggy in froggys.OrderBy(f => f.transform.position.x).ThenBy(f => f.transform.position.z))
            {
                var porteur = froggy.GetComponent<PorteurCookies>();
                if (porteur == null) porteur = froggy.AddComponent<PorteurCookies>();
                porteurs.Add(porteur);
                porteur.Numeroter(porteurs.Count);
                porteur.Fixer(0, 1f);
            }
        }

        private void FixedUpdate() => Pas(Time.fixedDeltaTime);

        /// <summary>Un pas de la partie, au pas physique (un test l'appelle hors jeu).</summary>
        public void Pas(float dt)
        {
            if (Regles == null) return;
            positions.Clear();
            foreach (var p in porteurs)
                if (p != null && p.isActiveAndEnabled) positions.Add((p.Joueur, p.transform.position));
            prises.Clear();
            Regles.Pas(dt, positions, prises);

            foreach (var prise in prises)
            {
                var porteur = porteurs[prise.Joueur - 1];
                int portes = Regles.Portes(prise.Joueur);
                porteur.Fixer(portes, reglages.FacteurVitesse(portes));
                EvenementsPartie.Emettre(TypeEvenement.CookieRamasse, porteur.gameObject, cookies[prise.Point], Regles.Point(prise.Point));
            }
            for (int i = 0; i < cookies.Count; i++)
                if (cookies[i] != null && cookies[i].activeSelf != Regles.Present(i)) cookies[i].SetActive(Regles.Present(i));
        }

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
