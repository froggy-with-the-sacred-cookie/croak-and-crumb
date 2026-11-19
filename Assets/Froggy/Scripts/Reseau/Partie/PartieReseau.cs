using System.Collections.Generic;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using Froggy.Arene;
using Froggy.Regles;
using Froggy.Socle;
using UnityEngine;

namespace Froggy.Reseau
{
    /// <summary>Score d'un joueur, recopié chez tous.</summary>
    public struct ScoreReseau
    {
        public int Joueur;
        public int Cookies;
    }

    /// <summary>Un cookie lâché, au sol, recopié chez tous.</summary>
    public struct CookieAuSolReseau
    {
        public int Numero;
        public Vector3 Position;
    }

    /// <summary>
    /// Les règles de partie en réseau (P2-04) : <b>seul l'hôte décide</b>. Il fait tourner les règles du poste 3
    /// (PartieCookies, DeroulementPartie) ; chacun reçoit les scores, les cookies présents, les cookies au sol, la phase,
    /// le temps, le gagnant et le porteur du cookie sacré (canal fiable : rien ne se perd), et ne fait que les montrer
    /// (mode Miroir). Les événements de partie (cookie ramassé, lâché, début, fin) sont renvoyés à chacun, qui les
    /// ré-émet chez lui : sons et effets des autres postes marchent pareil partout.
    ///
    /// Apparu par le salon quand tous les joueurs ont leur personnage (Salon.prefabPartie) ; retiré au retour au salon.
    /// Pour les postes suivants, côté hôte : FaireLacher (torgnole, P4-10) et PorteurCookieSacre (P5-08).
    /// </summary>
    [RequireComponent(typeof(PartieCookies), typeof(DeroulementPartie))]
    public class PartieReseau : NetworkBehaviour
    {
        private readonly SyncList<ScoreReseau> scores = new();
        private readonly SyncList<bool> presents = new();
        private readonly SyncList<CookieAuSolReseau> auSol = new();
        private readonly SyncVar<Phase> phase = new(Phase.Attente);
        private readonly SyncVar<float> reste = new(0f);
        private readonly SyncVar<float> prolonge = new(0f);
        private readonly SyncVar<int> gagnant = new(ReglesPartie.Personne);
        private readonly SyncVar<int> porteurSacre = new(ReglesPartie.Personne);

        private PartieCookies cookies;
        private DeroulementPartie deroulement;
        private bool aRefleter;

        // copies réutilisées (pas d'allocation à chaque image)
        private readonly List<bool> lusPresents = new();
        private readonly List<(int, Vector3)> lusAuSol = new();
        private readonly List<(int, int)> lusScores = new();

        /// <summary>Celle de la partie en cours, ou null.</summary>
        public static PartieReseau Actuelle { get; private set; }

        public PartieCookies Cookies => cookies;
        public DeroulementPartie Deroulement => deroulement;

        private void Awake()
        {
            cookies = GetComponent<PartieCookies>();
            deroulement = GetComponent<DeroulementPartie>();
            deroulement.RejouerPermis = false;   // en réseau, c'est le salon qui relance une partie
            scores.OnChange += (_, _, _, _, _) => aRefleter = true;
            presents.OnChange += (_, _, _, _, _) => aRefleter = true;
            auSol.OnChange += (_, _, _, _, _) => aRefleter = true;
            phase.OnChange += (_, _, _) => aRefleter = true;
            reste.OnChange += (_, _, _) => aRefleter = true;
            gagnant.OnChange += (_, _, _) => aRefleter = true;
        }

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            Actuelle = this;
        }

        public override void OnStopNetwork()
        {
            base.OnStopNetwork();
            if (Actuelle == this) Actuelle = null;
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            EvenementsPartie.Emis += Relayer;
        }

        public override void OnStopServer()
        {
            base.OnStopServer();
            EvenementsPartie.Emis -= Relayer;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            if (IsServerStarted) return;   // le client de l'hôte voit déjà les vraies règles
            cookies.Miroir = true;
            deroulement.Miroir = true;
            cookies.DemarrerNumerotes(Reperes(), Joueurs());
            aRefleter = true;
        }

        // ---------------------------------------------------------------- Hôte

        /// <summary>Hôte : commence la partie avec ces joueurs, déjà numérotés par le salon.</summary>
        [Server]
        public void Commencer(IEnumerable<PorteurCookies> joueurs) =>
            StartCoroutine(CommencerApresStart(new List<PorteurCookies>(joueurs)));

