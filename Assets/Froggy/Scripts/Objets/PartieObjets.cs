using System.Collections.Generic;
using System.Linq;
using Froggy.Arene;
using Froggy.Deplacement;
using Froggy.Socle;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Froggy.Objets
{
    /// <summary>
    /// Fait tourner les règles des objets dans une scène (P4-01), sans réseau : une boîte mystère à chaque repère
    /// « Boîte à objets » (RepereArene, posé avec le Bâtisseur), prise par la Froggy sans objet qui passe à portée. Les
    /// règles sont dans ReglesObjets ; ce composant leur donne les positions, montre les boîtes, tient PorteurObjet à
    /// jour et émet l'événement « objet utilisé » du socle.
    /// La boîte affichée est provisoire (un cube) tant que « visuel » est vide : P7-06 fournit la vraie.
    /// L'emplacement d'objet à l'écran est provisoire aussi : le HUD est P6-02.
    /// La touche d'utilisation (F, ou le bouton du haut de la manette) est une PROPOSITION, pas une décision.
    /// </summary>
    public class PartieObjets : MonoBehaviour
    {
        [SerializeField] private ReglagesObjets reglages;
        [Tooltip("Le préfabriqué de la boîte mystère (P7-06). Vide : un cube provisoire.")]
        [SerializeField] private GameObject visuel;
        [Tooltip("Affiche l'emplacement d'objet en bas à gauche, en attendant le HUD (P6-02).")]
        [SerializeField] private bool affichageProvisoire = true;

        private const float DureeAnnonce = 1.5f;

        private readonly List<PorteurObjet> porteurs = new List<PorteurObjet>();
        private readonly List<GameObject> boites = new List<GameObject>();
        private readonly List<FicheObjet> fiches = new List<FicheObjet>();
        private readonly List<(int, Vector3)> positions = new List<(int, Vector3)>();
        private readonly List<ReglesObjets.Prise> prises = new List<ReglesObjets.Prise>();
        private InputAction utiliser;
        private PorteurObjet local;
        private string annonce;
        private float finAnnonce;
        private GUIStyle style;

        public ReglesObjets Regles { get; private set; }
        public IReadOnlyList<PorteurObjet> Porteurs => porteurs;

        /// <summary>La Froggy du joueur de cette machine : celle que la touche d'utilisation commande.</summary>
        public PorteurObjet Local => local;

        /// <summary>L'action « utiliser l'objet » : le HUD (P6-02) y lit la touche à afficher.</summary>
        public InputAction ActionUtiliser => utiliser;

        /// <summary>L'emplacement d'objet provisoire à l'écran : le HUD (P6-02) l'éteint quand il prend sa place.</summary>
        public bool AffichageProvisoire { get => affichageProvisoire; set => affichageProvisoire = value; }

        /// <summary>
        /// En réseau, chez un joueur qui n'est pas l'hôte (#128) : rien ne se décide ici (ni prise, ni utilisation) ;
        /// ObjetsReseau montre les boîtes et les objets reçus de l'hôte.
        /// </summary>
        public bool Miroir { get; set; }

        /// <summary>En Miroir : la touche demande l'utilisation à l'hôte (ObjetsReseau). Vide : on utilise ici.</summary>
        public System.Action<PorteurObjet> DemandeUtiliser { get; set; }

        /// <summary>Pour un test : des réglages à soi (une copie, jamais l'asset partagé).</summary>
        public void Regler(ReglagesObjets r) => reglages = r;

        private void OnEnable()
        {
            SceneManager.sceneLoaded += QuandUneSceneArrive;
            if (utiliser == null)
            {
                utiliser = Froggy.Interface.Commandes.Creer("UtiliserObjet");   // F ou bouton nord, réaffectable (P6-05)
            }
            utiliser.Enable();
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= QuandUneSceneArrive;
            utiliser?.Disable();
        }

        private void OnDestroy() => Froggy.Interface.Commandes.Liberer(utiliser);

        private void Start() => Demarrer();

        // le décor (et ses repères, ses Froggy) peut arriver après cette scène : on repart de ce qu'il apporte
        private void QuandUneSceneArrive(Scene scene, LoadSceneMode mode) => Demarrer();

        /// <summary>(Re)lance la partie avec les repères de boîtes et les Froggy des scènes chargées.</summary>
        public void Demarrer()
        {
            var reperes = FindObjectsByType<RepereArene>().Where(r => r.sorte == RepereArene.Sorte.Boite);
            var froggys = FindObjectsByType<DeplacementWow>().Select(f => f.gameObject);
            Demarrer(reperes, froggys);
        }

        /// <summary>
        /// (Re)lance la partie avec ces repères et ces Froggy (un test donne les siens). « garderOrdre » : les numéros
        /// suivent l'ordre donné (en réseau, celui du salon, le même partout) au lieu des positions.
        /// </summary>
        public void Demarrer(IEnumerable<RepereArene> reperes, IEnumerable<GameObject> froggys, bool garderOrdre = false)
        {
            if (reglages == null) reglages = ScriptableObject.CreateInstance<ReglagesObjets>();
            foreach (var b in boites) if (b != null) Detruire(b);
            boites.Clear();
            porteurs.Clear();
            local = null;
            annonce = null;

            fiches.Clear();
            fiches.AddRange(reglages.objets.Where(f => f != null));

            // un ordre qui ne dépend pas de la machine : les numéros de points et de joueurs sont les mêmes partout
            var points = reperes.Select(r => r.transform.position + Vector3.up * reglages.hauteur)
                .OrderBy(p => p.x).ThenBy(p => p.z).ThenBy(p => p.y).ToList();
            int graine = reglages.graine != 0 ? reglages.graine : System.Environment.TickCount | 1;
            Regles = new ReglesObjets(points, fiches.Select(f => f.poids).ToList(), fiches.Select(f => f.recharge).ToList(),
                reglages.rayonRamassage, reglages.delaiReapparition, graine);
            foreach (var p in points) boites.Add(Montrer(p));

            // même ordre que PartieCookies : une Froggy a le même numéro pour les cookies et pour les objets
            var ordre = garderOrdre ? froggys : froggys.OrderBy(f => f.transform.position.x).ThenBy(f => f.transform.position.z);
            foreach (var froggy in ordre)
            {
                var porteur = froggy.GetComponent<PorteurObjet>();
                if (porteur == null) porteur = froggy.AddComponent<PorteurObjet>();
                porteurs.Add(porteur);
                porteur.Numeroter(porteurs.Count);
                porteur.Vider();
                // un PlayerInput allumé : les bots (P3-05) en gardent un éteint, exigé par FroggyApparence
                var entree = froggy.GetComponent<PlayerInput>();
                if (local == null && entree != null && entree.enabled) local = porteur;
            }
            if (local == null && porteurs.Count > 0) local = porteurs[0];
        }

        private void Update()
        {
            if (local == null || utiliser == null || !utiliser.WasPressedThisFrame()) return;
            if (DemandeUtiliser != null) DemandeUtiliser(local);
            else Utiliser(local);
        }

        private void FixedUpdate() => Pas(Time.fixedDeltaTime);

        /// <summary>Un pas de la partie, au pas physique (un test l'appelle hors jeu).</summary>
        public void Pas(float dt)
        {
            if (Regles == null || Miroir) return;
            positions.Clear();
            foreach (var p in porteurs)
                if (p != null && p.isActiveAndEnabled) positions.Add((p.Joueur, p.transform.position));
            prises.Clear();
            Regles.Pas(dt, positions, prises);

            foreach (var p in porteurs)
                if (p != null) p.Fixer(Fiche(Regles.Objet(p.Joueur)), Regles.Recharge(p.Joueur));
            foreach (var prise in prises)
                if (porteurs[prise.Joueur - 1] == local) Annoncer($"{fiches[prise.Objet].nom} !");
            for (int i = 0; i < boites.Count; i++)
                if (boites[i] != null && boites[i].activeSelf != Regles.Present(i)) boites[i].SetActive(Regles.Present(i));
        }

        /// <summary>
        /// Cette Froggy utilise son objet (la touche du joueur local, un bot, et en réseau l'hôte à la demande d'un
        /// client). Renvoie l'objet utilisé, ou rien si elle n'en a pas ou si elle est en recharge.
        /// </summary>
        public FicheObjet Utiliser(PorteurObjet porteur)
        {
            if (Regles == null || Miroir || porteur == null || !porteurs.Contains(porteur)) return null;
            var entrave = porteur.GetComponent<Modificateurs>();
            if (entrave != null && entrave.Entrave) return null;   // gelé : l'objet est gardé pour plus tard
            int objet = Regles.Utiliser(porteur.Joueur);
            if (objet == ReglesObjets.Aucun) return null;
            FicheObjet fiche = fiches[objet];
            porteur.Fixer(null, Regles.Recharge(porteur.Joueur));
            porteur.Signaler(fiche);
            EvenementsPartie.Emettre(TypeEvenement.ObjetUtilise, porteur.gameObject);
            if (porteur == local) Annoncer($"{fiche.nom} utilisé");
            return fiche;
        }

        /// <summary>La fiche de l'objet de ce rang (celui des règles), ou null.</summary>
        public FicheObjet Fiche(int objet) => objet < 0 || objet >= fiches.Count ? null : fiches[objet];

        /// <summary>Le rang de cette fiche dans les règles, ou ReglesObjets.Aucun.</summary>
        public int Rang(FicheObjet fiche) => fiche != null ? fiches.IndexOf(fiche) : ReglesObjets.Aucun;

        /// <summary>En Miroir : montre les boîtes pleines reçues de l'hôte (dans l'ordre des points).</summary>
        public void MontrerBoites(IReadOnlyList<bool> pleines)
        {
            for (int i = 0; i < boites.Count; i++)
            {
                bool voir = i < pleines.Count && pleines[i];
                if (boites[i] != null && boites[i].activeSelf != voir) boites[i].SetActive(voir);
            }
        }

        private void Annoncer(string texte)
        {
            annonce = texte;
            finAnnonce = Time.time + DureeAnnonce;
        }

        private GameObject Montrer(Vector3 position)
        {
            GameObject boite;
            if (visuel != null) boite = Instantiate(visuel, position, Quaternion.identity, transform);
            else
            {
                boite = GameObject.CreatePrimitive(PrimitiveType.Cube);
                boite.transform.SetParent(transform, false);
                boite.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, 45f, 0f));
                boite.transform.localScale = Vector3.one * 0.7f;
                Detruire(boite.GetComponent<Collider>());   // on la traverse : c'est la distance qui prend
                var couleur = new MaterialPropertyBlock();
                couleur.SetColor("_BaseColor", RepereArene.Couleur(RepereArene.Sorte.Boite, 0));
                boite.GetComponent<Renderer>().SetPropertyBlock(couleur);
            }
            boite.name = "Boîte mystère";
            return boite;
        }

        private static void Detruire(Object o)
        {
            if (Application.isPlaying) Destroy(o);
            else DestroyImmediate(o);
        }

        private void OnGUI()
        {
            if (!affichageProvisoire || Regles == null || local == null) return;
            if (style == null) style = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold };

            string texte = local.Fiche == null ? "Objet : aucun"
                : local.Pret ? $"Objet : {local.Fiche.nom}  ({utiliser?.GetBindingDisplayString()} pour utiliser)"
                : $"Objet : {local.Fiche.nom}";
            if (local.Recharge > 0f) texte += $"  (recharge {local.Recharge:0.0} s)";
            Ecrire(new Rect(14f, Screen.height - 44f, 700f, 30f), texte, local.Fiche != null ? local.Fiche.couleur : Color.white);
            if (annonce != null && Time.time < finAnnonce)
                Ecrire(new Rect(14f, Screen.height - 78f, 700f, 30f), annonce, Color.white);
        }

        private void Ecrire(Rect zone, string texte, Color couleur)
        {
            style.normal.textColor = Color.black;   // contour sombre : lisible sur le ciel comme sur l'herbe
            GUI.Label(new Rect(zone.x + 2f, zone.y + 2f, zone.width, zone.height), texte, style);
            style.normal.textColor = couleur;
            GUI.Label(zone, texte, style);
        }
    }
}
