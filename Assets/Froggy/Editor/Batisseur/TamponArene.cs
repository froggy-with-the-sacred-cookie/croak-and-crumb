using System;
using System.Collections.Generic;
using UnityEngine;

namespace Froggy.Outils
{
    /// <summary>
    /// Un tampon (un fichier par tampon dans Assets/Froggy/Batisseur/Tampons) : un coin d'arène réussi (une berge, un îlot
    /// de nénuphars, un abri au cookie) enregistré pour être reposé ailleurs, tourné, en miroir et un peu varié. Le jeu ne
    /// le lit pas. Repris des tampons de Lovecraft (2026-10-01).
    /// Une pièce garde son préfabriqué et sa place par rapport au centre du tampon, pas les retouches faites sur l'instance
    /// (couleur changée dans l'Inspecteur) : pour qu'elles comptent, en faire une variante de préfabriqué.
    /// </summary>
    [CreateAssetMenu(menuName = "Froggy/Bâtisseur/Tampon", fileName = "Tampon")]
    public class TamponArene : ScriptableObject
    {
        [Serializable]
        public class Piece
        {
            public GameObject modele;
            [Tooltip("Par rapport au centre du tampon (au sol)")] public Vector3 position;
            [Tooltip("Par-dessus la rotation du préfabriqué")] public Quaternion rotation = Quaternion.identity;
            [Tooltip("Par rapport à la taille du préfabriqué")] public Vector3 echelle = Vector3.one;
            [Tooltip("Sa hauteur au-dessus du sol quand le tampon a été enregistré : reposée, elle suit le relief")] public float hauteurSol;
            [Tooltip("Ne varie jamais (un pont, un mur, une grande pièce dont la place compte)")] public bool fixe;
        }

        [Tooltip("Ce qu'il est, à quoi il sert : s'affiche au survol")] [TextArea(2, 4)] public string note;
        public List<Piece> pieces = new List<Piece>();
    }
}
