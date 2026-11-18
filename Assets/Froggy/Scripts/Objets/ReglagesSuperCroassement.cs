using UnityEngine;

namespace Froggy.Objets
{
    /// <summary>Réglages du super croassement (P4-02). Toutes les valeurs sont PROVISOIRES : Notion ne les écrit pas.</summary>
    [CreateAssetMenu(fileName = "ReglagesSuperCroassement", menuName = "Froggy/Objets/Réglages du super croassement")]
    public class ReglagesSuperCroassement : ScriptableObject
    {
        [Tooltip("La fiche de l'objet dans les boîtes mystère : l'effet part quand cet objet est utilisé.")]
        public FicheObjet fiche;

        [Tooltip("Temps d'élan avant l'onde, en s : la gorge gonfle, les autres voient venir le coup et peuvent sauter.")]
        [Min(0f)] public float elan = 0.6f;

        [Tooltip("Distance à plat jusqu'où l'onde repousse, en m.")]
        [Min(0f)] public float rayon = 4f;

        [Tooltip("Vitesse donnée aux joueurs repoussés, vers l'extérieur, en m/s.")]
        [Min(0f)] public float poussee = 9f;

        [Tooltip("Vitesse vers le haut donnée aux joueurs repoussés, en m/s : décollés du sol, ils gardent leur élan.")]
        [Min(0f)] public float montee = 3.5f;
    }
}
