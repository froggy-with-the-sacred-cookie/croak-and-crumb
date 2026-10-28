using UnityEngine;

namespace Froggy.Template
{
    /// <summary>
    /// Réglages du joueur de la scène Template. Les valeurs de mouvement vivent ici, jamais en dur dans le code.
    /// Toutes PROVISOIRES : ce n'est pas le contrôleur de Froggy, seulement une base de travail.
    /// </summary>
    [CreateAssetMenu(fileName = "TemplateMovementSettings", menuName = "Froggy/Template/Réglages de mouvement")]
    public class TemplateMovementSettings : ScriptableObject
    {
        [Header("Course")]
        [Tooltip("Vitesse au sol, en m/s. PROVISOIRE.")]
        public float moveSpeed = 6f;

        [Tooltip("Multiplicateur quand Sprint est tenu. PROVISOIRE.")]
        public float sprintMultiplier = 1.6f;

        [Tooltip("Accélération au sol, en m/s². Grand = réponse nette. PROVISOIRE.")]
        public float groundAcceleration = 60f;

        [Tooltip("Accélération en l'air, en m/s² (contrôle aérien). PROVISOIRE.")]
        public float airAcceleration = 25f;

        [Header("Saut")]
        [Tooltip("Hauteur du saut, en m. La vitesse initiale en découle (v = √(2gh)). PROVISOIRE.")]
        public float jumpHeight = 1.5f;

        [Tooltip("Relâcher le saut en montée multiplie la vitesse verticale par ce facteur (saut variable). PROVISOIRE.")]
        [Range(0f, 1f)] public float jumpCutMultiplier = 0.5f;

        [Tooltip("Coyote time : le saut part encore ce temps après avoir quitté le sol, en s. PROVISOIRE.")]
        public float coyoteTime = 0.12f;

        [Tooltip("Tampon : un appui trop tôt part à l'atterrissage s'il arrive dans ce délai, en s. PROVISOIRE.")]
        public float jumpBufferTime = 0.1f;

        [Header("Sol")]
        [Tooltip("Distance sous le collider à laquelle on se considère au sol, en m.")]
        public float groundCheckDistance = 0.08f;

        [Tooltip("Couches considérées comme du sol.")]
        public LayerMask groundLayers = ~0;
    }
}
