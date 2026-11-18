using System.Collections.Generic;
using System.Linq;
using Froggy.Arene;
using Froggy.Socle;
using UnityEngine;

namespace Froggy.Regles
{
    /// <summary>
    /// Le cookie sacré et son gobelin dans une scène (P5-08), sans réseau, comme PartieCookies : les règles sont dans
    /// ReglesCookieSacre ; ce composant leur donne le temps de la partie (DeroulementPartie) et les joueurs (PartieCookies),
    /// fait apparaître le gobelin au repère « Départ du gobelin » (sinon à côté du premier joueur), et place le cookie
    /// sacré : entre les mains du gobelin, au-dessus de la tête de son porteur, ou au sol. Il tient
    /// DeroulementPartie.PorteurCookieSacre à jour : à la fin, le porteur gagne (ReglesPartie).
    /// Lâcher : une torgnole (« coup donné », P4-10) le fait tomber, même à qui n'a pas d'autre cookie ; un joueur qui lâche
    /// un cookie pour une autre raison (« cookie lâché », P3-02) le lâche aussi ; le gel éclair appellera FaireLacher().
    /// Un joueur intouchable (P3-04) ne le lâche pas. Une nouvelle partie (« Rejouer ») le remet à zéro.
    /// En réseau (P2-04), chez un joueur qui n'est pas l'hôte (DeroulementPartie en « Miroir »), il ne décide rien, comme
    /// PartieCookies : CookieSacreReseau lui envoie l'état de l'hôte, qu'il montre par Refleter (#128).
    /// Événement émis : « cookie sacré » (socle), à l'apparition (auteur : le gobelin), puis à chaque fois qu'il change de
    /// main ou tombe (auteur : le joueur). Le message à l'écran est provisoire, en attendant le HUD (P6-02).
    /// </summary>
    public class PartieCookieSacre : MonoBehaviour
    {
        [SerializeField] private ReglagesCookieSacre reglages;
        [Tooltip("Le gobelin (P7-07). Vide : une capsule provisoire.")]
        [SerializeField] private GameObject gobelin;
        [Tooltip("Le cookie sacré (P7-06, Cookie_Sacre). Vide : un disque doré provisoire.")]
        [SerializeField] private GameObject cookieSacre;
        [Tooltip("Largeur du cookie entre les mains du gobelin, en m (le modèle est mis à cette taille).")]
        [SerializeField] private float largeurDansLesMains = 0.24f;
        [Tooltip("Largeur du cookie porté au-dessus de la tête, et au sol, en m.")]
        [SerializeField] private float largeurPorte = 0.4f;
        [Tooltip("Où le gobelin apparaît. Vide : le repère « Départ du gobelin » (Bâtisseur) des scènes chargées.")]
        [SerializeField] private Transform depart;
        [SerializeField] private DeroulementPartie deroulement;
        [SerializeField] private PartieCookies cookies;
        [Tooltip("Affiche où est le cookie sacré, en attendant le HUD (P6-02).")]
        [SerializeField] private bool messageProvisoire = true;

        private readonly List<(int, Vector3)> joueurs = new List<(int, Vector3)>();
        private readonly List<Vector3> menaces = new List<Vector3>();
        private readonly List<(ReglesCookieSacre.Changement, int)> changements = new List<(ReglesCookieSacre.Changement, int)>();
        private readonly RaycastHit[] touches = new RaycastHit[16];
        private FuiteGobelin leGobelin;
        private GameObject leCookie;
        private float largeurModele = 1f;
        private Vector3 centreModele;   // le centre des maillages du cookie, depuis son origine (Cookie_Sacre : à sa base)
        private ReglesPartie partieVue;   // la partie en cours : si DeroulementPartie en refait une (« Rejouer »), on repart de zéro
        // en Miroir (réseau) : ce qui est déjà montré, pour ne replacer le cookie qu'à un changement
        private EtatCookieSacre etatMontre = EtatCookieSacre.Absent;
        private int porteurMontre = ReglesPartie.Personne;
        private Vector3 solMontre;
        private readonly List<Vector3> aucuneMenace = new List<Vector3>();
        private GUIStyle style;

        public ReglesCookieSacre Regles { get; private set; }
        public FuiteGobelin Gobelin => leGobelin;
        public GameObject Cookie => leCookie;

