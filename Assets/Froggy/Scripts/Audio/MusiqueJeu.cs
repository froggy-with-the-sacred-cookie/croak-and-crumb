using Froggy.Interface;
using Froggy.Regles;
using Froggy.Reseau;
using UnityEngine;

namespace Froggy.Audio
{
    /// <summary>
    /// La musique du jeu (P6-08) : un seul propriétaire, installé tout seul au lancement et gardé d'une scène à l'autre,
    /// comme le HUD. Le thème au menu ; en partie, les quatre couches du jardin lues ensemble, dosées selon la partie
    /// (ReglesMusique) ; un jingle à la fin. En ligne, il lit la partie que l'hôte envoie (PartieReseau), comme le HUD :
    /// chaque machine joue sa musique, rien ne passe par le réseau.
    /// Le volume suit « Musique » des Paramètres (ReglagesJoueur.volumeMusique), relu à chaque image.
    /// Menu Froggy ▸ Musique pour la couper dans l'éditeur.
    /// </summary>
    public sealed class MusiqueJeu : MonoBehaviour
    {
        private const string CleActive = "Froggy.Musique.active";
        private const float Recherche = .5f;       // tant qu'il manque une partie, on la cherche toutes les 0,5 s
        private const double Avance = .1;          // les couches partent 0,1 s après la demande, toutes au même échantillon

        public static MusiqueJeu Actuelle { get; private set; }

        /// <summary>Vrai si la musique joue dans l'éditeur (EditorPrefs, gardé d'une session à l'autre) ; un build la joue.</summary>
        public static bool Active
        {
            get
            {
#if UNITY_EDITOR
                return UnityEditor.EditorPrefs.GetBool(CleActive, true);
#else
                return true;
#endif
            }
            set
            {
#if UNITY_EDITOR
                UnityEditor.EditorPrefs.SetBool(CleActive, value);
#endif
                if (Actuelle != null) Actuelle.enabled = value;
            }
        }

        // sans rechargement du domaine à l'entrée en mode Play, la musique de la partie d'avant resterait
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Vider() => Actuelle = null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Installer()
        {
            if (Actuelle != null || FindAnyObjectByType<MusiqueJeu>(FindObjectsInactive.Include) != null) return;
            var reglages = Resources.Load<ReglagesMusique>(ReglagesMusique.Chemin);
            if (reglages == null) return;
            var go = new GameObject("Musique");
            go.SetActive(false);
            DontDestroyOnLoad(go);
            var musique = go.AddComponent<MusiqueJeu>();
            musique.Regler(reglages);
            musique.enabled = Active;
            go.SetActive(true);
        }

        [SerializeField] private ReglagesMusique reglages;

        private ReglesMusique regles;
        private readonly MelangeurMusique melangeur = new MelangeurMusique();
        private AudioSource sourceMenu, sourceJingle;
        private readonly AudioSource[] couches = new AudioSource[4];
        [System.NonSerialized] private bool couchesEnMarche;
        [System.NonSerialized] private double debutCouches;

        private DeroulementPartie deroulement;
        private PartieCookies cookies;
        [System.NonSerialized] private float prochaineRecherche;

        /// <summary>Ce que joue la musique.</summary>
        public EtatMusique Etat => regles != null ? regles.Etat : EtatMusique.Silence;

        /// <summary>Les réglages à jouer (l'installation, ou un test).</summary>
        public void Regler(ReglagesMusique r)
        {
            reglages = r;
            regles = r != null ? new ReglesMusique(r.seuilDerniereMinute, r.margeSortie) : null;
        }

        private void Awake()
        {
            if (Actuelle != null && Actuelle != this) { Destroy(this); return; }
            Actuelle = this;
            sourceMenu = NouvelleSource("Menu", true);
            for (int i = 0; i < couches.Length; i++) couches[i] = NouvelleSource(((Couche)(i + 1)).ToString(), true);
            sourceJingle = NouvelleSource("Jingle", false);
            if (regles == null && reglages != null) Regler(reglages);
        }

        private void OnDestroy()
        {
            if (Actuelle == this) Actuelle = null;
        }

        private void OnDisable()
        {
            // coupée (menu Froggy ▸ Musique) : tout se tait ; rallumée, elle reprend l'état de la partie
            if (sourceMenu != null) sourceMenu.Stop();
            if (sourceJingle != null) sourceJingle.Stop();
            ArreterCouches();
            if (reglages != null) Regler(reglages);
            melangeur.Viser(default, 0d, 0d, 0f, 0f);
            melangeur.Avancer(0d, 0f);
        }

        private AudioSource NouvelleSource(string nom, bool boucle)
        {
            var enfant = new GameObject(nom);
            enfant.transform.SetParent(transform, false);
            var s = enfant.AddComponent<AudioSource>();
            s.playOnAwake = false;
            s.loop = boucle;
            s.spatialBlend = 0f;
            s.priority = 0;                 // la musique ne se fait jamais voler sa voix
            s.bypassReverbZones = true;
            s.dopplerLevel = 0f;
            s.volume = 0f;
            return s;
        }

