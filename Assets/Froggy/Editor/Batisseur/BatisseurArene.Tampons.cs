using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Froggy.Outils
{
    /// <summary>
    /// L'outil Tampons : la bibliothèque des coins réussis (Assets/Froggy/Batisseur/Tampons, un fichier par tampon), à
    /// reposer d'un clic, tournés (A / E), en miroir (M), un peu variés (T : une autre variante) ; enregistrer la sélection
    /// comme tampon ; varier la sélection pour casser l'effet copier-coller.
    /// </summary>
    public partial class BatisseurArene
    {
        private const string DossierTampons = Dossier + "/Tampons";

        private List<TamponArene> bibliotheque;
        private TamponArene tamponEnMain;
        private float lacetTampon; private bool miroirTampon; private int graineTampon;
        // l'aperçu ne se recalcule que si le tampon, sa place ou ses réglages changent (6 ms pour 60 pièces, 2026-10-29)
        private List<(GameObject modele, Vector3 pos, Quaternion rot, Vector3 echelle)> planTampon; private string clePlanTampon;
        private Vector3 origineTampon; private bool origineOk;
        private string nomTampon = "", noteTampon = "", rapportTampon = "";

        private static float Variation { get => EditorPrefs.GetFloat(Pref + "variation", .3f); set => EditorPrefs.SetFloat(Pref + "variation", value); }
        private static bool CollerAuSol { get => EditorPrefs.GetBool(Pref + "collerAuSol", true); set => EditorPrefs.SetBool(Pref + "collerAuSol", value); }
        private static float ForceVarier { get => EditorPrefs.GetFloat(Pref + "forceVarier", .5f); set => EditorPrefs.SetFloat(Pref + "forceVarier", value); }
        private static bool VarierOrientation { get => EditorPrefs.GetBool(Pref + "varierOrientation", true); set => EditorPrefs.SetBool(Pref + "varierOrientation", value); }
        private static bool VarierTaille { get => EditorPrefs.GetBool(Pref + "varierTaille", true); set => EditorPrefs.SetBool(Pref + "varierTaille", value); }

        private List<TamponArene> Bibliotheque()
        {
            if (bibliotheque != null) return bibliotheque;
            bibliotheque = !AssetDatabase.IsValidFolder(DossierTampons) ? new List<TamponArene>() :
                AssetDatabase.FindAssets("t:TamponArene", new[] { DossierTampons })
                    .Select(g => AssetDatabase.LoadAssetAtPath<TamponArene>(AssetDatabase.GUIDToAssetPath(g)))
                    .Where(t => t != null).OrderBy(t => t.name).ToList();
            return bibliotheque;
        }

        private void TamponsGUI()
        {
            var biblio = Bibliotheque();
            EditorGUILayout.LabelField("Un tampon garde un coin réussi (une berge, un îlot de nénuphars, un abri) pour le reposer ailleurs, tourné et un peu varié.", EditorStyles.wordWrappedMiniLabel);
            if (biblio.Count == 0) EditorGUILayout.HelpBox("Pas encore de tampon. Sélectionne dans la scène les objets d'un coin réussi, puis « Enregistrer un tampon » ci-dessous.", MessageType.Info);
            int colonnes = Mathf.Max(1, (int)((position.width - 24) / 150));
            for (int i = 0; i < biblio.Count; i += colonnes)
                using (new EditorGUILayout.HorizontalScope())
                    for (int j = i; j < Mathf.Min(i + colonnes, biblio.Count); j++)
                    {
                        var t = biblio[j]; bool tenu = tamponEnMain == t;
                        var avant = GUI.backgroundColor;
                        if (tenu) GUI.backgroundColor = new Color(.55f, .85f, .55f);
                        var contenu = new GUIContent($"{t.name}\n{t.pieces.Count} pièce(s)", string.IsNullOrEmpty(t.note) ? "Clic : le prendre en main · clic droit : plus" : t.note);
                        var r = GUILayoutUtility.GetRect(contenu, GUI.skin.button, GUILayout.Height(36), GUILayout.MinWidth(100));
                        if (Event.current.type == EventType.ContextClick && r.Contains(Event.current.mousePosition))
                        {
                            var menu = new GenericMenu();
                            menu.AddItem(new GUIContent("Afficher dans le Project"), false, () => { EditorGUIUtility.PingObject(t); Selection.activeObject = t; });
                            menu.AddItem(new GUIContent("Mettre à la corbeille (ce qui a été posé reste)"), false, () =>
                            {
                                if (tamponEnMain == t) tamponEnMain = null;
                                AssetDatabase.MoveAssetToTrash(AssetDatabase.GetAssetPath(t)); bibliotheque = null; Repaint();
                            });
                            menu.ShowAsContext(); Event.current.Use();
                        }
                        if (GUI.Button(r, contenu))
                        {
                            tamponEnMain = tenu ? null : t;
                            if (tamponEnMain != null) { Prendre(Outil.Tampons); ClavierALaScene(); }
                        }
                        GUI.backgroundColor = avant;
                    }

            EditorGUILayout.Space(4);
            Variation = EditorGUILayout.Slider(new GUIContent("Variation", "0 : posé à l'identique ; plus haut, chaque pièce (sauf les fixes) tourne, grandit ou se décale un peu"), Variation, 0, 1);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("◀ A")) TournerEnMain(-1);
                if (GUILayout.Button("E ▶")) TournerEnMain(1);
                if (GUILayout.Button(miroirTampon ? "Miroir (M) ✓" : "Miroir (M)")) MiroirEnMain();
                if (GUILayout.Button("Autre variante (T)")) { graineTampon++; SceneView.RepaintAll(); }
            }

            EditorGUILayout.Space(6);
            if (SousTiroir("enregistrer", "Enregistrer un tampon"))
            {
                var pieces = Racines(Selection.gameObjects);
                EditorGUILayout.LabelField($"La sélection : {pieces.Count} objet(s) posé(s) depuis un préfabriqué", EditorStyles.miniLabel);
                nomTampon = EditorGUILayout.TextField("Nom", nomTampon);
                noteTampon = EditorGUILayout.TextField(new GUIContent("Note", "À quoi il sert : s'affiche au survol"), noteTampon);
                using (new EditorGUI.DisabledScope(pieces.Count == 0 || string.IsNullOrWhiteSpace(nomTampon)))
                    if (GUILayout.Button("Enregistrer la sélection")) Enregistrer(null, pieces);
                if (tamponEnMain != null)
                    using (new EditorGUI.DisabledScope(pieces.Count == 0))
                        if (GUILayout.Button($"Remplacer « {tamponEnMain.name} » par la sélection")
                            && EditorUtility.DisplayDialog("Remplacer le tampon", $"Remplacer les pièces de « {tamponEnMain.name} » par les {pieces.Count} objet(s) sélectionné(s) ? Ce qui a déjà été posé reste.", "Remplacer", "Annuler"))
                            Enregistrer(tamponEnMain, pieces);
                EditorGUILayout.HelpBox("Sélectionne les objets d'un coin réussi (ou leur groupe, comme Tampons ▸ …) : seuls les objets posés depuis un préfabriqué entrent dans le tampon, avec leur place, leur rotation et leur taille.", MessageType.None);
            }
            bool plus = Plus("tampons");
            if (plus)
                CollerAuSol = EditorGUILayout.ToggleLeft(new GUIContent("Suivre le relief", "Chaque pièce se pose sur le sol sous elle, à la hauteur qu'elle avait au-dessus du sien"), CollerAuSol);
            if (plus && SousTiroir("varier", "Varier la sélection"))
            {
                ForceVarier = EditorGUILayout.Slider("Force", ForceVarier, 0, 1);
                VarierOrientation = EditorGUILayout.ToggleLeft("Brasser l'orientation", VarierOrientation);
                VarierTaille = EditorGUILayout.ToggleLeft("Brasser la taille", VarierTaille);
                var pieces = Racines(Selection.gameObjects);
                using (new EditorGUI.DisabledScope(pieces.Count == 0))
                    if (GUILayout.Button($"Varier ({pieces.Count} objet(s))"))
                    {
                        VarierObjets(pieces, ForceVarier, VarierOrientation, VarierTaille);
                        Notifier($"{pieces.Count} objet(s) variés · Ctrl+Z pour annuler");
                    }
                EditorGUILayout.HelpBox("Les objets restent à leur place : ils tournent et changent de taille un peu, pour casser l'effet copier-coller d'une rangée. Chaque clic tire autre chose ; Ctrl+Z annule.", MessageType.None);
            }
            if (rapportTampon != "") EditorGUILayout.HelpBox(rapportTampon, MessageType.None);
        }

        /// <summary>Les objets posés depuis un préfabriqué parmi ceux-là et sous eux (le plus extérieur seulement).</summary>
        internal static List<GameObject> Racines(IEnumerable<GameObject> choisis)
        {
            var l = new List<GameObject>();
            foreach (var go in choisis.Where(g => g != null && g.scene.IsValid()))
            {
                var r = PrefabUtility.GetOutermostPrefabInstanceRoot(go);
                if (r != null) { if (!l.Contains(r)) l.Add(r); continue; }
                foreach (var t in go.GetComponentsInChildren<Transform>(true))
                    if (PrefabUtility.IsOutermostPrefabInstanceRoot(t.gameObject) && !l.Contains(t.gameObject)) l.Add(t.gameObject);
            }
            return l;
        }

        /// <summary>Enregistre ces objets comme tampon (nouveau si dans est nul), autour du centre de leur boîte, au sol.</summary>
        internal TamponArene Enregistrer(TamponArene dans, List<GameObject> objets)
        {
            var t = TamponDe(objets);
            if (dans == null)
            {
                if (!AssetDatabase.IsValidFolder(Dossier)) AssetDatabase.CreateFolder("Assets/Froggy", "Batisseur");
                if (!AssetDatabase.IsValidFolder(DossierTampons)) AssetDatabase.CreateFolder(Dossier, "Tampons");
                var nom = string.Join("_", nomTampon.Trim().Split(System.IO.Path.GetInvalidFileNameChars()));
                t.note = noteTampon;
                AssetDatabase.CreateAsset(t, AssetDatabase.GenerateUniqueAssetPath($"{DossierTampons}/{nom}.asset"));
                rapportTampon = $"Tampon « {t.name} » enregistré : {t.pieces.Count} pièce(s).";
                nomTampon = noteTampon = "";
            }
            else
            {
                Undo.RecordObject(dans, "Remplacer le tampon");
                dans.pieces = t.pieces; EditorUtility.SetDirty(dans); DestroyImmediate(t); t = dans;
                rapportTampon = $"Tampon « {t.name} » remplacé : {t.pieces.Count} pièce(s).";
            }
            AssetDatabase.SaveAssetIfDirty(t);
            bibliotheque = null; tamponEnMain = t;
            return t;
        }

        /// <summary>Le tampon de ces objets (pas encore enregistré) : les pièces autour du centre de leur boîte, au niveau du sol sous ce centre.</summary>
        internal static TamponArene TamponDe(List<GameObject> objets)
        {
            var t = CreateInstance<TamponArene>();
            if (objets.Count == 0) return t;
            Physics.SyncTransforms();
            var eux = new HashSet<Collider>(objets.SelectMany(o => o.GetComponentsInChildren<Collider>()));
            var b = Boite(objets[0]); foreach (var o in objets.Skip(1)) b.Encapsulate(Boite(o));
            var centre = new Vector3(b.center.x, b.min.y, b.center.z);
            GameObject leSol = null;
            if (SolSous(new Vector3(b.center.x, b.max.y, b.center.z), 1, eux, out var solCentre)) { centre.y = solCentre.point.y; leSol = ObjetDe(solCentre.collider.gameObject); }
            foreach (var o in objets)
            {
                var modele = PrefabUtility.GetCorrespondingObjectFromSource(o);
                if (modele == null) continue;
                var p = o.transform.position;
                // la hauteur au-dessus du sol du tampon, pas de la couronne d'un arbre voisin (2026-10-29 : des plantes enterrées de 1,9 m)
                float hauteur = SolSous(p, .5f, c => !eux.Contains(c) && (leSol == null || ObjetDe(c.gameObject) == leSol), out var sol)
                    ? p.y - sol.point.y : p.y - centre.y;
                var ob = Boite(o);
                t.pieces.Add(new TamponArene.Piece
                {
                    modele = modele, position = p - centre,
                    rotation = o.transform.rotation * Quaternion.Inverse(modele.transform.localRotation),
                    echelle = Diviser(o.transform.lossyScale, modele.transform.localScale), hauteurSol = hauteur,
                    fixe = Mathf.Max(ob.size.x, ob.size.z) > 4,
                });
            }
            return t;
        }

        private static Vector3 Diviser(Vector3 a, Vector3 b) =>
            new Vector3(b.x != 0 ? a.x / b.x : 1, b.y != 0 ? a.y / b.y : 1, b.z != 0 ? a.z / b.z : 1);

        /// <summary>Ce que le tampon poserait en origine (sans rien poser) : miroir, rotation, variation tirée par la graine, relief.</summary>
        internal static List<(GameObject modele, Vector3 pos, Quaternion rot, Vector3 echelle)> Preparer(TamponArene t, Vector3 origine, float lacet, bool miroir, int graine, float variation, bool collerAuSol)
        {
            var plan = new List<(GameObject, Vector3, Quaternion, Vector3)>();
            if (t == null) return plan;
            if (collerAuSol) Physics.SyncTransforms();
            var sols = collerAuSol ? SolsSous(new[] { origine }) : null;
            for (int i = 0; i < t.pieces.Count; i++)
            {
                var p = t.pieces[i]; if (p.modele == null) continue;
                var rng = new System.Random(graine * 7919 + i * 31 + 1);
                float H() => (float)rng.NextDouble() * 2 - 1;
                var local = p.position; var rot = p.rotation; var echelle = p.echelle;
                if (variation > 0 && !p.fixe)
                {
                    local += new Vector3(H(), 0, H()) * variation * .3f;
                    rot = Quaternion.Euler(0, H() * variation * 40, 0) * rot;
                    echelle *= 1 + H() * variation * .15f;
                }
                var (pos, r) = CourbeArene.Placer(local, rot, origine, lacet, miroir);
                if (collerAuSol && SolSous(new Vector3(pos.x, origine.y, pos.z), 3, c => sols.Contains(ObjetDe(c.gameObject)), out var sol)) pos.y = sol.point.y + p.hauteurSol;
                plan.Add((p.modele, pos, r, echelle));
            }
            return plan;
        }

        private void TamponsScene(SceneView sv, Event e)
        {
            if (tamponEnMain == null) return;
            if (e.type == EventType.MouseMove || e.type == EventType.MouseDrag || e.type == EventType.MouseDown)
                origineOk = ViserSurface(e.mousePosition, out origineTampon, out _);
            if (e.type == EventType.MouseDown && e.button == 0 && !e.alt && !e.shift && origineOk)
            {
                Undo.IncrementCurrentGroup(); int g = Undo.GetCurrentGroup();
                Undo.SetCurrentGroupName("Tampon : " + tamponEnMain.name);
                var plan = PlanTampon();
                var groupe = NouveauGroupe(Groupe("Tampons"), tamponEnMain.name);
                groupe.position = origineTampon;
                foreach (var (modele, pos, rot, echelle) in plan) Instancier(modele, pos, rot, echelle, groupe);
                Undo.CollapseUndoOperations(g);
                rapportTampon = $"« {tamponEnMain.name} » posé dans « {groupe.name} » ({plan.Count} pièce(s)). Ctrl+Z annule.";
                Notifier($"« {tamponEnMain.name} » posé ({plan.Count} pièce(s)) · Ctrl+Z pour annuler");
                graineTampon++; planTampon = null;   // la pose suivante tire une autre variante
                e.Use(); Repaint();
            }
            if (e.type == EventType.Repaint && origineOk)
            {
                foreach (var (modele, pos, rot, echelle) in PlanTampon())
                    Fantome.De(modele).Dessiner(Fantome.Place(pos, rot, echelle), sv.camera);
                using (new Handles.DrawingScope(new Color(.3f, 1, .4f)))
                {
                    Handles.DrawWireDisc(origineTampon, Vector3.up, .3f);
                    Handles.DrawLine(origineTampon, origineTampon + Quaternion.Euler(0, lacetTampon, 0) * Vector3.forward * 1.5f);
                }
            }
        }

        /// <summary>Le plan du tampon tenu à l'endroit visé, et ses images par la symétrie ; gardé tant que rien ne change.</summary>
        private List<(GameObject modele, Vector3 pos, Quaternion rot, Vector3 echelle)> PlanTampon()
        {
            var cle = $"{tamponEnMain.GetHashCode()}|{origineTampon}|{lacetTampon}|{miroirTampon}|{graineTampon}|{Variation}|{CollerAuSol}|{SymetrieChoisie}|{CentreArene()}";
            if (planTampon == null || cle != clePlanTampon)
            {
                planTampon = ImagesDuPlan(Preparer(tamponEnMain, origineTampon, lacetTampon, miroirTampon, graineTampon, Variation, CollerAuSol));
                clePlanTampon = cle;
            }
            return planTampon;
        }

        /// <summary>Varier la sélection : chaque objet tourne et change de taille un peu, sans bouger de place. Un Ctrl+Z.</summary>
        internal static void VarierObjets(List<GameObject> objets, float force, bool orientation, bool taille)
        {
            Undo.IncrementCurrentGroup(); int g = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Varier la sélection");
            foreach (var o in objets)
            {
                Undo.RecordObject(o.transform, "Varier");
                if (orientation) o.transform.rotation = Quaternion.Euler(0, Random.Range(-1f, 1f) * force * 180, 0) * o.transform.rotation;
                if (taille) o.transform.localScale *= 1 + Random.Range(-1f, 1f) * force * .25f;
            }
            Undo.CollapseUndoOperations(g);
        }

        /// <summary>A / E sans outil pris (si l'option est cochée) : les objets sélectionnés tournent sur eux-mêmes. Un Ctrl+Z.</summary>
        public static int TournerSelection(int sens)
        {
            var objets = Racines(Selection.gameObjects);
            if (objets.Count == 0) return 0;
            Undo.IncrementCurrentGroup(); Undo.SetCurrentGroupName(sens > 0 ? "Tourner à droite" : "Tourner à gauche");
            foreach (var o in objets)
            {
                Undo.RecordObject(o.transform, "Tourner");
                o.transform.rotation = Quaternion.Euler(0, sens * PasRotation, 0) * o.transform.rotation;
            }
            return objets.Count;
        }
    }
}