        /// <summary>Le message provisoire à l'écran : le HUD (P6-02) l'éteint quand il prend sa place.</summary>
        public bool MessageProvisoire { get => messageProvisoire; set => messageProvisoire = value; }

        /// <summary>Pour un test : des réglages, une partie, des cookies et un départ du gobelin à soi.</summary>
        public void Regler(ReglagesCookieSacre r, DeroulementPartie d, PartieCookies c, Transform departGobelin = null)
        {
            reglages = r; deroulement = d; cookies = c; depart = departGobelin;
        }

        private void OnEnable() => EvenementsPartie.Emis += Reagir;
        private void OnDisable() => EvenementsPartie.Emis -= Reagir;

        private void Start() => Preparer();

        /// <summary>Remet le cookie sacré à zéro : pas de gobelin, personne ne le porte.</summary>
        public void Preparer()
        {
            if (reglages == null) reglages = ScriptableObject.CreateInstance<ReglagesCookieSacre>();
            if (deroulement == null) deroulement = FindAnyObjectByType<DeroulementPartie>();
            if (cookies == null) cookies = FindAnyObjectByType<PartieCookies>();
            Regles = new ReglesCookieSacre(reglages.moment, reglages.rayonCapture, reglages.rayonRamassage, reglages.delaiReprise);
            if (leGobelin != null) Detruire(leGobelin.gameObject);
            if (leCookie != null) Detruire(leCookie);
            leGobelin = null; leCookie = null;
            etatMontre = EtatCookieSacre.Absent; porteurMontre = ReglesPartie.Personne;
            if (deroulement != null) deroulement.PorteurCookieSacre = null;
        }

        private void FixedUpdate() => Pas(Time.fixedDeltaTime);

        /// <summary>Un pas, au pas physique, après celui de la partie (un test l'appelle hors jeu).</summary>
        public void Pas(float dt)
        {
            if (Regles == null) Preparer();
            if (deroulement == null || deroulement.Regles == null || cookies == null) return;
            if (deroulement.Miroir) return;   // en réseau, seul l'hôte décide (P2-04)
            if (deroulement.Regles != partieVue)
            {
                if (partieVue != null) Preparer();
                partieVue = deroulement.Regles;
            }
            joueurs.Clear(); menaces.Clear();
            foreach (var p in cookies.Porteurs)
                if (p != null && p.isActiveAndEnabled) { joueurs.Add((p.Joueur, p.transform.position)); menaces.Add(p.transform.position); }

            Phase phase = deroulement.Regles.Phase;
            changements.Clear();
            Vector3 ou = leGobelin != null ? leGobelin.transform.position : Vector3.zero;
            Regles.Pas(dt, phase, deroulement.Regles.Reste, ou, joueurs, changements);

            foreach (var (quoi, joueur) in changements)
            {
                switch (quoi)
                {
                    case ReglesCookieSacre.Changement.Apparu:
                        Apparaitre();
                        break;
                    case ReglesCookieSacre.Changement.Attrape:
                    case ReglesCookieSacre.Changement.Ramasse:
                        var porteur = Porteur(joueur);
                        if (quoi == ReglesCookieSacre.Changement.Attrape && leGobelin != null) leGobelin.Attraper();
                        Porter(porteur);
                        deroulement.PorteurCookieSacre = porteur;
                        EvenementsPartie.Emettre(TypeEvenement.CookieSacre, porteur != null ? porteur.gameObject : null, leCookie);
                        break;
                }
            }
            if (leGobelin != null && leGobelin.isActiveAndEnabled)
                leGobelin.Pas(dt, menaces, bouger: phase == Phase.EnJeu || phase == Phase.Prolongation);
        }

        /// <summary>
        /// Fait lâcher le cookie sacré à son porteur (gel éclair, malus). Il tombe à « distanceLacher » dans la direction
        /// donnée (nulle : derrière lui), sans traverser un mur. Rend false si ce joueur ne le portait pas ou s'il est
        /// intouchable (il vient de réapparaître, P3-04), ou en Miroir (réseau, pas l'hôte), comme PartieCookies.FaireLacher.
        /// </summary>
        public bool FaireLacher(PorteurCookies porteur, Vector3 direction)
        {
            if (Regles == null || porteur == null || porteur.Intouchable || (deroulement != null && deroulement.Miroir)) return false;
            Vector3 chute = PointDeChute(porteur.transform, direction);
            if (!Regles.Lacher(porteur.Joueur, chute)) return false;
            Poser(chute);
            if (deroulement != null) deroulement.PorteurCookieSacre = null;
            EvenementsPartie.Emettre(TypeEvenement.CookieSacre, porteur.gameObject, leCookie, chute);
            return true;
        }

