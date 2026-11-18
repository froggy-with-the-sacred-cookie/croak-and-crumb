using Froggy.Cameras;
using NUnit.Framework;
using UnityEngine;

namespace Froggy.Outils.Tests
{
    /// <summary>
    /// Suivi de la caméra orbitale à 30, 60 et 144 images par seconde (skill camera, section 5) : pas de dépassement,
    /// pas de saut, même allure quelle que soit la fréquence ; rapprochement immédiat contre un obstacle ; molette ; virage au
    /// clavier (la caméra tourne avec Froggy, sauf quand on regarde ailleurs).
    /// </summary>
    public class CameraOrbitaleTests
    {
        [TestCase(30)]
        [TestCase(60)]
        [TestCase(144)]
        public void SeRallongeSansDepasserNiSauter(int ips)
        {
            float dt = 1f / ips, d = 2f, v = 0f, avant = d;
            for (float t = 0f; t < 1.5f; t += dt)
            {
                d = CameraOrbitale.Suivre(d, float.PositiveInfinity, 6f, ref v, 0.35f, dt);
                Assert.That(d, Is.LessThanOrEqualTo(6f), "dépassement");
                Assert.That(d, Is.GreaterThanOrEqualTo(avant), "la caméra recule puis revient");
                Assert.That(d - avant, Is.LessThan(12f * dt), "saut : plus de 12 m/s");
                avant = d;
            }
            Assert.That(d, Is.EqualTo(6f).Within(0.05f), "pas arrivée en 1,5 s");
        }

        [Test]
        public void MemeAllureQuelleQueSoitLaFrequence()
        {
            float A(int ips)
            {
                float dt = 1f / ips, d = 2f, v = 0f;
                for (int i = 0; i < ips * 0.35f; i++) d = CameraOrbitale.Suivre(d, float.PositiveInfinity, 6f, ref v, 0.35f, dt);
                return d;
            }
            Assert.That(A(30), Is.EqualTo(A(144)).Within(0.15f));
        }

        [Test]
        public void ObstacleRapprocheAussitot()
        {
            float v = 3f;
            Assert.That(CameraOrbitale.Suivre(6f, 1.5f, 6f, ref v, 0.35f, 1f / 60), Is.EqualTo(1.5f));
            Assert.That(v, Is.EqualTo(0f), "le ressort repart de zéro après un obstacle");
        }

