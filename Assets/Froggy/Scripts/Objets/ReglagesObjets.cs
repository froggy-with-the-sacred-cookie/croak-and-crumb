using System.Collections.Generic;
using UnityEngine;

namespace Froggy.Objets
{
    /// <summary>
    /// Réglages du système d'objets (P4-01) : les boîtes mystère et la liste des objets qu'elles donnent. Toutes les
    /// valeurs sont PROVISOIRES, et les règles qu'elles portent sont des PROPOSITIONS tant que Notion ne les écrit pas
    /// (18 novembre 2026) : ramassage en passant à portée, un joueur qui a déjà un objet laisse la boîte.
    /// </summary>
    [CreateAssetMenu(fileName = "ReglagesObjets", menuName = "Froggy/Objets/Réglages des objets")]
    public class ReglagesObjets : ScriptableObject
    {
        [Header("Boîtes mystère")]
        [Tooltip("Une boîte revient ce temps après avoir été prise, en s. 0 : jamais, une seule boîte par repère. PROVISOIRE.")]
        [Min(0f)] public float delaiReapparition = 10f;

        [Tooltip("Hauteur de la boîte au-dessus de son repère, en m. PROVISOIRE.")]
        public float hauteur = 0.6f;

        [Tooltip("Froggy prend une boîte en passant à cette distance, en m, sans touche. PROVISOIRE.")]
        [Min(0f)] public float rayonRamassage = 1.2f;

        [Header("Tirage")]
        [Tooltip("Les objets qu'une boîte peut donner. En V1 : gel éclair, super croassement, camouflage caméléon.")]
        public List<FicheObjet> objets = new List<FicheObjet>();

        [Tooltip("Graine du tirage. 0 : une graine différente à chaque partie. Autre : toujours la même suite (test, rejeu).")]
        public int graine;
    }
}
