using System.Collections.Generic;
using System.Linq;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using Froggy.Arene;
using Froggy.Deplacement;
using Froggy.Objets;
using Froggy.Regles;
using Froggy.Socle;
using UnityEngine;

namespace Froggy.Reseau
{
    /// <summary>L'objet tenu par un joueur et sa recharge, recopiés chez tous.</summary>
    public struct ObjetTenuReseau
    {
        public int Joueur;
        public int Objet;
        public float Recharge;
    }

    /// <summary>
    /// Les objets en réseau (#128) : <b>seul l'hôte décide</b> (PartieObjets, comme hors ligne : boîtes, tirage, prise,
    /// recharge, utilisation). Chacun reçoit les boîtes pleines et l'objet de chaque joueur ; chez un autre joueur, la
    /// touche demande l'utilisation à l'hôte. Quand un objet sert, l'hôte l'annonce à tous avec l'objet utilisé : chaque
    /// machine émet « objet utilisé », et les effets jouent partout (croassement, gel, camouflage). Les numéros suivent
    /// l'ordre du salon, les mêmes sur toutes les machines. Sur le préfabriqué de la partie en ligne.
    /// </summary>
    [RequireComponent(typeof(PartieObjets))]
    public class ObjetsReseau : NetworkBehaviour
    {
        private readonly SyncList<bool> boites = new();
        private readonly SyncList<ObjetTenuReseau> tenus = new();

        private PartieObjets objets;
        private bool aRefleter;
        private readonly List<bool> lues = new();

        private void Awake()
        {
            objets = GetComponent<PartieObjets>();
            boites.OnChange += (_, _, _, _, _) => aRefleter = true;
            tenus.OnChange += (_, _, _, _, _) => aRefleter = true;
        }

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            EvenementsPartie.Emis += Ecouter;
        }

        public override void OnStopNetwork()
        {
            base.OnStopNetwork();
            EvenementsPartie.Emis -= Ecouter;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            if (IsServerStarted) return;   // le client de l'hôte utilise ses objets pour de vrai
            objets.Miroir = true;
            objets.DemandeUtiliser = p => DemanderUtiliser(p.Joueur);
        }

        public override void OnStopClient()
        {
            base.OnStopClient();
            objets.DemandeUtiliser = null;
        }

        private void Ecouter(EvenementPartie e)
        {
            if (e.Type == TypeEvenement.PartieCommencee) Demarrer();
            // chez l'hôte : un objet vient de servir, on le dit à tous avec l'objet utilisé
            else if (e.Type == TypeEvenement.ObjetUtilise && IsServerStarted && e.Auteur != null)
            {
                var porteur = e.Auteur.GetComponent<PorteurObjet>();
                if (porteur != null && porteur.DernierUtilise != null)
                    UtiliseChezTous(porteur.Joueur, objets.Rang(porteur.DernierUtilise));
            }
        }

        // les Froggy des joueurs, dans l'ordre du salon : le même numéro d'objet sur toutes les machines
        private void Demarrer()
        {
            var reperes = FindObjectsByType<RepereArene>(FindObjectsSortMode.None).Where(r => r.sorte == RepereArene.Sorte.Boite);
            var froggys = FindObjectsByType<PorteurCookies>(FindObjectsSortMode.None)
                .Where(p => p.Joueur > 0 && p.GetComponent<DeplacementWow>() != null)
                .OrderBy(p => p.Joueur).Select(p => p.gameObject).ToList();
            objets.Demarrer(reperes, froggys, garderOrdre: true);
            aRefleter = true;
        }

        [ServerRpc(RequireOwnership = false)]
        private void DemanderUtiliser(int joueur, NetworkConnection appelant = null)
        {
            var porteur = Porteur(joueur);
            var cookies = porteur != null ? porteur.GetComponent<PorteurCookies>() : null;
            // un joueur n'utilise que l'objet de sa propre Froggy
            if (cookies == null || appelant == null || Salon.Local == null
                || !Salon.Local.NumeroDe(appelant.ClientId, out int numero) || numero != cookies.Joueur) return;
            objets.Utiliser(porteur);
        }

        [ObserversRpc(ExcludeServer = true)]
        private void UtiliseChezTous(int joueur, int rang)
        {
            var porteur = Porteur(joueur);
            var fiche = objets.Fiche(rang);
            if (porteur == null || fiche == null) return;
            porteur.Fixer(null, porteur.Recharge);
            porteur.Signaler(fiche);
            EvenementsPartie.Emettre(TypeEvenement.ObjetUtilise, porteur.gameObject);
        }

        private PorteurObjet Porteur(int joueur)
        {
            foreach (var p in objets.Porteurs) if (p != null && p.Joueur == joueur) return p;
            return null;
        }

        private void LateUpdate()
        {
            if (IsServerStarted) Publier();
            else if (aRefleter) Refleter();
        }

        // seules les différences partent ; la recharge, à 0,2 s près
        private void Publier()
        {
            ReglesObjets r = objets.Regles;
            if (r == null) return;
            for (int i = 0; i < r.NombrePoints; i++)
            {
                if (i >= boites.Count) boites.Add(r.Present(i));
                else if (boites[i] != r.Present(i)) boites[i] = r.Present(i);
            }
            while (boites.Count > r.NombrePoints) boites.RemoveAt(boites.Count - 1);

            int n = 0;
            foreach (var p in objets.Porteurs)
            {
                if (p == null) continue;
                var t = new ObjetTenuReseau { Joueur = p.Joueur, Objet = r.Objet(p.Joueur), Recharge = r.Recharge(p.Joueur) };
                if (n >= tenus.Count) tenus.Add(t);
                else if (tenus[n].Joueur != t.Joueur || tenus[n].Objet != t.Objet || Mathf.Abs(tenus[n].Recharge - t.Recharge) > 0.2f
                         || (t.Recharge == 0f && tenus[n].Recharge != 0f)) tenus[n] = t;
                n++;
            }
            while (tenus.Count > n) tenus.RemoveAt(tenus.Count - 1);
        }

        private void Refleter()
        {
            aRefleter = false;
            lues.Clear();
            foreach (bool b in boites) lues.Add(b);
            objets.MontrerBoites(lues);
            foreach (var t in tenus)
            {
                var porteur = Porteur(t.Joueur);
                if (porteur != null) porteur.Fixer(objets.Fiche(t.Objet), t.Recharge);
            }
        }
    }
}
