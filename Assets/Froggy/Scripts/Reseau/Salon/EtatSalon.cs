using System.Collections.Generic;

namespace Froggy.Reseau
{
    /// <summary>Où en est le salon.</summary>
    public enum PhaseSalon : byte
    {
        /// <summary>On attend, on se dit prêt, on choisit sa couleur.</summary>
        Salon,
        /// <summary>La partie est lancée ; personne ne peut arriver.</summary>
        EnPartie,
    }

    /// <summary>Pourquoi l'hôte refuse quelqu'un.</summary>
    public enum RefusSalon : byte
    {
        Aucun,
        CodeInconnu,
        SalonPlein,
        PartieEnCours,
    }

    /// <summary>Un joueur du salon, tel que tout le monde le voit.</summary>
    public struct MembreSalon
    {
        /// <summary>Identifiant FishNet de la connexion (NetworkConnection.ClientId).</summary>
        public int ClientId;
        /// <summary>Joueur 1 à 4, gardé toute la session ; le plus petit numéro libre à l'arrivée.</summary>
        public int Numero;
        /// <summary>Indice dans la palette du salon ; deux joueurs n'ont jamais la même.</summary>
        public int Couleur;
        public bool Pret;
        public bool Hote;
    }

    /// <summary>
    /// Les règles du salon (P2-03), sans réseau : qui est là, qui est prêt, peut-on lancer. Tenu par l'hôte seul ;
    /// les autres en reçoivent une copie (SalonReseau). Testé dans SalonTests.
    /// </summary>
    public class EtatSalon
    {
        /// <summary>PROVISOIRE : Froggy se joue de 2 à 4 (Notion).</summary>
        public const int JoueursMax = 4;
        public const int JoueursMin = 2;

        private readonly List<MembreSalon> membres = new();

        public EtatSalon(string code, int nombreCouleurs)
        {
            Code = code;
            NombreCouleurs = nombreCouleurs < JoueursMax ? JoueursMax : nombreCouleurs;
        }

        public string Code { get; }
        public int NombreCouleurs { get; }
        public PhaseSalon Phase { get; private set; } = PhaseSalon.Salon;
        /// <summary>Rangés par numéro.</summary>
        public IReadOnlyList<MembreSalon> Membres => membres;
        public bool EstPlein => membres.Count >= JoueursMax;

        /// <summary>
        /// Lancement permis : au moins 2 joueurs, dans le salon, et tous les invités prêts (l'hôte lance, il n'a pas à
        /// se dire prêt). PROVISOIRE, à valider en PR.
        /// </summary>
        public bool PeutLancer => PeutLancerAvec(membres, Phase);

        /// <summary>La même règle, pour une copie reçue du réseau.</summary>
        public static bool PeutLancerAvec(IReadOnlyList<MembreSalon> liste, PhaseSalon phase)
        {
            if (phase != PhaseSalon.Salon || liste == null || liste.Count < JoueursMin) return false;
            foreach (MembreSalon m in liste)
                if (!m.Hote && !m.Pret) return false;
            return true;
        }

        /// <summary>Ce que l'hôte répond à quelqu'un qui frappe avec ce code (déjà normalisé).</summary>
        public RefusSalon Examiner(string codeDonne)
        {
            if (codeDonne != Code) return RefusSalon.CodeInconnu;
            if (Phase != PhaseSalon.Salon) return RefusSalon.PartieEnCours;
            if (EstPlein) return RefusSalon.SalonPlein;
            return RefusSalon.Aucun;
        }

        /// <summary>Fait entrer un joueur : plus petit numéro et première couleur libres. Faux si refusé ou déjà là.</summary>
        public bool Accueillir(int clientId, bool hote)
        {
            if (Phase != PhaseSalon.Salon || EstPlein || Trouver(clientId) >= 0) return false;

            var membre = new MembreSalon
            {
                ClientId = clientId,
                Numero = PremierNumeroLibre(),
                Couleur = PremiereCouleurLibre(),
                Hote = hote,
            };
            int place = 0;
            while (place < membres.Count && membres[place].Numero < membre.Numero) place++;
            membres.Insert(place, membre);
            return true;
        }

        /// <summary>Un joueur part : son numéro et sa couleur se libèrent. Faux s'il n'était pas là.</summary>
        public bool Retirer(int clientId)
        {
            int i = Trouver(clientId);
            if (i < 0) return false;
            membres.RemoveAt(i);
            return true;
        }

        public bool SeDirePret(int clientId, bool pret)
        {
            int i = Trouver(clientId);
            if (i < 0 || Phase != PhaseSalon.Salon) return false;
            MembreSalon m = membres[i];
            m.Pret = pret;
            membres[i] = m;
            return true;
        }

        /// <summary>Change de couleur ; refusé si un autre l'a déjà ou si l'indice sort de la palette.</summary>
        public bool ChoisirCouleur(int clientId, int couleur)
        {
            int i = Trouver(clientId);
            if (i < 0 || Phase != PhaseSalon.Salon || couleur < 0 || couleur >= NombreCouleurs) return false;
            for (int j = 0; j < membres.Count; j++)
                if (j != i && membres[j].Couleur == couleur) return false;
            MembreSalon m = membres[i];
            m.Couleur = couleur;
            membres[i] = m;
            return true;
        }

        /// <summary>La prochaine couleur libre après la sienne (bouton « couleur suivante »). -1 si aucune.</summary>
        public int CouleurSuivante(int clientId)
        {
            int i = Trouver(clientId);
            if (i < 0) return -1;
            for (int pas = 1; pas < NombreCouleurs; pas++)
            {
                int c = (membres[i].Couleur + pas) % NombreCouleurs;
                if (!CouleurPrise(c)) return c;
            }
            return -1;
        }

        public bool Lancer()
        {
            if (!PeutLancer) return false;
            Phase = PhaseSalon.EnPartie;
            return true;
        }

        /// <summary>Après une partie : retour au salon, tout le monde redevient « pas prêt ».</summary>
        public void RevenirAuSalon()
        {
            Phase = PhaseSalon.Salon;
            for (int i = 0; i < membres.Count; i++)
            {
                MembreSalon m = membres[i];
                m.Pret = false;
                membres[i] = m;
            }
        }

        public int Trouver(int clientId)
        {
            for (int i = 0; i < membres.Count; i++)
                if (membres[i].ClientId == clientId) return i;
            return -1;
        }

        private int PremierNumeroLibre()
        {
            for (int n = 1; ; n++)
            {
                bool pris = false;
                foreach (MembreSalon m in membres) pris |= m.Numero == n;
                if (!pris) return n;
            }
        }

        private int PremiereCouleurLibre()
        {
            for (int c = 0; c < NombreCouleurs; c++)
                if (!CouleurPrise(c)) return c;
            return 0;
        }

        private bool CouleurPrise(int couleur)
        {
            foreach (MembreSalon m in membres)
                if (m.Couleur == couleur) return true;
            return false;
        }
    }
}
