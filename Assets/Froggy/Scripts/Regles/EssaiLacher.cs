using UnityEngine;
using UnityEngine.InputSystem;

namespace Froggy.Regles
{
    /// <summary>
    /// Pour essayer le cookie lâché (P3-02) à la main, en attendant la torgnole (P4-10) : la touche L fait lâcher un cookie
    /// à « cible » (un mannequin posé dans la scène de test), du côté opposé au joueur le plus proche, comme si ce joueur
    /// venait de le frapper. Outil de scène de test : à ne pas poser dans une arène.
    /// </summary>
    public class EssaiLacher : MonoBehaviour
    {
        [SerializeField] private PartieCookies partie;
        [Tooltip("Celui qui lâche un cookie quand on appuie sur L.")]
        [SerializeField] private PorteurCookies cible;

        private GUIStyle style;

        private void Update()
        {
            var clavier = Keyboard.current;
            if (clavier != null && clavier[Key.L].wasPressedThisFrame) Lacher();
        }

        /// <summary>Fait lâcher un cookie à la cible, à l'opposé du joueur le plus proche. Rend false si elle ne porte rien.</summary>
        public bool Lacher()
        {
            if (partie == null || cible == null) return false;
            Vector3 direction = Vector3.zero;
            float plusProche = float.MaxValue;
            foreach (var p in partie.Porteurs)
            {
                if (p == null || p == cible) continue;
                Vector3 ecart = cible.transform.position - p.transform.position;
                if (ecart.sqrMagnitude >= plusProche) continue;
                plusProche = ecart.sqrMagnitude;
                direction = ecart;
            }
            return partie.FaireLacher(cible, direction);
        }

        private void OnGUI()
        {
            if (cible == null) return;
            if (style == null) style = new GUIStyle(GUI.skin.label) { fontSize = 16 };
            var zone = new Rect(14f, Screen.height - 34f, 700f, 26f);
            string texte = $"L : faire lâcher un cookie au mannequin (il en porte {cible.Nombre})";
            style.normal.textColor = Color.black;
            GUI.Label(new Rect(zone.x + 1f, zone.y + 1f, zone.width, zone.height), texte, style);
            style.normal.textColor = Color.white;
            GUI.Label(zone, texte, style);
        }
    }
}