        // DeroulementPartie.Start() remet la partie à zéro : on attend qu'il soit passé (une image).
        private System.Collections.IEnumerator CommencerApresStart(List<PorteurCookies> joueurs)
        {
            yield return null;
            cookies.Miroir = false;
            deroulement.Miroir = false;
            // la Froggy en ligne (P2-02) porte les cookies sur sa copie visible, pas sur l'objet réseau que le salon
            // connaît : on prend aussi les joueurs numérotés de la scène
            foreach (var p in Joueurs()) if (!joueurs.Contains(p)) joueurs.Add(p);
            cookies.DemarrerNumerotes(Reperes(), joueurs);
            deroulement.Preparer();
            deroulement.Lancer();
            Publier();
        }

        /// <summary>Hôte : fait lâcher un cookie au joueur de ce numéro (ce que la torgnole appellera, P4-10).</summary>
        [Server]
        public bool FaireLacher(int joueur, Vector3 direction)
        {
            var porteur = cookies.Trouver(joueur);
            return porteur != null && cookies.FaireLacher(porteur, direction);
        }

        /// <summary>
        /// Hôte : ce joueur quitte la partie (P2-06), appelé par le salon <b>avant</b> que sa Froggy disparaisse. Le cookie
        /// sacré tombe derrière lui s'il le portait, ses cookies en couronne autour de lui, l'événement « joueur parti »
        /// part chez tous, puis il ne compte plus. La partie continue, sans bot à sa place.
        /// </summary>
        [Server]
        public void JoueurParti(int joueur)
        {
            var porteur = cookies.Trouver(joueur);
            if (porteur == null) return;
            var sacre = GetComponent<PartieCookieSacre>();
            if (sacre != null) sacre.LacherAuDepart(porteur);   // avant les cookies : rien ne le fait relâcher ensuite
            cookies.FaireToutLacher(porteur);
            EvenementsPartie.Emettre(TypeEvenement.JoueurParti, porteur.gameObject, null, porteur.transform.position);
            cookies.Retirer(porteur);
            if (porteurSacre.Value == joueur) PorteurCookieSacre = ReglesPartie.Personne;
            Publier();
        }

        /// <summary>Hôte : qui porte le cookie sacré (ReglesPartie.Personne : personne). Pour P5-08.</summary>
        public int PorteurCookieSacre
        {
            get => porteurSacre.Value;
            set
            {
                if (!IsServerStarted) return;
                porteurSacre.Value = value;
                deroulement.PorteurCookieSacre = cookies.Trouver(value);
            }
        }

        private void LateUpdate()
        {
            if (IsServerStarted) Publier();
            else if (aRefleter) Refleter();
        }

        /// <summary>Recopie ce que les règles ont décidé ; seules les différences partent.</summary>
        private void Publier()
        {
            ReglesCookies regles = cookies.Regles;
            if (regles == null) return;

            for (int i = 0; i < regles.NombrePoints; i++)
            {
                if (i >= presents.Count) presents.Add(regles.Present(i));
                else if (presents[i] != regles.Present(i)) presents[i] = regles.Present(i);
            }

            int n = 0;
            foreach (var p in cookies.Porteurs)
            {
                if (p == null) continue;
                var s = new ScoreReseau { Joueur = p.Joueur, Cookies = regles.Portes(p.Joueur) };
                if (n >= scores.Count) scores.Add(s);
                else if (scores[n].Joueur != s.Joueur || scores[n].Cookies != s.Cookies) scores[n] = s;
                n++;
            }
            while (scores.Count > n) scores.RemoveAt(scores.Count - 1);

            n = regles.NombreAuSol;
            for (int i = 0; i < n; i++)
            {
                var (numero, position) = regles.CookieAuSol(i);
                var c = new CookieAuSolReseau { Numero = numero, Position = position };
                if (i >= auSol.Count) auSol.Add(c);
                else if (auSol[i].Numero != numero) auSol[i] = c;
            }
            while (auSol.Count > n) auSol.RemoveAt(auSol.Count - 1);

            ReglesPartie partie = deroulement.Regles;
            if (partie == null) return;
            if (phase.Value != partie.Phase) phase.Value = partie.Phase;
            if (!Mathf.Approximately(reste.Value, partie.Reste)) reste.Value = partie.Reste;
            if (!Mathf.Approximately(prolonge.Value, partie.Prolonge)) prolonge.Value = partie.Prolonge;
            if (gagnant.Value != partie.Gagnant) gagnant.Value = partie.Gagnant;
        }

