using Froggy.Jardin;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Froggy.Outils.Tests
{
    /// <summary>
    /// La voltige de sakura (menu de debug) : chaque version s'allume et s'éteint seule, la densité règle le débit des
    /// particules sur la surface de la vitrine, et deux boucles posées à deux places ne partent pas en phase.
    /// </summary>
    public class VoltigeSakuraTests
    {
        private GameObject racine, boucles;
        private ParticleSystem particules;

        [SetUp]
        public void Preparer()
        {
            racine = new GameObject("Voltige de sakura");
            particules = new GameObject("Pétales au vent").AddComponent<ParticleSystem>();
            particules.transform.SetParent(racine.transform);
            boucles = new GameObject("Boucles précalculées");
            boucles.transform.SetParent(racine.transform);
            boucles.SetActive(false);
            var so = new SerializedObject(racine.AddComponent<VoltigeSakura>());
            so.FindProperty("auVent").objectReferenceValue = particules;
            so.FindProperty("boucles").objectReferenceValue = boucles;
            so.FindProperty("surface").floatValue = 2000f;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void Nettoyer() => Object.DestroyImmediate(racine);

        [Test]
        public void ChaqueVersionSeBasculeSeule()
        {
            var v = racine.GetComponent<VoltigeSakura>();
            Assert.That(v.AuVent, Is.True);
            Assert.That(v.Boucle, Is.False);
            v.AuVent = false;
            v.Boucle = true;
            Assert.That(particules.gameObject.activeSelf, Is.False);
            Assert.That(boucles.activeSelf, Is.True);
            Assert.That(v.AuVent, Is.False);
            Assert.That(v.Boucle, Is.True);
        }

        [Test]
        public void LaDensiteRegleLeDebit()
        {
            var v = racine.GetComponent<VoltigeSakura>();
            v.Densite = 0.03f;
            Assert.That(particules.emission.rateOverTime.constant, Is.EqualTo(60f).Within(1e-3f));
            v.Densite = -1f;
            Assert.That(particules.emission.rateOverTime.constant, Is.EqualTo(0f));
        }

        [Test]
        public void DeuxPlacesDeuxPhasesStables()
        {
            var a = new Vector3(-11f, 1.1f, -8f);
            var b = new Vector3(5f, 0f, -9f);
            Assert.That(VoltigeSakura.Phase(a), Is.EqualTo(VoltigeSakura.Phase(a)));
            Assert.That(VoltigeSakura.Phase(a), Is.Not.EqualTo(VoltigeSakura.Phase(b)));
            foreach (var p in new[] { a, b, Vector3.zero, new Vector3(8f, 2.1f, 16f) })
                Assert.That(VoltigeSakura.Phase(p), Is.InRange(0f, 0.99999f));
        }
    }
}
