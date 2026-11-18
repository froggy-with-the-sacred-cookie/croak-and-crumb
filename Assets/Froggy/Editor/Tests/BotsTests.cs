using Froggy.Bots;
using NUnit.Framework;
using UnityEngine;

namespace Froggy.Outils.Tests
{
    /// <summary>
    /// Décisions du bot de niveau 1 (P3-05), en C# pur : les choix de Lucas du 18 novembre 2026 (prudent quand il porte,
    /// vise le plus chargé, tous sur le cookie sacré), le côté brouillon (il rate parfois), et l'objet utilisé quel qu'il soit.
    /// </summary>
    public class BotsTests
    {
        private ReglagesBot r;

        [SetUp]
        public void Preparer() => r = ScriptableObject.CreateInstance<ReglagesBot>();

        [TearDown]
        public void Ranger() => Object.DestroyImmediate(r);

        private static SituationBot Situation(int cookies = 0)
        {
            var s = new SituationBot();
            s.Vider();
            s.position = Vector3.zero;
            s.cookies = cookies;
            return s;
        }

        private static RivalVu Rival(int joueur, float x, float z, int cookies = 0, bool sacre = false, bool intouchable = false) =>
            new RivalVu { joueur = joueur, position = new Vector3(x, 0f, z), cookies = cookies, porteSacre = sacre, intouchable = intouchable };

        private IntentionBot Decider(SituationBot s, int graine = 1) => ReglesBot.Decider(s, r, new System.Random(graine));

        [Test]
        public void HorsJeuIlAttend()
        {
            var s = Situation();
            s.enJeu = false;
            s.cookiesVisibles.Add(new Vector3(3f, 0f, 0f));
            Assert.That(Decider(s).sorte, Is.EqualTo(IntentionBot.Sorte.Attendre));
        }

        [Test]
        public void VaAuCookieLePlusProche()
        {
            var s = Situation();
            s.cookiesVisibles.Add(new Vector3(10f, 0f, 0f));
            s.cookiesVisibles.Add(new Vector3(0f, 0f, 3f));
            var i = Decider(s);
            Assert.That(i.sorte, Is.EqualTo(IntentionBot.Sorte.Aller));
            Assert.That(i.but, Is.EqualTo(new Vector3(0f, 0f, 3f)));
        }

        [Test]
        public void ChargeEtMenaceIlSEloigne()
        {
            var s = Situation(cookies: 2);
            s.rivaux.Add(Rival(2, 2.5f, 0f));
            s.cookiesVisibles.Add(new Vector3(3f, 0f, 0f));
            var i = Decider(s);
            Assert.That(i.sorte, Is.EqualTo(IntentionBot.Sorte.Fuir));
            Assert.That(ReglesBot.PointDeFuite(s.position, i.but).x, Is.LessThan(0f), "à l'opposé du rival");
        }

        [Test]
        public void PlusIlPorteMoinsIlLaisseApprocher()
        {
            var peu = Situation(cookies: 1);
            peu.rivaux.Add(Rival(2, 4f, 0f));
            Assert.That(Decider(peu).sorte, Is.Not.EqualTo(IntentionBot.Sorte.Fuir), "1 cookie : un rival à 4 m ne fait pas fuir");

            var beaucoup = Situation(cookies: 5);
            beaucoup.rivaux.Add(Rival(2, 4f, 0f));
            Assert.That(Decider(beaucoup).sorte, Is.EqualTo(IntentionBot.Sorte.Fuir), "5 cookies : il s'éloigne dès 4 m");
        }

        [Test]
        public void NeFuitPasUnRivalAussiCharge()
        {
            var s = Situation(cookies: 2);
            s.rivaux.Add(Rival(2, 2.5f, 0f, cookies: 3));
            var i = Decider(s);
            Assert.That(i.sorte, Is.EqualTo(IntentionBot.Sorte.Aller), "plus chargé que lui : il attaque");
            Assert.That(i.cible, Is.EqualTo(2));
        }

        [Test]
        public void ChargeSansRienARamasserIlGardeSesCookies()
        {
            var s = Situation(cookies: 2);
            s.aUnObjet = true;
            s.rivaux.Add(Rival(2, 10f, 0f));
            Assert.That(Decider(s).sorte, Is.EqualTo(IntentionBot.Sorte.Attendre));
        }

        [Test]
        public void CoinceIlRiposte()
        {
            var s = Situation(cookies: 2);
            s.rivaux.Add(Rival(2, 1f, 0f));
            var i = Decider(s);
            Assert.That(i.frapper, Is.True);
            Assert.That(i.cible, Is.EqualTo(2));
        }

        [Test]
        public void ViseLePlusCharge()
        {
            var s = Situation();
            s.rivaux.Add(Rival(2, 1f, 0f, cookies: 0));
            s.rivaux.Add(Rival(3, 0f, 1.2f, cookies: 3));
            s.rivaux.Add(Rival(4, -1f, 0f, cookies: 1));
            var i = Decider(s);
            Assert.That(i.cible, Is.EqualTo(3));
            Assert.That(i.frapper, Is.True);
        }

        [Test]
        public void ChasseLeLeaderMemeDeLoin()
        {
            var s = Situation(cookies: 1);
            s.cookiesVisibles.Add(new Vector3(2f, 0f, 0f));
            s.rivaux.Add(Rival(2, 0f, 15f, cookies: 4));
            var i = Decider(s);
            Assert.That(i.cible, Is.EqualTo(2), "3 cookies d'avance : il le chasse plutôt que le cookie à 2 m");
            Assert.That(i.dash, Is.False, "à 15 m, il court : le dash (4 m) part une fois le leader à portée");
        }

