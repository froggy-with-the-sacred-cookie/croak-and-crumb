using System;
using UnityEngine;

namespace Froggy.Socle
{
    /// <summary>Ce qui peut arriver dans une partie et que les autres postes veulent savoir (liste de T-01).</summary>
    public enum TypeEvenement
    {
        CookieRamasse,
        CookieLache,
        ObjetUtilise,
        /// <summary>HYPOTHÈSE : un seul type pour le cookie sacré, T-01 ne dit pas s'il faut séparer apparition et capture.</summary>
        CookieSacre,
        /// <summary>Le compte à rebours est fini, on joue (P3-03).</summary>
        PartieCommencee,
        /// <summary>La partie est finie ; « Auteur » est le gagnant, s'il y en a un (P3-03).</summary>
        PartieFinie,
        /// <summary>Un joueur tombé hors de l'arène vient de réapparaître ; « Auteur » est ce joueur (P3-04).</summary>
        JoueurReapparu,
        /// <summary>Un joueur a donné une torgnole ; « Auteur » le frappeur, « Objet » le joueur touché, vide si raté (P4-10).</summary>
        CoupDonne,
        /// <summary>
        /// Un joueur a quitté la partie en ligne (P2-06) ; « Auteur » est ce joueur (sa Froggy, encore là à cet instant).
        /// Ses cookies et le cookie sacré viennent de tomber ; la partie continue sans lui.
        /// </summary>
        JoueurParti,
    }

    /// <summary>Un événement de partie : quoi, par qui, avec quoi, où. « Auteur » et « Objet » peuvent être vides.</summary>
    public readonly struct EvenementPartie
    {
        public readonly TypeEvenement Type;
        public readonly GameObject Auteur;
        public readonly GameObject Objet;
        public readonly Vector3 Position;

        public EvenementPartie(TypeEvenement type, GameObject auteur, GameObject objet, Vector3 position)
        {
            Type = type;
            Auteur = auteur;
            Objet = objet;
            Position = position;
        }
    }

    /// <summary>
    /// Les événements de partie (socle commun, T-01) : un poste émet, les autres écoutent, sans se connaître.
    ///
    /// Écouter (son, HUD, effet, score…) :
    ///   void OnEnable()  => EvenementsPartie.Emis += Reagir;
    ///   void OnDisable() => EvenementsPartie.Emis -= Reagir;     // toujours se désabonner
    ///   void Reagir(EvenementPartie e) { if (e.Type == TypeEvenement.CookieRamasse) … }
    /// Émettre :
    ///   EvenementsPartie.Emettre(TypeEvenement.CookieRamasse, auteur: froggy, objet: cookie);
    ///
    /// Local pour l'instant : en réseau, c'est l'autorité qui émettra et les clients qui recevront (à brancher avec
    /// FishNet, P2-04). Un écouteur qui plante n'empêche pas les autres de recevoir l'événement.
    /// </summary>
    public static class EvenementsPartie
    {
        public static event Action<EvenementPartie> Emis;

        public static void Emettre(TypeEvenement type, GameObject auteur = null, GameObject objet = null, Vector3? position = null)
        {
            Vector3 ou = position ?? (objet != null ? objet.transform.position
                                     : auteur != null ? auteur.transform.position : Vector3.zero);
            Emettre(new EvenementPartie(type, auteur, objet, ou));
        }

        public static void Emettre(EvenementPartie evenement)
        {
            var ecouteurs = Emis;
            if (ecouteurs == null) return;
            foreach (Action<EvenementPartie> ecouteur in ecouteurs.GetInvocationList())
            {
                try { ecouteur(evenement); }
                catch (Exception e) { Debug.LogException(e); }
            }
        }

        // sans rechargement du domaine à l'entrée en mode Play, les abonnés de la partie d'avant resteraient
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Vider() => Emis = null;
    }
}
