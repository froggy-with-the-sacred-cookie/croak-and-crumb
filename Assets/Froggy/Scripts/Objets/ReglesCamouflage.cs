using System.Collections.Generic;
using UnityEngine;

namespace Froggy.Objets
{
    /// <summary>
    /// Les règles du camouflage caméléon (P4-07), en C# pur : qui est camouflé, combien de temps, et à quel point on le
    /// voit. Révélé (onde du croassement, début de la pluie), le camouflage s'arrête pour de bon (choix de Lucas,
    /// 18 novembre 2026) ; pendant la pluie, il ne prend pas. Un second camouflage ne rallonge pas le premier.
    /// </summary>
    public class ReglesCamouflage
    {
        private readonly Dictionary<int, float> restes = new Dictionary<int, float>();
        private readonly List<int> cles = new List<int>();

        public bool EstCamoufle(int joueur) => restes.ContainsKey(joueur);
        public float Reste(int joueur) => restes.TryGetValue(joueur, out float r) ? r : 0f;

        /// <summary>Camoufle ce joueur ; refusé pendant la pluie ou s'il l'est déjà.</summary>
        public bool Camoufler(int joueur, float duree, bool pluie)
        {
            if (pluie || duree <= 0f || restes.ContainsKey(joueur)) return false;
            restes[joueur] = duree;
            return true;
        }

        /// <summary>Révèle ce joueur : rend true s'il était camouflé (le camouflage s'arrête).</summary>
        public bool Reveler(int joueur) => restes.Remove(joueur);

        /// <summary>Le temps passe ; « finis » reçoit les joueurs dont le camouflage vient de s'achever.</summary>
        public void Pas(float dt, List<int> finis)
        {
            finis?.Clear();
            cles.Clear();
            cles.AddRange(restes.Keys);
            foreach (int j in cles)
            {
                float r = restes[j] - dt;
                if (r > 0f) restes[j] = r;
                else
                {
                    restes.Remove(j);
                    finis?.Add(j);
                }
            }
        }

        public void Vider() => restes.Clear();

        /// <summary>
        /// L'opacité d'un camouflé : presque rien immobile, un peu plus en course, avec un chatoiement qui ne bat qu'en
        /// mouvement. « phase » décale le chatoiement d'un joueur à l'autre (jamais en phase). Bornée entre 0 et 1.
        /// </summary>
        public static float Opacite(float vitesse, float temps, float phase, ReglagesCamouflage r)
        {
            float part = Mathf.Clamp01(vitesse / r.vitesseDeCourse);
            float fond = Mathf.Lerp(r.opaciteImmobile, r.opaciteEnCourse, part);
            float bat = 0.5f + 0.5f * Mathf.Sin((temps * r.frequence + phase) * Mathf.PI * 2f);
            return Mathf.Clamp01(fond + r.chatoiement * part * bat);
        }

        /// <summary>Un fondu adouci (lent, rapide, lent) : 0 au début, 1 au bout de « duree ».</summary>
        public static float Fondu(float ecoule, float duree) => duree <= 0f ? 1f : Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(ecoule / duree));
    }
}
