using UnityEngine;

namespace Froggy.Bots
{
    /// <summary>
    /// Réglages du bot de niveau 1 (P3-05) : un joueur moyen, un peu brouillon. Toutes les valeurs sont des PROPOSITIONS,
    /// à juger en jouant contre eux (un humain doit les battre souvent, mais en se battant).
    /// </summary>
    [CreateAssetMenu(fileName = "ReglagesBot", menuName = "Froggy/Bots/Réglages d'un bot")]
    public class ReglagesBot : ScriptableObject
    {
        [Header("Réflexes")]
        [Tooltip("Temps entre deux décisions, en s : un humain réagit en 0,2 à 0,3 s.")]
        [Min(0.05f)] public float tempsReaction = 0.3f;
        [Tooltip("Variation de ce temps, en s, pour que les bots ne décident pas tous au même instant.")]
        [Min(0f)] public float variationReaction = 0.1f;
        [Tooltip("Vitesse à laquelle le bot se tourne, en degrés par seconde.")]
        [Min(30f)] public float vitesseRotation = 540f;

        [Header("Prudence d'un bot qui porte des cookies")]
        [Tooltip("Distance en dessous de laquelle un rival fait fuir un bot qui porte 1 cookie, en m.")]
        [Min(0f)] public float rayonPrudence = 3f;
        [Tooltip("Mètres de prudence en plus par cookie porté au-delà du premier.")]
        [Min(0f)] public float prudenceParCookie = 0.5f;
        [Tooltip("Plafond de la distance de prudence, en m.")]
        [Min(0f)] public float prudenceMax = 6f;

        [Header("Attaque")]
        [Tooltip("Un rival qui porte des cookies et passe à moins de cette distance attire le bot, en m.")]
        [Min(0f)] public float rayonChasse = 6f;
        [Tooltip("Distance à laquelle le bot donne sa torgnole, en m (la torgnole porte à 1,6 m).")]
        [Min(0f)] public float porteeFrappe = 1.4f;
        [Tooltip("Chance qu'il frappe trop tôt, de trop loin, et rate : c'est ce qui le rend brouillon.")]
        [Range(0f, 1f)] public float chanceRater = 0.25f;
        [Tooltip("Chasse au leader : un rival qui a au moins autant de cookies d'avance sur le bot est chassé même de loin.")]
        [Min(1)] public int ecartLeader = 3;
        [Tooltip("Jusqu'où le bot va chercher le leader, en m (assez pour couvrir une arène).")]
        [Min(0f)] public float rayonLeader = 30f;
        [Tooltip("Il utilise son objet quand un rival est à moins de cette distance, en m.")]
        [Min(0f)] public float rayonObjet = 5f;

        [Header("Ramassage")]
        [Tooltip("Il va chercher une boîte à objets si elle est à moins de cette distance et qu'il n'a pas d'objet, en m.")]
        [Min(0f)] public float rayonBoite = 8f;
        [Tooltip("Il sprinte pour fuir et pour la course au cookie sacré.")]
        public bool sprinter = true;
    }
}
