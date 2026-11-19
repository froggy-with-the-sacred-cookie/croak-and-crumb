using Froggy.Interface;
using UnityEditor;

namespace Froggy.Outils
{
    /// <summary>
    /// Menu Froggy ▸ Menus : comparer les écrans refaits d'après les maquettes de Julien (19/11/2026) avec les précédents
    /// (garde-fou 7). Le choix vaut pour cet éditeur ; il se lit à la construction des écrans (relancer le jeu). Un build
    /// montre toujours les versions refaites.
    /// </summary>
    public static class MenuMenus
    {
        private const string Tableau = "Froggy/Menus/Accueil avec le tableau du jardin";
        private const string Style = "Froggy/Menus/Style des maquettes de Julien";

        [MenuItem(Tableau, priority = 42)]
        private static void BasculerTableau() => MenuPrincipal.AccueilTableau = !MenuPrincipal.AccueilTableau;

        [MenuItem(Tableau, true)]
        private static bool CocherTableau()
        {
            Menu.SetChecked(Tableau, MenuPrincipal.AccueilTableau);
            return true;
        }

        [MenuItem(Style, priority = 43)]
        private static void BasculerStyle() => FabriqueMenu.Habille = !FabriqueMenu.Habille;

        [MenuItem(Style, true)]
        private static bool CocherStyle()
        {
            Menu.SetChecked(Style, FabriqueMenu.Habille);
            return true;
        }
    }
}
