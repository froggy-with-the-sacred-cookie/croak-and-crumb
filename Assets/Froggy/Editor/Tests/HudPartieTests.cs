using System.Collections.Generic;
using Froggy.Arene;
using Froggy.Interface;
using Froggy.Objets;
using Froggy.Regles;
using Froggy.Socle;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Froggy.Outils.Tests
{
    /// <summary>
    /// Le HUD de partie (P6-02). Les textes seuls (ContenuHud) : minuteur, scores, qui est en tête à 2 comme à 4 joueurs,
    /// cookie sacré et son temps de garde, objet, torgnole, fin de partie (y compris quand le porteur du cookie sacré
    /// n'arrive pas chez un joueur en réseau). Puis dans une scène (HudPartie) : à 2 et à 4 joueurs, le HUD lit les vraies
    /// valeurs des règles (scores, temps, recharge de la torgnole), prend la place des affichages provisoires et la rend.
    /// </summary>
    public class HudPartieTests
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

        [Test]
        public void UnBotNEstJamaisLeJoueurLocal()
        {
            // #132 : un bot garde un PlayerInput éteint ; le HUD le prenait pour « toi » et affichait son score
            var humain = new GameObject("Humain", typeof(PlayerInput));
            var bot = new GameObject("Bot", typeof(PlayerInput));
            bot.GetComponent<PlayerInput>().enabled = false;
            var mannequin = new GameObject("Mannequin");
            aDetruire.Add(humain);
            aDetruire.Add(bot);
            aDetruire.Add(mannequin);

            Assert.That(HudPartie.EstJoueurLocal(humain.transform), Is.True);
            Assert.That(HudPartie.EstJoueurLocal(bot.transform), Is.False, "PlayerInput éteint : un bot");
            Assert.That(HudPartie.EstJoueurLocal(mannequin.transform), Is.False);
            Assert.That(HudPartie.EstJoueurLocal(null), Is.False);
        }

        private static EtatHud Etat(Phase phase, float reste, params (int joueur, int cookies)[] joueurs)
        {
            var e = new EtatHud { PartieConnue = true, Phase = phase, Reste = reste, Local = 1 };
            foreach (var (j, c) in joueurs) e.Joueurs.Add(new JoueurHud { Joueur = j, Cookies = c });
            return e;
        }

        // ---------------------------------------------------------------- Textes

        [Test]
        public void LeMinuteurSuitLaPartie()
        {
            Assert.That(ContenuHud.Minuteur(Etat(Phase.Attente, 0f)), Is.Empty);
            Assert.That(ContenuHud.Minuteur(Etat(Phase.Decompte, 2.3f)), Is.EqualTo("3"));
            Assert.That(ContenuHud.Minuteur(Etat(Phase.Decompte, 0.01f)), Is.EqualTo("1"), "jamais « 0 » avant le départ");
            Assert.That(ContenuHud.Minuteur(Etat(Phase.EnJeu, 151.2f)), Is.EqualTo("2:32"));
            Assert.That(ContenuHud.Minuteur(Etat(Phase.Prolongation, 0f)), Is.EqualTo("Prolongation"));
            Assert.That(ContenuHud.Minuteur(Etat(Phase.Finie, 0f)), Is.Empty);

            Assert.That(ContenuHud.SousMinuteur(Etat(Phase.EnJeu, 61f)), Is.Empty);
            Assert.That(ContenuHud.MinuteurPresse(Etat(Phase.EnJeu, 61f)), Is.False);
            Assert.That(ContenuHud.SousMinuteur(Etat(Phase.EnJeu, 60f)), Is.EqualTo("Dernière minute"), "le gobelin arrive : dit en toutes lettres");
            Assert.That(ContenuHud.MinuteurPresse(Etat(Phase.EnJeu, 60f)), Is.True);
            Assert.That(ContenuHud.SousMinuteur(Etat(Phase.Prolongation, 0f)), Is.EqualTo("Le premier seul en tête gagne"));
        }

        [Test]
        public void LesScoresSeLisentAuSingulierCommeAuPluriel()
        {
            Assert.That(ContenuHud.Cookies(0), Is.EqualTo("0 cookie"));
            Assert.That(ContenuHud.Cookies(1), Is.EqualTo("1 cookie"));
            Assert.That(ContenuHud.Cookies(7), Is.EqualTo("7 cookies"));
            var e = Etat(Phase.EnJeu, 100f, (1, 0), (2, 0));
            Assert.That(ContenuHud.Nom(e, 1), Is.EqualTo("Joueur 1 (toi)"));
            Assert.That(ContenuHud.Nom(e, 2), Is.EqualTo("Joueur 2"));
        }

        [Test]
        public void EnTeteADeuxCommeAQuatre()
        {
            var deux = Etat(Phase.EnJeu, 100f, (1, 2), (2, 3));
            Assert.That(ContenuHud.EnTete(deux), Is.EqualTo(2));
            Assert.That(ContenuHud.Marque(deux, 2), Is.EqualTo("en tête"));
            Assert.That(ContenuHud.Marque(deux, 1), Is.Empty);

            var quatre = Etat(Phase.EnJeu, 100f, (1, 1), (2, 4), (3, 4), (4, 0));
            Assert.That(ContenuHud.EnTete(quatre), Is.EqualTo(P), "égalité en tête : personne n'est marqué");
            Assert.That(ContenuHud.Marque(quatre, 2), Is.Empty);

            quatre.Sacre = EtatCookieSacre.Porte;
            quatre.PorteurSacre = 4;
            Assert.That(ContenuHud.EnTete(quatre), Is.EqualTo(4), "le porteur du cookie sacré gagne, même sans cookie");
            Assert.That(ContenuHud.Marque(quatre, 4), Is.EqualTo("cookie sacré"));

            Assert.That(ContenuHud.Marque(Etat(Phase.Decompte, 2f, (1, 0), (2, 3)), 2), Is.Empty, "rien avant le départ");
        }

        [Test]
        public void LeCookieSacreDitOuIlEstEtCombienDeTempsLeGarder()
        {
            var e = Etat(Phase.EnJeu, 37f, (1, 0), (2, 0));
            Assert.That(ContenuHud.CookieSacre(e), Is.Empty, "pas encore là");
            e.Sacre = EtatCookieSacre.Gobelin;
            Assert.That(ContenuHud.CookieSacre(e), Does.Contain("gobelin"));
            e.Sacre = EtatCookieSacre.Porte;
            e.PorteurSacre = 2;
            Assert.That(ContenuHud.CookieSacre(e), Is.EqualTo("Joueur 2 porte le cookie sacré : encore 0:37 à le garder"));
            e.PorteurSacre = 1;
            Assert.That(ContenuHud.CookieSacre(e), Is.EqualTo("Tu portes le cookie sacré : encore 0:37 à le garder"));
            e.Sacre = EtatCookieSacre.AuSol;
            e.PorteurSacre = P;
            Assert.That(ContenuHud.CookieSacre(e), Does.Contain("tombé"));

            var prolongation = Etat(Phase.Prolongation, 0f, (1, 0), (2, 0));
            prolongation.Sacre = EtatCookieSacre.Gobelin;
            Assert.That(ContenuHud.CookieSacre(prolongation), Does.Contain("gobelin"), "le gobelin arrive aussi en prolongation");
        }

        [Test]
        public void LObjetEtLaTorgnoleDisentLeurTouche()
        {
            var e = Etat(Phase.EnJeu, 100f);
            Assert.That(ContenuHud.Objet(e), Is.Empty, "pas d'objets dans la scène");
            Assert.That(ContenuHud.Torgnole(e), Is.Empty, "pas de torgnole dans la scène");

            e.ObjetsConnus = true;
            Assert.That(ContenuHud.Objet(e), Is.EqualTo("Objet : aucun"));
            e.Objet = "Super croassement";
            e.ToucheObjet = "F";
            Assert.That(ContenuHud.Objet(e), Is.EqualTo("Objet : Super croassement · F pour l'utiliser"));
            e.Objet = null;
            e.RechargeObjet = 1.21f;
            Assert.That(ContenuHud.Objet(e), Is.EqualTo("Objet : aucun · recharge 1,3 s"));

            e.TorgnoleConnue = true;
            e.ToucheTorgnole = "V";
            Assert.That(ContenuHud.Torgnole(e), Is.EqualTo("Torgnole (V) : prête"));
            e.RechargeTorgnole = 3.2f;
            e.DureeRechargeTorgnole = 5f;
            Assert.That(ContenuHud.Torgnole(e), Is.EqualTo("Torgnole (V) : 3,2 s"));
            Assert.That(ContenuHud.Jauge(3.2f, 5f), Is.EqualTo(0.36f).Within(1e-4f));
            Assert.That(ContenuHud.Jauge(0f, 5f), Is.EqualTo(1f));
        }

        [Test]
        public void LesSecondesNeDisentJamaisZeroAvantLaFin()
        {
            Assert.That(ContenuHud.Secondes(0.04f), Is.EqualTo("0,1 s"));
            Assert.That(ContenuHud.Secondes(0f), Is.EqualTo("0,0 s"));
            Assert.That(ContenuHud.Secondes(-1f), Is.EqualTo("0,0 s"));
            Assert.That(ContenuHud.Secondes(5f), Is.EqualTo("5,0 s"));
        }

        [Test]
        public void LaMeteoSEcritEnToutesLettres()
        {
            var e = Etat(Phase.EnJeu, 100f);
            Assert.That(ContenuHud.Meteo(e), Is.Empty, "temps calme");
            e.Meteo = "Pluie tropicale";
            e.MeteoAnnoncee = true;
            e.MeteoReste = 2.2f;
            e.MeteoEffets = "vitesse +20 %";
            Assert.That(ContenuHud.Meteo(e), Is.EqualTo("Dans 3 s : Pluie tropicale · vitesse +20 %"));
            e.MeteoAnnoncee = false;
            e.MeteoReste = 12f;
            Assert.That(ContenuHud.Meteo(e), Is.EqualTo("Pluie tropicale : encore 12 s · vitesse +20 %"));
        }

        [Test]
        public void LaFinDitQuiAGagneEtPourquoi()
        {
            Assert.That(ContenuHud.Fin(Etat(Phase.EnJeu, 10f, (1, 2))), Is.EqualTo(("", "")));

            var e = Etat(Phase.Finie, 0f, (1, 2), (2, 5), (3, 1), (4, 0));
            e.Gagnant = 2;
            Assert.That(ContenuHud.Fin(e), Is.EqualTo(("Joueur 2 gagne", "avec 5 cookies")));
            e.Gagnant = 1;
            e.Sacre = EtatCookieSacre.Porte;
            e.PorteurSacre = 1;
            Assert.That(ContenuHud.Fin(e), Is.EqualTo(("Tu as gagné !", "avec le cookie sacré")));

            // en réseau, le porteur du cookie sacré n'arrive pas encore chez les autres : un gagnant qui n'a pas le plus
            // de cookies ne peut avoir gagné qu'avec lui
            e.Sacre = EtatCookieSacre.Absent;
            e.PorteurSacre = P;
            e.Gagnant = 4;
            Assert.That(ContenuHud.Fin(e), Is.EqualTo(("Joueur 4 gagne", "avec le cookie sacré")));

            e.Gagnant = P;
            Assert.That(ContenuHud.Fin(e), Is.EqualTo(("Partie finie", "")));

            var deux = Etat(Phase.Finie, 0f, (1, 1), (2, 1));
            deux.Gagnant = 2;
            deux.Local = 2;
            Assert.That(ContenuHud.Fin(deux).titre, Is.EqualTo("Tu as gagné !"));
        }

        // ---------------------------------------------------------------- Dans une scène

        [TestCase(2)]
        [TestCase(4)]
        public void DansUneSceneLeHudLitLesVraiesValeurs(int nombre)
        {
            var reglagesCookies = ScriptableObject.CreateInstance<ReglagesCookies>();
            var reglagesPartie = ScriptableObject.CreateInstance<ReglagesPartie>();
            var reglagesTorgnole = ScriptableObject.CreateInstance<ReglagesTorgnole>();
            aDetruire.Add(reglagesCookies);
            aDetruire.Add(reglagesPartie);
            aDetruire.Add(reglagesTorgnole);
            reglagesPartie.decompte = 1f;
            reglagesPartie.duree = 10f;
            scene = EditorSceneManager.NewPreviewScene();

            // un cookie sous chaque joueur sauf le dernier ; les joueurs à 3 m les uns des autres, numérotés par x
            var reperes = new List<RepereArene>();
            var froggys = new List<GameObject>();
            for (int i = 0; i < nombre; i++)
            {
                var ici = new Vector3(i * 3f, 0.5f, 0f);
                froggys.Add(Objet($"Froggy {i + 1}", ici));
                if (i == nombre - 1) continue;
                var r = Objet($"Cookie {i + 1}", new Vector3(ici.x, 0f, 0f)).AddComponent<RepereArene>();
                r.sorte = RepereArene.Sorte.Cookie;
                reperes.Add(r);
            }
            froggys[0].AddComponent<PlayerInput>();   // le joueur de cette machine

            var cookies = Objet("Cookies", Vector3.zero).AddComponent<PartieCookies>();
            cookies.Regler(reglagesCookies);
            cookies.Demarrer(reperes, froggys);
            var partie = Objet("Partie", Vector3.zero).AddComponent<DeroulementPartie>();
            partie.Regler(reglagesPartie, cookies);
            partie.Preparer();
            var torgnoles = Objet("Torgnoles", Vector3.zero).AddComponent<PartieTorgnoles>();
            torgnoles.Regler(reglagesTorgnole, cookies);
            torgnoles.Pas(Dt);

            var hud = Objet("HUD", Vector3.zero).AddComponent<HudPartie>();
            Assert.That(cookies.ScoreProvisoire && partie.AffichageProvisoire && torgnoles.AffichageProvisoire, Is.True);
            hud.Regler(partie, cookies, null, torgnoles);
            Assert.That(cookies.ScoreProvisoire, Is.False, "le HUD remplace le score provisoire");
            Assert.That(partie.AffichageProvisoire, Is.False, "et le temps provisoire");
            Assert.That(torgnoles.AffichageProvisoire, Is.False, "et la recharge provisoire");

            partie.Lancer();
            hud.Rafraichir();
            Assert.That(hud.Etat.Phase, Is.EqualTo(Phase.Decompte));
            Assert.That(ContenuHud.Minuteur(hud.Etat), Is.EqualTo("1"));

            // départ, puis une seconde de jeu : chacun ramasse le cookie sous lui
            for (int i = 0; i < 100; i++) { partie.Pas(Dt); cookies.Pas(Dt); torgnoles.Pas(Dt); }
            hud.Rafraichir();
            var e = hud.Etat;
            Assert.That(e.Phase, Is.EqualTo(Phase.EnJeu));
            Assert.That(ContenuHud.Minuteur(e), Is.EqualTo(DeroulementPartie.Temps(partie.Regles.Reste)));
            Assert.That(e.Local, Is.EqualTo(1), "le joueur qui a un PlayerInput");
            Assert.That(e.Joueurs.Count, Is.EqualTo(nombre));
            for (int i = 0; i < nombre; i++)
            {
                var porteur = froggys[i].GetComponent<PorteurCookies>();
                Assert.That(e.Joueurs[i].Joueur, Is.EqualTo(porteur.Joueur), "dans l'ordre des numéros");
                Assert.That(e.Joueurs[i].Cookies, Is.EqualTo(porteur.Nombre));
                Assert.That(e.Joueurs[i].Cookies, Is.EqualTo(i == nombre - 1 ? 0 : 1), $"joueur {porteur.Joueur}");
            }

            // une torgnole du joueur local : la recharge et le message arrivent au HUD
            var local = froggys[0].GetComponent<PorteurCookies>();
            froggys[1].transform.position = new Vector3(0f, 0.5f, 1f);   // le joueur 2 passe devant lui
            var recus = new List<EvenementPartie>();
            System.Action<EvenementPartie> ecoute = ev => recus.Add(ev);
            EvenementsPartie.Emis += ecoute;
            try { Assert.That(torgnoles.Frapper(local), Is.Not.Null); }
            finally { EvenementsPartie.Emis -= ecoute; }
            foreach (var ev in recus) hud.Reagir(ev);
            hud.Rafraichir();
            Assert.That(hud.Etat.RechargeTorgnole, Is.EqualTo(5f).Within(1e-3f));
            Assert.That(ContenuHud.Torgnole(hud.Etat), Does.StartWith("Torgnole").And.EndWith("5,0 s"));
            Assert.That(hud.MessageTorgnole, Is.EqualTo("Torgnole sur Joueur 2"));
            Assert.That(hud.Etat.Joueurs[1].Cookies, Is.EqualTo(0), "le joueur 2 a lâché son cookie");

            hud.RendreLaPlace();
            Assert.That(cookies.ScoreProvisoire && partie.AffichageProvisoire && torgnoles.AffichageProvisoire, Is.True,
                "HUD coupé : les affichages provisoires reviennent");
        }

        [Test]
        public void LeHudLitLObjetDuJoueurLocal()
        {
            var reglages = ScriptableObject.CreateInstance<ReglagesObjets>();
            var fiche = ScriptableObject.CreateInstance<FicheObjet>();
            aDetruire.Add(reglages);
            aDetruire.Add(fiche);
            fiche.nom = "Super croassement";
            fiche.recharge = 2f;
            reglages.objets = new List<FicheObjet> { fiche };
            scene = EditorSceneManager.NewPreviewScene();

            var boite = Objet("Boîte", Vector3.zero).AddComponent<RepereArene>();
            boite.sorte = RepereArene.Sorte.Boite;
            var froggy = Objet("Froggy", new Vector3(0f, 0.5f, 0f));
            var objets = Objet("Objets", Vector3.zero).AddComponent<PartieObjets>();
            objets.Regler(reglages);
            objets.Demarrer(new[] { boite }, new[] { froggy });

            var hud = Objet("HUD", Vector3.zero).AddComponent<HudPartie>();
            hud.Regler(null, null, objets);
            Assert.That(objets.AffichageProvisoire, Is.False);
            hud.Rafraichir();
            Assert.That(ContenuHud.Objet(hud.Etat), Is.EqualTo("Objet : aucun"));

            objets.Pas(Dt);   // il prend la boîte
            hud.Rafraichir();
            Assert.That(hud.Etat.Objet, Is.EqualTo("Super croassement"));
            Assert.That(hud.MessageObjet, Is.EqualTo("Super croassement !"));

            Assert.That(objets.Utiliser(objets.Local), Is.SameAs(fiche));
            hud.Rafraichir();
            Assert.That(hud.Etat.Objet, Is.Null);
            Assert.That(hud.Etat.RechargeObjet, Is.EqualTo(2f).Within(1e-3f));
            Assert.That(hud.Etat.DureeRechargeObjet, Is.EqualTo(2f));
            Assert.That(hud.MessageObjet, Is.EqualTo("Super croassement utilisé"));
            hud.RendreLaPlace();
            Assert.That(objets.AffichageProvisoire, Is.True);
        }

        private GameObject Objet(string nom, Vector3 position)
        {
            var go = new GameObject(nom);
            SceneManager.MoveGameObjectToScene(go, scene);
            go.transform.position = position;
            return go;
        }

        // ---------------------------------------------------------------- HUD habillé (refonte du 19/11/2026)

        private static readonly string[] IconesDuHud =
        {
            "dash", "torgnole", "super_croassement", "camouflage_cameleon", "gel_eclair", "assomme", "plongeon",
            "cookie", "cookie_sacre", "couronne", "pluie_tropicale", "neige"
        };

        [TestCase("Gel éclair", "gel_eclair")]
        [TestCase("Camouflage caméléon", "camouflage_cameleon")]
        [TestCase("Super croassement", "super_croassement")]
        [TestCase("Pluie tropicale", "pluie_tropicale")]
        [TestCase("Neige", "neige")]
        [TestCase("  L'Averse d'essai ", "l_averse_d_essai")]
        [TestCase("Cœur", "coeur")]
        [TestCase("", "")]
        public void LaCleDUneIconeVientDuNomAffiche(string nom, string cle)
        {
            Assert.That(ContenuHud.CleIcone(nom), Is.EqualTo(cle));
        }

        [Test]
        public void ChaqueIconeDuHudSeCharge()
        {
            foreach (string cle in IconesDuHud)
                Assert.That(Resources.Load<Sprite>(VueHud.DossierIcones + cle), Is.Not.Null, cle);
        }

        [Test]
        public void ChaqueObjetEtChaqueMeteoDuJardinOntUneIcone()
        {
            // un objet ou une météo ajoutés sans icône : le HUD montrerait une poche vide (ou une pastille muette)
            var reglagesObjets = UnityEditor.AssetDatabase.LoadAssetAtPath<ReglagesObjets>("Assets/Froggy/Postes/4_ObjetsCapacites/ReglagesObjets.asset");
            Assert.That(reglagesObjets, Is.Not.Null);
            foreach (var fiche in reglagesObjets.objets)
                Assert.That(fiche.icone != null || Resources.Load<Sprite>(VueHud.DossierIcones + ContenuHud.CleIcone(fiche.nom)) != null,
                    Is.True, "icône de l'objet « " + fiche.nom + " »");
            var meteo = UnityEditor.AssetDatabase.LoadAssetAtPath<Meteo.ReglagesMeteo>("Assets/Froggy/Postes/5_MeteoEvenements/ReglagesMeteo_Jardin.asset");
            Assert.That(meteo, Is.Not.Null);
            foreach (var tirage in meteo.evenements)
                Assert.That(Resources.Load<Sprite>(VueHud.DossierIcones + ContenuHud.CleIcone(tirage.evenement.nom)), Is.Not.Null,
                    "icône de la météo « " + tirage.evenement.nom + " »");
        }

        // ---------------------------------------------------------------- Fin de partie, chargement, taille (maquettes de Julien)

        [TestCase(1, "1er")]
        [TestCase(2, "2e")]
        [TestCase(4, "4e")]
        public void LeRangSEcritALaFrancaise(int rang, string texte)
        {
            Assert.That(ContenuHud.Rang(rang), Is.EqualTo(texte));
        }

        [Test]
        public void LeClassementMetLeGagnantDevantPuisLesCookies()
        {
            // le joueur 2 a gagné avec le cookie sacré malgré moins de cookies : il passe devant ; égalité : le plus petit numéro
            var e = Etat(Phase.Finie, 0f, (1, 3), (2, 1), (3, 3), (4, 0));
            e.Gagnant = 2;
            var ordre = ContenuHud.Classement(e);
            Assert.That(ordre.ConvertAll(j => j.Joueur), Is.EqualTo(new List<int> { 2, 1, 3, 4 }));
        }

        [Test]
        public void ChaqueAstuceDeChargementSeLitSansTouche()
        {
            Assert.That(ContenuChargement.Astuces, Is.Not.Empty);
            for (int i = 0; i < ContenuChargement.Astuces.Length * 2; i++)
                Assert.That(ContenuChargement.Astuce(i), Does.StartWith("Astuce : "));
            Assert.That(ContenuChargement.Astuce(-3), Is.Not.Empty, "un nombre négatif ne casse rien");
            Assert.That(ContenuChargement.Astuce(7), Is.EqualTo(ContenuChargement.Astuce(7)));
        }

        [Test]
        public void LaTailleDuHudResteDansSesBornes()
        {
            Assert.That(new ReglagesJoueur().tailleHud, Is.EqualTo(1f));
            var r = new ReglagesJoueur { tailleHud = 4f };
            r.Borner();
            Assert.That(r.tailleHud, Is.EqualTo(ReglagesJoueur.TailleHudMax));
            r.tailleHud = 0f;   // un ancien fichier sans ce réglage
            r.Borner();
            Assert.That(r.tailleHud, Is.EqualTo(1f));
            r.tailleHud = float.NaN;
            r.Borner();
            Assert.That(r.tailleHud, Is.EqualTo(1f));
        }

        [Test]
        public void LaFinMontreLePodiumEtTonRang()
        {
            var parent = new GameObject("HUD de test");
            aDetruire.Add(parent);
            var vue = VueHud.Creer(parent.transform);
            var e = Etat(Phase.Finie, 0f, (1, 3), (2, 1), (3, 2), (4, 0));
            e.Gagnant = 2;
            e.Local = 3;
            var d = new DonneesVueHud();
            vue.Montrer(e, d);
            Assert.That(vue.FinVisible, Is.True);
            Assert.That(vue.PodiumVisible, Is.EqualTo(3), "trois marches à 4 joueurs");
            Assert.That(vue.RangLocal, Is.EqualTo("3e"), "2 (gagnant), 1, puis 3");
            Assert.That(vue.BarreTonRang, Is.False, "déjà sur le podium : pas de barre « Ton classement » en plus");
            Assert.That(vue.Taille, Is.EqualTo(ReglagesJoueur.Actuels.tailleHud), "la taille du HUD suit le réglage");

            var deux = Etat(Phase.Finie, 0f, (1, 0), (2, 2));
            deux.Gagnant = 2;
            vue.Montrer(deux, d);
            Assert.That(vue.PodiumVisible, Is.EqualTo(2));
            Assert.That(vue.RangLocal, Is.EqualTo("2e"), "le joueur local du test est le 1");
        }

        // ---------------------------------------------------------------- Repère du cookie sacré (#152)

        [Test]
        public void LeRepereSaitQuandLeCookieSacreEstHorsChamp()
        {
            Rect zone = BordEcran.ZoneLibre;
            Assert.That(BordEcran.HorsChamp(new Vector3(.5f, .5f, 8f), zone), Is.False, "au milieu de l'écran");
            Assert.That(BordEcran.HorsChamp(new Vector3(.5f, .5f, -8f), zone), Is.True, "derrière la caméra");
            Assert.That(BordEcran.HorsChamp(new Vector3(1.3f, .5f, 8f), zone), Is.True, "à droite, hors de l'écran");
            Assert.That(BordEcran.HorsChamp(new Vector3(.5f, .9f, 8f), zone), Is.True, "sous le HUD du haut");
        }

        [Test]
        public void HorsChampLeRepereSePoseAuBordDansSaDirection()
        {
            Rect zone = BordEcran.ZoneLibre;
            Vector2 p = BordEcran.AuBord(new Vector3(2f, .5f, 8f), zone, 16f / 9f, out float angle);
            Assert.That(p.x, Is.EqualTo(zone.xMax).Within(1e-4f), "à droite : bord droit");
            Assert.That(p.y, Is.EqualTo(.5f).Within(1e-4f));
            Assert.That(angle, Is.EqualTo(0f).Within(.01f), "la flèche montre la droite");

            p = BordEcran.AuBord(new Vector3(.5f, 3f, 8f), zone, 16f / 9f, out angle);
            Assert.That(p.y, Is.EqualTo(zone.yMax).Within(1e-4f), "loin devant, en haut : sous le HUD du haut");
            Assert.That(angle, Is.EqualTo(90f).Within(.01f));

            // derrière la caméra, la projection est retournée : un cookie derrière à droite se montre à gauche
            p = BordEcran.AuBord(new Vector3(.8f, .5f, -8f), zone, 16f / 9f, out angle);
            Assert.That(p.x, Is.EqualTo(zone.xMin).Within(1e-4f));
            Assert.That(Mathf.Abs(angle), Is.EqualTo(180f).Within(.01f));

            // pile derrière : vers le bas (« retourne-toi »)
            p = BordEcran.AuBord(new Vector3(.5f, .5f, -8f), zone, 16f / 9f, out angle);
            Assert.That(p.y, Is.EqualTo(zone.yMin).Within(1e-4f));
            Assert.That(angle, Is.EqualTo(-90f).Within(.01f));
        }

        [Test]
        public void PasDeRepereQuandCestToiQuiPortesLeCookieSacre()
        {
            var parent = new GameObject("HUD de test");
            aDetruire.Add(parent);
            var vue = VueHud.Creer(parent.transform);
            var e = Etat(Phase.EnJeu, 50f, (1, 0), (2, 0));
            e.Sacre = EtatCookieSacre.Porte; e.PorteurSacre = 1;
            var d = new DonneesVueHud { SacreVisible = true, SacrePorte = true, SacreChezMoi = true };
            vue.Montrer(e, d);
            Assert.That(vue.RepereSacreVisible, Is.False, "ton jeton et l'annonce le disent déjà");
            d.SacreVisible = false; d.SacreChezMoi = false;
            vue.Montrer(e, d);
            Assert.That(vue.RepereSacreVisible, Is.False, "rien à suivre");
        }

        [Test]
        public void LeHudHabilleMontreLaPartie()
        {
            var parent = new GameObject("HUD de test");
            aDetruire.Add(parent);
            var vue = VueHud.Creer(parent.transform);
            var e = Etat(Phase.EnJeu, 95f, (1, 3), (2, 1), (3, 0), (4, 5));
            e.Duree = 180f;
            e.Local = 2;
            e.TorgnoleConnue = true; e.RechargeTorgnole = 2f; e.DureeRechargeTorgnole = 5f;
            e.DashConnu = true;
            e.ObjetsConnus = true; e.Objet = null;
            var d = new DonneesVueHud();

            vue.Montrer(e, d);
            Assert.That(vue.PastillesVisibles, Is.EqualTo(4), "un jeton par joueur, à 4");
            Assert.That(vue.Couronne, Is.EqualTo(4), "la couronne sur le plus de cookies");
            Assert.That(vue.TexteMinuteur, Is.EqualTo("1:35"));
            Assert.That(vue.BarreVisible, Is.True);
            Assert.That(vue.TorgnolePrete, Is.False, "en recharge");
            Assert.That(vue.DashPret, Is.True);
            Assert.That(vue.ObjetTenu, Is.False, "pas d'objet : poche vide");
            Assert.That(vue.FinVisible, Is.False);
            Assert.That(vue.Annonces, Is.Empty);

            // le cookie sacré porté par le joueur local : la couronne passe sur lui, l'annonce le dit avec le temps de garde
            e.Sacre = EtatCookieSacre.Porte; e.PorteurSacre = 2;
            e.Objet = "Super croassement";
            vue.Montrer(e, d);
            Assert.That(vue.Couronne, Is.EqualTo(2));
            Assert.That(vue.Annonces, Has.Some.StartsWith("Tu portes le cookie sacré"));
            Assert.That(vue.ObjetTenu, Is.True, "icône trouvée d'après le nom de l'objet");

            // la météo annoncée : sa pastille et son étiquette
            e.Meteo = "Pluie tropicale"; e.MeteoAnnoncee = true; e.MeteoReste = 4.2f; e.MeteoEffets = "course plus rapide";
            vue.Montrer(e, d);
            Assert.That(vue.MeteoVisible, Is.True);
            Assert.That(vue.Annonces, Has.Some.Contains("Pluie tropicale"));

            // à 2 joueurs : deux jetons seulement
            var deux = Etat(Phase.EnJeu, 30f, (1, 0), (2, 2));
            vue.Montrer(deux, d);
            Assert.That(vue.PastillesVisibles, Is.EqualTo(2));
            Assert.That(vue.Couronne, Is.EqualTo(2));
            Assert.That(vue.BarreVisible, Is.False, "durée inconnue, ou dernière minute : pas de barre");

            // la fin : le classement remplace tout le reste
            e.Phase = Phase.Finie; e.Gagnant = 2;
            vue.Montrer(e, d);
            Assert.That(vue.FinVisible, Is.True);
            Assert.That(vue.TexteMinuteur, Is.Empty);
        }
    }
}
