using UnityEngine;

namespace Froggy.Deplacement
{
    /// <summary>Réglages de la nage de Froggy (P1-02). Toutes les valeurs sont PROVISOIRES, à juger manette en main.</summary>
    [CreateAssetMenu(fileName = "ReglagesNage", menuName = "Froggy/Déplacement/Réglages de la nage")]
    public class ReglagesNage : ScriptableObject
    {
        [Tooltip("Vitesse de nage, en part de la vitesse de course au sol (pas de sprint dans l'eau).")]
        [Range(0.1f, 1.5f)] public float facteurVitesse = 0.6f;

        [Tooltip("Accélération horizontale dans l'eau, en m/s² : assez forte pour un demi-tour net (skill ressenti-mouvement).")]
        [Min(0f)] public float acceleration = 20f;

        [Tooltip("Profondeur du centre de Froggy sous la surface quand elle nage en surface, en m : la tête dépasse.")]
        [Min(0f)] public float flottaison = 0.3f;

        [Tooltip("Profondeur du centre de Froggy en plongée (touche de plongée tenue), en m.")]
        [Min(0f)] public float plongee = 1.3f;

        [Tooltip("Vitesse verticale maximale pour remonter ou plonger, en m/s.")]
        [Min(0.1f)] public float vitesseVerticale = 3f;

        [Tooltip("Accélération verticale dans l'eau, en m/s² : freine aussi une chute dans l'eau.")]
        [Min(0.1f)] public float accelerationVerticale = 25f;

        [Tooltip("Hauteur du saut de sortie, au-dessus de la position de nage, en m : de quoi monter sur un bord ou un nénuphar.")]
        [Min(0f)] public float hauteurSortie = 1.4f;

        [Tooltip("Marge sous la position de nage d'où l'on peut encore sauter hors de l'eau, en m.")]
        [Min(0f)] public float margeSortie = 0.25f;
    }
}
