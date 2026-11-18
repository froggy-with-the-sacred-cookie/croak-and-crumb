using UnityEngine;

namespace Froggy.Objets
{
    /// <summary>
    /// Réglages du gel éclair (P4-09). La durée de 5 s et le projectile droit devant viennent de Notion (page V1) ; les
    /// autres valeurs sont PROVISOIRES.
    /// </summary>
    [CreateAssetMenu(fileName = "ReglagesGelEclair", menuName = "Froggy/Objets/Réglages du gel éclair")]
    public class ReglagesGelEclair : ScriptableObject
    {
        [Tooltip("La fiche de l'objet : l'effet part quand une Froggy utilise cet objet.")]
        public FicheObjet fiche;

        [Tooltip("Durée du gel, en s (Notion : 5 s).")]
        [Min(0f)] public float duree = 5f;

        [Tooltip("Vitesse du projectile, en m/s : assez lent pour qu'on le voie venir.")]
        [Min(0.1f)] public float vitesse = 16f;

        [Tooltip("Distance au-delà de laquelle le projectile se perd, en m.")]
        [Min(0f)] public float portee = 15f;

        [Tooltip("Rayon du projectile, en m : il touche une Froggy dont le centre passe à moins de ce rayon + 0,5 m.")]
        [Min(0f)] public float rayon = 0.3f;

        [Tooltip("Hauteur du tir au-dessus des pieds, en m.")]
        [Min(0f)] public float hauteur = 0.6f;
    }
}
