using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;

namespace Froggy.Outils.Tests
{
    /// <summary>
    /// Les Froggy du menu de debug : chaque vitrine du jardin a sur son joueur un FroggyApparence avec tous les modèles de
    /// Assets/Froggy/Personnages, sans les raccourcis (E est le pas de côté de la caméra WoW), et part de la forme
    /// provisoire. On lit le fichier de la scène : le test n'ouvre rien dans l'éditeur.
    /// </summary>
    public class MenuFroggyTests
    {
        [TestCase("Assets/Froggy/Scenes/Vitrine_JardinJaponais.unity")]
        [TestCase("Assets/Froggy/Scenes/Vitrine_JardinJaponais_Grande.unity")]
        [TestCase("Assets/Froggy/Scenes/Vitrine_JardinJaponais_Chine.unity")]
        public void LaVitrineProposeTousLesFroggy(string scene)
        {
            string guid = AssetDatabase.AssetPathToGUID("Assets/Froggy/Scripts/Personnages/FroggyApparence.cs");
            var lignes = File.ReadAllLines(scene);
            int debut = System.Array.FindIndex(lignes, l => l.Contains("guid: " + guid));
            Assert.That(debut, Is.GreaterThanOrEqualTo(0), "pas de FroggyApparence dans " + scene);
            var bloc = lignes.Skip(debut).TakeWhile(l => !l.StartsWith("--- !u!")).ToArray();

            var attendus = AssetDatabase.GetSubFolders("Assets/Froggy/Personnages")
                .Where(d => Directory.GetFiles(d, "*_Repos.fbx").Length > 0).Select(Path.GetFileName).ToArray();
            var poses = bloc.Where(l => l.TrimStart().StartsWith("- nom: ")).Select(l => l.Trim().Substring(7)).ToArray();
            Assert.That(poses, Is.EquivalentTo(attendus));
            Assert.That(bloc, Has.Some.Contains("raccourcis: 0"));
            Assert.That(bloc, Has.Some.Contains("modeleDeDepart: -1"));
        }
    }
}
