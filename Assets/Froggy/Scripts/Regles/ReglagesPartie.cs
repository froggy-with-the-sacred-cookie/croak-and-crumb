using UnityEngine;

namespace Froggy.Regles
{
    /// <summary>Réglages du déroulement d'une partie (P3-03) : compte à rebours de départ et durée. PROVISOIRES.</summary>
    [CreateAssetMenu(fileName = "ReglagesPartie", menuName = "Froggy/Règles/Réglages de la partie")]
    public class ReglagesPartie : ScriptableObject
    {
        [Tooltip("Compte à rebours avant le départ, en s. Les joueurs ne bougent pas et rien ne se ramasse. PROVISOIRE.")]
        [Min(0f)] public float decompte = 3f;

        [Tooltip("Durée de la partie, en s. P3-03 demande 3 minutes. Pour un essai rapide, mettre 20 dans une copie.")]
        [Min(1f)] public float duree = 180f;
    }
}
