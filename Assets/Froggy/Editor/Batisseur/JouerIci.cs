using System.Globalization;
using System.Linq;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEngine;

namespace Froggy.Outils
{
    /// <summary>
    /// « Jouer ici » (F5, menu Froggy ▸ Jouer ici) : lancer la scène ouverte avec Froggy posé au point visé dans la vue
    /// Scène (sous la souris, sinon au centre de la vue), tourné comme la vue Scène. F5 en jeu arrête ; « Où j'étais en
    /// jeu » mène ensuite la vue Scène là où l'on s'est arrêté. Repris de Lovecraft (2026-10-01).
    /// Le joueur est le premier objet qui porte DeplacementWow ou TemplatePlayerController (sinon le tag Player) : quand
    /// le réseau (FishNet) fera apparaître les joueurs, ce point d'arrivée devra passer par l'apparition.
    /// Le panneau « Froggy : jouer » de la vue Scène réunit tout (menu ⋮ de la vue Scène ▸ Overlays).
    /// </summary>
    [InitializeOnLoad]
    public static class JouerIci
    {
        private const string SsPoint = "Froggy.jouerIci.point", SsOu = "Froggy.jouerIci.ou";
        private static readonly string[] Controleurs = { "DeplacementWow", "TemplatePlayerController" };

        // le rayon de la souris dans la vue Scène, tenu à jour quand elle y bouge (HandleUtility ne sert que pendant son dessin)
        private static Ray rayonSouris; private static SceneView vueSouris;

        static JouerIci()
        {
            SceneView.duringSceneGui += Suivre;
            EditorApplication.playModeStateChanged += Etat;
        }

        private static void Suivre(SceneView sv)
        {
            var e = Event.current;
            if (e.type != EventType.MouseMove && e.type != EventType.MouseDrag) return;
            rayonSouris = HandleUtility.GUIPointToWorldRay(e.mousePosition); vueSouris = sv;
        }

        [MenuItem("Froggy/Jouer ici _F5", priority = 1)]
        public static void Menu()
        {
            if (EditorApplication.isPlaying) { EditorApplication.isPlaying = false; return; }
            var sv = EditorWindow.mouseOverWindow as SceneView;
            if (sv != null && sv == vueSouris && Point(rayonSouris, out var p)) Lancer(p, sv.rotation.eulerAngles.y);
            else LancerAuCentre();
        }

        /// <summary>Lance au centre de la vue Scène (le sol sous le point qu'elle regarde).</summary>
        public static void LancerAuCentre()
        {
            var sv = SceneView.lastActiveSceneView;
            if (sv == null) { EditorApplication.isPlaying = true; return; }
            var ray = new Ray(sv.camera.transform.position, sv.camera.transform.forward);
            if (!Point(ray, out var p)) p = BatisseurArene.SolSous(sv.pivot, 20, (System.Collections.Generic.HashSet<Collider>)null, out var sol) ? sol.point : sv.pivot;
            Lancer(p, sv.rotation.eulerAngles.y);
        }

        private static bool Point(Ray ray, out Vector3 p)
        {
            Physics.SyncTransforms();
            var hits = Physics.RaycastAll(ray, 2000, ~0, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var h in hits)
                if (h.collider.attachedRigidbody == null || h.collider.attachedRigidbody.isKinematic) { p = h.point; return true; }
            p = default; return false;
        }

        /// <summary>Lance la partie, Froggy posé en p (un peu au-dessus), tourné de cap degrés.</summary>
        public static void Lancer(Vector3 p, float cap)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var ci = CultureInfo.InvariantCulture;
            SessionState.SetString(SsPoint, string.Join("|", p.x.ToString(ci), p.y.ToString(ci), p.z.ToString(ci), cap.ToString(ci)));
            EditorApplication.isPlaying = true;
        }

        private static GameObject Joueur()
        {
            var c = Object.FindObjectsByType<MonoBehaviour>().FirstOrDefault(m => m != null && Controleurs.Contains(m.GetType().Name));
            if (c != null) return c.gameObject;
            try { return GameObject.FindWithTag("Player"); } catch (UnityException) { return null; }
        }

        private static void Etat(PlayModeStateChange s)
        {
            if (s == PlayModeStateChange.EnteredPlayMode)
            {
                SessionState.SetBool(BatisseurArene.SsAJoue, true);
                var v = SessionState.GetString(SsPoint, "").Split('|');
                SessionState.EraseString(SsPoint);
                if (v.Length < 4) return;
                var ci = CultureInfo.InvariantCulture;
                var p = new Vector3(float.Parse(v[0], ci), float.Parse(v[1], ci), float.Parse(v[2], ci)) + Vector3.up * .1f;
                var rot = Quaternion.Euler(0, float.Parse(v[3], ci), 0);
                var j = Joueur();
                if (j == null) { Debug.LogWarning("Jouer ici : pas de joueur dans la scène (DeplacementWow, TemplatePlayerController ou le tag Player)."); return; }
                j.transform.SetPositionAndRotation(p, rot);
                if (j.TryGetComponent<Rigidbody>(out var rb))
                {
                    rb.position = p; rb.rotation = rot;
                    if (!rb.isKinematic) { rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }
                }
                Physics.SyncTransforms();
            }
            else if (s == PlayModeStateChange.ExitingPlayMode)
            {
                var j = Joueur(); if (j == null) return;
                var ci = CultureInfo.InvariantCulture; var p = j.transform.position;
                SessionState.SetString(SsOu, string.Join("|", p.x.ToString(ci), p.y.ToString(ci), p.z.ToString(ci)));
            }
        }

        /// <summary>Là où Froggy s'est arrêté à la dernière partie (null : aucune depuis l'ouverture de Unity).</summary>
        public static Vector3? OuJetais
        {
            get
            {
                var v = SessionState.GetString(SsOu, "").Split('|'); var ci = CultureInfo.InvariantCulture;
                if (v.Length < 3) return null;
                return new Vector3(float.Parse(v[0], ci), float.Parse(v[1], ci), float.Parse(v[2], ci));
            }
        }

        public static void AllerOuJetais()
        {
            var ou = OuJetais; var sv = SceneView.lastActiveSceneView;
            if (ou == null || sv == null) return;
            sv.LookAt(ou.Value, sv.rotation, Mathf.Min(sv.size, 10));
        }
    }

    /// <summary>Le panneau « Froggy : jouer » de la vue Scène.</summary>
    [Overlay(typeof(SceneView), "froggy-jouer", "Froggy : jouer", true)]
    internal class PanneauJouer : IMGUIOverlay
    {
        public override void OnGUI()
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(170)))
            {
                if (EditorApplication.isPlaying)
                {
                    if (GUILayout.Button(new GUIContent("■  Arrêter (F5)", "Retour à l'éditeur"), GUILayout.Height(24))) EditorApplication.isPlaying = false;
                    return;
                }
                if (GUILayout.Button(new GUIContent("▶  Jouer ici (F5)", "Lance la scène, Froggy au centre de la vue Scène ; F5 la souris sur la vue Scène : sous la souris"), GUILayout.Height(24)))
                    JouerIci.LancerAuCentre();
                if (JouerIci.OuJetais != null && GUILayout.Button(new GUIContent("Où j'étais en jeu", "Mène la vue Scène là où tu t'es arrêté")))
                    JouerIci.AllerOuJetais();
            }
        }
    }
}
