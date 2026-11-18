using UnityEngine;
using UnityEngine.UI;

namespace Froggy.Interface
{
    /// <summary>Les écrans du menu (P6-03), dans l'ordre où on les traverse.</summary>
    public enum Ecran
    {
        Accueil,
        Parametres,
        Salons,
        SalleAttente
    }

    /// <summary>
    /// Un écran du menu posé sur l'étang de Julien : MenuPrincipal en montre un seul à la fois et donne le focus à
    /// <see cref="PremierFocus"/>, pour que la manette sache toujours où elle est.
    /// </summary>
    public abstract class EcranMenu : MonoBehaviour
    {
        protected MenuPrincipal Menu { get; private set; }
        protected FabriqueMenu Fabrique { get; private set; }

        /// <summary>Ce qui reçoit le focus à l'ouverture de l'écran.</summary>
        public abstract Selectable PremierFocus { get; }

        /// <summary>Échap ou bouton B : vrai si l'écran a géré le retour.</summary>
        public virtual bool Retour() => false;

        /// <summary>Crée l'écran, étiré sur tout le parent, caché.</summary>
        public static T Creer<T>(string nom, Transform parent, MenuPrincipal menu, FabriqueMenu fabrique)
            where T : EcranMenu
        {
            var objet = new GameObject(nom, typeof(RectTransform));
            objet.transform.SetParent(parent, false);
            FabriqueMenu.Etirer((RectTransform)objet.transform);
            objet.SetActive(false);
            var ecran = objet.AddComponent<T>();
            ecran.Menu = menu;
            ecran.Fabrique = fabrique;
            ecran.Construire();
            return ecran;
        }

        protected abstract void Construire();
    }
}
