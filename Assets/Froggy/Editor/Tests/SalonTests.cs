using System.Collections.Generic;
using Froggy.Reseau;
using NUnit.Framework;

namespace Froggy.Outils.Tests
{
    /// <summary>
    /// Salon (P2-03), sans réseau : le code (format, saisie, lettres ambiguës) et les règles du salon (4 joueurs au plus,
    /// numéros et couleurs, prêt, lancement, retour après la partie, refus à la porte).
    /// </summary>
    public class SalonTests
    {
        private const int Couleurs = 6;

        private static EtatSalon SalonAvec(params int[] invites)
        {
            var etat = new EtatSalon("KRBF", Couleurs);
            etat.Accueillir(0, hote: true);
            foreach (int id in invites) etat.Accueillir(id, hote: false);
            return etat;
        }

        // ---------------------------------------------------------------- Code

        [Test]
        public void UnCodeAQuatreLettresFacilesALire()
        {
            var hasard = new System.Random(42);
            var vus = new HashSet<string>();
            for (int i = 0; i < 500; i++)
            {
                string code = CodeSalon.Generer(hasard);
                Assert.That(CodeSalon.EstValide(code), Is.True, code);
                Assert.That(code, Does.Not.Contain("O").And.Not.Contain("I"), "O et I se confondent avec 0 et 1");
                vus.Add(code);
            }
            Assert.That(vus.Count, Is.GreaterThan(490), "les codes doivent varier");
        }

        [Test]
        public void LaSaisieEstToleree()
        {
            Assert.That(CodeSalon.Normaliser(" kr-bf "), Is.EqualTo("KRBF"));
            Assert.That(CodeSalon.EstValide(CodeSalon.Normaliser("k r b f")), Is.True);
            Assert.That(CodeSalon.EstValide("KRB"), Is.False, "trop court");
            Assert.That(CodeSalon.EstValide("KRBO"), Is.False, "O n'est jamais tiré");
            Assert.That(CodeSalon.EstValide("KR8F"), Is.False, "pas de chiffres");
            Assert.That(CodeSalon.Normaliser(null), Is.EqualTo(""));
        }

        [Test]
        public void UnCodeEnLigneEstCeluiDeUnity()
        {
            // vrais codes donnés par Unity Relay le 18/11/2026 (P2-05) : 6 majuscules et chiffres
            foreach (string vrai in new[] { "8NTDNR", "TMQJWD", "PCWW7R" })
                Assert.That(CodeSalon.EstValideEnLigne(CodeSalon.Normaliser(vrai)), Is.True, vrai);
            Assert.That(CodeSalon.Normaliser(" tmq-jwd "), Is.EqualTo("TMQJWD"), "tapé en minuscules, avec un tiret");
            Assert.That(CodeSalon.EstValideEnLigne("AB"), Is.False, "trop court");
            Assert.That(CodeSalon.EstValideEnLigne("ABCDEFGHIJKLM"), Is.False, "trop long");
            Assert.That(CodeSalon.EstValideEnLigne("TMQ!WD"), Is.False, "caractère interdit");
            Assert.That(CodeSalon.EstValideEnLigne(null), Is.False);
        }

        [Test]
        public void LeChampDuMenuPrendUnCodeEnLigne()
        {
            // écran « Rejoindre » du poste 6 (P6-03) : en ligne, 6 cases, lettres et chiffres
            var saisie = new Froggy.Interface.CodeEnSaisie(enLigne: true);
            Assert.That(saisie.Longueur, Is.EqualTo(CodeSalon.LongueurEnLigne));
            saisie.Coller("pcww7r");
            Assert.That(saisie.Code, Is.EqualTo("PCWW7R"), "minuscules et chiffres acceptés");
            Assert.That(saisie.Complet, Is.True);
            saisie.Effacer();
            Assert.That(saisie.Complet, Is.False, "une case vide : pas encore");

            var local = new Froggy.Interface.CodeEnSaisie();
            Assert.That(local.Longueur, Is.EqualTo(CodeSalon.Longueur), "réseau local : toujours 4 lettres");
            Assert.That(local.Taper('7'), Is.False, "pas de chiffre en réseau local");
        }

        [Test]
        public void LaReponseDeLHoteSeLit()
        {
            Assert.That(RechercheSalon.Lire("FROGGY!KRBF:7771", "KRBF", out ushort port), Is.True);
            Assert.That(port, Is.EqualTo(7771));
            Assert.That(RechercheSalon.Lire("FROGGY!ABCD:7771", "KRBF", out _), Is.False, "autre salon");
            Assert.That(RechercheSalon.Lire("FROGGY!KRBF:", "KRBF", out _), Is.False);
            Assert.That(RechercheSalon.Lire(null, "KRBF", out _), Is.False);
        }

