using System.Collections.Generic;
using UnityEngine;

namespace Froggy.Outils
{
    /// <summary>
    /// Le calcul des outils du Bâtisseur d'arène, sans rien de l'éditeur (pour les tests) : la courbe de l'outil Tracer
    /// (Catmull-Rom qui passe par les points posés, puis des jalons à pas régulier), le placement d'une pièce de tampon
    /// (tournée, en miroir), le tirage pondéré d'un élément, les doublons et la symétrie de l'arène.
    /// </summary>
    public static class CourbeArene
    {
        /// <summary>Le point de la courbe entre p1 (t = 0) et p2 (t = 1), p0 et p3 donnant la tangente.</summary>
        public static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float t2 = t * t, t3 = t2 * t;
            return .5f * (2f * p1 + (p2 - p0) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (3f * p1 - p0 - 3f * p2 + p3) * t3);
        }

        /// <summary>La courbe lisse qui passe par tous les points, en ligne brisée (parSegment points entre deux points posés).</summary>
        public static List<Vector3> Echantillonner(IList<Vector3> points, int parSegment = 16)
        {
            var l = new List<Vector3>();
            if (points == null || points.Count == 0) return l;
            if (points.Count == 1) { l.Add(points[0]); return l; }
            for (int i = 0; i < points.Count - 1; i++)
            {
                // aux bouts, un point fantôme dans le prolongement : la courbe part droit
                var p0 = i > 0 ? points[i - 1] : 2 * points[0] - points[1];
                var p3 = i + 2 < points.Count ? points[i + 2] : 2 * points[i + 1] - points[i];
                for (int k = 0; k < parSegment; k++) l.Add(CatmullRom(p0, points[i], points[i + 1], p3, k / (float)parSegment));
            }
            l.Add(points[points.Count - 1]);
            return l;
        }

        public static float Longueur(IList<Vector3> ligne)
        {
            float d = 0;
            for (int i = 1; i < ligne.Count; i++) d += Vector3.Distance(ligne[i - 1], ligne[i]);
            return d;
        }

        /// <summary>Un point tous les « pas » mètres le long de la ligne (le premier au départ), avec la direction de la courbe.</summary>
        public static List<(Vector3 pos, Vector3 dir)> Jalons(IList<Vector3> ligne, float pas, float depart = 0)
        {
            var l = new List<(Vector3, Vector3)>();
            if (ligne.Count < 2 || pas <= 0.01f) return l;
            float visee = depart, parcouru = 0;
            for (int i = 1; i < ligne.Count; i++)
            {
                var a = ligne[i - 1]; var b = ligne[i]; float seg = Vector3.Distance(a, b);
                if (seg < 1e-5f) continue;
                var dir = (b - a) / seg;
                while (visee <= parcouru + seg + 1e-4f)
                {
                    l.Add((Vector3.Lerp(a, b, Mathf.Clamp01((visee - parcouru) / seg)), dir));
                    visee += pas;
                }
                parcouru += seg;
            }
            return l;
        }

        /// <summary>La distance horizontale d'un point à la ligne.</summary>
        public static float DistanceAPlat(Vector3 p, IList<Vector3> ligne)
        {
            var q = new Vector2(p.x, p.z); float best = float.MaxValue;
            for (int i = 1; i < ligne.Count; i++)
            {
                var a = new Vector2(ligne[i - 1].x, ligne[i - 1].z); var b = new Vector2(ligne[i].x, ligne[i].z);
                var ab = b - a; float t = ab.sqrMagnitude < 1e-8f ? 0 : Mathf.Clamp01(Vector2.Dot(q - a, ab) / ab.sqrMagnitude);
                best = Mathf.Min(best, Vector2.Distance(q, a + ab * t));
            }
            return ligne.Count == 1 ? Vector2.Distance(q, new Vector2(ligne[0].x, ligne[0].z)) : best;
        }

        /// <summary>Une rotation vue dans un miroir (le plan YZ) : le lacet change de sens.</summary>
        public static Quaternion Miroir(Quaternion q) => new Quaternion(q.x, -q.y, -q.z, q.w);

        /// <summary>Où va une pièce de tampon (position et rotation par rapport au centre du tampon) posée en origine, tournée de lacet degrés, en miroir ou non.</summary>
        public static (Vector3 pos, Quaternion rot) Placer(Vector3 local, Quaternion rotLocale, Vector3 origine, float lacet, bool miroir)
        {
            if (miroir) { local.x = -local.x; rotLocale = Miroir(rotLocale); }
            var r = Quaternion.Euler(0, lacet, 0);
            return (origine + r * local, r * rotLocale);
        }

