using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Froggy.Outils
{
    /// <summary>
    /// Le tiroir Vérifier du Bâtisseur d'arène : ce qui cloche dans la scène ouverte, avec un bouton Voir par constat.
    /// Préfabriqué perdu, script manquant, matériau vide, objets posés deux fois au même endroit (un copier-coller oublié),
    /// objets qui flottent au-dessus du sol, joueur sans sol sous lui ; et le compte de ce qui coûte au rendu.
    /// Repris de « Vérifier la carte » de Lovecraft ; les règles propres au Capture The Flag (bases, cookies, apparitions
    /// à égale distance) viendront quand ces objets existeront.
    /// </summary>
    public static class VerificationArene
    {
        /// <summary>Un constat ; ceux de même genre sur des objets de même nom se regroupent à l'affichage (« Muraille_Mur_1 ×12 »).</summary>
        public class Constat { public string texte, genre, quoi; public GameObject objet; public bool grave; }

        public static List<Constat> Verifier(float seuilFlotte, string[] ignorer, out string bilan)
        {
            var l = new List<Constat>();
            var scene = EditorSceneManager.GetActiveScene();
            // ce qui est allumé : les versions gardées de l'arène (éteintes) ne comptent pas
            var tous = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(false)).Select(t => t.gameObject).ToList();
            var poses = tous.Where(PrefabUtility.IsOutermostPrefabInstanceRoot).ToList();

            foreach (var go in tous)
            {
                if (PrefabUtility.IsOutermostPrefabInstanceRoot(go) && PrefabUtility.GetPrefabInstanceStatus(go) == PrefabInstanceStatus.MissingAsset)
                    l.Add(new Constat { texte = $"{go.name} : son préfabriqué n'existe plus", genre = "perdu", quoi = "son préfabriqué n'existe plus", objet = go, grave = true });
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go) > 0)
                    l.Add(new Constat { texte = $"{go.name} : un script manque", genre = "script", quoi = "un script manque", objet = go, grave = true });
                if (go.TryGetComponent<Renderer>(out var r) && !(r is ParticleSystemRenderer) && r.sharedMaterials.Any(m => m == null))
                    l.Add(new Constat { texte = $"{go.name} : un matériau vide (rose en jeu)", genre = "materiau", quoi = "un matériau vide (rose en jeu)", objet = go, grave = true });
            }

            var liste = poses.Select(p => (AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromSource(p)), p.transform.position, p.transform.rotation)).ToList();
            foreach (var (a, b) in CourbeArene.Doublons(liste))
                l.Add(new Constat { texte = $"{poses[b].name} : posé deux fois au même endroit (avec {poses[a].name})", genre = "doublon", quoi = "posé deux fois au même endroit", objet = poses[b] });

            Physics.SyncTransforms();
            foreach (var p in poses)
            {
                if (ignorer.Any(n => n.Length > 0 && p.name.IndexOf(n, System.StringComparison.OrdinalIgnoreCase) >= 0)) continue;
                if (p.GetComponentInChildren<Camera>(true) != null || p.GetComponentsInChildren<Rigidbody>(true).Any(rb => !rb.isKinematic)) continue;
                var boite = BatisseurArene.Boite(p);
                if (boite.size.x > 30 || boite.size.z > 30) continue;   // le sol lui-même, une muraille d'un seul tenant
                var eux = new HashSet<Collider>(p.GetComponentsInChildren<Collider>(true));
                // le rayon part de mi-hauteur : la base d'un arbre s'enfonce souvent un peu dans le sol
                var bas = new Vector3(boite.center.x, boite.min.y, boite.center.z);
                if (!BatisseurArene.SolSous(bas, boite.extents.y + .05f, eux, out var sol))
                    l.Add(new Constat { texte = $"{p.name} : rien dessous (au-dessus du vide ou d'une eau sans collision)", genre = "vide", quoi = "rien dessous (au-dessus du vide ou d'une eau sans collision)", objet = p });
                else if (boite.min.y - sol.point.y > seuilFlotte)
                    l.Add(new Constat { texte = $"{p.name} : flotte à {boite.min.y - sol.point.y:0.00} m du sol", genre = "flotte", quoi = "flotte au-dessus du sol", objet = p });
            }

            var joueur = Object.FindObjectsByType<MonoBehaviour>()
                .FirstOrDefault(m => m != null && (m.GetType().Name == "DeplacementWow" || m.GetType().Name == "TemplatePlayerController"));
            if (joueur == null) l.Add(new Constat { texte = "Pas de joueur dans la scène (Jouer ici ne pourra pas poser Froggy)" });
            else
            {
                var eux = new HashSet<Collider>(joueur.GetComponentsInChildren<Collider>(true));
                if (!BatisseurArene.SolSous(joueur.transform.position + Vector3.up * .5f, 0, eux, out _))
                    l.Add(new Constat { texte = "Le joueur n'a pas de sol sous lui", objet = joueur.gameObject, grave = true });
            }

            var rendus = tous.Select(g => g.GetComponent<Renderer>()).Where(r => r != null && r.enabled && r.gameObject.activeInHierarchy).ToList();
            long triangles = 0;
            foreach (var r in rendus)
            {
                Mesh m = r is SkinnedMeshRenderer s ? s.sharedMesh : r.TryGetComponent<MeshFilter>(out var mf) ? mf.sharedMesh : null;
                if (m == null) continue;
                // seul le premier niveau de détail compte (celui qu'on voit de près)
                var lod = r.GetComponentInParent<LODGroup>();
                if (lod != null && lod.GetLODs().Skip(1).Any(n => n.renderers.Contains(r))) continue;
                for (int i = 0; i < m.subMeshCount; i++) triangles += (long)m.GetIndexCount(i) / 3;
            }
            int lumieres = tous.Count(g => g.TryGetComponent<Light>(out var li) && li.enabled && g.activeInHierarchy && li.lightmapBakeType != LightmapBakeType.Baked);
            int collisions = tous.Count(g => g.TryGetComponent<Collider>(out var c) && c.enabled && g.activeInHierarchy);
            bilan = $"{poses.Count} objets posés, {rendus.Count} rendus, {triangles / 1000f:0.#} k triangles (de près), {collisions} collisions, {lumieres} lumière(s) en temps réel.";
            return l;
        }
    }

    public partial class BatisseurArene
    {
        private List<VerificationArene.Constat> constats; private string bilanVerif = "";

        private static float SeuilFlotte { get => EditorPrefs.GetFloat(Pref + "seuilFlotte", .3f); set => EditorPrefs.SetFloat(Pref + "seuilFlotte", value); }
        private static string Ignorer
        {
            get => EditorPrefs.GetString(Pref + "ignorer", "Nenuphar; Eau; Sakura; Petales; Montagnes");
            set => EditorPrefs.SetString(Pref + "ignorer", value);
        }

        private void VerifierGUI(bool tiroir = true)
        {
            if (tiroir && !SousTiroir("verifier", "Vérifier l'arène")) return;
            EditorGUILayout.LabelField("Cherche ce qui cloche : objets perdus ou posés deux fois, objets qui flottent, joueur sans sol, et ce qui coûte au rendu.", EditorStyles.wordWrappedMiniLabel);
            if (Plus("verifier"))
            {
                SeuilFlotte = EditorGUILayout.Slider(new GUIContent("Flotte au-delà de", "En mètres, entre le bas de l'objet et le sol"), SeuilFlotte, .05f, 2);
                Ignorer = EditorGUILayout.TextField(new GUIContent("Ne pas chercher s'ils flottent", "Les noms (ou des morceaux de nom) d'objets qui flottent exprès, séparés par « ; »"), Ignorer);
            }
            if (GUILayout.Button("Vérifier la scène ouverte")) VerifierMaintenant();
            if (constats == null) return;
            EditorGUILayout.HelpBox(bilanVerif, MessageType.None);
            if (constats.Count == 0) { EditorGUILayout.HelpBox("Rien à signaler.", MessageType.Info); return; }
            // les constats semblables (même genre, objets de même nom) en une ligne : 36 lignes de muraille noyaient le reste
            var groupes = constats.GroupBy(c => (c.genre ?? c.texte, c.genre == null ? "" : c.objet != null ? c.objet.name : ""))
                .Select(g => g.ToList()).OrderByDescending(g => g[0].grave).ThenByDescending(g => g.Count).ToList();
            EditorGUILayout.LabelField($"{constats.Count} constat(s), {groupes.Count} sorte(s)", EditorStyles.boldLabel);
            foreach (var g in groupes.Take(60))
                using (new EditorGUILayout.HorizontalScope())
                {
                    var c = g[0];
                    var texte = g.Count == 1 || c.genre == null ? c.texte : $"{c.objet.name} ×{g.Count} : {c.quoi}";
                    EditorGUILayout.LabelField((c.grave ? "✗ " : "• ") + texte, c.grave ? Rouge : EditorStyles.wordWrappedMiniLabel);
                    using (new EditorGUI.DisabledScope(c.objet == null))
                        if (GUILayout.Button(new GUIContent("Voir", g.Count > 1 ? "Sélectionne les objets du groupe" : "Sélectionne l'objet"), GUILayout.Width(44)))
                        {
                            Selection.objects = g.Where(x => x.objet != null).Select(x => (Object)x.objet).ToArray();
                            SceneView.lastActiveSceneView?.FrameSelected();
                        }
                }
            if (groupes.Count > 60) EditorGUILayout.LabelField($"… et {groupes.Count - 60} autres sortes", EditorStyles.miniLabel);
        }
    }
}