        /// <summary>
        /// En réseau, chez un joueur qui n'est pas l'hôte (#128) : montre l'état décidé par l'hôte (gobelin, cookie sacré,
        /// porteur), sans rien décider ni émettre : les événements « cookie sacré » arrivent déjà par PartieReseau.
        /// Appelé à chaque image par CookieSacreReseau.
        /// </summary>
        public void Refleter(EtatCookieSacre etat, int porteur, Vector3 auSol, Vector3 gobelinIci, float gobelinCap,
            bool gobelinAttrape, float dt)
        {
            if (Regles == null) Preparer();
            if (etat == EtatCookieSacre.Absent)
            {
                if (Regles.Etat != EtatCookieSacre.Absent || leGobelin != null) Preparer();   // une nouvelle partie
                return;
            }
            if (leGobelin == null && leCookie == null) Apparaitre(gobelinIci, emettre: false);
            Regles.Imposer(etat, porteur, auSol);

            bool change = etat != etatMontre || porteur != porteurMontre || (etat == EtatCookieSacre.AuSol && auSol != solMontre);
            if (change)
            {
                bool montre = true;
                switch (etat)
                {
                    case EtatCookieSacre.Porte:
                        var qui = Porteur(porteur);
                        if (qui == null) { montre = false; break; }   // sa Froggy n'est pas encore là : on réessaie
                        if (etatMontre == EtatCookieSacre.Gobelin && leGobelin != null) leGobelin.Attraper();
                        Porter(qui);
                        if (deroulement != null) deroulement.PorteurCookieSacre = qui;
                        break;
                    case EtatCookieSacre.AuSol:
                        Poser(auSol);
                        if (deroulement != null) deroulement.PorteurCookieSacre = null;
                        break;
                }
                if (montre) { etatMontre = etat; porteurMontre = porteur; solMontre = auSol; }
            }

            if (leGobelin != null && leGobelin.gameObject.activeSelf)
            {
                if (gobelinAttrape) leGobelin.Attraper();
                leGobelin.Pas(dt, aucuneMenace, bouger: false);   // apparition, attrapé, départ : comme chez l'hôte
                if (leGobelin.gameObject.activeSelf && leGobelin.EtatActuel != FuiteGobelin.Etat.Attrape)
                    leGobelin.Suivre(gobelinIci, Quaternion.Euler(0f, gobelinCap, 0f), dt);
            }
        }

        /// <summary>
        /// Écoute les événements du socle (un test l'appelle directement). Une torgnole sur le porteur (« coup donné »,
        /// « Objet » le touché) ou un cookie lâché par le porteur (« cookie lâché ») font tomber le cookie sacré, à 40° de
        /// la direction du coup : il ne se pose pas sur le cookie ordinaire qui tombe avec lui.
        /// </summary>
        public void Reagir(EvenementPartie e)
        {
            if (Regles == null || Regles.Etat != EtatCookieSacre.Porte) return;
            PorteurCookies porteur = null;
            Vector3 vers = Vector3.zero;
            if (e.Type == TypeEvenement.CoupDonne && e.Objet != null && e.Auteur != null)
            {
                porteur = e.Objet.GetComponent<PorteurCookies>();
                if (porteur != null) vers = porteur.transform.position - e.Auteur.transform.position;
            }
            else if (e.Type == TypeEvenement.CookieLache && e.Auteur != null)
            {
                porteur = e.Auteur.GetComponent<PorteurCookies>();
                if (porteur != null) vers = e.Position - porteur.transform.position;
            }
            if (porteur == null || porteur.Joueur != Regles.Porteur) return;
            FaireLacher(porteur, Quaternion.Euler(0f, 40f, 0f) * vers);
        }

        private PorteurCookies Porteur(int joueur) => cookies.Porteurs.FirstOrDefault(p => p != null && p.Joueur == joueur);

