using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Froggy.Arene;
using Froggy.Template;
using UnityEditor;
using UnityEngine;
using Sorte = Froggy.Arene.RepereArene.Sorte;

namespace Froggy.Outils
{
    /// <summary>
    /// L'outil Jeu : les repères de la partie (apparitions des joueurs, des cookies, boîtes à objets, départ du gobelin,
    /// centre de l'arène), rangés sous « Repères de jeu » ; et l'analyse des trajets : le temps de chaque apparition à
    /// chaque cookie et à chaque boîte, l'écart entre les joueurs, les territoires (qui arrive le premier où), les sauts
    /// serrés et ratés de peu. L'analyse suit les retouches (relancée une demi-seconde après chaque changement) : on bouge
    /// un nénuphar, le saut passe du rouge au vert.
    /// </summary>
    public partial class BatisseurArene
    {
        public enum Calque
        {
            [InspectorName("Rien")] Aucun,
            [InspectorName("Qui arrive le premier (territoires)")] Territoires,
            [InspectorName("Temps depuis un joueur")] Temps,
            [InspectorName("Les sauts seulement")] Sauts,
        }

        private const string RacineReperes = "Repères de jeu";
        private Sorte sorteRepere = Sorte.Joueur; private int joueurRepere = 1; private float lacetRepere;
        private RepereArene.Parties partiesRepere = RepereArene.Parties.Deux | RepereArene.Parties.Trois | RepereArene.Parties.Quatre;
        private Vector3 viseRepere; private bool viseRepereOk; private RepereArene repereVise;

        private GabaritFroggy gabarit; private SerializedObject gabaritSO;
        private GrilleTrajets grille; private Dictionary<RepereArene, float[]> tempsJoueurs = new Dictionary<RepereArene, float[]>();
        private List<RepereArene> reperes = new List<RepereArene>();
        private string bilanTrajets = ""; private double analysePerimee = -1;
        private Mesh calque; private Material calqueMat; private bool calqueAJour;

        private static int PartieA { get => EditorPrefs.GetInt(Pref + "partieA", 4); set => EditorPrefs.SetInt(Pref + "partieA", value); }
        private static GabaritFroggy.Meteo MeteoAnalyse { get => (GabaritFroggy.Meteo)EditorPrefs.GetInt(Pref + "meteo", 0); set => EditorPrefs.SetInt(Pref + "meteo", (int)value); }
        private static Calque CalqueChoisi { get => (Calque)EditorPrefs.GetInt(Pref + "calque", 1); set => EditorPrefs.SetInt(Pref + "calque", (int)value); }
        private static int JoueurCalque { get => EditorPrefs.GetInt(Pref + "joueurCalque", 1); set => EditorPrefs.SetInt(Pref + "joueurCalque", value); }
        private static bool SuivreRetouches { get => EditorPrefs.GetBool(Pref + "suivreRetouches", true); set => EditorPrefs.SetBool(Pref + "suivreRetouches", value); }

        public static GabaritFroggy Gabarit()
        {
            var chemin = Dossier + "/Gabarit.asset";
            var g = AssetDatabase.LoadAssetAtPath<GabaritFroggy>(chemin);
            if (g != null) return g;
            if (!AssetDatabase.IsValidFolder(Dossier)) AssetDatabase.CreateFolder("Assets/Froggy", "Batisseur");
            g = CreateInstance<GabaritFroggy>();
            ReprendreDuControleur(g);
            AssetDatabase.CreateAsset(g, chemin);
            AssetDatabase.SaveAssets();
            return g;
        }

        /// <summary>Relit la course, le sprint et le saut du contrôleur actuel (TemplateMovementSettings), en attendant celui de Froggy (P1-01).</summary>
        private static bool ReprendreDuControleur(GabaritFroggy g)
        {
            var guid = AssetDatabase.FindAssets("t:TemplateMovementSettings").FirstOrDefault();
            var r = guid == null ? null : AssetDatabase.LoadAssetAtPath<TemplateMovementSettings>(AssetDatabase.GUIDToAssetPath(guid));
            if (r == null) return false;
            g.vitesseCourse = r.moveSpeed; g.multiplicateurSprint = r.sprintMultiplier; g.hauteurSaut = r.jumpHeight;
            return true;
        }

        // ---------- Les repères ----------

        private static Transform GroupeReperes()
        {
            var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            var r = scene.GetRootGameObjects().FirstOrDefault(g => g.name == RacineReperes);
            if (r == null) { r = new GameObject(RacineReperes); Undo.RegisterCreatedObjectUndo(r, "Créer " + RacineReperes); }
            return r.transform;
        }

        private static List<RepereArene> Reperes() => UnityEngine.Object.FindObjectsByType<RepereArene>().Where(r => r.gameObject.activeInHierarchy).ToList();

        /// <summary>Le prochain joueur sans apparition (1 si tous en ont une).</summary>
        private static int JoueurManquant()
        {
            var pris = Reperes().Where(r => r.sorte == Sorte.Joueur).Select(r => r.joueur).ToList();
            for (int j = 1; j <= 4; j++) if (!pris.Contains(j)) return j;
            return 1;
        }

