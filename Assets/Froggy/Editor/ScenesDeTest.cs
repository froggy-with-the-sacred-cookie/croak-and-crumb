using System.IO;
using System.Text.RegularExpressions;
using Froggy.Socle;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Froggy.Outils
{
    /// <summary>
    /// Les scènes de test du socle commun (T-01) : une par poste, dans Assets/Froggy/Postes/&lt;poste&gt;/, presque vide,
    /// avec un DecorDeTest qui charge le jardin par-dessus. Chacun y pose ses essais sans toucher à la scène d'un autre.
    /// Deux personnes sur le même poste en même temps : chacune se fait une scène perso (menu), rangée dans
    /// Postes/_Perso/, que Git ignore. Ce qui doit être partagé vit dans un préfabriqué, pas dans la scène.
    /// À l'ouverture d'une scène de test, son décor s'ouvre à côté dans la Hiérarchie ; la scène de test reste la scène
    /// active, pour que les objets créés y aillent (et pas dans le jardin).
    /// </summary>
    [InitializeOnLoad]
    public static class ScenesDeTest
    {
        public const string Racine = "Assets/Froggy/Postes";
        public const string Perso = Racine + "/_Perso";

        /// <summary>Les huit postes du tableau : dossier, puis nom de la scène.</summary>
        public static readonly (string dossier, string scene)[] Postes =
        {
            ("1_MouvementCamera", "Test_MouvementCamera"),
            ("2_Reseau", "Test_Reseau"),
            ("3_ReglesBots", "Test_ReglesBots"),
            ("4_ObjetsCapacites", "Test_ObjetsCapacites"),
            ("5_MeteoEvenements", "Test_MeteoEvenements"),
            ("6_InterfaceSon", "Test_InterfaceSon"),
            ("7_PersonnagesAnimation", "Test_PersonnagesAnimation"),
            ("8_ArenesRendu", "Test_ArenesRendu"),
        };

        static ScenesDeTest()
        {
            EditorSceneManager.sceneOpened += (scene, mode) =>
            {
                if (mode != OpenSceneMode.Single) return;
                OuvrirLeDecor(scene);
            };
        }

        public static string Chemin(int poste) => $"{Racine}/{Postes[poste].dossier}/{Postes[poste].scene}.unity";

        [MenuItem("Froggy/Scènes de test/Créer celles des postes (si elles manquent)")]
        private static void MenuCreer() => Debug.Log($"Scènes de test : {CreerScenesDesPostes()} créée(s) dans {Racine}.");

        /// <summary>Crée les scènes qui manquent, sans toucher aux scènes ouvertes ni à celles qui existent. Rend leur nombre.</summary>
        public static int CreerScenesDesPostes()
        {
            int creees = 0;
            for (int i = 0; i < Postes.Length; i++)
                if (Creer(Chemin(i))) creees++;
            AssetDatabase.Refresh();
            return creees;
        }

        [MenuItem("Froggy/Scènes de test/Nouvelle scène perso (ignorée par Git)")]
        private static void MenuPerso()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            string chemin = CreerScenePerso(System.Environment.UserName);
            EditorSceneManager.OpenScene(chemin, OpenSceneMode.Single);
        }

        /// <summary>Crée une scène perso au nom donné (numérotée si elle existe déjà) et rend son chemin.</summary>
        public static string CreerScenePerso(string nom)
        {
            nom = Regex.Replace(nom ?? "", "[^A-Za-z0-9_-]", "");
            if (nom == "") nom = "Perso";
            string chemin = $"{Perso}/Test_{nom}.unity";
            for (int n = 2; File.Exists(chemin); n++) chemin = $"{Perso}/Test_{nom}_{n}.unity";
            Creer(chemin);
            AssetDatabase.Refresh();
            return chemin;
        }

        private static bool Creer(string chemin)
        {
            if (File.Exists(chemin)) return false;
            Directory.CreateDirectory(Path.GetDirectoryName(chemin));
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            var decor = new GameObject("Décor de test");
            SceneManager.MoveGameObjectToScene(decor, scene);
            decor.AddComponent<DecorDeTest>();
            bool ok = EditorSceneManager.SaveScene(scene, chemin);
            EditorSceneManager.CloseScene(scene, true);
            return ok;
        }

        /// <summary>Le décor demandé par une scène, ou null si ce n'est pas une scène de test.</summary>
        public static string DecorDe(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded) return null;
            foreach (GameObject racine in scene.GetRootGameObjects())
            {
                var decor = racine.GetComponentInChildren<DecorDeTest>(true);
                if (decor != null) return decor.Decor;
            }
            return null;
        }

        /// <summary>Ouvre à côté le décor d'une scène de test, si elle en demande un et qu'il n'est pas déjà là.</summary>
        public static void OuvrirLeDecor(Scene scene)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            string decor = DecorDe(scene);
            if (string.IsNullOrEmpty(decor) || !File.Exists(decor)) return;
            if (SceneManager.GetSceneByPath(decor).isLoaded) return;
            EditorSceneManager.OpenScene(decor, OpenSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
        }
    }
}
