using System;
using System.Collections.Generic;
using System.Linq;
using Froggy.Arene;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Sorte = Froggy.Arene.RepereArene.Sorte;

namespace Froggy.Outils
{
    /// <summary>
    /// Apprendre en faisant : le bac à sable du Bâtisseur (menu Froggy ▸ Bâtisseur ▸ Apprendre, ou le bouton en tête de la
    /// fenêtre) ouvre une scène neuve, jamais enregistrée (un sol gris, un étang, Froggy), et cinq missions qui se cochent
    /// toutes seules : poser les joueurs en symétrie, poser les cookies, rendre l'arène équitable, traverser l'étang en
    /// nénuphars, garder une version et jouer. Chaque mission dit comment faire et a un bouton qui ouvre la bonne étape.
    /// Dix minutes pour savoir faire une arène, sans rien lire d'autre ; la scène où l'on était se rouvre à la fin.
    /// </summary>
    public partial class BatisseurArene
    {
        private const string TerrainBac = "Terrain du bac à sable";
        private const string SsSceneAvantBac = "Froggy.batisseur.avantBac";
        public const string SsAJoue = "Froggy.jouerIci.ajoue";

        public static bool EstBacASable() =>
            EditorSceneManager.GetActiveScene().GetRootGameObjects().Any(g => g.name == TerrainBac);

        [MenuItem("Froggy/Bâtisseur/Apprendre : le bac à sable (10 minutes)", priority = 21)]
        public static void Apprendre()
        {
            // la scène en cours : enregistrée si l'utilisateur le veut, rouverte à la fin
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            OuvrirBacASable();
            var w = Fenetre(); w.OuvrirEtape(Etape.Jeu); w.Focus();
        }

        /// <summary>Construit le bac à sable dans une scène neuve (sans demander : l'appelant a déjà proposé d'enregistrer).</summary>
        internal static void OuvrirBacASable()
        {
            var avant = EditorSceneManager.GetActiveScene().path;
            if (!string.IsNullOrEmpty(avant)) SessionState.SetString(SsSceneAvantBac, avant);
            if (!KitGris.Existe) KitGris.Creer();
            SessionState.SetBool(SsAJoue, false);
            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // le terrain : une arène de 36 × 28 m, un étang de 12 × 8 m au milieu (fond à -1 m, eau à -0,15 m)
            var terrain = new GameObject(TerrainBac);
            var sol = KitGris.MateriauSol(); var eau = KitGris.MateriauEau(); var bord = KitGris.MateriauBloque();
            void Dalle(string nom, Vector3 centre, Vector3 taille, Material m)
            {
                var go = new GameObject(nom); go.transform.SetParent(terrain.transform, false);
                go.transform.position = centre;
                var maillage = KitGris.Boite(taille, -taille.y);   // le dessus de la dalle à la hauteur du centre
                maillage.name = nom;
                go.AddComponent<MeshFilter>().sharedMesh = maillage;
                go.AddComponent<MeshRenderer>().sharedMaterial = m;
                var b = go.AddComponent<BoxCollider>(); b.center = new Vector3(0, -taille.y / 2, 0); b.size = taille;
            }
            Dalle("Sol ouest", new Vector3(-12, 0, 0), new Vector3(12, 1, 28), sol);
            Dalle("Sol est", new Vector3(12, 0, 0), new Vector3(12, 1, 28), sol);
            Dalle("Sol nord", new Vector3(0, 0, 9), new Vector3(12, 1, 10), sol);
            Dalle("Sol sud", new Vector3(0, 0, -9), new Vector3(12, 1, 10), sol);
            Dalle("Fond de l'étang", new Vector3(0, -1, 0), new Vector3(12, 1, 8), sol);
            // des murs autour, plus hauts qu'un saut : on ne tombe pas de l'arène
            Dalle("Bord nord", new Vector3(0, 2, 14.25f), new Vector3(36, 2, .5f), bord);
            Dalle("Bord sud", new Vector3(0, 2, -14.25f), new Vector3(36, 2, .5f), bord);
            Dalle("Bord ouest", new Vector3(-18.25f, 2, 0), new Vector3(.5f, 2, 29), bord);
            Dalle("Bord est", new Vector3(18.25f, 2, 0), new Vector3(.5f, 2, 29), bord);
            var plan = GameObject.CreatePrimitive(PrimitiveType.Quad);
            plan.name = "Eau de l'étang"; plan.transform.SetParent(terrain.transform, false);
            UnityEngine.Object.DestroyImmediate(plan.GetComponent<Collider>());
            plan.transform.SetPositionAndRotation(new Vector3(0, -.15f, 0), Quaternion.Euler(90, 0, 0));
            plan.transform.localScale = new Vector3(12, 8, 1);
            plan.GetComponent<MeshRenderer>().sharedMaterial = eau;

            // Froggy, pour jouer (la caméra du joueur remplace celle de la scène)
            var joueur = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Template/Prefabs/PlayerTemplate.prefab");
            if (joueur != null)
            {
                var j = (GameObject)PrefabUtility.InstantiatePrefab(joueur);
                j.transform.position = new Vector3(0, .1f, -10);
                var cam = GameObject.Find("Main Camera");
                if (cam != null && j.GetComponentInChildren<Camera>(true) != null) UnityEngine.Object.DestroyImmediate(cam);
            }
            var sv = SceneView.lastActiveSceneView;
            if (sv != null) sv.LookAt(Vector3.zero, Quaternion.Euler(55, 0, 0), 22);
        }

