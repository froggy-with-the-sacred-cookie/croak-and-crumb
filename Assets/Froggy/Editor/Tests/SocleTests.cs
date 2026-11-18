using System.Collections.Generic;
using Froggy.Deplacement;
using Froggy.Socle;
using Froggy.Template;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Froggy.Outils.Tests
{
    /// <summary>
    /// Socle commun (T-01) : les modificateurs de vitesse et de saut se multiplient, se remplacent par source et se
    /// retirent ; le contrôleur les applique (vitesse de course, hauteur de saut à 30, 60 et 144 pas par seconde) ;
    /// les événements de partie arrivent à tous les écouteurs, même si l'un d'eux plante.
    /// </summary>
    public class SocleTests
    {
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
        public void LesModificateursSeMultiplient()
        {
            var m = Nouveau();
            object pluie = new object(), cookies = new object();
            Assert.That(m.Vitesse, Is.EqualTo(1f), "sans modificateur, rien ne change");
            Assert.That(m.Saut, Is.EqualTo(1f));
            m.Poser(pluie, vitesse: 1.3f);
            m.Poser(cookies, vitesse: 0.8f, saut: 0.5f);
            Assert.That(m.Vitesse, Is.EqualTo(1.04f).Within(1e-5f));
            Assert.That(m.Saut, Is.EqualTo(0.5f).Within(1e-5f));
            Assert.That(m.Nombre, Is.EqualTo(2));
        }

        [Test]
        public void UneSourceRemplaceSonModificateurEtLeRetire()
        {
            var m = Nouveau();
            object pluie = new object(), cookies = new object();
            int changements = 0;
            m.Change += () => changements++;
            m.Poser(pluie, vitesse: 1.3f);
            m.Poser(cookies, vitesse: 0.8f);
            m.Poser(pluie, vitesse: 1.5f);   // la pluie forcit : remplace, ne s'ajoute pas
            Assert.That(m.Nombre, Is.EqualTo(2));
            Assert.That(m.Vitesse, Is.EqualTo(1.2f).Within(1e-5f));
            m.Retirer(pluie);
            Assert.That(m.Vitesse, Is.EqualTo(0.8f).Within(1e-5f), "il ne reste que les cookies");
            m.Retirer(pluie);   // deux fois : sans effet
            m.Retirer(new object());
            Assert.That(changements, Is.EqualTo(4), "prévenu à chaque changement du total, pas plus");
            m.Vider();
            Assert.That(m.Vitesse, Is.EqualTo(1f));
            Assert.That(m.Nombre, Is.EqualTo(0));
        }

        [Test]
        public void UnFacteurNegatifVautZero()
        {
            var m = Nouveau();
            m.Poser(this, vitesse: -2f, saut: -1f);
            Assert.That(m.Vitesse, Is.EqualTo(0f));
            Assert.That(m.Saut, Is.EqualTo(0f));
        }

        [Test]
        public void LaVitesseDeCourseSuitLeModificateur()
        {
            var (joueur, physique, course) = Terrain();
            var corps = joueur.GetComponent<Rigidbody>();
            float dt = 1f / 60f;
            joueur.Imposer(new DeplacementWow.Entrees { deplacement = new Vector2(0f, 1f) });
            for (int i = 0; i < 60; i++) Avancer(joueur, physique, dt);
            Assert.That(corps.linearVelocity.z, Is.EqualTo(course.moveSpeed).Within(0.05f), "sans modificateur");

            var m = joueur.GetComponent<Modificateurs>();
            Assert.That(m != null, "le contrôleur devait donner un composant Modificateurs à Froggy");
            m.Poser(this, vitesse: 1.5f);
            for (int i = 0; i < 60; i++) Avancer(joueur, physique, dt);
            Assert.That(corps.linearVelocity.z, Is.EqualTo(course.moveSpeed * 1.5f).Within(0.05f), "×1,5");

            m.Retirer(this);
            for (int i = 0; i < 60; i++) Avancer(joueur, physique, dt);
            Assert.That(corps.linearVelocity.z, Is.EqualTo(course.moveSpeed).Within(0.05f), "retiré : vitesse d'origine");
        }

        [TestCase(30)]
        [TestCase(60)]
        [TestCase(144)]
        public void LaHauteurDuSautSuitLeModificateur(int pasParSeconde)
        {
            const float facteur = 2f;
            float dt = 1f / pasParSeconde;
            var (joueur, physique, course) = Terrain();
            var corps = joueur.GetComponent<Rigidbody>();
            for (int i = 0; i < pasParSeconde / 2; i++) Avancer(joueur, physique, dt);   // posé
            Assert.That(joueur.AuSol, Is.True, "pas posé au sol");
            joueur.GetComponent<Modificateurs>().Poser(this, saut: facteur);
            float depart = corps.position.y, sommet = depart, vol = 0f;
            joueur.Imposer(new DeplacementWow.Entrees { sautAppuye = true, sautTenu = true });
            Avancer(joueur, physique, dt);
            joueur.Imposer(new DeplacementWow.Entrees { sautTenu = true });
            for (int i = 0; i < pasParSeconde * 4 && !(joueur.AuSol && vol > 0.1f); i++)
            {
                Avancer(joueur, physique, dt);
                vol += dt;
                sommet = Mathf.Max(sommet, corps.position.y);
            }
            float voulue = course.jumpHeight * facteur;
            Assert.That(sommet - depart, Is.EqualTo(voulue).Within(voulue * 0.08f), "hauteur ×2");
        }

        [Test]
        public void UnEvenementArriveATousLesEcouteurs()
        {
            var froggy = new GameObject("Froggy");
            aDetruire.Add(froggy);
            froggy.transform.position = new Vector3(3f, 0f, 4f);
            var recus = new List<EvenementPartie>();
            int autre = 0;
            System.Action<EvenementPartie> a = e => recus.Add(e), b = e => autre++;
            EvenementsPartie.Emis += a;
            EvenementsPartie.Emis += b;
            try
            {
                EvenementsPartie.Emettre(TypeEvenement.CookieRamasse, auteur: froggy);
                Assert.That(recus.Count, Is.EqualTo(1));
                Assert.That(autre, Is.EqualTo(1));
                Assert.That(recus[0].Type, Is.EqualTo(TypeEvenement.CookieRamasse));
                Assert.That(recus[0].Auteur, Is.SameAs(froggy));
                Assert.That(recus[0].Position, Is.EqualTo(froggy.transform.position), "sans position donnée : celle de l'auteur");

                EvenementsPartie.Emis -= a;
                EvenementsPartie.Emettre(TypeEvenement.CookieLache, position: Vector3.one);
                Assert.That(recus.Count, Is.EqualTo(1), "désabonné : ne reçoit plus");
                Assert.That(autre, Is.EqualTo(2));
            }
            finally
            {
                EvenementsPartie.Emis -= a;
                EvenementsPartie.Emis -= b;
            }
        }

        [Test]
        public void UnEcouteurQuiPlanteNeBloquePasLesAutres()
        {
            int recus = 0;
            System.Action<EvenementPartie> casse = e => throw new System.InvalidOperationException("écouteur cassé (test)");
            System.Action<EvenementPartie> sain = e => recus++;
            EvenementsPartie.Emis += casse;
            EvenementsPartie.Emis += sain;
            try
            {
                LogAssert.Expect(LogType.Exception, new System.Text.RegularExpressions.Regex("écouteur cassé"));
                EvenementsPartie.Emettre(TypeEvenement.ObjetUtilise);
                Assert.That(recus, Is.EqualTo(1));
            }
            finally
            {
                EvenementsPartie.Emis -= casse;
                EvenementsPartie.Emis -= sain;
            }
        }

        private Modificateurs Nouveau()
        {
            var go = new GameObject("Froggy");
            aDetruire.Add(go);
            return go.AddComponent<Modificateurs>();
        }

        // le même terrain que DeplacementWowTests : scène d'aperçu avec sa propre physique, copies des réglages
        private (DeplacementWow, PhysicsScene, TemplateMovementSettings) Terrain()
        {
            var course = ScriptableObject.CreateInstance<TemplateMovementSettings>();
            var wow = ScriptableObject.CreateInstance<ReglagesWow>();
            aDetruire.Add(course);
            aDetruire.Add(wow);
            scene = EditorSceneManager.NewPreviewScene();
            var physique = scene.GetPhysicsScene();
            var sol = new GameObject("Sol");
            SceneManager.MoveGameObjectToScene(sol, scene);
            sol.AddComponent<BoxCollider>().size = new Vector3(400f, 1f, 400f);
            sol.transform.position = new Vector3(0f, -0.5f, 0f);
            var go = new GameObject("Joueur");
            SceneManager.MoveGameObjectToScene(go, scene);
            go.transform.position = new Vector3(0f, 0.52f, 0f);
            // sans frottement, comme le préfabriqué du joueur (TemplateNoFriction) : sinon le sol mange un peu de vitesse
            var glisse = new PhysicsMaterial { dynamicFriction = 0f, staticFriction = 0f, frictionCombine = PhysicsMaterialCombine.Minimum };
            aDetruire.Add(glisse);
            go.AddComponent<BoxCollider>().sharedMaterial = glisse;
            go.AddComponent<Rigidbody>().freezeRotation = true;
            var joueur = go.AddComponent<DeplacementWow>();
            joueur.Regler(course, wow);
            joueur.Imposer(new DeplacementWow.Entrees());
            return (joueur, physique, course);
        }

        private static void Avancer(DeplacementWow joueur, PhysicsScene physique, float dt)
        {
            joueur.Pas(dt);
            physique.Simulate(dt);
        }
    }
}
