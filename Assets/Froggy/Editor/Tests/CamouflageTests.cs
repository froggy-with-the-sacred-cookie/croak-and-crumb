using Froggy.Meteo;
using Froggy.Objets;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Froggy.Outils.Tests
{
    /// <summary>
    /// Camouflage caméléon (P4-07) : 6 s presque invisible, un peu plus visible en course ; l'onde du croassement et la
    /// pluie le révèlent (le camouflage s'arrête, choix de Lucas du 18 novembre 2026) ; sous la pluie il ne prend pas ;
    /// un rendu de cookie reste visible ; à la fin, les vrais matériaux reviennent.
    /// </summary>
    public class CamouflageTests
    {
        private ReglagesCamouflage reglages;
        private DefinitionEvenement pluie;
        private Scene scene;

        [SetUp]
        public void Preparer()
        {
            reglages = ScriptableObject.CreateInstance<ReglagesCamouflage>();
            pluie = ScriptableObject.CreateInstance<DefinitionEvenement>();
            reglages.revelePar.Add(pluie);
        }

        [TearDown]
        public void Ranger()
        {
            if (scene.IsValid()) EditorSceneManager.ClosePreviewScene(scene);
            Object.DestroyImmediate(reglages);
            Object.DestroyImmediate(pluie);
        }

        [Test]
        public void PresqueInvisibleImmobilePlusVisibleEnCourse()
        {
            float immobile = ReglesCamouflage.Opacite(0f, 0.3f, 0f, reglages);
            Assert.That(immobile, Is.EqualTo(reglages.opaciteImmobile).Within(1e-4f), "immobile : pas de chatoiement");
            Assert.That(immobile, Is.LessThan(0.1f), "on ne le voit pas à 10 m");
            float plusFort = 0f;
            for (float t = 0f; t < 1f; t += 0.01f) plusFort = Mathf.Max(plusFort, ReglesCamouflage.Opacite(6f, t, 0f, reglages));
            Assert.That(plusFort, Is.GreaterThan(immobile * 3f), "en course, il chatoie");
            Assert.That(plusFort, Is.LessThanOrEqualTo(1f));
        }

        [Test]
        public void DeuxCamouflesNeChatoientPasEnPhase()
        {
            float a = ReglesCamouflage.Opacite(6f, 0.1f, 0f, reglages), b = ReglesCamouflage.Opacite(6f, 0.1f, 0.618f, reglages);
            Assert.That(a, Is.Not.EqualTo(b).Within(1e-3f));
        }

        [Test]
        public void SixSecondesPuisLesVraisMateriauxReviennent()
        {
            var (froggy, effet) = Preparer1();
            var rendu = froggy.GetComponent<Renderer>();
            var avant = rendu.sharedMaterial;
            Assert.That(effet.Camoufler(froggy), Is.True);
            Assert.That(rendu.sharedMaterial, Is.Not.SameAs(avant));
            effet.Dessiner(1f, 0f);
            Assert.That(effet.Opacite(froggy), Is.LessThan(0.1f));
            for (int i = 0; i < 290; i++) effet.Pas(0.02f);   // 5,8 s
            Assert.That(effet.EstCamoufle(froggy), Is.True);
            for (int i = 0; i < 15; i++) effet.Pas(0.02f);
            effet.Dessiner(1f, 0f);
            Assert.That(effet.EstCamoufle(froggy), Is.False);
            Assert.That(rendu.sharedMaterial, Is.SameAs(avant), "le vrai matériau revient");
        }

        [Test]
        public void LOndeDuCroassementLeRevele()
        {
            var (froggy, effet) = Preparer1();
            effet.Camoufler(froggy);
            effet.Dessiner(1f, 0f);   // fondu d'entrée fini : presque invisible
            effet.Onde(new Vector3(10f, 0f, 0f), 4f);
            Assert.That(effet.EstCamoufle(froggy), Is.True, "trop loin : rien");
            effet.Onde(new Vector3(3f, 0f, 0f), 4f);
            Assert.That(effet.EstCamoufle(froggy), Is.False, "touché par l'onde : révélé");
            effet.Dessiner(0.1f, 0f);
            Assert.That(effet.Opacite(froggy), Is.GreaterThan(0.04f).And.LessThan(1f), "il réapparaît en fondu");
        }

        [Test]
        public void LaPluieLeReveleEtLEmpecheDeSeCacher()
        {
            var (froggy, effet) = Preparer1();
            effet.Camoufler(froggy);
            effet.DebutMeteo(pluie);
            Assert.That(effet.EstCamoufle(froggy), Is.False, "la pluie le révèle");
            effet.Dessiner(1f, 0f);
            Assert.That(effet.Camoufler(froggy), Is.False, "sous la pluie, ça ne prend pas");
            effet.FinMeteo(pluie);
            Assert.That(effet.Camoufler(froggy), Is.True, "après la pluie, de nouveau");
        }

        [Test]
        public void UnCookiePorteResteVisible()
        {
            var (froggy, effet) = Preparer1();
            var cookie = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cookie.name = "Cookie porté";
            cookie.transform.SetParent(froggy.transform, false);
            var avant = cookie.GetComponent<Renderer>().sharedMaterial;
            effet.Camoufler(froggy);
            Assert.That(cookie.GetComponent<Renderer>().sharedMaterial, Is.SameAs(avant));
        }

        private (GameObject froggy, EffetCamouflage effet) Preparer1()
        {
            scene = EditorSceneManager.NewPreviewScene();
            var froggy = GameObject.CreatePrimitive(PrimitiveType.Cube);
            SceneManager.MoveGameObjectToScene(froggy, scene);
            var hote = new GameObject("Effet");
            SceneManager.MoveGameObjectToScene(hote, scene);
            var effet = hote.AddComponent<EffetCamouflage>();
            effet.Regler(reglages);
            return (froggy, effet);
        }
    }
}
