using System.Collections.Generic;
using Froggy.Meteo;
using Froggy.Socle;
using NUnit.Framework;
using UnityEngine;

namespace Froggy.Outils.Tests
{
    /// <summary>
    /// Gestionnaire de météo et d'événements (P5-01). Les règles seules (ReglesMeteo) : calme, annonce, actif, retour au
    /// calme ; tirage selon les poids (0 ne sort jamais), même graine même suite. Puis le gestionnaire : un événement
    /// factice est tiré, annoncé, appliqué aux Froggy par les modificateurs du socle, puis retiré.
    /// </summary>
    public class MeteoTests
    {
        private const float Dt = 1f / 50f;
        private readonly List<Object> aDetruire = new List<Object>();

        [TearDown]
        public void Ranger()
        {
            foreach (var o in aDetruire) if (o != null) Object.DestroyImmediate(o);
            aDetruire.Clear();
        }

        private static List<(ChangementMeteo, int)> Avancer(ReglesMeteo regles, float secondes)
        {
            var changements = new List<(ChangementMeteo, int)>();
            int pas = Mathf.RoundToInt(secondes / Dt);
            for (int i = 0; i < pas; i++) regles.Pas(Dt, changements);
            return changements;
        }

        [Test]
        public void CalmeAnnonceActifPuisCalme()
        {
            var regles = new ReglesMeteo(new[] { 1f }, new[] { 10f }, 3f, 5f, 5f, 42);
            Assert.That(regles.Phase, Is.EqualTo(PhaseMeteo.Calme));
            Assert.That(Avancer(regles, 4.9f), Is.Empty, "5 s de calme");

            var c = Avancer(regles, 0.2f);
            Assert.That(c, Is.EqualTo(new[] { (ChangementMeteo.Annonce, 0) }));
            Assert.That(regles.Phase, Is.EqualTo(PhaseMeteo.Annonce));
            Assert.That(Avancer(regles, 2.8f), Is.Empty, "annoncé 3 s avant");

            c = Avancer(regles, 0.2f);
            Assert.That(c, Is.EqualTo(new[] { (ChangementMeteo.Debut, 0) }));
            Assert.That(regles.Phase, Is.EqualTo(PhaseMeteo.Actif));
            Assert.That(Avancer(regles, 9.8f), Is.Empty, "actif 10 s");

            c = Avancer(regles, 0.2f);
            Assert.That(c, Is.EqualTo(new[] { (ChangementMeteo.Fin, 0) }));
            Assert.That(regles.Phase, Is.EqualTo(PhaseMeteo.Calme));
            Assert.That(regles.Courant, Is.EqualTo(-1));
        }

        [Test]
        public void LesPoidsDecidentCeQuiSort()
        {
            var regles = new ReglesMeteo(new[] { 1f, 0f, 3f }, new[] { 1f, 1f, 1f }, 0f, 1f, 1f, 7);
            var compte = new int[3];
            for (int i = 0; i < 4000; i++) compte[regles.Tirer()]++;
            Assert.That(compte[1], Is.EqualTo(0), "un poids de 0 ne sort jamais");
            float rapport = compte[2] / (float)compte[0];
            Assert.That(rapport, Is.InRange(2.6f, 3.4f), $"poids 3 contre 1 : sorti {compte[2]} fois contre {compte[0]}");

            Assert.That(new ReglesMeteo(new[] { 0f }, new[] { 1f }, 0f, 1f, 1f, 7).Tirer(), Is.EqualTo(-1), "rien ne peut sortir");
        }

        [Test]
        public void MemeGraineMemeSuite()
        {
            var a = new ReglesMeteo(new[] { 1f, 1f, 1f }, new[] { 2f, 3f, 4f }, 1f, 2f, 9f, 123);
            var b = new ReglesMeteo(new[] { 1f, 1f, 1f }, new[] { 2f, 3f, 4f }, 1f, 2f, 9f, 123);
            Assert.That(Avancer(a, 120f), Is.EqualTo(Avancer(b, 120f)), "l'hôte et les autres voient la même météo");
        }

        [Test]
        public void LesEffetsSEcriventEnToutesLettres()
        {
            var e = ScriptableObject.CreateInstance<DefinitionEvenement>();
            aDetruire.Add(e);
            Assert.That(e.Effets, Is.Empty, "sans effet, rien à écrire");
            e.facteurVitesse = 1.3f;
            Assert.That(e.Effets, Is.EqualTo("vitesse +30 %"));
            e.facteurSaut = 0.5f;
            Assert.That(e.Effets, Is.EqualTo("vitesse +30 % · saut −50 %"));
        }

