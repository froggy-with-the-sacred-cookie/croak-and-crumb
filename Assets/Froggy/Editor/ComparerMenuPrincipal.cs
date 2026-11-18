using UnityEditor;

namespace Froggy.Outils
{
    internal static class ComparerMenuPrincipal
    {
        private const string CheminMenu = "Froggy/Interface/Comparer avec l’ancien menu";
        private const string CleSession = "Froggy.Menu.VersionCroquis";

        [MenuItem(CheminMenu)]
        private static void Basculer() => SessionState.SetBool(CleSession, !SessionState.GetBool(CleSession, false));

        [MenuItem(CheminMenu, true)]
        private static bool Valider()
        {
            bool croquisPresent = !string.IsNullOrEmpty(
                AssetDatabase.AssetPathToGUID("Assets/Froggy/Interface/Resources/MenuPeint/Eau_Croquis.png"));
            Menu.SetChecked(CheminMenu, croquisPresent && SessionState.GetBool(CleSession, false));
            return croquisPresent && !EditorApplication.isPlayingOrWillChangePlaymode;
        }
    }
}
