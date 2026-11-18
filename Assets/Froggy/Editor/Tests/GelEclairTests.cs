using System.Collections.Generic;
using Froggy.Objets;
using Froggy.Regles;
using Froggy.Socle;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Froggy.Outils.Tests
{
    /// <summary>
    /// Gel éclair (P4-09) : le projectile touche le premier joueur devant lui (pas derrière, pas à côté, pas hors de
    /// portée, pas un intouchable, pas à travers un mur) ; le gelé ne bouge plus 5 s puis repart ; un coup brise la glace
    /// (choix de Lucas, 18 novembre 2026) ; un second gel ne rallonge pas le premier.
    /// </summary>
    public class GelEclairTests
    {
        private ReglagesGelEclair reglages;
        private Scene scene;

        [SetUp]
        public void Preparer() => reglages = ScriptableObject.CreateInstance<ReglagesGelEclair>();

        [TearDown]
        public void Ranger()
        {
            if (scene.IsValid()) EditorSceneManager.ClosePreviewScene(scene);
            Object.DestroyImmediate(reglages);
        }

        private static List<(int, Vector3, bool)> Cibles(params (int, Vector3, bool)[] c) => new List<(int, Vector3, bool)>(c);

        [Test]
        public void ToucheLePremierDevantLui()
        {
            var c = Cibles((2, new Vector3(0f, 0f, 8f), false), (3, new Vector3(0.3f, 0f, 4f), false));
            Assert.That(ReglesGelEclair.Toucher(Vector3.zero, Vector3.forward, 15f, 0.3f, c, out float a), Is.EqualTo(3));
            Assert.That(a, Is.EqualTo(4f).Within(1f));
        }

        [Test]
        public void NeToucheNiDerriereNiACoteNiTropLoin()
        {
            Assert.That(ReglesGelEclair.Toucher(Vector3.zero, Vector3.forward, 15f, 0.3f, Cibles((2, new Vector3(0f, 0f, -3f), false)), out _), Is.EqualTo(-1), "derrière");
            Assert.That(ReglesGelEclair.Toucher(Vector3.zero, Vector3.forward, 15f, 0.3f, Cibles((2, new Vector3(2f, 0f, 5f), false)), out _), Is.EqualTo(-1), "à 2 m sur le côté");
            Assert.That(ReglesGelEclair.Toucher(Vector3.zero, Vector3.forward, 15f, 0.3f, Cibles((2, new Vector3(0f, 0f, 20f), false)), out _), Is.EqualTo(-1), "hors de portée");
        }

        [Test]
        public void TraverseUnIntouchable()
        {
            var c = Cibles((2, new Vector3(0f, 0f, 3f), true), (3, new Vector3(0f, 0f, 6f), false));
            Assert.That(ReglesGelEclair.Toucher(Vector3.zero, Vector3.forward, 15f, 0.3f, c, out _), Is.EqualTo(3));
        }

        [Test]
        public void GeleCinqSecondesPuisRepart()
        {
            var r = new ReglesGelEclair();
            var liberes = new List<int>();
            Assert.That(r.Geler(2, 5f), Is.True);
            for (int i = 0; i < 245; i++) r.Pas(0.02f, liberes);   // 4,9 s
            Assert.That(r.EstGele(2), Is.True);
            for (int i = 0; i < 10; i++) r.Pas(0.02f, liberes);
            Assert.That(r.EstGele(2), Is.False, "libre au bout de 5 s");
        }

        [Test]
        public void UnSecondGelNeRallongePas()
        {
            var r = new ReglesGelEclair();
            r.Geler(2, 5f);
            r.Pas(3f, null);
            Assert.That(r.Geler(2, 5f), Is.False);
            Assert.That(r.Reste(2), Is.EqualTo(2f).Within(1e-4f));
        }

        [Test]
        public void LeProjectileGeleLaCibleQuiNeBougePlus()
        {
            var (tireur, cible, effet) = Duel(3f);
            effet.Tirer(tireur.gameObject);
            for (int i = 0; i < 20; i++) effet.Pas(0.02f);   // 0,4 s : 6,4 m à 16 m/s
            var m = cible.GetComponent<Modificateurs>();
            Assert.That(effet.EstGele(cible.gameObject), Is.True);
            Assert.That(m.Vitesse, Is.EqualTo(0f), "il ne bouge plus");
            Assert.That(m.Saut, Is.EqualTo(0f), "il ne saute plus");
            Assert.That(m.Entrave, Is.True, "ni torgnole ni objet");
            Assert.That(effet.ProjectilesEnVol, Is.EqualTo(0), "le projectile s'arrête sur lui");
            for (int i = 0; i < 250; i++) effet.Pas(0.02f);
            Assert.That(effet.EstGele(cible.gameObject), Is.False);
            Assert.That(m.Vitesse, Is.EqualTo(1f), "et repart au bout de 5 s");
            Assert.That(m.Entrave, Is.False, "et peut de nouveau agir");
        }

        [Test]
        public void UnCoupBriseLaGlace()
        {
            var (tireur, cible, effet) = Duel(2f);
            Assert.That(effet.Geler(cible.gameObject, Vector3.forward), Is.True);
            effet.Reagir(new EvenementPartie(TypeEvenement.CoupDonne, tireur.gameObject, cible.gameObject, cible.transform.position));
            Assert.That(effet.EstGele(cible.gameObject), Is.False, "la torgnole le libère aussitôt");
            Assert.That(cible.GetComponent<Modificateurs>().Vitesse, Is.EqualTo(1f));
        }

        [Test]
        public void UnMurArreteLeProjectile()
        {
            var (tireur, cible, effet) = Duel(6f);
            var mur = Objet("Mur", new Vector3(0f, 0.6f, 3f));
            mur.AddComponent<BoxCollider>().size = new Vector3(4f, 3f, 0.5f);
            Physics.SyncTransforms();
            effet.Tirer(tireur.gameObject);
            for (int i = 0; i < 40; i++) effet.Pas(0.02f);
            Assert.That(effet.EstGele(cible.gameObject), Is.False, "caché derrière le mur");
            Assert.That(effet.ProjectilesEnVol, Is.EqualTo(0));
        }

        // un tireur à l'origine tourné vers +z, une cible à « distance » devant, dans une scène physique à part
        private (PorteurCookies tireur, PorteurCookies cible, EffetGelEclair effet) Duel(float distance)
        {
            scene = EditorSceneManager.NewPreviewScene();
            var tireur = Objet("Tireur", Vector3.zero).AddComponent<PorteurCookies>();
            var cible = Objet("Cible", new Vector3(0f, 0f, distance)).AddComponent<PorteurCookies>();
            tireur.Numeroter(1);
            cible.Numeroter(2);
            var effet = Objet("Effet", Vector3.zero).AddComponent<EffetGelEclair>();
            effet.Regler(reglages, new[] { tireur.gameObject, cible.gameObject });
            return (tireur, cible, effet);
        }

        private GameObject Objet(string nom, Vector3 position)
        {
            var go = new GameObject(nom);
            go.transform.position = position;
            SceneManager.MoveGameObjectToScene(go, scene);
            return go;
        }
    }
}
