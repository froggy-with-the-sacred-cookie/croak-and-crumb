using System.Collections.Generic;
using UnityEngine;

namespace Froggy.Regles
{
    /// <summary>
    /// Les règles de la torgnole (P4-10), en C# pur, sans scène ni réseau : c'est ce que l'hôte fera tourner.
    /// Un coup de poing devant soi : il touche le joueur le plus proche dans un cône de « demiAngle » degrés et à moins de
    /// « portee » mètres, puis il faut attendre « recharge » secondes avant le suivant, coup raté compris.
    /// PROPOSITION : le cône, la portée et le fait qu'un coup raté recharge aussi sont des choix de l'IA, à juger en jeu.
    /// </summary>
    public class ReglesTorgnole
    {
        /// <summary>Pas de cible : le coup est parti dans le vide.</summary>
        public const int Personne = 0;

        private readonly float recharge, portee, demiAngle, hauteur;
        private readonly Dictionary<int, float> restes = new Dictionary<int, float>();
        private readonly List<int> cles = new List<int>();

        public ReglesTorgnole(float recharge, float portee, float demiAngle, float hauteur)
        {
            this.recharge = Mathf.Max(0f, recharge);
            this.portee = Mathf.Max(0f, portee);
            this.demiAngle = Mathf.Clamp(demiAngle, 0f, 180f);
            this.hauteur = Mathf.Max(0f, hauteur);
        }

        /// <summary>La recharge entière après un coup, en s (la jauge du HUD, P6-02).</summary>
        public float DureeRecharge => recharge;

        /// <summary>Le temps avant de pouvoir refrapper ; 0 : prête.</summary>
        public float Reste(int joueur) => restes.TryGetValue(joueur, out float r) ? Mathf.Max(0f, r) : 0f;

        public bool Prete(int joueur) => Reste(joueur) <= 0f;

        /// <summary>Un pas : les recharges avancent.</summary>
        public void Pas(float dt)
        {
            cles.Clear();
            cles.AddRange(restes.Keys);
            foreach (int j in cles) restes[j] = Mathf.Max(0f, restes[j] - dt);
        }

        /// <summary>
        /// Le joueur frappe depuis « position », tourné vers « avant ». Rend false s'il recharge encore (aucun coup) ;
        /// sinon le coup part, la recharge commence, et « cible » est le joueur touché ou Personne.
        /// </summary>
        public bool Frapper(int joueur, Vector3 position, Vector3 avant, IList<(int joueur, Vector3 position)> autres, out int cible)
        {
            cible = Personne;
            if (!Prete(joueur)) return false;
            restes[joueur] = recharge;
            cible = Cible(joueur, position, avant, autres);
            return true;
        }

        /// <summary>Qui le coup toucherait, sans le donner : le plus proche dans le cône et à portée, ou Personne.</summary>
        public int Cible(int joueur, Vector3 position, Vector3 avant, IList<(int joueur, Vector3 position)> autres)
        {
            avant.y = 0f;
            int cible = Personne;
            float meilleure = float.MaxValue;
            foreach (var (autre, ou) in autres)
            {
                if (autre == joueur) continue;
                Vector3 ecart = ou - position;
                if (Mathf.Abs(ecart.y) > hauteur) continue;
                ecart.y = 0f;
                float distance = ecart.magnitude;
                if (distance > portee || distance >= meilleure) continue;
                // collé à lui, l'angle ne veut plus rien dire : on le touche
                if (distance > 0.05f && avant.sqrMagnitude > 1e-4f && Vector3.Angle(avant, ecart) > demiAngle) continue;
                meilleure = distance;
                cible = autre;
            }
            return cible;
        }
    }
}