        private void Apparaitre() => Apparaitre(null, true);

        // « imposee » : en Miroir, là où l'hôte l'a fait apparaître ; « emettre » faux : l'hôte a déjà annoncé l'apparition
        private void Apparaitre(Vector3? imposee, bool emettre)
        {
            Transform repere = depart;
            if (repere == null)
            {
                var trouve = FindObjectsByType<RepereArene>().FirstOrDefault(r => r.sorte == RepereArene.Sorte.Gobelin);
                if (trouve != null) repere = trouve.transform;
            }
            Vector3 ici = imposee ?? (repere != null ? repere.position
                : joueurs.Count > 0 ? joueurs[0].Item2 + new Vector3(0f, 0f, 6f) : transform.position);

            GameObject go = gobelin != null ? Instantiate(gobelin, ici, Quaternion.identity, transform) : CapsuleProvisoire(ici);
            go.name = "Gobelin";
            leGobelin = go.GetComponent<FuiteGobelin>();
            if (leGobelin == null) leGobelin = go.AddComponent<FuiteGobelin>();
            leGobelin.Apparaitre(ici, reglages);

            leCookie = cookieSacre != null ? Instantiate(cookieSacre, transform) : DisqueProvisoire();
            leCookie.name = "Cookie sacré";
            (largeurModele, centreModele) = Mesurer(leCookie);
            // entre ses mains : redressé (Cookie_Sacre penche de 45° en arrière), couronne vers l'avant, centré
            Accrocher(leGobelin.Mains, Vector3.zero, largeurDansLesMains, Quaternion.Euler(45f, 0f, 0f), centrer: true);
            etatMontre = EtatCookieSacre.Gobelin;
            if (emettre) EvenementsPartie.Emettre(TypeEvenement.CookieSacre, go, leCookie, ici);
        }

        /// <summary>Au-dessus de la tête du porteur, à plat et bien visible.</summary>
        private void Porter(PorteurCookies porteur)
        {
            if (leCookie == null || porteur == null) return;
            float haut = 1f;
            var rendus = porteur.GetComponentsInChildren<Renderer>();
            if (rendus.Length > 0) haut = rendus.Max(r => r.bounds.max.y) - porteur.transform.position.y;
            // posé par sa base sur la tête : la couronne du revers se voit de la caméra, derrière le joueur
            Accrocher(porteur.transform, Vector3.up * (haut + 0.05f), largeurPorte, Quaternion.identity, centrer: false);
        }

        private void Poser(Vector3 sol)
        {
            if (leCookie == null) return;
            leCookie.transform.SetParent(transform, true);
            leCookie.transform.SetPositionAndRotation(sol, Quaternion.identity);
            leCookie.transform.localScale = Vector3.one * (largeurPorte / largeurModele);
            Balancer(true);
        }

        private void Accrocher(Transform parent, Vector3 decalage, float largeur, Quaternion rotation, bool centrer)
        {
            leCookie.transform.SetParent(parent, false);
            Vector3 echelle = parent.lossyScale;
            Vector3 inverse = new Vector3(1f / Mathf.Max(1e-4f, echelle.x), 1f / Mathf.Max(1e-4f, echelle.y), 1f / Mathf.Max(1e-4f, echelle.z));
            float s = largeur / largeurModele;
            Vector3 monde = decalage - (centrer ? rotation * (centreModele * s) : Vector3.zero);
            leCookie.transform.localRotation = rotation;
            leCookie.transform.localScale = inverse * s;
            leCookie.transform.localPosition = Vector3.Scale(monde, inverse);
            Balancer(false);   // tenu ou porté : le balancement (qui peut animer sa racine) ne joue qu'au sol
        }

        private void Balancer(bool oui)
        {
            foreach (var a in leCookie.GetComponentsInChildren<Animator>()) a.enabled = oui;
        }

        /// <summary>La largeur du cookie à l'échelle 1 et son centre depuis son origine, d'après ses maillages (sans les effets).</summary>
        private static (float, Vector3) Mesurer(GameObject cookie)
        {
            var b = new Bounds(cookie.transform.position, Vector3.zero);
            bool trouve = false;
            foreach (var r in cookie.GetComponentsInChildren<MeshRenderer>())
            {
                if (!trouve) { b = r.bounds; trouve = true; } else b.Encapsulate(r.bounds);
            }
            float l = trouve ? Mathf.Max(b.size.x, b.size.z, b.size.y) : 0.6f;
            Vector3 centre = trouve ? Quaternion.Inverse(cookie.transform.rotation) * (b.center - cookie.transform.position) : Vector3.zero;
            return (l > 1e-3f ? l : 0.6f, centre);
        }

