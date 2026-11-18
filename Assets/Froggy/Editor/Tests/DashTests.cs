using System.Collections.Generic;
using Froggy.Deplacement;
using Froggy.Interface;
using Froggy.Regles;
using Froggy.Socle;
using Froggy.Template;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Froggy.Outils.Tests
{
    /// <summary>
    /// Le dash (#134), à la place du sprint. Les règles seules (ReglesDash) : recharge, assommement, choc. Le geste dans
    /// une scène physique à part (skill ressenti-mouvement) : 4 m mesurés à 30, 60 et 144 pas par seconde, refusé en
    /// l'air et quand Froggy est arrêtée, coupé par un saut sans rallonger le saut. Les chocs (PartieDash) : une Froggy en
    /// dash bouscule l'autre ; deux en dash s'assomment, puis se relèvent. Et le texte du HUD.
    /// </summary>
    public class DashTests
    {
        private TemplateMovementSettings course;
        private ReglagesWow wow;
        private ReglagesDash reglages;
        private Scene scene;
        private readonly List<Object> aDetruire = new List<Object>();

        [SetUp]
        public void Preparer()
        {
            course = ScriptableObject.CreateInstance<TemplateMovementSettings>();
            wow = ScriptableObject.CreateInstance<ReglagesWow>();
            reglages = ScriptableObject.CreateInstance<ReglagesDash>();
            reglages.visuelProvisoire = false;   // pas d'étoiles créées dans la scène ouverte de l'éditeur
        }

        [TearDown]
        public void Ranger()
        {
            if (scene.IsValid()) EditorSceneManager.ClosePreviewScene(scene);
            Object.DestroyImmediate(course);
            Object.DestroyImmediate(wow);
            Object.DestroyImmediate(reglages);
            foreach (var o in aDetruire) if (o != null) Object.DestroyImmediate(o);
            aDetruire.Clear();
        }

        // ---------------------------------------------------------------- Règles

        [Test]
        public void ApresUnDashIlFautAttendreLaRecharge()
        {
            var r = new ReglesDash(3f, 1.5f);
            Assert.That(r.Dasher(1), Is.True);
            Assert.That(r.Dasher(1), Is.False, "en recharge");
            r.Pas(2.9f);
            Assert.That(r.Pret(1), Is.False);
            r.Pas(0.2f);
            Assert.That(r.Pret(1), Is.True, "3 s après");
            Assert.That(r.Pret(2), Is.True, "chacun sa recharge");
        }

        [Test]
        public void UnDashRefuseParLeControleurRendLaRecharge()
        {
            var r = new ReglesDash(3f, 1.5f);
            r.Dasher(1);
            r.Rendre(1);
            Assert.That(r.Pret(1), Is.True);
        }

        [Test]
        public void AssommeOnNeDashePas()
        {
            var r = new ReglesDash(3f, 1.5f);
            r.Assommer(2);
            Assert.That(r.Pret(2), Is.False);
            Assert.That(r.Dasher(2), Is.False);
            r.Pas(1.5f);
            Assert.That(r.Assomme(2), Is.False, "relevé après 1,5 s");
            Assert.That(r.Pret(2), Is.True);
        }

        [Test]
        public void LeChocDependDeLAutre()
        {
            Assert.That(ReglesDash.Choc(false, false), Is.EqualTo(ChocDash.Bouscule));
            Assert.That(ReglesDash.Choc(true, false), Is.EqualTo(ChocDash.DoubleAssomme), "deux dashs en même temps");
            Assert.That(ReglesDash.Choc(false, true), Is.EqualTo(ChocDash.Rien), "intouchable");
            Assert.That(ReglesDash.Choc(true, true), Is.EqualTo(ChocDash.Rien));
        }

        // ---------------------------------------------------------------- Le geste

        [TestCase(30)]
        [TestCase(60)]
        [TestCase(144)]
        public void DashALaDistanceVoulue(int pasParSeconde)
        {
            float dt = 1f / pasParSeconde;
            var physique = Terrain();
            var joueur = Froggy("Joueur", Vector3.zero, 0f, physique);
            var corps = joueur.GetComponent<Rigidbody>();
            for (int i = 0; i < pasParSeconde / 2; i++) Avancer(joueur, physique, dt);
            Assert.That(joueur.AuSol, Is.True, "pas posé au sol");

            float depart = corps.position.z;
            Assert.That(joueur.Dasher(Vector3.forward, reglages.distance, reglages.duree), Is.True);
            int pas = 0;
            while (joueur.EnDash && pas < pasParSeconde) { Avancer(joueur, physique, dt); pas++; }
            float parcouru = corps.position.z - depart;
            Assert.That(parcouru, Is.EqualTo(reglages.distance).Within(0.25f), "4 m");
            Assert.That(pas * dt, Is.EqualTo(reglages.duree).Within(dt + 1e-4f), "en 0,2 s");

            // sans toucher à rien : Froggy s'arrête vite, sans glisser loin après le dash
            for (int i = 0; i < pasParSeconde / 2; i++) Avancer(joueur, physique, dt);
            Assert.That(corps.position.z - depart, Is.LessThan(reglages.distance + 0.8f), "pas de glissade après");
        }

        [Test]
        public void PasDeDashEnLAir()
        {
            var physique = Terrain();
            var joueur = Froggy("Joueur", Vector3.zero, 0f, physique);
            float dt = 1f / 60f;
            for (int i = 0; i < 30; i++) Avancer(joueur, physique, dt);
            joueur.Imposer(new DeplacementWow.Entrees { sautAppuye = true, sautTenu = true });
            Avancer(joueur, physique, dt);
            joueur.Imposer(new DeplacementWow.Entrees { sautTenu = true });
            for (int i = 0; i < 5; i++) Avancer(joueur, physique, dt);
            Assert.That(joueur.AuSol, Is.False);
            Assert.That(joueur.Dasher(Vector3.forward, 4f, 0.2f), Is.False, "au sol seulement");
        }

        [Test]
        public void SauterCoupeLeDashSansRallongerLeSaut()
        {
            var physique = Terrain();
            var joueur = Froggy("Joueur", Vector3.zero, 0f, physique);
            var corps = joueur.GetComponent<Rigidbody>();
            float dt = 1f / 60f;
            for (int i = 0; i < 30; i++) Avancer(joueur, physique, dt);
            joueur.Dasher(Vector3.forward, 4f, 0.2f);
            Avancer(joueur, physique, dt);
            Assert.That(joueur.EnDash, Is.True);
            joueur.Imposer(new DeplacementWow.Entrees { sautAppuye = true, sautTenu = true });
            Avancer(joueur, physique, dt);
            Assert.That(joueur.EnDash, Is.False, "le saut coupe le dash");
            float horizontale = new Vector3(corps.linearVelocity.x, 0f, corps.linearVelocity.z).magnitude;
            Assert.That(horizontale, Is.LessThanOrEqualTo(course.moveSpeed + 0.01f), "on saute à la vitesse de course, pas à 20 m/s");
        }

        [Test]
        public void UneFroggyArreteeNeDashePas()
        {
            var physique = Terrain();
            var joueur = Froggy("Joueur", Vector3.zero, 0f, physique);
            for (int i = 0; i < 30; i++) Avancer(joueur, physique, 1f / 60f);
            joueur.GetComponent<Modificateurs>().Poser(this, 0f, 0f);   // gelée, assommée
            Assert.That(joueur.Dasher(Vector3.forward, 4f, 0.2f), Is.False);
        }

        // ---------------------------------------------------------------- Les chocs

        [Test]
        public void UnDashBousculeLAutre()
        {
            var physique = Terrain();
            var a = Froggy("A", Vector3.zero, 0f, physique);
            var b = Froggy("B", new Vector3(0f, 0f, 3f), 0f, physique);
            var dash = Partie(a, b);
            float dt = 1f / 50f;
            for (int i = 0; i < 25; i++) Pas(dash, physique, dt, a, b);

            Assert.That(dash.Dasher(a), Is.True);
            Assert.That(dash.Dasher(a), Is.False, "en recharge");
            for (int i = 0; i < 15 && dash.Bousculades == 0; i++) Pas(dash, physique, dt, a, b);
            Assert.That(dash.Bousculades, Is.EqualTo(1), "B est percutée");
            Assert.That(a.EnDash, Is.False, "le dash de A s'arrête sur B");
            Assert.That(b.GetComponent<Rigidbody>().linearVelocity.z, Is.GreaterThan(5f), "B recule dans le sens du dash");
            Assert.That(dash.Assommements, Is.Zero);
        }

        [Test]
        public void DeuxDashsQuiSeRencontrentAssommentLesDeux()
        {
            var physique = Terrain();
            var a = Froggy("A", Vector3.zero, 0f, physique);
            var b = Froggy("B", new Vector3(0f, 0f, 4f), 180f, physique);
            var dash = Partie(a, b);
            float dt = 1f / 50f;
            for (int i = 0; i < 25; i++) Pas(dash, physique, dt, a, b);

            Assert.That(dash.Dasher(a) && dash.Dasher(b), Is.True);
            for (int i = 0; i < 15 && dash.Assommements == 0; i++) Pas(dash, physique, dt, a, b);
            Assert.That(dash.Assommements, Is.EqualTo(1));
            foreach (var f in new[] { a, b })
            {
                var m = f.GetComponent<Modificateurs>();
                Assert.That(m.Vitesse, Is.Zero, f.name + " assommée : elle ne court plus");
                Assert.That(m.Entrave, Is.True, f.name + " : ni torgnole ni objet");
                Assert.That(dash.Regles.Assomme(dash.Cle(f)), Is.True);
            }

            for (int i = 0; i < 80; i++) Pas(dash, physique, dt, a, b);   // 1,6 s > 1,5 s
            foreach (var f in new[] { a, b })
            {
                var m = f.GetComponent<Modificateurs>();
                Assert.That(m.Vitesse, Is.EqualTo(1f), f.name + " relevée");
                Assert.That(m.Entrave, Is.False);
            }
        }

        // ---------------------------------------------------------------- HUD

        [Test]
        public void LeHudMontreLeDash()
        {
            var e = new EtatHud { DashConnu = true, ToucheDash = "Maj gauche", DureeRechargeDash = 3f };
            Assert.That(ContenuHud.Dash(e), Is.EqualTo("Dash (Maj gauche) : prêt"));
            e.RechargeDash = 1.25f;
            Assert.That(ContenuHud.Dash(e), Does.StartWith("Dash (Maj gauche) : "));
            Assert.That(ContenuHud.Dash(e), Does.Not.Contain("prêt"));
            e.Assomme = true;
            Assert.That(ContenuHud.Dash(e), Is.EqualTo("Assommé !"));
            Assert.That(ContenuHud.Dash(new EtatHud()), Is.Empty, "pas de dash dans la scène : rien");
        }

        // ---------------------------------------------------------------- Outils

        private PhysicsScene Terrain()
        {
            scene = EditorSceneManager.NewPreviewScene();
            var sol = new GameObject("Sol");
            SceneManager.MoveGameObjectToScene(sol, scene);
            sol.AddComponent<BoxCollider>().size = new Vector3(60f, 1f, 60f);
            sol.transform.position = new Vector3(0f, -0.5f, 0f);
            return scene.GetPhysicsScene();
        }

        private DeplacementWow Froggy(string nom, Vector3 position, float cap, PhysicsScene physique)
        {
            var go = new GameObject(nom);
            SceneManager.MoveGameObjectToScene(go, scene);
            go.transform.SetPositionAndRotation(position + new Vector3(0f, 0.52f, 0f), Quaternion.Euler(0f, cap, 0f));
            go.AddComponent<BoxCollider>();
            var corps = go.AddComponent<Rigidbody>();
            corps.freezeRotation = true;
            var joueur = go.AddComponent<DeplacementWow>();
            joueur.Regler(course, wow);
            joueur.Imposer(new DeplacementWow.Entrees());
            return joueur;
        }

        private PartieDash Partie(params DeplacementWow[] froggys)
        {
            var go = new GameObject("Dash (test)");
            aDetruire.Add(go);
            var dash = go.AddComponent<PartieDash>();
            dash.Regler(reglages, froggys);
            return dash;
        }

        private static void Avancer(DeplacementWow joueur, PhysicsScene physique, float dt)
        {
            joueur.Pas(dt);
            physique.Simulate(dt);
        }

        private static void Pas(PartieDash dash, PhysicsScene physique, float dt, params DeplacementWow[] froggys)
        {
            foreach (var f in froggys) f.Pas(dt);
            physique.Simulate(dt);
            dash.Pas(dt);
        }
    }
}
