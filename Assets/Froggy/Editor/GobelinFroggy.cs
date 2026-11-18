using System.Linq;
using Froggy.Arene;
using Froggy.Regles;
using Froggy.Socle;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Froggy.Outils
{
    /// <summary>
    /// Construit le gobelin du cookie sacré (P5-08, P7-07) et sa scène de test, depuis les fichiers du dossier Gobelin
    /// (modèle et clips faits par script Blender : kappa_squelette.py, kappa_animations.py, import en générique) :
    /// le contrôleur (Apparition, puis Course en boucle ; Attrape sur déclencheur), le préfabriqué Gobelin_Kappa avec le
    /// point « Mains » où le cookie sacré se tient, les réglages du cookie sacré, et la scène Test_CookieSacre (poste 5) :
    /// le jardin (DecorDeTest), une partie de 90 s (le gobelin arrive à 30 s), des cookies, le départ du gobelin, la
    /// touche K pour lâcher le cookie sacré. Relancer le menu refait tout ; la scène de test est refaite à neuf.
    /// </summary>
    public static class GobelinFroggy
    {
        private const string Dossier = "Assets/Froggy/Gobelin/";
        private const string Modele = Dossier + "Kappa_Modele.fbx";
        private const string Controleur = Dossier + "Kappa.controller";
        private const string Prefabrique = Dossier + "Gobelin_Kappa.prefab";
        private const string Poste = "Assets/Froggy/Postes/5_MeteoEvenements/";
        private const string Reglages = Poste + "ReglagesCookieSacre.asset";
        private const string ReglagesPartieEssai = Poste + "ReglagesPartie_CookieSacre.asset";
        private const string Scene = Poste + "Test_CookieSacre.unity";
        private const string CookieSacre = "Assets/Froggy/Prefabs/Objets/Cookie_Sacre.prefab";
        private const string Cookie = "Assets/Froggy/Prefabs/Objets/Cookie.prefab";
        private const string ReglagesCookies = "Assets/Froggy/Postes/3_ReglesBots/ReglagesCookies.asset";

        // entre ses mains, dans le repère du modèle au repos (il regarde vers +Z) : mesuré sur la pose « cookie serré »
        // de kappa_animations.py (Blender (0 ; -0,115 ; 0,42) → Unity (0 ; 0,42 ; 0,115))
        private static readonly Vector3 PointMains = new Vector3(0f, 0.42f, 0.115f);

        [MenuItem("Froggy/Gobelin/Construire le gobelin et sa scène de test")]
        public static string Construire()
        {
            var controleur = FaireControleur();
            var prefab = FairePrefabrique(controleur);
            var reglages = FaireReglages();
            string scene = FaireScene(prefab, reglages);
            return $"{Prefabrique} ; {scene}";
        }

        public static AnimatorController FaireControleur()
        {
            AnimationClip Clip(string nom) => AssetDatabase.LoadAllAssetsAtPath(Dossier + "Kappa_" + nom + ".fbx")
                .OfType<AnimationClip>().First(c => !c.name.StartsWith("__preview"));
            var ac = AssetDatabase.LoadAssetAtPath<AnimatorController>(Controleur) ?? AnimatorController.CreateAnimatorControllerAtPath(Controleur);
            foreach (var p in ac.parameters.ToArray()) ac.RemoveParameter(p);
            ac.AddParameter(new AnimatorControllerParameter { name = "Vitesse", type = AnimatorControllerParameterType.Float, defaultFloat = 1f });
            ac.AddParameter("Attrape", AnimatorControllerParameterType.Trigger);
            var sm = ac.layers[0].stateMachine;
            foreach (var s in sm.states.ToArray()) sm.RemoveState(s.state);
            foreach (var t in sm.anyStateTransitions.ToArray()) sm.RemoveAnyStateTransition(t);

            var apparition = sm.AddState("Apparition"); apparition.motion = Clip("Apparition");
            var course = sm.AddState("Course"); course.motion = Clip("Course");
            course.speedParameter = "Vitesse"; course.speedParameterActive = true;   // il trottine plus lentement sans peur
            var attrape = sm.AddState("Attrape"); attrape.motion = Clip("Attrape");
            sm.defaultState = apparition;
            var versCourse = apparition.AddTransition(course); versCourse.hasExitTime = true; versCourse.exitTime = 0.95f; versCourse.duration = 0.15f;
            var versAttrape = sm.AddAnyStateTransition(attrape);
            versAttrape.AddCondition(AnimatorConditionMode.If, 0, "Attrape");
            versAttrape.canTransitionToSelf = false; versAttrape.duration = 0.1f;
            EditorUtility.SetDirty(ac); AssetDatabase.SaveAssets();
            return ac;
        }

        public static GameObject FairePrefabrique(AnimatorController controleur)
        {
            var modele = AssetDatabase.LoadAssetAtPath<GameObject>(Modele);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(modele);
            try
            {
                go.name = "Gobelin_Kappa";
                go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                var animateur = go.GetComponent<Animator>();
                if (animateur == null) animateur = go.AddComponent<Animator>();   // pas de ?? : faux null de Unity
                animateur.runtimeAnimatorController = controleur;
                animateur.applyRootMotion = false;

                var torse = go.GetComponentsInChildren<Transform>().First(t => t.name == "Spine1");
                var mains = new GameObject("Mains").transform;
                mains.SetParent(torse, false);
                mains.SetPositionAndRotation(PointMains, Quaternion.identity);
                Vector3 e = torse.lossyScale;   // les os du FBX sont à l'échelle 100 : le point reste à l'échelle 1
                mains.localScale = new Vector3(1f / e.x, 1f / e.y, 1f / e.z);

                var fuite = go.GetComponent<FuiteGobelin>();
                if (fuite == null) fuite = go.AddComponent<FuiteGobelin>();
                var so = new SerializedObject(fuite);
                so.FindProperty("mains").objectReferenceValue = mains;
                so.ApplyModifiedPropertiesWithoutUndo();
                return PrefabUtility.SaveAsPrefabAsset(go, Prefabrique);
            }
            finally { Object.DestroyImmediate(go); }
        }

        public static ReglagesCookieSacre FaireReglages()
        {
            var r = AssetDatabase.LoadAssetAtPath<ReglagesCookieSacre>(Reglages);
            if (r != null) return r;
            r = ScriptableObject.CreateInstance<ReglagesCookieSacre>();
            AssetDatabase.CreateAsset(r, Reglages);
            return r;
        }

        public static string FaireScene(GameObject gobelin, ReglagesCookieSacre reglages)
        {
            var partieEssai = AssetDatabase.LoadAssetAtPath<ReglagesPartie>(ReglagesPartieEssai);
            if (partieEssai == null)
            {
                partieEssai = ScriptableObject.CreateInstance<ReglagesPartie>();
                partieEssai.decompte = 3f; partieEssai.duree = 90f;   // le gobelin arrive à 30 s de jeu
                AssetDatabase.CreateAsset(partieEssai, ReglagesPartieEssai);
            }
            string avant = SceneManager.GetActiveScene().path;
            if (SceneManager.GetActiveScene().isDirty) return "la scène ouverte a des modifications : enregistrer d'abord";
            AssetDatabase.SaveAssets();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            // ouvrir une scène décharge les réglages qu'on vient de créer : on les relit sur le disque
            reglages = AssetDatabase.LoadAssetAtPath<ReglagesCookieSacre>(Reglages);
            partieEssai = AssetDatabase.LoadAssetAtPath<ReglagesPartie>(ReglagesPartieEssai);
            gobelin = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabrique);

            new GameObject("Décor de test").AddComponent<DecorDeTest>();

            var partie = new GameObject("Partie");
            var cookies = partie.AddComponent<PartieCookies>();
            Regler(cookies, ("reglages", AssetDatabase.LoadAssetAtPath<Object>(ReglagesCookies)),
                ("visuel", AssetDatabase.LoadAssetAtPath<Object>(Cookie)));
            var deroulement = partie.AddComponent<DeroulementPartie>();
            Regler(deroulement, ("reglages", partieEssai), ("cookies", cookies));
            // au départ de Froggy dans le jardin, sur le sol plat (comme Test_ReglesBots) ; le gobelin sur l'herbe dégagée,
            // à 8 m devant la caméra du joueur (vérifié en jeu le 18/11/2026 : rien autour à 1,5 m)
            var reperes = new GameObject("Repères").transform;
            Vector3[] cookiesAuSol = { new Vector3(-1.5f, 0f, -10.5f), new Vector3(-5.5f, 0f, -10.5f), new Vector3(-3.5f, 0f, -8f), new Vector3(-3.5f, 0f, -13f) };
            for (int i = 0; i < cookiesAuSol.Length; i++) Repere(reperes, $"Cookies {i + 1}", RepereArene.Sorte.Cookie, cookiesAuSol[i]);
            var depart = Repere(reperes, "Départ du gobelin", RepereArene.Sorte.Gobelin, new Vector3(4f, 0f, -6f));

            var sacre = partie.AddComponent<PartieCookieSacre>();
            Regler(sacre, ("reglages", reglages), ("gobelin", gobelin), ("cookieSacre", AssetDatabase.LoadAssetAtPath<Object>(CookieSacre)),
                ("depart", depart.transform), ("deroulement", deroulement), ("cookies", cookies));
            var essai = partie.AddComponent<EssaiCookieSacre>();
            Regler(essai, ("sacre", sacre), ("cookies", cookies));

            EditorSceneManager.SaveScene(scene, Scene);
            if (!string.IsNullOrEmpty(avant) && avant != Scene) EditorSceneManager.OpenScene(avant, OpenSceneMode.Single);
            return Scene;
        }

        private static RepereArene Repere(Transform parent, string nom, RepereArene.Sorte sorte, Vector3 position)
        {
            var go = new GameObject(nom);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            var r = go.AddComponent<RepereArene>();
            r.sorte = sorte;
            return r;
        }

        private static void Regler(Object cible, params (string champ, Object valeur)[] champs)
        {
            var so = new SerializedObject(cible);
            foreach (var (champ, valeur) in champs) so.FindProperty(champ).objectReferenceValue = valeur;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
