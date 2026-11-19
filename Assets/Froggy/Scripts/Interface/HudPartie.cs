using System.Collections.Generic;
using Froggy.Arene;
using Froggy.Meteo;
using Froggy.Objets;
using Froggy.Regles;
using Froggy.Reseau;
using Froggy.Socle;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Froggy.Interface
{
    /// <summary>
    /// Le HUD de partie (P6-02), version brute du jalon 1 : le score de chaque joueur, le minuteur, l'objet tenu, la
    /// recharge de la torgnole, le cookie sacré (qui le porte, combien de temps encore à le garder), la météo, et l'écran
    /// de fin avec « Rejouer ». L'habillage (charte, icônes) vient au jalon 2 (P6-01).
    ///
    /// Il ne décide rien : il lit ce que les règles des autres postes ont décidé (DeroulementPartie, PartieCookies,
    /// PartieObjets, PartieTorgnoles, PartieCookieSacre, GestionnaireMeteo), et ne montre que ce que la scène a. En
    /// réseau, il lit la partie de PartieReseau, que les joueurs reçoivent de l'hôte (scores, temps, gagnant). Les textes
    /// viennent de ContenuHud (testé) ; ici, on ne fait que photographier la partie et dessiner.
    ///
    /// Il s'installe tout seul au lancement (un objet « HUD de partie » qui survit aux changements de scène), sans
    /// toucher aux scènes ni aux préfabriqués des postes. Il éteint alors les affichages provisoires qu'il remplace et
    /// les rallume quand on le coupe : menu Froggy ▸ HUD de partie (réglage de l'éditeur, pour comparer).
    ///
    /// Habillage du jalon 2 (refonte du 19/11/2026) : VueHud le dessine sur un Canvas (jetons, icônes peintes, étiquettes
    /// penchées, score des autres au-dessus de leur tête). L'ancien dessin IMGUI reste, pour comparer : menu Froggy ▸
    /// HUD de partie habillé (décoché : l'ancien).
    /// </summary>
    [DisallowMultipleComponent]
    public class HudPartie : MonoBehaviour
    {
        private const string CleAffiche = "Froggy.HudPartie.affiche";
        private const float DureeMessage = 1.6f;
        private const float Recherche = 0.5f;      // tant qu'il manque une partie, on la cherche toutes les 0,5 s
        private const float Reference = 1080f;     // les tailles sont écrites pour un écran de 1080 px de haut

        // presque opaques : le projet mélange en espace linéaire, où un fond sombre à 0,84 paraît à moitié transparent
        private static readonly Color Fond = new Color(.06f, .07f, .1f, .94f);
        private static readonly Color FondLocal = new Color(.26f, .28f, .35f, 1f);
        private static readonly Color Or = new Color(1f, .82f, .3f);
        private static readonly Color Gris = new Color(.75f, .77f, .82f);
        private static readonly Color JaugeVide = new Color(1f, 1f, 1f, .18f);
        private static readonly System.Comparison<JoueurHud> ParNumero = (a, b) => a.Joueur.CompareTo(b.Joueur);

        /// <summary>Le HUD de cette partie, s'il y en a un.</summary>
        public static HudPartie Actuel { get; private set; }

        /// <summary>
        /// Montrer le HUD (vrai par défaut). Faux : les affichages provisoires des postes reviennent. Dans l'éditeur, le
        /// choix est gardé d'une session à l'autre (EditorPrefs) ; dans un build, toujours vrai.
        /// </summary>
        public static bool Affiche
        {
            get
            {
#if UNITY_EDITOR
                return UnityEditor.EditorPrefs.GetBool(CleAffiche, true);
#else
                return true;
#endif
            }
            set
            {
#if UNITY_EDITOR
                UnityEditor.EditorPrefs.SetBool(CleAffiche, value);
#endif
                if (Actuel != null) Actuel.enabled = value;
            }
        }

        private const string CleHabille = "Froggy.HudPartie.habille";

        /// <summary>
        /// Le HUD habillé (VueHud, vrai par défaut) ; faux : l'ancien dessin IMGUI du jalon 1, pour comparer (garde-fou 7).
        /// Gardé d'une session à l'autre dans l'éditeur ; toujours vrai dans un build.
        /// </summary>
        public static bool Habille
        {
            get
            {
#if UNITY_EDITOR
                return UnityEditor.EditorPrefs.GetBool(CleHabille, true);
#else
                return true;
#endif
            }
            set
            {
#if UNITY_EDITOR
                UnityEditor.EditorPrefs.SetBool(CleHabille, value);
#endif
            }
        }

        // sans rechargement du domaine à l'entrée en mode Play, le HUD de la partie d'avant resterait
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Vider() => Actuel = null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Installer()
        {
            if (Actuel != null || FindAnyObjectByType<HudPartie>(FindObjectsInactive.Include) != null) return;
            var go = new GameObject("HUD de partie");
            go.SetActive(false);
            DontDestroyOnLoad(go);
            go.AddComponent<HudPartie>().enabled = Affiche;
            go.SetActive(true);
        }

        private DeroulementPartie deroulement;
        private PartieCookies cookies;
        private PartieObjets objets;
        private PartieTorgnoles torgnoles;
        private PartieCookieSacre sacre;
        private GestionnaireMeteo meteo;
        [System.NonSerialized] private float prochaineRecherche;

        private readonly EtatHud etat = new EtatHud();

        // les icônes des touches (P6-05, IconesTouches) ; sans icône, le nom de la touche s'écrit dans le texte
        private Sprite iconeTorgnole, iconeDash, iconeObjet;
        private readonly HashSet<Object> pris = new HashSet<Object>();
        private readonly List<System.Action> aRendre = new List<System.Action>();
        private readonly List<JoueurHud> classement = new List<JoueurHud>();

        [System.NonSerialized] private string messageTorgnole, messageObjet;
        [System.NonSerialized] private float finTorgnole, finObjet;
        [System.NonSerialized] private FicheObjet ficheVue;

        // le HUD habillé et ce qu'il lit en plus de l'état
        private VueHud vue;
        private readonly DonneesVueHud donneesVue = new DonneesVueHud();
        private readonly Dictionary<Transform, Renderer> rendus = new Dictionary<Transform, Renderer>();
        private System.Action rejouer;

        private GUIStyle texte, petit, gauche, droite, grand, enorme, bouton;
        private float echelleStyles = -1f, k = 1f;

        /// <summary>La dernière photo de la partie (pour un test ou un autre affichage).</summary>
        public EtatHud Etat => etat;

        private void Awake()
        {
            if (Actuel != null && Actuel != this) { Destroy(this); return; }
            Actuel = this;
        }

        private void OnDestroy()
        {
            if (Actuel == this) Actuel = null;
        }

        private void OnEnable()
        {
            EvenementsPartie.Emis += Reagir;
            if (!float.IsPositiveInfinity(prochaineRecherche)) prochaineRecherche = 0f;
            else PrendreLaPlace();
        }

        private void OnDisable()
        {
            EvenementsPartie.Emis -= Reagir;
            RendreLaPlace();
            if (vue != null) vue.gameObject.SetActive(false);
        }

        private void Update()
        {
            Chercher();
            Photographier();
            MontrerVue();
            var clavier = Keyboard.current;
            if (etat.PartieConnue && etat.Phase == Phase.Finie && etat.RejouerPermis && clavier != null && clavier[Key.R].wasPressedThisFrame)
                deroulement.Rejouer();
        }

        // ---------------------------------------------------------------- Trouver la partie

        private void Chercher()
        {
            if (Time.unscaledTime < prochaineRecherche) return;
            prochaineRecherche = Time.unscaledTime + Recherche;

            var reseau = PartieReseau.Actuelle;
            if (reseau != null && reseau.Deroulement != null)
            {
                deroulement = reseau.Deroulement;
                cookies = reseau.Cookies;
            }
            if (deroulement == null) deroulement = FindAnyObjectByType<DeroulementPartie>();
            if (cookies == null)
                cookies = deroulement != null && deroulement.TryGetComponent(out PartieCookies voisin) ? voisin : FindAnyObjectByType<PartieCookies>();
            if (objets == null) objets = FindAnyObjectByType<PartieObjets>();
            if (torgnoles == null) torgnoles = FindAnyObjectByType<PartieTorgnoles>();
            if (sacre == null) sacre = FindAnyObjectByType<PartieCookieSacre>();
            if (meteo == null) meteo = FindAnyObjectByType<GestionnaireMeteo>();
            PrendreLaPlace();
        }

        /// <summary>
        /// Pour un test (les objets d'une scène d'aperçu ne se trouvent pas par recherche) : la partie à lire, sans plus
        /// rien chercher. Le HUD prend tout de suite la place de leurs affichages provisoires.
        /// </summary>
        public void Regler(DeroulementPartie d, PartieCookies c, PartieObjets o = null, PartieTorgnoles t = null,
            PartieCookieSacre s = null, GestionnaireMeteo m = null)
        {
            deroulement = d; cookies = c; objets = o; torgnoles = t; sacre = s; meteo = m;
            prochaineRecherche = float.PositiveInfinity;
            PrendreLaPlace();
        }

        /// <summary>Relit la partie et la montre (à chaque image ; un test l'appelle hors jeu).</summary>
        public void Rafraichir()
        {
            Photographier();
            MontrerVue();
        }

        /// <summary>Le HUD habillé de cette partie (créé à la première image), ou rien avec l'ancien dessin.</summary>
        public VueHud Vue => vue;

        private void MontrerVue()
        {
            if (!Habille)
            {
                if (vue != null) vue.gameObject.SetActive(false);
                return;
            }
            if (vue == null) vue = VueHud.Creer(transform);

            var d = donneesVue;
            d.ToucheDash = iconeDash;
            d.ToucheTorgnole = iconeTorgnole;
            d.ToucheObjet = iconeObjet;
            PorteurObjet porteObjet = objets != null && objets.Regles != null ? objets.Local : null;
            d.IconeObjet = porteObjet != null && porteObjet.Fiche != null ? porteObjet.Fiche.icone : null;
            d.MessageObjet = MessageObjet;
            d.MessageTorgnole = MessageTorgnole;
            d.Rejouer = rejouer ??= () => { if (deroulement != null) deroulement.Rejouer(); };
            d.Froggy.Clear();
            if (cookies != null)
                foreach (var p in cookies.Porteurs)
                    if (p != null)
                        d.Froggy.Add(new FroggyHud { Joueur = p.Joueur, Cookies = p.Nombre, Couleur = CouleurDe(p.Joueur), Tete = Tete(p.transform) });
            PhotographierRepere(d);
            vue.Montrer(etat, d);
        }

        // où est le cookie sacré dans le monde (#152) : sur le gobelin, au sol, ou sur la Froggy qui le porte ; en réseau,
        // PartieCookieSacre reflète l'état de l'hôte, et le porteur seul suffit quand il n'y a que lui
        private void PhotographierRepere(DonneesVueHud d)
        {
            d.SacreVisible = d.SacrePorte = d.SacreChezMoi = false;
            switch (etat.Sacre)
            {
                case EtatCookieSacre.Gobelin:
                    if (sacre != null && sacre.Gobelin != null)
                    {
                        d.SacreIci = Tete(sacre.Gobelin.transform);
                        d.SacreVisible = true;
                    }
                    break;
                case EtatCookieSacre.AuSol:
                    if (sacre != null && sacre.Regles != null)
                    {
                        Vector3 sol = sacre.Cookie != null ? sacre.Cookie.transform.position : sacre.Regles.PositionAuSol;
                        d.SacreIci = sol + Vector3.up * .5f;
                        d.SacreVisible = true;
                    }
                    break;
                case EtatCookieSacre.Porte:
                    PorteurCookies porteur = cookies != null ? cookies.Trouver(etat.PorteurSacre) : null;
                    if (porteur != null)
                    {
                        d.SacreIci = Tete(porteur.transform);
                        d.SacreVisible = d.SacrePorte = true;
                        d.SacreChezMoi = etat.PorteurSacre == etat.Local;
                    }
                    break;
            }
        }

        // le haut de la tête d'une Froggy ou du gobelin : le haut de son dessin (le premier rendu trouvé), sinon 1,2 m
        private Vector3 Tete(Transform p)
        {
            if (!rendus.TryGetValue(p, out Renderer rendu) || rendu == null)
            {
                rendu = p.GetComponentInChildren<SkinnedMeshRenderer>();
                if (rendu == null) rendu = p.GetComponentInChildren<Renderer>();
                rendus[p] = rendu;
            }
            Vector3 pos = p.position;
            float haut = rendu != null ? rendu.bounds.max.y : pos.y + 1.2f;
            return new Vector3(pos.x, haut + .25f, pos.z);
        }

        // le HUD prend la place des affichages provisoires « en attendant le HUD (P6-02) »
        private void PrendreLaPlace()
        {
            Prendre(deroulement, m => m.AffichageProvisoire, (m, v) => m.AffichageProvisoire = v);
            Prendre(cookies, m => m.ScoreProvisoire, (m, v) => m.ScoreProvisoire = v);
            Prendre(objets, m => m.AffichageProvisoire, (m, v) => m.AffichageProvisoire = v);
            Prendre(torgnoles, m => m.AffichageProvisoire, (m, v) => m.AffichageProvisoire = v);
            Prendre(sacre, m => m.MessageProvisoire, (m, v) => m.MessageProvisoire = v);
            Prendre(meteo, m => m.AffichageProvisoire, (m, v) => m.AffichageProvisoire = v);
        }

        private void Prendre<T>(T module, System.Func<T, bool> lire, System.Action<T, bool> ecrire) where T : Object
        {
            if (module == null || !pris.Add(module)) return;
            bool avant = lire(module);
            ecrire(module, false);
            aRendre.Add(() => { if (module != null) ecrire(module, avant); });
        }

        /// <summary>Le HUD coupé : chaque affichage provisoire revient comme il était.</summary>
        public void RendreLaPlace()
        {
            foreach (var rendre in aRendre) rendre();
            aRendre.Clear();
            pris.Clear();
        }

        // ---------------------------------------------------------------- Photographier la partie

        private void Photographier()
        {
            var e = etat;
            e.Local = JoueurLocal();

            e.PartieConnue = deroulement != null && deroulement.Regles != null;
            if (e.PartieConnue)
            {
                ReglesPartie r = deroulement.Regles;
                e.Phase = r.Phase;
                e.Reste = r.Reste;
                e.Duree = r.Duree;
                e.Gagnant = r.Gagnant;
                e.RejouerPermis = deroulement.RejouerPermis;
            }
            else
            {
                // une scène d'essai sans déroulement (cookies ou objets seuls) : on joue, sans minuteur ni fin
                e.Phase = Phase.EnJeu;
                e.Reste = 0f;
                e.Gagnant = ReglesPartie.Personne;
                e.RejouerPermis = false;
            }

            e.Joueurs.Clear();
            if (cookies != null && cookies.Regles != null)
                foreach (var p in cookies.Porteurs)
                    if (p != null) e.Joueurs.Add(new JoueurHud { Joueur = p.Joueur, Cookies = p.Nombre, Couleur = CouleurDe(p.Joueur) });
            e.Joueurs.Sort(ParNumero);

            PhotographierSacre(e);
            PhotographierObjet(e);

            PorteurCookies local = cookies != null ? cookies.Trouver(e.Local) : null;
            e.TorgnoleConnue = torgnoles != null && torgnoles.Regles != null && local != null;
            if (e.TorgnoleConnue)
            {
                e.RechargeTorgnole = torgnoles.Regles.Reste(e.Local);
                e.DureeRechargeTorgnole = torgnoles.Regles.DureeRecharge;
                e.ToucheTorgnole = Touche(torgnoles.ActionFrapper, out iconeTorgnole);
            }

            // le dash (#134) : celui de la Froggy de ce joueur, qu'il y ait une partie ou non
            var dash = PartieDash.Actuel;
            Deplacement.DeplacementWow froggy = dash != null ? dash.JoueurLocal() : null;
            e.DashConnu = froggy != null && dash.Regles != null;
            if (e.DashConnu)
            {
                int cle = dash.Cle(froggy);
                e.RechargeDash = dash.Regles.Reste(cle);
                e.DureeRechargeDash = dash.Regles.DureeRecharge;
                e.Assomme = dash.Regles.Assomme(cle);
                e.ToucheDash = Touche(dash.ActionDash, out iconeDash);
            }

            e.Meteo = null;
            if (meteo != null && meteo.EnMarche && meteo.Regles != null && meteo.Courant != null)
            {
                e.Meteo = meteo.Courant.nom;
                e.MeteoAnnoncee = meteo.Regles.Phase == PhaseMeteo.Annonce;
                e.MeteoReste = meteo.Regles.Reste;
                e.MeteoEffets = meteo.Courant.Effets ?? "";
            }
        }

        private void PhotographierSacre(EtatHud e)
        {
            e.Sacre = EtatCookieSacre.Absent;
            e.PorteurSacre = ReglesPartie.Personne;
            if (sacre != null && sacre.Regles != null && sacre.Regles.Etat != EtatCookieSacre.Absent)
            {
                e.Sacre = sacre.Regles.Etat;
                e.PorteurSacre = sacre.Regles.Porteur;
                return;
            }
            // chez un joueur qui n'est pas l'hôte, PartieCookieSacre ne décide rien : le porteur vient de l'hôte, s'il l'envoie
            int porteur = PartieReseau.Actuelle != null ? PartieReseau.Actuelle.PorteurCookieSacre : ReglesPartie.Personne;
            if (porteur == ReglesPartie.Personne && deroulement != null && deroulement.PorteurCookieSacre != null)
                porteur = deroulement.PorteurCookieSacre.Joueur;
            if (porteur == ReglesPartie.Personne) return;
            e.Sacre = EtatCookieSacre.Porte;
            e.PorteurSacre = porteur;
        }

        private void PhotographierObjet(EtatHud e)
        {
            PorteurObjet local = objets != null && objets.Regles != null ? objets.Local : null;
            e.ObjetsConnus = local != null;
            if (!e.ObjetsConnus) return;
            e.Objet = local.Fiche != null ? local.Fiche.nom : null;
            e.RechargeObjet = local.Recharge;
            e.DureeRechargeObjet = local.DernierUtilise != null ? local.DernierUtilise.recharge : 0f;
            e.ToucheObjet = Touche(objets.ActionUtiliser, out iconeObjet);

            if (local.Fiche == ficheVue) return;
            if (local.Fiche != null) Annoncer(ref messageObjet, ref finObjet, $"{local.Fiche.nom} !");
            else if (ficheVue != null) Annoncer(ref messageObjet, ref finObjet, $"{ficheVue.nom} utilisé");
            ficheVue = local.Fiche;
        }

        /// <summary>
        /// Le joueur de cette machine : en réseau, son numéro dans le salon ; sinon la Froggy dont le PlayerInput est
        /// allumé (comme PartieTorgnoles.JoueurLocal et BotsPartie.Humain).
        /// </summary>
        private int JoueurLocal()
        {
            var salon = Salon.Local;
            if (PartieReseau.Actuelle != null && salon != null && salon.Connecte)
            {
                MembreSalon? moi = salon.Moi;
                if (moi.HasValue) return moi.Value.Numero;
            }
            if (cookies != null)
                foreach (var p in cookies.Porteurs)
                    if (EstJoueurLocal(p)) return p.Joueur;
            return ReglesPartie.Personne;
        }

        /// <summary>
        /// Vrai pour la Froggy du joueur de cette machine : un PlayerInput <b>allumé</b>, et pas de cerveau de bot. Un bot
        /// garde un PlayerInput éteint (exigé par FroggyApparence) : le reconnaître à la seule présence du composant
        /// faisait afficher son score sur la ligne « (toi) » (#132, 18 novembre 2026).
        /// </summary>
        public static bool EstJoueurLocal(Component froggy)
        {
            return froggy != null && froggy.TryGetComponent(out PlayerInput entree) && entree.enabled
                   && !froggy.TryGetComponent(out Bots.CerveauBot _);
        }

        /// <summary>La couleur du joueur : celle choisie au salon en réseau, sinon celle de son repère d'apparition.</summary>
        private static Color CouleurDe(int joueur)
        {
            var salon = Salon.Local;
            if (PartieReseau.Actuelle != null && salon != null)
            {
                var membres = salon.Membres;
                for (int i = 0; i < membres.Count; i++)
                    if (membres[i].Numero == joueur) return salon.Couleur(membres[i].Couleur);
            }
            return RepereArene.Couleur(RepereArene.Sorte.Joueur, joueur);
        }

        /// <summary>
        /// La touche d'une action telle que l'Input System l'affiche, pour le dernier appareil utilisé (clavier ou
        /// manette) : réaffectée, elle suit (skill accessibilite).
        /// </summary>
        private static string Touche(InputAction action, out Sprite icone)
        {
            icone = null;
            if (action == null) return "";
            string appareil = IconesTouches.ParManette() ? "<Gamepad>" : "<Keyboard>";
            var liaisons = action.bindings;
            for (int i = 0; i < liaisons.Count; i++)
            {
                string chemin = liaisons[i].effectivePath;
                if (chemin == null || !chemin.StartsWith(appareil)) continue;
                icone = IconesTouches.Pour(action, i);
                return icone != null ? "" : action.GetBindingDisplayString(i);   // l'icône remplace le nom
            }
            return action.GetBindingDisplayString();
        }

        private float LargeurIcone(Sprite icone) => icone == null ? 0f : S(44f) * IconesTouches.Echelle(icone) + S(2f);

        // l'icône d'une touche dans un panneau du bas, à la hauteur du texte
        private void DessinerIcone(Sprite icone, float x, Rect zone, Color teinte)
        {
            if (icone == null) return;
            Color avant = GUI.color;
            GUI.color = teinte;
            // le dessin de Kenney garde une marge : 44 points pour une touche à la taille du texte, plus pour Espace, Maj…
            float cote = S(44f) * IconesTouches.Echelle(icone);
            GUI.DrawTexture(new Rect(x, zone.y + S(24f) - cote * .5f, cote, cote), icone.texture, ScaleMode.ScaleToFit);
            GUI.color = avant;
        }

        // ---------------------------------------------------------------- Messages courts

        /// <summary>Le message de torgnole en cours (« Torgnole sur Joueur 2 »…), ou vide.</summary>
        public string MessageTorgnole => messageTorgnole != null && Time.unscaledTime < finTorgnole ? messageTorgnole : "";

        /// <summary>Le message d'objet en cours (« Super croassement ! »…), ou vide.</summary>
        public string MessageObjet => messageObjet != null && Time.unscaledTime < finObjet ? messageObjet : "";

        /// <summary>Écoute les événements du socle (un test l'appelle directement) : les messages de torgnole.</summary>
        public void Reagir(EvenementPartie ev)
        {
            // un joueur a quitté la partie en ligne (P2-06) : même emplacement que les messages de torgnole
            if (ev.Type == TypeEvenement.JoueurParti && ev.Auteur != null && ev.Auteur.TryGetComponent(out PorteurCookies parti))
            {
                Annoncer(ref messageTorgnole, ref finTorgnole, $"Joueur {parti.Joueur} a quitté la partie");
                return;
            }
            if (ev.Type != TypeEvenement.CoupDonne || cookies == null) return;
            PorteurCookies local = cookies.Trouver(etat.Local);
            if (local == null) return;
            PorteurCookies touche = ev.Objet != null && ev.Objet.TryGetComponent(out PorteurCookies t) ? t : null;
            if (ev.Auteur == local.gameObject)
            {
                string quoi = touche == null ? "Torgnole dans le vide"
                    : touche.Intouchable ? $"Joueur {touche.Joueur} est intouchable"
                    : $"Torgnole sur Joueur {touche.Joueur}";
                Annoncer(ref messageTorgnole, ref finTorgnole, quoi);
            }
            else if (touche == local && ev.Auteur != null && ev.Auteur.TryGetComponent(out PorteurCookies frappeur))
                Annoncer(ref messageTorgnole, ref finTorgnole, $"Joueur {frappeur.Joueur} t'a mis une torgnole !");
        }

        private static void Annoncer(ref string message, ref float fin, string texte)
        {
            message = texte;
            fin = Time.unscaledTime + DureeMessage;
        }

        // ---------------------------------------------------------------- Dessin

        private float S(float v) => v * k;

        private void Styles()
        {
            k = Mathf.Max(.6f, Screen.height / Reference);
            if (texte != null && Mathf.Approximately(echelleStyles, k)) return;
            echelleStyles = k;
            texte = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(S(24f)), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter,
                wordWrap = false, clipping = TextClipping.Overflow,
            };
            texte.normal.textColor = Color.white;
            petit = new GUIStyle(texte) { fontSize = Mathf.RoundToInt(S(19f)) };
            gauche = new GUIStyle(texte) { alignment = TextAnchor.MiddleLeft };
            droite = new GUIStyle(texte) { alignment = TextAnchor.MiddleRight };
            grand = new GUIStyle(texte) { fontSize = Mathf.RoundToInt(S(46f)) };
            enorme = new GUIStyle(texte) { fontSize = Mathf.RoundToInt(S(150f)) };
            bouton = new GUIStyle(GUI.skin.button) { fontSize = Mathf.RoundToInt(S(26f)), fontStyle = FontStyle.Bold };
        }

        private void OnGUI()
        {
            if (Habille) return;   // le HUD habillé (VueHud) dessine ; ceci est l'ancien dessin, gardé pour comparer
            var e = etat;
            bool rien = !e.PartieConnue && e.Joueurs.Count == 0 && !e.ObjetsConnus && !e.TorgnoleConnue && string.IsNullOrEmpty(e.Meteo)
                        && !e.DashConnu;
            if (rien) return;
            Styles();
            float w = Screen.width, h = Screen.height;

            float sousLeHaut = S(14f);
            if (e.PartieConnue && e.Phase == Phase.Decompte) DessinerDecompte(e, w, h);
            if (e.PartieConnue && (e.Phase == Phase.EnJeu || e.Phase == Phase.Prolongation)) sousLeHaut = DessinerMinuteur(e, w);
            sousLeHaut = Bandeau(ContenuHud.CookieSacre(e), Or, w, sousLeHaut);
            Bandeau(ContenuHud.Meteo(e), Color.white, w, sousLeHaut);

            if (e.Joueurs.Count > 0 && e.Phase != Phase.Finie) DessinerScores(e, w);
            if (e.Phase != Phase.Finie)
            {
                DessinerObjet(e, h);
                DessinerTorgnole(e, w, h);
            }
            if (e.PartieConnue && e.Phase == Phase.Finie) DessinerFin(e, w, h);
        }

        private void DessinerDecompte(EtatHud e, float w, float h)
        {
            // un peu au-dessus du centre : Froggy reste visible dessous
            var zone = new Rect(w * .5f - S(150f), h * .4f - S(130f), S(300f), S(260f));
            Panneau(zone, Fond);
            Ecrire(new Rect(zone.x, zone.y, zone.width, S(200f)), ContenuHud.Minuteur(e), enorme, Color.white);
            Ecrire(new Rect(zone.x, zone.yMax - S(64f), zone.width, S(48f)), ContenuHud.SousMinuteur(e), texte, Gris);
        }

        /// <summary>Le minuteur en haut au centre ; rend le bas de ce qu'il a dessiné.</summary>
        private float DessinerMinuteur(EtatHud e, float w)
        {
            string temps = ContenuHud.Minuteur(e), sous = ContenuHud.SousMinuteur(e);
            bool presse = ContenuHud.MinuteurPresse(e);
            float largeur = Mathf.Max(S(200f), grand.CalcSize(new GUIContent(temps)).x + S(48f), texte.CalcSize(new GUIContent(sous)).x + S(40f));
            float hauteur = S(66f) + (sous.Length > 0 ? S(30f) : 0f);
            var zone = new Rect(w * .5f - largeur * .5f, S(12f), largeur, hauteur);
            Panneau(zone, Fond);
            Ecrire(new Rect(zone.x, zone.y + S(4f), zone.width, S(60f)), temps, grand, presse ? Or : Color.white);
            if (sous.Length > 0) Ecrire(new Rect(zone.x, zone.y + S(60f), zone.width, S(30f)), sous, petit, presse ? Or : Gris);
            return zone.yMax + S(8f);
        }

        /// <summary>Une ligne centrée sur fond sombre (cookie sacré, météo) ; rend le bas de ce qu'il a dessiné.</summary>
        private float Bandeau(string message, Color couleur, float w, float y)
        {
            if (string.IsNullOrEmpty(message)) return y;
            float largeur = texte.CalcSize(new GUIContent(message)).x + S(36f);
            var zone = new Rect(w * .5f - largeur * .5f, y, largeur, S(42f));
            Panneau(zone, Fond);
            Ecrire(zone, message, texte, couleur);
            return zone.yMax + S(8f);
        }

        /// <summary>Les scores en haut à droite (le menu de debug s'ouvre à gauche), dans l'ordre des numéros.</summary>
        private void DessinerScores(EtatHud e, float w)
        {
            float ligne = S(44f), marge = S(14f);
            // assez large pour la plus longue ligne : pastille, nom, mention (« cookie sacré »), score
            float largeur = S(380f);
            foreach (var j in e.Joueurs)
            {
                string m = ContenuHud.Marque(e, j.Joueur);
                float l = S(64f) + gauche.CalcSize(new GUIContent(ContenuHud.Nom(e, j.Joueur))).x
                          + (m.Length > 0 ? S(10f) + petit.CalcSize(new GUIContent(m)).x : 0f)
                          + S(24f) + droite.CalcSize(new GUIContent(ContenuHud.Cookies(j.Cookies))).x;
                largeur = Mathf.Max(largeur, l);
            }
            var cadre = new Rect(w - largeur - marge, marge, largeur, ligne * e.Joueurs.Count + S(12f));
            Panneau(cadre, Fond);
            float y = cadre.y + S(6f);
            foreach (var j in e.Joueurs)
            {
                var rang = new Rect(cadre.x + S(6f), y, cadre.width - S(12f), ligne - S(4f));
                if (j.Joueur == e.Local) Panneau(rang, FondLocal);
                Pastille(new Rect(rang.x + S(8f), rang.y + (rang.height - S(28f)) * .5f, S(28f), S(28f)), j);
                string nom = ContenuHud.Nom(e, j.Joueur);
                string marque = ContenuHud.Marque(e, j.Joueur);
                var zoneNom = new Rect(rang.x + S(46f), rang.y, rang.width - S(54f), rang.height);
                Ecrire(zoneNom, nom, gauche, Color.white);
                if (marque.Length > 0)
                {
                    float apres = gauche.CalcSize(new GUIContent(nom)).x + S(10f);
                    Ecrire(new Rect(zoneNom.x + apres, zoneNom.y, zoneNom.width - apres, zoneNom.height), marque, petit, Or, TextAnchor.MiddleLeft);
                }
                Ecrire(new Rect(rang.x, rang.y, rang.width - S(10f), rang.height), ContenuHud.Cookies(j.Cookies), droite, Color.white);
                y += ligne;
            }
        }

        /// <summary>Le carré du joueur : sa couleur ET son numéro (la couleur seule ne suffit pas, skill accessibilite).</summary>
        private void Pastille(Rect zone, JoueurHud j)
        {
            Panneau(zone, j.Couleur, S(6f));
            float luminance = .2126f * j.Couleur.linear.r + .7152f * j.Couleur.linear.g + .0722f * j.Couleur.linear.b;
            Ecrire(zone, j.Joueur.ToString(), petit, luminance > .18f ? Color.black : Color.white);
        }

        private void DessinerObjet(EtatHud e, float h)
        {
            string ligne = ContenuHud.Objet(e);
            float marge = S(14f);
            float bas = h - marge;
            if (ligne.Length > 0)
            {
                Sprite icone = e.RechargeObjet > 0f || string.IsNullOrEmpty(e.Objet) ? null : iconeObjet;
                float texteLarge = gauche.CalcSize(new GUIContent(ligne)).x;
                float largeur = Mathf.Max(S(300f), texteLarge + S(32f) + LargeurIcone(icone));
                var zone = new Rect(marge, bas - S(60f), largeur, S(60f));
                Panneau(zone, Fond);
                Ecrire(new Rect(zone.x + S(16f), zone.y + S(4f), zone.width - S(32f), S(40f)), ligne, gauche, Color.white);
                DessinerIcone(icone, zone.x + S(16f) + texteLarge + S(4f), zone, Color.white);
                Jauge(new Rect(zone.x + S(16f), zone.yMax - S(14f), zone.width - S(32f), S(6f)),
                    ContenuHud.Jauge(e.RechargeObjet, e.DureeRechargeObjet), e.RechargeObjet > 0f);
                bas = zone.y - S(8f);
            }
            if (messageObjet != null && Time.unscaledTime < finObjet)
                Message(messageObjet, new Rect(marge, bas - S(40f), S(600f), S(40f)), TextAnchor.MiddleLeft);
        }

        private void DessinerTorgnole(EtatHud e, float w, float h)
        {
            string ligne = ContenuHud.Torgnole(e);
            float marge = S(14f);
            float bas = h - marge;
            if (ligne.Length > 0)
            {
                float decale = LargeurIcone(iconeTorgnole);
                float largeur = Mathf.Max(S(300f), gauche.CalcSize(new GUIContent(ligne)).x + S(32f) + decale);
                var zone = new Rect(w - largeur - marge, bas - S(60f), largeur, S(60f));
                Panneau(zone, Fond);
                Color teinte = e.RechargeTorgnole > 0f ? Gris : Color.white;
                DessinerIcone(iconeTorgnole, zone.x + S(10f), zone, teinte);
                Ecrire(new Rect(zone.x + S(16f) + decale, zone.y + S(4f), zone.width - S(32f) - decale, S(40f)), ligne,
                    gauche, teinte);
                Jauge(new Rect(zone.x + S(16f), zone.yMax - S(14f), zone.width - S(32f), S(6f)),
                    ContenuHud.Jauge(e.RechargeTorgnole, e.DureeRechargeTorgnole), e.RechargeTorgnole > 0f);
                bas = zone.y - S(8f);
            }
            // le dash (#134), juste au-dessus de la torgnole : même panneau, même jauge
            string dash = ContenuHud.Dash(e);
            if (dash.Length > 0)
            {
                bool attente = e.RechargeDash > 0f || e.Assomme;
                Sprite icone = e.Assomme ? null : iconeDash;
                float decale = LargeurIcone(icone);
                float largeur = Mathf.Max(S(300f), gauche.CalcSize(new GUIContent(dash)).x + S(32f) + decale);
                var zone = new Rect(w - largeur - marge, bas - S(60f), largeur, S(60f));
                Panneau(zone, Fond);
                Color teinte = e.Assomme ? Or : attente ? Gris : Color.white;
                DessinerIcone(icone, zone.x + S(10f), zone, teinte);
                Ecrire(new Rect(zone.x + S(16f) + decale, zone.y + S(4f), zone.width - S(32f) - decale, S(40f)), dash,
                    gauche, teinte);
                Jauge(new Rect(zone.x + S(16f), zone.yMax - S(14f), zone.width - S(32f), S(6f)),
                    ContenuHud.Jauge(e.RechargeDash, e.DureeRechargeDash), attente);
                bas = zone.y - S(8f);
            }
            if (messageTorgnole != null && Time.unscaledTime < finTorgnole)
                Message(messageTorgnole, new Rect(w - S(600f) - marge, bas - S(40f), S(600f), S(40f)), TextAnchor.MiddleRight);
        }

        private void DessinerFin(EtatHud e, float w, float h)
        {
            var (titre, raison) = ContenuHud.Fin(e);
            classement.Clear();
            classement.AddRange(e.Joueurs);
            // le gagnant d'abord, puis les cookies, puis le numéro
            classement.Sort((a, b) =>
            {
                if ((a.Joueur == e.Gagnant) != (b.Joueur == e.Gagnant)) return a.Joueur == e.Gagnant ? -1 : 1;
                int c = b.Cookies.CompareTo(a.Cookies);
                return c != 0 ? c : a.Joueur.CompareTo(b.Joueur);
            });

            float ligne = S(42f);
            float hauteur = S(100f) + (raison.Length > 0 ? S(36f) : 0f) + ligne * classement.Count + (e.RejouerPermis ? S(86f) : S(20f));
            float largeur = S(560f);
            var cadre = new Rect(w * .5f - largeur * .5f, h * .5f - hauteur * .5f, largeur, hauteur);
            Panneau(cadre, Fond);
            float y = cadre.y + S(20f);
            Ecrire(new Rect(cadre.x, y, cadre.width, S(64f)), titre, grand, e.Gagnant == e.Local ? Or : Color.white);
            y += S(70f);
            if (raison.Length > 0)
            {
                Ecrire(new Rect(cadre.x, y, cadre.width, S(32f)), raison, texte, Gris);
                y += S(36f);
            }
            y += S(10f);
            foreach (var j in classement)
            {
                var rang = new Rect(cadre.x + S(40f), y, cadre.width - S(80f), ligne - S(4f));
                if (j.Joueur == e.Local) Panneau(rang, FondLocal);
                Pastille(new Rect(rang.x + S(8f), rang.y + (rang.height - S(28f)) * .5f, S(28f), S(28f)), j);
                Ecrire(new Rect(rang.x + S(46f), rang.y, rang.width - S(54f), rang.height), ContenuHud.Nom(e, j.Joueur), gauche, Color.white);
                Ecrire(new Rect(rang.x, rang.y, rang.width - S(10f), rang.height), ContenuHud.Cookies(j.Cookies), droite, Color.white);
                y += ligne;
            }
            if (!e.RejouerPermis) return;   // en réseau : le salon relance (P2-04)

            // dès l'appui : au clic gauche, la caméra capture la souris et le relâchement n'arrive jamais au bouton
            var zoneBouton = new Rect(w * .5f - S(130f), cadre.yMax - S(70f), S(260f), S(52f));
            var ev = Event.current;
            if (ev.type == EventType.MouseDown && ev.button == 0 && zoneBouton.Contains(ev.mousePosition))
            {
                ev.Use();
                deroulement.Rejouer();
                return;
            }
            GUI.Button(zoneBouton, "Rejouer (R)", bouton);
        }

        private void Message(string message, Rect zone, TextAnchor ancre)
        {
            float largeur = texte.CalcSize(new GUIContent(message)).x + S(32f);
            var fond = ancre == TextAnchor.MiddleRight
                ? new Rect(zone.xMax - largeur, zone.y, largeur, zone.height)
                : new Rect(zone.x, zone.y, largeur, zone.height);
            Panneau(fond, Fond);
            Ecrire(fond, message, texte, Color.white);
        }

        private void Jauge(Rect zone, float plein, bool enCours)
        {
            if (!enCours) return;   // prête : pas de jauge, le texte le dit
            Panneau(zone, JaugeVide, S(3f));
            Panneau(new Rect(zone.x, zone.y, zone.width * plein, zone.height), Color.white, S(3f));
        }

        private void Panneau(Rect zone, Color couleur, float arrondi = -1f)
        {
            if (Event.current.type != EventType.Repaint) return;
            GUI.DrawTexture(zone, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, couleur, 0f, arrondi < 0f ? S(10f) : arrondi);
        }

        private static void Ecrire(Rect zone, string message, GUIStyle style, Color couleur, TextAnchor? ancre = null)
        {
            if (string.IsNullOrEmpty(message)) return;
            var avant = style.alignment;
            if (ancre.HasValue) style.alignment = ancre.Value;
            style.normal.textColor = couleur;
            GUI.Label(zone, message, style);
            style.alignment = avant;
        }
    }
}
