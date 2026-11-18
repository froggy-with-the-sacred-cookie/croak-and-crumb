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
