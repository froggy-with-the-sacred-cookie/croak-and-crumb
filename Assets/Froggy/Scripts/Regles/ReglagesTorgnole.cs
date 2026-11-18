using UnityEngine;

namespace Froggy.Regles
{
    /// <summary>Réglages de la torgnole (P4-10). La recharge vient de la carte ; le reste est PROVISOIRE.</summary>
    [CreateAssetMenu(fileName = "ReglagesTorgnole", menuName = "Froggy/Règles/Réglages de la torgnole")]
    public class ReglagesTorgnole : ScriptableObject
    {
        [Tooltip("Temps avant de pouvoir refrapper, en s, coup raté compris. P4-10 : 5 s.")]
        [Min(0f)] public float recharge = 5f;

        [Tooltip("Distance à plat jusqu'où le coup touche, en m (Froggy fait 1 m de large). PROVISOIRE.")]
        [Min(0f)] public float portee = 1.6f;

        [Tooltip("Demi-angle du cône devant Froggy où le coup touche, en degrés. PROVISOIRE.")]
        [Range(0f, 180f)] public float demiAngle = 60f;

        [Tooltip("Écart de hauteur maximal avec la cible, en m (pas de coup à travers un étage). PROVISOIRE.")]
        [Min(0f)] public float hauteur = 1.2f;

        [Tooltip("Vitesse donnée à la cible touchée, à l'opposé du frappeur, en m/s : le recul dit « touché ». PROVISOIRE.")]
        [Min(0f)] public float recul = 4f;
    }
}
