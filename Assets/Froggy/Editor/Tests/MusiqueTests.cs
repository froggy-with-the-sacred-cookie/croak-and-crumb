using Froggy.Audio;
using Froggy.Regles;
using NUnit.Framework;
using UnityEngine;

namespace Froggy.Outils.Tests
{
    /// <summary>La musique (P6-08) : quel morceau joue selon la partie, les fondus, et les fichiers eux-mêmes.</summary>
    public sealed class MusiqueTests
    {
        private const float Seuil = 60f, Marge = 5f;

        private static PhotoMusique EnJeu(float reste) =>
            new PhotoMusique { PartieConnue = true, Phase = Phase.EnJeu, Reste = reste, Gagnant = ReglesPartie.Personne, Local = 0 };

        private static PhotoMusique Fin(int gagnant, int local) =>
            new PhotoMusique { PartieConnue = true, Phase = Phase.Finie, Gagnant = gagnant, Local = local };

        // ---------------------------------------------------------------- ce qui joue

        [Test]
        public void LeMenuJoueLeTheme()
        {
            var r = new ReglesMusique(Seuil, Marge);
            Assert.That(r.Decider(new PhotoMusique { MenuVisible = true }), Is.EqualTo(EtatMusique.Menu));
        }

        [Test]
        public void SansPartieLaMusiqueSeTait()
        {
            var r = new ReglesMusique(Seuil, Marge);
            Assert.That(r.Decider(new PhotoMusique()), Is.EqualTo(EtatMusique.Silence));
        }

        [TestCase(Phase.Attente)]
        [TestCase(Phase.Decompte)]
        public void AvantLeJeuLeJardinCommenceParSonAmbiance(Phase phase)
        {
            var r = new ReglesMusique(Seuil, Marge);
            var p = new PhotoMusique { PartieConnue = true, Phase = phase, Reste = 3f };
            Assert.That(r.Decider(p), Is.EqualTo(EtatMusique.Depart));
        }

        [Test]
        public void LaTensionEntreALaDerniereMinute()
        {
            var r = new ReglesMusique(Seuil, Marge);
            Assert.That(r.Decider(EnJeu(180f)), Is.EqualTo(EtatMusique.Partie));
            Assert.That(r.Decider(EnJeu(60.01f)), Is.EqualTo(EtatMusique.Partie));
            Assert.That(r.Decider(EnJeu(60f)), Is.EqualTo(EtatMusique.DerniereMinute));
        }

        [Test]
        public void UnTempsQuiRemonteDunPoilNeFaitPasClignoterLaTension()
        {
            var r = new ReglesMusique(Seuil, Marge);
            r.Decider(EnJeu(59f));
            Assert.That(r.Decider(EnJeu(62f)), Is.EqualTo(EtatMusique.DerniereMinute), "dans la marge");
            Assert.That(r.Decider(EnJeu(Seuil + Marge + 1f)), Is.EqualTo(EtatMusique.Partie), "au-delà de la marge");
        }

        [Test]
        public void LaProlongationGardeLaTension()
        {
            var r = new ReglesMusique(Seuil, Marge);
            var p = new PhotoMusique { PartieConnue = true, Phase = Phase.Prolongation, Reste = 0f };
            Assert.That(r.Decider(p), Is.EqualTo(EtatMusique.DerniereMinute));
        }

        [Test]
        public void LaFinDitVictoireOuDefaiteSelonLeJoueurDeCetteMachine()
        {
            Assert.That(new ReglesMusique(Seuil, Marge).Decider(Fin(gagnant: 2, local: 2)), Is.EqualTo(EtatMusique.Victoire));
            Assert.That(new ReglesMusique(Seuil, Marge).Decider(Fin(gagnant: 1, local: 2)), Is.EqualTo(EtatMusique.Defaite));
            Assert.That(new ReglesMusique(Seuil, Marge).Decider(Fin(gagnant: 1, local: ReglesPartie.Personne)),
                Is.EqualTo(EtatMusique.Victoire), "sans joueur sur cette machine, on fête le gagnant");
        }

