using System;
using System.Collections;
using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Managing.Scened;
using FishNet.Object;
using FishNet.Transporting;
using Froggy.Socle;
using UnityEngine;

namespace Froggy.Reseau
{
    /// <summary>
    /// Le salon (P2-03) : créer une partie, obtenir un code, rejoindre, se dire prêt, lancer, revenir au salon.
    /// Posé sur le NetworkManager du prefab Salon. <b>C'est la porte d'entrée des écrans (P6-03)</b> :
    ///
    ///   Salon salon = Salon.Local;                 // celui de cette fenêtre
    ///   salon.Change += Rafraichir;                // tout changement : joueurs, prêt, phase, connexion
    ///   salon.Erreur += AfficherMessage;           // « Aucun salon KRBF trouvé », « Salon plein »…
    ///   salon.Creer();  salon.Rejoindre("krbf");   salon.Quitter();
    ///   salon.SeDirePret(true);  salon.CouleurSuivante();  salon.Lancer();  salon.RevenirAuSalon();
    ///   salon.Code, salon.Membres, salon.Moi, salon.EstHote, salon.PeutLancer, salon.Phase, salon.Couleur(i)
    ///
    /// L'hôte décide de tout (EtatSalon) ; chacun en reçoit une copie (SalonReseau). Au lancement, la scène de partie
    /// est chargée chez tous et chaque joueur reçoit un personnage provisoire (capsule de P2-01, en attendant P2-02).
    /// La partie revient seule au salon quelques secondes après l'événement « partie finie » (P3-03, P2-04).
    ///
    /// Deux façons de se retrouver (P2-05) : <b>en ligne</b> (par défaut), par Unity Relay, avec le code donné par
    /// Unity, depuis n'importe quelle maison ; <b>réseau local</b> (salon.EnLigne = false), sans internet, avec un code
    /// de 4 lettres trouvé sur le réseau local. Les deux passent par le transport Unity (UnityTransport) du prefab.
    /// </summary>
    [RequireComponent(typeof(NetworkManager), typeof(AuthentificateurSalon), typeof(RechercheSalon))]
    [RequireComponent(typeof(RelaisSalon))]
    public class Salon : MonoBehaviour
    {
        [Header("En ligne")]
        [Tooltip("Coché : par internet (Unity Relay). Décoché : réseau local seulement, sans internet.")]
        [SerializeField] private bool enLigne = true;

        [Header("Partie")]
        [Tooltip("Scène chargée chez tous au lancement (doit être dans les Build Settings).")]
        [SerializeField] private string scenePartie = "Vitrine_JardinJaponais";

        [Tooltip("Personnage provisoire de chaque joueur (NetworkObject).")]
        [SerializeField] private NetworkObject prefabJoueur;

        [Tooltip("Points d'apparition, dans l'ordre des numéros de joueur.")]
        [SerializeField] private Transform[] apparitions;

        [Tooltip("Secondes entre « partie finie » et le retour au salon.")]
        [SerializeField] private float delaiRetour = 5f;

        [Tooltip("Règles de partie en réseau (PartieReseau, P2-04), apparues quand tous les joueurs sont là. Vide : " +
                 "pas de règles (on se promène).")]
        [SerializeField] private NetworkObject prefabPartie;

        [Header("Salon")]
        [Tooltip("Objet réseau du salon (SalonReseau, NetworkObject global).")]
        [SerializeField] private NetworkObject prefabSalonReseau;

        [Tooltip("PROVISOIRE : palette des joueurs (P6-03 choisira les vraies).")]
        [SerializeField] private Color[] couleurs =
        {
            new(0.30f, 0.80f, 0.35f), new(0.90f, 0.35f, 0.30f), new(0.30f, 0.55f, 0.95f),
            new(0.95f, 0.80f, 0.25f), new(0.70f, 0.40f, 0.90f), new(0.95f, 0.55f, 0.20f),
        };

        [Header("Connexion")]
        [Tooltip("Premier port de jeu essayé par l'hôte ; les suivants si pris (FishNet garde parfois un port, voir P2-07).")]
        [SerializeField] private ushort premierPort = 7770;
        [SerializeField] private int portsEssayes = 10;

        [Tooltip("Connexion simulée des tests (Froggy ▸ Réseau ▸ 3. Connexion). Facultatif.")]
        [SerializeField] private ReglagesTestReseau reglagesTest;