        private void JeuGUI()
        {
            reperes = Reperes();
            var joueurs = reperes.Where(r => r.sorte == Sorte.Joueur).ToList();
            SymetrieGUI();
            EditorGUILayout.LabelField("Quoi poser", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
                for (int j = 1; j <= 4; j++)
                    BoutonRepere(Sorte.Joueur, j, $"{"●▲■◆"[j - 1]} J{j}", $"L'apparition du joueur {j}" + (joueurs.Any(r => r.joueur == j) ? " (déjà posée)" : ""));
            using (new EditorGUILayout.HorizontalScope())
            {
                BoutonRepere(Sorte.Cookie, 1, "Cookies", "Un endroit où des cookies apparaissent");
                BoutonRepere(Sorte.Boite, 1, "Boîte", "Une boîte à objets (gel éclair, super croassement, camouflage caméléon)");
                BoutonRepere(Sorte.Gobelin, 1, "Gobelin", "D'où part le gobelin du cookie sacré, à la dernière minute");
                BoutonRepere(Sorte.Centre, 1, "Centre", "Le centre de l'arène : celui de la symétrie");
            }
            if (joueurs.Count == 0)
                EditorGUILayout.HelpBox("Commence par l'apparition du joueur 1 : clique « ● J1 », puis clique dans la vue Scène, là où il doit apparaître. " +
                    "Avec la symétrie « Quart de tour » ou « Deux miroirs », les joueurs 2 à 4 se posent en même temps, à égale distance du centre.", MessageType.Info);
            else
            {
                EditorGUILayout.Space(2);
                for (int n = 2; n <= 4; n++)
                {
                    var la = joueurs.Where(r => r.SertA(n)).Select(r => r.joueur).Distinct().OrderBy(j => j).ToList();
                    int cookies = reperes.Count(r => r.sorte == Sorte.Cookie && r.SertA(n)), boites = reperes.Count(r => r.sorte == Sorte.Boite && r.SertA(n));
                    bool ok = la.Count == n;
                    EditorGUILayout.LabelField($"Partie à {n} : {(ok ? "✓" : "✗")} {string.Join(" ", la.Select(j => "J" + j))}{(ok ? "" : $" (il en faut {n})")} · {cookies} cookie(s) · {boites} boîte(s)",
                        ok ? EditorStyles.miniLabel : Rouge);
                }
                if (!reperes.Any(r => r.sorte == Sorte.Gobelin)) EditorGUILayout.LabelField("Pas encore de départ du gobelin (le cookie sacré, à la dernière minute).", EditorStyles.wordWrappedMiniLabel);
            }
            if (Plus("jeu"))
                partiesRepere = (RepereArene.Parties)EditorGUILayout.EnumFlagsField(new GUIContent("Sert dans les parties",
                    "Une arène à 4 se joue aussi à 2 et à 3 : un repère peut ne servir qu'à certaines"), partiesRepere);
        }

        private void BoutonRepere(Sorte s, int j, string texte, string aide)
        {
            bool choisi = sorteRepere == s && (s != Sorte.Joueur || joueurRepere == j);
            var avant = GUI.backgroundColor;
            if (choisi) GUI.backgroundColor = RepereArene.Couleur(s, j);
            if (GUILayout.Button(new GUIContent(texte, aide), choisi ? BoutonMontre : BoutonOutil))
            {
                sorteRepere = s; if (s == Sorte.Joueur) joueurRepere = j;
                Prendre(Outil.Jeu); ClavierALaScene();
            }
            GUI.backgroundColor = avant;
        }

        private static GUIStyle rouge, bon, moyen, mauvais;
        private static GUIStyle Rouge => rouge ??= new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = new Color(1, .45f, .4f) } };
        private static GUIStyle Verdict(float part)
        {
            if (part < .1f) return bon ??= new GUIStyle(EditorStyles.miniBoldLabel) { normal = { textColor = new Color(.4f, .85f, .4f) } };
            if (part < .2f) return moyen ??= new GUIStyle(EditorStyles.miniBoldLabel) { normal = { textColor = new Color(1, .75f, .3f) } };
            return mauvais ??= new GUIStyle(EditorStyles.miniBoldLabel) { normal = { textColor = new Color(1, .4f, .35f) } };
        }

