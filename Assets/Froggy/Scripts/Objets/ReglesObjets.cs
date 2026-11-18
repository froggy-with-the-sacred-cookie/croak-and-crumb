using System.Collections.Generic;
using UnityEngine;

namespace Froggy.Objets
{
    /// <summary>
    /// Les règles des objets (P4-01), sans réseau ni scène : des boîtes mystère à des points, un joueur assez proche et
    /// sans objet en prend une et reçoit un objet tiré au hasard, il l'utilise, puis attend le temps de recharge. La
    /// boîte revient après un délai. Un seul emplacement d'objet par joueur.
    /// Du C# pur, comme ReglesCookies : l'hôte le fera tourner tel quel en réseau. À entrées égales (graine, positions,
    /// pas de temps, demandes d'utilisation), le résultat est le même partout : le tirage a son propre générateur, il ne
    /// dépend ni de la machine ni de UnityEngine.Random. Un client n'annonce jamais « j'ai eu tel objet ».
    /// Les objets sont des numéros (leur rang dans la liste des fiches) : ce que fait un objet n'est pas ici.
    /// </summary>
    public class ReglesObjets
    {
        /// <summary>Pas d'objet dans l'emplacement.</summary>
        public const int Aucun = -1;

        /// <summary>Une boîte prise pendant un pas : à quel point, par quel joueur, quel objet il a reçu.</summary>
        public readonly struct Prise
        {
            public readonly int Point, Joueur, Objet;
            public Prise(int point, int joueur, int objet) { Point = point; Joueur = joueur; Objet = objet; }
        }

        private readonly Vector3[] points;
        private readonly bool[] present;
        private readonly float[] attente;
        private readonly float[] poids, recharges;
        private readonly float totalPoids, rayon, delai;
        private readonly SortedDictionary<int, int> emplacements = new SortedDictionary<int, int>();
        private readonly SortedDictionary<int, float> enRecharge = new SortedDictionary<int, float>();
        private readonly List<int> finis = new List<int>();
        private uint hasard;

        /// <param name="points">Où sont les boîtes (leur centre). Une boîte par point au départ.</param>
        /// <param name="poids">La part de chaque objet dans le tirage (0 : ne sort jamais).</param>
        /// <param name="recharges">Le temps de recharge de chaque objet après utilisation, en s (même rang que « poids »).</param>
        /// <param name="rayonRamassage">Distance à laquelle un joueur prend une boîte, en m.</param>
        /// <param name="delaiReapparition">Temps avant qu'une boîte revienne, en s. 0 : jamais.</param>
        /// <param name="graine">Graine du tirage : même graine, même suite d'objets.</param>
        public ReglesObjets(IReadOnlyList<Vector3> points, IReadOnlyList<float> poids, IReadOnlyList<float> recharges,
            float rayonRamassage, float delaiReapparition, int graine)
        {
            this.points = new Vector3[points.Count];
            present = new bool[points.Count];
            attente = new float[points.Count];
            for (int i = 0; i < points.Count; i++) { this.points[i] = points[i]; present[i] = true; }

            this.poids = new float[poids.Count];
            this.recharges = new float[poids.Count];
            for (int i = 0; i < poids.Count; i++)
            {
                this.poids[i] = Mathf.Max(0f, poids[i]);
                this.recharges[i] = recharges != null && i < recharges.Count ? Mathf.Max(0f, recharges[i]) : 0f;
                totalPoids += this.poids[i];
            }
            rayon = Mathf.Max(0f, rayonRamassage);
            delai = Mathf.Max(0f, delaiReapparition);
            hasard = graine != 0 ? (uint)graine : 0x9E3779B9u;   // un xorshift ne sort jamais de 0
        }

        public int NombrePoints => points.Length;
        public int NombreObjets => poids.Length;
        public Vector3 Point(int i) => points[i];

        /// <summary>Y a-t-il une boîte à ce point en ce moment ?</summary>
        public bool Present(int point) => present[point];

