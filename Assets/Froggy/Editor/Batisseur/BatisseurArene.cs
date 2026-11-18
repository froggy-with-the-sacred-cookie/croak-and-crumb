using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Froggy.Outils
{
    /// <summary>
    /// Le Bâtisseur d'arène (menu Froggy ▸ Bâtisseur d'arène) : la fenêtre qui réunit les outils pour habiller une arène
    /// dans la vue Scène. Repris du Bâtisseur de Lovecraft (2026-10-08), refait pour des arènes en 3D libre (Lovecraft
    /// posait sur une grille de cases). Rangée en cinq étapes (BatisseurArene.Etapes), avec cinq outils :
    /// Poser (la palette des préfabriqués), Semer (un mélange au pinceau), Tracer (des pièces le long d'une courbe),
    /// Tampons (reposer un coin réussi), Jeu (les repères de la partie et l'analyse des trajets). La symétrie de l'arène,
    /// les tiroirs Vérifier et Versions. « Jouer ici » (F5) est dans JouerIci.
    /// Les mêmes gestes partout : clic pose, Maj+clic retire, I+clic prélève, A / E tournent, Entrée applique, Échap
    /// rend la main aux outils d'Unity, Ctrl+Z annule un geste entier (chaque geste ouvre son groupe d'annulation).
    /// L'outil fabrique ; la disposition de l'arène se décide dans l'éditeur, par un humain.
    /// Mode d'emploi : Docs/Outils/BATISSEUR.md.
    /// </summary>
    public partial class BatisseurArene : EditorWindow
    {
        public enum Outil { Poser, Semer, Tracer, Tampons, Jeu }
        private static readonly string[] NomsOutils = { "Poser", "Semer", "Tracer", "Tampons", "Jeu" };
        private static readonly string[] AidesOutils =
        {
            "Poser un préfabriqué de la palette, d'un clic dans la vue Scène",
            "Répandre un mélange (sous-bois, pierres, massifs) au pinceau",
            "Des pièces le long d'une courbe : clôture, muraille, allée, gué, nénuphars",
            "Reposer un coin réussi, en enregistrer un, varier la sélection",
            "Les repères de la partie (apparitions, cookies, boîtes, gobelin) et l'équité des trajets",
        };

        public const string Dossier = "Assets/Froggy/Batisseur";
        private const string Pref = "Froggy.batisseur.";

        /// <summary>L'Inspecteur d'Unity (une classe interne) : la fenêtre se range en onglet à côté de lui.</summary>
        private static readonly Type Inspecteur = typeof(EditorWindow).Assembly.GetType("UnityEditor.InspectorWindow");

        public static BatisseurArene Instance { get; private set; }

        [MenuItem("Froggy/Bâtisseur d'arène", priority = 0)]
        public static void Ouvrir() => Fenetre();

        public static BatisseurArene Fenetre()
        {
            var w = Inspecteur != null ? GetWindow<BatisseurArene>(Inspecteur) : GetWindow<BatisseurArene>();
            w.titleContent = new GUIContent("Bâtisseur");
            return w;
        }

        [SerializeField] private Outil outil = Outil.Poser;
        private bool actif, pipette;
        private Vector2 defilement;
        private GameObject racine;

        /// <summary>Un outil du Bâtisseur est pris (la vue Scène lui appartient).</summary>
        public bool UnOutilPris => actif;
        public Outil OutilCourant => outil;

        private void OnEnable()
        {
            Instance = this;
            titleContent = new GUIContent("Bâtisseur");
            outil = (Outil)Mathf.Clamp(EditorPrefs.GetInt(Pref + "outil", 0), 0, NomsOutils.Length - 1);
            SceneView.duringSceneGui += SceneGUI;
            EditorApplication.projectChanged += OublierCatalogues;
            Undo.undoRedoPerformed += ApresAnnulation;
            EditorSceneManager.activeSceneChangedInEditMode += ChangementDeScene;
            EditorApplication.update += SuivreLaScene;
            EditorApplication.hierarchyChanged += ChangementDeHierarchie;
            ObjectChangeEvents.changesPublished += Changements;
            analysePerimee = EditorApplication.timeSinceStartup;   // une première mesure, pour la liste « Prête pour un test »
            Froggy.Arene.RepereArene.DessinesParLeBatisseur = true;
        }

        private void OnDisable()
        {
            if (Instance == this) { Instance = null; Froggy.Arene.RepereArene.DessinesParLeBatisseur = false; }
            SceneView.duringSceneGui -= SceneGUI;
            EditorApplication.projectChanged -= OublierCatalogues;
            Undo.undoRedoPerformed -= ApresAnnulation;
            EditorSceneManager.activeSceneChangedInEditMode -= ChangementDeScene;
            EditorApplication.update -= SuivreLaScene;
            EditorApplication.hierarchyChanged -= ChangementDeHierarchie;
            ObjectChangeEvents.changesPublished -= Changements;
            if (calque != null) DestroyImmediate(calque);
            if (calqueMat != null) DestroyImmediate(calqueMat);
        }

        /// <summary>Toute retouche de la scène périme l'analyse des trajets (refaite une demi-seconde plus tard si on la suit).</summary>
        private void Changements(ref ObjectChangeEventStream flux) => PerimerAnalyse();

        private void ChangementDeHierarchie() { racineEssai = -10; eauxVisibles = null; }

        private void OublierCatalogues()
        {
            palette = null; bibliotheque = null; Fantome.Oublier(); Repaint();
        }

        private void ApresAnnulation() { planTrace = null; planTampon = null; PerimerAnalyse(); Repaint(); SceneView.RepaintAll(); }

        private void ChangementDeScene(UnityEngine.SceneManagement.Scene a, UnityEngine.SceneManagement.Scene b)
        {
            racine = null; racineEssai = -10; derniereApplication = null; grille = null; tempsJoueurs.Clear();
            constats = null; gravesListe = null; eauxVisibles = null; analysePerimee = EditorApplication.timeSinceStartup; Repaint();
        }

        // ---------- Les outils ----------

        /// <summary>Prend un outil : la vue Scène lui appartient jusqu'à Échap ou un outil d'Unity (W, E, R…).</summary>
        public void Prendre(Outil o)
        {
            if (o != outil) QuitterOutil();
            outil = o; actif = true;
            EditorPrefs.SetInt(Pref + "outil", (int)o);
            // l'étape suit l'outil (un raccourci, la barre de la vue Scène), et retient son dernier outil
            if (!OutilsEtapes[(int)EtapeChoisie].Contains(o))
                EtapeChoisie = (Etape)Array.FindIndex(OutilsEtapes, l => l.Contains(o));
            EditorPrefs.SetInt(Pref + "outil." + EtapeChoisie, (int)o);
            Tools.current = Tool.None;
            Repaint(); SceneView.RepaintAll();
        }

        /// <summary>Échap, ou un outil d'Unity repris : la vue Scène redevient celle d'Unity.</summary>
        public void Lacher()
        {
            actif = false; pipette = false; QuitterOutil();
            if (Tools.current == Tool.None) Tools.current = Tool.Move;
            Repaint(); SceneView.RepaintAll();
        }

        /// <summary>Ce qu'on oublie en quittant un outil : l'objet ou le tampon qu'on tenait (la courbe reste).</summary>
        private void QuitterOutil() { enMain = null; tamponEnMain = null; planTampon = null; enTrait = false; glisse = -1; }

        /// <summary>Après un clic dans la fenêtre, le clavier revient à la vue Scène : Échap, I et Entrée y marchent tout de suite.</summary>
        private void ClavierALaScene()
        {
            var sv = SceneView.lastActiveSceneView;
            if (sv == null || (docked && sv.docked && position.Overlaps(sv.position))) return;
            EditorApplication.delayCall += () => { if (sv != null) sv.Focus(); };
        }

        /// <summary>A et E : ce qu'on tient tourne. Faux si l'on ne tient rien.</summary>
        public bool TournerEnMain(int sens)
        {
            if (!actif) return false;
            float pas = PasRotation;
            if (outil == Outil.Poser && enMain != null) { lacetEnMain = Mathf.Repeat(lacetEnMain + sens * pas, 360); SceneView.RepaintAll(); return true; }
            if (outil == Outil.Tampons && tamponEnMain != null) { lacetTampon = Mathf.Repeat(lacetTampon + sens * pas, 360); planTampon = null; SceneView.RepaintAll(); return true; }
            if (outil == Outil.Jeu) { lacetRepere = Mathf.Repeat(lacetRepere + sens * pas, 360); SceneView.RepaintAll(); return true; }
            return false;
        }

        /// <summary>M : le tampon qu'on tient passe en miroir.</summary>
        public void MiroirEnMain()
        {
            if (outil == Outil.Tampons && tamponEnMain != null) { miroirTampon = !miroirTampon; planTampon = null; SceneView.RepaintAll(); }
        }

        /// <summary>T : un autre tirage (la variation du tampon, l'orientation et la taille au hasard de l'objet à poser, le tracé).</summary>
        public void AutreEnMain()
        {
            if (outil == Outil.Tampons) { graineTampon++; planTampon = null; }
            else if (outil == Outil.Poser) TirerAuHasard();
            else if (outil == Outil.Tracer) { graineTrace++; planTrace = null; }
            SceneView.RepaintAll();
        }

        public static float PasRotation
        {
            get => EditorPrefs.GetFloat(Pref + "pasRotation", 45);
            set => EditorPrefs.SetFloat(Pref + "pasRotation", value);
        }

        /// <summary>A et E tournent aussi les objets sélectionnés quand aucun outil n'est pris (sinon E reste l'outil de rotation d'Unity).</summary>
        public static bool TournerLaSelection
        {
            get => EditorPrefs.GetBool(Pref + "tournerSelection", false);
            set => EditorPrefs.SetBool(Pref + "tournerSelection", value);
        }

        // ---------- La fenêtre ----------

        private void OnGUI()
        {
            Instance = this;
            // jamais plus large que la fenêtre : le texte passe à la ligne au lieu de déborder (rangée contre l'Inspecteur, elle est étroite)
            using (var s = new EditorGUILayout.ScrollViewScope(defilement, false, false, GUIStyle.none, GUI.skin.verticalScrollbar, GUIStyle.none))
            using (new EditorGUILayout.VerticalScope(GUILayout.MaxWidth(position.width - 18)))
            {
                defilement = s.scrollPosition;
                if (EstBacASable()) MissionsGUI();
                else { EnTeteArene(); ProchaineEtape(); }
                EditorGUILayout.Space(4);
                BarreEtapes();
                EditorGUILayout.Space(4);
                var e = EtapeChoisie;
                EditorGUI.BeginChangeCheck();
                switch (e)
                {
                    case Etape.Jeu: JeuGUI(); break;
                    case Etape.Equilibre: EquilibreGUI(); break;
                    case Etape.Tester: TesterGUI(); break;
                    default:
                        SymetrieGUI();
                        OngletsOutils();
                        EditorGUILayout.Space(2);
                        switch (outil)
                        {
                            case Outil.Poser: PoserGUI(); break;
                            case Outil.Semer: SemerGUI(); break;
                            case Outil.Tracer: TracerGUI(); break;
                            case Outil.Tampons: TamponsGUI(); break;
                        }
                        break;
                }
                // toucher un réglage reprend l'outil de l'étape
                if (EditorGUI.EndChangeCheck() && !actif && OutilsEtapes[(int)e].Contains(outil)) Prendre(outil);
                EditorGUILayout.Space(10);
                ReglagesGUI();
            }
        }

        private static readonly float[] Pas = { 15, 30, 45, 90 };

        private static GUIStyle boutonOutil, boutonMontre;
        private static GUIStyle BoutonOutil => boutonOutil ??= new GUIStyle(EditorStyles.miniButton) { fixedHeight = 26 };
        private static GUIStyle BoutonMontre => boutonMontre ??= new GUIStyle(EditorStyles.miniButton) { fixedHeight = 26, fontStyle = FontStyle.Bold };

        /// <summary>L'aide de l'outil pris (la fenêtre et la barre de la vue Scène).</summary>
        public string Gestes()
        {
            switch (outil)
            {
                case Outil.Poser: return "Clic : poser · Maj+clic : retirer · I+clic : prélever · A / E : tourner · T : autre tirage · Échap : arrêter";
                case Outil.Semer: return "Glisser : semer · Maj+glisser : effacer · Échap : arrêter";
                case Outil.Tracer: return "Clic : un point (sur la courbe : en insérer un) · glisser un point · Maj+clic : le retirer · Retour arrière : le dernier · Entrée : tracer · Échap : effacer la courbe";
                case Outil.Tampons: return "Clic : poser le tampon · A / E : tourner · M : miroir · T : autre variante · Échap : arrêter";
                default: return "Clic : poser le repère · Maj+clic : le retirer · A / E : tourner · Échap : arrêter";
            }
        }

        private static bool SousTiroir(string cle, string titre)
        {
            bool ouvert = EditorPrefs.GetBool(Pref + "tiroir." + cle, false);
            bool n = EditorGUILayout.Foldout(ouvert, titre, true);
            if (n != ouvert) EditorPrefs.SetBool(Pref + "tiroir." + cle, n);
            return n;
        }

        // ---------- La vue Scène ----------

        private void SceneGUI(SceneView sv)
        {
            DessinerTrajets(sv);   // le calque de l'analyse reste visible, outil pris ou non
            if (Event.current.type == EventType.MouseDown) DessinerLegende(sv);   // un clic sur la légende la replie, avant les outils
            if (Event.current.type == EventType.Repaint)
                foreach (var r in UnityEngine.Object.FindObjectsByType<Froggy.Arene.RepereArene>())
                    if (r.gameObject.activeInHierarchy) r.Dessiner(Selection.Contains(r.gameObject));
            OutilsScene(sv);
            if (Event.current.type == EventType.Repaint) DessinerLegende(sv);     // dessinée en dernier : au-dessus des étiquettes
        }

        private void OutilsScene(SceneView sv)
        {
            if (!actif) return;
            // un outil d'Unity repris (W, E, R…) lâche le nôtre
            if (Tools.current != Tool.None) { Lacher(); return; }
            var e = Event.current;
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
            {
                if (!(outil == Outil.Tracer && EffacerCourbe())) Lacher();
                e.Use(); return;
            }
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.I) { pipette = true; e.Use(); }
            if (e.type == EventType.KeyUp && e.keyCode == KeyCode.I) { pipette = false; e.Use(); }
            if (Tools.viewToolActive) return;   // on navigue (clic droit, Alt)
            if (e.type == EventType.Layout) HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
            if (e.type == EventType.Repaint) DessinerSymetrie();

            if (pipette && e.type == EventType.MouseDown && e.button == 0)
            {
                Prelever(e.mousePosition); e.Use(); return;
            }
            switch (outil)
            {
                case Outil.Poser: PoserScene(sv, e); break;
                case Outil.Semer: SemerScene(sv, e); break;
                case Outil.Tracer: TracerScene(sv, e); break;
                case Outil.Tampons: TamponsScene(sv, e); break;
                case Outil.Jeu: JeuScene(sv, e); break;
            }
            DessinerBandeau(sv, e);
            if (e.type == EventType.MouseMove) sv.Repaint();
        }

        /// <summary>I+clic : le préfabriqué visé devient celui qu'on pose (outil Poser).</summary>
        private void Prelever(Vector2 souris)
        {
            var go = HandleUtility.PickGameObject(souris, false);
            var r = go != null ? PrefabUtility.GetOutermostPrefabInstanceRoot(go) : null;
            var modele = r != null ? PrefabUtility.GetCorrespondingObjectFromSource(r) : null;
            if (modele == null) { ShowNotification(new GUIContent("Rien à prélever : vise un préfabriqué")); return; }
            if (outil != Outil.Poser) Prendre(Outil.Poser);
            // le lacet par-dessus la rotation du préfabriqué (à 90° sur X, les angles d'Euler de l'objet ne le donnent pas)
            enMain = modele; lacetEnMain = Mathf.Round((r.transform.rotation * Quaternion.Inverse(modele.transform.localRotation)).eulerAngles.y);
            Repaint();
        }

        // ---------- Viser et ranger ----------

        /// <summary>Le point visé sur ce qui se voit (eau comprise), par le dessin de la scène.</summary>
        internal static bool ViserSurface(Vector2 souris, out Vector3 pos, out Vector3 normale)
            => HandleUtility.PlaceObject(souris, out pos, out normale);

        /// <summary>Le point visé sur ce qui a une collision (le sol, un talus ; l'eau n'en a pas).</summary>
        internal static bool ViserCollision(Vector2 souris, out RaycastHit hit)
        {
            Physics.SyncTransforms();
            var ray = HandleUtility.GUIPointToWorldRay(souris);
            var hits = Physics.RaycastAll(ray, 2000, ~0, QueryTriggerInteraction.Ignore);
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var h in hits) if (!Mobile(h.collider)) { hit = h; return true; }
            hit = default; return false;
        }

        /// <summary>Le sol sous un point : la première collision en descendant depuis auDessus mètres plus haut.</summary>
        internal static bool SolSous(Vector3 p, float auDessus, HashSet<Collider> ignorer, out RaycastHit sol)
            => SolSous(p, auDessus, c => ignorer == null || !ignorer.Contains(c), out sol);

        /// <summary>La première collision acceptée en descendant depuis auDessus mètres plus haut.</summary>
        internal static bool SolSous(Vector3 p, float auDessus, Func<Collider, bool> accepter, out RaycastHit sol)
        {
            var hits = Physics.RaycastAll(p + Vector3.up * auDessus, Vector3.down, auDessus + 60, ~0, QueryTriggerInteraction.Ignore);
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var h in hits)
                if (accepter(h.collider) && !Mobile(h.collider)) { sol = h; return true; }
            sol = default; return false;
        }

        /// <summary>
        /// Les sols d'un geste : les objets sous ces points (là où l'on a cliqué). Une pièce se pose sur eux seulement, pas
        /// sur la couronne d'un arbre voisin (2026-10-29 : des morceaux de clôture s'étaient posés sur les cerisiers).
        /// </summary>
        internal static HashSet<GameObject> SolsSous(IEnumerable<Vector3> pts)
        {
            var sols = new HashSet<GameObject>();
            foreach (var p in pts)
                if (SolSous(p, .3f, (HashSet<Collider>)null, out var h)) sols.Add(ObjetDe(h.collider.gameObject));
            return sols;
        }

        /// <summary>Ce qui bouge en jeu (le joueur, un objet physique) n'est pas un sol.</summary>
        private static bool Mobile(Collider c) => c.attachedRigidbody != null && !c.attachedRigidbody.isKinematic;

        /// <summary>L'objet de la scène auquel appartient un morceau : son préfabriqué le plus extérieur, sinon lui-même.</summary>
        internal static GameObject ObjetDe(GameObject go)
        {
            if (go == null) return null;
            var r = PrefabUtility.GetOutermostPrefabInstanceRoot(go);
            return r != null ? r : go;
        }

        /// <summary>Le groupe où ranger ce qu'on pose : celui choisi pour la scène, sinon « Décor » (créé si creer).</summary>
        private double racineEssai = -10;

        private GameObject Racine(bool creer)
        {
            if (racine != null && racine.scene == EditorSceneManager.GetActiveScene()) return racine;
            racine = null;
            // sans groupe dans la scène, on ne le recherche pas à chaque image (la hiérarchie qui change relance la recherche)
            if (!creer && EditorApplication.timeSinceStartup - racineEssai < 1) return null;
            racineEssai = EditorApplication.timeSinceStartup;
            var scene = EditorSceneManager.GetActiveScene();
            var id = EditorPrefs.GetString(Pref + "racine." + scene.path, "");
            if (id != "" && GlobalObjectId.TryParse(id, out var gid))
                racine = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(gid) as GameObject;
            if (racine == null) racine = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "Décor");
            if (racine == null && creer)
            {
                racine = new GameObject("Décor");
                Undo.RegisterCreatedObjectUndo(racine, "Créer le groupe Décor");
            }
            return racine;
        }

        private void ChoisirRacine(GameObject r)
        {
            racine = r;
            var scene = EditorSceneManager.GetActiveScene();
            if (r == null) EditorPrefs.DeleteKey(Pref + "racine." + scene.path);
            else EditorPrefs.SetString(Pref + "racine." + scene.path, GlobalObjectId.GetGlobalObjectIdSlow(r).ToString());
        }

        /// <summary>Le groupe racine ▸ chemin…, créé au besoin (dans le geste d'annulation en cours).</summary>
        private Transform Groupe(params string[] chemin)
        {
            var t = Racine(true).transform;
            foreach (var nom in chemin)
            {
                var enfant = t.Find(nom);
                if (enfant == null)
                {
                    var go = new GameObject(nom);
                    Undo.RegisterCreatedObjectUndo(go, "Créer " + nom);
                    go.transform.SetParent(t, false);
                    enfant = go.transform;
                }
                t = enfant;
            }
            return t;
        }

        /// <summary>Un nouveau groupe sous parent, au nom unique (« Clôture de bambou 2 »).</summary>
        private static Transform NouveauGroupe(Transform parent, string nom)
        {
            string n = nom; int k = 2;
            while (parent.Find(n) != null) n = $"{nom} {k++}";
            var go = new GameObject(n);
            Undo.RegisterCreatedObjectUndo(go, "Créer " + n);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        /// <summary>
        /// Pose un préfabriqué (lien gardé) dans le geste d'annulation en cours : tourné de rot et grandi de taille par-dessus
        /// la rotation et l'échelle de sa racine (les FBX de Blender arrivent tournés de 90° sur X : les écraser couchait
        /// les lanternes et dressait les nénuphars, 2026-10-29).
        /// </summary>
        internal static GameObject Instancier(GameObject modele, Vector3 pos, Quaternion rot, Vector3 taille, Transform parent)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(modele, parent);
            go.transform.SetPositionAndRotation(pos, rot * modele.transform.localRotation);
            go.transform.localScale = Vector3.Scale(modele.transform.localScale, taille);
            Undo.RegisterCreatedObjectUndo(go, "Poser " + modele.name);
            return go;
        }

        /// <summary>Les objets allumés de la scène active posés depuis ces préfabriqués (pas ceux des versions gardées).</summary>
        internal static List<GameObject> Instances(ICollection<GameObject> modeles)
        {
            var l = new List<GameObject>();
            if (modeles.Count == 0) return l;
            foreach (var r in EditorSceneManager.GetActiveScene().GetRootGameObjects())
                foreach (var t in r.GetComponentsInChildren<Transform>(true))
                    if (t.gameObject.activeInHierarchy && PrefabUtility.IsOutermostPrefabInstanceRoot(t.gameObject)
                        && modeles.Contains(PrefabUtility.GetCorrespondingObjectFromSource(t.gameObject)))
                        l.Add(t.gameObject);
            return l;
        }

        /// <summary>La boîte d'un objet de la scène (tous ses rendus, sinon ses collisions, sinon sa position).</summary>
        internal static Bounds Boite(GameObject go)
        {
            bool vide = true; var b = new Bounds(go.transform.position, Vector3.zero);
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                if (r is ParticleSystemRenderer) continue;
                if (vide) { b = r.bounds; vide = false; } else b.Encapsulate(r.bounds);
            }
            if (vide)
                foreach (var c in go.GetComponentsInChildren<Collider>())
                    if (vide) { b = c.bounds; vide = false; } else b.Encapsulate(c.bounds);
            return b;
        }

        /// <summary>Un objet qu'on peut retirer du Maj+clic : un objet posé, pas le joueur ni une caméra.</summary>
        internal static GameObject Retirable(GameObject vise)
        {
            var o = ObjetDe(vise);
            if (o == null || o.GetComponentInChildren<Camera>(true) != null) return null;
            if (o.GetComponentsInChildren<Rigidbody>(true).Any(r => !r.isKinematic)) return null;
            return o;
        }
    }

    /// <summary>La barre « Bâtisseur » de la vue Scène : les cinq étapes, et dessous les gestes de l'outil pris.</summary>
    [Overlay(typeof(SceneView), "froggy-batisseur", "Bâtisseur d'arène", true)]
    internal class PanneauBatisseur : IMGUIOverlay
    {
        private static readonly string[] Courts = { "① Terrain", "② Jeu", "③ Équilibre", "④ Décor", "⑤ Tester" };

        public override void OnGUI()
        {
            var w = BatisseurArene.Instance;
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(330)))
            {
                using (new EditorGUILayout.HorizontalScope())
                    for (int i = 0; i < Courts.Length; i++)
                    {
                        bool ici = w != null && BatisseurArene.EtapeChoisie == (BatisseurArene.Etape)i;
                        var avant = GUI.backgroundColor;
                        if (ici && w.UnOutilPris) GUI.backgroundColor = new Color(.55f, .85f, .55f);
                        if (GUILayout.Button(Courts[i], ici ? EditorStyles.miniButtonMid : EditorStyles.miniButton))
                            (w != null ? w : BatisseurArene.Fenetre()).OuvrirEtape((BatisseurArene.Etape)i);
                        GUI.backgroundColor = avant;
                    }
                if (w != null && w.UnOutilPris) GUILayout.Label(w.Gestes(), EditorStyles.wordWrappedMiniLabel);
                bool aide = EditorPrefs.GetBool("Froggy.batisseur.aideTouches", false);
                bool n = GUILayout.Toggle(aide, aide ? "▾ Toutes les touches" : "▸ Toutes les touches", EditorStyles.miniLabel);
                if (n != aide) EditorPrefs.SetBool("Froggy.batisseur.aideTouches", n);
                if (n)
                    foreach (var (touche, quoi) in Touches)
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            GUILayout.Label(touche, EditorStyles.miniBoldLabel, GUILayout.Width(95));
                            GUILayout.Label(quoi, EditorStyles.wordWrappedMiniLabel);
                        }
            }
        }

        private static readonly (string, string)[] Touches =
        {
            ("Maj+1 … Maj+5", "ouvrir les étapes ① à ⑤"),
            ("Clic", "poser, semer, ajouter un point"),
            ("Maj+clic", "retirer ce qui est visé (Maj+glisser : effacer au pinceau)"),
            ("I+clic", "prendre en main l'objet visé"),
            ("A / E", "tourner à gauche / à droite ce qu'on tient"),
            ("T", "un autre tirage (taille, orientation, variante)"),
            ("M", "tampon en miroir"),
            ("Entrée", "poser le tracé"),
            ("Retour arrière", "retirer le dernier point de la courbe"),
            ("Échap", "arrêter l'outil (Tracer : effacer la courbe)"),
            ("Ctrl+Z", "annuler un geste entier"),
            ("F5", "jouer ici (la souris sur la vue Scène) ; en jeu : arrêter"),
        };
    }
}
