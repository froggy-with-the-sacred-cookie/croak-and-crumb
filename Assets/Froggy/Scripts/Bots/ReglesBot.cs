using System.Collections.Generic;
using UnityEngine;

namespace Froggy.Bots
{
    /// <summary>Un rival, vu par un bot.</summary>
    public struct RivalVu
    {
        public int joueur;
        public Vector3 position;
        public int cookies;
        public bool intouchable, porteSacre;
    }

    /// <summary>Ce qu'un bot sait au moment de décider : sa place, la partie, les autres.</summary>
    public class SituationBot
    {
        public bool enJeu = true;
        public Vector3 position, avant = Vector3.forward;
        public int cookies;
        public bool porteSacre, torgnolePrete = true, aUnObjet, objetPret, dashPret;
        public readonly List<RivalVu> rivaux = new List<RivalVu>();
        public readonly List<Vector3> cookiesVisibles = new List<Vector3>();
        public readonly List<Vector3> boites = new List<Vector3>();
        /// <summary>Le gobelin qui fuit avec le cookie sacré, s'il est là.</summary>
        public Vector3? gobelin;
        /// <summary>Le cookie sacré posé au sol, s'il y est.</summary>
        public Vector3? sacreAuSol;

        public void Vider()
        {
            rivaux.Clear();
            cookiesVisibles.Clear();
            boites.Clear();
            gobelin = sacreAuSol = null;
            cookies = 0;
            porteSacre = aUnObjet = objetPret = false;
            torgnolePrete = enJeu = true;
        }
    }

    /// <summary>Ce que le bot veut faire, jusqu'à sa prochaine décision.</summary>
    public struct IntentionBot
    {
        public enum Sorte { Attendre, Aller, Fuir }

        public Sorte sorte;
        /// <summary>Où aller (Aller), ou le point d'où il s'éloigne (Fuir).</summary>
        public Vector3 but;
        /// <summary>Le joueur visé, ou 0.</summary>
        public int cible;
        public bool frapper, utiliserObjet;
        /// <summary>Dasher maintenant (#134) : rattraper, fuir ; jamais pour un simple cookie.</summary>
        public bool dash;
        /// <summary>Pourquoi, en mots : pour le menu de debug et les tests.</summary>
        public string raison;
    }