        private void JeuScene(SceneView sv, Event e)
        {
            if (e.type == EventType.MouseMove || e.type == EventType.MouseDrag || e.type == EventType.MouseDown)
            {
                viseRepereOk = !e.shift && ViserSurface(e.mousePosition, out viseRepere, out _);
                repereVise = e.shift ? RepereSous(e.mousePosition) : null;
                problemeRepere = viseRepereOk && sorteRepere != Sorte.Centre ? ProblemePose(viseRepere) : null;
            }
            if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
            {
                if (e.shift)
                {
                    if (repereVise != null)
                    {
                        Undo.IncrementCurrentGroup(); Undo.SetCurrentGroupName("Retirer " + repereVise.name);
                        Notifier($"« {repereVise.Nom} » retiré · Ctrl+Z pour le remettre");
                        Undo.DestroyObjectImmediate(repereVise.gameObject); repereVise = null;
                    }
                }
                // une apparition de joueur là où Froggy ne tient pas : refusée, avec la raison (prévenir plutôt que corriger)
                else if (viseRepereOk && sorteRepere == Sorte.Joueur && problemeRepere != null) Notifier("Pas ici : " + problemeRepere);
                else if (viseRepereOk) PoserRepere(viseRepere);
                e.Use(); return;
            }
            if (e.type != EventType.Repaint) return;
            if (e.shift && repereVise != null)
                using (new Handles.DrawingScope(new Color(1, .3f, .25f))) Handles.DrawWireDisc(repereVise.transform.position, Vector3.up, 1, 3);
            else if (viseRepereOk)
                foreach (var (p, r, k) in Images(viseRepere, Quaternion.Euler(0, lacetRepere, 0)))
                {
                    int j = sorteRepere == Sorte.Joueur ? CourbeArene.JoueurImage(joueurRepere, SymetrieChoisie, k) : 1;
                    using (new Handles.DrawingScope(k == 0 && problemeRepere != null ? new Color(1, .3f, .25f) : RepereArene.Couleur(sorteRepere, j)))
                    {
                        Handles.DrawWireDisc(p, Vector3.up, .6f, 3);
                        Handles.DrawLine(p, p + r * Vector3.forward * 1.2f, 3);
                        Handles.Label(p + Vector3.up * 1.2f, sorteRepere == Sorte.Joueur ? "J" + j : NomsSortes[(int)sorteRepere]);
                    }
                }
        }

        private string problemeRepere; private List<Bounds> eauxVisibles;
        private static readonly string[] NomsSortes = { "Joueur", "Cookies", "Boîte", "Gobelin", "Centre" };

        /// <summary>
        /// Ce qui empêche Froggy d'apparaître ou de ramasser quelque chose en p : dans l'eau, trop en pente, pas la place
        /// (un objet le gêne), rien dessous. Nul si l'endroit convient. Les mêmes règles que l'analyse des trajets.
        /// </summary>
        private string ProblemePose(Vector3 p)
        {
            if (gabarit == null) gabarit = Gabarit();
            if (eauxVisibles == null)
            {
                var noms = gabarit.nomsEau.Split(';').Select(n => n.Trim()).Where(n => n.Length > 0).ToArray();
                eauxVisibles = UnityEngine.Object.FindObjectsByType<Renderer>()
                    .Where(r => r.enabled && r.gameObject.activeInHierarchy && noms.Any(n => r.name.IndexOf(n, StringComparison.OrdinalIgnoreCase) >= 0))
                    .Select(r => r.bounds).ToList();
            }
            Physics.SyncTransforms();
            bool solTrouve = SolSous(p, .5f, (HashSet<Collider>)null, out var sol);
            foreach (var e in eauxVisibles)
                if (p.x > e.min.x && p.x < e.max.x && p.z > e.min.z && p.z < e.max.z && (!solTrouve || e.max.y > sol.point.y + .15f))
                    return "dans l'eau (Froggy y nagerait)";
            if (!solTrouve) return "rien dessous";
            if (Vector3.Angle(sol.normal, Vector3.up) > gabarit.penteMax) return "trop en pente pour tenir debout";
            var bas = sol.point + Vector3.up * (gabarit.rayon + gabarit.hauteurMarche);
            var haut = sol.point + Vector3.up * Mathf.Max(gabarit.rayon + gabarit.hauteurMarche, gabarit.hauteur - gabarit.rayon);
            foreach (var c in Physics.OverlapCapsule(bas, haut, gabarit.rayon * .9f, ~0, QueryTriggerInteraction.Ignore))
                if (c != sol.collider && (c.attachedRigidbody == null || c.attachedRigidbody.isKinematic))
                    return $"pas la place pour Froggy (« {ObjetDe(c.gameObject).name} » le gêne)";
            return null;
        }

        /// <summary>Le repère le plus proche de la souris, à 25 points au plus.</summary>
        private static RepereArene RepereSous(Vector2 souris)
        {
            RepereArene best = null; float d = 25;
            foreach (var r in Reperes())
            {
                float dd = Vector2.Distance(HandleUtility.WorldToGUIPoint(r.transform.position), souris);
                if (dd < d) { d = dd; best = r; }
            }
            return best;
        }