        [Test]
        public void SeulsLesMursMarquesArretentLaCamera()
        {
            var scene = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
            try
            {
                GameObject Boite(string nom, float z)
                {
                    var go = new GameObject(nom);
                    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, scene);
                    go.AddComponent<BoxCollider>().size = new Vector3(4f, 4f, 0.5f);
                    go.transform.position = new Vector3(0f, 0f, -z);
                    return go;
                }
                Boite("Rocher", 1.5f);
                Boite("Muraille", 4f).AddComponent<BloqueCamera>();
                Physics.SyncTransforms();   // sans pas physique, les colliders seraient encore à l'origine
                var physique = scene.GetPhysicsScene();
                var touches = new RaycastHit[32];
                float libre = CameraOrbitale.Libre(physique, Vector3.zero, Vector3.back, 6f, 0.25f, ~0, null, touches, false);
                Assert.That(libre, Is.EqualTo(4f - 0.25f - 0.25f).Within(0.02f), "le rocher ne devait pas l'arrêter, la muraille si");
                float tout = CameraOrbitale.Libre(physique, Vector3.zero, Vector3.back, 6f, 0.25f, ~0, null, touches, true);
                Assert.That(tout, Is.EqualTo(1.5f - 0.25f - 0.25f).Within(0.02f), "avec toutBloque, le rocher l'arrête");
            }
            finally { UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene); }
        }

        [TestCase(30)]
        [TestCase(60)]
        [TestCase(144)]
        public void LaCameraTourneAvecFroggy(int ips)
        {
            float cap = 350f, lacet = 350f, dt = 1f / ips;
            for (int i = 0; i < ips / 2; i++) (cap, lacet) = CameraOrbitale.Tourner(cap, lacet, 1f, 180f, dt, false);
            Assert.That(Mathf.DeltaAngle(350f, cap), Is.EqualTo(90f).Within(3.5f), "une demi-seconde à 180°/s");
            Assert.That(lacet, Is.EqualTo(cap).Within(0.001f), "la caméra reste derrière Froggy");
        }

        [Test]
        public void EnRegardantAilleursLaCameraNeSuitPas()
        {
            var (cap, lacet) = CameraOrbitale.Tourner(0f, 40f, -1f, 180f, 0.5f, true);
            Assert.That(cap, Is.EqualTo(270f).Within(0.001f));
            Assert.That(lacet, Is.EqualTo(40f));
        }

        // ---- Voir où l'on atterrit (#15) ----

        private const float G = 9.81f;

        [TestCase(30)]
        [TestCase(60)]
        [TestCase(144)]
        public void UnSautNormalNeFaitPresquePasMonterLeCadre(int ips)
        {
            float dt = 1f / ips, v0 = Mathf.Sqrt(2f * G * 1.5f), h = 0f, vh = 0f, avant = 0f, hautMax = 0f;
            for (float t = dt; t < 2f * v0 / G; t += dt)
            {
                float y = v0 * t - 0.5f * G * t * t;
                h = CameraOrbitale.HauteurCadre(h, ref vh, y, 0f, false, 1.2f, 2f, 0.2f, dt);
                Assert.That(h, Is.LessThanOrEqualTo(y + 1e-4f), "le cadre passe au-dessus de Froggy");
                Assert.That(Mathf.Abs(h - avant), Is.LessThan(8f * dt), "saut du cadre");
                hautMax = Mathf.Max(hautMax, h);
                avant = h;
            }
            Assert.That(hautMax, Is.LessThan(0.3f), "un saut de 1,5 m ne doit faire monter le cadre que de 0,3 m au plus");
        }

        [TestCase(30)]
        [TestCase(60)]
        [TestCase(144)]
        public void SousLeDernierSolLeCadreSuitSansRetard(int ips)
        {
            float dt = 1f / ips, h = 0f, vh = 0f;
            for (float t = dt; t < 1.2f; t += dt)
            {
                float y = -0.5f * G * t * t;   // Froggy tombe d'un rebord
                h = CameraOrbitale.HauteurCadre(h, ref vh, y, 0f, false, 1.2f, 2f, 0.2f, dt);
                Assert.That(h, Is.EqualTo(y).Within(1e-3f), "en chute, le cadre suit Froggy sans traîner");
            }
        }

        [TestCase(30)]
        [TestCase(60)]
        [TestCase(144)]
        public void PoseSurUnSolPlusHautLeCadreSeRecaleSansDepasser(int ips)
        {
            float dt = 1f / ips, h = 0f, vh = 0f;
            for (float t = 0f; t < 1f; t += dt)
            {
                h = CameraOrbitale.HauteurCadre(h, ref vh, 2f, 2f, true, 1.2f, 2f, 0.2f, dt);
                Assert.That(h, Is.LessThanOrEqualTo(2f + 1e-4f), "dépassement");
            }
            Assert.That(h, Is.EqualTo(2f).Within(0.01f));
        }

        [Test]
        public void MemeCadreQuelleQueSoitLaFrequence()
        {
            float A(int ips)
            {
                float dt = 1f / ips, h = 0f, vh = 0f, v0 = Mathf.Sqrt(2f * G * 3f);   // un grand saut : le cadre monte
                for (float t = dt; t <= 0.7f; t += dt)
                    h = CameraOrbitale.HauteurCadre(h, ref vh, v0 * t - 0.5f * G * t * t, 0f, false, 1.2f, 2f, 0.2f, dt);
                return h;
            }
            Assert.That(A(30), Is.EqualTo(A(144)).Within(0.1f));
        }

        [Test]
        public void PointDeChuteSurUnSolPlat()
        {
            var scene = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
            try
            {
                var sol = new GameObject("Sol");
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(sol, scene);
                sol.AddComponent<BoxCollider>().size = new Vector3(40f, 1f, 40f);
                sol.transform.position = new Vector3(0f, -0.5f, 0f);
                Physics.SyncTransforms();
                var touches = new RaycastHit[32];
                bool trouve = CameraOrbitale.PointDeChute(scene.GetPhysicsScene(), new Vector3(0f, 3f, 0f),
                    new Vector3(4f, 0f, 0f), new Vector3(0f, -G, 0f), 2.5f, ~0, null, touches, out var point);
                Assert.That(trouve, Is.True);
                Assert.That(point.y, Is.EqualTo(0f).Within(0.01f));
                Assert.That(point.x, Is.EqualTo(4f * Mathf.Sqrt(6f / G)).Within(0.05f), "x = v·√(2h/g)");

                bool vide = CameraOrbitale.PointDeChute(scene.GetPhysicsScene(), new Vector3(100f, 3f, 0f),
                    Vector3.zero, new Vector3(0f, -G, 0f), 2.5f, ~0, null, touches, out _);
                Assert.That(vide, Is.False, "au-dessus du vide, rien de prévu");
            }
            finally { UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene); }
        }

        [Test]
        public void LeRegardSeBaissePourVoirLaChute()
        {
            var cam = new Vector3(0f, 2f, -6f);
            // 12 m plus bas, 8 m devant : 56° sous l'horizon ; le bas utile de l'image est à 18 + 60 × 0,35 = 39°
            Assert.That(CameraOrbitale.InclinaisonManquante(cam, 18f, 0f, new Vector3(0f, -10f, 2f), 60f, 0.15f),
                Is.EqualTo(Mathf.Atan2(12f, 8f) * Mathf.Rad2Deg - 39f).Within(0.01f));
            Assert.That(CameraOrbitale.InclinaisonManquante(cam, 18f, 0f, new Vector3(0f, 0f, 2f), 60f, 0.15f),
                Is.EqualTo(0f), "déjà dans l'image");
            Assert.That(CameraOrbitale.InclinaisonManquante(cam, 18f, 0f, new Vector3(0f, -10f, -9f), 60f, 0.15f),
                Is.EqualTo(0f), "derrière la caméra : baisser le regard n'y change rien");
            // le cap compte : la même chute vue d'une caméra tournée de 90°
            var tournee = Quaternion.Euler(0f, 90f, 0f);
            Assert.That(CameraOrbitale.InclinaisonManquante(tournee * cam, 18f, 90f, tournee * new Vector3(0f, -10f, 2f), 60f, 0.15f),
                Is.EqualTo(Mathf.Atan2(12f, 8f) * Mathf.Rad2Deg - 39f).Within(0.01f));
        }

        [Test]
        public void LePlanProcheTientDansLaSphereDeCollision()
        {
            // réglages du PlayerTemplate (plan proche 0,1 m) en 16:9 et 21:9, 3e et 1re personne
            Assert.That(CameraOrbitale.RayonPlanProche(0.1f, 60f, 16f / 9f), Is.LessThan(0.25f));
            Assert.That(CameraOrbitale.RayonPlanProche(0.1f, 75f, 21f / 9f), Is.LessThan(0.25f));
            Assert.That(CameraOrbitale.RayonPlanProche(0.3f, 60f, 16f / 9f), Is.GreaterThan(0.25f),
                "au plan proche par défaut de Unity, la sphère doit grossir");
        }

        [Test]
        public void MoletteJusquaLaPremierePersonneEtRetour()
        {
            Assert.That(CameraOrbitale.Zoomer(6f, -1f, 1f, 15f, 0.8f), Is.EqualTo(5f));
            Assert.That(CameraOrbitale.Zoomer(1f, -1f, 1f, 15f, 0.8f), Is.EqualTo(0f), "sous le seuil : première personne");
            Assert.That(CameraOrbitale.Zoomer(0f, 1f, 0.5f, 15f, 0.8f), Is.EqualTo(0.8f), "on ressort au moins au seuil");
            Assert.That(CameraOrbitale.Zoomer(14.5f, 3f, 1f, 15f, 0.8f), Is.EqualTo(15f));
            Assert.That(CameraOrbitale.Zoomer(0f, 0f, 1f, 15f, 0.8f), Is.EqualTo(0f));
        }
    }
}
