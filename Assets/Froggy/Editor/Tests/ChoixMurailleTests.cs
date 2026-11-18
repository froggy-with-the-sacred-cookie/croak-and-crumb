using Froggy.Jardin;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Froggy.Outils.Tests
{
    /// <summary>
    /// Le choix du contour de l'arène (menu de debug) : dans la scène du temple, un seul contour allumé à la fois, et le
    /// contour actuel se lit d'après ce qui est allumé ; la scène de la muraille de Chine se reconnaît.
    /// </summary>
    public class ChoixMurailleTests
    {
        private GameObject menu, artisanale, simple, bambous;

        [SetUp]
        public void Preparer()
        {
            artisanale = new GameObject("Muraille artisanale");
            simple = new GameObject("Muraille");
            bambous = new GameObject("Bordure de bambous");
            simple.SetActive(false);
            bambous.SetActive(false);
            menu = new GameObject("Menu debug caméra");
            var so = new SerializedObject(menu.AddComponent<ChoixMuraille>());
            so.FindProperty("artisanale").objectReferenceValue = artisanale;
            so.FindProperty("simple").objectReferenceValue = simple;
            so.FindProperty("bambous").objectReferenceValue = bambous;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void Nettoyer()
        {
            foreach (var go in new[] { menu, artisanale, simple, bambous }) Object.DestroyImmediate(go);
        }

        [TestCase(ChoixMuraille.Contour.TempleArtisanal)]
        [TestCase(ChoixMuraille.Contour.TempleSimple)]
        [TestCase(ChoixMuraille.Contour.Bambous)]
        public void UnSeulContourAllume(ChoixMuraille.Contour contour)
        {
            var choix = menu.GetComponent<ChoixMuraille>();
            choix.Choisir(contour);
            Assert.That(artisanale.activeSelf, Is.EqualTo(contour == ChoixMuraille.Contour.TempleArtisanal));
            Assert.That(simple.activeSelf, Is.EqualTo(contour == ChoixMuraille.Contour.TempleSimple));
            Assert.That(bambous.activeSelf, Is.EqualTo(contour == ChoixMuraille.Contour.Bambous));
            Assert.That(choix.Actuel, Is.EqualTo(contour));
        }

        [Test]
        public void LaSceneDeChineSeReconnait()
        {
            var so = new SerializedObject(menu.GetComponent<ChoixMuraille>());
            so.FindProperty("estChine").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
            Assert.That(menu.GetComponent<ChoixMuraille>().Actuel, Is.EqualTo(ChoixMuraille.Contour.Chine));
        }
    }
}
