using System;
using System.Collections.Generic;
using UnityEngine;

namespace Froggy.Outils
{
    /// <summary>
    /// Les modèles de tracé de l'outil Tracer du Bâtisseur d'arène (Assets/Froggy/Batisseur/Traces.asset) : ce qu'une courbe
    /// devient (une clôture, une muraille, une allée de lanternes, un gué de pierres, un chemin de nénuphars). Tout se
    /// change dans la fenêtre ou l'Inspecteur, sans code. Le jeu ne le lit pas.
    /// Repris de l'outil Tracer de Lovecraft (2026-10-01) ; là-bas la courbe peignait aussi le sol des cases, ici elle ne
    /// pose que des préfabriqués.
    /// </summary>
    [CreateAssetMenu(menuName = "Froggy/Bâtisseur/Modèles de tracé", fileName = "Traces")]
    public class TracesArene : ScriptableObject
    {
        public enum Disposition { [InspectorName("Une rangée")] Rangee, [InspectorName("Deux rangées (allée)")] Allee }
        public enum Orientation
        {
            [InspectorName("Le long de la courbe")] LeLong,
            [InspectorName("Face à la courbe")] Face,
            [InspectorName("Au hasard")] Hasard,
        }
        public enum Hauteur { [InspectorName("Sur le sol")] Sol, [InspectorName("Celle de la courbe (sur l'eau, un pont)")] Courbe }
        public enum Axe { X, Z }

        [Serializable]
        public class Piece
        {
            public GameObject modele;
            [Min(0)] public float poids = 1;
        }

        [Serializable]
        public class Trace
        {
            public string nom = "Nouveau tracé";
            [TextArea(1, 3)] public string note;
            public List<Piece> pieces = new List<Piece>();
            public Disposition disposition = Disposition.Rangee;
            [Tooltip("Mètres entre deux pièces")] [Min(.1f)] public float pas = 2;
            [Tooltip("Le pas est la longueur de la pièce (un mur, une clôture : les pièces se touchent)")] public bool pasDeLaPiece;
            [Tooltip("L'axe de la longueur de la pièce, dans son préfabriqué")] public Axe axe = Axe.X;
            public Orientation orientation = Orientation.LeLong;
            [Tooltip("Allée : mètres entre les deux rangées")] [Min(0)] public float ecartRangees = 3;
            [Tooltip("Écart de côté au hasard, en mètres")] [Min(0)] public float decalage;
            [Tooltip("Écart de lacet au hasard, en degrés")] [Range(0, 180)] public float ecartLacet;
            [Range(.2f, 3)] public float tailleMin = 1;
            [Range(.2f, 3)] public float tailleMax = 1;
            public Hauteur hauteur = Hauteur.Sol;
            [Tooltip("Retire ce que le semis a posé sur le passage (à moins de la moitié de cette largeur de la courbe, en mètres)")]
            [Min(0)] public float degager;
        }

        public List<Trace> traces = new List<Trace>();
    }
}