        [Test]
        public void LaFinNeChangePlusUneFoisDecidee()
        {
            var r = new ReglesMusique(Seuil, Marge);
            r.Decider(Fin(gagnant: 1, local: 2));
            Assert.That(r.Decider(Fin(gagnant: 2, local: 2)), Is.EqualTo(EtatMusique.Defaite));
        }

        [Test]
        public void RejouerRepartDuDepart()
        {
            var r = new ReglesMusique(Seuil, Marge);
            r.Decider(EnJeu(30f));
            r.Decider(Fin(gagnant: 0, local: 0));
            var p = new PhotoMusique { PartieConnue = true, Phase = Phase.Decompte, Reste = 3f };
            Assert.That(r.Decider(p), Is.EqualTo(EtatMusique.Depart));
            Assert.That(r.Decider(EnJeu(180f)), Is.EqualTo(EtatMusique.Partie));
        }

        // ---------------------------------------------------------------- le dosage

        [Test]
        public void LaTensionNeSonneQuALaDerniereMinute()
        {
            foreach (EtatMusique e in System.Enum.GetValues(typeof(EtatMusique)))
                Assert.That(ReglesMusique.DosageDe(e).Tension > 0f, Is.EqualTo(e == EtatMusique.DerniereMinute), e.ToString());
        }

        [Test]
        public void LaTensionAjouteSansRienRetirer()
        {
            Dosage partie = ReglesMusique.DosageDe(EtatMusique.Partie), fin = ReglesMusique.DosageDe(EtatMusique.DerniereMinute);
            foreach (Couche c in new[] { Couche.Ambiance, Couche.Melodie, Couche.Rythme })
                Assert.That(fin[c], Is.EqualTo(partie[c]), c.ToString());
        }

        [Test]
        public void LesJinglesJouentSurUnJardinMuet()
        {
            foreach (var e in new[] { EtatMusique.Victoire, EtatMusique.Defaite })
            {
                Assert.That(ReglesMusique.JoueUnJingle(e), Is.True);
                Assert.That(ReglesMusique.DosageDe(e).Jardin, Is.False);
            }
        }

        [Test]
        public void SeulsLesChangementsDansLeJardinAttendentLaMesure()
        {
            Assert.That(ReglesMusique.AttendLaMesure(EtatMusique.Partie, EtatMusique.DerniereMinute), Is.True);
            Assert.That(ReglesMusique.AttendLaMesure(EtatMusique.Depart, EtatMusique.Partie), Is.True);
            Assert.That(ReglesMusique.AttendLaMesure(EtatMusique.DerniereMinute, EtatMusique.Victoire), Is.False, "la fin n'attend jamais");
            Assert.That(ReglesMusique.AttendLaMesure(EtatMusique.Menu, EtatMusique.Depart), Is.False);
        }

        [Test]
        public void LaProchaineMesureTombeSurUneBarre()
        {
            const double mesure = 4 * 60d / 112d;
            Assert.That(ReglesMusique.ProchaineMesure(10d, 10d + 0.3 * mesure, mesure), Is.EqualTo(10d + mesure).Within(1e-9));
            Assert.That(ReglesMusique.ProchaineMesure(10d, 10d + 2 * mesure, mesure), Is.EqualTo(10d + 2 * mesure).Within(1e-9));
            Assert.That(ReglesMusique.ProchaineMesure(10d, 5d, mesure), Is.EqualTo(10d), "avant le départ : le départ");
        }

        // ---------------------------------------------------------------- les fondus

        [Test]
        public void UnFonduRejointSaCibleEnSaDuree()
        {
            var m = new MelangeurMusique();
            m.Viser(ReglesMusique.DosageDe(EtatMusique.Partie), 0d, 0d, montee: 2f, descente: 1f);
            m.Avancer(0d, 1f);
            Assert.That(m.Volume(Couche.Melodie), Is.EqualTo(.5f).Within(1e-5));
            m.Avancer(1d, 1.5f);
            Assert.That(m.Volume(Couche.Melodie), Is.EqualTo(1f));
            Assert.That(m.Volume(Couche.Tension), Is.Zero);
        }