        [Test]
        public void IlDasheQuandLaCibleEstAPortee()
        {
            // #134 : le dash remplace le sprint ; il part vers un leader entre dashMin et dashMax, s'il est prêt
            var s = Situation(cookies: 1);
            s.dashPret = true;
            s.torgnolePrete = false;
            s.rivaux.Add(Rival(2, 0f, 4f, cookies: 4));
            Assert.That(Decider(s).dash, Is.True, "leader à 4 m : un dash pour le rattraper");

            s.rivaux.Clear();
            s.rivaux.Add(Rival(2, 0f, 1f, cookies: 4));
            Assert.That(Decider(s).dash, Is.False, "à 1 m, trop près : il marche");

            s.rivaux.Clear();
            s.rivaux.Add(Rival(2, 0f, 4f, cookies: 4));
            s.dashPret = false;
            Assert.That(Decider(s).dash, Is.False, "dash en recharge");
        }

        [Test]
        public void IlDashePourFuirUneMenaceProche()
        {
            var s = Situation(cookies: 3);
            s.dashPret = true;
            s.torgnolePrete = false;   // pas de riposte : il fuit
            s.rivaux.Add(Rival(2, 0f, 2.5f));
            var i = Decider(s);
            Assert.That(i.sorte, Is.EqualTo(IntentionBot.Sorte.Fuir));
            Assert.That(i.dash, Is.True, "menace à 2,5 m : un dash pour s'échapper");
        }

        [Test]
        public void JamaisDeDashPourUnSimpleCookie()
        {
            var s = Situation();
            s.dashPret = true;
            s.cookiesVisibles.Add(new Vector3(0f, 0f, 3f));
            var i = Decider(s);
            Assert.That(i.raison, Does.Contain("cookie"));
            Assert.That(i.dash, Is.False, "le dash se garde pour rattraper, fuir ou le cookie sacré");
        }

        [Test]
        public void PetiteAvanceNeFaitPasUnLeader()
        {
            var s = Situation(cookies: 1);
            s.cookiesVisibles.Add(new Vector3(2f, 0f, 0f));
            s.rivaux.Add(Rival(2, 0f, 15f, cookies: 3));
            var i = Decider(s);
            Assert.That(i.but, Is.EqualTo(new Vector3(2f, 0f, 0f)), "2 d'avance seulement : il ramasse");
        }

        [Test]
        public void NeFrappePasUnIntouchable()
        {
            var s = Situation();
            s.rivaux.Add(Rival(2, 1f, 0f, cookies: 3, intouchable: true));
            Assert.That(Decider(s).frapper, Is.False);
        }

        [Test]
        public void ParfoisIlFrappeDeTropLoin()
        {
            var s = Situation();
            s.rivaux.Add(Rival(2, r.porteeFrappe + 0.5f, 0f, cookies: 1));
            r.chanceRater = 0f;
            Assert.That(Decider(s).frapper, Is.False, "jamais brouillon : il attend d'être à portée");
            r.chanceRater = 1f;
            Assert.That(Decider(s).frapper, Is.True, "toujours brouillon : il frappe trop tôt");
        }

        [Test]
        public void SansTorgnoleIlNeFrappePas()
        {
            var s = Situation();
            s.torgnolePrete = false;
            s.rivaux.Add(Rival(2, 1f, 0f, cookies: 2));
            Assert.That(Decider(s).frapper, Is.False);
        }

        [Test]
        public void TousSurLeGobelin()
        {
            var s = Situation(cookies: 3);
            s.gobelin = new Vector3(20f, 0f, 0f);
            s.cookiesVisibles.Add(new Vector3(1f, 0f, 0f));
            s.rivaux.Add(Rival(2, 2f, 0f));
            var i = Decider(s);
            Assert.That(i.sorte, Is.EqualTo(IntentionBot.Sorte.Aller));
            Assert.That(i.but, Is.EqualTo(s.gobelin.Value));
        }

        [Test]
        public void ChasseLePorteurDuCookieSacre()
        {
            var s = Situation();
            s.cookiesVisibles.Add(new Vector3(1f, 0f, 0f));
            s.rivaux.Add(Rival(3, 15f, 0f, sacre: true));
            var i = Decider(s);
            Assert.That(i.cible, Is.EqualTo(3));
            Assert.That(i.dash, Is.False, "à 15 m, il court ; le dash part à portée");
        }

        [Test]
        public void AvecLeCookieSacreIlFuit()
        {
            var s = Situation();
            s.porteSacre = true;
            s.rivaux.Add(Rival(2, 5f, 0f));
            Assert.That(Decider(s).sorte, Is.EqualTo(IntentionBot.Sorte.Fuir));
        }

        [Test]
        public void UtiliseSonObjetQuandUnRivalEstPres()
        {
            var s = Situation();
            s.aUnObjet = s.objetPret = true;
            s.rivaux.Add(Rival(2, 4f, 0f));
            Assert.That(Decider(s).utiliserObjet, Is.True);

            var loin = Situation();
            loin.aUnObjet = loin.objetPret = true;
            loin.rivaux.Add(Rival(2, 30f, 0f));
            Assert.That(Decider(loin).utiliserObjet, Is.False);
        }

        [Test]
        public void SansCookieIlVaChercherUneBoite()
        {
            var s = Situation();
            s.boites.Add(new Vector3(5f, 0f, 0f));
            var i = Decider(s);
            Assert.That(i.but, Is.EqualTo(new Vector3(5f, 0f, 0f)));

            s.aUnObjet = true;
            Assert.That(Decider(s).sorte, Is.EqualTo(IntentionBot.Sorte.Attendre), "il a déjà un objet : pas de boîte");
        }
    }
}