        private NetworkManager reseau;
        private AuthentificateurSalon porte;
        private RechercheSalon recherche;
        private RelaisSalon relais;
        private FishNet.Transporting.UTP.UnityTransport transport;   // null si le prefab garde Tugboat
        private EtatSalon etat;                       // hôte seulement
        private readonly Dictionary<int, NetworkObject> joueurs = new();
        private NetworkObject partie;                 // hôte : les règles de la partie en cours
        private LocalConnectionState etatServeur = LocalConnectionState.Stopped;
        private Coroutine enCours;
        private Coroutine retourPrevu;

        /// <summary>Le salon de cette fenêtre.</summary>
        public static Salon Local { get; private set; }

        /// <summary>Le salon de l'hôte, côté serveur (utilisé par SalonReseau).</summary>
        internal static Salon Serveur => Local != null && Local.etat != null ? Local : null;

        public event Action Change;
        public event Action<string> Erreur;

        /// <summary>Ce qui se passe, en une phrase (« Recherche du salon KRBF… »).</summary>
        public string Etat { get; private set; } = "Hors ligne";
        public string DerniereErreur { get; private set; } = "";

        /// <summary>Vrai pendant une création ou une recherche de salon.</summary>
        public bool Occupe => enCours != null;

        /// <summary>
        /// Arrivé pendant une partie (P2-06) : on attend qu'elle finisse, le jeu réessaie tout seul ; salon.Etat dit
        /// « Partie en cours : vous entrerez dès qu'elle sera finie (≈ m:ss) ». Quitter() arrête l'attente.
        /// </summary>
        public bool EnAttente { get; private set; }

        /// <summary>PROVISOIRE : secondes entre deux essais pendant l'attente.</summary>
        private const float AttenteEntreEssais = 5f;

        private bool fermeturePrevenue;   // client : l'hôte a dit qu'il fermait (P2-06)
        private bool fermetureEnPartie;
        private string codeAttendu;
        public bool Connecte => SalonReseau.Actuel != null && reseau.IsClientStarted;
        public bool EstHote => reseau != null && reseau.IsServerStarted;

        public string Code => SalonReseau.Actuel != null ? SalonReseau.Actuel.Code : "";
        public PhaseSalon Phase => SalonReseau.Actuel != null ? SalonReseau.Actuel.Phase : PhaseSalon.Salon;
        public IReadOnlyList<MembreSalon> Membres =>
            SalonReseau.Actuel != null ? SalonReseau.Actuel.Membres : Array.Empty<MembreSalon>();
        public bool PeutLancer => EstHote && EtatSalon.PeutLancerAvec(Membres, Phase);
        public int NombreCouleurs => couleurs.Length;

        /// <summary>Soi-même dans la liste, ou null si pas (encore) dans un salon.</summary>
        public MembreSalon? Moi
        {
            get
            {
                if (!reseau.IsClientStarted || !reseau.ClientManager.Connection.IsValid) return null;
                int id = reseau.ClientManager.Connection.ClientId;
                foreach (MembreSalon m in Membres)
                    if (m.ClientId == id) return m;
                return null;
            }
        }

        public Color Couleur(int indice) =>
            couleurs.Length == 0 ? Color.white : couleurs[Mathf.Clamp(indice, 0, couleurs.Length - 1)];

        /// <summary>Numéro (1 à 4) d'un joueur d'après son identifiant FishNet ; faux s'il n'est pas dans le salon.</summary>
        public bool NumeroDe(int clientId, out int numero)
        {
            foreach (MembreSalon m in Membres)
            {
                if (m.ClientId != clientId) continue;
                numero = m.Numero;
                return true;
            }
            numero = 0;
            return false;
        }

        /// <summary>Couleur d'un joueur d'après son identifiant FishNet ; faux s'il n'est pas dans le salon.</summary>
        public bool CouleurDe(int clientId, out Color couleur)
        {
            foreach (MembreSalon m in Membres)
            {
                if (m.ClientId != clientId) continue;
                couleur = Couleur(m.Couleur);
                return true;
            }
            couleur = Color.white;
            return false;
        }

        // ---------------------------------------------------------------- Actions (tout le monde)

        /// <summary>
        /// En ligne (Unity Relay, depuis n'importe où) ou réseau local (sans internet). Ne change qu'hors d'un salon.
        /// </summary>
        public bool EnLigne
        {
            get => enLigne;
            set
            {
                if (Occupe || Connecte) return;
                enLigne = value;
                Change?.Invoke();
            }
        }

