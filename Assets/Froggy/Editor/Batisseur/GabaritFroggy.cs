using UnityEngine;

namespace Froggy.Outils
{
    /// <summary>
    /// Le gabarit de Froggy pour l'analyse des trajets du Bâtisseur (Assets/Froggy/Batisseur/Gabarit.asset) : sa taille, ses
    /// vitesses, son saut, l'effet de la météo. Toutes les valeurs sont PROVISOIRES tant que le contrôleur de Froggy (P1-01)
    /// et la nage (P1-02) ne sont pas faits : « Reprendre du contrôleur » relit la course et le saut du contrôleur actuel.
    /// Le jeu ne le lit pas ; le Bâtisseur s'en sert pour mesurer, pas pour jouer.
    /// </summary>
    [CreateAssetMenu(menuName = "Froggy/Bâtisseur/Gabarit de Froggy", fileName = "Gabarit")]
    public class GabaritFroggy : ScriptableObject
    {
        public enum Meteo { [InspectorName("Temps normal")] Normal, [InspectorName("Pluie tropicale")] Pluie, Neige }

        [Header("Corps")]
        [Tooltip("Rayon de Froggy, en mètres. PROVISOIRE.")] [Min(.05f)] public float rayon = .3f;
        [Tooltip("Hauteur de Froggy, en mètres. PROVISOIRE.")] [Min(.2f)] public float hauteur = 1f;
        [Tooltip("Marche franchie sans sauter, en mètres. PROVISOIRE.")] [Min(0)] public float hauteurMarche = .35f;
        [Tooltip("Pente au-delà de laquelle on ne tient plus debout, en degrés. PROVISOIRE.")] [Range(0, 89)] public float penteMax = 40;

        [Header("Vitesses")]
        [Tooltip("Course, en m/s (contrôleur actuel : 6). PROVISOIRE.")] [Min(.1f)] public float vitesseCourse = 6;
        [Tooltip("Nage, en m/s. HYPOTHÈSE : la moitié de la course, en attendant P1-02.")] [Min(.1f)] public float vitesseNage = 3;
        [Tooltip("Sortir de l'eau sur une berge : hauteur franchie, en mètres. HYPOTHÈSE.")] [Min(0)] public float sortieDeLEau = .6f;

        [Header("Saut")]
        [Tooltip("Hauteur du saut, en mètres (contrôleur actuel : 1,5). PROVISOIRE.")] [Min(.1f)] public float hauteurSaut = 1.5f;

        [Header("Météo")]
        [Tooltip("La pluie tropicale accélère les grenouilles (Notion, V1) : de combien ? HYPOTHÈSE.")] [Min(.1f)] public float multiplicateurPluie = 1.25f;
        [Tooltip("La neige ralentit les déplacements (Notion, V1) : de combien ? HYPOTHÈSE.")] [Min(.1f)] public float multiplicateurNeige = .75f;

        [Header("Analyse")]
        [Tooltip("Côté d'une case de l'analyse, en mètres : plus fin est plus juste et plus lent")] [Range(.2f, 2)] public float pas = .5f;
        [Tooltip("On ne cherche pas le sol plus haut que ça au-dessus des repères : sinon Froggy « marcherait » sur la couronne des arbres")]
        [Min(.5f)] public float plafond = 3;
        [Tooltip("Les objets d'eau (noms ou morceaux de nom, séparés par « ; ») : l'eau n'a pas de collision, on la reconnaît à son nom")]
        public string nomsEau = "Eau";

        /// <summary>Les vitesses et le saut pour une météo.</summary>
        public TrajetsArene.Mouvement Mouvement(Meteo m)
        {
            float k = m == Meteo.Pluie ? multiplicateurPluie : m == Meteo.Neige ? multiplicateurNeige : 1;
            return new TrajetsArene.Mouvement
            {
                course = vitesseCourse * k,   // pas de sprint ; le dash, au sol seulement, ne rallonge pas un saut (#134)
                nage = vitesseNage * k,
                hauteurSaut = hauteurSaut,
                gravite = Mathf.Abs(Physics.gravity.y),
                marche = hauteurMarche,
                sortie = sortieDeLEau,
            };
        }
    }
}
