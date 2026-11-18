using System.Collections.Generic;
using Froggy.Socle;
using UnityEngine;

namespace Froggy.Regles
{
    /// <summary>
    /// Fait dérouler une partie dans une scène (P3-03), sans réseau : compte à rebours, temps de jeu, fin, gagnant. Les
    /// règles sont dans ReglesPartie ; ce composant lit les scores de PartieCookies, retient les joueurs pendant le compte
    /// à rebours (par les Modificateurs du socle, sans toucher au contrôleur), arrête les cookies avant le départ et après
    /// la fin, et émet « partie commencée » et « partie finie ».
    /// Le cookie sacré n'existe pas encore (P5-08) : celui qui le portera se donne par PorteurCookieSacre.
    /// À la fin, l'événement « partie finie » part ; en réseau, le salon (P2-03) ramène alors tout le monde.
    /// L'affichage (temps, gagnant) est provisoire, en attendant le HUD (P6-02).
    /// En réseau (P2-04, PartieReseau) : l'hôte le fait tourner ; chez les autres il est en « Miroir » (Refleter).
    /// </summary>
    public class DeroulementPartie : MonoBehaviour
    {
        [SerializeField] private ReglagesPartie reglages;
        [SerializeField] private PartieCookies cookies;
        [Tooltip("Lance le compte à rebours dès le début de la scène. Sinon, appeler Lancer().")]
        [SerializeField] private bool lancerAuDepart = true;
        [Tooltip("Affiche le temps et le gagnant, en attendant le HUD (P6-02).")]
        [SerializeField] private bool affichageProvisoire = true;

        private readonly List<(int, int)> scores = new List<(int, int)>();
        private readonly List<Modificateurs> retenus = new List<Modificateurs>();
        private GUIStyle petit, grand;

        public ReglesPartie Regles { get; private set; }

        /// <summary>Celui qui porte le cookie sacré, ou vide. À tenir à jour par P5-08.</summary>
        public PorteurCookies PorteurCookieSacre { get; set; }

        /// <summary>Le gagnant, une fois la partie finie (vide si elle s'est finie sans joueur).</summary>
        public PorteurCookies Gagnant { get; private set; }

        /// <summary>Pour un test : des réglages et une partie de cookies à soi.</summary>
        public void Regler(ReglagesPartie r, PartieCookies c)
        {
            reglages = r;
            cookies = c;
        }

        private void Start()
        {
            Preparer();
            if (lancerAuDepart) Lancer();
        }

        /// <summary>Remet la partie à zéro, pas encore lancée : les cookies attendent.</summary>
        public void Preparer()
        {
            if (reglages == null) reglages = ScriptableObject.CreateInstance<ReglagesPartie>();
            Liberer();
            Regles = new ReglesPartie(reglages.decompte, reglages.duree);
            Gagnant = null;
            if (cookies != null) cookies.EnPause = true;
        }

        /// <summary>Lance le compte à rebours.</summary>
        public void Lancer()
        {
            if (Regles == null) Preparer();
            Regles.Lancer();
        }

        /// <summary>En réseau, chez un joueur qui n'est pas l'hôte (P2-04) : rien ne se décide ici, voir Refleter.</summary>
        public bool Miroir { get; set; }

        /// <summary>En Miroir : montre l'état décidé par l'hôte (phase, temps restant, prolongation, gagnant).</summary>
        public void Refleter(Phase phase, float reste, float prolonge, int gagnant)
        {
            if (Regles == null) Preparer();
            Regles.Imposer(phase, reste, prolonge, gagnant);
            Gagnant = null;
            if (phase == Phase.Finie && cookies != null)
                foreach (var p in cookies.Porteurs)
                    if (p != null && p.Joueur == gagnant) Gagnant = p;
        }

        private void FixedUpdate() => Pas(Time.fixedDeltaTime);

        /// <summary>Un pas de la partie, au pas physique (un test l'appelle hors jeu). À appeler avant celui des cookies.</summary>
        public void Pas(float dt)
        {
            if (Regles == null || cookies == null || Miroir) return;
            Phase avant = Regles.Phase;

            scores.Clear();
            foreach (var p in cookies.Porteurs)
                if (p != null) scores.Add((p.Joueur, p.Nombre));
            int sacre = PorteurCookieSacre != null ? PorteurCookieSacre.Joueur : ReglesPartie.Personne;
            Regles.Pas(dt, scores, sacre);

            Phase phase = Regles.Phase;
            if (phase == Phase.Attente || phase == Phase.Decompte) Retenir();
            else Liberer();
            cookies.EnPause = phase != Phase.EnJeu && phase != Phase.Prolongation;

            if (phase == avant) return;
            if (phase == Phase.EnJeu) EvenementsPartie.Emettre(TypeEvenement.PartieCommencee);
            if (phase == Phase.Finie)
            {
                foreach (var p in cookies.Porteurs)
                    if (p != null && p.Joueur == Regles.Gagnant) Gagnant = p;
                EvenementsPartie.Emettre(TypeEvenement.PartieFinie, Gagnant != null ? Gagnant.gameObject : null);
            }
        }

        // pendant le compte à rebours, personne ne bouge : vitesse et saut à zéro, par les modificateurs du socle
        private void Retenir()
        {
            foreach (var p in cookies.Porteurs)
            {
                var m = p != null ? p.GetComponent<Modificateurs>() : null;
                if (m == null || retenus.Contains(m)) continue;
                m.Poser(this, vitesse: 0f, saut: 0f);
                retenus.Add(m);
            }
        }

        private void Liberer()
        {
            foreach (var m in retenus)
                if (m != null) m.Retirer(this);
            retenus.Clear();
        }

        private void OnDisable() => Liberer();

        /// <summary>Le temps en minutes et secondes, arrondi vers le haut : « 0:01 » jusqu'à la dernière image.</summary>
        public static string Temps(float secondes)
        {
            int s = Mathf.CeilToInt(Mathf.Max(0f, secondes));
            return $"{s / 60}:{s % 60:00}";
        }

        private void OnGUI()
        {
            if (!affichageProvisoire || Regles == null) return;
            if (petit == null)
            {
                petit = new GUIStyle(GUI.skin.label) { fontSize = 26, fontStyle = FontStyle.Bold, alignment = TextAnchor.UpperCenter };
                grand = new GUIStyle(petit) { fontSize = 72, alignment = TextAnchor.MiddleCenter };
            }
            switch (Regles.Phase)
            {
                case Phase.Decompte:
                    Ecrire(new Rect(0f, 0f, Screen.width, Screen.height), Mathf.CeilToInt(Regles.Reste).ToString(), grand);
                    break;
                case Phase.EnJeu:
                    Ecrire(new Rect(0f, 10f, Screen.width, 40f), Temps(Regles.Reste), petit);
                    break;
                case Phase.Prolongation:
                    Ecrire(new Rect(0f, 10f, Screen.width, 40f), "Prolongation : le premier en tête gagne", petit);
                    break;
                case Phase.Finie:
                    Ecrire(new Rect(0f, 0f, Screen.width, Screen.height),
                        Gagnant != null ? $"Joueur {Gagnant.Joueur} gagne" : "Partie finie", grand);
                    break;
            }
        }

        // un contour sombre : lisible sur le ciel comme sur l'herbe
        private static void Ecrire(Rect zone, string texte, GUIStyle style)
        {
            style.normal.textColor = Color.black;
            GUI.Label(new Rect(zone.x + 2f, zone.y + 2f, zone.width, zone.height), texte, style);
            style.normal.textColor = Color.white;
            GUI.Label(zone, texte, style);
        }
    }
}
