using UnityEngine;

namespace Froggy.Deplacement
{
    /// <summary>
    /// Réglages des nénuphars (#16), lus par Nenuphar. Toutes PROVISOIRES, à juger en jeu.
    /// Faits appliqués (fiche Docs/Recherches/nenuphars-2026-11-18.md) :
    /// - un nénuphar ordinaire est souple (18 à 31 N/m, contre 690 pour le géant Victoria ; Box et al. 2022) : une
    ///   grenouille le fait nettement descendre, et c'est l'eau déplacée qui le remonte ;
    /// - un corps flottant chargé oscille à ω ≈ √(g/d), d son enfoncement : la fréquence se dérive de la profondeur,
    ///   elle ne se règle pas à part (comme la vitesse d'un saut se dérive de sa hauteur) ;
    /// - l'amortissement vient des vagues que le nénuphar fait en bougeant : non mesuré, réglé à l'œil.
    /// La houle, le lacet et la pente au vent sont déjà dans le shader (CouleurSommet, _Flotter) : pas ici.
    /// </summary>
    [CreateAssetMenu(fileName = "ReglagesNenuphar", menuName = "Froggy/Réglages des nénuphars")]
    public class ReglagesNenuphar : ScriptableObject
    {
        [Header("Le poids")]
        [Tooltip("De combien le nénuphar descend sous une grenouille posée, en m. Donne aussi sa fréquence (≈ 2,2 Hz à 5 cm). PROVISOIRE.")]
        [Min(0.005f)] public float enfoncementParGrenouille = 0.05f;
        [Tooltip("Au plus, quel que soit le nombre de grenouilles, en m. Au-delà de la hauteur du nénuphar au-dessus de " +
                 "l'eau (9 cm dans le jardin), l'eau passe dessus. PROVISOIRE.")]
        [Min(0f)] public float enfoncementMax = 0.12f;
        [Tooltip("Amortissement (1 : revient sans rebondir, 0 : rebondit sans fin). PROVISOIRE : deux petits rebonds visibles.")]
        [Range(0.05f, 1f)] public float amortissement = 0.35f;

        [Header("L'atterrissage")]
        [Tooltip("Part de la vitesse de chute de Froggy donnée au nénuphar quand il s'y pose, sans unité : plus il tombe de " +
                 "haut, plus le nénuphar plonge avant de remonter. PROVISOIRE.")]
        [Range(0f, 0.5f)] public float impact = 0.1f;
        [Tooltip("Vitesse de plongée au plus, en m/s : une chute de très haut ne l'envoie pas au fond. PROVISOIRE.")]
        [Min(0f)] public float impactMax = 1f;

        [Header("Le bord")]
        [Tooltip("Inclinaison quand une grenouille se tient tout au bord, en degrés : le côté chargé descend. PROVISOIRE.")]
        [Range(0f, 15f)] public float penteAuBord = 4f;
    }
}
