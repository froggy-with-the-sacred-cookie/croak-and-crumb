using System.Collections.Generic;
using System.Linq;
using Froggy.Arene;
using UnityEditor;
using UnityEngine;
using Symetrie = Froggy.Outils.CourbeArene.Symetrie;

namespace Froggy.Outils
{
    /// <summary>
    /// La symétrie de l'arène : une arène de Capture The Flag à 4 joueurs est souvent la même vue de chaque base. Choisie en
    /// haut de la fenêtre, elle s'applique à Poser, Tracer, Tampons et aux repères de Jeu (l'apparition du joueur 1 pose
    /// celles des joueurs 2 à 4) ; « Symétriser la sélection » complète après coup ce qui est déjà posé. Les images gardent
    /// la rotation du préfabriqué : seule la pose est en miroir, jamais l'échelle (pas d'objet retourné, ni d'éclairage faux).
    /// Le centre est le repère « Centre de l'arène » s'il y en a un, sinon l'origine de la scène.
    /// </summary>
    public partial class BatisseurArene
    {
        public static Symetrie SymetrieChoisie
        {
            get => (Symetrie)EditorPrefs.GetInt(Pref + "symetrie", 0);
            set => EditorPrefs.SetInt(Pref + "symetrie", (int)value);
        }

        /// <summary>Le centre de la symétrie : le repère Centre de l'arène, sinon l'origine.</summary>
        public static Vector3 CentreArene()
        {
            var c = Object.FindObjectsByType<RepereArene>().FirstOrDefault(r => r.sorte == RepereArene.Sorte.Centre);
            return c != null ? c.transform.position : Vector3.zero;
        }

        /// <summary>Toutes les images d'une pose (la pose elle-même en premier) selon la symétrie choisie.</summary>
        internal static List<(Vector3 pos, Quaternion rot, int rang)> Images(Vector3 pos, Quaternion rot)
        {
            var s = SymetrieChoisie; var centre = CentreArene();
            var l = new List<(Vector3, Quaternion, int)>();
            for (int k = 0; k < CourbeArene.NombreImages(s); k++)
            {
                var (p, r) = CourbeArene.Image(pos, rot, centre, s, k);
                l.Add((p, r, k));
            }
            return l;
        }

        /// <summary>Les images d'un plan (tracé, tampon) : chaque pièce à chaque rang.</summary>
        internal static List<(GameObject modele, Vector3 pos, Quaternion rot, Vector3 echelle)> ImagesDuPlan(List<(GameObject modele, Vector3 pos, Quaternion rot, Vector3 echelle)> plan)
        {
            var s = SymetrieChoisie; if (s == Symetrie.Aucune) return plan;
            var centre = CentreArene(); var l = new List<(GameObject, Vector3, Quaternion, Vector3)>();
            for (int k = 0; k < CourbeArene.NombreImages(s); k++)
                foreach (var (m, p, r, e) in plan)
                {
                    var (pi, ri) = CourbeArene.Image(p, r, centre, s, k);
                    l.Add((m, pi, ri, e));
                }
            return l;
        }

        private void SymetrieGUI()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                SymetrieChoisie = (Symetrie)EditorGUILayout.EnumPopup(new GUIContent("Symétrie",
                    "Une arène juste se ressemble de chaque base : ce que tu poses d'un côté se pose aussi de l'autre (aperçu en violet). " +
                    "Le centre est le repère « Centre » de l'étape Jeu, sinon l'origine de la scène"), SymetrieChoisie);
                var choisis = Racines(Selection.gameObjects).Concat(Selection.gameObjects.Where(g => g != null && g.GetComponent<RepereArene>() != null)).Distinct().ToList();
                using (new EditorGUI.DisabledScope(SymetrieChoisie == Symetrie.Aucune || choisis.Count == 0))
                    if (GUILayout.Button(new GUIContent("Symétriser la sélection", "Pose les images des objets sélectionnés (un Ctrl+Z)"), GUILayout.Width(150)))
                        Notifier($"{SymetriserSelection(choisis)} copie(s) posée(s) par symétrie · Ctrl+Z pour annuler");
            }
        }

        /// <summary>Pose les images (rangs 1 et suivants) de ces objets, dans le même groupe. Un Ctrl+Z.</summary>
        internal static int SymetriserSelection(List<GameObject> objets)
        {
            var s = SymetrieChoisie; if (s == Symetrie.Aucune || objets.Count == 0) return 0;
            Undo.IncrementCurrentGroup(); int g = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Symétriser la sélection");
            var centre = CentreArene(); int n = 0;
            foreach (var o in objets)
            {
                var modele = PrefabUtility.GetCorrespondingObjectFromSource(o);
                var r0 = modele != null ? modele.transform.localRotation : Quaternion.identity;
                var pose = o.transform.rotation * Quaternion.Inverse(r0);
                for (int k = 1; k < CourbeArene.NombreImages(s); k++)
                {
                    var (p, r) = CourbeArene.Image(o.transform.position, pose, centre, s, k);
                    GameObject copie;
                    if (modele != null)
                    {
                        copie = (GameObject)PrefabUtility.InstantiatePrefab(modele, o.transform.parent);
                        copie.transform.localScale = o.transform.localScale;
                    }
                    else copie = Object.Instantiate(o, o.transform.parent);
                    copie.transform.SetPositionAndRotation(p, r * r0);
                    Undo.RegisterCreatedObjectUndo(copie, "Symétriser");
                    if (copie.TryGetComponent<RepereArene>(out var rep) && rep.sorte == RepereArene.Sorte.Joueur)
                    {
                        rep.joueur = CourbeArene.JoueurImage(rep.joueur, s, k);
                        copie.name = "J" + rep.joueur;
                    }
                    n++;
                }
            }
            Undo.CollapseUndoOperations(g);
            return n;
        }

        /// <summary>Les axes de la symétrie dans la vue Scène, tant qu'un outil est pris.</summary>
        private static void DessinerSymetrie()
        {
            var s = SymetrieChoisie; if (s == Symetrie.Aucune) return;
            var c = CentreArene() + Vector3.up * .05f; const float l = 40;
            using (new Handles.DrawingScope(new Color(.75f, .55f, 1f, .8f)))
            {
                if (s == Symetrie.MiroirX || s == Symetrie.MiroirXZ) Handles.DrawDottedLine(c + Vector3.back * l, c + Vector3.forward * l, 6);
                if (s == Symetrie.MiroirZ || s == Symetrie.MiroirXZ) Handles.DrawDottedLine(c + Vector3.left * l, c + Vector3.right * l, 6);
                if (s == Symetrie.Rotation2 || s == Symetrie.Rotation4)
                {
                    Handles.DrawWireDisc(c, Vector3.up, 1.5f, 2);
                    for (int k = 0; k < CourbeArene.NombreImages(s); k++)
                        Handles.DrawDottedLine(c, c + Quaternion.Euler(0, 360f * k / CourbeArene.NombreImages(s), 0) * Vector3.forward * l, 6);
                }
            }
        }
    }
}