        /// <summary>Ouvre un salon : cette fenêtre devient l'hôte et reçoit un code.</summary>
        public void Creer()
        {
            if (Occupe || reseau.IsClientStarted || reseau.IsServerStarted) return;
            enCours = StartCoroutine(enLigne ? CreerEnLigne() : CreerSalon());
        }

        /// <summary>Rejoint le salon de ce code (majuscules, espaces et tirets acceptés).</summary>
        public void Rejoindre(string saisie)
        {
            if (Occupe || reseau.IsClientStarted || reseau.IsServerStarted) return;
            string code = CodeSalon.Normaliser(saisie);
            if (enLigne ? !CodeSalon.EstValideEnLigne(code) : !CodeSalon.EstValide(code))
            {
                Signaler(enLigne ? "Code incomplet : recopier le code affiché chez l'hôte."
                                 : $"Un code, c'est {CodeSalon.Longueur} lettres (sans O ni I).");
                return;
            }
            enCours = StartCoroutine(enLigne ? RejoindreEnLigne(code) : RejoindreSalon(code));
        }

        /// <summary>Quitte le salon ; pour l'hôte, le ferme pour tout le monde.</summary>
        public void Quitter()
        {
            if (enCours != null) StopCoroutine(enCours);
            enCours = null;
            EnAttente = false;
            Etat = "Hors ligne"; // avant Fermer : un départ voulu n'est pas une « déconnexion »
            DerniereErreur = "";
            Fermer();
            Change?.Invoke();
        }

        public void SeDirePret(bool pret) => SalonReseau.Actuel?.DemanderPret(pret);

        public void ChoisirCouleur(int indice) => SalonReseau.Actuel?.DemanderCouleur(indice);

        /// <summary>Prend la prochaine couleur libre de la palette.</summary>
        public void CouleurSuivante()
        {
            MembreSalon? moi = Moi;
            if (moi == null || couleurs.Length == 0) return;
            for (int pas = 1; pas < couleurs.Length; pas++)
            {
                int c = (moi.Value.Couleur + pas) % couleurs.Length;
                bool prise = false;
                foreach (MembreSalon m in Membres) prise |= m.Couleur == c;
                if (prise) continue;
                ChoisirCouleur(c);
                return;
            }
        }

        /// <summary>Hôte seulement : lance la partie pour tous (si PeutLancer).</summary>
        public void Lancer() => SalonReseau.Actuel?.DemanderLancement();

        /// <summary>Hôte seulement : arrête la partie et ramène tout le monde au salon.</summary>
        public void RevenirAuSalon() => SalonReseau.Actuel?.DemanderRetour();

        // ---------------------------------------------------------------- Cycle de vie

        private void Awake()
        {
            reseau = GetComponent<NetworkManager>();
            porte = GetComponent<AuthentificateurSalon>();
            recherche = GetComponent<RechercheSalon>();
            relais = GetComponent<RelaisSalon>();
            transport = GetComponent<FishNet.Transporting.UTP.UnityTransport>();
            if (transport == null) enLigne = false;   // sans transport Unity, pas de relais possible
            Local = this;
        }

        private void OnEnable()
        {
            reseau.ServerManager.OnServerConnectionState += QuandServeurChange;
            reseau.ServerManager.OnAuthenticationResult += QuandAuthentifie;
            reseau.ServerManager.OnRemoteConnectionState += QuandConnexionDistante;
            reseau.ClientManager.OnClientConnectionState += QuandClientChange;
            reseau.SceneManager.OnClientPresenceChangeEnd += QuandPresenceChange;
            porte.Reponse += QuandReponse;
            porte.TempsRestant = TempsRestantPartie;
            reseau.ClientManager.RegisterBroadcast<FermetureSalon>(QuandFermeture);
            SalonReseau.Change += Relayer;
            EvenementsPartie.Emis += QuandEvenement;
        }

