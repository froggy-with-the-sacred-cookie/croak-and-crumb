using System.Collections.Generic;
using Froggy.Meteo;
using UnityEngine;

namespace Froggy.Objets
{
    /// <summary>
    /// Réglages du camouflage caméléon (P4-07). Les 6 s viennent de Notion ; le reste est PROPOSITION, à juger à l'œil :
    /// un camouflé immobile ne doit pas se voir à 10 m, un camouflé qui court doit chatoyer un peu.
    /// </summary>
    [CreateAssetMenu(fileName = "ReglagesCamouflage", menuName = "Froggy/Objets/Réglages du camouflage")]
    public class ReglagesCamouflage : ScriptableObject
    {
        [Tooltip("La fiche de l'objet : l'effet part quand une Froggy utilise cet objet.")]
        public FicheObjet fiche;

        [Tooltip("Durée du camouflage, en s (Notion : 6 s).")]
        [Min(0f)] public float duree = 6f;

        [Tooltip("Opacité d'un camouflé immobile (0 : invisible, 1 : normal).")]
        [Range(0f, 1f)] public float opaciteImmobile = 0.04f;

        [Tooltip("Opacité d'un camouflé qui court à pleine vitesse : le chatoiement trahit le mouvement.")]
        [Range(0f, 1f)] public float opaciteEnCourse = 0.22f;

        [Tooltip("Vitesse au sol (m/s) à laquelle on atteint l'opacité « en course ».")]
        [Min(0.1f)] public float vitesseDeCourse = 6f;

        [Tooltip("Ampleur du chatoiement en mouvement (ajoutée à l'opacité), et sa fréquence en Hz.")]
        [Range(0f, 0.5f)] public float chatoiement = 0.08f;
        [Min(0f)] public float frequence = 5f;

        [Tooltip("Durée des fondus d'entrée et de sortie du camouflage, en s.")]
        [Min(0f)] public float fondu = 0.35f;

        [Tooltip("Les événements météo qui révèlent les camouflés et empêchent de se camoufler (la pluie, Notion).")]
        public List<DefinitionEvenement> revelePar = new List<DefinitionEvenement>();
    }
}
