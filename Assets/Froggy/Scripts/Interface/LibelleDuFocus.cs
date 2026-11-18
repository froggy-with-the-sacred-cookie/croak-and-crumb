using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Froggy.Interface
{
    /// <summary>
    /// Double l'indice du focus d'un élément (curseur) : son libellé passe à l'anis tant qu'il a le focus, pour qu'on
    /// voie toujours où est la manette, même quand la couleur de l'élément change peu.
    /// </summary>
    public sealed class LibelleDuFocus : MonoBehaviour, ISelectHandler,
        IDeselectHandler
    {
        public Text Libelle;

        public void OnSelect(BaseEventData e)
        {
            if (Libelle != null) Libelle.color = FabriqueMenu.Anis;
        }

        public void OnDeselect(BaseEventData e)
        {
            if (Libelle != null) Libelle.color = FabriqueMenu.Ivoire;
        }

        private void OnDisable() => OnDeselect(null);
    }
}
