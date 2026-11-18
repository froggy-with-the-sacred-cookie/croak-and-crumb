using Froggy.Cameras;
using NUnit.Framework;

namespace Froggy.Outils.Tests
{
    /// <summary>
    /// Menu de debug de la caméra (#99) : son cadre ne dépasse jamais le bas de l'écran. Un menu plus haut que la fenêtre
    /// s'arrête à la marge et défile ; un menu qui tient garde sa hauteur.
    /// </summary>
    public class MenuCameraTests
    {
        [Test]
        public void UnMenuQuiTientGardeSaHauteur()
        {
            Assert.That(MenuCamera.HauteurVisible(585f, 900f, 16f), Is.EqualTo(585f));
        }

        [TestCase(1001f, 900f)]   // le menu complet d'une vitrine dans une fenêtre de 900 px
        [TestCase(1001f, 600f)]
        [TestCase(1001f, 480f)]
        public void UnMenuTropHautSArreteAuBasDeLEcran(float menu, float ecran)
        {
            float visible = MenuCamera.HauteurVisible(menu, ecran, 16f);
            Assert.That(visible, Is.LessThan(menu), "il doit défiler");
            Assert.That(16f + visible, Is.LessThanOrEqualTo(ecran - 16f + 0.001f), "le cadre sort de l'écran");
        }

        [Test]
        public void MemeDansUneFenetreMinusculeIlResteUnCadre()
        {
            Assert.That(MenuCamera.HauteurVisible(1001f, 60f, 16f), Is.EqualTo(120f));
        }
    }
}
