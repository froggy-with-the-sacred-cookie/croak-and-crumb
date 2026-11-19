using System;
using FishNet.Authenticating;
using FishNet.Broadcast;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Transporting;
using UnityEngine;

namespace Froggy.Reseau
{
    /// <summary>Envoyé par celui qui arrive, avant d'être accepté.</summary>
    public struct DemandeEntreeSalon : IBroadcast
    {
        public string Code;
        /// <summary>Jeton connu seulement du processus de l'hôte : son propre client le joint pour être reconnu.</summary>
        public string JetonHote;
    }

    /// <summary>Réponse de l'hôte : Aucun = accepté.</summary>
    public struct ReponseEntreeSalon : IBroadcast
    {
        public RefusSalon Refus;
        /// <summary>Partie en cours (P2-06) : secondes avant la fin, environ ; 0 si inconnu.</summary>
        public float Reste;
    }

    /// <summary>L'hôte ferme le salon (P2-06) : envoyé à tous juste avant l'arrêt, pour dire pourquoi on est coupé.</summary>
    public struct FermetureSalon : IBroadcast
    {
        public bool EnPartie;
    }

    /// <summary>
    /// Porte du salon (P2-03), posée sur le NetworkManager (FishNet la trouve seul). Celui qui arrive donne le code ;
    /// l'hôte refuse un mauvais code, un salon plein ou une partie en cours, et dit pourquoi avant de couper.
    /// Écrit d'après l'exemple PasswordAuthenticator de FishNet 4.7.4 (Demos/Authenticator).
    /// </summary>
    public class AuthentificateurSalon : Authenticator
    {
        public override event Action<NetworkConnection, bool> OnAuthenticationResult;

        /// <summary>Côté hôte : décide pour un code donné. Sans juge, tout le monde est refusé.</summary>
        public Func<string, RefusSalon> Juge { get; set; }

        /// <summary>Côté client : le code à présenter à la prochaine connexion.</summary>
        public string CodeAPresenter { get; set; } = "";

        /// <summary>Côté hôte : secondes avant la fin de la partie en cours (P2-06), pour celui qui attend.</summary>
        public Func<float> TempsRestant { get; set; }

        /// <summary>Côté client : l'hôte a répondu (Aucun = accepté) ; en partie, le temps restant en secondes.</summary>
        public event Action<RefusSalon, float> Reponse;

        private string jetonHote = "";

        /// <summary>Côté hôte, avant de démarrer son propre client : nouveau jeton.</summary>
        public void PreparerHote() => jetonHote = Guid.NewGuid().ToString("N");

        public void OublierHote() => jetonHote = "";

        /// <summary>Vrai pour la connexion du client de l'hôte lui-même (reconnu à l'entrée).</summary>
        public bool EstLHote(NetworkConnection conn) => conn != null && conn.CustomData is MarqueHote;

        private sealed class MarqueHote { }

        public override void InitializeOnce(NetworkManager networkManager)
        {
            base.InitializeOnce(networkManager);
            NetworkManager.ClientManager.OnClientConnectionState += QuandClientChange;
            // false : celui qui arrive n'est pas encore authentifié.
            NetworkManager.ServerManager.RegisterBroadcast<DemandeEntreeSalon>(QuandDemande, false);
            NetworkManager.ClientManager.RegisterBroadcast<ReponseEntreeSalon>(QuandReponse);
        }

        private void QuandClientChange(ClientConnectionStateArgs args)
        {
            if (args.ConnectionState != LocalConnectionState.Started) return;
            NetworkManager.ClientManager.Broadcast(new DemandeEntreeSalon
            {
                Code = CodeAPresenter,
                JetonHote = NetworkManager.IsServerStarted ? jetonHote : "",
            });
        }

        private void QuandDemande(NetworkConnection conn, DemandeEntreeSalon demande, Channel canal)
        {
            // Déjà accepté : demande en double, suspecte.
            if (conn.IsAuthenticated)
            {
                conn.Disconnect(true);
                return;
            }

            bool hote = !string.IsNullOrEmpty(jetonHote) && demande.JetonHote == jetonHote;
            RefusSalon refus = hote ? RefusSalon.Aucun
                             : Juge != null ? Juge(CodeSalon.Normaliser(demande.Code))
                             : RefusSalon.CodeInconnu;
            conn.CustomData = hote ? new MarqueHote() : null;

            // La réponse part avant le résultat : en cas de refus, FishNet coupe juste après.
            float reste = refus == RefusSalon.PartieEnCours && TempsRestant != null ? TempsRestant() : 0f;
            NetworkManager.ServerManager.Broadcast(conn, new ReponseEntreeSalon { Refus = refus, Reste = reste }, false);
            OnAuthenticationResult?.Invoke(conn, refus == RefusSalon.Aucun);
        }

        private void QuandReponse(ReponseEntreeSalon reponse, Channel canal) => Reponse?.Invoke(reponse.Refus, reponse.Reste);

        private void OnDestroy()
        {
            if (NetworkManager == null) return;
            NetworkManager.ClientManager.OnClientConnectionState -= QuandClientChange;
            NetworkManager.ServerManager.UnregisterBroadcast<DemandeEntreeSalon>(QuandDemande);
            NetworkManager.ClientManager.UnregisterBroadcast<ReponseEntreeSalon>(QuandReponse);
        }
    }
}