        private void OnDisable()
        {
            if (reseau != null)
            {
                reseau.ServerManager.OnServerConnectionState -= QuandServeurChange;
                reseau.ServerManager.OnAuthenticationResult -= QuandAuthentifie;
                reseau.ServerManager.OnRemoteConnectionState -= QuandConnexionDistante;
                reseau.ClientManager.OnClientConnectionState -= QuandClientChange;
                reseau.SceneManager.OnClientPresenceChangeEnd -= QuandPresenceChange;
            }
            if (porte != null) porte.Reponse -= QuandReponse;
            if (reseau != null) reseau.ClientManager.UnregisterBroadcast<FermetureSalon>(QuandFermeture);
            SalonReseau.Change -= Relayer;
            EvenementsPartie.Emis -= QuandEvenement;
        }

        private void Start()
        {
            if (reglagesTest != null) reglagesTest.Appliquer(reseau, reglagesTest.prereglageActif);
        }

        private void OnApplicationQuit() => Fermer(); // voir P2-07 : fermer avant la destruction des objets

        private void OnDestroy()
        {
            if (Local == this) Local = null;
        }

        // ---------------------------------------------------------------- Créer et rejoindre

        private IEnumerator CreerSalon()
        {
            DerniereErreur = "";
            string code = CodeSalon.Generer(new System.Random());
            etat = new EtatSalon(code, couleurs.Length);
            porte.Juge = etat.Examiner;

            for (int i = 0; i < portsEssayes; i++)
            {
                ushort port = (ushort)(premierPort + i);
                if (!PortLibre(port)) continue;

                Etat = $"Ouverture du salon (port {port})…";
                Change?.Invoke();
                // transport Unity : connexion directe (pas le relais), à l'écoute sur toutes les cartes réseau
                if (transport != null) transport.SetConnectionData("127.0.0.1", port, "0.0.0.0");
                else reseau.TransportManager.Transport.SetPort(port);
                etatServeur = LocalConnectionState.Starting;
                reseau.ServerManager.StartConnection();
                yield return new WaitUntil(() => etatServeur == LocalConnectionState.Started
                                                 || etatServeur == LocalConnectionState.Stopped);
                if (etatServeur != LocalConnectionState.Started) continue;

                // Objet réseau du salon, puis le client de l'hôte, reconnu par son jeton.
                NetworkObject salon = Instantiate(prefabSalonReseau);
                reseau.ServerManager.Spawn(salon);
                SalonReseau.Actuel?.Publier(etat);

                porte.PreparerHote();
                porte.CodeAPresenter = code;
                recherche.Annoncer(code, port);
                reseau.ClientManager.StartConnection("127.0.0.1", port);   // une adresse : le transport Unity ne lit pas « localhost »
                Etat = $"Salon {code} ouvert";
                enCours = null;
                Change?.Invoke();
                yield break;
            }

            etat = null;
            enCours = null;
            Signaler($"Impossible d'ouvrir un salon : ports {premierPort} à {premierPort + portsEssayes - 1} pris. Redémarrer Unity.");
        }

        private IEnumerator RejoindreSalon(string code)
        {
            DerniereErreur = "";
            fermeturePrevenue = false;
            if (!EnAttente) Etat = $"Recherche du salon {code}…";
            Change?.Invoke();

            string adresse = null;
            ushort port = 0;
            yield return recherche.Chercher(code, (a, p) => { adresse = a; port = p; }, () => { });

            enCours = null;
            if (adresse == null)
            {
                Signaler($"Aucun salon {code} trouvé sur ce réseau. Bon code ? L'hôte a-t-il ouvert son salon ?");
                yield break;
            }

            if (!EnAttente) Etat = $"Connexion au salon {code} ({adresse}:{port})…";
            Change?.Invoke();
            porte.CodeAPresenter = code;
            if (transport != null) transport.SetConnectionData(adresse, port);   // remet aussi le mode direct
            reseau.ClientManager.StartConnection(adresse, port);
        }

        // ---------------------------------------------------------------- En ligne (Unity Relay, P2-05)

