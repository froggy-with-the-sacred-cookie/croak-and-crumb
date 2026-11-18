using Froggy.Reseau;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Froggy.Outils
{
    /// <summary>
    /// Menu Froggy ▸ Réseau (P2-07) : ouvrir la scène de test du poste Réseau, ouvrir la fenêtre Multiplayer Play Mode
    /// (où l'on coche Player 2-4 : Unity ne permet pas de le faire par script), choisir la connexion simulée.
    /// Mode d'emploi : Docs/Reseau/GUIDE.md, « Tester à 2, 3 ou 4 ».
    /// </summary>
    public static class MenuTestReseau
    {
        public const string CheminReglages = "Assets/Froggy/Reseau/ReglagesTestReseau.asset";
        private const string Racine = "Froggy/Réseau/";

        [MenuItem(Racine + "1. Ouvrir la scène de test", priority = 0)]
        private static void OuvrirScene()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("Arrêter le Play avant d'ouvrir la scène de test.");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(TestReseau.SceneDeTest, OpenSceneMode.Single);
        }

        [MenuItem(Racine + "2. Fenêtre Multiplayer Play Mode (cocher Player 2, 3, 4)", priority = 1)]
        private static void OuvrirMultiplayerPlayMode()
        {
            if (!EditorApplication.ExecuteMenuItem("Window/Multiplayer/Multiplayer Play Mode"))
                Debug.LogWarning("Menu introuvable : ouvrir Window ▸ Multiplayer ▸ Multiplayer Play Mode à la main.");
        }

        [MenuItem(Racine + "3. Connexion/Parfait", priority = 20)]
        private static void Parfait() => Choisir(0);

        [MenuItem(Racine + "3. Connexion/Wifi moyen", priority = 21)]
        private static void WifiMoyen() => Choisir(1);

        [MenuItem(Racine + "3. Connexion/Mauvaise connexion", priority = 22)]
        private static void Mauvaise() => Choisir(2);

        [MenuItem(Racine + "3. Connexion/Parfait", true)]
        private static bool ParfaitCoche() => Cocher(Racine + "3. Connexion/Parfait", 0);

        [MenuItem(Racine + "3. Connexion/Wifi moyen", true)]
        private static bool WifiMoyenCoche() => Cocher(Racine + "3. Connexion/Wifi moyen", 1);

        [MenuItem(Racine + "3. Connexion/Mauvaise connexion", true)]
        private static bool MauvaiseCoche() => Cocher(Racine + "3. Connexion/Mauvaise connexion", 2);

        public static ReglagesTestReseau Reglages => AssetDatabase.LoadAssetAtPath<ReglagesTestReseau>(CheminReglages);

        /// <summary>Écrit le préréglage actif dans l'asset partagé par toutes les fenêtres.</summary>
        public static void Choisir(int index)
        {
            ReglagesTestReseau reglages = Reglages;
            if (reglages == null)
            {
                Debug.LogError($"Réglages de test introuvables : {CheminReglages}");
                return;
            }

            var so = new SerializedObject(reglages);
            so.FindProperty("prereglageActif").intValue = index;
            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssetIfDirty(reglages);
            Debug.Log($"Connexion simulée pour le prochain Play : {reglages.Lire(index).Resume}"
                      + (EditorApplication.isPlaying ? " (en jeu : F8 dans chaque fenêtre)" : ""));
        }

        private static bool Cocher(string menu, int index)
        {
            ReglagesTestReseau reglages = Reglages;
            Menu.SetChecked(menu, reglages != null && reglages.prereglageActif == index);
            return reglages != null;
        }
    }
}
