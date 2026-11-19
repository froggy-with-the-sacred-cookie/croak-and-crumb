using Froggy.Deplacement;
using Froggy.Template;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Froggy.Outils.Tests
{
    /// <summary>
    /// Contrôleur (#13, skill ressenti-mouvement) : « on enchaîne 10 sauts de plate-forme en plate-forme sans rater par
    /// injustice, à 30 comme à 144 images par seconde ». Un joueur simulé et honnête (il vise la plate-forme suivante, tient
    /// l'avance et le saut) fait tout le parcours d'un coup, en appuyant un peu tôt, pile au bord ou 0,1 s après avoir
    /// quitté le bord (le coyote time doit le rattraper). Puis les tolérances une à une, avec leur limite : un appui trop
    /// tard ou trop tôt ne doit pas partir. Toujours sur des copies des vrais réglages du jardin, jamais sur les assets.
    /// </summary>
    public class ParcoursSautsTests
    {
        public enum Moment { UnPeuTot, AuBord, EnRetard }

        private const string CheminCourse = "Assets/Froggy/Joueur/Settings/TemplateMovementSettings.asset";
        private const string CheminWow = "Assets/Froggy/Vitrine/ReglagesWow.asset";
        private const string CheminMatiere = "Assets/Froggy/Joueur/Settings/TemplateNoFriction.physicMaterial";

        private TemplateMovementSettings course;
        private ReglagesWow wow;
        private Scene scene;
        private PhysicsScene physique;

        [SetUp]
        public void Preparer()
        {
            course = Object.Instantiate(AssetDatabase.LoadAssetAtPath<TemplateMovementSettings>(CheminCourse));
            wow = Object.Instantiate(AssetDatabase.LoadAssetAtPath<ReglagesWow>(CheminWow));
        }

        [TearDown]
        public void Ranger()
        {
            if (scene.IsValid()) EditorSceneManager.ClosePreviewScene(scene);
            Object.DestroyImmediate(course);
            Object.DestroyImmediate(wow);
        }

        [Test]
        public void ChaqueSautEstDansLaPortee()
        {
            // portée vers une plate-forme dh plus haute, à la vitesse de course, depuis le bord : x = v·t, y(t) = dh en descente
            float g = -Physics.gravity.y, v0 = Mathf.Sqrt(2f * g * course.jumpHeight);
            foreach (var s in ParcoursSauts.Sauts)
            {
                Assert.That(s.y, Is.LessThan(course.jumpHeight * 0.7f), "marche trop haute pour un saut");
                float t = (v0 + Mathf.Sqrt(v0 * v0 - 2f * g * s.y)) / g;
                float portee = course.moveSpeed * t;
                // de bord à bord, et l'on doit encore poser le milieu du corps (0,5 m) sur la plate-forme d'arrivée
                Assert.That(s.x + 0.5f, Is.LessThan(portee * 0.85f), $"écart de {s.x} m trop juste (portée {portee:0.0} m)");
            }
        }

        [Test]
        public void DixSautsEnchaines(
            [Values(30, 60, 144)] int pasParSeconde,
            [Values(Moment.UnPeuTot, Moment.AuBord, Moment.EnRetard)] Moment moment)
        {
            var parcours = Parcours();
            var joueur = Joueur(parcours.Dessus(0));
            var corps = joueur.GetComponent<Rigidbody>();
            parcours.Suivre(joueur);   // le compteur à l'écran doit dire 10 lui aussi
            float dt = 1f / pasParSeconde;
            for (int i = 0; i < 10; i++) Avancer(joueur, dt);

            int cible = 1, pas = 0;
            bool aQuitte = false, saute = false;
            float horsSol = 0f;
            int total = parcours.Plateformes.Count - 1;
            while (cible <= total)
            {
                Assert.That(pas++, Is.LessThan(pasParSeconde * 8 * total), $"bloqué avant la plate-forme {cible}");
                Vector3 pos = corps.position;
                Vector3 vers = parcours.Dessus(cible) - pos;
                vers.y = 0f;
                float cap = Mathf.Atan2(vers.x, vers.z) * Mathf.Rad2Deg;
                corps.rotation = Quaternion.Euler(0f, cap, 0f);   // le cap de DeplacementWow : on vise la suivante

                bool appui = false;
                if (!saute)
                {
                    if (joueur.AuSol)
                    {
                        if (moment == Moment.UnPeuTot && HorsDe(parcours.Plateformes[cible - 1], pos + vers.normalized * 0.3f))
                            appui = true;   // 0,3 m avant le bord
                    }
                    else
                    {
                        aQuitte = true;
                        horsSol += dt;
                        if (moment == Moment.AuBord) appui = true;   // au premier pas hors du sol
                        // 0,1 s après le bord, compté au pas qui lira l'appui (le suivant)
                        if (moment == Moment.EnRetard && horsSol + dt >= 0.1f - 1e-4f) appui = true;
                    }
                }
                joueur.Imposer(new DeplacementWow.Entrees { deplacement = new Vector2(0f, 1f), sautAppuye = appui, sautTenu = true });
                Avancer(joueur, dt);
                if (appui)
                {
                    saute = true;
                    Assert.That(corps.linearVelocity.y, Is.GreaterThan(1f),
                        $"le saut vers la plate-forme {cible} n'est pas parti ({moment}, {horsSol:0.000} s hors du sol)");
                }

                Assert.That(corps.position.y, Is.GreaterThan(parcours.Dessus(cible).y - 2f),
                    $"raté vers la plate-forme {cible} ({moment}, {pasParSeconde} pas/s)");

                if (saute && joueur.AuSol && SousLesPieds(corps) == parcours.Plateformes[cible])
                {
                    cible++;
                    saute = aQuitte = false;
                    horsSol = 0f;
                }
            }
            Assert.That(parcours.Serie, Is.EqualTo(total), "le compteur du parcours n'a pas vu les dix sauts");
        }

        [Test]
        public void UnSautEnchaineParLeTamponCompteQuandMeme([Values(30, 60, 144)] int pasParSeconde)
        {
            // il repart dès que ses pieds passent à groundCheckDistance du dessus : il ne touche jamais la plate-forme
            var parcours = Parcours();
            var joueur = Joueur(parcours.Dessus(1) + Vector3.up * 1.5f);
            var corps = joueur.GetComponent<Rigidbody>();
            parcours.Suivre(joueur);
            float dt = 1f / pasParSeconde, contact = Mathf.Sqrt(2f * 1.5f / -Physics.gravity.y);
            bool appuye = false, reparti = false;
            for (float t = 0f; t < contact + 0.4f && !reparti; t += dt)
            {
                bool appui = !appuye && t >= contact - 0.05f;
                appuye |= appui;
                joueur.Imposer(new DeplacementWow.Entrees { sautAppuye = appui, sautTenu = true });
                Avancer(joueur, dt);
                reparti = appuye && corps.linearVelocity.y > 1f;
            }
            Assert.That(reparti, Is.True, "le tampon devait relancer le saut");
            Assert.That(parcours.Courante, Is.EqualTo(1), "la plate-forme d'où il est reparti doit compter");
        }

        [Test]
        public void LaSerieCompteChaquePlateformeDansLOrdre()
        {
            var parcours = Parcours();
            var joueur = Joueur(parcours.Dessus(0));
            var froggy = joueur.GetComponent<Rigidbody>();
            var autre = Joueur(parcours.Dessus(0) + Vector3.right * 5f).GetComponent<Rigidbody>();
            parcours.Suivre(joueur);
            parcours.Atterrir(1, froggy);
            parcours.Atterrir(1, froggy);   // reposé sur la même : rien ne change
            parcours.Atterrir(2, froggy);
            parcours.Atterrir(3, autre);    // un autre corps ne compte pas
            Assert.That(parcours.Serie, Is.EqualTo(2));
            Assert.That(parcours.Courante, Is.EqualTo(2));
            parcours.Atterrir(4, froggy);   // une plate-forme sautée : la série repart
            Assert.That(parcours.Serie, Is.EqualTo(0));
            parcours.Atterrir(5, froggy);
            Assert.That(parcours.Serie, Is.EqualTo(1));
            Assert.That(parcours.Meilleure, Is.EqualTo(2));
        }

        [Test]
        public void CoyoteTimeEtSaLimite([Values(30, 60, 144)] int pasParSeconde)
        {
            Assert.That(SauteApresLeBord(pasParSeconde, course.coyoteTime - 0.03f), Is.True, "dans le coyote time : il doit partir");
            Assert.That(SauteApresLeBord(pasParSeconde, course.coyoteTime + 0.05f), Is.False, "trop tard : il ne doit pas partir");
        }

        [Test]
        public void TamponEtSaLimite([Values(30, 60, 144)] int pasParSeconde)
        {
            Assert.That(SauteALAtterrissage(pasParSeconde, course.jumpBufferTime - 0.03f), Is.True, "dans le tampon : il part à l'atterrissage. " + Diagnostic);
            Assert.That(SauteALAtterrissage(pasParSeconde, course.jumpBufferTime + 0.08f), Is.False, "trop tôt : l'appui est oublié. " + Diagnostic);
        }

        // ---- les gestes ----

        /// <summary>
        /// Court vers l'arrière du départ (le vide, aucune plate-forme où se rattraper), le quitte, et appuie pour que le pas
        /// qui lit l'appui tombe retard secondes après avoir quitté le bord : le saut part-il ?
        /// </summary>
        private bool SauteApresLeBord(int pasParSeconde, float retard)
        {
            var parcours = Parcours();
            var joueur = Joueur(parcours.Dessus(0));
            var corps = joueur.GetComponent<Rigidbody>();
            float dt = 1f / pasParSeconde, horsSol = 0f;
            for (int i = 0; i < 10; i++) Avancer(joueur, dt);
            corps.rotation = Quaternion.Euler(0f, 180f, 0f);   // vers -z, à l'opposé du parcours
            for (int i = 0; i < pasParSeconde * 3; i++)
            {
                bool appui = false;
                if (!joueur.AuSol)
                {
                    horsSol += dt;
                    if (horsSol + dt >= retard - 1e-4f) appui = true;
                }
                joueur.Imposer(new DeplacementWow.Entrees { deplacement = new Vector2(0f, 1f), sautAppuye = appui, sautTenu = true });
                Avancer(joueur, dt);
                if (appui) return corps.linearVelocity.y > 1f;
            }
            Assert.Fail("jamais sorti du départ");
            return false;
        }

        /// <summary>Tombe sur le départ d'un peu haut, appuie avance secondes avant de toucher : repart-il en touchant ?</summary>
        private bool SauteALAtterrissage(int pasParSeconde, float avance)
        {
            var parcours = Parcours();
            float g = -Physics.gravity.y, chute = 1.5f;
            var joueur = Joueur(parcours.Dessus(0) + Vector3.up * chute);
            var corps = joueur.GetComponent<Rigidbody>();
            float dt = 1f / pasParSeconde;
            float contact = Mathf.Sqrt(2f * chute / g);   // instant où il touche, chute libre
            bool appuye = false;
            float tAppui = -1f, tSol = -1f;
            for (float t = 0f; t < contact + 0.5f; t += dt)
            {
                bool appui = !appuye && t >= contact - avance;
                if (appui) tAppui = t;
                appuye |= appui;
                joueur.Imposer(new DeplacementWow.Entrees { sautAppuye = appui, sautTenu = true });
                Avancer(joueur, dt);
                if (tSol < 0f && joueur.AuSol) tSol = t;
                if (appuye && corps.linearVelocity.y > 1f)
                {
                    Diagnostic = $"appui à {tAppui:0.000} s, au sol à {tSol:0.000} s, saut à {t:0.000} s (contact théorique {contact:0.000} s)";
                    return true;
                }
            }
            Diagnostic = $"appui à {tAppui:0.000} s, au sol à {tSol:0.000} s, pas de saut (contact théorique {contact:0.000} s)";
            return false;
        }

        private string Diagnostic = "";

        // ---- le décor ----

        /// <summary>Un parcours neuf dans une scène neuve : un essai ne bute jamais sur le Froggy de l'essai d'avant.</summary>
        private ParcoursSauts Parcours()
        {
            if (scene.IsValid()) EditorSceneManager.ClosePreviewScene(scene);
            scene = EditorSceneManager.NewPreviewScene();
            physique = scene.GetPhysicsScene();
            var racine = new GameObject("Parcours");
            SceneManager.MoveGameObjectToScene(racine, scene);
            ParcoursSauts.Construire(racine.transform);
            var parcours = racine.AddComponent<ParcoursSauts>();
            parcours.enabled = false;   // pas de compteur ni de touche R dans un test
            Physics.SyncTransforms();
            return parcours;
        }

        private DeplacementWow Joueur(Vector3 dessus)
        {
            var go = new GameObject("Froggy");
            SceneManager.MoveGameObjectToScene(go, scene);
            go.transform.position = dessus + Vector3.up * 0.52f;
            var boite = go.AddComponent<BoxCollider>();
            boite.sharedMaterial = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(CheminMatiere);
            var corps = go.AddComponent<Rigidbody>();
            corps.freezeRotation = true;
            var joueur = go.AddComponent<DeplacementWow>();
            joueur.Regler(course, wow);
            joueur.Imposer(new DeplacementWow.Entrees());
            return joueur;
        }

        private void Avancer(DeplacementWow joueur, float dt)
        {
            joueur.Pas(dt);
            physique.Simulate(dt);
        }

        private static bool HorsDe(Transform plateforme, Vector3 point)
        {
            Vector3 l = plateforme.InverseTransformPoint(point);
            return Mathf.Abs(l.x) > 0.5f || Mathf.Abs(l.z) > 0.5f;
        }

        private Transform SousLesPieds(Rigidbody corps)
        {
            return physique.Raycast(corps.position, Vector3.down, out var h, 0.8f, ~0, QueryTriggerInteraction.Ignore)
                ? h.collider.transform : null;
        }
    }
}