        private void PoserRepere(Vector3 p)
        {
            Undo.IncrementCurrentGroup(); int g = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Poser un repère");
            var groupe = GroupeReperes();
            foreach (var (pos, rot, k) in Images(p, Quaternion.Euler(0, lacetRepere, 0)))
            {
                int j = sorteRepere == Sorte.Joueur ? CourbeArene.JoueurImage(joueurRepere, SymetrieChoisie, k) : 1;
                var go = new GameObject(sorteRepere == Sorte.Joueur ? "J" + j : sorteRepere == Sorte.Cookie ? "Cookies" : sorteRepere == Sorte.Boite ? "Boîte" : sorteRepere.ToString());
                Undo.RegisterCreatedObjectUndo(go, "Poser un repère");
                go.transform.SetParent(groupe, false);
                go.transform.SetPositionAndRotation(pos, rot);
                var rep = go.AddComponent<RepereArene>();
                rep.sorte = sorteRepere; rep.joueur = j; rep.parties = partiesRepere;
            }
            Undo.CollapseUndoOperations(g);
            int images = CourbeArene.NombreImages(SymetrieChoisie);
            Notifier((sorteRepere == Sorte.Joueur ? $"Apparition de J{joueurRepere} posée" : $"« {NomsSortes[(int)sorteRepere]} » posé") +
                (images > 1 ? $" (+ {images - 1} par symétrie)" : "") + " · Ctrl+Z pour annuler");
            if (sorteRepere == Sorte.Joueur) joueurRepere = JoueurManquant();
            Repaint();
        }

        // ---------- Les trajets ----------

        private void EquilibreGUI()
        {
            reperes = Reperes();
            if (!reperes.Any(r => r.sorte == Sorte.Joueur))
            {
                EditorGUILayout.HelpBox("Pour mesurer, il faut d'abord savoir où apparaissent les joueurs.", MessageType.Info);
                if (GUILayout.Button("Aller à ② Jeu")) OuvrirEtape(Etape.Jeu);
                return;
            }
            var avant = (MeteoAnalyse, CalqueChoisi, JoueurCalque);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(new GUIContent("Météo", "La pluie tropicale accélère les grenouilles, la neige les ralentit"), GUILayout.Width(95));
                MeteoAnalyse = (GabaritFroggy.Meteo)EditorGUILayout.EnumPopup(MeteoAnalyse);
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(new GUIContent("Voir sur l'arène", "Les couleurs dessinées dans la vue Scène (la légende est en haut à droite)"), GUILayout.Width(95));
                CalqueChoisi = (Calque)EditorGUILayout.EnumPopup(CalqueChoisi);
                if (CalqueChoisi == Calque.Temps) JoueurCalque = EditorGUILayout.IntPopup(JoueurCalque, new[] { "J1", "J2", "J3", "J4" }, new[] { 1, 2, 3, 4 }, GUILayout.Width(50));
            }
            bool legende = EditorGUILayout.ToggleLeft(new GUIContent("Légende dans la vue Scène", "Ce que veulent dire les couleurs, en haut à droite de la vue Scène"), LegendeVisible);
            if (legende != LegendeVisible) { LegendeVisible = legende; SceneView.RepaintAll(); }
            if (avant.Item1 != MeteoAnalyse && grille != null) Analyser();   // la météo change les vitesses et la portée des sauts
            else if (avant != (MeteoAnalyse, CalqueChoisi, JoueurCalque)) { calqueAJour = false; SceneView.RepaintAll(); }

