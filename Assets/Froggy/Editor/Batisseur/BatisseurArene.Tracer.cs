using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Froggy.Outils
{
    /// <summary>
    /// L'outil Tracer : quelques points posés dans la vue Scène, une courbe lisse passe par eux, et un modèle de tracé
    /// (Assets/Froggy/Batisseur/Traces.asset) y aligne ses pièces : une clôture, une muraille, une allée de lanternes, un
    /// gué de pierres, un chemin de nénuphars. Entrée applique (un Ctrl+Z) ; la courbe reste, et Entrée après une retouche
    /// refait le tracé à la place du précédent. Échap efface la courbe pour en commencer une autre.
    /// </summary>
    public partial class BatisseurArene
    {
        [SerializeField] private List<Vector3> points = new List<Vector3>();
        private TracesArene traces; private SerializedObject tracesSO;
        private int glisse = -1, graineTrace;
        private GameObject derniereApplication;
        private List<(GameObject modele, Vector3 pos, Quaternion rot, Vector3 echelle)> planTrace;
        private string rapportTrace = "";

        private static int TraceChoisi { get => EditorPrefs.GetInt(Pref + "trace", 0); set => EditorPrefs.SetInt(Pref + "trace", value); }

        public static TracesArene Traces()
        {
            var chemin = Dossier + "/Traces.asset";
            var t = AssetDatabase.LoadAssetAtPath<TracesArene>(chemin);
            if (t != null) return t;
            if (!AssetDatabase.IsValidFolder(Dossier)) AssetDatabase.CreateFolder("Assets/Froggy", "Batisseur");
            t = CreateInstance<TracesArene>();
            TracesDeDepart(t);
            AssetDatabase.CreateAsset(t, chemin);
            AssetDatabase.SaveAssets();
            return t;
        }

        /// <summary>Les modèles de tracé de départ, d'après le kit du jardin japonais (des propositions, à régler en jeu).</summary>
        private static void TracesDeDepart(TracesArene t)
        {
            TracesArene.Piece P(string nom, float poids = 1) => new TracesArene.Piece { modele = AssetDatabase.LoadAssetAtPath<GameObject>(Jardin + nom + ".prefab"), poids = poids };
            void Ajouter(TracesArene.Trace tr)
            {
                tr.pieces.RemoveAll(p => p.modele == null);
                if (tr.pieces.Count == 0) return;
                // l'axe de la longueur : le plus long des deux côtés de la première pièce
                var f = Fantome.De(tr.pieces[0].modele);
                if (f != null) tr.axe = f.Bornes.size.x >= f.Bornes.size.z ? TracesArene.Axe.X : TracesArene.Axe.Z;
                t.traces.Add(tr);
            }
            Ajouter(new TracesArene.Trace { nom = "Clôture de bambou", pieces = { P("Cloture_Bambou") }, pasDeLaPiece = true });
            Ajouter(new TracesArene.Trace { nom = "Muraille de Chine", pieces = { P("Chine_Mur_1"), P("Chine_Mur_2", .5f) }, pasDeLaPiece = true });
            Ajouter(new TracesArene.Trace { nom = "Haie de karikomi", pieces = { P("Karikomi_Petit"), P("Karikomi_Moyen", .6f) }, pas = 1.1f,
                orientation = TracesArene.Orientation.Hasard, decalage = .1f, tailleMin = .9f, tailleMax = 1.1f });
            Ajouter(new TracesArene.Trace { nom = "Allée de lanternes", pieces = { P("Lanterne_Kasuga") }, disposition = TracesArene.Disposition.Allee,
                pas = 5, ecartRangees = 3.5f, orientation = TracesArene.Orientation.Face });
            Ajouter(new TracesArene.Trace { nom = "Gué de pierres", note = "Trace la courbe en cliquant sur l'eau : les pierres affleurent à sa hauteur, pour sauter de l'une à l'autre",
                pieces = { P("PierreGue_1"), P("PierreGue_2"), P("PierreGue_3") }, pas = 1.3f, orientation = TracesArene.Orientation.Hasard, decalage = .25f,
                hauteur = TracesArene.Hauteur.Courbe });
            Ajouter(new TracesArene.Trace { nom = "Chemin de nénuphars", note = "Trace la courbe en cliquant sur l'eau : les nénuphars restent à sa hauteur",
                pieces = { P("Nenuphar_Grand"), P("Nenuphar_Moyen"), P("Nenuphar_Petit", .5f) }, pas = 2.4f,
                orientation = TracesArene.Orientation.Hasard, decalage = .4f, hauteur = TracesArene.Hauteur.Courbe });
            Ajouter(new TracesArene.Trace { nom = "Pont yatsuhashi", note = "Des planches bout à bout, en zigzag si la courbe tourne",
                pieces = { P("Yatsuhashi_Planche") }, pasDeLaPiece = true, hauteur = TracesArene.Hauteur.Courbe });
        }

        private TracesArene.Trace Trace()
        {
            if (traces == null) traces = Traces();
            if (traces.traces.Count == 0) return null;
            return traces.traces[Mathf.Clamp(TraceChoisi, 0, traces.traces.Count - 1)];
        }

        private void TracerGUI()
        {
            if (traces == null) traces = Traces();
            if (tracesSO == null || tracesSO.targetObject != traces) tracesSO = new SerializedObject(traces);
            tracesSO.Update();
            var noms = traces.traces.Select((t, i) => $"{i + 1}. {t.nom}").ToArray();
            if (noms.Length == 0) EditorGUILayout.HelpBox("Aucun modèle de tracé : crée-en un dans « Plus de réglages ».", MessageType.Info);
            else
            {
                EditorGUI.BeginChangeCheck();
                TraceChoisi = EditorGUILayout.Popup(new GUIContent("Ce que devient la courbe", "Les pièces posées le long de la courbe"), Mathf.Clamp(TraceChoisi, 0, noms.Length - 1), noms);
                if (EditorGUI.EndChangeCheck()) planTrace = null;
                var t = Trace();
                if (!string.IsNullOrEmpty(t.note)) EditorGUILayout.LabelField(t.note, EditorStyles.wordWrappedMiniLabel);
            }
            EditorGUILayout.Space(2);
            var ligne = CourbeArene.Echantillonner(points);
            EditorGUILayout.LabelField(points.Count == 0 ? "Clique dans la vue Scène pour poser les points de la courbe ; l'aperçu suit chaque point."
                : $"Courbe : {points.Count} point(s), {CourbeArene.Longueur(ligne):0.0} m" + (planTrace != null ? $", {planTrace.Count} pièce(s)" : ""), EditorStyles.wordWrappedMiniLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(points.Count < 2))
                    if (GUILayout.Button(new GUIContent(derniereApplication != null ? "Refaire le tracé (Entrée)" : "Tracer (Entrée)",
                        "Pose les pièces ; la courbe reste, et le refaire remplace le précédent"))) Appliquer();
                if (GUILayout.Button(new GUIContent("Autre tirage", "Un autre hasard (écarts, orientations, choix des pièces)"))) { graineTrace++; planTrace = null; SceneView.RepaintAll(); }
                using (new EditorGUI.DisabledScope(points.Count == 0))
                    if (GUILayout.Button(new GUIContent("Nouvelle courbe", "Efface la courbe (ce qui a été tracé reste)"))) EffacerCourbe();
            }
            if (rapportTrace != "") EditorGUILayout.HelpBox(rapportTrace, MessageType.None);
            if (!Plus("tracer")) return;

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("Les modèles de tracé", EditorStyles.miniBoldLabel);
                if (GUILayout.Button("Nouveau", EditorStyles.miniButtonLeft, GUILayout.Width(62)))
                {
                    Undo.RecordObject(traces, "Nouveau tracé");
                    traces.traces.Add(new TracesArene.Trace()); TraceChoisi = traces.traces.Count - 1; EditorUtility.SetDirty(traces);
                }
                using (new EditorGUI.DisabledScope(noms.Length == 0))
                {
                    if (GUILayout.Button("Copier", EditorStyles.miniButtonMid, GUILayout.Width(52)))
                    {
                        Undo.RecordObject(traces, "Copier le tracé");
                        var copie = JsonUtility.FromJson<TracesArene.Trace>(JsonUtility.ToJson(Trace()));
                        copie.nom += " (copie)"; traces.traces.Add(copie); TraceChoisi = traces.traces.Count - 1; EditorUtility.SetDirty(traces);
                    }
                    if (GUILayout.Button("Suppr.", EditorStyles.miniButtonRight, GUILayout.Width(48))
                        && EditorUtility.DisplayDialog("Supprimer le modèle de tracé", $"Supprimer « {Trace().nom} » ? Ce qui a été tracé reste.", "Supprimer", "Annuler"))
                    {
                        Undo.RecordObject(traces, "Supprimer le tracé");
                        traces.traces.RemoveAt(Mathf.Clamp(TraceChoisi, 0, traces.traces.Count - 1)); EditorUtility.SetDirty(traces);
                        tracesSO.Update();
                    }
                }
            }
            if (traces.traces.Count > 0)
            {
                var p = tracesSO.FindProperty("traces").GetArrayElementAtIndex(Mathf.Clamp(TraceChoisi, 0, traces.traces.Count - 1));
                var it = p.Copy(); var fin = p.GetEndProperty(); bool entrer = true;
                while (it.NextVisible(entrer) && !SerializedProperty.EqualContents(it, fin))
                {
                    EditorGUILayout.PropertyField(it, true); entrer = false;
                }
                if (tracesSO.ApplyModifiedProperties()) { AssetDatabase.SaveAssetIfDirty(traces); planTrace = null; }
            }
        }

        /// <summary>Échap : efface la courbe (vrai), ou rien à effacer (faux : l'outil se lâche).</summary>
        private bool EffacerCourbe()
        {
            if (points.Count == 0) return false;
            points.Clear(); derniereApplication = null; planTrace = null; glisse = -1;
            Repaint(); SceneView.RepaintAll();
            return true;
        }

        /// <summary>Retour arrière : retire le dernier point.</summary>
        public void RetirerDernierPoint()
        {
            if (points.Count == 0) return;
            points.RemoveAt(points.Count - 1); planTrace = null; Repaint(); SceneView.RepaintAll();
        }

        private void TracerScene(SceneView sv, Event e)
        {
            var ligne = CourbeArene.Echantillonner(points);
            if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
            {
                int proche = PointProche(e.mousePosition);
                if (proche >= 0 && e.shift) { points.RemoveAt(proche); planTrace = null; }
                else if (proche >= 0) glisse = proche;
                else if (!e.shift && ViserSurface(e.mousePosition, out var pos, out _))
                {
                    int segment = SegmentProche(e.mousePosition);
                    if (segment >= 0) { points.Insert(segment + 1, pos); glisse = segment + 1; }
                    else points.Add(pos);
                    planTrace = null;
                }
                e.Use(); Repaint();
            }
            else if (glisse >= 0 && e.type == EventType.MouseDrag)
            {
                if (glisse < points.Count && ViserSurface(e.mousePosition, out var pos, out _)) { points[glisse] = pos; planTrace = null; }
                e.Use();
            }
            else if (glisse >= 0 && (e.type == EventType.MouseUp || e.rawType == EventType.MouseUp)) { glisse = -1; e.Use(); Repaint(); }
            else if (e.type == EventType.KeyDown && (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)) { Appliquer(); e.Use(); }

            if (e.type != EventType.Repaint) return;
            if (ligne.Count > 1)
                using (new Handles.DrawingScope(new Color(1, .85f, .3f))) Handles.DrawAAPolyLine(4, ligne.ToArray());
            for (int i = 0; i < points.Count; i++)
            {
                float t = HandleUtility.GetHandleSize(points[i]) * .07f;
                using (new Handles.DrawingScope(i == glisse ? Color.white : new Color(1, .6f, .2f)))
                    Handles.DotHandleCap(0, points[i], Quaternion.identity, t, EventType.Repaint);
            }
            if (points.Count >= 2)
            {
                if (planTrace == null) planTrace = PlanEtImages();
                foreach (var (modele, pos, rot, echelle) in planTrace.Take(1200))
                    Fantome.De(modele).Dessiner(Fantome.Place(pos, rot, echelle), sv.camera);
            }
        }

        private int PointProche(Vector2 souris)
        {
            for (int i = points.Count - 1; i >= 0; i--)
                if (Vector2.Distance(HandleUtility.WorldToGUIPoint(points[i]), souris) < 10) return i;
            return -1;
        }

        /// <summary>Le segment de la courbe sous la souris (entre le point i et i + 1), ou -1.</summary>
        private int SegmentProche(Vector2 souris)
        {
            var ligne = CourbeArene.Echantillonner(points);
            for (int i = 0; i < points.Count - 1; i++)
            {
                var morceau = ligne.Skip(i * 16).Take(17).ToArray();
                if (morceau.Length > 1 && HandleUtility.DistanceToPolyLine(morceau) < 8) return i;
            }
            return -1;
        }

        /// <summary>Le plan du tracé et ses images par la symétrie de l'arène.</summary>
        private List<(GameObject modele, Vector3 pos, Quaternion rot, Vector3 echelle)> PlanEtImages()
            => ImagesDuPlan(Plan(Trace(), points).Select(x => (x.modele, x.pos, x.rot, Vector3.one * x.taille)).ToList());

        /// <summary>Les pièces que le tracé poserait sur cette courbe (sans rien poser).</summary>
        internal List<(GameObject modele, Vector3 pos, Quaternion rot, float taille)> Plan(TracesArene.Trace t, IList<Vector3> pts)
        {
            var plan = new List<(GameObject, Vector3, Quaternion, float)>();
            if (t == null || pts.Count < 2) return plan;
            var pieces = t.pieces.Where(p => p.modele != null).ToList();
            if (pieces.Count == 0) return plan;
            var poids = pieces.Select(p => p.poids).ToList();
            var rng = new System.Random(graineTrace * 7919 + 17);
            float Hasard() => (float)rng.NextDouble();
            float tailleMoy = (t.tailleMin + t.tailleMax) / 2;
            float pas = t.pasDeLaPiece ? Mathf.Max(.1f, Fantome.Longueur(pieces[0].modele, t.axe) * tailleMoy) : t.pas;
            var ligne = CourbeArene.Echantillonner(pts);
            var rangees = t.disposition == TracesArene.Disposition.Allee ? new[] { -t.ecartRangees / 2, t.ecartRangees / 2 } : new[] { 0f };
            Physics.SyncTransforms();
            // l'aperçu ne se pose pas sur le tracé qu'il va remplacer
            var ancien = derniereApplication != null ? new HashSet<Collider>(derniereApplication.GetComponentsInChildren<Collider>()) : new HashSet<Collider>();
            var sols = SolsSous(pts);
            foreach (var cote in rangees)
                foreach (var (jalon, dir) in CourbeArene.Jalons(ligne, pas, t.pasDeLaPiece ? pas / 2 : 0))
                {
                    var avant = new Vector3(dir.x, 0, dir.z); if (avant.sqrMagnitude < 1e-6f) avant = Vector3.forward; avant.Normalize();
                    var droite = new Vector3(avant.z, 0, -avant.x);
                    var pos = jalon + droite * (cote + (Hasard() * 2 - 1) * t.decalage);
                    var piece = pieces[Mathf.Max(0, CourbeArene.Tirer(poids, Hasard()))];
                    float lacet = Mathf.Atan2(avant.x, avant.z) * Mathf.Rad2Deg;
                    switch (t.orientation)
                    {
                        // la longueur de la pièce suit la courbe
                        case TracesArene.Orientation.LeLong: if (t.axe == TracesArene.Axe.X) lacet -= 90; break;
                        // l'avant de la pièce regarde la courbe (une allée : chaque rangée se tourne vers le milieu)
                        case TracesArene.Orientation.Face: lacet += cote > 0 ? -90 : 90; break;
                        default: lacet = Hasard() * 360; break;
                    }
                    lacet += (Hasard() * 2 - 1) * t.ecartLacet;
                    float taille = Mathf.Lerp(t.tailleMin, Mathf.Max(t.tailleMin, t.tailleMax), Hasard());
                    if (t.hauteur == TracesArene.Hauteur.Sol && SolSous(pos, 3, c => !ancien.Contains(c) && sols.Contains(ObjetDe(c.gameObject)), out var sol)) pos.y = sol.point.y;
                    plan.Add((piece.modele, pos, Quaternion.Euler(0, lacet, 0), taille));
                }
            return plan;
        }

        private void Appliquer()
        {
            var t = Trace();
            if (t == null || points.Count < 2) return;
            Undo.IncrementCurrentGroup(); int g = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Tracer : " + t.nom);
            int remplaces = 0, degages = 0;
            if (derniereApplication != null) { remplaces = derniereApplication.transform.childCount; Undo.DestroyObjectImmediate(derniereApplication); }
            var ligne = CourbeArene.Echantillonner(points);
            if (t.degager > 0)
            {
                // ce que le semis a posé sur le passage
                if (semis == null) semis = Semis();
                var semes = new HashSet<GameObject>(semis.melanges.SelectMany(m => m.elements).Where(x => x.modele != null).Select(x => x.modele));
                foreach (var go in Instances(semes))
                    if (CourbeArene.DistanceAPlat(go.transform.position, ligne) <= t.degager / 2) { Undo.DestroyObjectImmediate(go); degages++; }
            }
            var plan = PlanEtImages();
            var groupe = NouveauGroupe(Groupe("Tracés"), t.nom);
            foreach (var (modele, pos, rot, echelle) in plan) Instancier(modele, pos, rot, echelle, groupe);
            derniereApplication = groupe.gameObject;
            Undo.CollapseUndoOperations(g);
            rapportTrace = $"{t.nom} : {plan.Count} pièce(s) dans « {groupe.name} »" + (remplaces > 0 ? $", à la place du tracé précédent" : "")
                + (degages > 0 ? $", {degages} objet(s) semé(s) retiré(s)" : "") + ". Ctrl+Z annule.";
            Notifier($"« {t.nom} » : {plan.Count} pièce(s) posée(s){(remplaces > 0 ? ", à la place du tracé précédent" : "")} · Ctrl+Z pour annuler");
            planTrace = null; Repaint();
        }
    }
}
