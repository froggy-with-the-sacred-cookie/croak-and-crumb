using UnityEngine;

namespace Froggy.Audio
{
    /// <summary>À poser sur l'objet transmis dans EvenementPartie.Objet pour nommer son son d'utilisation.</summary>
    public sealed class SonObjet : MonoBehaviour
    {
        [SerializeField] private IdSon utilisation = IdSon.BoiteMystereOuverture;
        public IdSon Utilisation => utilisation;
    }
}
