using UnityEngine;

namespace Froggy.Objets
{
    /// <summary>
    /// La fiche de réglages d'un objet (P4-01) : un objet = une fiche. Le système d'objets ne lit que le poids du tirage
    /// et le temps de recharge ; l'effet de l'objet (P4-02 super croassement, P4-07 camouflage, P4-09 gel éclair) lit le
    /// reste, et ajoute ses propres réglages dans une fiche qui hérite de celle-ci.
    /// Toutes les valeurs sont PROVISOIRES tant que Notion ne les écrit pas (18 novembre 2026).
    /// </summary>
    [CreateAssetMenu(fileName = "FicheObjet", menuName = "Froggy/Objets/Fiche d'objet")]
    public class FicheObjet : ScriptableObject
    {
        [Header("Présentation")]
        [Tooltip("Le nom affiché au joueur.")]
        public string nom = "Objet";

        [Tooltip("Ce que fait l'objet, en une phrase (HUD, aide).")]
        [TextArea] public string description;

        [Tooltip("L'icône de l'emplacement d'objet (P6-06). Vide : le nom seul.")]
        public Sprite icone;

        [Tooltip("La couleur de l'objet dans l'affichage provisoire, en attendant son icône.")]
        public Color couleur = Color.white;

        [Header("Tirage")]
        [Tooltip("Sa part dans le tirage d'une boîte mystère : 2 sort deux fois plus souvent que 1. 0 : ne sort jamais. PROVISOIRE.")]
        [Min(0f)] public float poids = 1f;

        [Header("Utilisation")]
        [Tooltip("Durée de l'effet, en s. 0 : effet instantané. Lue par l'effet de l'objet, pas par le système. PROVISOIRE.")]
        [Min(0f)] public float duree;

        [Tooltip("Temps de recharge après utilisation, en s : le joueur ne peut pas utiliser un autre objet avant. PROVISOIRE.")]
        [Min(0f)] public float recharge = 2f;
    }
}