        /// <summary>Où le cookie sacré tombe : devant un mur s'il y en a un, puis au sol (comme PartieCookies).</summary>
        private Vector3 PointDeChute(Transform joueur, Vector3 direction)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude < 1e-4f) direction = -joueur.forward;
            direction.y = 0f;
            direction = direction.sqrMagnitude < 1e-4f ? Vector3.back : direction.normalized;
            var physique = joueur.gameObject.scene.GetPhysicsScene();
            float distance = reglages.distanceLacher;
            int n = physique.Raycast(joueur.position, direction, touches, distance, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
                if (!touches[i].transform.IsChildOf(joueur)) distance = Mathf.Min(distance, Mathf.Max(0f, touches[i].distance - 0.3f));
            Vector3 dessus = joueur.position + direction * distance;
            float sol = dessus.y - 0.5f, plusProche = float.MaxValue;
            n = physique.Raycast(dessus + Vector3.up * 0.5f, Vector3.down, touches, 30f, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
                if (!touches[i].transform.IsChildOf(joueur) && touches[i].distance < plusProche) { plusProche = touches[i].distance; sol = touches[i].point.y; }
            return new Vector3(dessus.x, sol + 0.1f, dessus.z);   // Cookie_Sacre a son origine à sa base
        }

        // une racine à l'échelle 1 (le cookie s'y accroche sans se déformer), une capsule et le point des mains
        private GameObject CapsuleProvisoire(Vector3 ici)
        {
            var racine = new GameObject("Gobelin");
            racine.transform.SetParent(transform, false);
            racine.transform.position = ici;
            var corps = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            corps.transform.SetParent(racine.transform, false);
            corps.transform.localPosition = new Vector3(0f, 0.4f, 0f);
            corps.transform.localScale = new Vector3(0.5f, 0.4f, 0.5f);
            Detruire(corps.GetComponent<Collider>());
            var mains = new GameObject("Mains").transform;
            mains.SetParent(racine.transform, false);
            mains.localPosition = new Vector3(0f, 0.45f, 0.3f);
            racine.AddComponent<FuiteGobelin>().Mains = mains;
            return racine;
        }

        private GameObject DisqueProvisoire()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.transform.localScale = new Vector3(0.6f, 0.05f, 0.6f);
            Detruire(go.GetComponent<Collider>());
            var couleur = new MaterialPropertyBlock();
            couleur.SetColor("_BaseColor", new Color(1f, .8f, .2f));
            go.GetComponent<Renderer>().SetPropertyBlock(couleur);
            var parent = new GameObject("Cookie sacré");
            parent.transform.SetParent(transform, false);
            go.transform.SetParent(parent.transform, false);
            return parent;
        }

        private static void Detruire(Object o)
        {
            if (Application.isPlaying) Destroy(o);
            else DestroyImmediate(o);
        }

        private void OnGUI()
        {
            if (!messageProvisoire || Regles == null || Regles.Etat == EtatCookieSacre.Absent) return;
            if (style == null) style = new GUIStyle(GUI.skin.label) { fontSize = 24, fontStyle = FontStyle.Bold, alignment = TextAnchor.UpperCenter };
            string texte = Regles.Etat switch
            {
                EtatCookieSacre.Gobelin => "Le gobelin s'enfuit avec le cookie sacré : attrapez-le !",
                EtatCookieSacre.Porte => $"Joueur {Regles.Porteur} porte le cookie sacré",
                _ => "Le cookie sacré est tombé !",
            };
            var zone = new Rect(0f, 50f, Screen.width, 36f);
            style.normal.textColor = Color.black;   // contour sombre, comme les autres messages provisoires
            GUI.Label(new Rect(zone.x + 2f, zone.y + 2f, zone.width, zone.height), texte, style);
            style.normal.textColor = new Color(1f, .85f, .3f);
            GUI.Label(zone, texte, style);
        }
    }
}
