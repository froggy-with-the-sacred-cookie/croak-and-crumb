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
    /// Déroulement d'une partie (P3-03). Les règles seules (ReglesPartie) : compte à rebours, 3 minutes, gagnant avec et
    /// sans cookie sacré, égalité qui part en prolongation et s'arrête dès qu'un joueur passe devant. Puis dans une scène
    /// (DeroulementPartie) : joueurs retenus et cookies arrêtés pendant le compte à rebours, événements de début et de fin.
    /// </summary>
    public class PartieTests
    {
        private const float Dt = 1f / 50f;
        private readonly List<Object> aDetruire = new List<Object>();
        private Scene scene;

        [TearDown]
        public void Ranger()
        {
            if (scene.IsValid()) EditorSceneManager.ClosePreviewScene(scene);
            foreach (var o in aDetruire) if (o != null) Object.DestroyImmediate(o);
            aDetruire.Clear();
        }

        private static List<(int, int)> Scores(params (int, int)[] s) => new List<(int, int)>(s);

        /// <summary>Avance la partie de « secondes », par pas de 1/50 s.</summary>
        private static void Avancer(ReglesPartie regles, float secondes, List<(int, int)> scores, int sacre = ReglesPartie.Personne)
        {
            int pas = Mathf.RoundToInt(secondes / Dt);
            for (int i = 0; i < pas; i++) regles.Pas(Dt, scores, sacre);
        }

        [Test]
        public void UnePartieNeCommencePasToutSeule()
        {
            var regles = new ReglesPartie(3f, 180f);
            Avancer(regles, 10f, Scores((1, 0)));
            Assert.That(regles.Phase, Is.EqualTo(Phase.Attente));
        }

        [Test]
        public void TroisMinutesPuisLeBonGagnant()
        {
            var reglages = ScriptableObject.CreateInstance<ReglagesPartie>();
            aDetruire.Add(reglages);
            Assert.That(reglages.duree, Is.EqualTo(180f), "P3-03 : une partie de 3 minutes");
            var regles = new ReglesPartie(reglages.decompte, reglages.duree);
            var scores = Scores((1, 2), (2, 5), (3, 4));

            regles.Lancer();
            Assert.That(regles.Phase, Is.EqualTo(Phase.Decompte));
            Avancer(regles, 2.9f, scores);
            Assert.That(regles.Phase, Is.EqualTo(Phase.Decompte), "2,9 s : encore le compte à rebours");
            Avancer(regles, 0.2f, scores);
            Assert.That(regles.Phase, Is.EqualTo(Phase.EnJeu));
            Assert.That(regles.Reste, Is.EqualTo(179.9f).Within(0.05f));

            Avancer(regles, 179.7f, scores);
            Assert.That(regles.Phase, Is.EqualTo(Phase.EnJeu), "à 0,2 s de la fin : on joue encore");
            Avancer(regles, 0.4f, scores);
            Assert.That(regles.Phase, Is.EqualTo(Phase.Finie), "elle se termine seule");
            Assert.That(regles.Gagnant, Is.EqualTo(2), "celui qui porte le plus de cookies");
        }

        [Test]
        public void LeCookieSacreLEmporteSurLeNombre()
        {
            var regles = new ReglesPartie(0f, 10f);
            regles.Lancer();
            Avancer(regles, 11f, Scores((1, 9), (2, 0), (3, 4)), sacre: 2);
            Assert.That(regles.Phase, Is.EqualTo(Phase.Finie));
            Assert.That(regles.Gagnant, Is.EqualTo(2), "le porteur du cookie sacré gagne, même sans autre cookie");
        }

        [Test]
        public void UneEgalitePartEnProlongationEtSArreteDesQuUnJoueurPasseDevant()
        {
            var regles = new ReglesPartie(0f, 10f);
            regles.Lancer();
            var egalite = Scores((1, 3), (2, 3), (3, 1));
            Avancer(regles, 11f, egalite);
            Assert.That(regles.Phase, Is.EqualTo(Phase.Prolongation));
            Avancer(regles, 30f, egalite);
            Assert.That(regles.Phase, Is.EqualTo(Phase.Prolongation), "tant que l'égalité dure, on continue");
            Assert.That(regles.Prolonge, Is.GreaterThan(29f));

            regles.Pas(Dt, Scores((1, 3), (2, 4), (3, 1)));
            Assert.That(regles.Phase, Is.EqualTo(Phase.Finie), "dès qu'un seul joueur est en tête");
            Assert.That(regles.Gagnant, Is.EqualTo(2));
        }

        [Test]
        public void EnProlongationPerdreUnCookieDepartageAussi()
        {
            var regles = new ReglesPartie(0f, 10f);
            regles.Lancer();
            Avancer(regles, 11f, Scores((1, 3), (2, 3)));
            Assert.That(regles.Phase, Is.EqualTo(Phase.Prolongation));
            regles.Pas(Dt, Scores((1, 3), (2, 2)));   // le joueur 2 a lâché un cookie (P3-02)
            Assert.That(regles.Phase, Is.EqualTo(Phase.Finie));
            Assert.That(regles.Gagnant, Is.EqualTo(1));
        }

        [Test]
        public void EnProlongationLeCookieSacreTermineLaPartie()
        {
            var regles = new ReglesPartie(0f, 10f);
            regles.Lancer();
            var egalite = Scores((1, 0), (2, 0));
            Avancer(regles, 11f, egalite);
            Assert.That(regles.Phase, Is.EqualTo(Phase.Prolongation), "tout le monde à zéro : égalité");
            regles.Pas(Dt, egalite, porteurCookieSacre: 1);
            Assert.That(regles.Phase, Is.EqualTo(Phase.Finie));
            Assert.That(regles.Gagnant, Is.EqualTo(1));
        }

        [Test]
        public void SansJoueurLaPartieFinitSansGagnant()
        {
            var regles = new ReglesPartie(0f, 10f);
            regles.Lancer();
            Avancer(regles, 11f, Scores());
            Assert.That(regles.Phase, Is.EqualTo(Phase.Finie));
            Assert.That(regles.Gagnant, Is.EqualTo(ReglesPartie.Personne));
        }

        [Test]
        public void LeTempsSAfficheArrondiVersLeHaut()
        {
            Assert.That(DeroulementPartie.Temps(180f), Is.EqualTo("3:00"));
            Assert.That(DeroulementPartie.Temps(59.2f), Is.EqualTo("1:00"));
            Assert.That(DeroulementPartie.Temps(0.3f), Is.EqualTo("0:01"));
            Assert.That(DeroulementPartie.Temps(-1f), Is.EqualTo("0:00"));
        }

        [Test]
        public void DansUneSceneLeDecompteRetientLesJoueursPuisLaPartieSeJoueEtSeTermine()
        {
            var reglagesCookies = ScriptableObject.CreateInstance<ReglagesCookies>();
            var reglagesPartie = ScriptableObject.CreateInstance<ReglagesPartie>();
            aDetruire.Add(reglagesCookies);
            aDetruire.Add(reglagesPartie);
            reglagesCookies.delaiReapparition = 0f;
            reglagesPartie.decompte = 1f;
            reglagesPartie.duree = 2f;
            scene = EditorSceneManager.NewPreviewScene();

            var repere = Objet("Repère", Vector3.zero).AddComponent<RepereArene>();
            repere.sorte = RepereArene.Sorte.Cookie;
            var froggy = Objet("Froggy", new Vector3(0f, reglagesCookies.hauteur, 0f));   // posée sur le cookie dès le départ
            var modificateurs = froggy.AddComponent<Modificateurs>();
            var autre = Objet("Autre", new Vector3(40f, 0f, 0f));
            var cookies = Objet("Cookies", Vector3.zero).AddComponent<PartieCookies>();
            cookies.Regler(reglagesCookies);
            cookies.Demarrer(new[] { repere }, new[] { froggy, autre });
            var partie = Objet("Partie", Vector3.zero).AddComponent<DeroulementPartie>();
            partie.Regler(reglagesPartie, cookies);
            partie.Preparer();
            var porteur = froggy.GetComponent<PorteurCookies>();

            var recus = new List<EvenementPartie>();
            System.Action<EvenementPartie> ecoute = e => { if (e.Type == TypeEvenement.PartieCommencee || e.Type == TypeEvenement.PartieFinie) recus.Add(e); };
            EvenementsPartie.Emis += ecoute;
            try
            {
                partie.Lancer();
                for (int i = 0; i < 25; i++) { partie.Pas(Dt); cookies.Pas(Dt); }   // 0,5 s : compte à rebours
                Assert.That(partie.Regles.Phase, Is.EqualTo(Phase.Decompte));
                Assert.That(modificateurs.Vitesse, Is.EqualTo(0f), "pendant le compte à rebours, Froggy ne bouge pas");
                Assert.That(modificateurs.Saut, Is.EqualTo(0f));
                Assert.That(porteur.Nombre, Is.EqualTo(0), "et rien ne se ramasse");
                Assert.That(recus.Count, Is.EqualTo(0));

                for (int i = 0; i < 35; i++) { partie.Pas(Dt); cookies.Pas(Dt); }   // 1,2 s : on joue
                Assert.That(partie.Regles.Phase, Is.EqualTo(Phase.EnJeu));
                Assert.That(modificateurs.Vitesse, Is.EqualTo(1f), "au départ, Froggy est libérée");
                Assert.That(porteur.Nombre, Is.EqualTo(1), "et ramasse le cookie sous elle");
                Assert.That(recus.Count, Is.EqualTo(1));
                Assert.That(recus[0].Type, Is.EqualTo(TypeEvenement.PartieCommencee));

                for (int i = 0; i < 110; i++) { partie.Pas(Dt); cookies.Pas(Dt); }   // 3,4 s : finie
                Assert.That(partie.Regles.Phase, Is.EqualTo(Phase.Finie));
                Assert.That(partie.Gagnant, Is.SameAs(porteur));
                Assert.That(cookies.EnPause, Is.True, "après la fin, les scores ne bougent plus");
                Assert.That(recus.Count, Is.EqualTo(2));
                Assert.That(recus[1].Type, Is.EqualTo(TypeEvenement.PartieFinie));
                Assert.That(recus[1].Auteur, Is.SameAs(froggy), "l'événement désigne le gagnant");
            }
            finally { EvenementsPartie.Emis -= ecoute; }
        }

        [Test]
        public void DansUneSceneLePorteurDuCookieSacreGagne()
        {
            var reglagesCookies = ScriptableObject.CreateInstance<ReglagesCookies>();
            var reglagesPartie = ScriptableObject.CreateInstance<ReglagesPartie>();
            aDetruire.Add(reglagesCookies);
            aDetruire.Add(reglagesPartie);
            reglagesCookies.delaiReapparition = 0f;
            reglagesPartie.decompte = 0f;
            reglagesPartie.duree = 1f;
            scene = EditorSceneManager.NewPreviewScene();

            var repere = Objet("Repère", Vector3.zero).AddComponent<RepereArene>();
            repere.sorte = RepereArene.Sorte.Cookie;
            var riche = Objet("Riche", new Vector3(0f, reglagesCookies.hauteur, 0f));
            var sacre = Objet("Sacré", new Vector3(40f, 0f, 0f));
            var cookies = Objet("Cookies", Vector3.zero).AddComponent<PartieCookies>();
            cookies.Regler(reglagesCookies);
            cookies.Demarrer(new[] { repere }, new[] { riche, sacre });
            var partie = Objet("Partie", Vector3.zero).AddComponent<DeroulementPartie>();
            partie.Regler(reglagesPartie, cookies);
            partie.Preparer();
            partie.PorteurCookieSacre = sacre.GetComponent<PorteurCookies>();
            partie.Lancer();
            for (int i = 0; i < 70; i++) { partie.Pas(Dt); cookies.Pas(Dt); }
            Assert.That(riche.GetComponent<PorteurCookies>().Nombre, Is.EqualTo(1));
            Assert.That(partie.Regles.Phase, Is.EqualTo(Phase.Finie));
            Assert.That(partie.Gagnant, Is.SameAs(sacre.GetComponent<PorteurCookies>()), "le cookie sacré l'emporte sur le nombre");
        }

        private GameObject Objet(string nom, Vector3 position)
        {
            var go = new GameObject(nom);
            SceneManager.MoveGameObjectToScene(go, scene);
            go.transform.position = position;
            return go;
        }
    }
}
