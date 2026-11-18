using Froggy.Deplacement;
using Froggy.Template;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Froggy.Outils.Tests
{
    /// <summary>
    /// Nage (P1-02), dans une scène physique à part avec un bassin de 3 m et un bord à droite : Froggy flotte la tête hors
    /// de l'eau (à 30, 60 et 144 pas par seconde), nage plus lentement qu'elle ne court avec un demi-tour net, plonge tant
    /// qu'on tient la touche et remonte, ressort en sautant sur le bord. Hors de l'eau, rien ne change.
    /// </summary>
    public class NageTests
    {
        private TemplateMovementSettings course;
        private ReglagesWow wow;
        private ReglagesNage nage;
        private Scene scene;

        [SetUp]
        public void Preparer()
        {
            course = ScriptableObject.CreateInstance<TemplateMovementSettings>();
            wow = ScriptableObject.CreateInstance<ReglagesWow>();
            nage = ScriptableObject.CreateInstance<ReglagesNage>();
        }

        [TearDown]
        public void Ranger()
        {
            if (scene.IsValid()) EditorSceneManager.ClosePreviewScene(scene);
            Object.DestroyImmediate(course);
            Object.DestroyImmediate(wow);
            Object.DestroyImmediate(nage);
        }

        [TestCase(30)]
        [TestCase(60)]
        [TestCase(144)]
        public void FroggyFlotteLaTeteHorsDeLEau(int pasParSeconde)
        {
            float dt = 1f / pasParSeconde;
            var (joueur, physique, nageur) = Bassin(new Vector3(0f, 2f, 0f));   // tombe dans l'eau de 2 m
            var corps = joueur.GetComponent<Rigidbody>();
            for (int i = 0; i < pasParSeconde * 3; i++) Avancer(joueur, physique, dt);
            Assert.That(nageur.DansLEau, Is.True);
            Assert.That(corps.position.y, Is.EqualTo(-nage.flottaison).Within(0.08f), "le centre sous la surface, la tête dehors");
            Assert.That(Mathf.Abs(corps.linearVelocity.y), Is.LessThan(0.1f), "stable, sans rebondir");
            Assert.That(joueur.AuSol, Is.False);
        }

        [Test]
        public void NagePlusLentementQuOnNeCourtEtDemiTourNet()
        {
            float dt = 1f / 60f;
            var (joueur, physique, nageur) = Bassin(new Vector3(-3f, -0.3f, 0f));
            var corps = joueur.GetComponent<Rigidbody>();
            joueur.Imposer(new DeplacementWow.Entrees { deplacement = new Vector2(0f, 1f) });   // en avant : +z
            for (int i = 0; i < 60; i++) Avancer(joueur, physique, dt);
            float voulue = course.moveSpeed * nage.facteurVitesse;
            Assert.That(corps.linearVelocity.z, Is.EqualTo(voulue).Within(0.05f), "nage à 60 % de la course");

            joueur.Imposer(new DeplacementWow.Entrees { deplacement = new Vector2(0f, -1f) });   // demi-tour (recul)
            float tempsDemiTour = 0f;
            while (corps.linearVelocity.z > 0f && tempsDemiTour < 2f) { Avancer(joueur, physique, dt); tempsDemiTour += dt; }
            Assert.That(tempsDemiTour, Is.LessThan(0.3f), "demi-tour net : pas de flottement");
        }

        [Test]
        public void PlongeTantQuOnTientPuisRemonte()
        {
            float dt = 1f / 60f;
            var (joueur, physique, nageur) = Bassin(new Vector3(-3f, -0.3f, 0f));
            var corps = joueur.GetComponent<Rigidbody>();
            nageur.Imposer(true);
            for (int i = 0; i < 90; i++) Avancer(joueur, physique, dt);
            Assert.That(nageur.Plonge, Is.True);
            Assert.That(corps.position.y, Is.EqualTo(-nage.plongee).Within(0.1f), "sous l'eau, à la profondeur de plongée");
            nageur.Imposer(false);
            for (int i = 0; i < 90; i++) Avancer(joueur, physique, dt);
            Assert.That(corps.position.y, Is.EqualTo(-nage.flottaison).Within(0.1f), "remontée à la surface");
        }

        [Test]
        public void RessortEnSautantSurLeBord()
        {
            float dt = 1f / 60f;
            var (joueur, physique, nageur) = Bassin(new Vector3(3.3f, -0.3f, 0f));   // à 1,2 m du bord (x = 4,5)
            var corps = joueur.GetComponent<Rigidbody>();
            corps.rotation = Quaternion.Euler(0f, 90f, 0f);   // tournée vers le bord (+x)
            joueur.transform.rotation = corps.rotation;
            for (int i = 0; i < 30; i++) Avancer(joueur, physique, dt);
            joueur.Imposer(new DeplacementWow.Entrees { deplacement = new Vector2(0f, 1f), sautAppuye = true, sautTenu = true });
            Avancer(joueur, physique, dt);
            joueur.Imposer(new DeplacementWow.Entrees { deplacement = new Vector2(0f, 1f), sautTenu = true });
            for (int i = 0; i < 120 && !joueur.AuSol; i++) Avancer(joueur, physique, dt);
            Assert.That(joueur.AuSol, Is.True, "posée sur le bord");
            Assert.That(nageur.DansLEau, Is.False);
            Assert.That(corps.position.x, Is.GreaterThan(4.5f));
            Assert.That(corps.useGravity, Is.True, "la gravité revient hors de l'eau");
        }

        [Test]
        public void HorsDeLEauRienNeChange()
        {
            float dt = 1f / 60f;
            var (joueur, physique, nageur) = Bassin(new Vector3(6f, 0.52f, 0f));   // sur le bord
            for (int i = 0; i < 30; i++) Avancer(joueur, physique, dt);
            Assert.That(joueur.AuSol, Is.True);
            Assert.That(nageur.DansLEau, Is.False);
            Assert.That(joueur.GetComponent<Rigidbody>().useGravity, Is.True);
        }

        // un bassin de 3 m de profondeur (surface à y = 0) de x = -6 à 4,5, un bord à droite (dessus à y = 0)
        private (DeplacementWow, PhysicsScene, NageFroggy) Bassin(Vector3 depart)
        {
            scene = EditorSceneManager.NewPreviewScene();
            var physique = scene.GetPhysicsScene();
            Boite("Fond", new Vector3(0f, -3.5f, 0f), new Vector3(30f, 1f, 30f), false);
            Boite("Bord", new Vector3(9f, -1.5f, 0f), new Vector3(9f, 3f, 30f), false);
            Boite("Eau", new Vector3(-0.75f, -1.5f, 0f), new Vector3(10.5f, 3f, 30f), true).gameObject.AddComponent<ZoneEau>();

            var go = new GameObject("Joueur");
            SceneManager.MoveGameObjectToScene(go, scene);
            go.transform.position = depart;
            go.AddComponent<BoxCollider>();
            var corps = go.AddComponent<Rigidbody>();
            corps.freezeRotation = true;
            var nageur = go.AddComponent<NageFroggy>();
            nageur.Regler(nage);
            nageur.Imposer(false);
            var joueur = go.AddComponent<DeplacementWow>();
            joueur.Regler(course, wow);
            joueur.Imposer(new DeplacementWow.Entrees());
            return (joueur, physique, nageur);
        }

        private BoxCollider Boite(string nom, Vector3 position, Vector3 taille, bool declencheur)
        {
            var go = new GameObject(nom);
            SceneManager.MoveGameObjectToScene(go, scene);
            go.transform.position = position;
            var b = go.AddComponent<BoxCollider>();
            b.size = taille;
            b.isTrigger = declencheur;
            return b;
        }

        private static void Avancer(DeplacementWow joueur, PhysicsScene physique, float dt)
        {
            joueur.Pas(dt);
            physique.Simulate(dt);
        }
    }
}