    /// <summary>
    /// Les décisions d'un bot (P3-05), en C# pur : sans scène ni réseau, pour que les tests les vérifient et que l'hôte
    /// les fasse tourner plus tard. Ordre des priorités (choix de Lucas, 18 novembre 2026, propositions pour l'équipe) :
    /// 1. il porte le cookie sacré : il fuit, et riposte s'il est coincé ;
    /// 2. le cookie sacré est en jeu : tous dessus (le gobelin, le cookie au sol, puis celui qui le porte) ;
    /// 3. il porte des cookies et un rival moins chargé approche (celui-là a tout à gagner) : il s'éloigne, d'autant plus
    ///    tôt qu'il porte beaucoup ; face à un rival aussi chargé ou plus, il attaque (sinon, en fin de partie, tous les
    ///    bots chargés se fuyaient et plus personne ne frappait : essai du 18 novembre 2026) ;
    /// 4. un rival a au moins « ecartLeader » cookies d'avance sur lui : il va le chercher, même de loin (chasse au
    ///    leader, choix de Lucas du 18 novembre 2026 : dans un essai, un bot était monté à 7 cookies sans être inquiété) ;
    /// 5. un rival chargé passe près de lui : il le chasse et vise le plus chargé (ça freine le leader) ;
    /// 6. sinon le cookie le plus proche, puis une boîte à objets s'il n'en a pas ; les mains vides, il va embêter le
    ///    rival le plus proche, chargé, il garde ses cookies.
    /// Il utilise son objet dès qu'un rival est assez près, quel que soit l'objet : une attaque ajoutée plus tard marche
    /// donc chez lui sans rien changer ici.
    /// </summary>
    public static class ReglesBot
    {
        /// <summary>Décide. « alea » sert à rater parfois : un même tirage donne la même décision.</summary>
        public static IntentionBot Decider(SituationBot s, ReglagesBot r, System.Random alea)
        {
            if (!s.enJeu) return new IntentionBot { sorte = IntentionBot.Sorte.Attendre, raison = "pas en jeu" };

            int proche = PlusProche(s, s.position, float.PositiveInfinity, false, out float dProche);
            bool objet = s.objetPret && proche >= 0 && dProche <= r.rayonObjet;

            // 1. le cookie sacré dans les mains : fuir
            if (s.porteSacre)
            {
                if (proche < 0) return Attendre(objet, "porte le cookie sacré, personne autour");
                var menace = s.rivaux[proche];
                if (dProche <= r.porteeFrappe && s.torgnolePrete) return Frapper(menace, r, objet, "coincé avec le cookie sacré : riposte");
                return Fuir(s, menace.position, menace.joueur, r, objet, "porte le cookie sacré : fuit");
            }

            // 2. le cookie sacré est en jeu : tous dessus
            if (s.gobelin.HasValue) return Aller(s.gobelin.Value, 0, DashVers(s, r, s.gobelin.Value), objet, "poursuit le gobelin");
            if (s.sacreAuSol.HasValue) return Aller(s.sacreAuSol.Value, 0, DashVers(s, r, s.sacreAuSol.Value), objet, "court au cookie sacré");
            for (int i = 0; i < s.rivaux.Count; i++)
                if (s.rivaux[i].porteSacre)
                    return Chasser(s, s.rivaux[i], r, alea, objet, true, "chasse le porteur du cookie sacré");

            // 3. chargé et menacé par un moins chargé (lui a tout à gagner) : s'éloigner, riposter si coincé
            if (s.cookies > 0)
            {
                float rayon = Mathf.Min(r.prudenceMax, r.rayonPrudence + r.prudenceParCookie * (s.cookies - 1));
                int m = PlusProche(s, s.position, rayon, false, out float dm, moinsDe: s.cookies);
                if (m >= 0)
                {
                    if (dm <= r.porteeFrappe && s.torgnolePrete) return Frapper(s.rivaux[m], r, objet, "coincé : riposte");
                    return Fuir(s, s.rivaux[m].position, s.rivaux[m].joueur, r, objet, $"porte {s.cookies} cookie(s) : s'éloigne");
                }
            }

            // 4. un leader qui s'envole : on va le chercher, même de loin (contre la boule de neige)
            int leader = PlusCharge(s, r.rayonLeader);
            if (leader >= 0 && s.rivaux[leader].cookies >= s.cookies + r.ecartLeader)
                // un dash pour combler l'écart quand il est à portée (le sprint, retiré le 18 novembre 2026, servait à ça)
                return Chasser(s, s.rivaux[leader], r, alea, objet, true, $"chasse le leader, joueur {s.rivaux[leader].joueur}");

            // 5. un rival chargé tout près : le plus chargé
            int cookie = PlusProchePoint(s.cookiesVisibles, s.position, out float dCookie);
            int charge = PlusCharge(s, r.rayonChasse);
            if (charge >= 0 && Distance(s.position, s.rivaux[charge].position) <= dCookie)
                return Chasser(s, s.rivaux[charge], r, alea, objet, false, $"chasse le joueur {s.rivaux[charge].joueur}, chargé");

            // 6. ramasser, s'équiper, ou embêter
            if (cookie >= 0) return Aller(s.cookiesVisibles[cookie], 0, false, objet, "va au cookie le plus proche");
            if (!s.aUnObjet)
            {
                int boite = PlusProchePoint(s.boites, s.position, out float dBoite);
                if (boite >= 0 && dBoite <= r.rayonBoite) return Aller(s.boites[boite], 0, false, objet, "va à la boîte à objets");
            }
            // chargé, il garde ce qu'il a ; les mains vides, il va embêter
            if (s.cookies > 0) return Attendre(objet, "rien à ramasser : garde ses cookies");
            if (proche >= 0) return Chasser(s, s.rivaux[proche], r, alea, objet, false, "rien à ramasser : va embêter le plus proche");
            return Attendre(objet, "rien à faire");
        }

        /// <summary>Le point dont le bot s'éloigne : à l'opposé de la menace, à quelques mètres.</summary>
        public static Vector3 PointDeFuite(Vector3 position, Vector3 menace, float distance = 5f)
        {
            Vector3 loin = position - menace;
            loin.y = 0f;
            if (loin.sqrMagnitude < 1e-4f) loin = Vector3.forward;
            return position + loin.normalized * distance;
        }

