using System;
using System.Collections.Generic;
using UnityEngine;

namespace Froggy.Meteo
{
    /// <summary>
    /// La fiche météo d'une arène (P5-01) : quels événements peuvent sortir, avec quel poids, et le rythme (calme entre deux
    /// événements, temps d'annonce). Les poids se règlent ici, sans code. Les valeurs sont PROVISOIRES.
    /// </summary>
    [CreateAssetMenu(fileName = "ReglagesMeteo", menuName = "Froggy/Météo/Réglages de l'arène")]
    public class ReglagesMeteo : ScriptableObject
    {
        [Serializable]
        public class Tirage
        {
            public DefinitionEvenement evenement;
            [Tooltip("0 : ne sort jamais ; 2 sort deux fois plus souvent que 1.")]
            [Min(0f)] public float poids = 1f;
        }

        [Tooltip("Les événements possibles dans cette arène et leur poids.")]
        public List<Tirage> evenements = new List<Tirage>();

        [Tooltip("Temps d'annonce avant le début d'un événement, en s.")]
        [Min(0f)] public float annonce = 5f;

        [Tooltip("Calme minimal entre deux événements, en s.")]
        [Min(0f)] public float pauseMin = 30f;

        [Tooltip("Calme maximal entre deux événements, en s.")]
        [Min(0f)] public float pauseMax = 50f;

        [Tooltip("Graine du tirage : la même suite d'événements partout. 0 : une graine différente à chaque partie.")]
        public int graine;
    }
}
