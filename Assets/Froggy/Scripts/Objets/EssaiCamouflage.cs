using UnityEngine;
using UnityEngine.InputSystem;

namespace Froggy.Objets
{
    /// <summary>
    /// Pour essayer le camouflage caméléon (P4-07) à la main sans attendre de le tirer dans une boîte : H camoufle la
    /// Froggy du joueur local. Outil de scène de test : à ne pas poser dans une arène.
    /// </summary>
    public class EssaiCamouflage : MonoBehaviour
    {
        [SerializeField] private EffetCamouflage effet;
        [SerializeField] private PartieObjets partie;

        private GUIStyle style;

        private void Update()
        {
            var clavier = Keyboard.current;
            if (clavier == null || !clavier[Key.H].wasPressedThisFrame || effet == null || partie == null || partie.Local == null) return;
            effet.Camoufler(partie.Local.gameObject);
        }

        private void OnGUI()
        {
            if (effet == null || partie == null) return;
            if (style == null) style = new GUIStyle(GUI.skin.label) { fontSize = 16, alignment = TextAnchor.UpperRight };
            bool cache = partie.Local != null && effet.EstCamoufle(partie.Local.gameObject);
            string texte = cache ? "H : camouflage — tu es presque invisible"
                : effet.Pluie ? "H : camouflage (impossible sous la pluie)" : "H : camouflage caméléon (essai, sans objet)";
            // au-dessus des lignes du super croassement (B) et du gel éclair (G), même coin
            var zone = new Rect(Screen.width - 914f, Screen.height - 86f, 900f, 26f);
            style.normal.textColor = Color.black;
            GUI.Label(new Rect(zone.x + 1f, zone.y + 1f, zone.width, zone.height), texte, style);
            style.normal.textColor = Color.white;
            GUI.Label(zone, texte, style);
        }
    }
}
