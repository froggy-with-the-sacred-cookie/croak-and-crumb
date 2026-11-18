using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Froggy.Arene;
using Froggy.Interface;
using NUnit.Framework;
using UnityEditor;

namespace Froggy.Outils.Tests
{
    /// <summary>
    /// La scène de partie complète (Partie_JardinJaponais) : chaque système de jeu y est une fois, sans les outils
    /// d'essai des scènes de test, avec les vrais réglages, des repères de chaque sorte, et « Solo » la lance. On lit le
    /// fichier de la scène : le test n'ouvre rien dans l'éditeur.
    /// </summary>
    public class PartieJardinTests
    {
        private const string Scene = "Assets/Froggy/Scenes/Partie_JardinJaponais.unity";

        private static string texte;
        private static string Texte => texte ??= File.ReadAllText(Scene);

        private static string Guid(string chemin)
        {
            string guid = AssetDatabase.AssetPathToGUID(chemin);
            Assert.That(guid, Is.Not.Empty, "introuvable : " + chemin);
            return guid;
        }

        private static int Fois(string guid) => Regex.Matches(Texte, "guid: " + guid).Count;

        [TestCase("Assets/Froggy/Scripts/Socle/DecorDeTest.cs")]
        [TestCase("Assets/Froggy/Scripts/Regles/DeroulementPartie.cs")]
        [TestCase("Assets/Froggy/Scripts/Regles/PartieCookies.cs")]
        [TestCase("Assets/Froggy/Scripts/Regles/PartieCookieSacre.cs")]
        [TestCase("Assets/Froggy/Scripts/Regles/PartieTorgnoles.cs")]
        [TestCase("Assets/Froggy/Scripts/Regles/ApparitionJoueurs.cs")]
        [TestCase("Assets/Froggy/Scripts/Bots/BotsPartie.cs")]
        [TestCase("Assets/Froggy/Scripts/Objets/PartieObjets.cs")]
        [TestCase("Assets/Froggy/Scripts/Objets/EffetSuperCroassement.cs")]
        [TestCase("Assets/Froggy/Scripts/Objets/EffetCamouflage.cs")]
        [TestCase("Assets/Froggy/Scripts/Objets/EffetGelEclair.cs")]
        [TestCase("Assets/Froggy/Scripts/Meteo/GestionnaireMeteo.cs")]
        public void ChaqueSystemeEstLaUneFois(string script)
        {
            Assert.That(Fois(Guid(script)), Is.EqualTo(1));
        }

        [Test]
        public void AucunOutilDEssai()
        {
            var essais = AssetDatabase.FindAssets("t:MonoScript Essai", new[] { "Assets/Froggy/Scripts" })
                .Select(AssetDatabase.GUIDToAssetPath).Where(p => Path.GetFileName(p).StartsWith("Essai")).ToArray();
            Assert.That(essais, Is.Not.Empty);
            foreach (string essai in essais)
                Assert.That(Fois(AssetDatabase.AssetPathToGUID(essai)), Is.Zero, essai + " est un outil de scène de test");
        }

        [Test]
        public void LesVraisReglagesPasCeuxDEssai()
        {
            Assert.That(Fois(Guid("Assets/Froggy/Postes/3_ReglesBots/ReglagesPartie.asset")), Is.EqualTo(1));
            Assert.That(Fois(Guid("Assets/Froggy/Postes/5_MeteoEvenements/ReglagesMeteo_Jardin.asset")), Is.EqualTo(1));
            Assert.That(Fois(Guid("Assets/Froggy/Postes/3_ReglesBots/ReglagesPartie_Essai.asset")), Is.Zero);
            Assert.That(Fois(Guid("Assets/Froggy/Postes/5_MeteoEvenements/ReglagesPartie_CookieSacre.asset")), Is.Zero);
            Assert.That(Fois(Guid("Assets/Froggy/Postes/5_MeteoEvenements/ReglagesMeteo_Essai.asset")), Is.Zero);
            Assert.That(Texte, Does.Contain("attendreLaPartie: 1"), "la météo démarre avec la partie");
        }

        [Test]
        public void IlYaDesReperesDeChaqueSorte()
        {
            var sortes = Regex.Matches(Texte, @"sorte: (\d+)").Cast<Match>()
                .Select(m => (RepereArene.Sorte)int.Parse(m.Groups[1].Value)).ToList();
            Assert.That(sortes.Count(s => s == RepereArene.Sorte.Joueur), Is.GreaterThanOrEqualTo(2), "2 joueurs au moins");
            Assert.That(sortes, Does.Contain(RepereArene.Sorte.Cookie));
            Assert.That(sortes, Does.Contain(RepereArene.Sorte.Boite));
            Assert.That(sortes, Does.Contain(RepereArene.Sorte.Gobelin));
        }

        [Test]
        public void SoloLanceLaPartieQuiSuitLeMenuDansLeBuild()
        {
            Assert.That(MenuPrincipal.SceneSolo, Is.EqualTo(Scene));
            var scenes = EditorBuildSettings.scenes;
            int menu = System.Array.FindIndex(scenes, s => s.path == MenuPrincipal.SceneMenu);
            Assert.That(menu, Is.GreaterThanOrEqualTo(0));
            Assert.That(scenes[menu + 1].path, Is.EqualTo(Scene));
            Assert.That(scenes[menu + 1].enabled, Is.True);
            Assert.That(scenes.Any(s => s.enabled && s.path == DecorDeTestJardin), "le jardin doit être dans le build");
        }

        private const string DecorDeTestJardin = Socle.DecorDeTest.Jardin;

        /// <summary>
        /// Garde-fou : un poste qui ajoute un système à sa scène de test l'ajoute aussi à la partie complète. Exceptions :
        /// ses outils Essai… et les porteurs (PorteurCookies, PorteurObjet), que les règles posent seules sur les Froggy.
        /// Les scènes perso (Postes/_Perso) ne comptent pas.
        /// </summary>
        [Test]
        public void ToutSystemeDUneSceneDeTestEstDansLaPartie()
        {
            var dansLaPartie = Scripts(Texte);
            var scenes = Directory.GetFiles("Assets/Froggy/Postes", "Test_*.unity", SearchOption.AllDirectories)
                .Where(s => !s.Replace('\\', '/').Contains("/_Perso/")).ToArray();
            Assert.That(scenes, Is.Not.Empty);
            foreach (string scene in scenes)
            foreach (string script in Scripts(File.ReadAllText(scene)))
            {
                string nom = Path.GetFileNameWithoutExtension(script);
                if (nom.StartsWith("Essai") || nom.StartsWith("Porteur")) continue;
                Assert.That(dansLaPartie, Does.Contain(script),
                    $"{nom} (scène {Path.GetFileName(scene)}) manque à la partie complète {Path.GetFileName(Scene)}");
            }
        }

        private static System.Collections.Generic.HashSet<string> Scripts(string scene) =>
            Regex.Matches(scene, @"m_Script: \{fileID: 11500000, guid: (\w+)").Cast<Match>()
                .Select(m => AssetDatabase.GUIDToAssetPath(m.Groups[1].Value))
                .Where(p => p.StartsWith("Assets/Froggy/")).ToHashSet();
    }
}