        [Test]
        public void LaTensionAttendLaMesurePourEntrer()
        {
            var m = new MelangeurMusique();
            m.Viser(ReglesMusique.DosageDe(EtatMusique.Partie), 0d, 0d, 0f, 0f);
            m.Avancer(0d, 0f);
            m.Viser(ReglesMusique.DosageDe(EtatMusique.DerniereMinute), aPartirDe: 5d, maintenant: 4d, montee: 1f, descente: 1f);
            m.Avancer(4.5d, .5f);
            Assert.That(m.EnAttente, Is.True);
            Assert.That(m.Volume(Couche.Tension), Is.Zero, "avant la barre");
            m.Avancer(5d, .25f);
            Assert.That(m.Volume(Couche.Tension), Is.EqualTo(.25f).Within(1e-5), "dès la barre");
        }

        [Test]
        public void LeJardinMuetSeReconnaitEtRepartDeZero()
        {
            var m = new MelangeurMusique();
            Assert.That(m.JardinMuet, Is.True);
            m.Viser(ReglesMusique.DosageDe(EtatMusique.Depart), 1d, 0d, 1f, 1f);
            Assert.That(m.JardinMuet, Is.False, "un dosage en attente compte");
            m.Avancer(1d, 1f);
            m.Viser(ReglesMusique.DosageDe(EtatMusique.Victoire), 2d, 2d, 1f, .5f);
            m.Avancer(2d, .5f);
            Assert.That(m.JardinMuet, Is.True);
        }

        // ---------------------------------------------------------------- les fichiers

        [Test]
        public void LesReglagesSontChargesEtCompletes()
        {
            var r = Resources.Load<ReglagesMusique>(ReglagesMusique.Chemin);
            Assert.That(r, Is.Not.Null, "Resources/" + ReglagesMusique.Chemin);
            foreach (Couche c in System.Enum.GetValues(typeof(Couche)))
                Assert.That(r.Clip(c), Is.Not.Null, c.ToString());
            Assert.That(r.victoire, Is.Not.Null);
            Assert.That(r.defaite, Is.Not.Null);
            Assert.That(r.menu.loadType, Is.EqualTo(AudioClipLoadType.Streaming), "musique longue : lue en flux");
        }

        [Test]
        public void LesCouchesDuJardinOntExactementLaMemeLongueur()
        {
            var r = Resources.Load<ReglagesMusique>(ReglagesMusique.Chemin);
            AudioClip reference = r.ambiance;
            foreach (var c in new[] { Couche.Melodie, Couche.Rythme, Couche.Tension })
            {
                AudioClip clip = r.Clip(c);
                Assert.That(clip.samples, Is.EqualTo(reference.samples), clip.name);
                Assert.That(clip.frequency, Is.EqualTo(reference.frequency), clip.name);
                Assert.That(clip.channels, Is.EqualTo(reference.channels), clip.name);
            }
        }

        [Test]
        public void LaBoucleDuJardinFaitUnNombreEntierDeMesures()
        {
            var r = Resources.Load<ReglagesMusique>(ReglagesMusique.Chemin);
            double duree = (double)r.ambiance.samples / r.ambiance.frequency;
            double mesures = duree / r.DureeMesure;
            Assert.That(mesures, Is.EqualTo(System.Math.Round(mesures)).Within(1e-3), $"{mesures} mesures : le tempo ne colle pas");
            Assert.That(System.Math.Round(mesures), Is.EqualTo(32d));
        }

        [Test]
        public void SansReglagesLaMusiqueResteMuette()
        {
            var racine = new GameObject("Musique sans réglages");
            try
            {
                var musique = racine.AddComponent<MusiqueJeu>();
                Assert.That(musique.Etat, Is.EqualTo(EtatMusique.Silence));
            }
            finally { Object.DestroyImmediate(racine); }
        }
    }
}
