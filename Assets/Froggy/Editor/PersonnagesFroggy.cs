using System.Collections.Generic;
using System.IO;
using System.Linq;
using Froggy.Personnages;
using Froggy.Template;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Froggy.Outils
{
    /// <summary>
    /// Construit, pour chaque dossier de Assets/Froggy/Personnages, un Animator Controller (Repos / Marche / Course par
    /// la vitesse, Saut, Victoire, puis un état par autre geste du modèle : torgnole, croassement, nage…), puis la scène
    /// Test_Personnages : une copie de la scène Template dont le joueur porte FroggyApparence avec tous les modèles.
    /// Relançable : les contrôleurs sont refaits, la scène recâblée.
    /// </summary>
    public static class PersonnagesFroggy
    {
        private const string SceneTemplate = "Assets/Froggy/Joueur/Scenes/Template.unity";
        public const string SceneTest = "Assets/Froggy/Scenes/Test_Personnages.unity";

        // Seuils du mélange, en m/s. PROVISOIRES : à régler en jeu avec les vraies vitesses de Froggy.
        private const float SeuilMarche = 2.5f;
        private const float SeuilCourse = 7f;

        /// <summary>Les clips qui ont leur place à part (mélange du déplacement, saut et victoire par paramètres).</summary>
        public static readonly string[] EtatsFixes = { "Repos", "Marche", "Course", "Saut", "Victoire" };

        [MenuItem("Froggy/Personnages/Construire la scène de test")]
        public static string Construire()
        {
            var modeles = Modeles();
            if (modeles.Count == 0) return "Aucun modèle trouvé dans " + ImportPersonnagesFroggy.Dossier;

            if (!File.Exists(SceneTest)) AssetDatabase.CopyAsset(SceneTemplate, SceneTest);
            var scene = EditorSceneManager.OpenScene(SceneTest, OpenSceneMode.Single);
            var joueur = Object.FindAnyObjectByType<TemplatePlayerController>();
            if (joueur == null) return "Pas de joueur dans " + SceneTest;
            Brancher(joueur, modeles);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return $"{SceneTest} : {string.Join(", ", modeles.Select(m => m.nom))}";
        }

        /// <summary>Un modèle par dossier de Assets/Froggy/Personnages qui a son Repos, avec son contrôleur refait.</summary>
        public static List<FroggyApparence.Modele> Modeles()
        {
            var modeles = new List<FroggyApparence.Modele>();
            foreach (string dossier in AssetDatabase.GetSubFolders(ImportPersonnagesFroggy.Dossier.TrimEnd('/')).OrderBy(d => d))
            {
                string nom = Path.GetFileName(dossier);
                var clips = new Dictionary<string, AnimationClip>();
                GameObject porteur = null;
                foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { dossier }))
                {
                    string chemin = AssetDatabase.GUIDToAssetPath(guid);
                    foreach (var clip in AssetDatabase.LoadAllAssetsAtPath(chemin).OfType<AnimationClip>())
                        if (!clip.name.StartsWith("__preview__")) clips[clip.name] = clip;
                    if (chemin.EndsWith("_Repos.fbx")) porteur = AssetDatabase.LoadAssetAtPath<GameObject>(chemin);
                }
                if (porteur != null) ExtraireTextures(AssetDatabase.GetAssetPath(porteur), dossier + "/Textures");
                if (porteur == null || !clips.ContainsKey("Repos")) continue;
                modeles.Add(new FroggyApparence.Modele
                {
                    nom = nom,
                    prefab = porteur,
                    animateur = Animateur($"{dossier}/{nom}.controller", clips),
                });
            }
            return modeles;
        }

        /// <summary>Pose FroggyApparence sur le joueur avec ces modèles (la forme du template est cachée sous un modèle).</summary>
        public static FroggyApparence Brancher(TemplatePlayerController joueur, List<FroggyApparence.Modele> modeles)
        {
            Transform visual = joueur.transform.Find("Visual");
            var apparence = joueur.GetComponent<FroggyApparence>();  // pas de ?? : faux null de Unity
            if (apparence == null) apparence = joueur.gameObject.AddComponent<FroggyApparence>();

            var so = new SerializedObject(apparence);
            so.FindProperty("visual").objectReferenceValue = visual;
            var liste = so.FindProperty("modeles");
            liste.arraySize = modeles.Count;
            for (int i = 0; i < modeles.Count; i++)
            {
                var e = liste.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("nom").stringValue = modeles[i].nom;
                e.FindPropertyRelative("prefab").objectReferenceValue = modeles[i].prefab;
                e.FindPropertyRelative("animateur").objectReferenceValue = modeles[i].animateur;
            }
            var rendus = visual != null ? visual.GetComponentsInChildren<Renderer>(true) : new Renderer[0];
            var cacher = so.FindProperty("aCacher");
            cacher.arraySize = rendus.Length;
            for (int i = 0; i < rendus.Length; i++) cacher.GetArrayElementAtIndex(i).objectReferenceValue = rendus[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            return apparence;
        }

        /// <summary>
        /// La texture est embarquée dans le FBX, mais Unity ne la branche qu'une fois extraite en fichier : on l'extrait
        /// (une fois) à côté du modèle qui s'affiche, et le réimport la retrouve par son nom.
        /// </summary>
        private static void ExtraireTextures(string fbx, string dossier)
        {
            if (Directory.Exists(dossier) && Directory.GetFiles(dossier, "*.png").Length > 0) return;
            Directory.CreateDirectory(dossier);
            var importeur = (ModelImporter)AssetImporter.GetAtPath(fbx);
            importeur.ExtractTextures(dossier);
            AssetDatabase.Refresh();
            AssetDatabase.ImportAsset(fbx, ImportAssetOptions.ForceUpdate);
        }

        private static AnimatorController Animateur(string chemin, Dictionary<string, AnimationClip> clips)
        {
            var ac = AssetDatabase.LoadAssetAtPath<AnimatorController>(chemin) ?? AnimatorController.CreateAnimatorControllerAtPath(chemin);
            foreach (var p in ac.parameters.ToArray()) ac.RemoveParameter(p);
            ac.AddParameter("Vitesse", AnimatorControllerParameterType.Float);
            ac.AddParameter("AuSol", AnimatorControllerParameterType.Bool);
            ac.AddParameter("Saut", AnimatorControllerParameterType.Trigger);
            ac.AddParameter("Victoire", AnimatorControllerParameterType.Trigger);

            var sm = ac.layers[0].stateMachine;
            foreach (var s in sm.states.ToArray()) sm.RemoveState(s.state);
            foreach (var t in sm.anyStateTransitions.ToArray()) sm.RemoveAnyStateTransition(t);
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(chemin).OfType<BlendTree>().ToArray())
                AssetDatabase.RemoveObjectFromAsset(o);

            var deplacement = ac.CreateBlendTreeInController("Deplacement", out BlendTree arbre, 0);
            arbre.blendParameter = "Vitesse";
            arbre.useAutomaticThresholds = false;
            arbre.AddChild(clips["Repos"], 0f);
            if (clips.TryGetValue("Marche", out var marche)) arbre.AddChild(marche, SeuilMarche);
            if (clips.TryGetValue("Course", out var course)) arbre.AddChild(course, SeuilCourse);
            sm.defaultState = deplacement;

            if (clips.TryGetValue("Saut", out var saut))
            {
                var etat = sm.AddState("Saut"); etat.motion = saut;
                var entree = sm.AddAnyStateTransition(etat);
                entree.AddCondition(AnimatorConditionMode.If, 0, "Saut");
                entree.canTransitionToSelf = false; entree.duration = 0.1f;
                var sortie = etat.AddTransition(deplacement);
                sortie.AddCondition(AnimatorConditionMode.If, 0, "AuSol");
                sortie.hasExitTime = true; sortie.exitTime = 0.3f; sortie.duration = 0.15f;
            }
            if (clips.TryGetValue("Victoire", out var victoire))
            {
                var etat = sm.AddState("Victoire"); etat.motion = victoire;
                var entree = sm.AddAnyStateTransition(etat);
                entree.AddCondition(AnimatorConditionMode.If, 0, "Victoire");
                entree.canTransitionToSelf = false; entree.duration = 0.15f;
                var fin = etat.AddTransition(deplacement); fin.hasExitTime = true; fin.exitTime = 1f; fin.duration = 0.25f;
                var interrompue = etat.AddTransition(deplacement);  // bouger interrompt la célébration
                interrompue.AddCondition(AnimatorConditionMode.Greater, 0.5f, "Vitesse"); interrompue.duration = 0.2f;
            }

            // Les autres gestes (torgnole, croassement, nage…) : un état chacun, du nom du clip, lancé par ce nom
            // (FroggyApparence.Jouer, menu de debug). Une boucle (nage, porter) tourne jusqu'à l'ordre suivant ; un geste
            // unique revient au déplacement à sa fin.
            int rang = 0;
            foreach (var paire in clips.OrderBy(p => p.Key))
            {
                if (EtatsFixes.Contains(paire.Key)) continue;
                var etat = sm.AddState(paire.Key, new Vector3(520f + 220f * (rang % 3), 60f * (rang / 3), 0f));
                etat.motion = paire.Value;
                rang++;
                if (paire.Value.isLooping) continue;
                var fin = etat.AddTransition(deplacement); fin.hasExitTime = true; fin.exitTime = 1f; fin.duration = 0.2f;
                if (paire.Key == "Atterrissage")
                {
                    // joué au retour au sol (FroggyApparence) : en courant, l'écrasement se voit (il culmine à 0,17 s),
                    // puis la course reprend, sinon les pieds glisseraient
                    var reprise = etat.AddTransition(deplacement);
                    reprise.AddCondition(AnimatorConditionMode.Greater, 1f, "Vitesse");
                    reprise.hasExitTime = true; reprise.exitTime = 0.35f; reprise.duration = 0.15f;
                }
            }
            EditorUtility.SetDirty(ac);
            AssetDatabase.SaveAssets();
            return ac;
        }
    }
}
