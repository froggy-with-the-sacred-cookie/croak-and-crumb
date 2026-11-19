using UnityEngine;

namespace Froggy.Reseau
{
    /// <summary>
    /// Dans la partie complète (#130) : ses systèmes locaux (cookies, déroulement, cookie sacré, torgnole, réapparition,
    /// bots, objets, météo) attendent éteints, et ne s'allument que hors ligne. En ligne, le salon charge la même scène
    /// pour son arène (décor, repères) et PartieReseau apporte les systèmes décidés par l'hôte : les deux ne doivent
    /// jamais tourner ensemble. Éteints dans la scène, ils ne s'éveillent pas avant ce choix.
    /// </summary>
    public class SystemesHorsLigne : MonoBehaviour
    {
        [Tooltip("Le groupe des systèmes locaux, éteint dans la scène.")]
        [SerializeField] private GameObject systemes;

        /// <summary>Vrai quand une partie en ligne se joue sur cette machine (un salon connecté).</summary>
        public static bool EnLigne => Salon.Local != null && Salon.Local.Connecte;

        public GameObject Systemes => systemes;

        private void Awake()
        {
            if (systemes != null) systemes.SetActive(!EnLigne);
        }
    }
}
