using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Froggy.Outils
{
    /// <summary>
    /// La grille de l'analyse des trajets : pour chaque case de l'arène, la surface où Froggy tient (sol, nénuphar, berge),
    /// l'eau où il nage, ou rien (vide, trop raide, encombré) ; et les sauts possibles d'une surface à l'autre.
    /// </summary>
    public class GrilleTrajets
    {
        public const byte Vide = 0, Sol = 1, Eau = 2;

        public Vector3 origine; public float pas; public int nx, nz;
        public float[] hauteur; public byte[] genre;
        /// <summary>L'objet de chaque case de sol (indice dans objets), -1 sinon.</summary>
        public int[] objet;
        public List<GameObject> objets = new List<GameObject>();
        /// <summary>Les sauts, rangés par case de départ : de sautDebut[i] à sautDebut[i + 1] dans sautVers et sautCout.</summary>
        public int[] sautDebut, sautVers; public float[] sautCout;
        /// <summary>Le meilleur saut d'une surface à une autre : sa marge (portée moins distance ; négative : raté de peu) et ses deux cases.</summary>
        public Dictionary<(int, int), (float marge, int de, int vers)> liens = new Dictionary<(int, int), (float, int, int)>();

        public GrilleTrajets(Vector3 origine, float pas, int nx, int nz)
        {
            this.origine = origine; this.pas = pas; this.nx = nx; this.nz = nz;
            hauteur = new float[nx * nz]; genre = new byte[nx * nz]; objet = Enumerable.Repeat(-1, nx * nz).ToArray();
        }

        public int Taille => nx * nz;
        public Vector3 Centre(int i) => new Vector3(origine.x + (i % nx + .5f) * pas, hauteur[i], origine.z + (i / nx + .5f) * pas);

        public int Cellule(Vector3 p)
        {
            int x = Mathf.FloorToInt((p.x - origine.x) / pas), z = Mathf.FloorToInt((p.z - origine.z) / pas);
            return x < 0 || z < 0 || x >= nx || z >= nz ? -1 : z * nx + x;
        }

        /// <summary>La case praticable la plus proche de p (à rayon mètres au plus, à la hauteur la plus proche), ou -1.</summary>
        public int CellulePraticable(Vector3 p, float rayon)
        {
            int c = Cellule(p); if (c < 0) return -1;
            int r = Mathf.CeilToInt(rayon / pas), cx = c % nx, cz = c / nx, meilleure = -1; float best = float.MaxValue;
            for (int z = Mathf.Max(0, cz - r); z <= Mathf.Min(nz - 1, cz + r); z++)
                for (int x = Mathf.Max(0, cx - r); x <= Mathf.Min(nx - 1, cx + r); x++)
                {
                    int i = z * nx + x; if (genre[i] == Vide) continue;
                    float d = (Centre(i) - p).sqrMagnitude;
                    if (d < best) { best = d; meilleure = i; }
                }
            return best <= (rayon + pas) * (rayon + pas) ? meilleure : -1;
        }
    }

    /// <summary>
    /// L'analyse des trajets du Bâtisseur : combien de secondes il faut à Froggy pour aller d'un point de l'arène à un autre,
    /// en courant, en nageant et en sautant (de nénuphar en nénuphar, sur une berge plus haute). Sert à juger l'équité des
    /// apparitions (P8-02 : « chaque point d'apparition est à distance équitable des cookies »), à voir qui arrive le premier
    /// où (les territoires), et quels sauts sont serrés ou ratés de peu.
    /// Approximation : on court en ligne droite d'une case à l'autre, sans accélération ; un saut part à pleine vitesse, sans
    /// obstacle en vol ; les chiffres de Froggy sont ceux du gabarit (PROVISOIRES). Assez pour comparer deux apparitions ou
    /// deux dispositions, pas pour chronométrer au dixième.
    /// </summary>
    public static class TrajetsArene
    {
        public struct Mouvement { public float course, nage, hauteurSaut, gravite, marche, sortie; }

        /// <summary>
        /// Le temps de vol d'un saut de hauteurSaut, pour arriver denivele mètres plus haut (négatif : plus bas) ; -1 si c'est
        /// trop haut. La montée part à v0 = √(2gh), la retombée s'arrête à la hauteur d'arrivée.
        /// </summary>
        public static float TempsDeVol(float hauteurSaut, float denivele, float g)
        {
            float v0 = Mathf.Sqrt(2 * g * hauteurSaut), d = v0 * v0 - 2 * g * denivele;
            return d < 0 ? -1 : (v0 + Mathf.Sqrt(d)) / g;
        }

        private static readonly Vector2[] Directions = Enumerable.Range(0, 16)
            .Select(k => new Vector2(Mathf.Cos(k * Mathf.PI / 8), Mathf.Sin(k * Mathf.PI / 8))).ToArray();

        /// <summary>
        /// Les sauts de la grille pour ce mouvement : depuis chaque bord de surface, dans 16 directions, jusqu'à la première
        /// case où l'on peut se poser après un trou, une marche trop haute ou une descente. Note aussi, par paire de surfaces, le
        /// meilleur saut et sa marge (les ratés de peu, jusqu'à 0,75 m trop loin, ont une marge négative), sauf les sauts
        /// serrés qu'un détour à l'aise par une troisième surface rend inutiles.
        /// </summary>
        public static void Sauts(GrilleTrajets g, Mouvement m)
        {
            int n = g.Taille;
            var de = new List<int>(); var vers = new List<int>(); var cout = new List<float>();
            g.liens.Clear();
            float porteeMax = m.course * TempsDeVol(m.hauteurSaut, -m.hauteurSaut * 2, m.gravite) + 1;
            int kMax = Mathf.CeilToInt(porteeMax / g.pas);
            for (int s = 0; s < n; s++)
            {
                if (g.genre[s] != GrilleTrajets.Sol || !Bord(g, s, m.marche)) continue;
                int sx = s % g.nx, sz = s / g.nx; var ps = g.Centre(s);
                foreach (var d in Directions)
                {
                    bool trou = false; int precedente = s;
                    for (int k = 1; k <= kMax; k++)
                    {
                        int x = Mathf.RoundToInt(sx + d.x * k), z = Mathf.RoundToInt(sz + d.y * k);
                        if (x < 0 || z < 0 || x >= g.nx || z >= g.nz) break;
                        int t = z * g.nx + x; if (t == precedente) continue; precedente = t;
                        if (g.genre[t] != GrilleTrajets.Sol) { trou = true; continue; }
                        float dh = g.hauteur[t] - g.hauteur[s];
                        if (dh > m.hauteurSaut) break;                                   // un mur plus haut que le saut
                        if (!trou && Mathf.Abs(dh) <= m.marche) continue;                // la même surface : on y marche
                        if (!trou && dh < -m.marche) { trou = true; continue; }          // une descente : on peut sauter au-delà
                        var pt = g.Centre(t);
                        float dist = new Vector2(pt.x - ps.x, pt.z - ps.z).magnitude;
                        float vol = TempsDeVol(m.hauteurSaut, dh, m.gravite);
                        float marge = vol < 0 ? -99 : m.course * vol - dist;
                        if (marge >= 0) { de.Add(s); vers.Add(t); cout.Add(vol + .05f); }
                        int a = g.objet[s], b = g.objet[t];
                        if (a >= 0 && b >= 0 && a != b && marge > -.75f)
                        {
                            var cle = a < b ? (a, b) : (b, a);
                            if (!g.liens.TryGetValue(cle, out var l) || marge > l.marge) g.liens[cle] = (marge, s, t);
                        }
                        break;
                    }
                }
            }
            // un saut serré ou raté de peu ne compte pas si les deux surfaces se rejoignent déjà, à l'aise, par une troisième
            // (2026-10-29 : un long saut qui enjambait un nénuphar s'affichait en orange, alors qu'on passe par ce nénuphar)
            var voisins = new Dictionary<int, HashSet<int>>();
            foreach (var kv in g.liens)
                if (kv.Value.marge >= .5f)
                {
                    if (!voisins.TryGetValue(kv.Key.Item1, out var va)) voisins[kv.Key.Item1] = va = new HashSet<int>();
                    if (!voisins.TryGetValue(kv.Key.Item2, out var vb)) voisins[kv.Key.Item2] = vb = new HashSet<int>();
                    va.Add(kv.Key.Item2); vb.Add(kv.Key.Item1);
                }
            foreach (var cle in g.liens.Keys.ToList())
                if (g.liens[cle].marge < .5f && voisins.TryGetValue(cle.Item1, out var v1) && voisins.TryGetValue(cle.Item2, out var v2) && v1.Overlaps(v2))
                    g.liens.Remove(cle);

            // rangés par case de départ (les sauts sont déjà produits dans l'ordre des cases)
            g.sautDebut = new int[n + 1]; g.sautVers = vers.ToArray(); g.sautCout = cout.ToArray();
            int j = 0;
            for (int i = 0; i < n; i++) { g.sautDebut[i] = j; while (j < de.Count && de[j] == i) j++; }
            g.sautDebut[n] = j;
        }

        /// <summary>Une case de sol au bord : un voisin vide, dans l'eau, ou à une marche trop haute ou trop basse.</summary>
        private static bool Bord(GrilleTrajets g, int i, float marche)
        {
            int x = i % g.nx, z = i / g.nx;
            for (int dz = -1; dz <= 1; dz++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dz == 0) continue;
                    int vx = x + dx, vz = z + dz;
                    if (vx < 0 || vz < 0 || vx >= g.nx || vz >= g.nz) return true;
                    int v = vz * g.nx + vx;
                    if (g.genre[v] != GrilleTrajets.Sol || Mathf.Abs(g.hauteur[v] - g.hauteur[i]) > marche) return true;
                }
            return false;
        }

        /// <summary>
        /// Le temps, en secondes, pour aller de depart à chaque case (infini : on n'y arrive pas).
        /// Les temps se calculent et se rangent en double, et une amélioration doit dépasser 1 µs : le Mono de Unity calcule
        /// les float en double puis les arrondit en les rangeant, une case semblait améliorée sans l'être et repartait dans
        /// la file sans fin (2026-10-29 : plus de 15 minutes sur un sol plat de 6 000 cases, 2 ms en .NET).
        /// </summary>
        public static float[] Temps(GrilleTrajets g, int depart, Mouvement m)
        {
            int n = g.Taille;
            var t = new double[n];
            for (int i = 0; i < n; i++) t[i] = double.PositiveInfinity;
            var resultat = new float[n];
            if (depart < 0 || depart >= n || g.genre[depart] == GrilleTrajets.Vide)
            {
                for (int i = 0; i < n; i++) resultat[i] = float.PositiveInfinity;
                return resultat;
            }
            const double Ecart = 1e-6;
            var tas = new Tas(n); t[depart] = 0; tas.Ajouter(depart, 0);
            double diag = g.pas * 1.4142135623730951;
            while (tas.Nombre > 0)
            {
                var (i, ti) = tas.Retirer();
                if (ti > t[i]) continue;
                int x = i % g.nx, z = i / g.nx; byte gi = g.genre[i];
                for (int dz = -1; dz <= 1; dz++)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        if (dx == 0 && dz == 0) continue;
                        int vx = x + dx, vz = z + dz;
                        if (vx < 0 || vz < 0 || vx >= g.nx || vz >= g.nz) continue;
                        int j = vz * g.nx + vx; byte gj = g.genre[j];
                        if (gj == GrilleTrajets.Vide) continue;
                        float dh = g.hauteur[j] - g.hauteur[i]; double d = dx != 0 && dz != 0 ? diag : g.pas, c;
                        if (gi == GrilleTrajets.Sol && gj == GrilleTrajets.Sol)
                        {
                            if (dh > m.marche) continue;
                            c = d / m.course + (dh < -m.marche ? Mathf.Sqrt(2 * -dh / m.gravite) : 0);   // une marche à descendre : on tombe
                        }
                        else if (gj == GrilleTrajets.Eau)
                        {
                            if (gi == GrilleTrajets.Sol && dh > m.marche) continue;
                            c = d / m.nage;
                        }
                        else
                        {
                            if (dh > m.sortie) continue;                                       // sortir de l'eau sur une berge
                            c = d / m.course + .3f;
                        }
                        double tj = ti + c;
                        if (tj < t[j] - Ecart) { t[j] = tj; tas.Ajouter(j, tj); }
                    }
                if (g.sautDebut == null) continue;
                for (int e = g.sautDebut[i]; e < g.sautDebut[i + 1]; e++)
                {
                    int j = g.sautVers[e]; double tj = ti + g.sautCout[e];
                    if (tj < t[j] - Ecart) { t[j] = tj; tas.Ajouter(j, tj); }
                }
            }
            for (int i = 0; i < n; i++) resultat[i] = (float)t[i];
            return resultat;
        }

        /// <summary>Un tas binaire minimal (le .NET de Unity n'a pas de PriorityQueue).</summary>
        private class Tas
        {
            private int[] ids; private double[] cles; public int Nombre;
            public Tas(int capacite) { ids = new int[Mathf.Max(16, capacite)]; cles = new double[ids.Length]; }
            public void Ajouter(int id, double cle)
            {
                if (Nombre == ids.Length) { Array.Resize(ref ids, Nombre * 2); Array.Resize(ref cles, Nombre * 2); }
                int i = Nombre++;
                while (i > 0) { int p = (i - 1) / 2; if (cles[p] <= cle) break; ids[i] = ids[p]; cles[i] = cles[p]; i = p; }
                ids[i] = id; cles[i] = cle;
            }
            public (int, double) Retirer()
            {
                var r = (ids[0], cles[0]);
                int id = ids[--Nombre]; double cle = cles[Nombre]; int i = 0;
                while (true)
                {
                    int f = i * 2 + 1; if (f >= Nombre) break;
                    if (f + 1 < Nombre && cles[f + 1] < cles[f]) f++;
                    if (cles[f] >= cle) break;
                    ids[i] = ids[f]; cles[i] = cles[f]; i = f;
                }
                ids[i] = id; cles[i] = cle;
                return r;
            }
        }

        /// <summary>
        /// Mesure l'arène dans la zone : pour chaque case, la plus haute surface sous le plafond où Froggy tient (pente, place
        /// pour son corps), ou l'eau (reconnue au nom de son objet, car elle n'a pas de collision).
        /// </summary>
        public static GrilleTrajets Construire(Bounds zone, float plafond, GabaritFroggy gab)
        {
            float pas = gab.pas;
            // pas plus de 250 000 cases : au-delà, la case grandit
            while ((zone.size.x / pas) * (zone.size.z / pas) > 250000) pas *= 1.5f;
            int nx = Mathf.Max(1, Mathf.CeilToInt(zone.size.x / pas)), nz = Mathf.Max(1, Mathf.CeilToInt(zone.size.z / pas));
            var g = new GrilleTrajets(new Vector3(zone.min.x, 0, zone.min.z), pas, nx, nz);
            Physics.SyncTransforms();

            var noms = gab.nomsEau.Split(';').Select(s => s.Trim()).Where(s => s.Length > 0).ToArray();
            var eaux = UnityEngine.Object.FindObjectsByType<Renderer>()
                .Where(r => r.enabled && r.gameObject.activeInHierarchy && noms.Any(nm => r.name.IndexOf(nm, StringComparison.OrdinalIgnoreCase) >= 0))
                .Select(r => r.bounds).ToList();
            var index = new Dictionary<GameObject, int>();
            var touches = new RaycastHit[32]; var autour = new Collider[16];
            float haut = plafond + 2;
            for (int i = 0; i < g.Taille; i++)
            {
                var p = new Vector3(g.origine.x + (i % nx + .5f) * pas, haut, g.origine.z + (i / nx + .5f) * pas);
                int nb = Physics.RaycastNonAlloc(p, Vector3.down, touches, haut - zone.min.y + 20, ~0, QueryTriggerInteraction.Ignore);
                Array.Sort(touches, 0, nb, ParDistance);
                bool encombre = false;
                for (int h = 0; h < nb; h++)
                {
                    var hit = touches[h];
                    if (hit.point.y > plafond || Mobile(hit.collider)) continue;
                    if (Vector3.Angle(hit.normal, Vector3.up) > gab.penteMax) break;
                    // la place pour le corps, au-dessus de la marche
                    var bas = hit.point + Vector3.up * (gab.rayon + gab.hauteurMarche);
                    var hautCorps = hit.point + Vector3.up * Mathf.Max(gab.rayon + gab.hauteurMarche, gab.hauteur - gab.rayon);
                    int nc = Physics.OverlapCapsuleNonAlloc(bas, hautCorps, gab.rayon * .9f, autour, ~0, QueryTriggerInteraction.Ignore);
                    for (int c = 0; c < nc; c++) if (autour[c] != hit.collider && !Mobile(autour[c])) { encombre = true; break; }
                    if (encombre) break;
                    g.genre[i] = GrilleTrajets.Sol; g.hauteur[i] = hit.point.y;
                    var o = BatisseurArene.ObjetDe(hit.collider.gameObject);
                    if (!index.TryGetValue(o, out int k)) { k = g.objets.Count; g.objets.Add(o); index[o] = k; }
                    g.objet[i] = k;
                    break;
                }
                // l'eau : on y nage si sa surface passe au-dessus du sol trouvé (un nénuphar, une berge restent du sol)
                if (encombre) continue;
                foreach (var e in eaux)
                {
                    if (p.x < e.min.x || p.x > e.max.x || p.z < e.min.z || p.z > e.max.z) continue;
                    if (g.genre[i] == GrilleTrajets.Sol && e.max.y <= g.hauteur[i] + .15f) continue;
                    g.genre[i] = GrilleTrajets.Eau; g.hauteur[i] = e.max.y; g.objet[i] = -1;
                }
            }
            return g;
        }

        private static readonly IComparer<RaycastHit> ParDistance = Comparer<RaycastHit>.Create((a, b) => a.distance.CompareTo(b.distance));

        private static bool Mobile(Collider c) => c.attachedRigidbody != null && !c.attachedRigidbody.isKinematic;
    }
}
