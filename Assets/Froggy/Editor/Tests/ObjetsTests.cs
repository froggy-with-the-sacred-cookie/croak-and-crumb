using System.Collections.Generic;
using Froggy.Arene;
using Froggy.Objets;
using Froggy.Socle;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Froggy.Outils.Tests
{
    /// <summary>
    /// Objets et boîtes mystère (P4-01). Les règles seules (ReglesObjets) : prendre une boîte à portée donne un objet,
    /// un seul emplacement, la boîte revient après le délai, le tirage suit les poids et la graine, l'utilisation vide
    /// l'emplacement et lance la recharge. Puis dans une scène (PartieObjets) : une boîte par repère, la fiche dans
    /// PorteurObjet, l'événement « objet utilisé ».
    /// </summary>
    public class ObjetsTests
    {
        private const float Dt = 1f / 60f;

        private readonly List<Object> aDetruire = new List<Object>();
        private readonly List<ReglesObjets.Prise> prises = new List<ReglesObjets.Prise>();
        private Scene scene;

        private static readonly Vector3[] DeuxPoints = { new Vector3(0f, 0f, 0f), new Vector3(10f, 0f, 0f) };
        private static readonly float[] TroisObjets = { 1f, 1f, 1f };
        private static readonly float[] SansRecharge = { 0f, 0f, 0f };

        [TearDown]
        public void Ranger()
        {
            if (scene.IsValid()) EditorSceneManager.ClosePreviewScene(scene);
            foreach (var o in aDetruire) if (o != null) Object.DestroyImmediate(o);
            aDetruire.Clear();
            prises.Clear();
        }

        private static List<(int, Vector3)> Joueurs(params (int, Vector3)[] j) => new List<(int, Vector3)>(j);

        private static ReglesObjets Regles(float delai = 0f, float[] poids = null, float[] recharges = null, int graine = 7)
            => new ReglesObjets(DeuxPoints, poids ?? TroisObjets, recharges ?? SansRecharge, 1f, delai, graine);

        [Test]
        public void PrendreUneBoiteDonneUnObjet()
        {
            var regles = Regles();
            Assert.That(regles.Objet(1), Is.EqualTo(ReglesObjets.Aucun), "au départ, l'emplacement est vide");
            regles.Pas(Dt, Joueurs((1, new Vector3(1.01f, 0f, 0f))), prises);
            Assert.That(regles.Objet(1), Is.EqualTo(ReglesObjets.Aucun), "hors de portée : rien");
            regles.Pas(Dt, Joueurs((1, new Vector3(0.99f, 0f, 0f))), prises);
            Assert.That(regles.Objet(1), Is.InRange(0, 2), "à portée : un des trois objets");
            Assert.That(regles.Present(0), Is.False, "la boîte prise n'est plus là");
            Assert.That(regles.Present(1), Is.True);
            Assert.That(prises.Count, Is.EqualTo(1));
            Assert.That(prises[0].Point, Is.EqualTo(0));
            Assert.That(prises[0].Joueur, Is.EqualTo(1));
            Assert.That(prises[0].Objet, Is.EqualTo(regles.Objet(1)));
        }

        [Test]
        public void UnSeulEmplacementLeJoueurLaisseLaBoite()
        {
            var regles = Regles();
            regles.Pas(Dt, Joueurs((1, DeuxPoints[0])), prises);
            int objet = regles.Objet(1);
            regles.Pas(Dt, Joueurs((1, DeuxPoints[1])), prises);
            Assert.That(regles.Present(1), Is.True, "il a déjà un objet : la boîte reste");
            Assert.That(regles.Objet(1), Is.EqualTo(objet), "et son objet ne change pas");
            Assert.That(regles.Utiliser(1), Is.EqualTo(objet));
            regles.Pas(Dt, Joueurs((1, DeuxPoints[1])), prises);
            Assert.That(regles.Present(1), Is.False, "emplacement vide : il prend la boîte");
        }

        [Test]
        public void LaBoiteRevientApresLeDelai()
        {
            var regles = Regles(delai: 5f);
            regles.Pas(0.1f, Joueurs((1, DeuxPoints[0])), prises);
            var personne = Joueurs();
            for (int i = 0; i < 49; i++) regles.Pas(0.1f, personne, prises);
            Assert.That(regles.Present(0), Is.False, "4,9 s : pas encore");
            for (int i = 0; i < 2; i++) regles.Pas(0.1f, personne, prises);
            Assert.That(regles.Present(0), Is.True, "5,1 s : revenue");
        }

        [Test]
        public void SansDelaiLaBoiteNeRevientJamais()
        {
            var regles = Regles(delai: 0f);
            regles.Pas(0.1f, Joueurs((1, DeuxPoints[0])), prises);
            for (int i = 0; i < 600; i++) regles.Pas(0.1f, Joueurs(), prises);
            Assert.That(regles.Present(0), Is.False);
        }

        [Test]
        public void LaBoiteVaAuPlusProcheEtAUnSeulJoueur()
        {
            var regles = Regles();
            regles.Pas(Dt, Joueurs((1, new Vector3(0.8f, 0f, 0f)), (2, new Vector3(0.3f, 0f, 0f))), prises);
            Assert.That(regles.Objet(2), Is.Not.EqualTo(ReglesObjets.Aucun), "le plus proche la prend");
            Assert.That(regles.Objet(1), Is.EqualTo(ReglesObjets.Aucun));

            regles = Regles();
            regles.Pas(Dt, Joueurs((2, new Vector3(0.5f, 0f, 0f)), (1, new Vector3(-0.5f, 0f, 0f))), prises);
            Assert.That(regles.Objet(1), Is.Not.EqualTo(ReglesObjets.Aucun), "à égalité : le plus petit numéro");
            Assert.That(regles.Objet(2), Is.EqualTo(ReglesObjets.Aucun));
        }

        [Test]
        public void LeTirageSuitLesPoids()
        {
            var regles = Regles(delai: 0.01f, poids: new[] { 0f, 1f, 3f });
            var compte = new int[3];
            for (int i = 0; i < 4000; i++)
            {
                regles.Pas(1f, Joueurs((1, DeuxPoints[0])), prises);
                int objet = regles.Utiliser(1);
                Assert.That(objet, Is.Not.EqualTo(ReglesObjets.Aucun));
                compte[objet]++;
            }
            Assert.That(compte[0], Is.EqualTo(0), "un poids de 0 ne sort jamais");
            Assert.That(compte[2] / (float)compte[1], Is.EqualTo(3f).Within(0.4f), "un poids de 3 sort trois fois plus qu'un poids de 1");
        }

        [Test]
        public void LaMemeGraineDonneLaMemeSuite()
        {
            List<int> Suite(int graine)
            {
                var regles = Regles(delai: 0.01f, graine: graine);
                var suite = new List<int>();
                for (int i = 0; i < 40; i++)
                {
                    regles.Pas(1f, Joueurs((1, DeuxPoints[0])), prises);
                    suite.Add(regles.Utiliser(1));
                }
                return suite;
            }
            Assert.That(Suite(42), Is.EqualTo(Suite(42)), "même graine : même suite, chez l'hôte comme en rejeu");
            Assert.That(Suite(42), Is.Not.EqualTo(Suite(43)));
            Assert.That(Suite(42), Has.Some.Not.EqualTo(Suite(42)[0]), "et la suite n'est pas toujours le même objet");
        }

        [Test]
        public void SansObjetADonnerLesBoitesRestent()
        {
            var regles = Regles(poids: new[] { 0f, 0f, 0f });
            regles.Pas(Dt, Joueurs((1, DeuxPoints[0])), prises);
            Assert.That(regles.Present(0), Is.True);
            Assert.That(regles.Objet(1), Is.EqualTo(ReglesObjets.Aucun));

            regles = new ReglesObjets(DeuxPoints, new float[0], new float[0], 1f, 0f, 1);
            regles.Pas(Dt, Joueurs((1, DeuxPoints[0])), prises);   // aucune fiche : ne plante pas
            Assert.That(regles.Present(0), Is.True);
        }

        [Test]
        public void UtiliserVideLEmplacementEtLanceLaRecharge()
        {
            var regles = Regles(recharges: new[] { 2f, 2f, 2f });
            Assert.That(regles.Utiliser(1), Is.EqualTo(ReglesObjets.Aucun), "sans objet : rien à utiliser");
            regles.Pas(Dt, Joueurs((1, DeuxPoints[0])), prises);
            int objet = regles.Objet(1);
            Assert.That(regles.Utiliser(1), Is.EqualTo(objet));
            Assert.That(regles.Objet(1), Is.EqualTo(ReglesObjets.Aucun), "utilisé : l'emplacement est vide");
            Assert.That(regles.Recharge(1), Is.EqualTo(2f));

            // en recharge, il peut reprendre une boîte mais pas utiliser l'objet
            regles.Pas(0.5f, Joueurs((1, DeuxPoints[1])), prises);
            Assert.That(regles.Objet(1), Is.Not.EqualTo(ReglesObjets.Aucun));
            Assert.That(regles.Recharge(1), Is.EqualTo(1.5f).Within(1e-5f));
            Assert.That(regles.Utiliser(1), Is.EqualTo(ReglesObjets.Aucun), "encore 1,5 s de recharge");
            regles.Pas(1.6f, Joueurs(), prises);
            Assert.That(regles.Recharge(1), Is.EqualTo(0f));
            Assert.That(regles.Utiliser(1), Is.Not.EqualTo(ReglesObjets.Aucun), "recharge finie : il l'utilise");
        }

        [Test]
        public void ViderRemetUnJoueurAZero()
        {
            var regles = Regles(recharges: new[] { 2f, 2f, 2f });
            regles.Pas(Dt, Joueurs((1, DeuxPoints[0])), prises);
            regles.Utiliser(1);
            regles.Pas(Dt, Joueurs((1, DeuxPoints[1])), prises);
            regles.Vider(1);
            Assert.That(regles.Objet(1), Is.EqualTo(ReglesObjets.Aucun));
            Assert.That(regles.Recharge(1), Is.EqualTo(0f));
        }

        [Test]
        public void DansUneSceneOnPrendOnUtiliseEtLaBoiteRevient()
        {
            // le critère de l'issue : on prend une boîte, on reçoit un objet de test, on l'utilise, la boîte revient
            var fiche = Fiche("Objet de test", recharge: 1f);
            var reglages = ScriptableObject.CreateInstance<ReglagesObjets>();
            aDetruire.Add(reglages);
            reglages.objets.Add(null);   // une case vide dans la liste ne casse rien
            reglages.objets.Add(fiche);
            reglages.delaiReapparition = 3f;
            reglages.graine = 5;
            scene = EditorSceneManager.NewPreviewScene();

            var reperes = new List<RepereArene>();
            foreach (var point in DeuxPoints) reperes.Add(Repere(point, RepereArene.Sorte.Boite));
            var froggy = Objet("Froggy", new Vector3(-50f, 0f, 0f));
            var partie = Objet("Partie", Vector3.zero).AddComponent<PartieObjets>();
            partie.Regler(reglages);
            partie.Demarrer(reperes, new[] { froggy });

            Assert.That(partie.Regles.NombrePoints, Is.EqualTo(2));
            Assert.That(partie.transform.childCount, Is.EqualTo(2), "une boîte affichée par repère");
            var porteur = froggy.GetComponent<PorteurObjet>();
            Assert.That(porteur != null, "la partie devait donner un PorteurObjet à Froggy");
            Assert.That(partie.Local, Is.SameAs(porteur));

            int changements = 0;
            FicheObjet declenche = null;
            porteur.Change += () => changements++;
            porteur.Utilise += f => declenche = f;
            var recus = new List<EvenementPartie>();
            System.Action<EvenementPartie> ecoute = e => recus.Add(e);
            EvenementsPartie.Emis += ecoute;
            try
            {
                partie.Pas(Dt);
                Assert.That(porteur.Fiche == null, "loin des boîtes : rien");
                Assert.That(partie.Utiliser(porteur) == null, "sans objet : rien à utiliser");

                froggy.transform.position = DeuxPoints[0] + Vector3.up * reglages.hauteur;
                partie.Pas(Dt);
                Assert.That(porteur.Fiche, Is.SameAs(fiche), "on reçoit l'objet de test");
                Assert.That(porteur.Pret, Is.True);
                Assert.That(partie.transform.GetChild(0).gameObject.activeSelf, Is.False, "boîte prise : cachée");
                Assert.That(recus.Count, Is.EqualTo(0), "prendre une boîte n'est pas utiliser un objet");

                Assert.That(partie.Utiliser(porteur), Is.SameAs(fiche));
                Assert.That(porteur.Fiche == null, "utilisé : l'emplacement est vide");
                Assert.That(porteur.Recharge, Is.EqualTo(1f));
                Assert.That(porteur.DernierUtilise, Is.SameAs(fiche));
                Assert.That(declenche, Is.SameAs(fiche), "l'effet de l'objet est prévenu");

                froggy.transform.position = new Vector3(-50f, 0f, 0f);
                for (int i = 0; i < 29; i++) partie.Pas(0.1f);
                Assert.That(partie.transform.GetChild(0).gameObject.activeSelf, Is.False, "2,9 s : pas encore");
                for (int i = 0; i < 2; i++) partie.Pas(0.1f);
                Assert.That(partie.transform.GetChild(0).gameObject.activeSelf, Is.True, "3,1 s : la boîte est revenue");
                Assert.That(porteur.Recharge, Is.EqualTo(0f));
            }
            finally { EvenementsPartie.Emis -= ecoute; }

            Assert.That(recus.Count, Is.EqualTo(1));
            Assert.That(recus[0].Type, Is.EqualTo(TypeEvenement.ObjetUtilise));
            Assert.That(recus[0].Auteur, Is.SameAs(froggy));
            Assert.That(changements, Is.EqualTo(2), "prévenu quand l'objet arrive et quand il part, pas à chaque pas");
        }

        [Test]
        public void SansJoueurNiFicheLaPartieTourneQuandMeme()
        {
            scene = EditorSceneManager.NewPreviewScene();
            var reperes = new List<RepereArene> { Repere(Vector3.zero, RepereArene.Sorte.Boite) };
            var partie = Objet("Partie", Vector3.zero).AddComponent<PartieObjets>();
            var reglages = ScriptableObject.CreateInstance<ReglagesObjets>();
            aDetruire.Add(reglages);
            partie.Regler(reglages);
            partie.Demarrer(reperes, new GameObject[0]);
            Assert.That(partie.Regles.NombrePoints, Is.EqualTo(1));
            partie.Pas(Dt);   // sans joueur ni objet à donner : ne plante pas
            Assert.That(partie.Regles.Present(0), Is.True);
            Assert.That(partie.Local == null);
        }

        private FicheObjet Fiche(string nom, float recharge)
        {
            var fiche = ScriptableObject.CreateInstance<FicheObjet>();
            aDetruire.Add(fiche);
            fiche.nom = nom;
            fiche.recharge = recharge;
            return fiche;
        }

        private GameObject Objet(string nom, Vector3 position)
        {
            var go = new GameObject(nom);
            SceneManager.MoveGameObjectToScene(go, scene);
            go.transform.position = position;
            return go;
        }

        private RepereArene Repere(Vector3 position, RepereArene.Sorte sorte)
        {
            var repere = Objet("Repère", position).AddComponent<RepereArene>();
            repere.sorte = sorte;
            return repere;
        }
    }
}
