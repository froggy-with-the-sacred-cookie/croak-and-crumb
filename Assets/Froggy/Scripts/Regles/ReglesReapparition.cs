using System.Collections.Generic;
using UnityEngine;

namespace Froggy.Regles
{
    /// <summary>Où en est un joueur pour la réapparition (P3-04).</summary>
    public enum EtatReapparition
    {
        /// <summary>Il joue normalement.</summary>
        EnJeu,
        /// <summary>Il est tombé hors de l'arène et attend de réapparaître.</summary>
        Tombe,
        /// <summary>Il vient de réapparaître : on ne peut pas le frapper.</summary>
        Intouchable,
    }

    /// <summary>
    /// Les règles de la réapparition (P3-04), en C# pur, sans scène ni réseau : c'est ce que l'hôte fera tourner.
    /// Un joueur qui passe sous « hauteurChute » est tombé hors de l'arène ; après « delai », il réapparaît au point
    /// d'apparition le plus loin des autres joueurs, puis reste intouchable pendant « dureeIntouchable ».
    /// PROPOSITION : le point le plus loin des autres (pour ne pas réapparaître sous les coups) et les durées sont des choix
    /// de l'IA, à juger en jeu par l'équipe. Les cookies portés sont gardés pendant la chute (Notion ne dit rien).
    /// </summary>
    public class ReglesReapparition
    {
        /// <summary>Une réapparition qui vient d'avoir lieu : qui, et où.</summary>
        public readonly struct Reapparu
        {
            public readonly int Joueur;
            public readonly Vector3 Point;
            public Reapparu(int joueur, Vector3 point) { Joueur = joueur; Point = point; }
        }

        private struct Suivi { public EtatReapparition etat; public float reste; }

        private readonly List<(int joueur, Vector3 point)> points;
        private readonly float delai, dureeIntouchable;
        private readonly Dictionary<int, Suivi> suivis = new Dictionary<int, Suivi>();

        /// <summary>Sous cette hauteur, un joueur est tombé hors de l'arène.</summary>
        public float HauteurChute { get; }

        /// <param name="points">Les points d'apparition, chacun avec le joueur qui y commence la partie.</param>
        public ReglesReapparition(IEnumerable<(int joueur, Vector3 point)> points, float hauteurChute, float delai, float dureeIntouchable)
        {
            this.points = new List<(int, Vector3)>(points);
            HauteurChute = hauteurChute;
            this.delai = Mathf.Max(0f, delai);
            this.dureeIntouchable = Mathf.Max(0f, dureeIntouchable);
        }

        public int NombrePoints => points.Count;

        /// <summary>Où le joueur commence la partie : son point à lui, sinon un point pris dans l'ordre. Faux s'il n'y en a aucun.</summary>
        public bool Depart(int joueur, out Vector3 point)
        {
            point = default;
            if (points.Count == 0) return false;
            foreach (var p in points)
                if (p.joueur == joueur) { point = p.point; return true; }
            point = points[((joueur - 1) % points.Count + points.Count) % points.Count].point;
            return true;
        }

        public EtatReapparition Etat(int joueur) => suivis.TryGetValue(joueur, out Suivi s) ? s.etat : EtatReapparition.EnJeu;

        /// <summary>Le joueur peut-il être frappé ? Non pendant la chute ni juste après la réapparition.</summary>
        public bool Intouchable(int joueur) => Etat(joueur) != EtatReapparition.EnJeu;

        /// <summary>Le temps qui reste avant de réapparaître (tombé) ou d'être touchable (intouchable) ; 0 en jeu.</summary>
        public float Reste(int joueur) => suivis.TryGetValue(joueur, out Suivi s) ? Mathf.Max(0f, s.reste) : 0f;

        /// <summary>Un pas des règles. « positions » : chaque joueur et où il est ; les réapparitions du pas vont dans « reapparus ».</summary>
        public void Pas(float dt, IList<(int joueur, Vector3 position)> positions, List<Reapparu> reapparus)
        {
            foreach (var (joueur, position) in positions)
            {
                suivis.TryGetValue(joueur, out Suivi s);
                switch (s.etat)
                {
                    case EtatReapparition.Tombe:
                        s.reste -= dt;
                        if (s.reste <= 0f && PlusLoin(joueur, positions, out Vector3 point))
                        {
                            s.etat = dureeIntouchable > 0f ? EtatReapparition.Intouchable : EtatReapparition.EnJeu;
                            s.reste = dureeIntouchable;
                            reapparus.Add(new Reapparu(joueur, point));
                        }
                        break;
                    default:
                        if (position.y < HauteurChute)
                        {
                            s.etat = EtatReapparition.Tombe;
                            s.reste = delai;
                            break;
                        }
                        if (s.etat == EtatReapparition.Intouchable)
                        {
                            s.reste -= dt;
                            if (s.reste <= 0f) { s.etat = EtatReapparition.EnJeu; s.reste = 0f; }
                        }
                        break;
                }
                suivis[joueur] = s;
            }
        }

        /// <summary>
        /// Le point d'apparition le plus loin du joueur en jeu le plus proche (distance à plat). Sans autre joueur en jeu :
        /// le point de départ du joueur. Faux s'il n'y a aucun point.
        /// </summary>
        public bool PlusLoin(int joueur, IList<(int joueur, Vector3 position)> positions, out Vector3 point)
        {
            if (!Depart(joueur, out point)) return false;
            float meilleur = -1f;
            bool autres = false;
            foreach (var p in points)
            {
                float plusProche = float.MaxValue;
                foreach (var (autre, position) in positions)
                {
                    if (autre == joueur || Etat(autre) == EtatReapparition.Tombe) continue;
                    autres = true;
                    Vector3 ecart = position - p.point;
                    ecart.y = 0f;
                    plusProche = Mathf.Min(plusProche, ecart.sqrMagnitude);
                }
                if (!autres) return true;
                if (plusProche > meilleur + 1e-4f) { meilleur = plusProche; point = p.point; }
            }
            return true;
        }
    }
}
