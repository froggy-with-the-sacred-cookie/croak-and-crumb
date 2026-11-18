using Froggy.Interface;
using UnityEditor;

namespace Froggy.Outils
{
    /// <summary>
    /// Menu Froggy ▸ HUD de partie : coché, le HUD (P6-02) s'affiche en jeu ; décoché, ce sont les affichages provisoires
    /// des postes, pour comparer (garde-fou 7). Le choix vaut pour cet éditeur, d'une session à l'autre, et marche aussi
    /// en pleine partie. Un build montre toujours le HUD.
    /// </summary>
    public static class MenuHudPartie
    {
        private const string Chemin = "Froggy/HUD de partie";

        [MenuItem(Chemin, priority = 40)]
        private static void Basculer() => HudPartie.Affiche = !HudPartie.Affiche;

        [MenuItem(Chemin, true)]
        private static bool Cocher()
        {
            Menu.SetChecked(Chemin, HudPartie.Affiche);
            return true;
        }
    }
}
