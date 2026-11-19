using UnityEngine;
using UnityEngine.EventSystems;

namespace Froggy.Interface
{
    /// <summary>Un bouton qui part dès l'appui (pas au relâchement, que la caméra peut avaler).</summary>
    public class AppuiHud : MonoBehaviour, IPointerDownHandler
    {
        public System.Action Action;
        public void OnPointerDown(PointerEventData eventData) => Action?.Invoke();
    }
}
