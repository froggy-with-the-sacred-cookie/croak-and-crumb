using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Froggy.Outils.Tests
{
    /// <summary>
    /// Le calcul du Bâtisseur d'arène : la courbe de Tracer passe par les points posés et donne des jalons réguliers, une
    /// pièce de tampon se place tournée et en miroir, le tirage suit les poids, les objets posés deux fois se repèrent, la
    /// symétrie de l'arène place et numérote les images, l'analyse des trajets saute les trous à sa portée et nage sinon.
    /// </summary>
    public class BatisseurAreneTests
    {
        private static void Proche(Vector3 attendu, Vector3 vu, float tol = 1e-3f) =>
            Assert.Less(Vector3.Distance(attendu, vu), tol, $"attendu {attendu}, vu {vu}");

        [Test]
        public void LaCourbePasseParLesPointsPoses()
        {
            var pts = new List<Vector3> { new Vector3(0, 0, 0), new Vector3(4, 0, 2), new Vector3(8, 1, -1), new Vector3(12, 0, 3) };
            var ligne = CourbeArene.Echantillonner(pts, 10);
            Assert.AreEqual(3 * 10 + 1, ligne.Count);
            for (int i = 0; i < pts.Count; i++) Proche(pts[i], ligne[i * 10]);
        }

        [Test]
        public void LesJalonsSontRegulierEtSuiventLaCourbe()
        {
            var ligne = CourbeArene.Echantillonner(new List<Vector3> { Vector3.zero, new Vector3(0, 0, 10) });
            var jalons = CourbeArene.Jalons(ligne, 2.5f);
            Assert.AreEqual(5, jalons.Count);
            for (int i = 0; i < 5; i++)
            {
                Proche(new Vector3(0, 0, 2.5f * i), jalons[i].pos);
                Proche(Vector3.forward, jalons[i].dir);
            }
            // un mur : la première pièce centrée à un demi-pas du départ
            var murs = CourbeArene.Jalons(ligne, 4, 2);
            Assert.AreEqual(3, murs.Count);
            Proche(new Vector3(0, 0, 2), murs[0].pos);
        }

        [Test]
        public void LaDistanceAPlatIgnoreLaHauteur()
        {
            var ligne = new List<Vector3> { Vector3.zero, new Vector3(10, 0, 0) };
            Assert.AreEqual(3, CourbeArene.DistanceAPlat(new Vector3(5, 7, 3), ligne), 1e-4f);
            Assert.AreEqual(2, CourbeArene.DistanceAPlat(new Vector3(12, 0, 0), ligne), 1e-4f);
        }

        [Test]
        public void UnePieceDeTamponTourneAvecLui()
        {
            var (pos, rot) = CourbeArene.Placer(new Vector3(1, 0, 0), Quaternion.identity, new Vector3(10, 2, 10), 90, false);
            Proche(new Vector3(10, 2, 9), pos);
            Assert.AreEqual(90, rot.eulerAngles.y, .01f);
        }

        [Test]
        public void LeMiroirInverseLeCoteEtLeLacet()
        {
            var rot = Quaternion.Euler(0, 30, 0);
            var (pos, r) = CourbeArene.Placer(new Vector3(2, 1, 3), rot, Vector3.zero, 0, true);
            Proche(new Vector3(-2, 1, 3), pos);
            Assert.AreEqual(330, r.eulerAngles.y, .01f);
            Assert.Less(Quaternion.Angle(rot, CourbeArene.Miroir(CourbeArene.Miroir(rot))), .01f);
        }

        [Test]
        public void LeTirageSuitLesPoids()
        {
            var poids = new List<float> { 1, 0, 3 };
            Assert.AreEqual(0, CourbeArene.Tirer(poids, .1f));
            Assert.AreEqual(2, CourbeArene.Tirer(poids, .5f));
            Assert.AreEqual(2, CourbeArene.Tirer(poids, 1));
            Assert.AreEqual(-1, CourbeArene.Tirer(new List<float> { 0, 0 }, .5f));
        }

        [Test]
        public void LesObjetsPosesDeuxFoisSeRepèrent()
        {
            var objets = new List<(string, Vector3, Quaternion)>
            {
                ("Lanterne", Vector3.zero, Quaternion.identity),
                ("Lanterne", new Vector3(.01f, 0, 0), Quaternion.identity),
                ("Rocher", Vector3.zero, Quaternion.identity),
                ("Lanterne", new Vector3(3, 0, 0), Quaternion.identity),
            };
            var d = CourbeArene.Doublons(objets);
            Assert.AreEqual(1, d.Count);
            Assert.AreEqual((0, 1), d[0]);
        }

        [Test]
        public void LaRechercheRapideDesDoublonsTrouveLesMemesPaires()
        {
            var rnd = new System.Random(3); var objets = new List<(string, Vector3, Quaternion)>();
            for (int i = 0; i < 300; i++)
            {
                var p = new Vector3((float)rnd.NextDouble() * 20, 0, (float)rnd.NextDouble() * 20);
                objets.Add(("M" + rnd.Next(3), p, Quaternion.identity));
                if (i % 7 == 0) objets.Add(("M" + rnd.Next(3), p + new Vector3(.02f, 0, -.01f), Quaternion.identity));
            }
            var attendu = new HashSet<(int, int)>();
            for (int i = 0; i < objets.Count; i++)
                for (int j = i + 1; j < objets.Count; j++)
                    if (objets[i].Item1 == objets[j].Item1 && Vector3.Distance(objets[i].Item2, objets[j].Item2) < .05f) attendu.Add((i, j));
            CollectionAssert.AreEquivalent(attendu, CourbeArene.Doublons(objets));
            Assert.Greater(attendu.Count, 0);
        }

        [Test]
        public void LesMiroirsGardentLAvantDesObjetsDansLeMiroir()
        {
            var s = CourbeArene.Symetrie.MiroirZ;
            var (pos, rot) = CourbeArene.Image(new Vector3(2, 0, 3), Quaternion.Euler(0, 90, 0), Vector3.zero, s, 1);
            Proche(new Vector3(2, 0, -3), pos);
            Proche(Vector3.right, rot * Vector3.forward);        // regardait +X, regarde toujours +X
            (pos, rot) = CourbeArene.Image(new Vector3(2, 0, 3), Quaternion.Euler(0, 0, 0), Vector3.zero, s, 1);
            Proche(Vector3.back, rot * Vector3.forward);         // regardait +Z, regarde -Z
            (pos, rot) = CourbeArene.Image(new Vector3(2, 0, 3), Quaternion.Euler(0, 30, 0), new Vector3(1, 0, 0), CourbeArene.Symetrie.MiroirX, 1);
            Proche(new Vector3(0, 0, 3), pos);
            Assert.AreEqual(330, rot.eulerAngles.y, .01f);
            (pos, rot) = CourbeArene.Image(new Vector3(2, 0, 3), Quaternion.Euler(0, 30, 0), Vector3.zero, CourbeArene.Symetrie.MiroirXZ, 2);
            Proche(new Vector3(-2, 0, -3), pos);
            Assert.AreEqual(210, rot.eulerAngles.y, .01f);    // les deux miroirs : un demi-tour
        }

        [Test]
        public void LeQuartDeTourFaitLeTourDeLArene()
        {
            var s = CourbeArene.Symetrie.Rotation4;
            Assert.AreEqual(4, CourbeArene.NombreImages(s));
            var (pos, rot) = CourbeArene.Image(new Vector3(2, 0, 0), Quaternion.identity, Vector3.zero, s, 1);
            Proche(new Vector3(0, 0, -2), pos);
            Assert.AreEqual(90, rot.eulerAngles.y, .01f);
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 }, new[] { 0, 1, 2, 3 }.Select(k => CourbeArene.JoueurImage(1, s, k)).ToArray());
            Assert.AreEqual(3, CourbeArene.JoueurImage(1, CourbeArene.Symetrie.MiroirX, 1));
            Assert.AreEqual(4, CourbeArene.JoueurImage(2, CourbeArene.Symetrie.Rotation2, 1));
        }

        [Test]
        public void LeTempsDeVolSuitLaHauteurDuSaut()
        {
            Assert.AreEqual(2 * Mathf.Sqrt(2 * 1.5f / 9.81f), TrajetsArene.TempsDeVol(1.5f, 0, 9.81f), 1e-3f);
            Assert.Greater(TrajetsArene.TempsDeVol(1.5f, -1, 9.81f), TrajetsArene.TempsDeVol(1.5f, 0, 9.81f));
            Assert.AreEqual(-1, TrajetsArene.TempsDeVol(1.5f, 2, 9.81f));
        }

        /// <summary>Une rangée de cases d'un mètre : sol, puis un trou ou de l'eau, puis sol.</summary>
        private static GrilleTrajets Rangee(string cases)
        {
            var g = new GrilleTrajets(Vector3.zero, 1, cases.Length, 1);
            for (int i = 0; i < cases.Length; i++)
            {
                g.genre[i] = cases[i] == '#' ? GrilleTrajets.Sol : cases[i] == '~' ? GrilleTrajets.Eau : GrilleTrajets.Vide;
                g.hauteur[i] = cases[i] == '~' ? -.1f : 0;
                g.objet[i] = cases[i] != '#' ? -1 : i < cases.IndexOf('.') || cases.IndexOf('.') < 0 && i < cases.IndexOf('~') ? 0 : 1;
            }
            g.objets.Add(null); g.objets.Add(null);
            return g;
        }

        private static TrajetsArene.Mouvement Froggy(float saut) =>
            new TrajetsArene.Mouvement { course = 6, nage = 3, hauteurSaut = saut, gravite = 9.81f, marche = .35f, sortie = .6f };

        [Test]
        public void UnTrouSeSauteSIlEstALaPortee()
        {
            var g = Rangee("####...#####");
            var m = Froggy(1.5f);
            TrajetsArene.Sauts(g, m);
            float t = TrajetsArene.Temps(g, 0, m)[11];
            // le plus vite : 1 m de course, un saut de 6 m (la portée est de 6,6 m) par-dessus le bord et le trou, 4 m de course
            Assert.AreEqual(1 / 6f + TrajetsArene.TempsDeVol(1.5f, 0, 9.81f) + .05f + 4 / 6f, t, .01f);
            Assert.IsTrue(g.liens.ContainsKey((0, 1)));
            Assert.AreEqual(6 * TrajetsArene.TempsDeVol(1.5f, 0, 9.81f) - 4, g.liens[(0, 1)].marge, .01f);

            var petit = Froggy(.2f);   // portée 2,4 m : le trou de 4 m ne passe plus
            TrajetsArene.Sauts(g, petit);
            Assert.IsTrue(float.IsInfinity(TrajetsArene.Temps(g, 0, petit)[11]));
        }

        [Test]
        public void SansSautOnTraverseALaNage()
        {
            var g = Rangee("#~~~#");
            var petit = Froggy(.2f);
            TrajetsArene.Sauts(g, petit);
            // trois cases d'eau à 3 m/s, puis la berge (sortie de l'eau : 0,3 s)
            Assert.AreEqual(3 / 3f + 1 / 6f + .3f, TrajetsArene.Temps(g, 0, petit)[4], .01f);
            var m = Froggy(1.5f);
            TrajetsArene.Sauts(g, m);
            Assert.AreEqual(TrajetsArene.TempsDeVol(1.5f, .0f, 9.81f) + .05f, TrajetsArene.Temps(g, 0, m)[4], .01f);   // le saut est plus court
        }

        [Test]
        public void UnSautSerreQuOnPeutContournerNeSAffichePas()
        {
            // trois rangées d'eau ; A (au milieu, à gauche) et C (au milieu, à droite) à 6 m : saut serré (portée 6,3 m)
            GrilleTrajets Bassin(bool avecB)
            {
                var g = new GrilleTrajets(Vector3.zero, 1, 9, 3);
                for (int i = 0; i < g.Taille; i++) g.genre[i] = GrilleTrajets.Eau;
                void Poser(int x, int z, int o) { int i = z * 9 + x; g.genre[i] = GrilleTrajets.Sol; g.objet[i] = o; }
                Poser(0, 1, 0); Poser(1, 1, 0); Poser(7, 1, 2); Poser(8, 1, 2);
                if (avecB) { Poser(4, 0, 1); Poser(4, 2, 1); }   // B, en haut et en bas : on peut passer par lui
                for (int k = 0; k < 3; k++) g.objets.Add(null);
                return g;
            }
            var m = Froggy(1.5f); m.course = 5.7f;
            var seul = Bassin(false);
            TrajetsArene.Sauts(seul, m);
            Assert.IsTrue(seul.liens.ContainsKey((0, 2)));
            Assert.Less(seul.liens[(0, 2)].marge, .5f);
            var avecB = Bassin(true);
            TrajetsArene.Sauts(avecB, m);
            Assert.GreaterOrEqual(avecB.liens[(0, 1)].marge, .5f);
            Assert.GreaterOrEqual(avecB.liens[(1, 2)].marge, .5f);
            Assert.IsFalse(avecB.liens.ContainsKey((0, 2)), "le saut A → C, serré, se contourne par B");
        }

        [Test]
        public void UneBoiteDuKitGrisALaBonneTailleEtSesFacesVersLExterieur()
        {
            var m = KitGris.Boite(new Vector3(2, 1, 4), 0);
            Proche(new Vector3(2, 1, 4), m.bounds.size);
            Assert.AreEqual(0, m.bounds.min.y, 1e-4f);
            var v = m.vertices; var n = m.normals; var c = m.bounds.center;
            for (int i = 0; i < v.Length; i++)
                Assert.Greater(Vector3.Dot(n[i], v[i] - c), 0, $"la normale du sommet {i} rentre dans la boîte");
            // le damier : les UV sont en mètres (le dessus fait 2 × 4)
            Assert.AreEqual(4, m.uv.Max(u => u.y), 1e-4f);
            Object.DestroyImmediate(m);
        }
    }
}
