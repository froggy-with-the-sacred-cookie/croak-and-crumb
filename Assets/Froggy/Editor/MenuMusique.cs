using Froggy.Audio;
using UnityEditor;

namespace Froggy.Outils
{
    /// <summary>
    /// Menu Froggy ▸ Musique : coché, la musique (P6-08) joue dans l'éditeur ; décoché, elle se tait (pour travailler
    /// sur un poste au calme). Le choix vaut pour cet éditeur, d'une session à l'autre, et marche aussi en pleine partie.
    /// Un build joue toujours la musique ; le joueur la règle dans Paramètres ▸ Musique.
    /// </summary>
    public static class MenuMusique
    {
        private const string Chemin = "Froggy/Musique";

        [MenuItem(Chemin, priority = 42)]
        private static void Basculer() => MusiqueJeu.Active = !MusiqueJeu.Active;

        [MenuItem(Chemin, true)]
        private static bool Cocher()
        {
            Menu.SetChecked(Chemin, MusiqueJeu.Active);
            return true;
        }
    }
}
