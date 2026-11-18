using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Froggy.Outils
{
    /// <summary>
    /// L'outil Poser : la palette des préfabriqués (recherche, catégories = dossiers, vignettes), un clic dans la vue Scène
    /// pose le préfabriqué tenu sur ce qu'on vise. Glisser une vignette dans la vue Scène la pose à la façon d'Unity.
    /// </summary>
    public partial class BatisseurArene
    {
        private class Article { public GameObject modele; public string categorie, chemin; }

        private List<Article> palette;
        private GameObject enMain;
        private float lacetEnMain, tailleEnMain = 1;
        private string recherche = "";
        private Vector3 visePos, viseNormale; private bool viseOk, viseOccupe;
        private GameObject viseRetrait; private Vector2 sourisRetrait;

        private static string Dossiers
        {
            get => EditorPrefs.GetString(Pref + "dossiers", "Assets/Froggy/Prefabs");
            set => EditorPrefs.SetString(Pref + "dossiers", value);
        }
        /// <summary>La catégorie de la palette, retenue par étape : ① Terrain s'ouvre sur les blocs gris, ④ Décor sur tout.</summary>
        private static string Categorie
        {
            get => EditorPrefs.GetString(Pref + "categorie." + EtapeChoisie, EtapeChoisie == Etape.Terrain ? "Gris" : "");
            set => EditorPrefs.SetString(Pref + "categorie." + EtapeChoisie, value);
        }
        private static bool LacetHasard { get => EditorPrefs.GetBool(Pref + "lacetHasard", false); set => EditorPrefs.SetBool(Pref + "lacetHasard", value); }
        private static bool SuivrePente { get => EditorPrefs.GetBool(Pref + "suivrePente", false); set => EditorPrefs.SetBool(Pref + "suivrePente", value); }
        private static float TailleMin { get => EditorPrefs.GetFloat(Pref + "tailleMin", 1); set => EditorPrefs.SetFloat(Pref + "tailleMin", value); }
        private static float TailleMax { get => EditorPrefs.GetFloat(Pref + "tailleMax", 1); set => EditorPrefs.SetFloat(Pref + "tailleMax", value); }
        private static int TailleVignette { get => EditorPrefs.GetInt(Pref + "vignette", 64); set => EditorPrefs.SetInt(Pref + "vignette", value); }
        private static bool Aimanter { get => EditorPrefs.GetBool(Pref + "aimanter", false); set => EditorPrefs.SetBool(Pref + "aimanter", value); }
        private static float PasGrille { get => EditorPrefs.GetFloat(Pref + "pasGrille", .5f); set => EditorPrefs.SetFloat(Pref + "pasGrille", value); }
        private static readonly float[] PasGrilles = { .25f, .5f, 1, 2 };

        /// <summary>Aimante le point visé à la grille (au sol sous le point aimanté).</summary>
        private static void Aimantee(ref Vector3 p)
        {
            float g = PasGrille;
            var q = new Vector3(Mathf.Round(p.x / g) * g, p.y, Mathf.Round(p.z / g) * g);
            if (SolSous(q, 1, (HashSet<Collider>)null, out var sol) && Mathf.Abs(sol.point.y - p.y) < 2) q.y = sol.point.y;
            p = q;
        }

        private List<Article> Palette()
        {
            if (palette != null) return palette;
            palette = new List<Article>();
            var dossiers = Dossiers.Split(';').Select(d => d.Trim().TrimEnd('/')).Where(AssetDatabase.IsValidFolder).ToArray();
            if (dossiers.Length == 0) return palette;
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", dossiers))
            {
                var chemin = AssetDatabase.GUIDToAssetPath(guid);
                var modele = AssetDatabase.LoadAssetAtPath<GameObject>(chemin);
                if (modele == null) continue;
                var base_ = dossiers.Where(d => chemin.StartsWith(d + "/")).OrderByDescending(d => d.Length).First();
                var rel = Path.GetDirectoryName(chemin.Substring(base_.Length + 1))?.Replace('\\', '/');
                palette.Add(new Article { modele = modele, chemin = chemin, categorie = string.IsNullOrEmpty(rel) ? Path.GetFileName(base_) : rel.Split('/')[0] });
            }
            palette.Sort((a, b) => string.Compare(a.modele.name, b.modele.name, System.StringComparison.OrdinalIgnoreCase));
            return palette;
        }

        private void PoserGUI()
        {
            if (EtapeChoisie == Etape.Terrain)
            {
                if (!KitGris.Existe)
                {
                    EditorGUILayout.HelpBox("Commence en blocs gris : des formes simples (sol, marches, plates-formes, murs, nénuphars, eau), " +
                        "avec un damier de 1 m pour juger les distances. Le vrai décor les remplacera ensuite.", MessageType.Info);
                    if (GUILayout.Button("Créer le kit de blocs gris", GrosBouton)) { KitGris.Creer(); palette = null; Categorie = "Gris"; }
                }
                using (new EditorGUILayout.HorizontalScope())
                {
                    Aimanter = EditorGUILayout.ToggleLeft(new GUIContent("Aimanter à la grille", "La pose tombe sur une grille (au sol sous le point) : les blocs s'alignent"), Aimanter, GUILayout.Width(150));
                    using (new EditorGUI.DisabledScope(!Aimanter))
                    {
                        int ig = EditorGUILayout.Popup(Mathf.Max(0, System.Array.IndexOf(PasGrilles, PasGrille)), PasGrilles.Select(g => g + " m").ToArray());
                        PasGrille = PasGrilles[ig];
                    }
                }
            }
            recherche = EditorGUILayout.TextField(new GUIContent("Chercher", "« lanterne », « pierre », « nenuphar »…"), recherche);

            var articles = Palette();
            var categories = articles.Select(a => a.categorie).Distinct().OrderBy(c => c).ToList();
            categories.Insert(0, "Tout");
            int ic = Mathf.Max(0, categories.IndexOf(Categorie));
            int nc = GUILayout.SelectionGrid(ic, categories.ToArray(), Mathf.Clamp((int)(position.width / 110), 1, 6), EditorStyles.miniButton);
            if (nc != ic) Categorie = nc == 0 ? "" : categories[nc];

            var vus = articles.Where(a => (Categorie == "" || a.categorie == Categorie)
                && (recherche == "" || a.modele.name.IndexOf(recherche, System.StringComparison.OrdinalIgnoreCase) >= 0)).ToList();
            if (articles.Count == 0) EditorGUILayout.HelpBox("Aucun objet à poser dans les dossiers de la palette (Plus de réglages ▸ Dossiers).", MessageType.Info);
            else if (vus.Count == 0) EditorGUILayout.HelpBox($"Rien ne s'appelle « {recherche} » ici. Essaie un autre mot, ou la catégorie « Tout ».", MessageType.Info);
            Vignettes(vus);

            EditorGUILayout.Space(4);
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("En main", enMain != null ? enMain.name : "rien : clique une vignette, puis clique dans la vue Scène", EditorStyles.boldLabel);
                if (enMain != null && GUILayout.Button("Lâcher", GUILayout.Width(60))) enMain = null;
            }
            RetouchesGUI();
            if (!Plus("poser")) return;
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUI.BeginChangeCheck();
                var d = EditorGUILayout.DelayedTextField(new GUIContent("Dossiers", "Les dossiers de préfabriqués de la palette, séparés par « ; »"), Dossiers);
                if (EditorGUI.EndChangeCheck()) { Dossiers = d; palette = null; }
            }
            LacetHasard = EditorGUILayout.ToggleLeft(new GUIContent("Orientation au hasard", "Chaque pose tire une autre orientation (A / E la changent encore)"), LacetHasard);
            SuivrePente = EditorGUILayout.ToggleLeft(new GUIContent("Pencher comme le sol", "Sinon l'objet reste droit (arbres, lanternes)"), SuivrePente);
            float min = TailleMin, max = TailleMax;
            EditorGUILayout.MinMaxSlider(new GUIContent($"Taille au hasard ({min:0.00} à {max:0.00})"), ref min, ref max, .3f, 2.5f);
            TailleMin = min; TailleMax = max;
            if (EtapeChoisie != Etape.Terrain)
            using (new EditorGUILayout.HorizontalScope())
            {
                Aimanter = EditorGUILayout.ToggleLeft(new GUIContent("Aimanter à la grille", "La pose tombe sur une grille (au sol sous le point)"), Aimanter, GUILayout.Width(150));
                using (new EditorGUI.DisabledScope(!Aimanter))
                {
                    int ig = EditorGUILayout.Popup(Mathf.Max(0, System.Array.IndexOf(PasGrilles, PasGrille)), PasGrilles.Select(g => g + " m").ToArray());
                    PasGrille = PasGrilles[ig];
                }
            }
            TailleVignette = EditorGUILayout.IntSlider("Taille des vignettes", TailleVignette, 40, 128);
            EditorGUILayout.HelpBox("Le cadre vert dit que la place est libre, le rouge qu'un autre objet est déjà là (on peut poser quand même). " +
                "Pour qu'un objet (le sol, l'eau) ne soit jamais visé par Maj+clic, rends-le non sélectionnable (la main dans la Hiérarchie).", MessageType.None);
        }

        private void Vignettes(List<Article> vus)
        {
            int t = TailleVignette;
            int colonnes = Mathf.Max(1, (int)((position.width - 24) / (t + 6)));
            bool charge = false;
            for (int i = 0; i < vus.Count; i += colonnes)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    for (int j = i; j < Mathf.Min(i + colonnes, vus.Count); j++)
                    {
                        var a = vus[j];
                        var image = AssetPreview.GetAssetPreview(a.modele);
                        if (image == null) { charge = true; image = AssetPreview.GetMiniThumbnail(a.modele); }
                        var r = GUILayoutUtility.GetRect(t, t + 14, GUILayout.Width(t), GUILayout.Height(t + 14));
                        Vignette(r, a, image);
                    }
                    GUILayout.FlexibleSpace();
                }
            }
            // les vignettes arrivent une à une ; un préfabriqué sans rendu n'en aura jamais
            if (charge && AssetPreview.IsLoadingAssetPreviews()) Repaint();
        }

        private void Vignette(Rect r, Article a, Texture image)
        {
            var e = Event.current;
            var cadre = new Rect(r.x, r.y, r.width, r.width);
            bool tenu = enMain == a.modele;
            if (e.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(cadre, tenu ? new Color(.35f, .7f, .35f) : new Color(0, 0, 0, .15f));
                if (image != null) GUI.DrawTexture(new Rect(cadre.x + 2, cadre.y + 2, cadre.width - 4, cadre.height - 4), image, ScaleMode.ScaleToFit);
                GUI.Label(new Rect(r.x, r.y + r.width, r.width, 14), Abrege(a.modele.name.Replace('_', ' '), r.width), EditorStyles.centeredGreyMiniLabel);
            }
            GUI.Label(r, new GUIContent("", $"{a.modele.name}\n{a.chemin}\nClic : le prendre en main · glisser : le poser dans la vue Scène"));
            if (!r.Contains(e.mousePosition)) return;
            if (e.type == EventType.MouseDrag && e.button == 0)
            {
                DragAndDrop.PrepareStartDrag();
                DragAndDrop.objectReferences = new Object[] { a.modele };
                DragAndDrop.StartDrag(a.modele.name);
                e.Use();
            }
            else if (e.type == EventType.MouseUp && e.button == 0)
            {
                enMain = a.modele; TirerAuHasard();
                Prendre(Outil.Poser); ClavierALaScene(); e.Use();
            }
        }

        /// <summary>Le nom coupé à la fin (« Bordure Bambo… ») plutôt qu'au milieu, pour tenir sous la vignette.</summary>
        private static string Abrege(string nom, float largeur)
        {
            var style = EditorStyles.centeredGreyMiniLabel;
            if (style.CalcSize(new GUIContent(nom)).x <= largeur) return nom;
            for (int n = nom.Length - 1; n > 1; n--)
            {
                var court = nom.Substring(0, n).TrimEnd() + "…";
                if (style.CalcSize(new GUIContent(court)).x <= largeur) return court;
            }
            return nom.Substring(0, 1) + "…";
        }

        /// <summary>T, ou après chaque pose : une autre orientation (si au hasard) et une autre taille.</summary>
        private void TirerAuHasard()
        {
            if (LacetHasard) lacetEnMain = Random.Range(0f, 360f);
            tailleEnMain = Random.Range(TailleMin, Mathf.Max(TailleMin, TailleMax));
        }

        private Matrix4x4 PlaceEnMain()
        {
            var rot = (SuivrePente ? Quaternion.FromToRotation(Vector3.up, viseNormale) : Quaternion.identity) * Quaternion.Euler(0, lacetEnMain, 0);
            return Fantome.Place(visePos, rot, Vector3.one * tailleEnMain);
        }

        private void PoserScene(SceneView sv, Event e)
        {
            bool retrait = e.shift;
            if (e.type == EventType.MouseMove || e.type == EventType.MouseDrag || e.type == EventType.MouseDown)
            {
                // la sélection d'Unity sous la souris redessine la scène : seulement si la souris a vraiment bougé (ou au clic)
                if (!retrait) viseRetrait = null;
                else if (e.type == EventType.MouseDown || viseRetrait == null || Vector2.Distance(e.mousePosition, sourisRetrait) > 3)
                {
                    viseRetrait = Retirable(HandleUtility.PickGameObject(e.mousePosition, false)); sourisRetrait = e.mousePosition;
                }
                viseOk = !retrait && enMain != null && ViserSurface(e.mousePosition, out visePos, out viseNormale);
                if (viseOk && Aimanter) Aimantee(ref visePos);
                if (viseOk) viseOccupe = Occupe(e.mousePosition);
            }
            if (e.type == EventType.KeyDown && (e.keyCode == KeyCode.LeftShift || e.keyCode == KeyCode.RightShift)) sv.Repaint();

            if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
            {
                if (retrait)
                {
                    if (viseRetrait != null)
                    {
                        Undo.IncrementCurrentGroup(); Undo.SetCurrentGroupName("Retirer " + viseRetrait.name);
                        Notifier($"« {viseRetrait.name} » retiré · Ctrl+Z pour le remettre");
                        Undo.DestroyObjectImmediate(viseRetrait); viseRetrait = null;
                    }
                    e.Use(); return;
                }
                if (enMain != null && viseOk)
                {
                    Undo.IncrementCurrentGroup(); Undo.SetCurrentGroupName("Poser " + enMain.name);
                    var categorie = Palette().FirstOrDefault(a => a.modele == enMain)?.categorie ?? "Posés";
                    var m = PlaceEnMain();
                    var images = Images(visePos, m.rotation);
                    foreach (var (p, r, k) in images)
                        Instancier(enMain, p, r, Vector3.one * tailleEnMain, Groupe(categorie));
                    Notifier($"« {enMain.name} » posé{(images.Count > 1 ? $" ×{images.Count}" : "")} · Ctrl+Z pour annuler");
                    TirerAuHasard();
                    e.Use();
                }
            }

            if (e.type != EventType.Repaint) return;
            if (retrait && viseRetrait != null)
            {
                var b = Boite(viseRetrait);
                using (new Handles.DrawingScope(new Color(1, .25f, .2f))) Handles.DrawWireCube(b.center, b.size);
                Handles.Label(b.center + Vector3.up * b.extents.y, "Retirer " + viseRetrait.name, EditorStyles.whiteBoldLabel);
            }
            else if (enMain != null && viseOk)
            {
                var f = Fantome.De(enMain); var m = PlaceEnMain();
                foreach (var (p, r, k) in Images(visePos, m.rotation))
                {
                    var mi = Fantome.Place(p, r, Vector3.one * tailleEnMain);
                    f.Dessiner(mi, sv.camera);
                    f.Cadre(mi, k > 0 ? new Color(.75f, .55f, 1f) : viseOccupe ? new Color(1, .3f, .25f) : new Color(.3f, 1, .4f));
                }
            }
        }

        /// <summary>Un autre objet est-il déjà dans le cadre du fantôme ? (le sol visé ne compte pas)</summary>
        private bool Occupe(Vector2 souris)
        {
            var f = Fantome.De(enMain); if (f == null) return false;
            GameObject sol = ViserCollision(souris, out var hit) ? ObjetDe(hit.collider.gameObject) : null;
            var m = PlaceEnMain();
            var centre = m.MultiplyPoint3x4(f.Bornes.center);
            var demi = f.Bornes.extents * tailleEnMain * .85f;
            demi.y = Mathf.Max(.01f, demi.y - .1f);
            foreach (var c in Physics.OverlapBox(centre + Vector3.up * .1f, demi, m.rotation, ~0, QueryTriggerInteraction.Ignore))
                if (ObjetDe(c.gameObject) != sol && !Mobile(c)) return true;
            return false;
        }

        // ---------- Retoucher la sélection ----------

        private void RetouchesGUI()
        {
            if (!SousTiroir("retouches", "Retoucher la sélection")) return;
            var choisis = Racines(Selection.gameObjects);
            EditorGUILayout.LabelField(choisis.Count == 0 ? "Sélectionne des objets dans la scène (Échap pour poser l'outil, puis clic) : tu pourras les remplacer ou les poser sur le sol."
                : $"{choisis.Count} objet(s) sélectionné(s)", EditorStyles.wordWrappedMiniLabel);
            using (new EditorGUI.DisabledScope(choisis.Count == 0 || enMain == null))
                if (GUILayout.Button(new GUIContent(enMain != null ? $"Remplacer par « {enMain.name} »" : "Remplacer par l'objet en main",
                    "Même place, même orientation, même taille relative : un bloc gris devient le vrai décor")))
                {
                    Selection.objects = Remplacer(choisis, enMain).ToArray();
                    Notifier($"{choisis.Count} objet(s) remplacé(s) par « {enMain.name} » · Ctrl+Z pour annuler");
                }
            using (new EditorGUI.DisabledScope(choisis.Count == 0))
                if (GUILayout.Button(new GUIContent("Poser sur le sol", "Chaque objet descend (ou monte) jusqu'à toucher ce qui est dessous")))
                    Notifier($"{PoserSurLeSol(choisis)} objet(s) posé(s) sur le sol · Ctrl+Z pour annuler");
        }

        /// <summary>
        /// Remplace ces objets par le préfabriqué donné : même parent, même place dans la hiérarchie, même position, même pose
        /// (par-dessus la rotation de chaque préfabriqué) et même taille relative. Un Ctrl+Z.
        /// </summary>
        internal static List<GameObject> Remplacer(List<GameObject> objets, GameObject modele)
        {
            var l = new List<GameObject>();
            Undo.IncrementCurrentGroup(); int g = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Remplacer par " + modele.name);
            foreach (var o in objets)
            {
                var ancien = PrefabUtility.GetCorrespondingObjectFromSource(o);
                var r0 = ancien != null ? ancien.transform.localRotation : Quaternion.identity;
                var s0 = ancien != null ? ancien.transform.localScale : Vector3.one;
                var facteur = new Vector3(s0.x != 0 ? o.transform.localScale.x / s0.x : 1, s0.y != 0 ? o.transform.localScale.y / s0.y : 1, s0.z != 0 ? o.transform.localScale.z / s0.z : 1);
                var neuf = Instancier(modele, o.transform.position, o.transform.rotation * Quaternion.Inverse(r0), facteur, o.transform.parent);
                neuf.transform.SetSiblingIndex(o.transform.GetSiblingIndex());
                Undo.DestroyObjectImmediate(o);
                l.Add(neuf);
            }
            Undo.CollapseUndoOperations(g);
            return l;
        }

        /// <summary>Chaque objet se pose sur ce qui est sous lui (le bas de sa boîte contre le sol). Un Ctrl+Z.</summary>
        internal static int PoserSurLeSol(List<GameObject> objets)
        {
            Undo.IncrementCurrentGroup(); int g = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Poser sur le sol");
            Physics.SyncTransforms(); int n = 0;
            foreach (var o in objets)
            {
                var eux = new HashSet<Collider>(o.GetComponentsInChildren<Collider>());
                var b = Boite(o);
                if (!SolSous(new Vector3(b.center.x, b.min.y, b.center.z), b.size.y + .05f, eux, out var sol)) continue;
                Undo.RecordObject(o.transform, "Poser sur le sol");
                o.transform.position += Vector3.up * (sol.point.y - b.min.y);
                Physics.SyncTransforms(); n++;
            }
            Undo.CollapseUndoOperations(g);
            return n;
        }
    }
}
