using System.Collections.Generic;
using UnityEngine;

namespace Froggy.Regles
{
    /// <summary>Où est le cookie sacré.</summary>
    public enum EtatCookieSacre
    {
        /// <summary>Pas encore là : la dernière minute n'a pas commencé.</summary>
        Absent,
        /// <summary>Le gobelin l'emporte en fuyant.</summary>
        Gobelin,
        /// <summary>Un joueur le porte.</summary>
        Porte,
        /// <summary>Lâché, il attend au sol le premier qui passe.</summary>
        AuSol,
    }

    /// <summary>
    /// Les règles du cookie sacré et de son gobelin (P5-08), sans réseau ni scène. Quand il reste « moment » secondes de
    /// jeu (la dernière minute), le gobelin apparaît avec le cookie sacré. Le premier joueur à portée l'attrape et porte
    /// le cookie sacré ; frappé ou gelé, il le lâche (Lacher) et le premier qui passe à portée le ramasse, sauf lui pendant
    /// le délai de reprise. À la fin, ReglesPartie fait gagner le porteur (déjà écrit, P3-03).
    /// Du C# pur comme ReglesCookies : à entrées égales (temps restant, positions), le résultat est le même partout, et
    /// l'hôte le fera tourner tel quel en réseau (P2-04). Le gobelin, lui, est déplacé par la scène : on lui donne sa
    /// position à chaque pas.
    /// </summary>
    public class ReglesCookieSacre
    {
        /// <summary>Ce qui a changé pendant un pas, pour la scène (afficher, émettre un événement).</summary>
        public enum Changement { Apparu, Attrape, Lache, Ramasse }

        private readonly float moment, rayonCapture, rayonRamassage, delaiReprise;
        private float attente;
        private int lacheur = ReglesPartie.Personne;

        /// <param name="moment">Temps de jeu restant auquel le gobelin apparaît, en s (Notion : la dernière minute, 60).</param>
        /// <param name="rayonCapture">Distance à laquelle un joueur attrape le gobelin, en m.</param>
        /// <param name="rayonRamassage">Distance à laquelle un joueur ramasse le cookie sacré au sol, en m.</param>
        /// <param name="delaiReprise">Temps pendant lequel celui qui l'a lâché ne peut pas le reprendre, en s.</param>
        public ReglesCookieSacre(float moment, float rayonCapture, float rayonRamassage, float delaiReprise)
        {
            this.moment = Mathf.Max(0f, moment);
            this.rayonCapture = Mathf.Max(0f, rayonCapture);
            this.rayonRamassage = Mathf.Max(0f, rayonRamassage);
            this.delaiReprise = Mathf.Max(0f, delaiReprise);
        }

        public EtatCookieSacre Etat { get; private set; } = EtatCookieSacre.Absent;

        /// <summary>Le joueur qui porte le cookie sacré, ou ReglesPartie.Personne.</summary>
        public int Porteur { get; private set; } = ReglesPartie.Personne;

        /// <summary>Où il attend, s'il est au sol.</summary>
        public Vector3 PositionAuSol { get; private set; }

        /// <summary>
        /// Un pas. « phase » et « reste » viennent de ReglesPartie ; « gobelin » est la position du gobelin (ignorée tant
        /// qu'il n'est pas là) ; « joueurs » leurs numéros et positions. Ce qui change s'ajoute à « changements ».
        /// En prolongation, le gobelin arrive tout de suite s'il n'est pas encore venu (une partie plus courte qu'une minute).
        /// </summary>
        public void Pas(float dt, Phase phase, float reste, Vector3 gobelin,
            IReadOnlyList<(int joueur, Vector3 position)> joueurs, List<(Changement quoi, int joueur)> changements)
        {
            bool enJeu = phase == Phase.EnJeu || phase == Phase.Prolongation;
            if (!enJeu) return;

            if (Etat == EtatCookieSacre.Absent)
            {
                if (phase == Phase.Prolongation || reste <= moment)
                {
                    Etat = EtatCookieSacre.Gobelin;
                    changements?.Add((Changement.Apparu, ReglesPartie.Personne));
                }
                return;   // il vient d'apparaître : on ne l'attrape pas dans le même pas
            }

            if (attente > 0f) attente -= dt;

            if (Etat == EtatCookieSacre.Gobelin && PlusProche(gobelin, rayonCapture, joueurs, ReglesPartie.Personne, out int qui))
            {
                Donner(qui);
                changements?.Add((Changement.Attrape, qui));
            }
            else if (Etat == EtatCookieSacre.AuSol)
            {
                int exclu = attente > 0f ? lacheur : ReglesPartie.Personne;
                if (PlusProche(PositionAuSol, rayonRamassage, joueurs, exclu, out int ramasseur))
                {
                    Donner(ramasseur);
                    changements?.Add((Changement.Ramasse, ramasseur));
                }
            }
        }

        /// <summary>
        /// Le porteur lâche le cookie sacré (une torgnole, un gel éclair : Notion). Il tombe à « position » ; celui qui
        /// l'a lâché attend le délai de reprise. Rend false si ce joueur ne le portait pas.
        /// </summary>
        public bool Lacher(int joueur, Vector3 position)
        {
            if (Etat != EtatCookieSacre.Porte || joueur != Porteur) return false;
            Etat = EtatCookieSacre.AuSol;
            PositionAuSol = position;
            lacheur = joueur;
            attente = delaiReprise;
            Porteur = ReglesPartie.Personne;
            return true;
        }

        private void Donner(int joueur)
        {
            Etat = EtatCookieSacre.Porte;
            Porteur = joueur;
        }

        /// <summary>Le joueur à portée le plus proche (à égalité, le plus petit numéro), hors « exclu ».</summary>
        private static bool PlusProche(Vector3 cible, float rayon, IReadOnlyList<(int joueur, Vector3 position)> joueurs,
            int exclu, out int gagnant)
        {
            gagnant = ReglesPartie.Personne;
            float meilleure = rayon * rayon;
            bool trouve = false;
            foreach (var (joueur, position) in joueurs)
            {
                if (joueur == exclu) continue;
                Vector3 ecart = position - cible;
                ecart.y = 0f;                                   // à plat : un joueur qui saute par-dessus l'attrape aussi
                float d = ecart.sqrMagnitude;
                if (d > meilleure || (trouve && d == meilleure && joueur > gagnant)) continue;
                meilleure = d;
                gagnant = joueur;
                trouve = true;
            }
            return trouve;
        }
    }
}
