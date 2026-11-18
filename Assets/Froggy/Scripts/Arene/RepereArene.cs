using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Froggy.Arene
{
    /// <summary>
    /// Un repère de jeu posé dans l'arène : l'apparition d'un joueur (1 à 4), une apparition de cookies, une boîte à objets,
    /// le départ du gobelin du cookie sacré, le centre de l'arène (celui de la symétrie). Il ne fait rien en jeu : les règles
    /// le lisent (P3-01 cookies, P3-04 réapparition, les boîtes du poste 4, le gobelin du poste 5).
    /// PROPOSITION d'interface entre les postes, posée avec le Bâtisseur d'arène le 2026-10-29 : à valider par les
    /// responsables de ces tâches avant d'en dépendre.
    /// Dans la vue Scène, chaque joueur a sa couleur ET sa forme (rond, triangle, carré, losange), lisibles aussi par un
    /// daltonien ou en niveaux de gris.
    /// </summary>
    [DisallowMultipleComponent]
    public class RepereArene : MonoBehaviour
    {
        public enum Sorte
        {
            [InspectorName("Apparition d'un joueur")] Joueur,
            [InspectorName("Apparition de cookies")] Cookie,
            [InspectorName("Boîte à objets")] Boite,
            [InspectorName("Départ du gobelin")] Gobelin,
            [InspectorName("Centre de l'arène")] Centre,
        }

        [Flags]
        public enum Parties { [InspectorName("À 2")] Deux = 1, [InspectorName("À 3")] Trois = 2, [InspectorName("À 4")] Quatre = 4 }

        public Sorte sorte;
        [Tooltip("Apparition d'un joueur : son numéro")] [Range(1, 4)] public int joueur = 1;
        [Tooltip("Les parties où ce repère sert : une arène à 4 se joue aussi à 2 et à 3")]
        public Parties parties = Parties.Deux | Parties.Trois | Parties.Quatre;

        /// <summary>Le repère sert-il dans une partie à nbJoueurs (2 à 4) ?</summary>
        public bool SertA(int nbJoueurs) => nbJoueurs >= 2 && nbJoueurs <= 4 && (parties & (Parties)(1 << (nbJoueurs - 2))) != 0;

        /// <summary>Les couleurs des joueurs (palette d'Okabe et Ito, distinguable par les daltoniens).</summary>
        public static readonly Color[] CouleursJoueurs =
            { new Color(.90f, .62f, 0f), new Color(.34f, .71f, .91f), new Color(0f, .62f, .45f), new Color(.80f, .47f, .65f) };

        public static Color Couleur(Sorte s, int joueur)
        {
            switch (s)
            {
                case Sorte.Joueur: return CouleursJoueurs[Mathf.Clamp(joueur - 1, 0, 3)];
                case Sorte.Cookie: return new Color(.86f, .58f, .28f);
                case Sorte.Boite: return new Color(.98f, .90f, .25f);
                case Sorte.Gobelin: return new Color(.55f, .85f, .20f);
                default: return Color.white;
            }
        }

        public string Nom => sorte == Sorte.Joueur ? $"Joueur {joueur}" : sorte == Sorte.Cookie ? "Cookies" : sorte == Sorte.Boite ? "Boîte"
            : sorte == Sorte.Gobelin ? "Gobelin" : "Centre";

#if UNITY_EDITOR
        /// <summary>Vrai tant que le Bâtisseur d'arène est ouvert : c'est lui qui dessine les repères (une seule fois).</summary>
        public static bool DessinesParLeBatisseur;

        private void OnDrawGizmos() { Volume(false); if (!DessinesParLeBatisseur) Dessiner(false); }
        private void OnDrawGizmosSelected() { Volume(true); if (!DessinesParLeBatisseur) Dessiner(true); }

        /// <summary>Un volume plein, peu visible, pour qu'un clic dans la vue Scène sélectionne le repère (avec les gizmos).</summary>
        private void Volume(bool choisi)
        {
            var p = transform.position; var c = Couleur(sorte, joueur);
            Gizmos.color = new Color(c.r, c.g, c.b, choisi ? .45f : .25f);
            switch (sorte)
            {
                case Sorte.Joueur: Gizmos.DrawCube(p + Vector3.up * .5f, new Vector3(.5f, 1f, .5f)); break;
                case Sorte.Boite: Gizmos.DrawCube(p + Vector3.up * .4f, Vector3.one * .8f); break;
                case Sorte.Centre: Gizmos.DrawSphere(p, .2f); break;
                default: Gizmos.DrawSphere(p + Vector3.up * .3f, .3f); break;
            }
        }

        /// <summary>
        /// Le dessin du repère (forme, sens, nom). Le Bâtisseur l'appelle à chaque image de la vue Scène : les repères se
        /// voient même quand les gizmos sont coupés (2026-10-29 : coupés dans la vue Scène de Florent, les apparitions
        /// étaient invisibles).
        /// </summary>
        public void Dessiner(bool choisi)
        {
            var p = transform.position; var c = Couleur(sorte, joueur);
            var avant = Quaternion.Euler(0, transform.eulerAngles.y, 0) * Vector3.forward;
            Handles.color = c;
            float l = choisi ? 4 : 3;
            switch (sorte)
            {
                case Sorte.Joueur:
                    Handles.DrawAAPolyLine(l, Forme(p + Vector3.up * .03f, joueur, .7f, avant));
                    Handles.DrawAAPolyLine(l, p + Vector3.up * .05f, p + Vector3.up * .05f + avant * 1.2f);
                    Handles.DrawWireDisc(p + Vector3.up * 1f, Vector3.up, .25f);
                    break;
                case Sorte.Cookie:
                    Handles.DrawWireDisc(p + Vector3.up * .03f, Vector3.up, .5f, l);
                    Handles.DrawWireDisc(p + Vector3.up * .03f, Vector3.up, .25f, l);
                    break;
                case Sorte.Boite:
                    Handles.DrawWireCube(p + Vector3.up * .4f, Vector3.one * .8f);
                    break;
                case Sorte.Gobelin:
                    Handles.DrawAAPolyLine(l, Forme(p + Vector3.up * .03f, 4, .6f, avant));
                    Handles.DrawAAPolyLine(l, p + Vector3.up * .05f, p + Vector3.up * .05f + avant * 1.2f);
                    break;
                default:
                    Handles.DrawAAPolyLine(l, p + Vector3.left * 2, p + Vector3.right * 2);
                    Handles.DrawAAPolyLine(l, p + Vector3.back * 2, p + Vector3.forward * 2);
                    break;
            }
            if (etiquette == null || fondEtiquette == null)
            {
                // un fond sombre : l'étiquette se lit aussi sur le territoire de sa propre couleur
                fondEtiquette = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
                fondEtiquette.SetPixel(0, 0, new Color(0, 0, 0, .65f)); fondEtiquette.Apply();
                etiquette = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter, padding = new RectOffset(4, 4, 1, 1) };
                etiquette.normal.background = fondEtiquette;
            }
            etiquette.normal.textColor = c;
            Handles.Label(p + Vector3.up * 1.4f, sorte == Sorte.Joueur ? $"J{joueur}" : Nom, etiquette);
        }

        private static GUIStyle etiquette;
        private static Texture2D fondEtiquette;

        /// <summary>La forme du joueur sur le sol : 1 rond, 2 triangle, 3 carré, 4 losange (pointe vers l'avant).</summary>
        private static Vector3[] Forme(Vector3 centre, int joueur, float r, Vector3 avant)
        {
            int cotes = joueur == 1 ? 24 : joueur == 2 ? 3 : 4;
            float depart = joueur == 3 ? 45 : 0;
            var droite = Vector3.Cross(Vector3.up, avant);
            var pts = new Vector3[cotes + 1];
            for (int i = 0; i <= cotes; i++)
            {
                float a = (depart + 360f * i / cotes) * Mathf.Deg2Rad;
                pts[i] = centre + (avant * Mathf.Cos(a) + droite * Mathf.Sin(a)) * r;
            }
            return pts;
        }
#endif
    }
}
