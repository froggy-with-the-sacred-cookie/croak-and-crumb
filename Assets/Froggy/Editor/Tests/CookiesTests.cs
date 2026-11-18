using System.Collections.Generic;
using Froggy.Arene;
using Froggy.Regles;
using Froggy.Socle;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Froggy.Outils.Tests
{
    /// <summary>
    /// Cookies (P3-01). Les règles seules (ReglesCookies) : ramasser à portée, score = cookies portés, réapparition au
    /// choix, limite au choix, un cookie pour un seul joueur. Puis dans une scène (PartieCookies) : un cookie par repère,
    /// l'événement « cookie ramassé », le ralentissement du porteur par les modificateurs.
    /// </summary>
    public class CookiesTests
    {
        private readonly List<Object> aDetruire = new List<Object>();
        private readonly List<ReglesCookies.Prise> prises = new List<ReglesCookies.Prise>();
        private Scene scene;

        private static readonly Vector3[] TroisPoints = { new Vector3(0f, 0f, 0f), new Vector3(10f, 0f, 0f), new Vector3(20f, 0f, 0f) };

        [TearDown]
        public void Ranger()
        {
            if (scene.IsValid()) EditorSceneManager.ClosePreviewScene(scene);
            foreach (var o in aDetruire) if (o != null) Object.DestroyImmediate(o);
            aDetruire.Clear();
            prises.Clear();
        }

        private static List<(int, Vector3)> Joueurs(params (int, Vector3)[] j) => new List<(int, Vector3)>(j);

        [Test]
        public void TroisCookiesRamassesFontUnScoreDeTrois()
        {
            var regles = new ReglesCookies(TroisPoints, rayonRamassage: 1f, delaiReapparition: 0f, maxPortes: 0);
            foreach (var point in TroisPoints) regles.Pas(1f / 60f, Joueurs((1, point + new Vector3(0.5f, 0f, 0f))), prises);
            Assert.That(regles.Portes(1), Is.EqualTo(3), "le critère de l'issue : 3 cookies, score 3");
            Assert.That(prises.Count, Is.EqualTo(3));
            for (int i = 0; i < 3; i++) Assert.That(regles.Present(i), Is.False, "un cookie ramassé n'est plus là");
        }

        [Test]
        public void HorsDePorteeOnNeRamasseRien()
        {
            var regles = new ReglesCookies(TroisPoints, 1f, 0f, 0);
            regles.Pas(1f / 60f, Joueurs((1, new Vector3(1.01f, 0f, 0f))), prises);
            Assert.That(regles.Portes(1), Is.EqualTo(0));
            Assert.That(regles.Present(0), Is.True);
            regles.Pas(1f / 60f, Joueurs((1, new Vector3(0.99f, 0f, 0f))), prises);
            Assert.That(regles.Portes(1), Is.EqualTo(1), "juste à portée : ramassé");
        }

        [Test]
        public void SansDelaiUnPointNeRedonneJamais()
        {
            var regles = new ReglesCookies(TroisPoints, 1f, delaiReapparition: 0f, maxPortes: 0);
            var surLePoint = Joueurs((1, TroisPoints[0]));
            for (int i = 0; i < 600; i++) regles.Pas(0.1f, surLePoint, prises);
            Assert.That(regles.Portes(1), Is.EqualTo(1), "un seul cookie par point");
        }

        [Test]
        public void AvecDelaiLeCookieRevient()
        {
            var regles = new ReglesCookies(TroisPoints, 1f, delaiReapparition: 5f, maxPortes: 0);
            regles.Pas(0.1f, Joueurs((1, TroisPoints[0])), prises);
            var personne = Joueurs();
            for (int i = 0; i < 49; i++) regles.Pas(0.1f, personne, prises);
            Assert.That(regles.Present(0), Is.False, "4,9 s : pas encore");
            for (int i = 0; i < 2; i++) regles.Pas(0.1f, personne, prises);
            Assert.That(regles.Present(0), Is.True, "5,1 s : revenu");
            Assert.That(regles.Portes(1), Is.EqualTo(1), "revenu ne veut pas dire ramassé");
        }

        [Test]
        public void LaLimiteDeCookiesPortesTient()
        {
            var regles = new ReglesCookies(TroisPoints, 1f, 0f, maxPortes: 2);
            foreach (var point in TroisPoints) regles.Pas(1f / 60f, Joueurs((1, point)), prises);
            Assert.That(regles.Portes(1), Is.EqualTo(2));
            Assert.That(regles.Present(2), Is.True, "le troisième reste pour un autre");
        }

        [Test]
        public void UnCookiePourUnSeulJoueurLePlusProche()
        {
            var regles = new ReglesCookies(TroisPoints, 1f, 0f, 0);
            regles.Pas(1f / 60f, Joueurs((1, new Vector3(0.8f, 0f, 0f)), (2, new Vector3(-0.3f, 0f, 0f))), prises);
            Assert.That(regles.Portes(2), Is.EqualTo(1), "le plus proche");
            Assert.That(regles.Portes(1), Is.EqualTo(0));

            // à égalité : le plus petit numéro, quel que soit l'ordre dans lequel on donne les joueurs
            regles.Pas(1f / 60f, Joueurs((4, new Vector3(10.5f, 0f, 0f)), (3, new Vector3(9.5f, 0f, 0f))), prises);
            Assert.That(regles.Portes(3), Is.EqualTo(1));
            Assert.That(regles.Portes(4), Is.EqualTo(0));
        }

        [Test]
        public void LeRalentissementEstSansEffetParDefaut()
        {
            var reglages = ScriptableObject.CreateInstance<ReglagesCookies>();
            aDetruire.Add(reglages);
            Assert.That(reglages.FacteurVitesse(5), Is.EqualTo(1f), "réglage d'origine : porter des cookies ne ralentit pas");
            reglages.ralentissementParCookie = 0.1f;
            Assert.That(reglages.FacteurVitesse(0), Is.EqualTo(1f));
            Assert.That(reglages.FacteurVitesse(2), Is.EqualTo(0.81f).Within(1e-5f), "−10 % par cookie, multipliés");
        }

        [Test]
        public void DansUneSceneUnCookieParRepereEtLEvenementPart()
        {
            var reglages = ScriptableObject.CreateInstance<ReglagesCookies>();
            aDetruire.Add(reglages);
            reglages.delaiReapparition = 0f;
            reglages.ralentissementParCookie = 0.1f;
            scene = EditorSceneManager.NewPreviewScene();

            var reperes = new List<RepereArene>();
            foreach (var point in TroisPoints) reperes.Add(Repere(point, RepereArene.Sorte.Cookie));
            var froggy = Objet("Froggy", new Vector3(-50f, 0f, 0f));
            var modificateurs = froggy.AddComponent<Modificateurs>();
            var partie = Objet("Partie", Vector3.zero).AddComponent<PartieCookies>();
            partie.Regler(reglages);
            partie.Demarrer(reperes, new[] { froggy });

            Assert.That(partie.Regles.NombrePoints, Is.EqualTo(3));
            Assert.That(partie.transform.childCount, Is.EqualTo(3), "un cookie affiché par repère");
            var porteur = froggy.GetComponent<PorteurCookies>();
            Assert.That(porteur != null, "la partie devait donner un PorteurCookies à Froggy");

            var recus = new List<EvenementPartie>();
            System.Action<EvenementPartie> ecoute = e => recus.Add(e);
            EvenementsPartie.Emis += ecoute;
            try
            {
                partie.Pas(1f / 60f);
                Assert.That(porteur.Nombre, Is.EqualTo(0), "loin des cookies : rien");
                foreach (var point in TroisPoints)
                {
                    froggy.transform.position = point + Vector3.up * reglages.hauteur;
                    partie.Pas(1f / 60f);
                }
            }
            finally { EvenementsPartie.Emis -= ecoute; }

            Assert.That(porteur.Nombre, Is.EqualTo(3), "3 cookies, score 3");
            Assert.That(recus.Count, Is.EqualTo(3));
            Assert.That(recus[0].Type, Is.EqualTo(TypeEvenement.CookieRamasse));
            Assert.That(recus[0].Auteur, Is.SameAs(froggy));
            Assert.That(modificateurs.Vitesse, Is.EqualTo(0.729f).Within(1e-4f), "3 cookies à −10 % : ×0,729");
            for (int i = 0; i < 3; i++) Assert.That(partie.transform.GetChild(i).gameObject.activeSelf, Is.False, "cookie ramassé : caché");
        }

        [Test]
        public void SansJoueurLaPartieTourneQuandMeme()
        {
            scene = EditorSceneManager.NewPreviewScene();
            var reperes = new List<RepereArene> { Repere(Vector3.zero, RepereArene.Sorte.Cookie) };
            var partie = Objet("Partie", Vector3.zero).AddComponent<PartieCookies>();
            var reglages = ScriptableObject.CreateInstance<ReglagesCookies>();
            aDetruire.Add(reglages);
            partie.Regler(reglages);
            partie.Demarrer(reperes, new GameObject[0]);
            Assert.That(partie.Regles.NombrePoints, Is.EqualTo(1));
            partie.Pas(1f / 60f);   // sans joueur : ne plante pas
            Assert.That(partie.Regles.Present(0), Is.True);
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