        // ---------------------------------------------------------------- Les autres joueurs

        private void Refleter()
        {
            aRefleter = false;
            // un joueur apparu après la partie (ordre d'arrivée des messages) : on l'ajoute
            foreach (var p in Joueurs()) cookies.Inscrire(p);

            lusPresents.Clear();
            foreach (bool b in presents) lusPresents.Add(b);
            lusAuSol.Clear();
            foreach (var c in auSol) lusAuSol.Add((c.Numero, c.Position));
            lusScores.Clear();
            foreach (var s in scores)
            {
                int nombre = s.Cookies;
                if (enAvance.TryGetValue(s.Joueur, out var avance))
                {
                    if (avance.nombre == s.Cookies || Time.unscaledTime > avance.jusqua) enAvance.Remove(s.Joueur);
                    else nombre = avance.nombre;
                }
                lusScores.Add((s.Joueur, nombre));
            }
            cookies.Refleter(lusPresents, lusAuSol, lusScores);

            if (finEnAvance.HasValue && (phase.Value == Phase.Finie || Time.unscaledTime > finEnAvance.Value.jusqua))
                finEnAvance = null;
            if (finEnAvance.HasValue)
                deroulement.Refleter(Phase.Finie, 0f, prolonge.Value, finEnAvance.Value.gagnant);
            else
                deroulement.Refleter(phase.Value, reste.Value, prolonge.Value, gagnant.Value);
            if (enAvance.Count > 0 || finEnAvance.HasValue) aRefleter = true;   // revoir à l'image suivante
        }

        /// <summary>
        /// Hôte : renvoie chaque événement de partie à tous les autres joueurs (fiable). Le message porte aussi le
        /// nouveau score de l'auteur : il arrive souvent avant la copie de l'état (envoyée toutes les 0,1 s), et un écran
        /// qui réagit à l'événement doit déjà lire le bon score (ou le bon gagnant).
        /// </summary>
        private void Relayer(EvenementPartie e)
        {
            if (e.Type == TypeEvenement.ObjetUtilise) return;   // ObjetsReseau l'envoie avec l'objet utilisé (#128)
            var porteur = e.Auteur != null ? e.Auteur.GetComponent<PorteurCookies>() : null;
            int joueur = porteur != null ? porteur.Joueur : ReglesPartie.Personne;
            int nombre = porteur != null && cookies.Regles != null ? cookies.Regles.Portes(joueur) : 0;
            EvenementChezTous(e.Type, joueur, nombre, e.Position);
        }

        [ObserversRpc(ExcludeServer = true)]
        private void EvenementChezTous(TypeEvenement type, int joueur, int nombre, Vector3 position)
        {
            var auteur = cookies.Trouver(joueur);
            if (auteur != null && (type == TypeEvenement.CookieRamasse || type == TypeEvenement.CookieLache))
                enAvance[joueur] = (nombre, Time.unscaledTime + 1f);
            if (type == TypeEvenement.PartieFinie)
                finEnAvance = (auteur != null ? joueur : ReglesPartie.Personne, Time.unscaledTime + 1f);
            Refleter();
            EvenementsPartie.Emettre(type, auteur != null ? auteur.gameObject : null, null, position);
        }

        // Ce que les messages d'événement ont appris avant la copie de l'état : gardé jusqu'à ce que la copie le
        // rattrape (ou 1 s au plus), pour qu'un score ne revienne pas en arrière une fraction de seconde.
        private readonly Dictionary<int, (int nombre, float jusqua)> enAvance = new();
        private (int gagnant, float jusqua)? finEnAvance;

        // ---------------------------------------------------------------- Outils

        // les repères de cookies de l'arène (la partie complète, placés au Bâtisseur), comme PartieCookies hors ligne
        private static IEnumerable<RepereArene> Reperes()
        {
            foreach (var r in FindObjectsByType<RepereArene>())
                if (r.sorte == RepereArene.Sorte.Cookie) yield return r;
        }

        /// <summary>Les personnages de la partie (Froggy en ligne ou capsules de test), numérotés par le salon.</summary>
        private static IEnumerable<PorteurCookies> Joueurs()
        {
            foreach (var p in FindObjectsByType<PorteurCookies>())
                if (p.Joueur > 0) yield return p;
        }
    }
}