        private IEnumerator CreerEnLigne()
        {
            DerniereErreur = "";
            Etat = "Création du salon en ligne…";
            Change?.Invoke();

            var reservation = relais.Heberger(EtatSalon.JoueursMax);
            yield return new WaitUntil(() => reservation.IsCompleted);
            var resultat = reservation.IsFaulted ? null : reservation.Result;
            if (resultat == null)
            {
                enCours = null;
                Signaler(string.IsNullOrEmpty(relais.Erreur) ? "Impossible de créer le salon en ligne." : relais.Erreur);
                yield break;
            }

            string code = CodeSalon.Normaliser(resultat.Value.code);
            etat = new EtatSalon(code, couleurs.Length);
            porte.Juge = etat.Examiner;
            transport.SetRelayServerData(resultat.Value.donnees);

            etatServeur = LocalConnectionState.Starting;
            reseau.ServerManager.StartConnection();
            yield return new WaitUntil(() => etatServeur == LocalConnectionState.Started
                                             || etatServeur == LocalConnectionState.Stopped);
            if (etatServeur != LocalConnectionState.Started)
            {
                etat = null;
                enCours = null;
                Signaler("Impossible d'ouvrir le salon en ligne (relais injoignable).");
                yield break;
            }

            NetworkObject salon = Instantiate(prefabSalonReseau);
            reseau.ServerManager.Spawn(salon);
            SalonReseau.Actuel?.Publier(etat);

            porte.PreparerHote();
            porte.CodeAPresenter = code;
            reseau.ClientManager.StartConnection();   // le client de l'hôte, par le même relais
            Etat = $"Salon {code} ouvert (en ligne)";
            enCours = null;
            Change?.Invoke();
        }

        private IEnumerator RejoindreEnLigne(string code)
        {
            DerniereErreur = "";
            fermeturePrevenue = false;
            if (!EnAttente) Etat = $"Connexion au salon {code} (en ligne)…";
            Change?.Invoke();

            var place = relais.Rejoindre(code);
            yield return new WaitUntil(() => place.IsCompleted);
            var donnees = place.IsFaulted ? null : place.Result;
            enCours = null;
            if (donnees == null)
            {
                Signaler(string.IsNullOrEmpty(relais.Erreur) ? "Impossible de rejoindre." : relais.Erreur);
                yield break;
            }

            porte.CodeAPresenter = code;
            transport.SetRelayServerData(donnees.Value);
            reseau.ClientManager.StartConnection();
        }

        private void Fermer()
        {
            if (retourPrevu != null) StopCoroutine(retourPrevu);
            retourPrevu = null;
            recherche.Arreter();
            porte.OublierHote();
            if (reseau == null) return;
            // P2-06 : l'hôte prévient tout le monde avant de couper (« L'hôte a quitté la partie » plutôt que « connexion
            // perdue ») ; le message part avant les messages de déconnexion de FishNet.
            if (reseau.IsServerStarted)
                reseau.ServerManager.Broadcast(new FermetureSalon { EnPartie = etat != null && etat.Phase == PhaseSalon.EnPartie });
            // Le serveur d'abord : il prévient chaque joueur (le client de l'hôte compris) pendant que tous sont encore
            // là. Dans l'autre ordre, le transport Unity écrit « stale connection » en rouge (vu le 18/11/2026, P2-05).
            if (reseau.IsServerStarted) reseau.ServerManager.StopConnection(true);
            if (reseau.IsClientStarted) reseau.ClientManager.StopConnection();
        }

        // ---------------------------------------------------------------- Hôte : décisions

        internal void QuandPret(NetworkConnection qui, bool pret)
        {
            if (qui != null && etat.SeDirePret(qui.ClientId, pret)) Publier();
        }

        internal void QuandCouleur(NetworkConnection qui, int couleur)
        {
            if (qui != null && etat.ChoisirCouleur(qui.ClientId, couleur)) Publier();
        }

        internal void QuandLancement(NetworkConnection qui)
        {
            if (!porte.EstLHote(qui) || !etat.Lancer()) return;
            Publier();
            // Additive : la scène du salon (et son NetworkManager) reste chargée.
            reseau.SceneManager.LoadGlobalScenes(new SceneLoadData(scenePartie) { ReplaceScenes = ReplaceOption.None });
        }

        internal void QuandRetour(NetworkConnection qui)
        {
            if (porte.EstLHote(qui)) RevenirChezSoi();
        }

        private void RevenirChezSoi()
        {
            if (etat == null || etat.Phase != PhaseSalon.EnPartie) return;
            if (retourPrevu != null) StopCoroutine(retourPrevu);
            retourPrevu = null;

            if (partie != null && partie.IsSpawned) reseau.ServerManager.Despawn(partie);
            partie = null;
            foreach (NetworkObject joueur in joueurs.Values)
                if (joueur != null && joueur.IsSpawned) reseau.ServerManager.Despawn(joueur);
            joueurs.Clear();

            reseau.SceneManager.UnloadGlobalScenes(new SceneUnloadData(scenePartie));
            etat.RevenirAuSalon();
            Publier();
        }

