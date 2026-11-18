using System;
using System.IO;
using System.Threading.Tasks;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

namespace Froggy.Reseau
{
    /// <summary>
    /// Jouer par internet (P2-05) : passe par Unity Relay, sans ouvrir de ports. L'hôte réserve une place sur un serveur
    /// relais de Unity et reçoit un code ; les autres rejoignent avec ce code. Les joueurs n'ont aucun compte : chaque
    /// fenêtre se connecte anonymement aux services de Unity (une fois).
    ///
    /// Le projet doit être relié à Unity Cloud (identifiant dans ProjectSettings, lu par Application.cloudProjectId).
    /// API lues dans com.unity.services.multiplayer 2.4.1 installé (RelayService, AllocationUtils) le 18/11/2026.
    /// </summary>
    public class RelaisSalon : MonoBehaviour
    {
        [Tooltip("Chiffrement des échanges avec le relais : « dtls » (chiffré) ou « udp ».")]
        [SerializeField] private string protocole = "dtls";

        /// <summary>Le dernier problème, en une phrase pour le joueur.</summary>
        public string Erreur { get; private set; } = "";

        /// <summary>
        /// Se connecte aux services de Unity (anonyme). Sur une machine avec plusieurs fenêtres (Multiplayer Play Mode),
        /// chacune prend son propre profil, sinon elles seraient le même joueur pour Unity.
        /// </summary>
        public async Task<bool> Connecter()
        {
            Erreur = "";
            try
            {
                if (UnityServices.State == ServicesInitializationState.Uninitialized)
                {
                    var options = new InitializationOptions();
                    string profil = ProfilDeCetteFenetre();
                    if (!string.IsNullOrEmpty(profil)) options.SetProfile(profil);
                    await UnityServices.InitializeAsync(options);
                }
                while (UnityServices.State == ServicesInitializationState.Initializing) await Task.Yield();
                if (!AuthenticationService.Instance.IsSignedIn)
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                return true;
            }
            catch (Exception e)
            {
                Erreur = string.IsNullOrEmpty(Application.cloudProjectId)
                    ? "Le projet n'est pas relié à Unity Cloud (voir le guide réseau)."
                    : "Connexion aux services de Unity impossible : internet coupé ?";
                Debug.LogWarning($"[Relais] {Erreur} ({e.GetType().Name} : {e.Message})", this);
                return false;
            }
        }

        /// <summary>Hôte : réserve une place pour toute la partie (lui + les autres) ; rend le code et les données.</summary>
        public async Task<(string code, RelayServerData donnees)?> Heberger(int joueursMax)
        {
            if (!await Connecter()) return null;
            try
            {
                Allocation allocation = await RelayService.Instance.CreateAllocationAsync(Mathf.Max(1, joueursMax - 1));
                string code = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
                return (code, allocation.ToRelayServerData(protocole));
            }
            catch (Exception e)
            {
                Erreur = Message(e, "Impossible de créer le salon en ligne");
                Debug.LogWarning($"[Relais] {Erreur} ({e.GetType().Name} : {e.Message})", this);
                return null;
            }
        }

        /// <summary>Rejoint par le code ; rend les données du relais, ou null (voir Erreur).</summary>
        public async Task<RelayServerData?> Rejoindre(string code)
        {
            if (!await Connecter()) return null;
            try
            {
                JoinAllocation place = await RelayService.Instance.JoinAllocationAsync(code);
                return place.ToRelayServerData(protocole);
            }
            catch (Exception e)
            {
                Erreur = Message(e, "Impossible de rejoindre");
                Debug.LogWarning($"[Relais] {Erreur} ({e.GetType().Name} : {e.Message})", this);
                return null;
            }
        }

        private static string Message(Exception e, string debut)
        {
            if (e is RelayServiceException r)
            {
                switch (r.Reason)
                {
                    case RelayExceptionReason.JoinCodeNotFound:
                    case RelayExceptionReason.AllocationNotFound:
                    case RelayExceptionReason.InvalidRequest:
                        return "Code inconnu : salon fermé ou code mal tapé.";
                    case RelayExceptionReason.RateLimited:
                        return "Trop d'essais : attendre quelques secondes.";
                    case RelayExceptionReason.InactiveProject:
                        return $"{debut} : le service Relay n'est pas activé pour ce projet (Unity Cloud).";
                }
            }
            return $"{debut} : internet coupé ou service de Unity indisponible.";
        }

        /// <summary>
        /// Un profil par fenêtre de Multiplayer Play Mode (son dossier « mppm… »), le profil par défaut sinon.
        /// Règle de Unity : lettres, chiffres, « - » et « _ », 30 caractères au plus.
        /// </summary>
        private static string ProfilDeCetteFenetre()
        {
            if (!Application.isEditor) return null;
            string dossier = Path.GetFileName(Path.GetDirectoryName(Application.dataPath)) ?? "";
            return dossier.StartsWith("mppm", StringComparison.Ordinal)
                ? dossier.Length > 30 ? dossier.Substring(0, 30) : dossier
                : "principal";
        }
    }
}