            if (grille == null)
            {
                if (GUILayout.Button(new GUIContent("Mesurer l'arène", "Calcule les trajets de chaque joueur, en courant, en nageant et en sautant"), GrosBouton)) Analyser();
                return;
            }
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Ce que le Bâtisseur en pense", EditorStyles.boldLabel);
            foreach (var (gravite, texte, voir) in Conseils(PartieA))
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.HelpBox(texte, gravite == 0 ? MessageType.Info : gravite == 1 ? MessageType.Warning : MessageType.Error);
                    if (voir != null && GUILayout.Button("Voir", GUILayout.Width(44), GUILayout.Height(38))) voir();
                }
            if (SuivreRetouches)
                EditorGUILayout.LabelField("La mesure se refait toute seule après chaque retouche : bouge un nénuphar ou une apparition, et regarde.", EditorStyles.wordWrappedMiniLabel);
            if (ModeAvance || SousTiroir("chiffres", "Détails chiffrés"))
            {
                TableauEquite();
                if (bilanTrajets != "") EditorGUILayout.HelpBox(bilanTrajets, MessageType.None);
            }
            if (!Plus("equilibre")) return;
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(new GUIContent("Mesurer", "Refait la mesure maintenant"))) Analyser();
                SuivreRetouches = GUILayout.Toggle(SuivreRetouches, new GUIContent(" Suivre les retouches", "Refait la mesure une demi-seconde après chaque changement de la scène"));
                if (GUILayout.Button(new GUIContent("Effacer", "Efface la mesure et ses couleurs"), GUILayout.Width(60))) { grille = null; tempsJoueurs.Clear(); calqueAJour = false; SceneView.RepaintAll(); }
            }
            if (gabarit == null) gabarit = Gabarit();
            if (SousTiroir("gabarit", "Gabarit de Froggy (vitesses, saut, météo)"))
            {
                if (gabaritSO == null || gabaritSO.targetObject != gabarit) gabaritSO = new SerializedObject(gabarit);
                gabaritSO.Update();
                var it = gabaritSO.GetIterator(); bool entrer = true;
                while (it.NextVisible(entrer)) { entrer = false; if (it.name != "m_Script") EditorGUILayout.PropertyField(it, true); }
                if (gabaritSO.ApplyModifiedProperties()) { AssetDatabase.SaveAssetIfDirty(gabarit); PerimerAnalyse(); }
                if (GUILayout.Button(new GUIContent("Reprendre du contrôleur", "Relit la course, le sprint et le saut du contrôleur actuel (TemplateMovementSettings)")))
                {
                    Undo.RecordObject(gabarit, "Reprendre du contrôleur");
                    if (ReprendreDuControleur(gabarit)) { EditorUtility.SetDirty(gabarit); AssetDatabase.SaveAssetIfDirty(gabarit); PerimerAnalyse(); }
                }
                var m = gabarit.Mouvement(MeteoAnalyse);
                EditorGUILayout.HelpBox($"Portée d'un saut à plat : {m.course * TrajetsArene.TempsDeVol(m.hauteurSaut, 0, m.gravite):0.0} m " +
                    $"({TrajetsArene.TempsDeVol(m.hauteurSaut, 0, m.gravite):0.00} s en l'air), course {m.course:0.0} m/s, nage {m.nage:0.0} m/s.", MessageType.None);
            }
        }

        // ---------- Les mesures et les conseils ----------

        private static readonly string[] Colonnes = { "1er cookie", "Cookies (moy.)", "1re boîte", "Gobelin", "Cookies 1er" };
        private static readonly string[] AidesColonnes =
        {
            "Le temps jusqu'au cookie le plus proche", "Le temps moyen jusqu'à tous les cookies", "Le temps jusqu'à la boîte à objets la plus proche",
            "Le temps jusqu'au départ du gobelin", "Le nombre de cookies où il arrive le premier, avec plus de 0,25 s d'avance",
        };

        /// <summary>Les temps de chaque joueur d'une partie à n vers les repères (une ligne par joueur, une colonne par mesure).</summary>
        private class Mesures
        {
            public List<RepereArene> joueurs = new List<RepereArene>();
            public Dictionary<RepereArene, float[]> valeurs = new Dictionary<RepereArene, float[]>();
            public List<(RepereArene repere, List<int> qui)> inaccessibles = new List<(RepereArene, List<int>)>();

            /// <summary>L'écart entre le plus lent et le plus rapide, rapporté à la moyenne (NaN si on ne peut pas le dire).</summary>
            public float Ecart(int col)
            {
                if (joueurs.Count < 2) return float.NaN;
                var v = joueurs.Select(j => valeurs[j][col]).ToList();
                if (v.Any(float.IsNaN) || v.Any(float.IsInfinity)) return float.NaN;
                float moy = v.Average();
                return moy > 0 ? (v.Max() - v.Min()) / moy : 0;
            }

            public (RepereArene lent, RepereArene rapide) Extremes(int col, bool plusEstMieux = false)
            {
                var tri = joueurs.OrderBy(j => valeurs[j][col]).ToList();
                return plusEstMieux ? (tri.First(), tri.Last()) : (tri.Last(), tri.First());
            }
        }

        private Mesures Mesurer(int n)
        {
            var m = new Mesures();
            if (grille == null) return m;
            var reps = Reperes();
            m.joueurs = reps.Where(r => r.sorte == Sorte.Joueur && r.SertA(n) && tempsJoueurs.ContainsKey(r)).OrderBy(r => r.joueur).ToList();
            var cookies = reps.Where(r => r.sorte == Sorte.Cookie && r.SertA(n)).ToList();
            var boites = reps.Where(r => r.sorte == Sorte.Boite && r.SertA(n)).ToList();
            var gobelin = reps.FirstOrDefault(r => r.sorte == Sorte.Gobelin);
            var cases = new Dictionary<RepereArene, int>();
            float T(RepereArene j, RepereArene cible)
            {
                if (!cases.TryGetValue(cible, out int c)) cases[cible] = c = grille.CellulePraticable(cible.transform.position, 1.5f);
                return c < 0 ? float.PositiveInfinity : tempsJoueurs[j][c];
            }
            foreach (var j in m.joueurs)
                m.valeurs[j] = new[]
                {
                    cookies.Count == 0 ? float.NaN : cookies.Min(c => T(j, c)),
                    cookies.Count == 0 ? float.NaN : cookies.Average(c => T(j, c)),
                    boites.Count == 0 ? float.NaN : boites.Min(b => T(j, b)),
                    gobelin == null ? float.NaN : T(j, gobelin),
                    cookies.Count(c => m.joueurs.All(a => a == j || T(j, c) <= T(a, c) - .25f)),
                };
            foreach (var r in reps.Where(r => r.sorte != Sorte.Centre && r.SertA(n)))
            {
                var qui = m.joueurs.Where(j => float.IsInfinity(T(j, r))).Select(j => j.joueur).ToList();
                if (qui.Count > 0) m.inaccessibles.Add((r, qui));
            }
            return m;
        }

        private static Action Voir(GameObject go) => () => { Selection.activeGameObject = go; SceneView.lastActiveSceneView?.FrameSelected(); };

        /// <summary>Ce que l'analyse veut dire, en clair, avec quoi faire : 0 tout va bien, 1 à surveiller, 2 à corriger.</summary>
        private List<(int gravite, string texte, Action voir)> Conseils(int n)
        {
            var l = new List<(int, string, Action)>();
            var m = Mesurer(n);
            string meteo = MeteoAnalyse == GabaritFroggy.Meteo.Normal ? "" : MeteoAnalyse == GabaritFroggy.Meteo.Pluie ? " sous la pluie" : " sous la neige";
            foreach (var (r, qui) in m.inaccessibles)
                l.Add((2, $"« {r.Nom} » est hors d'atteinte pour {string.Join(", ", qui.Select(j => "J" + j))}{meteo} : coupé par l'eau profonde, un mur ou un saut trop long. " +
                    "Rapproche-le, ou ajoute un nénuphar ou un pont.", Voir(r.gameObject)));

            void Ecart(int col, string quoi, string remede)
            {
                float e = m.Ecart(col);
                if (float.IsNaN(e) || e < .1f) return;
                var (lent, rapide) = m.Extremes(col);
                l.Add((e >= .2f ? 2 : 1, $"J{lent.joueur} met {m.valeurs[lent][col]:0.0} s pour atteindre {quoi}, J{rapide.joueur} {m.valeurs[rapide][col]:0.0} s ({e:P0} d'écart){meteo}. " +
                    string.Format(remede, "J" + lent.joueur, "J" + rapide.joueur), Voir(lent.gameObject)));
            }
            Ecart(0, "son premier cookie", "Rapproche un cookie de {0}, ou éloigne de {1} celui qu'il atteint en premier.");
            Ecart(2, "sa première boîte", "Rapproche une boîte de {0}, ou éloigne celle de {1}.");
            Ecart(3, "le départ du gobelin", "Place le départ du gobelin plus au centre, à égale distance des joueurs.");
            if (m.joueurs.Count >= 2 && m.valeurs.Values.All(v => !float.IsNaN(v[1])))
            {
                var (moins, plus) = m.Extremes(4, true);
                float a = m.valeurs[moins][4], b = m.valeurs[plus][4];
                if (b - a >= 2)
                    l.Add((1, $"J{plus.joueur} arrive le premier sur {b:0} cookies, J{moins.joueur} sur {(a == 0 ? "aucun" : a.ToString("0"))}. Rééquilibre la place des cookies entre leurs deux côtés.", Voir(moins.gameObject)));
            }
            if (!Reperes().Any(r => r.sorte == Sorte.Cookie && r.SertA(n)))
                l.Add((1, "Pas encore de cookies : l'équité se mesure vers les cookies et les boîtes. Pose-les à l'étape ② Jeu.", () => { OuvrirEtape(Etape.Jeu); sorteRepere = Sorte.Cookie; }));

            var justes = grille.liens.Values.Where(x => x.marge >= 0 && x.marge < .5f).ToList();
            var rates = grille.liens.Values.Where(x => x.marge < 0).ToList();
            if (justes.Count > 0)
                l.Add((1, $"{justes.Count} saut(s) juste(s){meteo} : moins de 50 cm de marge. Un joueur pressé les ratera ; garde-les seulement si c'est voulu.", VoirSaut(justes[0])));
            if (rates.Count > 0)
                l.Add((1, $"{rates.Count} saut(s) raté(s) de peu{meteo} : il manque moins de 75 cm. Rapproche les deux surfaces si le saut doit passer.", VoirSaut(rates[0])));

            if (!l.Any(x => x.Item1 >= 1))
            {
                float e = m.Ecart(0);
                l.Insert(0, (0, float.IsNaN(e) ? $"Rien à redire pour une partie à {n}{meteo}."
                    : $"Équitable pour une partie à {n}{meteo} : au plus {e:P0} d'écart entre les joueurs pour le premier cookie, et tout est accessible.", null));
            }
            return l;
        }

        private Action VoirSaut((float marge, int de, int vers) lien) => () =>
        {
            var sv = SceneView.lastActiveSceneView; if (sv == null || grille == null) return;
            sv.LookAt((grille.Centre(lien.de) + grille.Centre(lien.vers)) / 2, sv.rotation, 6);
        };

        private void TableauEquite()
        {
            int n = PartieA;
            var m = Mesurer(n);
            if (m.joueurs.Count == 0) { EditorGUILayout.HelpBox($"Aucune apparition de joueur pour une partie à {n}.", MessageType.Warning); return; }
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("", GUILayout.Width(28));
                for (int c = 0; c < Colonnes.Length; c++) GUILayout.Label(new GUIContent(Colonnes[c], AidesColonnes[c]), EditorStyles.miniBoldLabel, GUILayout.Width(78));
            }
            foreach (var j in m.joueurs)
                using (new EditorGUILayout.HorizontalScope())
                {
                    var avant = GUI.contentColor; GUI.contentColor = RepereArene.CouleursJoueurs[j.joueur - 1];
                    if (GUILayout.Button("J" + j.joueur, EditorStyles.miniButton, GUILayout.Width(28))) Voir(j.gameObject)();
                    GUI.contentColor = avant;
                    for (int c = 0; c < Colonnes.Length; c++)
                    {
                        float v = m.valeurs[j][c];
                        GUILayout.Label(float.IsNaN(v) ? "—" : float.IsInfinity(v) ? "inaccessible" : c == 4 ? $"{v:0}" : $"{v:0.0} s", EditorStyles.miniLabel, GUILayout.Width(78));
                    }
                }
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("Écart", EditorStyles.miniBoldLabel, GUILayout.Width(28));
                for (int c = 0; c < Colonnes.Length; c++)
                {
                    var v = m.joueurs.Select(j => m.valeurs[j][c]).ToList();
                    if (v.Any(float.IsNaN) || v.Any(float.IsInfinity) || m.joueurs.Count < 2) { GUILayout.Label("", GUILayout.Width(78)); continue; }
                    float ecart = v.Max() - v.Min(), moy = v.Average(), part = moy > 0 ? ecart / moy : 0;
                    GUILayout.Label(c == 4 ? $"{ecart:0}" : $"{ecart:0.0} s ({part:P0})", Verdict(part), GUILayout.Width(78));
                }
            }
            EditorGUILayout.LabelField("Vert : moins de 10 % d'écart entre les joueurs ; orange : moins de 20 % ; rouge : au-delà.", EditorStyles.wordWrappedMiniLabel);
        }

        /// <summary>Mesure l'arène autour des repères et calcule les trajets depuis chaque apparition de joueur.</summary>
        internal string Analyser()
        {
            if (gabarit == null) gabarit = Gabarit();
            reperes = Reperes();
            var joueurs = reperes.Where(r => r.sorte == Sorte.Joueur).ToList();
            if (joueurs.Count == 0)
            {
                bilanTrajets = "Pose d'abord les apparitions des joueurs (étape ② Jeu)."; grille = null; analysePerimee = -1;
                VerifierMaintenant(); Repaint();
                return bilanTrajets;
            }
            var chrono = Stopwatch.StartNew();
            Physics.SyncTransforms();
            // la zone : les repères et les sols sous eux
            var zone = new Bounds(reperes[0].transform.position, Vector3.one);
            foreach (var r in reperes)
            {
                zone.Encapsulate(r.transform.position);
                if (SolSous(r.transform.position, 1, (HashSet<Collider>)null, out var sol))
                {
                    var b = Boite(ObjetDe(sol.collider.gameObject));
                    if (b.size.x < 300 && b.size.z < 300) zone.Encapsulate(b);
                }
            }
            zone.Expand(new Vector3(4, 0, 4));
            float plafond = reperes.Max(r => r.transform.position.y) + gabarit.plafond;
            var m = gabarit.Mouvement(MeteoAnalyse);
            grille = TrajetsArene.Construire(zone, plafond, gabarit);
            TrajetsArene.Sauts(grille, m);
            tempsJoueurs.Clear();
            foreach (var j in joueurs)
                tempsJoueurs[j] = TrajetsArene.Temps(grille, grille.CellulePraticable(j.transform.position, 1.5f), m);
            chrono.Stop();
            int praticables = grille.genre.Count(x => x != GrilleTrajets.Vide);
            int serres = grille.liens.Values.Count(l => l.marge >= 0 && l.marge < .5f), rates = grille.liens.Values.Count(l => l.marge < 0);
            bilanTrajets = $"{grille.nx} × {grille.nz} cases de {grille.pas:0.##} m, {praticables} praticables, {grille.sautVers.Length} sauts possibles ; " +
                $"{serres} saut(s) serré(s) (moins de 0,5 m de marge), {rates} raté(s) de peu. Calculé en {chrono.ElapsedMilliseconds} ms ({MeteoAnalyse}).";
            calqueAJour = false; analysePerimee = -1;
            VerifierMaintenant();
            Repaint(); SceneView.RepaintAll();
            return bilanTrajets;
        }

        private void PerimerAnalyse()
        {
            if (SuivreRetouches) analysePerimee = EditorApplication.timeSinceStartup;
            eauxVisibles = null;
        }

        /// <summary>Une demi-seconde après la dernière retouche, l'analyse est refaite.</summary>
        private void SuivreLaScene()
        {
            if (analysePerimee < 0 || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
            if (EditorApplication.timeSinceStartup - analysePerimee < .5) return;
            if (enTrait || glisse >= 0) return;   // pas pendant un coup de pinceau ou un glissé
            Analyser();
        }

        // ---------- Le calque dans la vue Scène ----------

        private void DessinerTrajets(SceneView sv)
        {
            if (grille == null || CalqueChoisi == Calque.Aucun || Event.current.type != EventType.Repaint) return;
            if (CalqueChoisi != Calque.Sauts)
            {
                if (!calqueAJour) { ConstruireCalque(); calqueAJour = true; }
                if (calque != null)
                {
                    if (calqueMat == null)
                    {
                        calqueMat = new Material(Shader.Find("Hidden/Internal-Colored")) { hideFlags = HideFlags.HideAndDontSave };
                        calqueMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                        calqueMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        calqueMat.SetInt("_Cull", 0); calqueMat.SetInt("_ZWrite", 0);
                        calqueMat.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.LessEqual);
                    }
                    calqueMat.SetPass(0);
                    Graphics.DrawMeshNow(calque, Matrix4x4.identity);
                }
            }
            // les sauts entre deux surfaces : vert, orange (serré), rouge (raté de peu)
            foreach (var l in grille.liens.Values)
            {
                if (l.marge >= .5f && CalqueChoisi != Calque.Sauts) continue;   // sur les autres calques, seulement ceux qui posent question
                var a = grille.Centre(l.de) + Vector3.up * .15f; var b = grille.Centre(l.vers) + Vector3.up * .15f;
                var c = l.marge >= .5f ? new Color(.35f, .9f, .4f) : l.marge >= 0 ? new Color(1, .7f, .2f) : new Color(1, .3f, .25f);
                using (new Handles.DrawingScope(c))
                {
                    Handles.DrawAAPolyLine(4, a, (a + b) / 2 + Vector3.up * .6f, b);
                    if (l.marge < .5f) Handles.Label((a + b) / 2 + Vector3.up * .8f, l.marge >= 0 ? $"+{l.marge:0.00} m" : $"{l.marge:0.00} m", EditorStyles.whiteMiniLabel);
                }
            }
        }

        private void ConstruireCalque()
        {
            if (calque == null) calque = new Mesh { hideFlags = HideFlags.HideAndDontSave, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            calque.Clear();
            var joueurs = reperes.Where(r => r != null && r.sorte == Sorte.Joueur && r.SertA(PartieA) && tempsJoueurs.ContainsKey(r)).ToList();
            var cible = joueurs.FirstOrDefault(j => j.joueur == JoueurCalque);
            float[] temps = cible != null ? tempsJoueurs[cible] : null;
            float tmax = 1;
            if (temps != null) { var finis = temps.Where(t => !float.IsInfinity(t)).OrderBy(t => t).ToList(); if (finis.Count > 0) tmax = Mathf.Max(1, finis[(int)(finis.Count * .95f)]); }
            var v = new List<Vector3>(); var col = new List<Color>(); var tri = new List<int>();
            float h = grille.pas * .5f;
            for (int i = 0; i < grille.Taille; i++)
            {
                if (grille.genre[i] == GrilleTrajets.Vide) continue;
                Color c;
                if (CalqueChoisi == Calque.Temps)
                {
                    if (temps == null) continue;
                    float t = temps[i];
                    c = float.IsInfinity(t) ? new Color(.4f, .4f, .4f, .55f) : Degrade(t / tmax);
                }
                else
                {
                    // le premier arrivé ; à moins de 0,5 s du deuxième, la case est disputée (blanche)
                    int premier = -1; float t1 = float.PositiveInfinity, t2 = float.PositiveInfinity;
                    foreach (var j in joueurs)
                    {
                        float t = tempsJoueurs[j][i];
                        if (t < t1) { t2 = t1; t1 = t; premier = j.joueur; } else if (t < t2) t2 = t;
                    }
                    if (premier < 0 || float.IsInfinity(t1)) c = new Color(.4f, .4f, .4f, .55f);
                    else if (t2 - t1 < .5f) c = new Color(1, 1, 1, .45f);
                    else { c = RepereArene.CouleursJoueurs[premier - 1]; c.a = .45f; }
                }
                if (grille.genre[i] == GrilleTrajets.Eau) c = Color.Lerp(c, new Color(.1f, .3f, .8f, c.a), .35f);
                var p = grille.Centre(i) + Vector3.up * .06f; int k = v.Count;
                v.Add(p + new Vector3(-h, 0, -h)); v.Add(p + new Vector3(-h, 0, h)); v.Add(p + new Vector3(h, 0, h)); v.Add(p + new Vector3(h, 0, -h));
                col.Add(c); col.Add(c); col.Add(c); col.Add(c);
                tri.Add(k); tri.Add(k + 1); tri.Add(k + 2); tri.Add(k); tri.Add(k + 2); tri.Add(k + 3);
            }
            calque.SetVertices(v); calque.SetColors(col); calque.SetTriangles(tri, 0);
        }

        /// <summary>Vert (tout près) → jaune → rouge (loin).</summary>
        private static Color Degrade(float x)
        {
            x = Mathf.Clamp01(x);
            var c = x < .5f ? Color.Lerp(new Color(.25f, .85f, .35f), new Color(1, .9f, .25f), x * 2) : Color.Lerp(new Color(1, .9f, .25f), new Color(.95f, .25f, .2f), (x - .5f) * 2);
            c.a = .5f; return c;
        }
    }
}