        private void Publier() => SalonReseau.Actuel?.Publier(etat);

        /// <summary>Chaque joueur reçoit son personnage quand il a fini de charger la scène de partie.</summary>
        private void QuandPresenceChange(ClientPresenceChangeEventArgs args)
        {
            if (etat == null || etat.Phase != PhaseSalon.EnPartie || !args.Added) return;
            if (args.Scene.name != scenePartie || prefabJoueur == null) return;

            NetworkConnection conn = args.Connection;
            int i = etat.Trouver(conn.ClientId);
            if (i < 0 || joueurs.ContainsKey(conn.ClientId)) return;

            int numero = etat.Membres[i].Numero;
            Transform ou = apparitions != null && apparitions.Length >= numero ? apparitions[numero - 1] : null;
            NetworkObject joueur = Instantiate(prefabJoueur,
                ou != null ? ou.position : Vector3.up * 2f, ou != null ? ou.rotation : Quaternion.identity);
            reseau.ServerManager.Spawn(joueur, conn);
            joueurs[conn.ClientId] = joueur;
            CommencerSiTousLa();
        }

        /// <summary>
        /// Quand chaque joueur du salon a son personnage, les règles de partie apparaissent et la partie commence
        /// (P2-04) : l'hôte seul décide, les autres reçoivent.
        /// </summary>
        private void CommencerSiTousLa()
        {
            if (prefabPartie == null || partie != null || etat == null || etat.Phase != PhaseSalon.EnPartie) return;
            foreach (MembreSalon m in etat.Membres)
                if (!joueurs.ContainsKey(m.ClientId)) return;

            partie = Instantiate(prefabPartie);
            reseau.ServerManager.Spawn(partie);
            var porteurs = new List<Froggy.Regles.PorteurCookies>();
            foreach (NetworkObject j in joueurs.Values)
            {
                var p = j != null ? j.GetComponent<Froggy.Regles.PorteurCookies>() : null;
                if (p != null) porteurs.Add(p);
            }
            partie.GetComponent<PartieReseau>().Commencer(porteurs);
        }

        /// <summary>Fin de partie (P3-03) : retour au salon après un court délai.</summary>
        private void QuandEvenement(EvenementPartie e)
        {
            if (e.Type != TypeEvenement.PartieFinie || etat == null || etat.Phase != PhaseSalon.EnPartie) return;
            if (retourPrevu == null) retourPrevu = StartCoroutine(RetourApres(delaiRetour));
        }

        private IEnumerator RetourApres(float secondes)
        {
            yield return new WaitForSeconds(secondes);
            retourPrevu = null;
            RevenirChezSoi();
        }

        // ---------------------------------------------------------------- Événements FishNet

        private void QuandServeurChange(ServerConnectionStateArgs args)
        {
            etatServeur = args.ConnectionState;
            if (etatServeur != LocalConnectionState.Stopped || enCours != null) return;
            etat = null;
            joueurs.Clear();
            partie = null;
            recherche.Arreter();
        }

        private void QuandAuthentifie(NetworkConnection conn, bool accepte)
        {
            if (!accepte || etat == null) return;
            // Deux arrivées en même temps peuvent passer la porte toutes les deux : la seconde est renvoyée.
            if (!etat.Accueillir(conn.ClientId, porte.EstLHote(conn)))
            {
                conn.Disconnect(false);
                return;
            }
            Publier();
        }

        private void QuandConnexionDistante(NetworkConnection conn, RemoteConnectionStateArgs args)
        {
            if (args.ConnectionState != RemoteConnectionState.Stopped || etat == null) return;
            // P2-06 : parti en pleine partie → ses cookies (et le cookie sacré) tombent, la partie continue. FishNet nous
            // prévient avant de détruire ses objets : sa Froggy et son numéro sont encore là.
            if (etat.Phase == PhaseSalon.EnPartie && PartieReseau.Actuelle != null && NumeroDe(conn.ClientId, out int numero))
                PartieReseau.Actuelle.JoueurParti(numero);
            if (joueurs.TryGetValue(conn.ClientId, out NetworkObject joueur))
            {
                if (joueur != null && joueur.IsSpawned) reseau.ServerManager.Despawn(joueur);
                joueurs.Remove(conn.ClientId);
            }
            if (etat.Retirer(conn.ClientId)) Publier();
            CommencerSiTousLa(); // celui qui manquait est parti pendant le chargement
        }

