using UnityEngine.UI;

namespace Froggy.Interface
{
    /// <summary>
    /// Paramètres au menu principal (P6-03, P6-05) : le panneau des réglages du joueur (PanneauReglages), le même que
    /// dans la pause. Volumes, confort, champ de vision et commandes ; chaque réglage s'applique tout de suite et
    /// s'enregistre en quittant l'écran.
    /// </summary>
    public sealed class EcranParametres : EcranMenu
    {
        private PanneauReglages panneau;
        private Ecran retourVers = Ecran.Accueil;

        public override Selectable PremierFocus => panneau != null ? panneau.PremierFocus : null;

        /// <summary>L'écran d'où l'on vient, pour y revenir.</summary>
        public void Depuis(Ecran ecran) => retourVers = ecran;

        protected override void Construire() =>
            panneau = PanneauReglages.Creer(transform, Fabrique, () => Menu.Afficher(retourVers));

        public override bool Retour() => panneau.Retour();
    }
}