        // ---------- Les missions ----------

        private class Mission { public string titre, comment, bouton; public bool faite; public Action aller; }

        private List<Mission> Missions()
        {
            var reps = Reperes();
            int joueurs = reps.Where(r => r.sorte == Sorte.Joueur).Select(r => r.joueur).Distinct().Count();
            int cookies = reps.Count(r => r.sorte == Sorte.Cookie), boites = reps.Count(r => r.sorte == Sorte.Boite);
            var m = grille != null ? Mesurer(4) : null;
            float ecart = m != null ? m.Ecart(0) : float.NaN;
            var eau = GameObject.Find("Eau de l'étang");
            var b = eau != null ? eau.GetComponent<Renderer>().bounds : new Bounds();
            int nenuphars = UnityEngine.Object.FindObjectsByType<Transform>()
                .Count(t => t.name.Contains("Nenuphar") && PrefabUtility.IsOutermostPrefabInstanceRoot(t.gameObject)
                    && eau != null && t.position.x > b.min.x && t.position.x < b.max.x && t.position.z > b.min.z && t.position.z < b.max.z);
            var versions = ConteneurVersions(false);
            return new List<Mission>
            {
                new Mission
                {
                    titre = "Pose les apparitions des 4 joueurs", faite = joueurs >= 4,
                    comment = "À l'étape ② Jeu, choisis la symétrie « Quart de tour » (ou « Deux miroirs »), clique « ● J1 », puis clique dans un coin de l'arène. Les joueurs 2 à 4 se posent tout seuls, à la même distance du centre.",
                    bouton = "Ouvrir ② Jeu",
                    aller = () => { OuvrirEtape(Etape.Jeu); sorteRepere = Sorte.Joueur; joueurRepere = 1; if (SymetrieChoisie == CourbeArene.Symetrie.Aucune) SymetrieChoisie = CourbeArene.Symetrie.Rotation4; },
                },
                new Mission
                {
                    titre = "Pose au moins 4 cookies et une boîte à objets", faite = cookies >= 4 && boites >= 1,
                    comment = $"Toujours à l'étape ② Jeu : « Cookies », puis clique là où ils doivent apparaître ; avec la symétrie, un clic en pose quatre. Puis « Boîte ». ({cookies} cookie(s), {boites} boîte(s) pour l'instant.)",
                    bouton = "Poser des cookies", aller = () => { OuvrirEtape(Etape.Jeu); sorteRepere = Sorte.Cookie; },
                },
                new Mission
                {
                    titre = "Rends l'arène équitable", faite = !float.IsNaN(ecart) && ecart < .1f && m.inaccessibles.Count == 0,
                    comment = "À l'étape ③ Équilibre, lis « Ce que le Bâtisseur en pense » et les couleurs sur l'arène (qui arrive le premier où). " +
                        "Déplace un cookie ou une apparition (outil de déplacement d'Unity, W) jusqu'à moins de 10 % d'écart." +
                        (float.IsNaN(ecart) ? "" : $" Écart actuel : {ecart:P0}."),
                    bouton = "Ouvrir ③ Équilibre", aller = () => OuvrirEtape(Etape.Equilibre),
                },
                new Mission
                {
                    titre = "Traverse l'étang en nénuphars", faite = nenuphars >= 3,
                    comment = "À l'étape ① Terrain, onglet « Tracer (courbe) », choisis « Chemin de nénuphars ». Clique 3 ou 4 points sur l'eau, d'une rive à l'autre, puis Entrée. " +
                        "À l'étape ③ Équilibre, les sauts trop longs s'affichent en orange ou en rouge.",
                    bouton = "Ouvrir le tracé",
                    aller = () =>
                    {
                        if (traces == null) traces = Traces();
                        int i = traces.traces.FindIndex(t => t.nom == "Chemin de nénuphars");
                        if (i >= 0) TraceChoisi = i;
                        EtapeChoisie = Etape.Terrain; Prendre(Outil.Tracer); planTrace = null;
                    },
                },
                new Mission
                {
                    titre = "Garde une version, puis joue", faite = versions != null && versions.transform.childCount > 0 && SessionState.GetBool(SsAJoue, false),
                    comment = "À l'étape ⑤ Tester : « Garder cette version », puis « ▶ Jouer ici ». En jeu, va voir si les nénuphars se sautent bien ; F5 pour revenir.",
                    bouton = "Ouvrir ⑤ Tester", aller = () => OuvrirEtape(Etape.Tester),
                },
            };
        }

