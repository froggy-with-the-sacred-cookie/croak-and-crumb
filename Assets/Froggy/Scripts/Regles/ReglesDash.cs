using System.Collections.Generic;
using UnityEngine;

namespace Froggy.Regles
{
    /// <summary>Ce qui arrive quand une Froggy en dash en touche une autre.</summary>
    public enum ChocDash
    {
        /// <summary>Rien : la cible est intouchable, ou c'est la même Froggy.</summary>
        Rien,
        /// <summary>La cible est bousculée, le dash de celui qui la percute s'arrête.</summary>
        Bouscule,
        /// <summary>Les deux étaient en dash : toutes les deux assommées.</summary>
        DoubleAssomme,
    }

    /// <summary>
    /// Les règles du dash (#134), en C# pur, sans scène ni réseau : c'est ce que l'hôte fera tourner (avec la #128).
    /// Le dash remplace le sprint (décision de Florent, 18 novembre 2026) : une recharge par joueur, refusé pendant un
    /// assommement ; une Froggy en dash bouscule celle qu'elle percute ; deux Froggy qui se dashent dessus en même
    /// temps sont assommées toutes les deux. Les durées viennent de ReglagesDash.
    /// </summary>
    public class ReglesDash
    {
        private readonly float recharge, dureeAssomme;
        private readonly Dictionary<int, float> restes = new Dictionary<int, float>();
        private readonly Dictionary<int, float> assommes = new Dictionary<int, float>();
        private readonly List<int> cles = new List<int>();

        public ReglesDash(float recharge, float dureeAssomme)
        {
            this.recharge = Mathf.Max(0f, recharge);
            this.dureeAssomme = Mathf.Max(0f, dureeAssomme);
        }

        /// <summary>La recharge entière après un dash, en s (la jauge du HUD).</summary>
        public float DureeRecharge => recharge;

        /// <summary>Le temps avant le prochain dash ; 0 : prêt.</summary>
        public float Reste(int joueur) => restes.TryGetValue(joueur, out float r) ? Mathf.Max(0f, r) : 0f;

        /// <summary>Le temps d'assommement qui reste ; 0 : debout.</summary>
        public float ResteAssomme(int joueur) => assommes.TryGetValue(joueur, out float r) ? Mathf.Max(0f, r) : 0f;

        public bool Assomme(int joueur) => ResteAssomme(joueur) > 0f;

        /// <summary>Prêt : recharge finie et pas assommé.</summary>
        public bool Pret(int joueur) => Reste(joueur) <= 0f && !Assomme(joueur);

        /// <summary>Un pas : recharges et assommements avancent.</summary>
        public void Pas(float dt)
        {
            Avancer(restes, dt);
            Avancer(assommes, dt);
        }

        private void Avancer(Dictionary<int, float> temps, float dt)
        {
            cles.Clear();
            cles.AddRange(temps.Keys);
            foreach (int j in cles) temps[j] = Mathf.Max(0f, temps[j] - dt);
        }

        /// <summary>
        /// Le joueur demande un dash. Faux s'il recharge ou s'il est assommé (rien ne part) ; vrai : la recharge commence.
        /// Le contrôleur peut encore le refuser (en l'air, dans l'eau) : appeler alors Rendre.
        /// </summary>
        public bool Dasher(int joueur)
        {
            if (!Pret(joueur)) return false;
            restes[joueur] = recharge;
            return true;
        }

        /// <summary>Le dash accordé n'est pas parti (le contrôleur l'a refusé) : la recharge est rendue.</summary>
        public void Rendre(int joueur) => restes.Remove(joueur);

        /// <summary>Ce que donne le contact entre une Froggy en dash et une autre.</summary>
        public static ChocDash Choc(bool autreEnDash, bool autreIntouchable)
        {
            if (autreIntouchable) return ChocDash.Rien;
            return autreEnDash ? ChocDash.DoubleAssomme : ChocDash.Bouscule;
        }

        /// <summary>Assomme un joueur pour la durée réglée (un assommement en cours repart de zéro, il ne s'ajoute pas).</summary>
        public void Assommer(int joueur) => assommes[joueur] = dureeAssomme;

        /// <summary>Tout le monde debout et prêt (« Rejouer »).</summary>
        public void Vider()
        {
            restes.Clear();
            assommes.Clear();
        }
    }
}
