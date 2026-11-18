using UnityEngine;

namespace Froggy.Regles
{
    /// <summary>
    /// Réglages des cookies (P3-01). Toutes les valeurs sont PROVISOIRES, et les règles qu'elles portent sont des
    /// PROPOSITIONS tant que Notion ne les écrit pas (18 novembre 2026) : réapparition au choix, pas de limite de
    /// cookies portés, ralentissement du porteur branché mais sans effet.
    /// </summary>
    [CreateAssetMenu(fileName = "ReglagesCookies", menuName = "Froggy/Règles/Réglages des cookies")]
    public class ReglagesCookies : ScriptableObject
    {
        [Header("Apparition")]
        [Tooltip("Un point redonne un cookie ce temps après le ramassage, en s. 0 : jamais, un seul cookie par point. PROVISOIRE.")]
        [Min(0f)] public float delaiReapparition = 8f;

        [Tooltip("Hauteur du cookie au-dessus de son repère, en m. PROVISOIRE.")]
        public float hauteur = 0.6f;

        [Header("Ramassage")]
        [Tooltip("Froggy ramasse un cookie en passant à cette distance, en m, sans touche. À terme, la portée de la langue. PROVISOIRE.")]
        [Min(0f)] public float rayonRamassage = 1.2f;

        [Tooltip("Nombre de cookies qu'un joueur peut porter. 0 : pas de limite (le score est le nombre de cookies portés).")]
        [Min(0)] public int maxPortes;

        [Header("Porteur")]
        [Tooltip("Chaque cookie porté retire cette part de la vitesse (0,1 : −10 % par cookie, qui se multiplient). " +
                 "0 : sans effet, À TRANCHER par l'équipe avant d'y toucher.")]
        [Range(0f, 0.5f)] public float ralentissementParCookie;

        /// <summary>Le facteur de vitesse d'un joueur qui porte « cookies » cookies. 1 : rien ne change.</summary>
        public float FacteurVitesse(int cookies) => Mathf.Pow(1f - ralentissementParCookie, Mathf.Max(0, cookies));
    }
}
