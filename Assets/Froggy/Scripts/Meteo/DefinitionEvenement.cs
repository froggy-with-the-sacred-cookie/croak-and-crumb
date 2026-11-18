using UnityEngine;

namespace Froggy.Meteo
{
    /// <summary>
    /// Un événement de météo ou de partie (P5-01) : son nom, sa durée, et ses effets sur les Froggy, posés par les
    /// Modificateurs du socle pendant qu'il est actif. La pluie (P5-02), le héron (P5-03)… en font chacun une fiche et
    /// ajoutent leur rendu en écoutant le gestionnaire (Debut, Fin). Les valeurs sont PROVISOIRES.
    /// </summary>
    [CreateAssetMenu(fileName = "Evenement", menuName = "Froggy/Météo/Événement")]
    public class DefinitionEvenement : ScriptableObject
    {
        [Tooltip("Le nom annoncé aux joueurs.")]
        public string nom = "Événement";

        [Tooltip("Durée de l'événement, en s.")]
        [Min(0.1f)] public float duree = 20f;

        [Tooltip("Vitesse des Froggy pendant l'événement (1 : inchangée, 1,2 : 20 % plus vite).")]
        [Min(0f)] public float facteurVitesse = 1f;

        [Tooltip("Hauteur de saut des Froggy pendant l'événement (1 : inchangée).")]
        [Min(0f)] public float facteurSaut = 1f;

        /// <summary>Les effets en mots, pour l'annonce et le HUD : « vitesse +30 % · saut −50 % » ; vide sans effet.</summary>
        public string Effets
        {
            get
            {
                string texte = Effet("vitesse", facteurVitesse);
                string saut = Effet("saut", facteurSaut);
                if (saut.Length > 0) texte = texte.Length > 0 ? texte + " · " + saut : saut;
                return texte;
            }
        }

        private static string Effet(string quoi, float facteur)
        {
            int pourcent = Mathf.RoundToInt((facteur - 1f) * 100f);
            if (pourcent == 0) return "";
            return $"{quoi} {(pourcent > 0 ? "+" : "−")}{Mathf.Abs(pourcent)} %";
        }
    }
}
