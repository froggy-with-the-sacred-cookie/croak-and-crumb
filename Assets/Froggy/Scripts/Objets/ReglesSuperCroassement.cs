using System.Collections.Generic;
using UnityEngine;

namespace Froggy.Objets
{
    /// <summary>
    /// Les règles du super croassement (P4-02), en C# pur, sans scène ni réseau : c'est ce que l'hôte fera tourner.
    /// L'onde part de la Froggy qui croasse et repousse, vers l'extérieur, chaque joueur au sol à moins de « rayon » mètres
    /// (à plat). Un joueur en l'air (il a sauté au bon moment) n'est pas touché : il esquive.
    /// PROPOSITION : la même poussée quelle que soit la distance (plus lisible), choix de l'IA à juger en jeu.
    /// </summary>
    public class ReglesSuperCroassement
    {
        /// <summary>Un joueur repoussé, et vers où (à plat, normé).</summary>
        public readonly struct Pousse
        {
            public readonly int Joueur;
            public readonly Vector3 Direction;
            public Pousse(int joueur, Vector3 direction) { Joueur = joueur; Direction = direction; }
        }

        private readonly float rayon;

        public ReglesSuperCroassement(float rayon) => this.rayon = Mathf.Max(0f, rayon);

        /// <summary>
        /// Les joueurs que l'onde partie de « centre » repousse. « autres » : chaque joueur, où il est, et s'il touche le sol.
        /// Celui qui croasse (« joueur ») n'est jamais repoussé.
        /// </summary>
        public void Cibles(int joueur, Vector3 centre, Vector3 avant, IList<(int joueur, Vector3 position, bool auSol)> autres, List<Pousse> sortie)
        {
            foreach (var (autre, position, auSol) in autres)
            {
                if (autre == joueur || !auSol) continue;
                Vector3 ecart = position - centre;
                ecart.y = 0f;
                if (ecart.magnitude > rayon) continue;
                // collé à lui : poussé droit devant celui qui croasse
                Vector3 direction = ecart.sqrMagnitude > 1e-4f ? ecart.normalized : new Vector3(avant.x, 0f, avant.z).normalized;
                if (direction.sqrMagnitude < 0.5f) direction = Vector3.forward;
                sortie.Add(new Pousse(autre, direction));
            }
        }
    }
}
