using System.Collections.Generic;
using System.IO;
using Froggy.Interface;
using Froggy.Reseau;
using NUnit.Framework;
using UnityEditor;

namespace Froggy.Outils.Tests
{
    /// <summary>
    /// Menus (P6-03), sans scène ni réseau : les places et les textes de la salle d'attente, la saisie du code de salon
    /// au clavier et à la manette, les réglages du joueur (dans un fichier de test, jamais le vrai), et la scène Menu
    /// qui porte le salon, en tête du build.
    /// </summary>
    public class MenusTests
    {
        private static MembreSalon Membre(int clientId, int numero, bool hote = false, bool pret = false, int couleur = 0)
            => new MembreSalon { ClientId = clientId, Numero = numero, Hote = hote, Pret = pret, Couleur = couleur };

        // ---------------------------------------------------------------- Salle d'attente

        [Test]
        public void SansJoueurLesQuatrePlacesSontLibres()
        {
            PlaceSalle[] places = ContenuSalle.Places(new List<MembreSalon>(), null);
            Assert.That(places.Length, Is.EqualTo(4));
            foreach (PlaceSalle p in places)
            {
                Assert.That(p.Etat, Is.EqualTo(EtatPlace.Libre));
                Assert.That(p.Nom, Is.EqualTo("Place libre"));
                Assert.That(p.TexteEtat, Is.Empty);
            }
        }

        [Test]
        public void LesPlacesSuiventLesNumerosEtMontrentQuiJeSuis()
        {
            var membres = new List<MembreSalon>
            {
                Membre(7, 3, pret: true, couleur: 2),
                Membre(0, 1, hote: true, couleur: 0),
                Membre(5, 2, couleur: 4)
            };
            PlaceSalle[] places = ContenuSalle.Places(membres, 7);

            Assert.That(places[0].Etat, Is.EqualTo(EtatPlace.Hote));
            Assert.That(places[0].TexteEtat, Is.EqualTo("Hôte"));
            Assert.That(places[1].Etat, Is.EqualTo(EtatPlace.EnAttente));
            Assert.That(places[1].TexteEtat, Is.EqualTo("En attente"));
            Assert.That(places[1].Couleur, Is.EqualTo(4));
            Assert.That(places[2].Etat, Is.EqualTo(EtatPlace.Pret));
            Assert.That(places[2].Nom, Is.EqualTo("Joueur 3 (vous)"));
            Assert.That(places[2].Vous, Is.True);
            Assert.That(places[0].Vous || places[1].Vous || places[3].Vous, Is.False);
            Assert.That(places[3].Etat, Is.EqualTo(EtatPlace.Libre));
        }

        [Test]
        public void LeBoutonPrincipalDependDuRole()
        {
            Assert.That(ContenuSalle.BoutonPrincipal(true, false, PhaseSalon.Salon), Is.EqualTo("Lancer la partie"));
            Assert.That(ContenuSalle.BoutonPrincipal(false, false, PhaseSalon.Salon), Is.EqualTo("Se mettre prêt"));
            Assert.That(ContenuSalle.BoutonPrincipal(false, true, PhaseSalon.Salon), Is.EqualTo("Je ne suis plus prêt"));
            Assert.That(ContenuSalle.BoutonPrincipal(true, false, PhaseSalon.EnPartie), Is.EqualTo("Partie en cours…"));
        }

        [Test]
        public void LAideDitCeQuOnAttend()
        {
            var seul = new List<MembreSalon> { Membre(0, 1, hote: true) };
            Assert.That(ContenuSalle.Aide(seul, true, false, PhaseSalon.Salon), Does.Contain("au moins 2 joueurs"));

            var troisNonPrets = new List<MembreSalon>
                { Membre(0, 1, hote: true), Membre(4, 4), Membre(2, 2), Membre(3, 3, pret: true) };
            Assert.That(ContenuSalle.Aide(troisNonPrets, true, false, PhaseSalon.Salon),
                Is.EqualTo("En attente des joueurs 2 et 4."));

            var unNonPret = new List<MembreSalon> { Membre(0, 1, hote: true), Membre(2, 2) };
            Assert.That(ContenuSalle.Aide(unNonPret, true, false, PhaseSalon.Salon),
                Is.EqualTo("En attente du joueur 2."));

            var tousPrets = new List<MembreSalon> { Membre(0, 1, hote: true), Membre(2, 2, pret: true) };
            Assert.That(ContenuSalle.Aide(tousPrets, true, false, PhaseSalon.Salon), Is.EqualTo("Tout le monde est prêt !"));
            Assert.That(ContenuSalle.Aide(tousPrets, false, false, PhaseSalon.Salon), Does.Contain("Mettez-vous prêt"));
        }

        [Test]
        public void LeCompteDesJoueurs()
        {
            Assert.That(ContenuSalle.Compte(1), Is.EqualTo("1/4 joueur"));
            Assert.That(ContenuSalle.Compte(3), Is.EqualTo("3/4 joueurs"));
        }

        // ---------------------------------------------------------------- Saisie du code

        [Test]
        public void OnTapeLeCodeDansLOrdre()
        {
            var code = new CodeEnSaisie();
            foreach (char c in "krbf") Assert.That(code.Taper(c), Is.True);
            Assert.That(code.Code, Is.EqualTo("KRBF"));
            Assert.That(code.Complet, Is.True);
        }

