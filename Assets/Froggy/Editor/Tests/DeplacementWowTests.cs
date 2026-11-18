using Froggy.Deplacement;
using Froggy.Template;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Froggy.Outils.Tests
{
    /// <summary>
    /// Déplacement à la WoW (skill ressenti-mouvement, section 5) : vitesses dans le repère du cap, recul plus lent,
    /// orientation en diagonale ; saut mesuré (hauteur, durée de vol) dans une scène physique à part, à 30, 60 et 144 pas
    /// par seconde, avec des copies des réglages ; élan figé en l'air seulement si on le coche.
    /// </summary>
    public class DeplacementWowTests
    {
        private TemplateMovementSettings course;
        private ReglagesWow wow;
        private Scene scene;

        [SetUp]
        public void Preparer()
        {
            course = ScriptableObject.CreateInstance<TemplateMovementSettings>();   // valeurs par défaut du Template
            wow = ScriptableObject.CreateInstance<ReglagesWow>();
        }

        [TearDown]
        public void Ranger()
        {
            if (scene.IsValid()) EditorSceneManager.ClosePreviewScene(scene);
            Object.DestroyImmediate(course);
            Object.DestroyImmediate(wow);
        }

        [Test]
        public void VitessesDansLeRepereDuCap()
        {
            Vector3 avant = DeplacementWow.Vitesse(new Vector2(0f, 1f), 90f, false, course, wow);
            Assert.That(Vector3.Distance(avant, new Vector3(course.moveSpeed, 0f, 0f)), Is.LessThan(1e-4f), "cap 90 : vers +x");
            Vector3 recul = DeplacementWow.Vitesse(new Vector2(0f, -1f), 0f, false, course, wow);
            Assert.That(recul.z, Is.EqualTo(-course.moveSpeed * wow.facteurRecul).Within(1e-4f), "recul plus lent");
            Vector3 cote = DeplacementWow.Vitesse(new Vector2(1f, 0f), 0f, false, course, wow);
            Assert.That(cote.x, Is.EqualTo(course.moveSpeed).Within(1e-4f), "pas de côté à pleine vitesse");
            Vector3 diag = DeplacementWow.Vitesse(new Vector2(1f, 1f), 0f, false, course, wow);
            Assert.That(diag.magnitude, Is.EqualTo(course.moveSpeed).Within(1e-4f), "la diagonale ne va pas plus vite");
        }

        [Test]
        public void FaceALAvantSaufEnDiagonale()
        {
            Assert.That(DeplacementWow.Orientation(new Vector2(0f, -1f), 45f), Is.EqualTo(0f), "en reculant, face à l'avant");
            Assert.That(DeplacementWow.Orientation(new Vector2(1f, 0f), 45f), Is.EqualTo(0f));
            Assert.That(DeplacementWow.Orientation(new Vector2(1f, 1f), 45f), Is.EqualTo(45f));
            Assert.That(DeplacementWow.Orientation(new Vector2(1f, -1f), 45f), Is.EqualTo(-45f), "recul en diagonale : le dos vers la course");
        }

        [TestCase(30)]
        [TestCase(60)]
        [TestCase(144)]
        public void SautALaHauteurVoulue(int pasParSeconde)
        {
            float dt = 1f / pasParSeconde;
            var (joueur, physique) = Terrain();
            var corps = joueur.GetComponent<Rigidbody>();
            for (int i = 0; i < pasParSeconde / 2; i++) Avancer(joueur, physique, dt);   // posé
            Assert.That(joueur.AuSol, Is.True, "pas posé au sol");
            float depart = corps.position.y, sommet = depart, vol = 0f;
            joueur.Imposer(new DeplacementWow.Entrees { sautAppuye = true, sautTenu = true });
            Avancer(joueur, physique, dt);
            joueur.Imposer(new DeplacementWow.Entrees { sautTenu = true });
            for (int i = 0; i < pasParSeconde * 3 && !(joueur.AuSol && vol > 0.1f); i++)
            {
                Avancer(joueur, physique, dt);
                vol += dt;
                sommet = Mathf.Max(sommet, corps.position.y);
            }
            float g = -Physics.gravity.y;
            Assert.That(sommet - depart, Is.EqualTo(course.jumpHeight).Within(course.jumpHeight * 0.08f), "hauteur");
            Assert.That(vol, Is.EqualTo(2f * Mathf.Sqrt(2f * course.jumpHeight / g)).Within(0.12f), "durée de vol");
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ElanFigeEnLairSeulementSiCoche(bool fige)
        {
            wow.elanFigeEnLair = fige;
            var (joueur, physique) = Terrain();
            var corps = joueur.GetComponent<Rigidbody>();
            float dt = 1f / 60f;
            for (int i = 0; i < 30; i++) Avancer(joueur, physique, dt);
            joueur.Imposer(new DeplacementWow.Entrees { sautAppuye = true, sautTenu = true });   // saut sur place
            Avancer(joueur, physique, dt);
            joueur.Imposer(new DeplacementWow.Entrees { deplacement = new Vector2(1f, 0f), sautTenu = true });   // puis à droite, en l'air
            for (int i = 0; i < 12; i++) Avancer(joueur, physique, dt);
            if (fige) Assert.That(Mathf.Abs(corps.linearVelocity.x), Is.LessThan(0.01f), "l'élan devait rester figé");
            else Assert.That(corps.linearVelocity.x, Is.GreaterThan(1f), "le contrôle aérien du Template devait jouer");
        }

        private (DeplacementWow, PhysicsScene) Terrain()
        {
            scene = EditorSceneManager.NewPreviewScene();
            var physique = scene.GetPhysicsScene();
            Assert.That(physique != Physics.defaultPhysicsScene, "la scène de test n'a pas sa propre physique");
            var sol = new GameObject("Sol");
            SceneManager.MoveGameObjectToScene(sol, scene);
            sol.AddComponent<BoxCollider>().size = new Vector3(40f, 1f, 40f);
            sol.transform.position = new Vector3(0f, -0.5f, 0f);
            var go = new GameObject("Joueur");
            SceneManager.MoveGameObjectToScene(go, scene);
            go.transform.position = new Vector3(0f, 0.52f, 0f);
            go.AddComponent<BoxCollider>();
            var corps = go.AddComponent<Rigidbody>();
            corps.freezeRotation = true;
            var joueur = go.AddComponent<DeplacementWow>();
            joueur.Regler(course, wow);
            joueur.Imposer(new DeplacementWow.Entrees());
            return (joueur, physique);
        }

        private static void Avancer(DeplacementWow joueur, PhysicsScene physique, float dt)
        {
            joueur.Pas(dt);
            physique.Simulate(dt);
        }
    }
}
