using UnityEngine;
using UnityEngine.UI;

namespace Froggy.Interface
{
    /// <summary>Références du préfabriqué pour relier les boutons à leurs actions.</summary>
    public sealed class VueMenuEtang : MonoBehaviour
    {
        [SerializeField] private RectTransform centre;
        [SerializeField] private Button rejoindre;
        [SerializeField] private Button parametres;
        [SerializeField] private Button solo;
        [SerializeField] private Font police;

        public RectTransform Centre => centre;
        public Button Rejoindre => rejoindre;
        public Button Parametres => parametres;
        public Button Solo => solo;
        public Font Police => police;
    }
}
