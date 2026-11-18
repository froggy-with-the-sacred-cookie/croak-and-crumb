using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using Froggy.Regles;
using UnityEngine;

namespace Froggy.Reseau
{
    /// <summary>L'état de réapparition d'un joueur (en jeu, tombé, intouchable), recopié chez tous.</summary>
    public struct EtatReapparitionReseau
    {
        public int Joueur;
        public EtatReapparition Etat;
    }

    /// <summary>
    /// La réapparition en réseau (#144) : <b>seul l'hôte décide</b> (ApparitionJoueurs, comme hors ligne) qui est tombé,
    /// quand et où il réapparaît, et qui est intouchable, avec sa copie de toutes les Froggy. Une Froggy pilotée par une
    /// autre machine reçoit sa place de réapparition, et son propriétaire la pose (lui seul fait bouger son corps).
    /// Chez les autres joueurs, ApparitionJoueurs passe en Miroir : l'état reçu fait clignoter les intouchables, et
    /// « joueur réapparu » arrive déjà par PartieReseau. Le salon a posé les Froggy au départ : ApparitionJoueurs ne les
    /// repose pas. Sur le préfabriqué de la partie en ligne, avec un repère « Apparition d'un joueur » par joueur.
    /// </summary>
    [RequireComponent(typeof(ApparitionJoueurs), typeof(PartieCookies))]
    public class ReapparitionReseau : NetworkBehaviour
    {
        private readonly SyncList<EtatReapparitionReseau> etats = new();

        private ApparitionJoueurs apparition;
        private PartieCookies cookies;
        private readonly List<EtatReapparitionReseau> voulus = new();

        private void Awake()
        {
            apparition = GetComponent<ApparitionJoueurs>();
            cookies = GetComponent<PartieCookies>();
            apparition.PoserAuDepart = false;
            etats.OnChange += (_, _, _, _, asServer) => { if (!asServer) Refleter(); };
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            apparition.Miroir = false;
            apparition.PoserAilleurs = Envoyer;
        }

        public override void OnStopServer()
        {
            base.OnStopServer();
            apparition.PoserAilleurs = null;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            if (IsServerStarted) return;   // le client de l'hôte voit déjà les vraies règles
            apparition.Miroir = true;
            Refleter();
        }

        // ---------------------------------------------------------------- Hôte

        // l'hôte n'écrit que ce qui a changé (canal fiable, envoi toutes les 0,1 s)
        private void LateUpdate()
        {
            if (!IsServerStarted || apparition.Regles == null) return;
            voulus.Clear();
            foreach (var p in cookies.Porteurs)
                if (p != null && p.Joueur > 0)
                    voulus.Add(new EtatReapparitionReseau { Joueur = p.Joueur, Etat = apparition.Etat(p.Joueur) });
            voulus.Sort((a, b) => a.Joueur.CompareTo(b.Joueur));
            for (int i = 0; i < voulus.Count; i++)
            {
                if (i >= etats.Count) etats.Add(voulus[i]);
                else if (etats[i].Joueur != voulus[i].Joueur || etats[i].Etat != voulus[i].Etat) etats[i] = voulus[i];
            }
            while (etats.Count > voulus.Count) etats.RemoveAt(etats.Count - 1);
        }

        // la Froggy est pilotée ailleurs : son propriétaire la pose
        private void Envoyer(PorteurCookies porteur, Vector3 position, Quaternion rotation)
        {
            foreach (var f in FindObjectsByType<FroggyReseau>(FindObjectsSortMode.None))
                if (f.Froggy == porteur.gameObject && f.Owner != null && f.Owner.IsValid)
                {
                    PoserChezLui(f.Owner, position, rotation);
                    return;
                }
        }

        [TargetRpc]
        private void PoserChezLui(NetworkConnection conn, Vector3 position, Quaternion rotation)
        {
            var moi = FroggyReseau.Locale;
            var porteur = moi != null && moi.Froggy != null ? moi.Froggy.GetComponent<PorteurCookies>() : null;
            if (porteur != null) apparition.Poser(porteur, position, rotation);
        }

        // ---------------------------------------------------------------- Les autres joueurs

        private void Refleter()
        {
            foreach (var e in etats) apparition.Refleter(e.Joueur, e.Etat);
        }
    }
}
