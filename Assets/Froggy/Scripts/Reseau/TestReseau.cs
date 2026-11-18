using System.Collections;
using System.IO;
using FishNet.Managing;
using FishNet.Transporting;
using Unity.Multiplayer.PlayMode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Froggy.Reseau
{
    /// <summary>
    /// Tests à plusieurs sur une machine (P2-07), posé sur le NetworkManager du prefab TestReseau.
    /// En éditeur, avec Multiplayer Play Mode : la fenêtre principale devient l'hôte, chaque Player 2-4 se connecte seul
    /// (avec réessais, car tout le monde démarre en même temps). Applique aussi le préréglage de connexion au simulateur
    /// de latence de FishNet. En build : rien d'automatique, on garde les boutons du HUD.
    ///
    /// Port : dans l'éditeur, FishNet 4.7.4 garde parfois le port du serveur ouvert après l'arrêt du Play (constaté le
    /// 18/11/2026, jusqu'au redémarrage de Unity). L'hôte prend donc le premier port libre à partir de celui du
    /// transport et l'écrit dans un fichier du dossier temporaire de Windows, que les Player 2-4 relisent à chaque essai.
    /// </summary>
    [RequireComponent(typeof(NetworkManager))]
    public class TestReseau : MonoBehaviour
    {
        public enum Role { Manuel, Hote, Client }

        /// <summary>Scène ouverte par le menu et rechargée par un Player 2-4 parti sur une scène vide.</summary>
        public const string SceneDeTest = "Assets/Froggy/Postes/2_Reseau/Test_Reseau.unity";

        [SerializeField] private ReglagesTestReseau reglages;

        [Tooltip("Secondes entre deux essais de connexion d'un client.")]
        [SerializeField] private float delaiEntreEssais = 1f;

        [SerializeField] private int essaisMax = 15;

        [Tooltip("Nombre de ports essayés par l'hôte, à partir de celui du transport (7770).")]
        [SerializeField] private int portsEssayes = 10;

        private NetworkManager reseau;
        private LocalConnectionState etatServeur = LocalConnectionState.Stopped;
        private LocalConnectionState etatClient = LocalConnectionState.Stopped;

        public Role RoleActuel { get; private set; } = Role.Manuel;
        public string Etat { get; private set; } = "";
        public int Prereglage { get; private set; }
        /// <summary>Port de l'hôte (pour un client, celui qu'il a visé : le transport rend sinon son port de sortie).</summary>
        public ushort Port { get; private set; }
        public ReglagesTestReseau Reglages => reglages;
        public NetworkManager Reseau => reseau;

        /// <summary>Vrai dans une fenêtre Player 2-4 de Multiplayer Play Mode (faux dans l'éditeur principal et en build).</summary>
        public static bool EstUnJoueurVirtuel => Application.isEditor && !CurrentPlayer.IsMainEditor;

        /// <summary>Fichier où l'hôte note son port : le dossier temporaire de Windows est commun à toutes les fenêtres.</summary>
        private static string FichierPort => Path.Combine(Path.GetTempPath(), "froggy-test-reseau-port.txt");

        private void Awake()
        {
            reseau = GetComponent<NetworkManager>();
            Prereglage = reglages != null ? reglages.prereglageActif : 0;
        }

        private void OnEnable()
        {
            reseau.ServerManager.OnServerConnectionState += QuandServeurChange;
            reseau.ClientManager.OnClientConnectionState += QuandClientChange;
        }

        private void OnDisable()
        {
            if (reseau == null) return;
            reseau.ServerManager.OnServerConnectionState -= QuandServeurChange;
            reseau.ClientManager.OnClientConnectionState -= QuandClientChange;
        }

        private void OnApplicationQuit()
        {
            // Fermer explicitement à l'arrêt du Play : ne pas compter sur la destruction des objets.
            if (reseau == null) return;
            if (reseau.IsClientStarted) reseau.ClientManager.StopConnection();
            if (reseau.IsServerStarted) reseau.ServerManager.StopConnection(true);
        }

        private void Start()
        {
            Appliquer(Prereglage);

            if (!Application.isEditor || reglages == null || !reglages.demarrageAuto)
            {
                Etat = "Démarrage manuel : boutons Server / Client";
                return;
            }

            if (CurrentPlayer.IsMainEditor)
            {
                RoleActuel = Role.Hote;
                StartCoroutine(DemarrerHote());
            }
            else
            {
                RoleActuel = Role.Client;
                StartCoroutine(ConnecterLeClient());
            }
        }

        /// <summary>Applique un préréglage au simulateur de latence de cette fenêtre seulement.</summary>
        public void Appliquer(int index)
        {
            if (reglages == null) return;
            Prereglage = Mathf.Clamp(index, 0, Mathf.Max(0, reglages.prereglages.Count - 1));
            reglages.Appliquer(reseau, Prereglage);
        }

        public void PrereglageSuivant()
        {
            if (reglages == null || reglages.prereglages.Count == 0) return;
            Appliquer((Prereglage + 1) % reglages.prereglages.Count);
        }

        private IEnumerator DemarrerHote()
        {
            Transport transport = reseau.TransportManager.Transport;
            ushort premier = transport.GetPort();

            for (int i = 0; i < portsEssayes; i++)
            {
                ushort port = (ushort)(premier + i);
                if (!PortLibre(port))
                {
                    Debug.Log($"[Test réseau] Port {port} encore tenu par un Play précédent : essai du suivant.");
                    continue;
                }
                Etat = $"Démarrage de l'hôte (port {port})…";
                transport.SetPort(port);

                etatServeur = LocalConnectionState.Starting;
                reseau.ServerManager.StartConnection();
                yield return new WaitUntil(() => etatServeur == LocalConnectionState.Started
                                                 || etatServeur == LocalConnectionState.Stopped);

                if (etatServeur != LocalConnectionState.Started)
                {
                    Debug.LogWarning($"[Test réseau] Port {port} déjà pris, essai du suivant.", this);
                    continue;
                }

                Port = port;
                EcrirePort(port);
                reseau.ClientManager.StartConnection();
                yield break;
            }

            Etat = $"Serveur impossible : ports {premier} à {premier + portsEssayes - 1} pris. Redémarrer Unity.";
            Debug.LogError($"[Test réseau] {Etat}", this);
        }

        private IEnumerator ConnecterLeClient()
        {
            // L'hôte démarre dans une autre fenêtre au même moment : on lui laisse un instant.
            yield return new WaitForSeconds(delaiEntreEssais);
            Transport transport = reseau.TransportManager.Transport;

            for (int essai = 1; essai <= essaisMax; essai++)
            {
                ushort? port = LirePort();
                if (port.HasValue) transport.SetPort(port.Value);
                Port = transport.GetPort();
                Etat = $"Connexion à l'hôte, port {Port}… (essai {essai}/{essaisMax})";

                // Noté « en cours » avant de lancer : sinon l'attente lit l'ancien « arrêté » tout de suite et une
                // seconde connexion part pendant que la première s'établit.
                etatClient = LocalConnectionState.Starting;
                reseau.ClientManager.StartConnection();

                float limite = Time.realtimeSinceStartup + 20f;
                yield return new WaitUntil(() => etatClient == LocalConnectionState.Started
                                                 || etatClient == LocalConnectionState.Stopped
                                                 || Time.realtimeSinceStartup > limite);

                if (etatClient == LocalConnectionState.Started) yield break;
                yield return new WaitForSeconds(delaiEntreEssais);
            }

            Etat = "Pas d'hôte trouvé : la fenêtre principale est-elle en Play ?";
            Debug.LogWarning($"[Test réseau] {Etat}", this);
        }

        /// <summary>Essaie d'ouvrir le port un instant : évite l'erreur rouge de FishNet sur un port déjà pris.</summary>
        private static bool PortLibre(ushort port)
        {
            try
            {
                using var essai = new System.Net.Sockets.UdpClient(port);
                return true;
            }
            catch (System.Net.Sockets.SocketException) { return false; }
        }

        private static void EcrirePort(ushort port)
        {
            try { File.WriteAllText(FichierPort, port.ToString()); }
            catch (IOException e) { Debug.LogWarning($"[Test réseau] Port non noté ({e.Message}) : les clients essaieront 7770."); }
        }

        private static ushort? LirePort()
        {
            try { return ushort.TryParse(File.ReadAllText(FichierPort).Trim(), out ushort p) ? p : null; }
            catch (IOException) { return null; }
        }

        private void QuandServeurChange(ServerConnectionStateArgs args) => etatServeur = args.ConnectionState;

        private void QuandClientChange(ClientConnectionStateArgs args)
        {
            etatClient = args.ConnectionState;
            if (etatClient == LocalConnectionState.Started)
                Etat = RoleActuel == Role.Hote ? $"Hôte prêt (port {Port})" : $"Connecté à l'hôte (port {Port})";
        }

#if UNITY_EDITOR
        /// <summary>
        /// Une fenêtre Player 2-4 peut démarrer sur une scène vide (Multiplayer Play Mode ne recopie que les scènes
        /// sauvegardées de la fenêtre principale) : on charge alors la scène de test.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void SecoursSceneVide()
        {
            if (!EstUnJoueurVirtuel) return;
            Scene active = SceneManager.GetActiveScene();
            if (SceneManager.sceneCount > 1 || !string.IsNullOrEmpty(active.path) || active.rootCount > 0) return;

            Debug.LogWarning($"[Test réseau] Ce joueur a démarré sur une scène vide : chargement de {SceneDeTest}.");
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(
                SceneDeTest, new LoadSceneParameters(LoadSceneMode.Single));
        }
#endif
    }
}