        private void QuandClientChange(ClientConnectionStateArgs args)
        {
            if (args.ConnectionState == LocalConnectionState.Stopped)
            {
                if (!EstHote && enCours == null && string.IsNullOrEmpty(DerniereErreur) && Etat != "Hors ligne")
                    Signaler(!fermeturePrevenue ? "Connexion perdue avec l'hôte."
                             : fermetureEnPartie ? "L'hôte a quitté la partie." : "L'hôte a fermé le salon.");
                fermeturePrevenue = false;
                if (EstHote) Fermer();
                if (!EnAttente) Etat = string.IsNullOrEmpty(DerniereErreur) ? "Hors ligne" : DerniereErreur;

                // Coupé en pleine partie (l'hôte est parti) : on retire le décor de la partie, resté chez nous ; les menus
                // (P6-03) reviennent alors sur « Rejoindre un salon » avec le message (P2-06).
                var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByName(scenePartie);
                if (scene.isLoaded && UnityEngine.SceneManagement.SceneManager.sceneCount > 1)
                    UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(scene);
            }
            Change?.Invoke();
        }

        private void QuandReponse(RefusSalon refus, float reste)
        {
            switch (refus)
            {
                case RefusSalon.Aucun:
                    EnAttente = false;
                    Etat = EstHote ? $"Salon {porte.CodeAPresenter} ouvert" : $"Dans le salon {porte.CodeAPresenter}";
                    Change?.Invoke();
                    break;
                case RefusSalon.CodeInconnu: Signaler("Code inconnu de cet hôte."); break;
                case RefusSalon.SalonPlein: Signaler($"Salon plein ({EtatSalon.JoueursMax} joueurs)."); break;
                case RefusSalon.PartieEnCours:
                    // P2-06 : on attend la fin de la partie, puis on réessaie tout seul
                    EnAttente = true;
                    codeAttendu = porte.CodeAPresenter;
                    Etat = MessageAttente(reste);
                    if (enCours != null) StopCoroutine(enCours);
                    enCours = StartCoroutine(AttendreFinDePartie());
                    Change?.Invoke();
                    break;
            }
        }

        /// <summary>« Partie en cours : vous entrerez dès qu'elle sera finie (≈ 1:23). »</summary>
        public static string MessageAttente(float reste) =>
            reste > 0f ? $"Partie en cours : vous entrerez dès qu'elle sera finie (≈ {Froggy.Regles.DeroulementPartie.Temps(reste)})."
                       : "Partie en cours : vous entrerez dès qu'elle sera finie.";

        /// <summary>Arrivé pendant une partie : quelques secondes, puis on refrappe à la porte avec le même code.</summary>
        private IEnumerator AttendreFinDePartie()
        {
            yield return new WaitForSecondsRealtime(AttenteEntreEssais);
            yield return new WaitUntil(() => !reseau.IsClientStarted);   // la porte a coupé après sa réponse
            enCours = StartCoroutine(enLigne ? RejoindreEnLigne(codeAttendu) : RejoindreSalon(codeAttendu));
        }

        /// <summary>Hôte : temps restant de la partie en cours, pour celui qui attend (0 : inconnu).</summary>
        private float TempsRestantPartie()
        {
            var partie = PartieReseau.Actuelle;
            var regles = partie != null && partie.Deroulement != null ? partie.Deroulement.Regles : null;
            return regles != null ? regles.Reste + delaiRetour : 0f;   // + le délai avant le retour au salon
        }

        private void QuandFermeture(FermetureSalon fermeture, Channel canal)
        {
            fermeturePrevenue = true;
            fermetureEnPartie = fermeture.EnPartie;
        }

        private void Relayer() => Change?.Invoke();

        private void Signaler(string message)
        {
            EnAttente = false;
            DerniereErreur = message;
            Etat = message;
            Debug.LogWarning($"[Salon] {message}", this);
            Erreur?.Invoke(message);
            Change?.Invoke();
        }

        /// <summary>Essaie d'ouvrir le port un instant (même parade que TestReseau, P2-07).</summary>
        private static bool PortLibre(ushort port)
        {
            try
            {
                using var essai = new System.Net.Sockets.UdpClient(port);
                return true;
            }
            catch (System.Net.Sockets.SocketException) { return false; }
        }
    }
}
