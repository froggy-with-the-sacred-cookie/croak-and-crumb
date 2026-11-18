using FishNet.Connection;
using FishNet.Object;
using Froggy.Regles;
using Froggy.Socle;
using UnityEngine;

namespace Froggy.Reseau
{
    /// <summary>
    /// La torgnole en réseau (#128) : <b>seul l'hôte frappe</b> (PartieTorgnoles, comme hors ligne), avec sa copie de
    /// toutes les Froggy. Chez un autre joueur, la touche demande le coup à l'hôte ; l'hôte vérifie que la demande vient
    /// bien du joueur de cette Froggy. Le recul d'une Froggy pilotée par une autre machine part chez elle (elle seule fait
    /// bouger son corps). Le cookie lâché et le cookie sacré tombé arrivent déjà par PartieReseau et CookieSacreReseau ;
    /// chez les autres, le geste « Torgnole » se joue quand le coup leur parvient. Sur le préfabriqué de la partie en ligne.
    /// Pas encore : la recharge affichée chez les autres joueurs (leur HUD voit la torgnole toujours prête).
    /// </summary>
    [RequireComponent(typeof(PartieTorgnoles))]
    public class TorgnoleReseau : NetworkBehaviour
    {
        private PartieTorgnoles torgnoles;
        private PartieCookies cookies;

        private void Awake()
        {
            torgnoles = GetComponent<PartieTorgnoles>();
            cookies = GetComponent<PartieCookies>();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            torgnoles.ReculAilleurs = EnvoyerRecul;
        }

        public override void OnStopServer()
        {
            base.OnStopServer();
            torgnoles.ReculAilleurs = null;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            if (IsServerStarted) return;   // le client de l'hôte frappe pour de vrai
            torgnoles.DemandeFrappe = p => DemanderFrappe(p.Joueur);
            EvenementsPartie.Emis += JouerLeGeste;
        }

        public override void OnStopClient()
        {
            base.OnStopClient();
            torgnoles.DemandeFrappe = null;
            EvenementsPartie.Emis -= JouerLeGeste;
        }

        [ServerRpc(RequireOwnership = false)]
        private void DemanderFrappe(int joueur, NetworkConnection appelant = null)
        {
            // un joueur ne frappe que pour sa propre Froggy
            if (appelant == null || Salon.Local == null || !Salon.Local.NumeroDe(appelant.ClientId, out int numero)
                || numero != joueur) return;
            var frappeur = cookies != null ? cookies.Trouver(joueur) : null;
            if (frappeur != null) torgnoles.Frapper(frappeur);
        }

        // chez l'hôte : la cible est pilotée ailleurs, son propriétaire applique le recul
        private void EnvoyerRecul(PorteurCookies cible, Vector3 vitesse)
        {
            foreach (var f in FindObjectsByType<FroggyReseau>(FindObjectsSortMode.None))
                if (f.Froggy == cible.gameObject && f.Owner != null && f.Owner.IsValid)
                {
                    Reculer(f.Owner, vitesse);
                    return;
                }
        }

        [TargetRpc]
        private void Reculer(NetworkConnection conn, Vector3 vitesse)
        {
            var moi = FroggyReseau.Locale;
            var corps = moi != null && moi.Froggy != null ? moi.Froggy.GetComponent<Rigidbody>() : null;
            if (corps == null || corps.isKinematic) return;
            Vector3 v = corps.linearVelocity;
            corps.linearVelocity = new Vector3(vitesse.x, Mathf.Max(v.y, 0f), vitesse.z);
        }

        // le coup relayé par PartieReseau : le frappeur joue son geste ici aussi
        private void JouerLeGeste(EvenementPartie e)
        {
            if (e.Type == TypeEvenement.CoupDonne && e.Auteur != null)
                e.Auteur.SendMessage("Jouer", "Torgnole", SendMessageOptions.DontRequireReceiver);
        }
    }
}