        /// <summary>Un dash vers « but » : s'il est prêt et que le but est à portée de dash (ni trop près, ni trop loin).</summary>
        public static bool DashVers(SituationBot s, ReglagesBot r, Vector3 but)
        {
            if (!r.dasher || !s.dashPret) return false;
            float d = Distance(s.position, but);
            return d >= r.dashMin && d <= r.dashMax;
        }

        private static IntentionBot Chasser(SituationBot s, RivalVu rival, ReglagesBot r, System.Random alea, bool objet, bool presse, string raison)
        {
            // pressé (leader, porteur du cookie sacré) : un dash pour le rattraper, ou le bousculer, quand il est à portée
            var i = Aller(rival.position, rival.joueur, presse && !rival.intouchable && DashVers(s, r, rival.position), objet, raison);
            // brouillon : parfois il frappe de trop loin, et rate
            float portee = alea != null && alea.NextDouble() < r.chanceRater ? r.porteeFrappe + 1f : r.porteeFrappe;
            i.frapper = s.torgnolePrete && !rival.intouchable && Distance(s.position, rival.position) <= portee;
            return i;
        }

        private static IntentionBot Frapper(RivalVu rival, ReglagesBot r, bool objet, string raison)
        {
            var i = Aller(rival.position, rival.joueur, false, objet, raison);
            i.frapper = true;
            return i;
        }

        private static IntentionBot Aller(Vector3 but, int cible, bool dash, bool objet, string raison) =>
            new IntentionBot { sorte = IntentionBot.Sorte.Aller, but = but, cible = cible, dash = dash, utiliserObjet = objet, raison = raison };

        // fuir : un dash quand la menace est toute proche (le bot se sert de la position dans la situation)
        private static IntentionBot Fuir(SituationBot s, Vector3 menace, int joueur, ReglagesBot r, bool objet, string raison) =>
            new IntentionBot
            {
                sorte = IntentionBot.Sorte.Fuir, but = menace, cible = joueur, utiliserObjet = objet, raison = raison,
                dash = r.dasher && s.dashPret && Distance(s.position, menace) <= r.dashFuite,
            };

        private static IntentionBot Attendre(bool objet, string raison) =>
            new IntentionBot { sorte = IntentionBot.Sorte.Attendre, utiliserObjet = objet, raison = raison };

        /// <summary>Le rival frappable le plus proche à moins de « rayon » (-1 sinon) ; « moinsDe » : qui porte moins que ça.</summary>
        private static int PlusProche(SituationBot s, Vector3 p, float rayon, bool chargeSeulement, out float distance,
            int moinsDe = int.MaxValue)
        {
            int choisi = -1;
            distance = float.PositiveInfinity;
            for (int i = 0; i < s.rivaux.Count; i++)
            {
                var v = s.rivaux[i];
                if (v.intouchable || (chargeSeulement && v.cookies <= 0) || v.cookies >= moinsDe) continue;
                float d = Distance(p, v.position);
                if (d <= rayon && d < distance) { distance = d; choisi = i; }
            }
            return choisi;
        }

        /// <summary>Parmi les rivaux chargés à moins de « rayon », celui qui porte le plus (le plus proche à égalité).</summary>
        private static int PlusCharge(SituationBot s, float rayon)
        {
            int choisi = -1;
            float dChoisi = float.PositiveInfinity;
            for (int i = 0; i < s.rivaux.Count; i++)
            {
                var v = s.rivaux[i];
                if (v.intouchable || v.cookies <= 0) continue;
                float d = Distance(s.position, v.position);
                if (d > rayon) continue;
                if (choisi < 0 || v.cookies > s.rivaux[choisi].cookies || (v.cookies == s.rivaux[choisi].cookies && d < dChoisi))
                {
                    choisi = i;
                    dChoisi = d;
                }
            }
            return choisi;
        }

        private static int PlusProchePoint(List<Vector3> points, Vector3 p, out float distance)
        {
            int choisi = -1;
            distance = float.PositiveInfinity;
            for (int i = 0; i < points.Count; i++)
            {
                float d = Distance(p, points[i]);
                if (d < distance) { distance = d; choisi = i; }
            }
            return choisi;
        }

        /// <summary>Distance au sol (sans la hauteur).</summary>
        public static float Distance(Vector3 a, Vector3 b)
        {
            a.y = b.y = 0f;
            return Vector3.Distance(a, b);
        }
    }
}