        /// <summary>L'indice tiré selon les poids, pour un tirage entre 0 et 1 (-1 si rien n'a de poids).</summary>
        public static int Tirer(IList<float> poids, float tirage)
        {
            float total = 0;
            foreach (var p in poids) total += Mathf.Max(0, p);
            if (total <= 0) return -1;
            float v = Mathf.Clamp01(tirage) * total;
            for (int i = 0; i < poids.Count; i++)
            {
                float p = Mathf.Max(0, poids[i]);
                if (p > 0 && v <= p) return i;
                v -= p;
            }
            for (int i = poids.Count - 1; i >= 0; i--) if (poids[i] > 0) return i;
            return -1;
        }

        /// <summary>
        /// Les paires d'objets posés l'un sur l'autre (même modèle, à moins de tolerance mètres, presque même rotation) : un
        /// copier-coller oublié. Rangés par cases de la taille de la tolérance, on ne compare que les voisins : 5000 objets
        /// prenaient 1,5 s en comparant toutes les paires (2026-10-29).
        /// </summary>
        public static List<(int a, int b)> Doublons(IList<(string modele, Vector3 pos, Quaternion rot)> objets, float tolerance = .05f)
        {
            var l = new List<(int, int)>();
            var cases = new Dictionary<(int, int, int), List<int>>();
            (int, int, int) Case(Vector3 p) => (Mathf.FloorToInt(p.x / tolerance), Mathf.FloorToInt(p.y / tolerance), Mathf.FloorToInt(p.z / tolerance));
            for (int i = 0; i < objets.Count; i++)
            {
                var (cx, cy, cz) = Case(objets[i].pos);
                for (int dx = -1; dx <= 1; dx++)
                    for (int dy = -1; dy <= 1; dy++)
                        for (int dz = -1; dz <= 1; dz++)
                            if (cases.TryGetValue((cx + dx, cy + dy, cz + dz), out var voisins))
                                foreach (int j in voisins)
                                    if (objets[i].modele == objets[j].modele && Vector3.Distance(objets[i].pos, objets[j].pos) < tolerance
                                        && Quaternion.Angle(objets[i].rot, objets[j].rot) < 2f)
                                        l.Add((j, i));
                if (!cases.TryGetValue((cx, cy, cz), out var ici)) cases[(cx, cy, cz)] = ici = new List<int>();
                ici.Add(i);
            }
            return l;
        }

        // ---------- La symétrie ----------

        public enum Symetrie
        {
            Aucune,
            [InspectorName("Miroir gauche-droite (X)")] MiroirX,
            [InspectorName("Miroir avant-arrière (Z)")] MiroirZ,
            [InspectorName("Deux miroirs (4 quarts)")] MiroirXZ,
            [InspectorName("Demi-tour (2 côtés)")] Rotation2,
            [InspectorName("Quart de tour (4 côtés)")] Rotation4,
        }

        public static int NombreImages(Symetrie s) => s == Symetrie.Aucune ? 1 : s == Symetrie.MiroirXZ || s == Symetrie.Rotation4 ? 4 : 2;

        /// <summary>
        /// Une rotation vue dans le miroir avant-arrière (le plan XY) : l'avant d'un objet suit le miroir (un lacet θ devient
        /// 180° − θ), sans échelle négative, comme Miroir pour le miroir gauche-droite.
        /// </summary>
        public static Quaternion MiroirZ(Quaternion q) => Quaternion.Euler(0, 180, 0) * Miroir(q);

        /// <summary>
        /// L'image de rang 0 à NombreImages − 1 d'un objet posé en pos, tourné de rot (la rotation de pose, par-dessus celle du
        /// préfabriqué), autour du centre. Rang 0 : l'objet lui-même. Les images font le tour de l'arène dans l'ordre (pour
        /// les deux miroirs : gauche-droite, puis les deux, puis avant-arrière).
        /// </summary>
        public static (Vector3 pos, Quaternion rot) Image(Vector3 pos, Quaternion rot, Vector3 centre, Symetrie s, int rang)
        {
            if (rang == 0 || s == Symetrie.Aucune) return (pos, rot);
            var d = pos - centre;
            var mx = (centre + new Vector3(-d.x, d.y, d.z), Miroir(rot));
            var mz = (centre + new Vector3(d.x, d.y, -d.z), MiroirZ(rot));
            switch (s)
            {
                case Symetrie.MiroirX: return mx;
                case Symetrie.MiroirZ: return mz;
                case Symetrie.MiroirXZ: return rang == 1 ? mx : rang == 3 ? mz : (centre + new Vector3(-d.x, d.y, -d.z), MiroirZ(Miroir(rot)));
                default:
                    var r = Quaternion.Euler(0, 360f * rang / NombreImages(s), 0);
                    return (centre + r * d, r * rot);
            }
        }

        /// <summary>Le joueur d'une apparition dans l'image de ce rang : les images se partagent les quatre joueurs (à deux images : 1 → 3).</summary>
        public static int JoueurImage(int joueur, Symetrie s, int rang) => (joueur - 1 + rang * (4 / NombreImages(s))) % 4 + 1;
    }
}
