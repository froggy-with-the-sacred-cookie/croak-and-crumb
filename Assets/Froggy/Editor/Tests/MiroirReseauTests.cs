using System.Collections.Generic;
using Froggy.Meteo;
using Froggy.Regles;
using NUnit.Framework;
using UnityEngine;

namespace Froggy.Outils.Tests
{
    /// <summary>
    /// Météo et cookie sacré chez un joueur qui n'est pas l'hôte (#128) : les règles prennent l'état reçu (Imposer) et
    /// rendent les mêmes changements que chez l'hôte (annonce, début, fin), même quand un message arrive en retard.
    /// </summary>
    public class MiroirReseauTests
    {
        private static ReglesMeteo Meteo() =>
            new ReglesMeteo(new List<float> { 1f, 1f }, new List<float> { 10f, 20f }, 3f, 5f, 5f, 1);

        [Test]
        public void LaMeteoRecueDonneAnnonceDebutEtFin()
        {
            var r = Meteo();
            var ch = new List<(ChangementMeteo, int)>();

            r.Imposer(PhaseMeteo.Annonce, 1, 3f, ch);
            Assert.That(ch, Is.EqualTo(new[] { (ChangementMeteo.Annonce, 1) }));
            Assert.That(r.Phase, Is.EqualTo(PhaseMeteo.Annonce));
            Assert.That(r.Courant, Is.EqualTo(1));

            ch.Clear();
            r.Imposer(PhaseMeteo.Annonce, 1, 2.5f, ch);
            Assert.That(ch, Is.Empty, "le temps qui passe seul ne change rien");
            Assert.That(r.Reste, Is.EqualTo(2.5f));

            ch.Clear();
            r.Imposer(PhaseMeteo.Actif, 1, 20f, ch);
            Assert.That(ch, Is.EqualTo(new[] { (ChangementMeteo.Debut, 1) }));

            ch.Clear();
            r.Imposer(PhaseMeteo.Calme, -1, 5f, ch);
            Assert.That(ch, Is.EqualTo(new[] { (ChangementMeteo.Fin, 1) }));
            Assert.That(r.Courant, Is.EqualTo(-1));
        }

        [Test]
        public void UneAnnonceManqueePasseAuDebut()
        {
            var r = Meteo();
            var ch = new List<(ChangementMeteo, int)>();
            r.Imposer(PhaseMeteo.Actif, 0, 10f, ch);
            Assert.That(ch, Is.EqualTo(new[] { (ChangementMeteo.Debut, 0) }));
        }

        [Test]
        public void UnAutreEvenementFinitLePremier()
        {
            var r = Meteo();
            var ch = new List<(ChangementMeteo, int)>();
            r.Imposer(PhaseMeteo.Actif, 0, 10f, ch);
            ch.Clear();
            r.Imposer(PhaseMeteo.Actif, 1, 20f, ch);
            Assert.That(ch, Is.EqualTo(new[] { (ChangementMeteo.Fin, 0), (ChangementMeteo.Debut, 1) }));
        }

        [Test]
        public void UnRangInconnuRevientAuCalme()
        {
            var r = Meteo();
            var ch = new List<(ChangementMeteo, int)>();
            r.Imposer(PhaseMeteo.Actif, 7, 10f, ch);
            Assert.That(ch, Is.Empty);
            Assert.That(r.Phase, Is.EqualTo(PhaseMeteo.Calme));
            Assert.That(r.Courant, Is.EqualTo(-1));
        }

        [Test]
        public void LeCookieSacreRecuSuitLHote()
        {
            var r = new ReglesCookieSacre(60f, 1f, 1f, 1f);
            r.Imposer(EtatCookieSacre.Porte, 2, Vector3.zero);
            Assert.That(r.Etat, Is.EqualTo(EtatCookieSacre.Porte));
            Assert.That(r.Porteur, Is.EqualTo(2));

            var sol = new Vector3(1f, 0f, 3f);
            r.Imposer(EtatCookieSacre.AuSol, 2, sol);
            Assert.That(r.Porteur, Is.EqualTo(ReglesPartie.Personne), "au sol, personne ne le porte");
            Assert.That(r.PositionAuSol, Is.EqualTo(sol));
        }
    }
}
