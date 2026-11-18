using UnityEngine;

namespace Froggy.Regles
{
    /// <summary>
    /// Les réglages du dash (#134). Valeurs de départ choisies par Florent le 18 novembre 2026 (4 m en 0,2 s, recharge
    /// 3 s) ; le reste est une PROPOSITION de l'IA, à régler en jouant. La vitesse du dash ne se règle pas : elle se
    /// dérive de la distance et de la durée (skill ressenti-mouvement).
    /// </summary>
    [CreateAssetMenu(menuName = "Froggy/Réglages du dash", fileName = "ReglagesDash")]
    public class ReglagesDash : ScriptableObject
    {
        [Tooltip("Distance parcourue pendant le dash, en m. PROVISOIRE (choix de Florent).")]
        [Min(0.5f)] public float distance = 4f;

        [Tooltip("Durée du dash, en s. La vitesse en découle (4 m en 0,2 s : 20 m/s). PROVISOIRE (choix de Florent).")]
        [Min(0.05f)] public float duree = 0.2f;

        [Tooltip("Attente avant le dash suivant, en s. PROVISOIRE (choix de Florent).")]
        [Min(0f)] public float recharge = 3f;

        [Tooltip("Distance entre deux Froggy à laquelle un dash percute l'autre, en m (centre à centre). PROPOSITION.")]
        [Min(0.3f)] public float rayonChoc = 1.1f;

        [Tooltip("Vitesse donnée à la Froggy bousculée, dans le sens du dash, en m/s (la torgnole recule à son réglage). PROPOSITION.")]
        [Min(0f)] public float recul = 9f;

        [Tooltip("Durée de l'assommement quand deux Froggy se dashent dessus en même temps, en s. PROPOSITION.")]
        [Min(0f)] public float dureeAssomme = 1.5f;

        [Tooltip("Petites étoiles au-dessus d'une Froggy assommée, en attendant l'animation (P7). PROVISOIRE.")]
        public bool visuelProvisoire = true;
    }
}
