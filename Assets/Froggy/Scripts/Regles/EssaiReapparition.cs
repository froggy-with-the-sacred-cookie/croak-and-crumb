using UnityEngine;
using UnityEngine.InputSystem;

namespace Froggy.Regles
{
    /// <summary>
    /// Pour essayer la réapparition (P3-04) à la main : K fait tomber la Froggy hors de l'arène (elle est posée sous la
    /// hauteur de chute), J la frappe comme le fera la torgnole (P4-10) : elle lâche un cookie, sauf si elle est intouchable.
    /// Outil de scène de test : à ne pas poser dans une arène.
    /// </summary>
    public class EssaiReapparition : MonoBehaviour
    {
        [SerializeField] private ApparitionJoueurs apparition;

        private GUIStyle style;
        private string dernier = "";

        private void Update()
        {
            var clavier = Keyboard.current;
            if (clavier == null) return;
            if (clavier[Key.K].wasPressedThisFrame) Tomber();
            if (clavier[Key.J].wasPressedThisFrame) Frapper();
        }

        /// <summary>La Froggy pilotée : le premier joueur qui a un Rigidbody.</summary>
        public PorteurCookies Froggy
        {
            get
            {
                if (apparition == null || apparition.Partie == null) return null;
                foreach (var p in apparition.Partie.Porteurs)
                    if (p != null && p.GetComponent<Rigidbody>() != null) return p;
                return null;
            }
        }

        /// <summary>Pose la Froggy 1 m sous la hauteur de chute : elle est hors de l'arène.</summary>
        public void Tomber()
        {
            var froggy = Froggy;
            if (froggy == null || apparition.Regles == null) return;
            Vector3 p = froggy.transform.position;
            p.y = apparition.Regles.HauteurChute - 1f;
            froggy.transform.position = p;
            froggy.GetComponent<Rigidbody>().position = p;
            dernier = "Froggy est tombée.";
        }

        /// <summary>Frappe la Froggy : elle lâche un cookie devant elle, si elle en porte un et n'est pas intouchable.</summary>
        public void Frapper()
        {
            var froggy = Froggy;
            if (froggy == null) return;
            if (froggy.Intouchable) dernier = "Coup sans effet : Froggy est intouchable.";
            else if (froggy.Nombre == 0) dernier = "Coup sans effet : Froggy ne porte aucun cookie.";
            else dernier = apparition.Partie.FaireLacher(froggy, froggy.transform.forward) ? "Froggy a lâché un cookie." : "Coup sans effet.";
        }

        private void OnGUI()
        {
            var froggy = Froggy;
            if (froggy == null || apparition.Regles == null) return;
            // en bas à droite : le menu de debug s'ouvre à gauche
            if (style == null) style = new GUIStyle(GUI.skin.label) { fontSize = 16, alignment = TextAnchor.UpperRight };
            var regles = apparition.Regles;
            string etat = regles.Etat(froggy.Joueur) == EtatReapparition.Tombe ? $"tombée, retour dans {regles.Reste(froggy.Joueur):0.0} s"
                : regles.Etat(froggy.Joueur) == EtatReapparition.Intouchable ? $"intouchable encore {regles.Reste(froggy.Joueur):0.0} s"
                : "en jeu";
            string texte = $"K : faire tomber Froggy · J : la frapper — Froggy {etat}. {dernier}";
            var zone = new Rect(Screen.width - 914f, Screen.height - 60f, 900f, 26f);
            style.normal.textColor = Color.black;
            GUI.Label(new Rect(zone.x + 1f, zone.y + 1f, zone.width, zone.height), texte, style);
            style.normal.textColor = Color.white;
            GUI.Label(zone, texte, style);
        }
    }
}
