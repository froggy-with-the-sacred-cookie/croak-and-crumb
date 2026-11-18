using System;
using System.Collections.Generic;
using Froggy.Audio;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Froggy.Outils.Tests
{
    public sealed class SonsTests
    {
        [TestCase(3)]
        [TestCase(5)]
        public void ChaqueTourUtiliseToutesLesVariantesSansRepetitionImmediate(int nombre)
        {
            var alea = new VariantesSansRepetition(nombre, new System.Random(68));
            int precedent = -1;
            for (int tour = 0; tour < 50; tour++)
            {
                var deja = new HashSet<int>();
                for (int i = 0; i < nombre; i++)
                {
                    int suivant = alea.Suivante();
                    Assert.That(suivant, Is.Not.EqualTo(precedent));
                    Assert.That(deja.Add(suivant), Is.True);
                    precedent = suivant;
                }
            }
        }

        [Test]
        public void UneVarianteResteJouable()
        {
            var alea = new VariantesSansRepetition(1, new System.Random(68));
            for (int i = 0; i < 3; i++) Assert.That(alea.Suivante(), Is.Zero);
        }

        [Test]
        public void TousLesEvenementsOntDesVariantesMonoDistinctes()
        {
            var catalogue = AssetDatabase.LoadAssetAtPath<CatalogueSons>("Assets/Froggy/Audio/Froggy_Sons.asset");
            Assert.That(catalogue, Is.Not.Null);
            var evenements = new HashSet<IdSon>();
            foreach (var son in catalogue.Evenements)
            {
                Assert.That(evenements.Add(son.id), Is.True, son.id.ToString());
                Assert.That(son.variantes.Length, Is.InRange(3, 5), son.id.ToString());
                var clips = new HashSet<AudioClip>();
                foreach (var clip in son.variantes)
                {
                    Assert.That(clip, Is.Not.Null, son.id.ToString());
                    Assert.That(clips.Add(clip), Is.True, son.id.ToString());
                    Assert.That(clip.channels, Is.EqualTo(1), clip.name);
                }
            }
            Assert.That(evenements.Count, Is.EqualTo(Enum.GetValues(typeof(IdSon)).Length));
        }

        [Test]
        public void LesEvenementsSansCatalogueRestentSilencieux()
        {
            var racine = new GameObject("Lecteur sans catalogue");
            try
            {
                var lecteur = racine.AddComponent<SonsPartie>();
                Assert.That(lecteur.Jouer(IdSon.CookieRamasse, Vector3.zero), Is.False);
            }
            finally { UnityEngine.Object.DestroyImmediate(racine); }
        }
    }
}
