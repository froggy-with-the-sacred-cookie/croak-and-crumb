using System.Collections.Generic;
using UnityEngine;

namespace Froggy.Regles
{
    /// <summary>
    /// Les règles des cookies (P3-01), sans réseau ni scène : des points donnent des cookies, un joueur assez proche
    /// les ramasse et les garde sur lui, son score est le nombre de cookies qu'il porte (pas de nid en V1).
    /// Du C# pur, pour se tester seul et pour que l'hôte le fasse tourner tel quel en réseau (P2-04) : à entrées égales
    /// (positions des joueurs, pas de temps), le résultat est le même partout. Un client n'annonce jamais « j'ai pris le
    /// cookie » : c'est Pas() qui le décide.
    /// </summary>
    public class ReglesCookies
    {
        /// <summary>Un cookie ramassé pendant un pas : à quel point, par quel joueur.</summary>
        public readonly struct Prise
        {
            public readonly int Point, Joueur;
            public Prise(int point, int joueur) { Point = point; Joueur = joueur; }
        }

        private readonly Vector3[] points;
        private readonly bool[] present;
        private readonly float[] attente;
        private readonly SortedDictionary<int, int> portes = new SortedDictionary<int, int>();
        private readonly float rayon, delai;
        private readonly int max;

        /// <param name="points">Où sont les cookies (leur centre). Un cookie par point au départ.</param>
        /// <param name="rayonRamassage">Distance à laquelle un joueur ramasse, en m.</param>
        /// <param name="delaiReapparition">Temps avant qu'un point redonne un cookie, en s. 0 : jamais.</param>
        /// <param name="maxPortes">Cookies portés au plus par joueur. 0 : pas de limite.</param>
        public ReglesCookies(IReadOnlyList<Vector3> points, float rayonRamassage, float delaiReapparition, int maxPortes)
        {
            this.points = new Vector3[points.Count];
            present = new bool[points.Count];
            attente = new float[points.Count];
            for (int i = 0; i < points.Count; i++) { this.points[i] = points[i]; present[i] = true; }
            rayon = Mathf.Max(0f, rayonRamassage);
            delai = Mathf.Max(0f, delaiReapparition);
            max = Mathf.Max(0, maxPortes);
        }

        public int NombrePoints => points.Length;
        public Vector3 Point(int i) => points[i];

        /// <summary>Y a-t-il un cookie à ce point en ce moment ?</summary>
        public bool Present(int point) => present[point];

        /// <summary>Le score d'un joueur : le nombre de cookies qu'il porte.</summary>
        public int Portes(int joueur) => portes.TryGetValue(joueur, out int n) ? n : 0;

        /// <summary>
        /// Un pas de la partie : les cookies attendus réapparaissent, puis chaque cookie présent va au joueur le plus
        /// proche à portée (à égalité, au plus petit numéro). Les cookies pris s'ajoutent à « prises ».
        /// </summary>
        public void Pas(float dt, IReadOnlyList<(int joueur, Vector3 position)> joueurs, List<Prise> prises)
        {
            if (delai > 0f)
                for (int i = 0; i < points.Length; i++)
                {
                    if (present[i]) continue;
                    attente[i] -= dt;
                    if (attente[i] <= 0f) present[i] = true;
                }

            float rayon2 = rayon * rayon;
            for (int i = 0; i < points.Length; i++)
            {
                if (!present[i]) continue;
                int gagnant = 0;
                float meilleure = float.MaxValue;
                bool trouve = false;
                foreach (var (joueur, position) in joueurs)
                {
                    if (max > 0 && Portes(joueur) >= max) continue;
                    float d2 = (position - points[i]).sqrMagnitude;
                    if (d2 > rayon2) continue;
                    if (trouve && (d2 > meilleure || (d2 == meilleure && joueur >= gagnant))) continue;
                    trouve = true;
                    gagnant = joueur;
                    meilleure = d2;
                }
                if (!trouve) continue;
                present[i] = false;
                attente[i] = delai;
                portes[gagnant] = Portes(gagnant) + 1;
                prises?.Add(new Prise(i, gagnant));
            }
        }
    }
}
