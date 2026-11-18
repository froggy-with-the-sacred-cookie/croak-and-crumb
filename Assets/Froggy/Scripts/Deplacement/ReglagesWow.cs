using UnityEngine;

namespace Froggy.Deplacement
{
    /// <summary>
    /// Réglages propres au déplacement à la World of Warcraft (DeplacementWow). La course et le saut restent
    /// ceux de TemplateMovementSettings. Toutes PROVISOIRES.
    /// </summary>
    [CreateAssetMenu(fileName = "ReglagesWow", menuName = "Froggy/Vitrine/Réglages du déplacement à la WoW")]
    public class ReglagesWow : ScriptableObject
    {
        [Tooltip("Vitesse en reculant, en fraction de la course. PROVISOIRE (hypothèse : WoW recule à 4,5 m/s pour 7 en course).")]
        [Range(0f, 1f)] public float facteurRecul = 0.65f;

        [Tooltip("En diagonale, Froggy se tourne de cet angle vers sa course, en degrés. PROVISOIRE.")]
        [Range(0f, 90f)] public float angleDiagonale = 45f;

        [Tooltip("Comme WoW : en l'air, on garde l'élan pris au sol (plus de contrôle aérien). COUPÉ par défaut : Notion " +
                 "demande des sauts précis de nénuphar en nénuphar ; à trancher par l'équipe.")]
        public bool elanFigeEnLair;
    }
}
