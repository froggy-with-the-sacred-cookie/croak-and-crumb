using System.Collections.Generic;
using UnityEngine;

namespace Froggy.Objets
{
    /// <summary>
    /// Les règles du gel éclair (P4-09), en C# pur : qui le projectile touche, et combien de temps chacun reste gelé.
    /// Un joueur gelé ne bouge plus pendant « duree » secondes ; un coup reçu brise la glace et le libère aussitôt (choix
    /// de Lucas, 18 novembre 2026 : sinon, à 4, il se fait démolir). Un gel en cours n'est pas rallongé par un second.
    /// </summary>
    public class ReglesGelEclair
    {
        /// <summary>Le rayon d'une Froggy, en m (elle fait 1 m).</summary>
        public const float RayonFroggy = 0.5f;

        private readonly Dictionary<int, float> restes = new Dictionary<int, float>();
        private readonly List<int> cles = new List<int>();

        public bool EstGele(int joueur) => restes.ContainsKey(joueur);
        public float Reste(int joueur) => restes.TryGetValue(joueur, out float r) ? r : 0f;
        public int NombreDeGeles => restes.Count;

        /// <summary>Gèle ce joueur ; rend false s'il l'était déjà (le gel n'est pas rallongé).</summary>
        public bool Geler(int joueur, float duree)
        {
            if (restes.ContainsKey(joueur) || duree <= 0f) return false;
            restes[joueur] = duree;
            return true;
        }

        /// <summary>Un coup brise la glace : rend true si ce joueur était gelé (il est libéré).</summary>
        public bool Briser(int joueur) => restes.Remove(joueur);

        /// <summary>Le temps passe ; « liberes » reçoit les joueurs dont le gel vient de finir.</summary>
        public void Pas(float dt, List<int> liberes)
        {
            liberes?.Clear();
            cles.Clear();
            cles.AddRange(restes.Keys);
            foreach (int j in cles)
            {
                float r = restes[j] - dt;
                if (r > 0f) restes[j] = r;
                else
                {
                    restes.Remove(j);
                    liberes?.Add(j);
                }
            }
        }

        public void Vider() => restes.Clear();

        /// <summary>
        /// Le premier joueur touché par un projectile qui va de « depart » sur « distance » mètres dans « direction » (à
        /// plat), ou -1. Un joueur intouchable est traversé. « parcouru » : à quelle distance du départ il est touché.
        /// </summary>
        public static int Toucher(Vector3 depart, Vector3 direction, float distance, float rayon,
            IList<(int joueur, Vector3 position, bool intouchable)> cibles, out float parcouru)
        {
            parcouru = float.PositiveInfinity;
            int touche = -1;
            direction.y = 0f;
            if (direction.sqrMagnitude < 1e-6f || distance <= 0f) return -1;
            direction.Normalize();
            float portee = rayon + RayonFroggy;
            foreach (var c in cibles)
            {
                if (c.intouchable) continue;
                Vector3 vers = c.position - depart;
                vers.y = 0f;
                float devant = Vector3.Dot(vers, direction);
                if (devant < -portee || devant > distance + portee) continue;
                float aCote = (vers - direction * devant).magnitude;
                if (aCote > portee) continue;
                float a = Mathf.Max(0f, devant - Mathf.Sqrt(Mathf.Max(0f, portee * portee - aCote * aCote)));
                if (a > distance) continue;
                if (a < parcouru) { parcouru = a; touche = c.joueur; }
            }
            return touche;
        }
    }
}