        [Test]
        public void LesLettresAmbiguesEtLesChiffresNEntrentPas()
        {
            var code = new CodeEnSaisie();
            foreach (char c in "OI1-é ") Assert.That(code.Taper(c), Is.False, $"« {c} » est entré");
            Assert.That(code.Code, Is.Empty);
            Assert.That(code.Complet, Is.False);
        }

        [Test]
        public void RetourArriereEfface()
        {
            var code = new CodeEnSaisie();
            code.Taper('K');
            code.Taper('R');
            code.Effacer();
            Assert.That(code.Code, Is.EqualTo("K"));
            Assert.That(code.Curseur, Is.EqualTo(1));

            code.Coller("KRBF");
            code.Effacer();
            Assert.That(code.Code, Is.EqualTo("KRB"), "sur un code plein, la dernière lettre s'efface");
            code.Effacer();
            Assert.That(code.Code, Is.EqualTo("KR"));
        }

        [Test]
        public void ALaManetteLaLettreDefileEnBoucleSansOniI()
        {
            var code = new CodeEnSaisie();
            code.Defiler(+1);
            Assert.That(code[0], Is.EqualTo('A'));
            code.Defiler(-1);
            Assert.That(code[0], Is.EqualTo('Z'));

            code.Vider();
            code.Taper('H');
            code.Deplacer(-1);
            code.Defiler(+1);
            Assert.That(code[0], Is.EqualTo('J'), "I est sauté");
        }

        [Test]
        public void OnSortDuChampParLesBords()
        {
            var code = new CodeEnSaisie();
            Assert.That(code.Deplacer(-1), Is.False);
            for (int i = 0; i < 3; i++) Assert.That(code.Deplacer(+1), Is.True);
            Assert.That(code.Curseur, Is.EqualTo(3));
            Assert.That(code.Deplacer(+1), Is.False);
        }

        [Test]
        public void ColleUnCodeTapeNImporteComment()
        {
            var code = new CodeEnSaisie();
            code.Coller("kr-bf");
            Assert.That(code.Code, Is.EqualTo("KRBF"));
            code.Coller("k r o b f x");
            Assert.That(code.Code, Is.EqualTo("KRBF"), "O sauté, X en trop ignoré");
        }

        // ---------------------------------------------------------------- Réglages du joueur

        private string fichierTest;

        [SetUp]
        public void PreparerFichierTest()
        {
            fichierTest = Path.Combine(Path.GetTempPath(), "froggy_tests", $"reglages_{System.Guid.NewGuid():N}.json");
            Assert.That(fichierTest, Is.Not.EqualTo(ReglagesJoueur.CheminReel));
        }

        [TearDown]
        public void EffacerFichierTest()
        {
            if (File.Exists(fichierTest)) File.Delete(fichierTest);
        }

        [Test]
        public void SansFichierToutEstA100()
        {
            ReglagesJoueur r = ReglagesJoueur.Lire(fichierTest);
            Assert.That(new[] { r.volumeGeneral, r.volumeMusique, r.volumeEffets, r.volumeAmbiance, r.secousses, r.flashs,
                r.vibrations }, Is.All.EqualTo(1f));
        }

        [Test]
        public void LesReglagesSurviventAuRedemarrage()
        {
            var r = new ReglagesJoueur { volumeMusique = 0.3f, secousses = 0f, vibrations = 0.5f };
            r.Ecrire(fichierTest);
            ReglagesJoueur relu = ReglagesJoueur.Lire(fichierTest);
            Assert.That(relu.volumeMusique, Is.EqualTo(0.3f).Within(1e-5f));
            Assert.That(relu.secousses, Is.EqualTo(0f), "0 coupe l'effet");
            Assert.That(relu.vibrations, Is.EqualTo(0.5f).Within(1e-5f));
            Assert.That(relu.volumeGeneral, Is.EqualTo(1f));
        }

        [Test]
        public void LesValeursSontBornees()
        {
            var r = new ReglagesJoueur { volumeGeneral = 3f, flashs = -1f, secousses = float.NaN };
            r.Borner();
            Assert.That(r.volumeGeneral, Is.EqualTo(1f));
            Assert.That(r.flashs, Is.EqualTo(0f));
            Assert.That(r.secousses, Is.EqualTo(1f));
        }

        [Test]
        public void UnFichierAbimeDonneLesReglagesParDefaut()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(fichierTest));
            File.WriteAllText(fichierTest, "{ pas du json");
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;
            ReglagesJoueur r = ReglagesJoueur.Lire(fichierTest);
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = false;
            Assert.That(r.volumeEffets, Is.EqualTo(1f));
        }

        // ---------------------------------------------------------------- Scène Menu

        [Test]
        public void LaSceneMenuEstLaPremiereDuBuild()
        {
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            Assert.That(scenes.Length, Is.GreaterThan(0));
            Assert.That(scenes[0].path, Is.EqualTo(MenuPrincipal.SceneMenu));
            Assert.That(scenes[0].enabled, Is.True);
        }

        [Test]
        public void LaSceneMenuPorteLeSalon()
        {
            string guid = AssetDatabase.AssetPathToGUID("Assets/Froggy/Prefabs/Reseau/Salon.prefab");
            Assert.That(guid, Is.Not.Empty);
            Assert.That(File.ReadAllText(MenuPrincipal.SceneMenu), Does.Contain(guid),
                "la scène Menu doit contenir le préfabriqué Salon (P2-03)");
        }
    }
}
