using System;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

namespace Froggy.Reseau
{
    /// <summary>
    /// Trouver un salon par son code sur le réseau local (P2-03). FishNet n'a pas de découverte réseau et le
    /// NetManager de Tugboat n'est pas accessible : on passe par un petit échange UDP à part, sur le port 7790.
    ///
    /// Celui qui rejoint diffuse « FROGGY?KRBF » (à tout le réseau local et à sa propre machine) ;
    /// l'hôte du salon KRBF répond « FROGGY!KRBF:7771 », avec son port de jeu ; son adresse est celle de la réponse.
    /// Le relais (P2-05) remplacera cette recherche ; le code affiché restera le même.
    /// </summary>
    public class RechercheSalon : MonoBehaviour
    {
        /// <summary>Port UDP de la recherche (le jeu est sur 7770-7779).</summary>
        public const int PortRecherche = 7790;
        private const string Question = "FROGGY?";
        private const string Reponse = "FROGGY!";

        [Tooltip("Secondes d'attente d'une réponse, par essai.")]
        [SerializeField] private float attente = 1f;
        [SerializeField] private int essais = 3;

        private UdpClient ecoute;
        private Thread fil;
        private volatile string codeAnnonce;
        private volatile int portAnnonce;

        public bool Annonce => ecoute != null;

        // ---- Hôte ----

        /// <summary>Répond désormais aux recherches de ce code. Faux si le port 7790 est indisponible.</summary>
        public bool Annoncer(string code, ushort portJeu)
        {
            Arreter();
            codeAnnonce = code;
            portAnnonce = portJeu;
            try
            {
                ecoute = new UdpClient { ExclusiveAddressUse = false };
                ecoute.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                ecoute.Client.Bind(new IPEndPoint(IPAddress.Any, PortRecherche));
            }
            catch (SocketException e)
            {
                Debug.LogWarning($"[Salon] Recherche indisponible (port {PortRecherche} : {e.Message}). " +
                                 "Personne ne pourra trouver ce salon par son code.", this);
                ecoute?.Close();
                ecoute = null;
                return false;
            }

            fil = new Thread(Ecouter) { IsBackground = true, Name = "Froggy recherche salon" };
            fil.Start();
            return true;
        }

        public void Arreter()
        {
            ecoute?.Close(); // débloque Receive dans le fil, qui s'arrête
            ecoute = null;
            fil = null;
        }

        private void Ecouter()
        {
            UdpClient socket = ecoute;
            while (socket != null)
            {
                try
                {
                    var depuis = new IPEndPoint(IPAddress.Any, 0);
                    string message = Encoding.ASCII.GetString(socket.Receive(ref depuis));
                    if (message != Question + codeAnnonce) continue;
                    byte[] reponse = Encoding.ASCII.GetBytes($"{Reponse}{codeAnnonce}:{portAnnonce}");
                    socket.Send(reponse, reponse.Length, depuis);
                }
                catch (ObjectDisposedException) { return; }
                catch (SocketException) { if (ecoute != socket) return; }
            }
        }

        // ---- Celui qui rejoint ----

        /// <summary>
        /// Cherche l'hôte du salon <paramref name="code"/>. Appelle <paramref name="trouve"/> (adresse, port) ou
        /// <paramref name="introuvable"/> après tous les essais.
        /// </summary>
        public IEnumerator Chercher(string code, Action<string, ushort> trouve, Action introuvable)
        {
            using var socket = new UdpClient(0) { EnableBroadcast = true };
            byte[] question = Encoding.ASCII.GetBytes(Question + code);

            for (int essai = 0; essai < essais; essai++)
            {
                Envoyer(socket, question, IPAddress.Broadcast);
                Envoyer(socket, question, IPAddress.Loopback);

                float limite = Time.realtimeSinceStartup + attente;
                while (Time.realtimeSinceStartup < limite)
                {
                    while (socket.Available > 0)
                    {
                        var depuis = new IPEndPoint(IPAddress.Any, 0);
                        byte[] recu;
                        try { recu = socket.Receive(ref depuis); }
                        catch (SocketException) { break; }
                        if (Lire(Encoding.ASCII.GetString(recu), code, out ushort port))
                        {
                            trouve(depuis.Address.ToString(), port);
                            yield break;
                        }
                    }
                    yield return null;
                }
            }
            introuvable();
        }

        private static void Envoyer(UdpClient socket, byte[] message, IPAddress adresse)
        {
            try { socket.Send(message, message.Length, new IPEndPoint(adresse, PortRecherche)); }
            catch (SocketException e) { Debug.LogWarning($"[Salon] Recherche vers {adresse} impossible : {e.Message}"); }
        }

        /// <summary>Lit « FROGGY!KRBF:7771 » ; faux si ce n'est pas la réponse attendue pour ce code.</summary>
        public static bool Lire(string message, string code, out ushort port)
        {
            port = 0;
            string debut = $"{Reponse}{code}:";
            return message != null && message.StartsWith(debut, StringComparison.Ordinal)
                   && ushort.TryParse(message.Substring(debut.Length), out port);
        }

        private void OnDestroy() => Arreter();
        private void OnApplicationQuit() => Arreter();
    }
}
