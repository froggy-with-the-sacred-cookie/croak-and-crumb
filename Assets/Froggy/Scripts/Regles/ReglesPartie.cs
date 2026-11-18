using System.Collections.Generic;

namespace Froggy.Regles
{
    /// <summary>Où en est une partie.</summary>
    public enum Phase
    {
        /// <summary>Pas encore lancée.</summary>
        Attente,
        /// <summary>Compte à rebours de départ : personne ne bouge.</summary>
        Decompte,
        EnJeu,
        /// <summary>Égalité à la fin du temps : on continue jusqu'à ce qu'un seul joueur soit en tête.</summary>
        Prolongation,
        Finie,
    }

    /// <summary>
    /// Le déroulement d'une partie (P3-03), sans réseau ni scène : compte à rebours, temps de jeu, fin, gagnant.
    /// Le gagnant est celui qui porte le cookie sacré, sinon celui qui porte le plus de cookies. À égalité, prolongation :
    /// tout le monde continue et la partie s'arrête dès qu'un seul joueur est en tête.
    /// Du C# pur, comme ReglesCookies : l'hôte le fera tourner tel quel (P2-04).
    /// </summary>
    public class ReglesPartie
    {
        /// <summary>Pas de joueur : personne ne porte le cookie sacré, ou pas de gagnant.</summary>
        public const int Personne = int.MinValue;

        private readonly float decompte, duree;

        public ReglesPartie(float decompte, float duree)
        {
            this.decompte = decompte < 0f ? 0f : decompte;
            this.duree = duree < 0f ? 0f : duree;
        }

        public Phase Phase { get; private set; } = Phase.Attente;

        /// <summary>Le temps qui reste dans la phase en cours, en s (compte à rebours ou partie). 0 en prolongation.</summary>
        public float Reste { get; private set; }

        /// <summary>Le temps passé en prolongation, en s.</summary>
        public float Prolonge { get; private set; }

        /// <summary>Le gagnant, une fois la partie finie. Personne si elle s'est finie sans joueur.</summary>
        public int Gagnant { get; private set; } = Personne;

        /// <summary>Lance la partie : le compte à rebours part. Sans effet si elle est déjà lancée.</summary>
        public void Lancer()
        {
            if (Phase != Phase.Attente) return;
            Phase = Phase.Decompte;
            Reste = decompte;
        }

        /// <summary>
        /// En réseau, chez un joueur qui n'est pas l'hôte (P2-04) : prend l'état décidé par l'hôte, pour l'afficher.
        /// </summary>
        public void Imposer(Phase phase, float reste, float prolonge, int gagnant)
        {
            Phase = phase;
            Reste = reste;
            Prolonge = prolonge;
            Gagnant = gagnant;
        }

        /// <summary>
        /// Un pas de la partie. « scores » donne le nombre de cookies de chaque joueur, « porteurCookieSacre » celui qui
        /// porte le cookie sacré (Personne s'il est au sol ou n'existe pas).
        /// </summary>
        public void Pas(float dt, IReadOnlyList<(int joueur, int cookies)> scores, int porteurCookieSacre = Personne)
        {
            switch (Phase)
            {
                case Phase.Decompte:
                    Reste -= dt;
                    if (Reste > 0f) return;
                    Phase = Phase.EnJeu;
                    Reste += duree;   // le dépassement du compte à rebours n'est pas perdu
                    return;

                case Phase.EnJeu:
                    Reste -= dt;
                    if (Reste > 0f) return;
                    Reste = 0f;
                    Trancher(scores, porteurCookieSacre);
                    return;

                case Phase.Prolongation:
                    Prolonge += dt;
                    Trancher(scores, porteurCookieSacre);
                    return;
            }
        }

        /// <summary>Le temps est écoulé : un gagnant et c'est fini, ou une égalité et on prolonge.</summary>
        private void Trancher(IReadOnlyList<(int joueur, int cookies)> scores, int porteurCookieSacre)
        {
            int gagnant = EnTete(scores, porteurCookieSacre, out bool egalite);
            if (egalite) { Phase = Phase.Prolongation; return; }
            Gagnant = gagnant;
            Phase = Phase.Finie;
        }

        /// <summary>
        /// Qui est en tête : le porteur du cookie sacré s'il est de la partie, sinon celui qui porte le plus de cookies.
        /// « egalite » est vrai si plusieurs joueurs partagent la tête. Sans joueur : Personne, sans égalité.
        /// </summary>
        public static int EnTete(IReadOnlyList<(int joueur, int cookies)> scores, int porteurCookieSacre, out bool egalite)
        {
            egalite = false;
            int meneur = Personne, meilleur = int.MinValue;
            foreach (var (joueur, cookies) in scores)
            {
                if (joueur == porteurCookieSacre && porteurCookieSacre != Personne) { egalite = false; return joueur; }
                if (cookies > meilleur) { meilleur = cookies; meneur = joueur; egalite = false; }
                else if (cookies == meilleur) egalite = true;
            }
            return meneur;
        }
    }
}
