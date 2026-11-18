using System.Collections.Generic;
using System.Text;
using Froggy.Reseau;

namespace Froggy.Interface
{
    /// <summary>État d'une place de la salle d'attente.</summary>
    public enum EtatPlace
    {
        Libre,
        Hote,
        Pret,
        EnAttente
    }

    /// <summary>Une place de la salle d'attente, prête à afficher.</summary>
    public readonly struct PlaceSalle
    {
        public readonly int Numero;
        public readonly EtatPlace Etat;
        /// <summary>Indice dans la palette du salon (Salon.Couleur) ; -1 pour une place libre.</summary>
        public readonly int Couleur;
        public readonly bool Vous;

        public PlaceSalle(int numero, EtatPlace etat, int couleur, bool vous)
        {
            Numero = numero;
            Etat = etat;
            Couleur = couleur;
            Vous = vous;
        }

        public string Nom => Etat == EtatPlace.Libre ? "Place libre" : $"Joueur {Numero}" + (Vous ? " (vous)" : "");

        public string TexteEtat => Etat switch
        {
            EtatPlace.Hote => "Hôte",
            EtatPlace.Pret => "Prêt",
            EtatPlace.EnAttente => "En attente",
            _ => ""
        };
    }

    /// <summary>
    /// Les textes de la salle d'attente (P6-03), en C# pur : une place par numéro de joueur (1 à 4), le bouton principal
    /// et l'aide en dessous. Lit ce que le salon (P2-03) publie, ne décide rien : l'hôte lance quand Salon.PeutLancer.
    /// Pas encore de pseudos dans le salon : « Joueur 2 ». Testé dans MenusTests.
    /// </summary>
    public static class ContenuSalle
    {
        /// <summary>Les 4 places dans l'ordre des numéros ; une place sans joueur est « libre ».</summary>
        public static PlaceSalle[] Places(IReadOnlyList<MembreSalon> membres, int? monClientId)
        {
            var places = new PlaceSalle[EtatSalon.JoueursMax];
            for (int numero = 1; numero <= places.Length; numero++)
                places[numero - 1] = new PlaceSalle(numero, EtatPlace.Libre, -1, false);
            if (membres == null) return places;

            foreach (MembreSalon m in membres)
            {
                if (m.Numero < 1 || m.Numero > places.Length) continue;
                EtatPlace etat = m.Hote ? EtatPlace.Hote : m.Pret ? EtatPlace.Pret : EtatPlace.EnAttente;
                places[m.Numero - 1] = new PlaceSalle(m.Numero, etat, m.Couleur, monClientId == m.ClientId);
            }
            return places;
        }

        /// <summary>« 3/4 joueurs ».</summary>
        public static string Compte(int joueurs) => $"{joueurs}/{EtatSalon.JoueursMax} joueur" + (joueurs > 1 ? "s" : "");

        /// <summary>Le bouton principal : l'hôte lance, un invité se dit prêt (ou ne l'est plus).</summary>
        public static string BoutonPrincipal(bool hote, bool pret, PhaseSalon phase)
        {
            if (phase == PhaseSalon.EnPartie) return "Partie en cours…";
            if (hote) return "Lancer la partie";
            return pret ? "Je ne suis plus prêt" : "Se mettre prêt";
        }

        /// <summary>La phrase sous les joueurs : ce qu'on attend, pour l'hôte comme pour un invité.</summary>
        public static string Aide(IReadOnlyList<MembreSalon> membres, bool hote, bool pret, PhaseSalon phase)
        {
            if (phase == PhaseSalon.EnPartie) return "La partie se charge.";
            int nombre = membres?.Count ?? 0;
            if (nombre < EtatSalon.JoueursMin)
                return hote ? "Il faut au moins 2 joueurs : donnez le code à vos amis." : "En attente d'autres joueurs.";

            if (!hote)
                return pret ? "L'hôte lance la partie quand tout le monde est prêt." : "Mettez-vous prêt pour que l'hôte puisse lancer.";

            var attendus = new List<int>();
            foreach (MembreSalon m in membres)
                if (!m.Hote && !m.Pret) attendus.Add(m.Numero);
            if (attendus.Count == 0) return "Tout le monde est prêt !";

            attendus.Sort();
            var texte = new StringBuilder(attendus.Count == 1 ? "En attente du joueur " : "En attente des joueurs ");
            for (int i = 0; i < attendus.Count; i++)
            {
                if (i > 0) texte.Append(i == attendus.Count - 1 ? " et " : ", ");
                texte.Append(attendus[i]);
            }
            return texte.Append('.').ToString();
        }
    }
}
