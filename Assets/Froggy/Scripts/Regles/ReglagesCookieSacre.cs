using UnityEngine;

namespace Froggy.Regles
{
    /// <summary>Réglages du cookie sacré et de son gobelin (P5-08). Tous PROVISOIRES : à régler en jouant à plusieurs.</summary>
    [CreateAssetMenu(fileName = "ReglagesCookieSacre", menuName = "Froggy/Règles/Réglages du cookie sacré")]
    public class ReglagesCookieSacre : ScriptableObject
    {
        [Tooltip("Temps de jeu restant auquel le gobelin apparaît, en s. Notion : la dernière minute (60).")]
        [Min(0f)] public float moment = 60f;

        [Tooltip("Distance à laquelle un joueur attrape le gobelin, en m. PROVISOIRE.")]
        [Min(0f)] public float rayonCapture = 1.0f;

        [Tooltip("Distance à laquelle on ramasse le cookie sacré au sol, en m (comme un cookie : 1,2). PROVISOIRE.")]
        [Min(0f)] public float rayonRamassage = 1.2f;

        [Tooltip("Temps pendant lequel celui qui l'a lâché ne peut pas le reprendre, en s. PROVISOIRE.")]
        [Min(0f)] public float delaiReprise = 1.5f;

        [Tooltip("Où il tombe quand on le lâche, en m dans la direction du coup. PROVISOIRE.")]
        [Min(0f)] public float distanceLacher = 2f;

        [Header("Gobelin")]
        [Tooltip("Vitesse de fuite, en m/s. Froggy court à 6 : le gobelin se rattrape en ligne droite. PROVISOIRE.")]
        [Min(0f)] public float vitesse = 5f;

        [Tooltip("Il fuit les joueurs plus proches que ça, en m ; plus loin, il trottine en rond. PROVISOIRE.")]
        [Min(0f)] public float distancePeur = 9f;

        [Tooltip("Il reste à moins de cette distance de son point de départ, en m (l'arène). PROVISOIRE.")]
        [Min(1f)] public float rayonArene = 14f;
    }
}