        [Test]
        public void SansEvenementPossibleLaMeteoResteCalme()
        {
            var regles = new ReglesMeteo(new float[0], new float[0], 1f, 1f, 1f, 1);
            Assert.That(Avancer(regles, 30f), Is.Empty);
            Assert.That(regles.Phase, Is.EqualTo(PhaseMeteo.Calme));
        }

        [Test]
        public void UnEvenementFacticeEstTireAnnonceAppliquePuisRetire()
        {
            var factice = ScriptableObject.CreateInstance<DefinitionEvenement>();
            var reglages = ScriptableObject.CreateInstance<ReglagesMeteo>();
            var froggy = new GameObject("Froggy");
            var hote = new GameObject("Météo");
            aDetruire.Add(factice);
            aDetruire.Add(reglages);
            aDetruire.Add(froggy);
            aDetruire.Add(hote);
            factice.nom = "Averse d'essai";
            factice.duree = 4f;
            factice.facteurVitesse = 1.5f;
            factice.facteurSaut = 0.5f;
            reglages.evenements.Add(new ReglagesMeteo.Tirage { evenement = factice, poids = 1f });
            reglages.annonce = 2f;
            reglages.pauseMin = reglages.pauseMax = 3f;
            reglages.graine = 5;
            var modificateurs = froggy.AddComponent<Modificateurs>();
            var meteo = hote.AddComponent<GestionnaireMeteo>();
            meteo.Regler(reglages, new[] { modificateurs });

            var vus = new List<string>();
            meteo.Annonce += e => vus.Add("annonce " + e.nom);
            meteo.Debut += e => vus.Add("début " + e.nom);
            meteo.Fin += e => vus.Add("fin " + e.nom);
            meteo.Demarrer();

            for (int i = 0; i < 160; i++) meteo.Pas(Dt);   // 3,2 s : annoncé
            Assert.That(vus, Is.EqualTo(new[] { "annonce Averse d'essai" }));
            Assert.That(meteo.Courant, Is.SameAs(factice));
            Assert.That(modificateurs.Vitesse, Is.EqualTo(1f), "annoncé, pas encore appliqué");

            for (int i = 0; i < 100; i++) meteo.Pas(Dt);   // 5,2 s : en cours
            Assert.That(vus.Count, Is.EqualTo(2));
            Assert.That(modificateurs.Vitesse, Is.EqualTo(1.5f), "appliqué par les modificateurs du socle");
            Assert.That(modificateurs.Saut, Is.EqualTo(0.5f));

            for (int i = 0; i < 200; i++) meteo.Pas(Dt);   // 9,2 s : fini
            Assert.That(vus, Is.EqualTo(new[] { "annonce Averse d'essai", "début Averse d'essai", "fin Averse d'essai" }));
            Assert.That(modificateurs.Vitesse, Is.EqualTo(1f), "retiré");
            Assert.That(modificateurs.Saut, Is.EqualTo(1f));
            Assert.That(meteo.Courant, Is.Null);
        }

        [Test]
        public void LaFinDeLaPartieRetireLEvenementEnCours()
        {
            var factice = ScriptableObject.CreateInstance<DefinitionEvenement>();
            var reglages = ScriptableObject.CreateInstance<ReglagesMeteo>();
            var froggy = new GameObject("Froggy");
            var hote = new GameObject("Météo");
            aDetruire.Add(factice);
            aDetruire.Add(reglages);
            aDetruire.Add(froggy);
            aDetruire.Add(hote);
            factice.facteurVitesse = 2f;
            reglages.evenements.Add(new ReglagesMeteo.Tirage { evenement = factice });
            reglages.annonce = 0f;
            reglages.pauseMin = reglages.pauseMax = 0.1f;
            var modificateurs = froggy.AddComponent<Modificateurs>();
            var meteo = hote.AddComponent<GestionnaireMeteo>();
            meteo.Regler(reglages, new[] { modificateurs });
            meteo.Demarrer();
            for (int i = 0; i < 10; i++) meteo.Pas(Dt);
            Assert.That(modificateurs.Vitesse, Is.EqualTo(2f));

            meteo.Arreter();
            Assert.That(modificateurs.Vitesse, Is.EqualTo(1f), "plus d'effet après la fin");
            for (int i = 0; i < 100; i++) meteo.Pas(Dt);
            Assert.That(modificateurs.Vitesse, Is.EqualTo(1f), "et plus de tirage");
        }
    }
}