        private static GUIStyle missionFaite, missionEnCours;
        private static GUIStyle MissionFaite => missionFaite ??= new GUIStyle(EditorStyles.label) { normal = { textColor = new Color(.45f, .8f, .45f) } };
        private static GUIStyle MissionEnCours => missionEnCours ??= new GUIStyle(EditorStyles.boldLabel) { wordWrap = true };

        /// <summary>Le panneau des missions (à la place de la jauge, dans le bac à sable).</summary>
        private void MissionsGUI()
        {
            var missions = Missions();
            int faites = missions.Count(x => x.faite);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label($"Bac à sable : mission {Mathf.Min(faites + 1, missions.Count)} sur {missions.Count}", EditorStyles.boldLabel);
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button(new GUIContent("Quitter", "Rouvre la scène où tu étais (le bac à sable n'est pas enregistré)"), EditorStyles.miniButton, GUILayout.Width(60))) QuitterBacASable();
                }
                var courante = missions.FirstOrDefault(x => !x.faite);
                for (int i = 0; i < missions.Count; i++)
                {
                    var x = missions[i];
                    if (x == courante)
                    {
                        GUILayout.Label($"▶ {i + 1}. {x.titre}", MissionEnCours);
                        EditorGUILayout.LabelField(x.comment, EditorStyles.wordWrappedLabel);
                        if (GUILayout.Button(x.bouton, GUILayout.Height(22))) x.aller();
                    }
                    else GUILayout.Label($"{(x.faite ? "✓" : "  ")} {i + 1}. {x.titre}", x.faite ? MissionFaite : EditorStyles.miniLabel);
                }
                if (courante == null)
                {
                    EditorGUILayout.HelpBox("Bravo : tu sais faire une arène, la rendre juste et la tester. Pour la vraie : quitte le bac à sable, ouvre ou crée la scène de l'arène et suis la jauge « Prête pour un test ».", MessageType.Info);
                    if (GUILayout.Button("Recommencer le bac à sable")) { OuvrirBacASable(); OuvrirEtape(Etape.Jeu); }
                }
            }
        }

        private void QuitterBacASable()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var avant = SessionState.GetString(SsSceneAvantBac, "");
            if (!string.IsNullOrEmpty(avant) && System.IO.File.Exists(avant)) EditorSceneManager.OpenScene(avant, OpenSceneMode.Single);
            else EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            SessionState.EraseString(SsSceneAvantBac);
        }
    }
}
