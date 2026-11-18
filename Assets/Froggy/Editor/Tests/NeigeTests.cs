using System.Collections.Generic;
using Froggy.Meteo;
using Froggy.Socle;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Froggy.Outils.Tests
{
    /// <summary>
    /// Neige (P5-05) : la vraie fiche du projet ralentit les grenouilles au début de la neige, et leur vitesse revient à
    /// la normale à la fin. La fiche du jardin la propose, à côté de la pluie (choix de Lucas, 18 novembre 2026).
    /// </summary>
    public class NeigeTests
    {
        private const string CheminNeige = "Assets/Froggy/Postes/5_MeteoEvenements/Neige.asset";
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
        public void LaNeigeRalentitLesGrenouilles()
        {
            var neige = AssetDatabase.LoadAssetAtPath<DefinitionEvenement>(CheminNeige);
            Assert.That(neige, Is.Not.Null, CheminNeige);
            Assert.That(neige.facteurVitesse, Is.LessThan(1f), "Notion : les déplacements ralentissent pendant la neige");
            Assert.That(neige.facteurVitesse, Is.GreaterThan(0f), "ralentir, pas figer");
            Assert.That(neige.facteurSaut, Is.EqualTo(1f), "rien d'autre ne change");
            Assert.That(neige.Effets, Does.Contain("vitesse −"), "l'annonce dit que l'on ralentit");
        }

        [Test]
        public void LeJardinProposeLaNeigeEtLaPluie()
        {
            var jardin = AssetDatabase.LoadAssetAtPath<ReglagesMeteo>(CheminJardin);
            Assert.That(jardin, Is.Not.Null, CheminJardin);
            Assert.That(jardin.evenements.Exists(t => t.evenement != null && t.evenement.name == "Neige" && t.poids > 0f));
            Assert.That(jardin.evenements.Exists(t => t.evenement != null && t.evenement.name == "PluieTropicale" && t.poids > 0f));
        }

        [Test]
        public void LaVitesseBaisseAuDebutEtRevientALaFin()
        {
            var neige = AssetDatabase.LoadAssetAtPath<DefinitionEvenement>(CheminNeige);
            var reglages = ScriptableObject.CreateInstance<ReglagesMeteo>();
            var froggy = new GameObject("Froggy");
            var hote = new GameObject("Météo");
            aDetruire.Add(reglages);
            aDetruire.Add(froggy);
            aDetruire.Add(hote);
            reglages.evenements.Add(new ReglagesMeteo.Tirage { evenement = neige, poids = 1f });
            reglages.annonce = 1f;
            reglages.pauseMin = reglages.pauseMax = 1f;
            var modificateurs = froggy.AddComponent<Modificateurs>();
            var meteo = hote.AddComponent<GestionnaireMeteo>();
            meteo.Regler(reglages, new[] { modificateurs });
            meteo.Demarrer();

            for (int i = 0; i < 95; i++) meteo.Pas(Dt);   // 1,9 s : annoncée, pas commencée
            Assert.That(modificateurs.Vitesse, Is.EqualTo(1f));
            for (int i = 0; i < 10; i++) meteo.Pas(Dt);   // 2,1 s : il neige
            Assert.That(modificateurs.Vitesse, Is.EqualTo(neige.facteurVitesse), "la vitesse baisse au début de la neige");
            int fin = Mathf.CeilToInt(neige.duree / Dt) + 5;
            for (int i = 0; i < fin; i++) meteo.Pas(Dt);
            Assert.That(modificateurs.Vitesse, Is.EqualTo(1f), "et revient à la normale à la fin");
        }
    }
}
