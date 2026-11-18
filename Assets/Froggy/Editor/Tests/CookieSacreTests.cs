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
    /// Le cookie sacré et son gobelin (P5-08). Les règles seules (ReglesCookieSacre) : rien avant la dernière minute ni
    /// hors du jeu, apparition à 60 s ou en prolongation, capture par le joueur le plus proche (pas dans le pas
    /// d'apparition), lâcher puis ramasser, délai du lâcheur. Puis dans une scène (PartieCookieSacre) : le gobelin
    /// apparaît avec le cookie entre les mains, se fait attraper, le cookie passe au-dessus de la tête du porteur, tombe,
    /// change de main, et son porteur gagne la partie ; une torgnole le fait tomber (pas à un intouchable), et une
    /// nouvelle partie le remet à zéro.
    /// </summary>
    public class CookieSacreTests
    {
        private const float Dt = 1f / 50f;
        private const int P = ReglesPartie.Personne;
        private readonly List<Object> aDetruire = new List<Object>();
        private Scene scene;

        [TearDown]
        public void Ranger()
        {
            if (scene.IsValid()) EditorSceneManager.ClosePreviewScene(scene);
            foreach (var o in aDetruire) if (o != null) Object.DestroyImmediate(o);
            aDetruire.Clear();
        }

        private static ReglesCookieSacre Regles() => new ReglesCookieSacre(60f, 1f, 1.2f, 1.5f);

        private static List<(int, Vector3)> Joueurs(params (int, Vector3)[] j) => new List<(int, Vector3)>(j);

        private static List<(ReglesCookieSacre.Changement, int)> Pas(ReglesCookieSacre r, Phase phase, float reste,
            Vector3 gobelin, List<(int, Vector3)> joueurs)
        {
            var changements = new List<(ReglesCookieSacre.Changement, int)>();
            r.Pas(Dt, phase, reste, gobelin, joueurs, changements);
            return changements;
        }

        /// <summary>Le gobelin apparaît à l'origine, personne autour.</summary>
        private static ReglesCookieSacre Apparu()
        {
            var r = Regles();
            Pas(r, Phase.EnJeu, 60f, Vector3.zero, Joueurs());
            Assert.That(r.Etat, Is.EqualTo(EtatCookieSacre.Gobelin));
            return r;
        }

        [Test]
        public void LesReglagesDisentLaDerniereMinute()
        {
            var reglages = ScriptableObject.CreateInstance<ReglagesCookieSacre>();
            aDetruire.Add(reglages);
            Assert.That(reglages.moment, Is.EqualTo(60f), "Notion : le gobelin arrive dans la dernière minute");
            Assert.That(reglages.vitesse, Is.LessThan(6f), "Froggy court à 6 m/s : le gobelin doit pouvoir être rattrapé");
        }

        [Test]
        public void RienAvantLaDerniereMinute()
        {
            var r = Regles();
            var tout = Joueurs((1, Vector3.zero));
            Assert.That(Pas(r, Phase.EnJeu, 61f, Vector3.zero, tout), Is.Empty);
            Assert.That(r.Etat, Is.EqualTo(EtatCookieSacre.Absent));

            var c = Pas(r, Phase.EnJeu, 60f, Vector3.zero, tout);
            Assert.That(c, Is.EqualTo(new[] { (ReglesCookieSacre.Changement.Apparu, P) }));
            Assert.That(r.Etat, Is.EqualTo(EtatCookieSacre.Gobelin));
        }

        [TestCase(Phase.Attente)]
        [TestCase(Phase.Decompte)]
        [TestCase(Phase.Finie)]
        public void RienHorsDuJeu(Phase phase)
        {
            var r = Regles();
            Assert.That(Pas(r, phase, 0f, Vector3.zero, Joueurs((1, Vector3.zero))), Is.Empty);
            Assert.That(r.Etat, Is.EqualTo(EtatCookieSacre.Absent));
        }

        [Test]
        public void EnProlongationIlArriveToutDeSuite()
        {
            var r = Regles();
            Assert.That(Pas(r, Phase.Prolongation, 0f, Vector3.zero, Joueurs()), Has.Count.EqualTo(1),
                "une partie plus courte qu'une minute a quand même son gobelin");
            Assert.That(r.Etat, Is.EqualTo(EtatCookieSacre.Gobelin));
        }

        [Test]
        public void PasDeCaptureDansLePasDApparition()
        {
            var r = Regles();
            var dessus = Joueurs((1, Vector3.zero));
            Pas(r, Phase.EnJeu, 30f, Vector3.zero, dessus);
            Assert.That(r.Etat, Is.EqualTo(EtatCookieSacre.Gobelin), "il jaillit : on ne l'attrape qu'au pas suivant");
            var c = Pas(r, Phase.EnJeu, 30f, Vector3.zero, dessus);
            Assert.That(c, Is.EqualTo(new[] { (ReglesCookieSacre.Changement.Attrape, 1) }));
            Assert.That(r.Etat, Is.EqualTo(EtatCookieSacre.Porte));
            Assert.That(r.Porteur, Is.EqualTo(1));
        }

        [Test]
        public void LePlusProcheLAttrape()
        {
            var r = Apparu();
            Pas(r, Phase.EnJeu, 59f, Vector3.zero, Joueurs((1, new Vector3(0.8f, 0f, 0f)), (2, new Vector3(0f, 0f, -0.3f))));
            Assert.That(r.Porteur, Is.EqualTo(2));
        }

        [Test]
        public void AEgaliteLePlusPetitNumero()
        {
            var r = Apparu();
            Pas(r, Phase.EnJeu, 59f, Vector3.zero, Joueurs((3, new Vector3(0.5f, 0f, 0f)), (2, new Vector3(-0.5f, 0f, 0f))));
            Assert.That(r.Porteur, Is.EqualTo(2), "le même résultat sur toutes les machines");
        }

        [Test]
        public void HorsDePorteePersonneMaisEnSautantParDessusOui()
        {
            var r = Apparu();
            Assert.That(Pas(r, Phase.EnJeu, 59f, Vector3.zero, Joueurs((1, new Vector3(1.5f, 0f, 0f)))), Is.Empty);
            Assert.That(r.Etat, Is.EqualTo(EtatCookieSacre.Gobelin));
            Pas(r, Phase.EnJeu, 59f, Vector3.zero, Joueurs((1, new Vector3(0.2f, 2f, 0f))));
            Assert.That(r.Porteur, Is.EqualTo(1), "la distance se mesure à plat");
        }

        [Test]
        public void LacherPuisUnAutreLeRamasse()
        {
            var r = Apparu();
            Pas(r, Phase.EnJeu, 59f, Vector3.zero, Joueurs((1, Vector3.zero)));
            Assert.That(r.Lacher(2, Vector3.zero), Is.False, "seul le porteur peut le lâcher");
            var chute = new Vector3(3f, 0f, 0f);
            Assert.That(r.Lacher(1, chute), Is.True);
            Assert.That(r.Etat, Is.EqualTo(EtatCookieSacre.AuSol));
            Assert.That(r.Porteur, Is.EqualTo(P));
            Assert.That(r.PositionAuSol, Is.EqualTo(chute));
            Assert.That(r.Lacher(1, chute), Is.False, "on ne lâche pas deux fois");

            Assert.That(Pas(r, Phase.EnJeu, 50f, Vector3.zero, Joueurs((2, new Vector3(5f, 0f, 0f)))), Is.Empty);
            var c = Pas(r, Phase.EnJeu, 50f, Vector3.zero, Joueurs((2, new Vector3(4f, 0f, 0f))));
            Assert.That(c, Is.EqualTo(new[] { (ReglesCookieSacre.Changement.Ramasse, 2) }));
            Assert.That(r.Porteur, Is.EqualTo(2));
        }

        [Test]
        public void LeLacheurAttendLeDelaiDeReprise()
        {
            var r = Apparu();
            Pas(r, Phase.EnJeu, 59f, Vector3.zero, Joueurs((1, Vector3.zero)));
            r.Lacher(1, Vector3.zero);
            var lui = Joueurs((1, Vector3.zero));
            for (int i = 0; i < 70; i++) Pas(r, Phase.EnJeu, 50f, Vector3.zero, lui);   // 1,4 s
            Assert.That(r.Etat, Is.EqualTo(EtatCookieSacre.AuSol), "frappé, il ne le reprend pas aussitôt");
            for (int i = 0; i < 10; i++) Pas(r, Phase.EnJeu, 50f, Vector3.zero, lui);   // 1,6 s
            Assert.That(r.Porteur, Is.EqualTo(1), "après le délai, si");
        }

        [Test]
        public void LePorteurEstEnTete()
        {
            var scores = new List<(int, int)> { (1, 7), (2, 0) };
            Assert.That(ReglesPartie.EnTete(scores, 2, out bool egalite), Is.EqualTo(2));
            Assert.That(egalite, Is.False);
        }

        /// <summary>Une partie de 4 s dans une scène d'aperçu : Froggy, un autre joueur, un départ du gobelin.</summary>
        private sealed class Montage
        {
            public GameObject Froggy, Autre, Depart;
            public DeroulementPartie Partie;
            public PartieCookies Cookies;
            public PartieCookieSacre Sacre;
            public ReglagesCookieSacre Reglages;
            public PorteurCookies Porteur => Froggy.GetComponent<PorteurCookies>();
            public PorteurCookies PorteurAutre => Autre.GetComponent<PorteurCookies>();
        }

        private Montage Monter()
        {
            var reglagesCookies = ScriptableObject.CreateInstance<ReglagesCookies>();
            var reglagesPartie = ScriptableObject.CreateInstance<ReglagesPartie>();
            var m = new Montage { Reglages = ScriptableObject.CreateInstance<ReglagesCookieSacre>() };
            aDetruire.Add(reglagesCookies);
            aDetruire.Add(reglagesPartie);
            aDetruire.Add(m.Reglages);
            reglagesPartie.decompte = 0f;
            reglagesPartie.duree = 4f;
            m.Reglages.moment = 3f;   // il arrive après 1 s de jeu
            scene = EditorSceneManager.NewPreviewScene();
            // loin de tout : les rayons (sol, murs) ne doivent rien toucher de la scène ouverte dans l'éditeur
            Vector3 o = new Vector3(0f, 300f, 0f);

            var repere = Objet("Repère", o + new Vector3(0f, 0f, -30f)).AddComponent<RepereArene>();
            repere.sorte = RepereArene.Sorte.Cookie;
            m.Froggy = Objet("Froggy", o);
            m.Autre = Objet("Autre", o + new Vector3(40f, 0f, 0f));
            m.Depart = Objet("Départ du gobelin", o + new Vector3(0f, 0f, 5f));
            m.Cookies = Objet("Cookies", o).AddComponent<PartieCookies>();
            m.Cookies.Regler(reglagesCookies);
            m.Cookies.Demarrer(new[] { repere }, new[] { m.Froggy, m.Autre });
            m.Partie = Objet("Partie", o).AddComponent<DeroulementPartie>();
            m.Partie.Regler(reglagesPartie, m.Cookies);
            m.Partie.Preparer();
            m.Sacre = Objet("Cookie sacré", o).AddComponent<PartieCookieSacre>();
            m.Sacre.Regler(m.Reglages, m.Partie, m.Cookies, m.Depart.transform);
            m.Sacre.Preparer();
            m.Partie.Lancer();
            return m;
        }

        /// <summary>Le gobelin arrive (après 1 s de jeu) et Froggy l'attrape aussitôt.</summary>
        private static FuiteGobelin Attraper(Montage m)
        {
            Avancer(m, 1.1f);
            var gobelin = m.Sacre.Gobelin;
            Assert.That(gobelin, Is.Not.Null, "le gobelin est là");
            m.Froggy.transform.position = gobelin.transform.position + new Vector3(0f, 0f, -0.5f);
            Avancer(m, Dt);
            Assert.That(m.Partie.PorteurCookieSacre, Is.SameAs(m.Porteur), "Froggy l'a attrapé");
            return gobelin;
        }

        [Test]
        public void DansUneSceneLeGobelinApparaitSeFaitAttraperEtLePorteurGagne()
        {
            var m = Monter();
            var recus = new List<EvenementPartie>();
            System.Action<EvenementPartie> ecoute = e => { if (e.Type == TypeEvenement.CookieSacre) recus.Add(e); };
            EvenementsPartie.Emis += ecoute;
            try
            {
                Avancer(m, 0.5f);
                Assert.That(m.Sacre.Gobelin, Is.Null, "pas avant son moment");
                Avancer(m, 0.6f);

                var gobelin = m.Sacre.Gobelin;
                Assert.That(gobelin, Is.Not.Null, "le gobelin est là");
                Assert.That(gobelin.transform.position, Is.EqualTo(m.Depart.transform.position), "à son départ");
                Assert.That(m.Sacre.Cookie.transform.parent, Is.SameAs(gobelin.Mains), "le cookie sacré entre ses mains");
                Assert.That(gobelin.Mains.name, Is.EqualTo("Mains"));
                Assert.That(Largeur(m.Sacre.Cookie), Is.EqualTo(0.24f).Within(0.01f), "à la taille de ses mains");
                Assert.That(recus.Count, Is.EqualTo(1));
                Assert.That(recus[0].Auteur, Is.SameAs(gobelin.gameObject));

                m.Froggy.transform.position = gobelin.transform.position + new Vector3(0f, 0f, -0.5f);
                Avancer(m, Dt);
                Assert.That(m.Sacre.Regles.Porteur, Is.EqualTo(m.Porteur.Joueur), "Froggy l'attrape");
                Assert.That(m.Partie.PorteurCookieSacre, Is.SameAs(m.Porteur), "DeroulementPartie le sait");
                Assert.That(gobelin.EtatActuel, Is.EqualTo(FuiteGobelin.Etat.Attrape));
                Assert.That(m.Sacre.Cookie.transform.parent, Is.SameAs(m.Froggy.transform), "au-dessus de sa tête");
                Assert.That(m.Sacre.Cookie.transform.position.y, Is.GreaterThan(m.Froggy.transform.position.y + 0.9f));
                Assert.That(recus.Count, Is.EqualTo(2));
                Assert.That(recus[1].Auteur, Is.SameAs(m.Froggy));

                Assert.That(m.Sacre.FaireLacher(m.PorteurAutre, Vector3.right), Is.False, "l'autre ne le porte pas");
                Assert.That(m.Sacre.FaireLacher(m.Porteur, Vector3.right), Is.True);
                Assert.That(m.Partie.PorteurCookieSacre, Is.Null);
                Assert.That(m.Sacre.Cookie.transform.parent, Is.SameAs(m.Sacre.transform), "tombé au sol");
                Assert.That(m.Sacre.Cookie.transform.position.x,
                    Is.EqualTo(m.Froggy.transform.position.x + m.Reglages.distanceLacher).Within(0.01f), "dans la direction du coup");

                m.Autre.transform.position = m.Sacre.Cookie.transform.position;
                Avancer(m, Dt);
                Assert.That(m.Partie.PorteurCookieSacre, Is.SameAs(m.PorteurAutre), "l'autre l'a ramassé");

                Avancer(m, 3f);
                Assert.That(m.Partie.Regles.Phase, Is.EqualTo(Phase.Finie));
                Assert.That(m.Partie.Gagnant, Is.SameAs(m.PorteurAutre), "son porteur gagne");
                Assert.That(gobelin.EtatActuel, Is.EqualTo(FuiteGobelin.Etat.Parti), "le gobelin attrapé est parti");
                Assert.That(gobelin.gameObject.activeSelf, Is.False);
            }
            finally { EvenementsPartie.Emis -= ecoute; }
        }

        [Test]
        public void UneTorgnoleLeFaitLacherMemeSansAutreCookie()
        {
            var m = Monter();
            Attraper(m);
            Assert.That(m.Porteur.Nombre, Is.EqualTo(0), "Froggy n'a aucun autre cookie : PartieCookies ne lui fait rien lâcher");
            m.Autre.transform.position = m.Froggy.transform.position + new Vector3(-1f, 0f, 0f);
            m.Sacre.Reagir(new EvenementPartie(TypeEvenement.CoupDonne, m.Autre, m.Froggy, m.Froggy.transform.position));
            Assert.That(m.Sacre.Regles.Etat, Is.EqualTo(EtatCookieSacre.AuSol), "la torgnole (P4-10) fait tomber le cookie sacré");
            Assert.That(m.Partie.PorteurCookieSacre, Is.Null);
            Vector3 chute = m.Sacre.Cookie.transform.position - m.Froggy.transform.position;
            chute.y = 0f;
            Assert.That(Vector3.Angle(chute, Vector3.right), Is.EqualTo(40f).Within(1f),
                "poussé loin du frappeur, à côté du cookie ordinaire qui tomberait avec lui");
        }

        [Test]
        public void UnCoupRateOuSurUnAutreNeChangeRien()
        {
            var m = Monter();
            Attraper(m);
            m.Sacre.Reagir(new EvenementPartie(TypeEvenement.CoupDonne, m.Froggy, null, m.Froggy.transform.position));
            m.Sacre.Reagir(new EvenementPartie(TypeEvenement.CoupDonne, m.Froggy, m.Autre, m.Autre.transform.position));
            Assert.That(m.Sacre.Regles.Etat, Is.EqualTo(EtatCookieSacre.Porte));
            Assert.That(m.Partie.PorteurCookieSacre, Is.SameAs(m.Porteur));
        }

        [Test]
        public void UnIntouchableNeLeLachePas()
        {
            var m = Monter();
            Attraper(m);
            m.Porteur.Intouchable = true;   // tout juste réapparu (P3-04)
            Assert.That(m.Sacre.FaireLacher(m.Porteur, Vector3.right), Is.False);
            m.Sacre.Reagir(new EvenementPartie(TypeEvenement.CoupDonne, m.Autre, m.Froggy, m.Froggy.transform.position));
            Assert.That(m.Sacre.Regles.Etat, Is.EqualTo(EtatCookieSacre.Porte), "comme ses cookies, on ne le lui prend pas");
        }

        [Test]
        public void UneNouvellePartieLeRemetAZero()
        {
            var m = Monter();
            Attraper(m);
            m.Partie.Preparer();   // ce que fait « Rejouer » (DeroulementPartie.Rejouer), sans aller chercher une réapparition
            m.Partie.Lancer();
            Avancer(m, Dt);
            Assert.That(m.Sacre.Regles.Etat, Is.EqualTo(EtatCookieSacre.Absent));
            Assert.That(m.Sacre.Gobelin, Is.Null);
            Assert.That(m.Sacre.Cookie, Is.Null, "le cookie sacré quitte la tête de Froggy");
            Assert.That(m.Partie.PorteurCookieSacre, Is.Null);
            Avancer(m, 1.1f);
            Assert.That(m.Sacre.Gobelin, Is.Not.Null, "et le gobelin revient dans la nouvelle partie");
        }

        [Test]
        public void EnMiroirLeGobelinNApparaitPasDeLuiMeme()
        {
            var m = Monter();
            Avancer(m, 0.9f);   // il reste 3,1 s : pas encore de gobelin
            m.Partie.Miroir = true;   // en réseau, chez un joueur qui n'est pas l'hôte (P2-04)
            m.Partie.Refleter(Phase.EnJeu, 2f, 0f, ReglesPartie.Personne);
            Avancer(m, 0.2f);
            Assert.That(m.Sacre.Gobelin, Is.Null, "c'est l'hôte qui décide");
        }

        [Test]
        public void EnMiroirOnNeLeFaitPasLacher()
        {
            var m = Monter();
            Attraper(m);
            m.Partie.Miroir = true;
            Assert.That(m.Sacre.FaireLacher(m.Porteur, Vector3.right), Is.False);
            m.Sacre.Reagir(new EvenementPartie(TypeEvenement.CoupDonne, m.Autre, m.Froggy, m.Froggy.transform.position));
            Assert.That(m.Sacre.Regles.Etat, Is.EqualTo(EtatCookieSacre.Porte), "c'est l'hôte qui décide");
        }

        private static void Avancer(Montage m, float secondes)
        {
            int pas = Mathf.Max(1, Mathf.RoundToInt(secondes / Dt));
            for (int i = 0; i < pas; i++) { m.Partie.Pas(Dt); m.Cookies.Pas(Dt); m.Sacre.Pas(Dt); }
        }

        private static float Largeur(GameObject cookie)
        {
            var b = new Bounds();
            bool trouve = false;
            foreach (var r in cookie.GetComponentsInChildren<MeshRenderer>())
            {
                if (!trouve) { b = r.bounds; trouve = true; } else b.Encapsulate(r.bounds);
            }
            return b.size.x;
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
