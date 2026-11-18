using System;
using Froggy.Socle;
using UnityEngine;

namespace Froggy.Regles
{
    /// <summary>
    /// Les cookies qu'une Froggy porte (P3-01) : son score. PartieCookies le pose sur chaque Froggy et le tient à jour ;
    /// le HUD, le son ou un effet lisent « Nombre » ou écoutent « Change ». Le ralentissement du porteur passe par les
    /// Modificateurs du socle (sans effet tant que le réglage vaut 0).
    /// </summary>
    [DisallowMultipleComponent]
    public class PorteurCookies : MonoBehaviour
    {
        /// <summary>Le numéro du joueur dans la partie (donné par PartieCookies ; en réseau, par l'hôte).</summary>
        public int Joueur { get; private set; }

        /// <summary>Le nombre de cookies portés : le score.</summary>
        public int Nombre { get; private set; }

        /// <summary>
        /// Vrai quand on ne peut pas frapper ce joueur : tombé hors de l'arène ou tout juste réapparu (ApparitionJoueurs,
        /// P3-04). FaireLacher le refuse ; un coup (P4-10) le lit aussi.
        /// </summary>
        public bool Intouchable { get; set; }

        /// <summary>Appelé quand le nombre de cookies portés change.</summary>
        public event Action Change;

        public void Numeroter(int joueur) => Joueur = joueur;

        /// <summary>Fixe le nombre de cookies portés et le facteur de vitesse qui va avec (1 : aucun).</summary>
        public void Fixer(int nombre, float facteurVitesse)
        {
            var modificateurs = GetComponent<Modificateurs>();
            if (modificateurs != null)
            {
                if (Mathf.Approximately(facteurVitesse, 1f)) modificateurs.Retirer(this);
                else modificateurs.Poser(this, vitesse: facteurVitesse);
            }
            if (nombre == Nombre) return;
            Nombre = nombre;
            Change?.Invoke();
        }
    }
}
