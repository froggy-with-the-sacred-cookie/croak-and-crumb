using System.Collections.Generic;
using System.Globalization;
using Froggy.Regles;
using UnityEngine;

namespace Froggy.Interface
{
    /// <summary>Un joueur tel que le HUD le montre.</summary>
    public struct JoueurHud
    {
        public int Joueur;
        public int Cookies;
        public Color Couleur;
    }

    /// <summary>
    /// Ce que le HUD lit de la partie à un instant (P6-02) : une photo, sans scène ni réseau. HudPartie la remplit à
    /// chaque image depuis les composants de la scène ; un test la remplit à la main. Une partie absente (pas d'objets,
    /// pas de torgnole dans cette scène) a son drapeau « …Connu » à faux : le HUD n'en montre rien.
    /// </summary>
    public class EtatHud
    {
        /// <summary>Le joueur de cette machine, ou ReglesPartie.Personne si on ne le connaît pas.</summary>
        public int Local = ReglesPartie.Personne;

        // déroulement (DeroulementPartie)
        public bool PartieConnue;
        public Phase Phase = Phase.Attente;
        public float Reste;
        public int Gagnant = ReglesPartie.Personne;
        public bool RejouerPermis;

        /// <summary>Les joueurs de la partie (PartieCookies), dans l'ordre de leur numéro.</summary>
        public readonly List<JoueurHud> Joueurs = new List<JoueurHud>();

        // cookie sacré (PartieCookieSacre, ou le porteur reçu de l'hôte)
        public EtatCookieSacre Sacre = EtatCookieSacre.Absent;
        public int PorteurSacre = ReglesPartie.Personne;

        // objet du joueur local (PartieObjets)
        public bool ObjetsConnus;
        /// <summary>Le nom de l'objet tenu ; vide : pas d'objet.</summary>
        public string Objet;
        public float RechargeObjet, DureeRechargeObjet;
        public string ToucheObjet = "";

        // torgnole du joueur local (PartieTorgnoles)
        public bool TorgnoleConnue;
        public float RechargeTorgnole, DureeRechargeTorgnole;
        public string ToucheTorgnole = "";

        // météo (GestionnaireMeteo) : vide quand le temps est calme
        public string Meteo;
        public bool MeteoAnnoncee;
        public float MeteoReste;
        public string MeteoEffets = "";

        /// <summary>Le nombre de cookies de ce joueur (0 s'il n'est pas de la partie).</summary>
        public int CookiesDe(int joueur)
        {
            foreach (var j in Joueurs)
                if (j.Joueur == joueur) return j.Cookies;
            return 0;
        }
    }

    /// <summary>
    /// Ce que le HUD écrit (P6-02), en C# pur : les textes et les jauges, tirés d'un EtatHud. Tout le jugement est ici
    /// (qui est en tête, comment se dit le temps, quoi dire du cookie sacré) pour être vérifié par des tests ; HudPartie
    /// ne fait que dessiner. Les formulations sont des PROPOSITIONS de l'IA, à reprendre avec l'habillage (jalon 2).
    /// </summary>
    public static class ContenuHud
    {
        /// <summary>À partir de combien de secondes restantes le minuteur dit « Dernière minute » (le gobelin arrive).</summary>
        public const float DerniereMinute = 60f;

        /// <summary>Le minuteur : le chiffre du compte à rebours, le temps de jeu, ou « Prolongation ». Vide sinon.</summary>
        public static string Minuteur(EtatHud e)
        {
            switch (e.Phase)
            {
                case Phase.Decompte: return Mathf.Max(1, Mathf.CeilToInt(e.Reste)).ToString(CultureInfo.InvariantCulture);
                case Phase.EnJeu: return DeroulementPartie.Temps(e.Reste);
                case Phase.Prolongation: return "Prolongation";
                default: return "";
            }
        }

        /// <summary>La ligne sous le minuteur : ce que le temps veut dire. Le texte double la couleur (accessibilité).</summary>
        public static string SousMinuteur(EtatHud e)
        {
            switch (e.Phase)
            {
                case Phase.Decompte: return "Prêts…";
                case Phase.EnJeu: return e.Reste <= DerniereMinute ? "Dernière minute" : "";
                case Phase.Prolongation: return "Le premier seul en tête gagne";
                default: return "";
            }
        }

        /// <summary>Le minuteur doit-il attirer l'œil (dernière minute, prolongation) ?</summary>
        public static bool MinuteurPresse(EtatHud e) =>
            (e.Phase == Phase.EnJeu && e.Reste <= DerniereMinute) || e.Phase == Phase.Prolongation;

        /// <summary>
        /// Qui est en tête, selon la règle de fin de partie (ReglesPartie.EnTete) : le porteur du cookie sacré, sinon le
        /// plus de cookies. Personne à égalité ou sans joueur.
        /// </summary>
        public static int EnTete(EtatHud e)
        {
            var scores = new List<(int, int)>(e.Joueurs.Count);
            foreach (var j in e.Joueurs) scores.Add((j.Joueur, j.Cookies));
            int meneur = ReglesPartie.EnTete(scores, PorteurSacreConnu(e), out bool egalite);
            return egalite ? ReglesPartie.Personne : meneur;
        }

        private static int PorteurSacreConnu(EtatHud e) =>
            e.Sacre == EtatCookieSacre.Porte ? e.PorteurSacre : ReglesPartie.Personne;

        /// <summary>« 0 cookie », « 1 cookie », « 3 cookies ».</summary>
        public static string Cookies(int n) => n + (n > 1 ? " cookies" : " cookie");

