using UnityEngine;

namespace Froggy.Regles
{
    /// <summary>Réglages de la réapparition (P3-04). PROVISOIRES : à régler en jeu par l'équipe.</summary>
    [CreateAssetMenu(fileName = "ReglagesReapparition", menuName = "Froggy/Règles/Réglages de la réapparition")]
    public class ReglagesReapparition : ScriptableObject
    {
        [Tooltip("De combien de mètres sous le point d'apparition le plus bas il faut tomber pour être hors de l'arène. " +
                 "Assez pour ne pas compter un saut dans un creux. PROVISOIRE.")]
        [Min(0.5f)] public float profondeurChute = 5f;

        [Tooltip("Temps entre la sortie de l'arène et la réapparition, en s. P3-04 : revenir en jeu en moins de 3 s, chute " +
                 "comprise (5 m de chute prennent 1 s). PROVISOIRE.")]
        [Min(0f)] public float delai = 1f;

        [Tooltip("Temps pendant lequel on ne peut pas frapper un joueur qui vient de réapparaître, en s. PROVISOIRE.")]
        [Min(0f)] public float dureeIntouchable = 2f;

        /// <summary>Le temps d'une chute de « profondeurChute » mètres, sans vitesse de départ.</summary>
        public float TempsDeChute => Mathf.Sqrt(2f * profondeurChute / Mathf.Max(0.01f, -Physics.gravity.y));
    }
}
