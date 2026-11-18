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
        /// <summary>
        /// Un cookie ramassé pendant un pas, par quel joueur : soit à un point d'apparition (Point), soit un cookie lâché
        /// au sol (Lache, son numéro). Celui des deux qui ne sert pas vaut -1.
        /// </summary>
        public readonly struct Prise
        {
            public readonly int Point, Joueur, Lache;
            public Prise(int point, int joueur, int lache = -1) { Point = point; Joueur = joueur; Lache = lache; }
        }

        /// <summary>Un cookie lâché, au sol : son numéro, où il est, qui l'a lâché et combien de temps ce joueur doit attendre.</summary>
        private struct AuSol { public int numero, lacheur; public Vector3 position; public float attente; }

        private readonly Vector3[] points;
        private readonly bool[] present;
        private readonly float[] attente;
        private readonly SortedDictionary<int, int> portes = new SortedDictionary<int, int>();
        private readonly List<AuSol> auSol = new List<AuSol>();
        private readonly float rayon, delai, delaiReprise;
        private readonly int max;
        private int prochainNumero;
        private const int Personne = int.MinValue;

        /// <param name="points">Où sont les cookies (leur centre). Un cookie par point au départ.</param>
        /// <param name="rayonRamassage">Distance à laquelle un joueur ramasse, en m.</param>
        /// <param name="delaiReapparition">Temps avant qu'un point redonne un cookie, en s. 0 : jamais.</param>
        /// <param name="maxPortes">Cookies portés au plus par joueur. 0 : pas de limite.</param>
        /// <param name="delaiRepriseLacheur">Temps pendant lequel celui qui a lâché un cookie ne peut pas le reprendre, en s.</param>
        public ReglesCookies(IReadOnlyList<Vector3> points, float rayonRamassage, float delaiReapparition, int maxPortes,
            float delaiRepriseLacheur = 0f)
        {
            delaiReprise = Mathf.Max(0f, delaiRepriseLacheur);
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

        /// <summary>Combien de cookies lâchés attendent au sol.</summary>
        public int NombreAuSol => auSol.Count;

        /// <summary>Le i-ème cookie au sol : son numéro (celui d'une Prise) et sa position.</summary>
        public (int numero, Vector3 position) CookieAuSol(int i) => (auSol[i].numero, auSol[i].position);

        /// <summary>
        /// Fait lâcher un cookie à un joueur (P3-02 : il vient de recevoir une torgnole). Le cookie quitte son score et
        /// attend à « position » le premier qui passe à portée ; celui qui l'a lâché doit d'abord laisser passer le délai
        /// de reprise. Rend le numéro du cookie au sol, ou -1 si le joueur ne portait rien.
        /// </summary>
        public int Lacher(int joueur, Vector3 position)
        {
            int n = Portes(joueur);
            if (n <= 0) return -1;
            portes[joueur] = n - 1;
            int numero = prochainNumero++;
            auSol.Add(new AuSol { numero = numero, lacheur = joueur, position = position, attente = delaiReprise });
            return numero;
        }

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

            for (int i = 0; i < points.Length; i++)
            {
                if (!present[i] || !PlusProche(points[i], joueurs, Personne, out int gagnant)) continue;
                present[i] = false;
                attente[i] = delai;
                portes[gagnant] = Portes(gagnant) + 1;
                prises?.Add(new Prise(i, gagnant));
            }

            // les cookies lâchés, dans l'ordre où ils sont tombés : au premier qui passe à portée
            for (int i = 0; i < auSol.Count; i++)
            {
                AuSol cookie = auSol[i];
                if (cookie.attente > 0f)
                {
                    cookie.attente -= dt;
                    auSol[i] = cookie;
                }
                int exclu = cookie.attente > 0f ? cookie.lacheur : Personne;
                if (!PlusProche(cookie.position, joueurs, exclu, out int gagnant)) continue;
                auSol.RemoveAt(i--);
                portes[gagnant] = Portes(gagnant) + 1;
                prises?.Add(new Prise(-1, gagnant, cookie.numero));
            }
        }

        /// <summary>Le joueur à portée le plus proche du cookie (à égalité, le plus petit numéro), hors « exclu » et hors ceux qui sont pleins.</summary>
        private bool PlusProche(Vector3 cookie, IReadOnlyList<(int joueur, Vector3 position)> joueurs, int exclu, out int gagnant)
        {
            gagnant = 0;
            float rayon2 = rayon * rayon, meilleure = float.MaxValue;
            bool trouve = false;
            foreach (var (joueur, position) in joueurs)
            {
                if (joueur == exclu || (max > 0 && Portes(joueur) >= max)) continue;
                float d2 = (position - cookie).sqrMagnitude;
                if (d2 > rayon2) continue;
                if (trouve && (d2 > meilleure || (d2 == meilleure && joueur >= gagnant))) continue;
                trouve = true;
                gagnant = joueur;
                meilleure = d2;
            }
            return trouve;
        }
    }
}
