using UnityEngine;
using UnityEngine.InputSystem;

namespace Froggy.Objets
{
    /// <summary>
    /// Pour essayer le gel éclair (P4-09) à la main sans attendre de le tirer dans une boîte : G lance le projectile
    /// devant la Froggy du joueur local. Outil de scène de test : à ne pas poser dans une arène.
    /// </summary>
    public class EssaiGelEclair : MonoBehaviour
    {
        [SerializeField] private EffetGelEclair effet;
        [SerializeField] private PartieObjets partie;

        private GUIStyle style;

        private void Update()
        {
            var clavier = Keyboard.current;
            if (clavier == null || !clavier[Key.G].wasPressedThisFrame || effet == null || partie == null || partie.Local == null) return;
            effet.Tirer(partie.Local.gameObject);
        }

        private void OnGUI()
        {
            if (effet == null) return;
            if (style == null) style = new GUIStyle(GUI.skin.label) { fontSize = 16, alignment = TextAnchor.UpperRight };
            string texte = effet.Regles.NombreDeGeles > 0 ? "G : gel éclair — un joueur est gelé" : "G : gel éclair (essai, sans objet)";
            // au-dessus de la ligne du super croassement (B), même coin
            var zone = new Rect(Screen.width - 914f, Screen.height - 60f, 900f, 26f);
            style.normal.textColor = Color.black;
            GUI.Label(new Rect(zone.x + 1f, zone.y + 1f, zone.width, zone.height), texte, style);
            style.normal.textColor = Color.white;
            GUI.Label(zone, texte, style);
        }
    }
}
