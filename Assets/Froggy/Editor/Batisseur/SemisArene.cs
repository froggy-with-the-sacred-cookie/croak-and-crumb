using System;
using System.Collections.Generic;
using UnityEngine;

namespace Froggy.Outils
{
    /// <summary>
    /// Les mélanges de l'outil Semer du Bâtisseur d'arène (Assets/Froggy/Batisseur/Semis.asset) : ce qu'on répand au pinceau
    /// (un sous-bois, des pierres, un massif de karikomi), en quelle proportion, à quelle densité, avec quel espacement, en
    /// massifs ou non. Tout se change dans la fenêtre ou l'Inspecteur, sans code. Le jeu ne le lit pas.
    /// Repris de l'outil Semis de Lovecraft (2026-10-01), refait pour des arènes en 3D libre : on sème sur ce qui a une
    /// collision (le sol, un talus), pas sur une grille.
    /// </summary>
    [CreateAssetMenu(menuName = "Froggy/Bâtisseur/Mélanges du semis", fileName = "Semis")]
    public class SemisArene : ScriptableObject
    {
        [Serializable]
        public class Element
        {
            [Tooltip("Le préfabriqué semé")] public GameObject modele;
            [Tooltip("Sa part dans le mélange : 2 en sème deux fois plus qu'un élément à 1")] [Min(0)] public float poids = 1;
            [Tooltip("Penché comme le sol (rochers, mousse) plutôt que droit (arbres, lanternes)")] public bool suivreLaPente;
            [Tooltip("Enfoncé dans le sol, en mètres (un rocher qui ne doit pas avoir l'air posé)")] public float enfoncement;
        }

        [Serializable]
        public class Melange
        {
            public string nom = "Nouveau mélange";
            [TextArea(1, 3)] public string note;
            public List<Element> elements = new List<Element>();
            [Tooltip("Objets par m² semés à chaque passage du pinceau ; repasser épaissit, jusqu'à l'espacement")] [Range(.01f, 4)] public float densite = .3f;
            [Tooltip("Distance minimale entre deux objets du mélange, en mètres")] [Range(0, 10)] public float espacement = 1;
            [Tooltip("En massifs et clairières plutôt que partout pareil (0 : régulier, 1 : très groupé)")] [Range(0, 1)] public float massifs = .4f;
            [Tooltip("Taille au hasard, de…")] [Range(.2f, 3)] public float tailleMin = .85f;
            [Tooltip("… à")] [Range(.2f, 3)] public float tailleMax = 1.15f;
            [Tooltip("Pente au-delà de laquelle on ne sème pas, en degrés")] [Range(0, 90)] public float penteMax = 35;
            [Tooltip("Altitude en dessous de laquelle on ne sème pas (le fond d'un étang : l'eau n'a pas de collision)")] public float altitudeMin = -1000;
            [Tooltip("Altitude au-dessus de laquelle on ne sème pas")] public float altitudeMax = 1000;
            [Tooltip("Ne jamais semer dans un autre objet (une lanterne, un pont, un mur)")] public bool eviterLesObjets = true;
        }

        public List<Melange> melanges = new List<Melange>();
    }
}