        private void Update()
        {
            if (reglages == null || regles == null) return;
            Chercher();
            var photo = Photographier();
            EtatMusique avant = regles.Etat;
            EtatMusique apres = regles.Decider(photo);
            double maintenant = AudioSettings.dspTime;
            if (apres != avant) Changer(avant, apres, maintenant);
            melangeur.Avancer(maintenant, Time.unscaledDeltaTime);
            Appliquer();
        }

        // ---------------------------------------------------------------- Trouver et lire la partie

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
            if (cookies == null && deroulement != null)
                cookies = deroulement.TryGetComponent(out PartieCookies voisin) ? voisin : FindAnyObjectByType<PartieCookies>();
        }

        private PhotoMusique Photographier()
        {
            var p = new PhotoMusique
            {
                MenuVisible = MenuPrincipal.MenuVisible(),
                PartieConnue = deroulement != null && deroulement.Regles != null,
                Gagnant = ReglesPartie.Personne,
                Local = ReglesPartie.Personne,
            };
            if (!p.PartieConnue) return p;
            ReglesPartie r = deroulement.Regles;
            p.Phase = r.Phase;
            p.Reste = r.Reste;
            p.Gagnant = r.Gagnant;
            if (r.Phase == Phase.Finie) p.Local = JoueurLocal();
            return p;
        }

        /// <summary>Le joueur de cette machine : la même règle que le HUD (HudPartie.JoueurLocal).</summary>
        private int JoueurLocal()
        {
            var salon = Salon.Local;
            if (PartieReseau.Actuelle != null && salon != null && salon.Connecte)
            {
                MembreSalon? moi = salon.Moi;
                if (moi.HasValue) return moi.Value.Numero;
            }
            if (cookies != null)
                foreach (var porteur in cookies.Porteurs)
                    if (HudPartie.EstJoueurLocal(porteur)) return porteur.Joueur;
            return ReglesPartie.Personne;
        }

        // ---------------------------------------------------------------- Jouer

        private void Changer(EtatMusique avant, EtatMusique apres, double maintenant)
        {
            Dosage dosage = ReglesMusique.DosageDe(apres);
            if (dosage.Menu > 0f && !sourceMenu.isPlaying && reglages.menu != null)
            {
                sourceMenu.clip = reglages.menu;
                sourceMenu.volume = 0f;
                sourceMenu.Play();
            }
            if (dosage.Jardin && !couchesEnMarche) DemarrerCouches(maintenant);

            double debut = maintenant;
            if (couchesEnMarche && ReglesMusique.AttendLaMesure(avant, apres))
                debut = ReglesMusique.ProchaineMesure(debutCouches, maintenant, reglages.DureeMesure);
            float montee = apres == EtatMusique.DerniereMinute ? reglages.fonduTension : reglages.fonduEntree;
            float descente = ReglesMusique.JoueUnJingle(apres) ? reglages.fonduFin : reglages.fonduSortie;
            melangeur.Viser(dosage, debut, maintenant, montee, descente);

            if (ReglesMusique.JoueUnJingle(apres))
            {
                sourceJingle.clip = apres == EtatMusique.Victoire ? reglages.victoire : reglages.defaite;
                if (sourceJingle.clip != null) sourceJingle.Play();
            }
            else if (sourceJingle.isPlaying) sourceJingle.Stop();   // « Rejouer » pendant le jingle
        }

        private void DemarrerCouches(double maintenant)
        {
            melangeur.CouperJardin();
            debutCouches = maintenant + Avance;
            for (int i = 0; i < couches.Length; i++)
            {
                var s = couches[i];
                s.clip = reglages.Clip((Couche)(i + 1));
                s.volume = 0f;
                if (s.clip != null) s.PlayScheduled(debutCouches);
            }
            couchesEnMarche = true;
        }

        private void ArreterCouches()
        {
            foreach (var s in couches)
                if (s != null) s.Stop();
            couchesEnMarche = false;
        }

        private void Appliquer()
        {
            float general = reglages.volume * ReglagesJoueur.Actuels.volumeMusique;
            sourceMenu.volume = melangeur.Volume(Couche.Menu) * general;
            if (sourceMenu.isPlaying && melangeur.Volume(Couche.Menu) <= 0f && melangeur.Cible(Couche.Menu) <= 0f)
                sourceMenu.Stop();
            for (int i = 0; i < couches.Length; i++) couches[i].volume = melangeur.Volume((Couche)(i + 1)) * general;
            if (couchesEnMarche && melangeur.JardinMuet) ArreterCouches();
            sourceJingle.volume = general;
        }
    }
}
