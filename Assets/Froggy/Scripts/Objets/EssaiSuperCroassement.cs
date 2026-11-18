using UnityEngine;
using UnityEngine.InputSystem;

namespace Froggy.Objets
{
    /// <summary>
    /// Pour essayer le super croassement (P4-02) à la main sans attendre de le tirer dans une boîte : B lance l'élan de la
    /// Froggy du joueur local. Outil de scène de test : à ne pas poser dans une arène.
    /// </summary>
    public class EssaiSuperCroassement : MonoBehaviour
    {
        [SerializeField] private EffetSuperCroassement effet;
        [SerializeField] private PartieObjets partie;

        private GUIStyle style;

        private void Update()
        {
            var clavier = Keyboard.current;
            if (clavier == null || !clavier[Key.B].wasPressedThisFrame || effet == null || partie == null || partie.Local == null) return;
            effet.Lancer(partie.Local.gameObject);
        }

        private void OnGUI()
        {
            if (effet == null) return;
            if (style == null) style = new GUIStyle(GUI.skin.label) { fontSize = 16, alignment = TextAnchor.UpperRight };
            string texte = effet.ElansEnCours > 0 ? "B : super croassement — la gorge gonfle…" : "B : super croassement (essai, sans objet)";
            var zone = new Rect(Screen.width - 914f, Screen.height - 34f, 900f, 26f);
            style.normal.textColor = Color.black;
            GUI.Label(new Rect(zone.x + 1f, zone.y + 1f, zone.width, zone.height), texte, style);
            style.normal.textColor = Color.white;
            GUI.Label(zone, texte, style);
        }
    }
}
