using System;
using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;

namespace Froggy.Reseau
{
    /// <summary>
    /// Le salon tel que tout le monde le voit (P2-03) : code, phase et joueurs, recopiés de l'hôte vers chacun.
    /// Apparu par l'hôte à l'ouverture du salon, global (il survit au chargement de la partie).
    /// Les demandes (prêt, couleur, lancer, revenir) remontent à l'hôte, qui décide dans Salon.
    /// Ne pas l'utiliser directement depuis un écran : passer par Salon.
    /// </summary>
    public class SalonReseau : NetworkBehaviour
    {
        private readonly SyncVar<string> code = new("");
        private readonly SyncVar<PhaseSalon> phase = new(PhaseSalon.Salon);
        private readonly SyncList<MembreSalon> membres = new();

        /// <summary>Celui de la session en cours, ou null.</summary>
        public static SalonReseau Actuel { get; private set; }

        /// <summary>Quelque chose a changé (code, phase ou joueurs).</summary>
        public static event Action Change;

        public string Code => code.Value;
        public PhaseSalon Phase => phase.Value;
        public IReadOnlyList<MembreSalon> Membres => membres;

        private void Awake()
        {
            code.OnChange += (_, _, _) => Change?.Invoke();
            phase.OnChange += (_, _, _) => Change?.Invoke();
            membres.OnChange += (_, _, _, _, _) => Change?.Invoke();
        }

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            Actuel = this;
            Change?.Invoke();
        }

        public override void OnStopNetwork()
        {
            base.OnStopNetwork();
            if (Actuel == this) Actuel = null;
            Change?.Invoke();
        }

        /// <summary>Hôte seulement : recopie l'état du salon pour tout le monde.</summary>
        [Server]
        public void Publier(EtatSalon etat)
        {
            code.Value = etat.Code;
            phase.Value = etat.Phase;
            membres.Clear();
            foreach (MembreSalon m in etat.Membres) membres.Add(m);
        }

        // sans rechargement du domaine à l'entrée en Play, les abonnés et le salon d'avant resteraient
        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Vider()
        {
            Change = null;
            Actuel = null;
        }

        // Demandes des joueurs : l'appelant est le dernier paramètre, rempli par FishNet côté hôte.

        [ServerRpc(RequireOwnership = false)]
        public void DemanderPret(bool pret, NetworkConnection appelant = null) => Salon.Serveur?.QuandPret(appelant, pret);

        [ServerRpc(RequireOwnership = false)]
        public void DemanderCouleur(int couleur, NetworkConnection appelant = null) => Salon.Serveur?.QuandCouleur(appelant, couleur);

        [ServerRpc(RequireOwnership = false)]
        public void DemanderLancement(NetworkConnection appelant = null) => Salon.Serveur?.QuandLancement(appelant);

        [ServerRpc(RequireOwnership = false)]
        public void DemanderRetour(NetworkConnection appelant = null) => Salon.Serveur?.QuandRetour(appelant);
    }
}