        /// <summary>L'objet dans l'emplacement d'un joueur, ou Aucun.</summary>
        public int Objet(int joueur) => emplacements.TryGetValue(joueur, out int o) ? o : Aucun;

        /// <summary>Le temps de recharge qui reste à un joueur, en s. 0 : il peut utiliser un objet.</summary>
        public float Recharge(int joueur) => enRecharge.TryGetValue(joueur, out float t) ? t : 0f;

        /// <summary>
        /// Un pas de la partie : les recharges avancent, les boîtes attendues reviennent, puis chaque boîte présente va au
        /// joueur sans objet le plus proche à portée (à égalité, au plus petit numéro), qui reçoit un objet tiré au
        /// hasard. Les boîtes prises s'ajoutent à « prises ».
        /// </summary>
        public void Pas(float dt, IReadOnlyList<(int joueur, Vector3 position)> joueurs, List<Prise> prises)
        {
            if (enRecharge.Count > 0)
            {
                finis.Clear();
                foreach (var recharge in enRecharge) finis.Add(recharge.Key);
                foreach (int joueur in finis)
                {
                    float reste = enRecharge[joueur] - dt;
                    if (reste <= 0f) enRecharge.Remove(joueur);
                    else enRecharge[joueur] = reste;
                }
            }

            if (delai > 0f)
                for (int i = 0; i < points.Length; i++)
                {
                    if (present[i]) continue;
                    attente[i] -= dt;
                    if (attente[i] <= 0f) present[i] = true;
                }

            if (totalPoids <= 0f) return;   // aucun objet à donner : les boîtes restent
            float rayon2 = rayon * rayon;
            for (int i = 0; i < points.Length; i++)
            {
                if (!present[i]) continue;
                int gagnant = 0;
                float meilleure = float.MaxValue;
                bool trouve = false;
                foreach (var (joueur, position) in joueurs)
                {
                    if (Objet(joueur) != Aucun) continue;   // un seul emplacement : il laisse la boîte
                    float d2 = (position - points[i]).sqrMagnitude;
                    if (d2 > rayon2) continue;
                    if (trouve && (d2 > meilleure || (d2 == meilleure && joueur >= gagnant))) continue;
                    trouve = true;
                    gagnant = joueur;
                    meilleure = d2;
                }
                if (!trouve) continue;
                present[i] = false;
                attente[i] = delai;
                int objet = Tirer();
                emplacements[gagnant] = objet;
                prises?.Add(new Prise(i, gagnant, objet));
            }
        }

        /// <summary>
        /// Le joueur utilise son objet. Renvoie l'objet utilisé (son emplacement se vide, sa recharge commence), ou Aucun
        /// s'il n'a pas d'objet ou s'il est encore en recharge.
        /// </summary>
        public int Utiliser(int joueur)
        {
            int objet = Objet(joueur);
            if (objet == Aucun || Recharge(joueur) > 0f) return Aucun;
            emplacements.Remove(joueur);
            if (recharges[objet] > 0f) enRecharge[joueur] = recharges[objet];
            return objet;
        }

        /// <summary>Vide l'emplacement et la recharge d'un joueur (réapparition, départ).</summary>
        public void Vider(int joueur)
        {
            emplacements.Remove(joueur);
            enRecharge.Remove(joueur);
        }

        // tirage pondéré ; xorshift32 : la même suite sur toutes les machines
        private int Tirer()
        {
            hasard ^= hasard << 13;
            hasard ^= hasard >> 17;
            hasard ^= hasard << 5;
            float cible = (hasard >> 8) / 16777216f * totalPoids;   // 24 bits : exact en float, dans [0, total[
            int dernier = Aucun;
            for (int i = 0; i < poids.Length; i++)
            {
                if (poids[i] <= 0f) continue;
                dernier = i;
                if (cible < poids[i]) return i;
                cible -= poids[i];
            }
            return dernier;   // arrondi : le dernier objet qui peut sortir
        }
    }
}
