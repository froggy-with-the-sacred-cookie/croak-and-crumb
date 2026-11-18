using System.Collections.Generic;
using Froggy.Meteo;
using Froggy.Socle;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Froggy.Outils.Tests
{
    /// <summary>
    /// Pluie tropicale (P5-02) : la vraie fiche du projet accélère les grenouilles au début de la pluie, et leur vitesse
    /// revient à la normale à la fin. La fiche du jardin la propose.
    /// </summary>
    public class PluieTests
    {
        private const string CheminPluie = "Assets/Froggy/Postes/5_MeteoEvenements/PluieTropicale.asset";
        private const string CheminJardin = "Assets/Froggy/Postes/5_MeteoEvenements/ReglagesMeteo_Jardin.asset";
        private const float Dt = 1f / 50f;
        private readonly List<Object> aDetruire = new List<Object>();

        [TearDown]
        public void Ranger()
        {
            foreach (var o in aDetruire) if (o != null) Object.DestroyImmediate(o);
            aDetruire.Clear();
        }

        [Test]
        public void LaPluieAccelereLesGrenouilles()
        {
            var pluie = AssetDatabase.LoadAssetAtPath<DefinitionEvenement>(CheminPluie);
            Assert.That(pluie, Is.Not.Null, CheminPluie);
            Assert.That(pluie.facteurVitesse, Is.GreaterThan(1f), "Notion : la pluie accélère les grenouilles");
            Assert.That(pluie.facteurSaut, Is.EqualTo(1f), "rien d'autre ne change");
        }

        [Test]
        public void LeJardinProposeLaPluie()
        {
            var jardin = AssetDatabase.LoadAssetAtPath<ReglagesMeteo>(CheminJardin);
            Assert.That(jardin, Is.Not.Null, CheminJardin);
            Assert.That(jardin.evenements.Exists(t => t.evenement != null && t.evenement.name == "PluieTropicale" && t.poids > 0f));
        }

        [Test]
        public void LaVitesseMonteAuDebutEtRevientALaFin()
        {
            var pluie = AssetDatabase.LoadAssetAtPath<DefinitionEvenement>(CheminPluie);
            var reglages = ScriptableObject.CreateInstance<ReglagesMeteo>();
            var froggy = new GameObject("Froggy");
            var hote = new GameObject("Météo");
            aDetruire.Add(reglages);
            aDetruire.Add(froggy);
            aDetruire.Add(hote);
            reglages.evenements.Add(new ReglagesMeteo.Tirage { evenement = pluie, poids = 1f });
            reglages.annonce = 1f;
            reglages.pauseMin = reglages.pauseMax = 1f;
            var modificateurs = froggy.AddComponent<Modificateurs>();
            var meteo = hote.AddComponent<GestionnaireMeteo>();
            meteo.Regler(reglages, new[] { modificateurs });
            meteo.Demarrer();

            for (int i = 0; i < 95; i++) meteo.Pas(Dt);   // 1,9 s : annoncée, pas commencée
            Assert.That(modificateurs.Vitesse, Is.EqualTo(1f));
            for (int i = 0; i < 10; i++) meteo.Pas(Dt);   // 2,1 s : il pleut
            Assert.That(modificateurs.Vitesse, Is.EqualTo(pluie.facteurVitesse), "la vitesse augmente au début de la pluie");
            int fin = Mathf.CeilToInt(pluie.duree / Dt) + 5;
            for (int i = 0; i < fin; i++) meteo.Pas(Dt);
            Assert.That(modificateurs.Vitesse, Is.EqualTo(1f), "et revient à la normale à la fin");
        }
    }
}