        // ---------------------------------------------------------------- Porte

        [Test]
        public void LaPorteRefuseAvecLaBonneRaison()
        {
            var etat = SalonAvec(1);
            Assert.That(etat.Examiner("KRBF"), Is.EqualTo(RefusSalon.Aucun));
            Assert.That(etat.Examiner("ABCD"), Is.EqualTo(RefusSalon.CodeInconnu));

            etat.Accueillir(2, false);
            etat.Accueillir(3, false);
            Assert.That(etat.Examiner("KRBF"), Is.EqualTo(RefusSalon.SalonPlein));
            Assert.That(etat.Accueillir(4, false), Is.False, "pas de 5e joueur");
            Assert.That(etat.Membres.Count, Is.EqualTo(4));

            etat.Retirer(3);
            foreach (int id in new[] { 1, 2 }) etat.SeDirePret(id, true);
            Assert.That(etat.Lancer(), Is.True);
            Assert.That(etat.Examiner("KRBF"), Is.EqualTo(RefusSalon.PartieEnCours));
            Assert.That(etat.Accueillir(5, false), Is.False, "personne n'arrive pendant une partie");
        }

        // ---------------------------------------------------------------- Numéros et couleurs

        [Test]
        public void UnNumeroLibereEstRepris()
        {
            var etat = SalonAvec(1, 2);
            Assert.That(Numeros(etat), Is.EqualTo(new[] { 1, 2, 3 }));
            etat.Retirer(1);
            etat.Accueillir(7, false);
            Assert.That(Numeros(etat), Is.EqualTo(new[] { 1, 2, 3 }), "le nouveau prend la place 2");
            Assert.That(etat.Membres[1].ClientId, Is.EqualTo(7));
            Assert.That(etat.Accueillir(7, false), Is.False, "déjà là");
        }

        [Test]
        public void DeuxJoueursNOntJamaisLaMemeCouleur()
        {
            var etat = SalonAvec(1, 2, 3);
            var prises = new HashSet<int>();
            foreach (MembreSalon m in etat.Membres) Assert.That(prises.Add(m.Couleur), Is.True);

            int couleurDe1 = etat.Membres[1].Couleur;
            Assert.That(etat.ChoisirCouleur(2, couleurDe1), Is.False, "couleur déjà prise");
            Assert.That(etat.ChoisirCouleur(2, Couleurs), Is.False, "hors palette");
            Assert.That(etat.ChoisirCouleur(2, 5), Is.True);
            Assert.That(etat.Membres[2].Couleur, Is.EqualTo(5));

            int suivante = etat.CouleurSuivante(3);
            Assert.That(suivante, Is.Not.EqualTo(-1));
            foreach (MembreSalon m in etat.Membres) Assert.That(m.Couleur, Is.Not.EqualTo(suivante));
        }

        // ---------------------------------------------------------------- Lancer et revenir

        [Test]
        public void SeulOnNeLancePas()
        {
            var etat = SalonAvec();
            Assert.That(etat.PeutLancer, Is.False);
            Assert.That(etat.Lancer(), Is.False);
            Assert.That(etat.Phase, Is.EqualTo(PhaseSalon.Salon));
        }

        [Test]
        public void OnLanceQuandTousLesInvitesSontPrets()
        {
            var etat = SalonAvec(1, 2);
            etat.SeDirePret(1, true);
            Assert.That(etat.PeutLancer, Is.False, "le joueur 3 n'est pas prêt");
            etat.SeDirePret(2, true);
            Assert.That(etat.PeutLancer, Is.True, "l'hôte n'a pas à se dire prêt");
            Assert.That(EtatSalon.PeutLancerAvec(etat.Membres, etat.Phase), Is.True, "même règle sur une copie");

            Assert.That(etat.Lancer(), Is.True);
            Assert.That(etat.Phase, Is.EqualTo(PhaseSalon.EnPartie));
            Assert.That(etat.SeDirePret(1, false), Is.False, "plus de changement pendant la partie");
        }

        [Test]
        public void ApresLaPartieOnRevientAuSalonPasPret()
        {
            var etat = SalonAvec(1);
            etat.SeDirePret(1, true);
            etat.Lancer();
            etat.RevenirAuSalon();
            Assert.That(etat.Phase, Is.EqualTo(PhaseSalon.Salon));
            Assert.That(etat.Membres[1].Pret, Is.False, "on se redit prêt pour la partie suivante");
            Assert.That(etat.Membres.Count, Is.EqualTo(2), "tout le monde est encore là");
            Assert.That(etat.PeutLancer, Is.False);
        }

        private static int[] Numeros(EtatSalon etat)
        {
            var n = new int[etat.Membres.Count];
            for (int i = 0; i < n.Length; i++) n[i] = etat.Membres[i].Numero;
            return n;
        }
    }
}