        /// <summary>Le nom d'un joueur à l'écran : « Joueur 2 », « Joueur 1 (toi) ».</summary>
        public static string Nom(EtatHud e, int joueur) => $"Joueur {joueur}" + (joueur == e.Local ? " (toi)" : "");

        /// <summary>
        /// Ce qui suit le score d'un joueur : « cookie sacré », « en tête », ou rien. Pendant le jeu seulement.
        /// </summary>
        public static string Marque(EtatHud e, int joueur)
        {
            if (e.Phase != Phase.EnJeu && e.Phase != Phase.Prolongation) return "";
            if (joueur == PorteurSacreConnu(e)) return "cookie sacré";
            return joueur == EnTete(e) ? "en tête" : "";
        }

        /// <summary>
        /// La bannière du cookie sacré, ou vide quand il n'est pas là. Il gagne s'il est porté à la fin (Notion, V1) :
        /// le « temps de garde » est donc le temps de jeu qui reste.
        /// </summary>
        public static string CookieSacre(EtatHud e)
        {
            if (e.Phase != Phase.EnJeu && e.Phase != Phase.Prolongation) return "";
            switch (e.Sacre)
            {
                case EtatCookieSacre.Gobelin:
                    return "Le gobelin s'enfuit avec le cookie sacré : attrapez-le !";
                case EtatCookieSacre.AuSol:
                    return "Le cookie sacré est tombé : ramassez-le !";
                case EtatCookieSacre.Porte:
                    string garde = e.Phase == Phase.EnJeu ? $" : encore {DeroulementPartie.Temps(e.Reste)} à le garder" : "";
                    return e.PorteurSacre == e.Local
                        ? "Tu portes le cookie sacré" + garde
                        : $"Joueur {e.PorteurSacre} porte le cookie sacré" + garde;
                default:
                    return "";
            }
        }

        /// <summary>L'emplacement d'objet du joueur local, ou vide si la scène n'a pas d'objets.</summary>
        public static string Objet(EtatHud e)
        {
            if (!e.ObjetsConnus) return "";
            string nom = string.IsNullOrEmpty(e.Objet) ? "aucun" : e.Objet;
            if (e.RechargeObjet > 0f) return $"Objet : {nom} · recharge {Secondes(e.RechargeObjet)}";
            if (string.IsNullOrEmpty(e.Objet)) return "Objet : aucun";
            return $"Objet : {nom}" + (e.ToucheObjet.Length > 0 ? $" · {e.ToucheObjet} pour l'utiliser" : "");
        }

        /// <summary>La torgnole du joueur local, ou vide si la scène n'en a pas.</summary>
        public static string Torgnole(EtatHud e)
        {
            if (!e.TorgnoleConnue) return "";
            string touche = e.ToucheTorgnole.Length > 0 ? $" ({e.ToucheTorgnole})" : "";
            return e.RechargeTorgnole > 0f
                ? $"Torgnole{touche} : {Secondes(e.RechargeTorgnole)}"
                : $"Torgnole{touche} : prête";
        }

        /// <summary>
        /// La météo, ou vide par temps calme. Comme l'affichage provisoire de GestionnaireMeteo : l'effet s'écrit en
        /// toutes lettres, un changement de jeu ne se devine pas.
        /// </summary>
        public static string Meteo(EtatHud e)
        {
            if (string.IsNullOrEmpty(e.Meteo)) return "";
            int s = Mathf.CeilToInt(Mathf.Max(0f, e.MeteoReste));
            string texte = e.MeteoAnnoncee ? $"Dans {s} s : {e.Meteo}" : $"{e.Meteo} : encore {s} s";
            return texte + (e.MeteoEffets.Length > 0 ? " · " + e.MeteoEffets : "");
        }

        /// <summary>Où en est une recharge, de 0 (vient de partir) à 1 (prête).</summary>
        public static float Jauge(float reste, float duree) =>
            reste <= 0f || duree <= 0f ? 1f : Mathf.Clamp01(1f - reste / duree);

        /// <summary>
        /// L'écran de fin : le titre (« Tu as gagné ! », « Joueur 2 gagne », « Partie finie ») et la raison. Sans le
        /// porteur du cookie sacré (il n'arrive pas encore chez les autres joueurs en réseau), on le déduit : un gagnant
        /// qui n'a pas seul le plus de cookies ne peut avoir gagné qu'avec le cookie sacré.
        /// </summary>
        public static (string titre, string raison) Fin(EtatHud e)
        {
            if (e.Phase != Phase.Finie) return ("", "");
            if (e.Gagnant == ReglesPartie.Personne) return ("Partie finie", "");
            string titre = e.Gagnant == e.Local ? "Tu as gagné !" : $"Joueur {e.Gagnant} gagne";

            int sesCookies = e.CookiesDe(e.Gagnant);
            bool seulEnTete = true;
            foreach (var j in e.Joueurs)
                if (j.Joueur != e.Gagnant && j.Cookies >= sesCookies) seulEnTete = false;
            bool parLeSacre = e.PorteurSacre == e.Gagnant || !seulEnTete;
            return (titre, parLeSacre ? "avec le cookie sacré" : "avec " + Cookies(sesCookies));
        }

        /// <summary>
        /// « 3,2 s » : une décimale arrondie vers le haut (jamais « 0,0 s » tant que ça recharge), virgule française,
        /// quelle que soit la langue de la machine.
        /// </summary>
        public static string Secondes(float s)
        {
            float t = Mathf.Ceil(s * 10f - 1e-3f) / 10f;
            if (t <= 0f) t = 0f;   // ni négatif ni « -0,0 »
            return t.ToString("0.0", CultureInfo.InvariantCulture).Replace('.', ',') + " s";
        }
    }
}
