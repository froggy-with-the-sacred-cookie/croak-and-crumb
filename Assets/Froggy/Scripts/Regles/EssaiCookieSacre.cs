using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Froggy.Regles
{
    /// <summary>
    /// Pour essayer le cookie sacré à la main (P5-08), en attendant la torgnole (P4-10) et le gel éclair : la touche K le
    /// fait lâcher à son porteur, derrière lui, comme s'il venait d'être frappé. Outil de scène de test (comme
    /// EssaiLacher) : à ne pas poser dans une arène.
    /// </summary>
    public class EssaiCookieSacre : MonoBehaviour
    {
        [SerializeField] private PartieCookieSacre sacre;
        [SerializeField] private PartieCookies cookies;
        private GUIStyle style;

        private void Update()
        {
            var clavier = Keyboard.current;
            if (clavier != null && clavier[Key.K].wasPressedThisFrame) Lacher();
        }

        /// <summary>Fait lâcher le cookie sacré à son porteur. Rend false si personne ne le porte.</summary>
        public bool Lacher()
        {
            if (sacre == null || cookies == null || sacre.Regles == null) return false;
            var porteur = cookies.Porteurs.FirstOrDefault(p => p != null && p.Joueur == sacre.Regles.Porteur);
            return porteur != null && sacre.FaireLacher(porteur, Vector3.zero);
        }

        private void OnGUI()
        {
            if (sacre == null || sacre.Regles == null || sacre.Regles.Etat != EtatCookieSacre.Porte) return;
            if (style == null) style = new GUIStyle(GUI.skin.label) { fontSize = 16 };
            var zone = new Rect(14f, Screen.height - 112f, 700f, 26f);   // au-dessus des messages des objets (P4)
            style.normal.textColor = Color.black;
            GUI.Label(new Rect(zone.x + 1f, zone.y + 1f, zone.width, zone.height), "K : faire lâcher le cookie sacré à son porteur", style);
            style.normal.textColor = Color.white;
            GUI.Label(zone, "K : faire lâcher le cookie sacré à son porteur", style);
        }
    }
}
