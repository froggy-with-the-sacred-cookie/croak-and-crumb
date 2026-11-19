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
    /// Réapparition (P3-04). Les règles seules (ReglesReapparition) : chute repérée sous la hauteur, retour après le délai
    /// au point le plus loin des autres, intouchable un moment, retour en jeu en moins de 3 s chute comprise. Puis dans une
    /// scène (ApparitionJoueurs) : Froggy posée sur son repère au départ, ramenée après une chute, et qu'on ne peut pas
    /// faire lâcher tant qu'elle est intouchable.
    /// </summary>
    public class ReapparitionTests
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

        private static readonly (int, Vector3)[] DeuxPoints = { (1, new Vector3(0f, 0f, 0f)), (2, new Vector3(20f, 0f, 0f)) };

        private static List<(int, Vector3)> Positions(params (int, Vector3)[] p) => new List<(int, Vector3)>(p);

        private static List<ReglesReapparition.Reapparu> Avancer(ReglesReapparition regles, float secondes, List<(int, Vector3)> positions)
        {
            var reapparus = new List<ReglesReapparition.Reapparu>();
            int pas = Mathf.RoundToInt(secondes / Dt);
            for (int i = 0; i < pas; i++) regles.Pas(Dt, positions, reapparus);
            return reapparus;
        }

        [Test]
        public void ChaqueJoueurPartDeSonPoint()
        {
            var regles = new ReglesReapparition(DeuxPoints, -5f, 1f, 2f);
            Assert.That(regles.Depart(2, out Vector3 p), Is.True);
            Assert.That(p, Is.EqualTo(new Vector3(20f, 0f, 0f)));
            Assert.That(regles.Depart(3, out p), Is.True, "un joueur sans point à lui en prend un dans l'ordre");
            Assert.That(p, Is.EqualTo(Vector3.zero));
            Assert.That(new ReglesReapparition(new (int, Vector3)[0], -5f, 1f, 2f).Depart(1, out _), Is.False);
        }

        [Test]
        public void SousLaHauteurDeChuteOnRevientApresLeDelai()
        {
            var regles = new ReglesReapparition(DeuxPoints, -5f, 1f, 2f);
            var positions = Positions((1, new Vector3(3f, -4.9f, 0f)));
            Assert.That(Avancer(regles, 0.5f, positions), Is.Empty);
            Assert.That(regles.Etat(1), Is.EqualTo(EtatReapparition.EnJeu), "au-dessus de la hauteur de chute, on joue");

            positions[0] = (1, new Vector3(3f, -5.1f, 0f));
            Assert.That(Avancer(regles, 0.9f, positions), Is.Empty);
            Assert.That(regles.Etat(1), Is.EqualTo(EtatReapparition.Tombe));
            Assert.That(regles.Intouchable(1), Is.True, "un joueur tombé ne se frappe pas");

            var reapparus = Avancer(regles, 0.2f, positions);
            Assert.That(reapparus.Count, Is.EqualTo(1), "1 s après la chute, il réapparaît, une seule fois");
            Assert.That(reapparus[0].Joueur, Is.EqualTo(1));
        }

        [Test]
        public void IntouchablePuisDeNouveauEnJeu()
        {
            var regles = new ReglesReapparition(DeuxPoints, -5f, 1f, 2f);
            var positions = Positions((1, new Vector3(0f, -6f, 0f)));
            Avancer(regles, 1f, positions);
            Assert.That(regles.Etat(1), Is.EqualTo(EtatReapparition.Tombe));
            positions[0] = (1, new Vector3(0f, 0.5f, 0f));   // pendant la chute, la hauteur ne compte plus
            Avancer(regles, 0.04f, positions);
            Assert.That(regles.Etat(1), Is.EqualTo(EtatReapparition.Intouchable));
            Avancer(regles, 1.9f, positions);
            Assert.That(regles.Intouchable(1), Is.True, "intouchable pendant 2 s");
            Avancer(regles, 0.2f, positions);
            Assert.That(regles.Etat(1), Is.EqualTo(EtatReapparition.EnJeu));
            Assert.That(regles.Intouchable(1), Is.False);
        }

        [Test]
        public void OnReapparaitLoinDesAutres()
        {
            var regles = new ReglesReapparition(DeuxPoints, -5f, 0f, 2f);
            // l'autre joueur est près du point 1 : on revient au point 2
            var positions = Positions((1, new Vector3(0f, -6f, 0f)), (2, new Vector3(2f, 0.5f, 0f)));
            var reapparus = Avancer(regles, 0.04f, positions);
            Assert.That(reapparus[0].Point, Is.EqualTo(new Vector3(20f, 0f, 0f)));

            // près du point 2 : on revient au point 1
            regles = new ReglesReapparition(DeuxPoints, -5f, 0f, 2f);
            positions = Positions((1, new Vector3(0f, -6f, 0f)), (2, new Vector3(18f, 0.5f, 0f)));
            reapparus = Avancer(regles, 0.04f, positions);
            Assert.That(reapparus[0].Point, Is.EqualTo(Vector3.zero));
        }

        [Test]
        public void AvecLesReglagesOnRevientEnMoinsDeTroisSecondes()
        {
            var reglages = ScriptableObject.CreateInstance<ReglagesReapparition>();
            aDetruire.Add(reglages);
            // P3-04 : « on tombe hors de l'arène et on revient en jeu en moins de 3 s », chute depuis le sol comprise
            Assert.That(reglages.TempsDeChute + reglages.delai, Is.LessThan(3f));
            Assert.That(reglages.dureeIntouchable, Is.GreaterThan(0f), "sans pouvoir être frappé à l'arrivée");
        }

        [Test]
        public void DansUneSceneFroggyRevientEtNePeutPasEtreFrappee()
        {
            var reglagesCookies = ScriptableObject.CreateInstance<ReglagesCookies>();
            var reglages = ScriptableObject.CreateInstance<ReglagesReapparition>();
            aDetruire.Add(reglagesCookies);
            aDetruire.Add(reglages);
            scene = EditorSceneManager.NewPreviewScene();

            var j1 = Repere(RepereArene.Sorte.Joueur, 1, new Vector3(0f, 0f, 0f));
            var j2 = Repere(RepereArene.Sorte.Joueur, 2, new Vector3(20f, 0f, 0f));
            var centre = Repere(RepereArene.Sorte.Centre, 1, new Vector3(10f, 0f, 0f));
            var cookie = Repere(RepereArene.Sorte.Cookie, 1, new Vector3(0f, 0f, 0f));   // sur l'apparition de Froggy

            var froggy = Objet("Froggy", new Vector3(-5f, 3f, 7f));
            froggy.AddComponent<BoxCollider>();   // 1 m de haut, centrée : 0,5 m du pivot aux pieds
            froggy.AddComponent<Rigidbody>();
            var capsule = Objet("Capsule", Vector3.zero).AddComponent<MeshRenderer>();   // la forme provisoire
            capsule.transform.SetParent(froggy.transform, false);
            var modele = Objet("Modèle", Vector3.zero).AddComponent<MeshRenderer>();     // le modèle choisi
            modele.transform.SetParent(froggy.transform, false);
            capsule.enabled = false;   // FroggyApparence a choisi le modèle : la capsule est cachée
            var autre = Objet("Autre", new Vector3(2f, 0.5f, 0f));   // sans Rigidbody : un mannequin, il ne bouge pas
            var cookies = Objet("Cookies", Vector3.zero).AddComponent<PartieCookies>();
            cookies.Regler(reglagesCookies);
            cookies.Demarrer(new[] { cookie }, new[] { froggy, autre });
            var porteur = froggy.GetComponent<PorteurCookies>();
            var apparition = Objet("Apparition", Vector3.zero).AddComponent<ApparitionJoueurs>();
            apparition.Regler(reglages, cookies);
            apparition.Demarrer(new[] { j1, j2, centre, cookie });

            Assert.That(porteur.Joueur, Is.EqualTo(1));
            Assert.That(Vector3.Distance(froggy.transform.position, new Vector3(0f, 0.52f, 0f)), Is.LessThan(1e-3f),
                "au départ, Froggy est posée debout sur l'apparition du joueur 1");
            Assert.That(Vector3.Angle(froggy.transform.forward, Vector3.right), Is.LessThan(1f), "tournée vers le centre");
            Assert.That(autre.transform.position, Is.EqualTo(new Vector3(2f, 0.5f, 0f)), "le mannequin reste où il est");
            cookies.Pas(Dt);
            Assert.That(porteur.Nombre, Is.EqualTo(1), "elle ramasse le cookie posé là");

            var recus = new List<EvenementPartie>();
            System.Action<EvenementPartie> ecoute = e => { if (e.Type == TypeEvenement.JoueurReapparu) recus.Add(e); };
            EvenementsPartie.Emis += ecoute;
            try
            {
                froggy.transform.position = new Vector3(5f, -10f, 0f);   // tombée hors de l'arène
                for (int i = 0; i < 25; i++) apparition.Pas(Dt);
                Assert.That(porteur.Intouchable, Is.True, "tombée : on ne peut pas la frapper");
                Assert.That(cookies.FaireLacher(porteur, Vector3.right), Is.False);

                for (int i = 0; i < 27; i++) apparition.Pas(Dt);   // un peu plus de 1 s après la chute
                Assert.That(recus.Count, Is.EqualTo(1), "« joueur réapparu » est émis");
                Assert.That(recus[0].Auteur, Is.SameAs(froggy));
                Assert.That(Vector3.Distance(froggy.transform.position, new Vector3(20f, 0.52f, 0f)), Is.LessThan(1e-3f),
                    "elle revient au point le plus loin du mannequin");
                Assert.That(Vector3.Angle(froggy.transform.forward, Vector3.left), Is.LessThan(1f), "tournée vers le centre");
                Assert.That(porteur.Intouchable, Is.True);
                Assert.That(cookies.FaireLacher(porteur, Vector3.right), Is.False, "intouchable : le coup ne lui fait rien lâcher");
                Assert.That(porteur.Nombre, Is.EqualTo(1), "elle a gardé son cookie");

                bool cachee = false;
                for (int i = 0; i < 105; i++) { apparition.Pas(Dt); cachee |= modele.forceRenderingOff; }   // 2,1 s plus tard
                Assert.That(cachee, Is.True, "intouchable, elle clignote");
                Assert.That(porteur.Intouchable, Is.False);
                Assert.That(modele.forceRenderingOff, Is.False, "le clignotement fini, elle est visible");
                Assert.That(capsule.enabled, Is.False, "le clignotement ne rallume pas la capsule cachée par le choix du modèle");
                Assert.That(modele.enabled, Is.True);
                Assert.That(cookies.FaireLacher(porteur, Vector3.right), Is.True, "de nouveau touchable");
            }
            finally { EvenementsPartie.Emis -= ecoute; }
        }

        // une Froggy (boîte de 1 m, Rigidbody) inscrite comme joueur 1, et une apparition sur deux repères et un centre
        private (ApparitionJoueurs apparition, PorteurCookies porteur, MeshRenderer modele, PartieCookies cookies) Arene(bool cinematique)
        {
            var reglagesCookies = ScriptableObject.CreateInstance<ReglagesCookies>();
            var reglages = ScriptableObject.CreateInstance<ReglagesReapparition>();
            aDetruire.Add(reglagesCookies);
            aDetruire.Add(reglages);
            scene = EditorSceneManager.NewPreviewScene();
            var j1 = Repere(RepereArene.Sorte.Joueur, 1, new Vector3(0f, 0f, 0f));
            var j2 = Repere(RepereArene.Sorte.Joueur, 2, new Vector3(20f, 0f, 0f));
            var centre = Repere(RepereArene.Sorte.Centre, 1, new Vector3(10f, 0f, 0f));
            var froggy = Objet("Froggy", new Vector3(-5f, 3f, 7f));
            froggy.AddComponent<BoxCollider>();
            froggy.AddComponent<Rigidbody>().isKinematic = cinematique;
            var modele = Objet("Modèle", Vector3.zero).AddComponent<MeshRenderer>();
            modele.transform.SetParent(froggy.transform, false);
            var autre = Objet("Autre", new Vector3(2f, 0.5f, 0f));   // un mannequin près du point du joueur 1
            var cookies = Objet("Cookies", Vector3.zero).AddComponent<PartieCookies>();
            cookies.Regler(reglagesCookies);
            cookies.Demarrer(new RepereArene[0], new[] { froggy, autre });
            var apparition = Objet("Apparition", Vector3.zero).AddComponent<ApparitionJoueurs>();
            apparition.Regler(reglages, cookies);
            apparition.PoserAuDepart = false;   // en réseau, le salon a posé les joueurs
            apparition.Demarrer(new[] { j1, j2, centre });
            return (apparition, froggy.GetComponent<PorteurCookies>(), modele, cookies);
        }

        [Test]
        public void EnLigneLHoteFaitPoserChezLuiLaFroggyDUnAutre()
        {
            var (apparition, porteur, _, _) = Arene(cinematique: true);   // chez l'hôte, la copie d'une Froggy distante
            Assert.That(porteur.transform.position, Is.EqualTo(new Vector3(-5f, 3f, 7f)), "#144 : le salon l'a posée, on n'y touche pas");

            var envois = new List<(PorteurCookies, Vector3, Quaternion)>();
            apparition.PoserAilleurs = (p, ou, rot) => envois.Add((p, ou, rot));
            porteur.transform.position = new Vector3(5f, -10f, 0f);   // tombée hors de l'arène
            for (int i = 0; i < 52; i++) apparition.Pas(Dt);   // un peu plus de 1 s
            Assert.That(envois.Count, Is.EqualTo(1), "sa place de réapparition part chez son propriétaire");
            Assert.That(envois[0].Item1, Is.SameAs(porteur));
            Assert.That(Vector3.Distance(envois[0].Item2, new Vector3(20f, 0.52f, 0f)), Is.LessThan(1e-3f), "loin du mannequin");
            Assert.That(Vector3.Angle(envois[0].Item3 * Vector3.forward, Vector3.left), Is.LessThan(1f), "tournée vers le centre");

            // le réseau n'a pas encore apporté sa nouvelle place : la copie suit encore la chute
            for (int i = 0; i < 20; i++) { porteur.transform.position = new Vector3(5f, -12f, 0f); apparition.Pas(Dt); }
            Assert.That(envois.Count, Is.EqualTo(1), "pas de deuxième réapparition pendant le trajet du message");
            Assert.That(apparition.Etat(porteur.Joueur), Is.EqualTo(EtatReapparition.Intouchable));
            porteur.transform.position = envois[0].Item2;   // la place arrive
            apparition.Pas(Dt);
            Assert.That(porteur.Intouchable, Is.True, "réapparue : intouchable un moment");
        }

        [Test]
        public void EnMiroirRienNEstDecideIciEtLEtatRecuSeMontre()
        {
            var (apparition, porteur, modele, _) = Arene(cinematique: false);
            apparition.Miroir = true;
            var recus = new List<EvenementPartie>();
            System.Action<EvenementPartie> ecoute = e => { if (e.Type == TypeEvenement.JoueurReapparu) recus.Add(e); };
            EvenementsPartie.Emis += ecoute;
            try
            {
                porteur.transform.position = new Vector3(5f, -10f, 0f);
                for (int i = 0; i < 60; i++) apparition.Pas(Dt);
                Assert.That(porteur.transform.position, Is.EqualTo(new Vector3(5f, -10f, 0f)), "#144 : c'est l'hôte qui la remet en jeu");
                Assert.That(recus, Is.Empty, "« joueur réapparu » arrive de l'hôte, pas d'ici");
                Assert.That(porteur.Intouchable, Is.False);

                apparition.Refleter(porteur.Joueur, EtatReapparition.Intouchable);
                bool cachee = false;
                for (int i = 0; i < 30; i++) { apparition.Pas(Dt); cachee |= modele.forceRenderingOff; }
                Assert.That(porteur.Intouchable, Is.True, "l'état de l'hôte se montre ici");
                Assert.That(cachee, Is.True, "elle clignote");

                apparition.Poser(porteur, new Vector3(20f, 0.52f, 0f), Quaternion.identity);   // l'ordre de l'hôte
                Assert.That(porteur.transform.position, Is.EqualTo(new Vector3(20f, 0.52f, 0f)));
                apparition.Refleter(porteur.Joueur, EtatReapparition.EnJeu);
                apparition.Pas(Dt);
                Assert.That(porteur.Intouchable, Is.False);
                Assert.That(modele.forceRenderingOff, Is.False, "visible de nouveau");
            }
            finally { EvenementsPartie.Emis -= ecoute; }
        }

        private RepereArene Repere(RepereArene.Sorte sorte, int joueur, Vector3 position)
        {
            var r = Objet(sorte.ToString(), position).AddComponent<RepereArene>();
            r.sorte = sorte;
            r.joueur = joueur;
            return r;
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
