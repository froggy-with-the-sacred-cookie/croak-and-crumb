using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Froggy.Outils
{
    /// <summary>
    /// Vitrine du jardin japonais : prépare les préfabriqués du kit (variantes des FBX, matériau à couleurs de sommets,
    /// collisions), construit une petite scène d'après Assets/Froggy/Vitrine/disposition_jardin.json (écrit par
    /// Tools/blender/vitrine_jardin.py) et en prend des captures. Ce n'est pas l'arène : une scène pour juger le kit, qui
    /// se remanie ensuite à la main. Construire ne remplace jamais une scène existante (sauf -reconstruire en ligne de
    /// commande), pour ne pas effacer ce qu'on y a déplacé.
    /// En ligne de commande (éditeur fermé) : -executeMethod Froggy.Outils.VitrineJardin.ToutFaire
    /// </summary>
    public static class VitrineJardin
    {
        private const string Modeles = "Assets/Froggy/Models/JardinJaponais";
        private const string Prefabs = "Assets/Froggy/Prefabs/JardinJaponais";
        private const string Materiaux = "Assets/Froggy/Materials";
        private const string Scene = "Assets/Froggy/Scenes/Vitrine_JardinJaponais.unity";
        private const string Disposition = "Assets/Froggy/Vitrine/disposition_jardin.json";
        private const string Joueur = "Assets/Template/Prefabs/PlayerTemplate.prefab";
        private const string DossierCaptures = "Captures/vitrine";
        private static readonly Color Ciel = new Color(0.70f, 0.82f, 0.90f);

        // ce qu'on traverse : fleurs, pétales, herbes, galets, l'eau elle-même, le bosquet de bambous (feuilles)
        private static readonly HashSet<string> SansCollision = new HashSet<string>
            { "Fleur_Nenuphar", "Petales_Sol", "Iris", "Galets", "Vitrine_Eau", "Bambous" };

        private static readonly HashSet<string> Feuillages = new HashSet<string>
            { "Cerisier_1", "Cerisier_2", "Erable_Momiji", "Pin_Niwaki", "Bambous" };

        [System.Serializable]
        private class Element { public string nom; public float[] pos; public float lacet; public float echelle; }

        [System.Serializable]
        private class Vue { public string nom; public float[] pos; public float[] cible; }

        [System.Serializable]
        private class Dispo
        {
            public Element[] elements;
            public float[] depart;
            public float[] bornes;
            public float[] controle;
            public Vue[] vues;
        }

        [MenuItem("Froggy/Vitrine du jardin japonais/1. Préparer matériaux et préfabriqués")]
        public static void Preparer()
        {
            Dossier(Materiaux);
            Dossier(Prefabs);
            var couleurs = Materiau("CouleurSommet", "Froggy/CouleurSommet", null);
            // un feuillage laisse passer la lumière : son dessous ne prend pas l'ambiante brune du sol (cerisiers mauves)
            var feuillage = Materiau("CouleurSommet_Feuillage", "Froggy/CouleurSommet", m =>
            {
                m.SetColor("_Sol", new Color(0.66f, 0.58f, 0.60f));
                m.SetFloat("_Douceur", 0.85f);
            });
            var eau = Materiau("Eau_Vitrine", "Froggy/EauSimple", null);
            System.Func<string, Material> materiauDe = nom =>
                nom == "Vitrine_Eau" ? eau : Feuillages.Contains(nom) ? feuillage : couleurs;
            var faits = new List<string>();
            var repeints = new List<string>();
            foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { Modeles }))
            {
                var chemin = AssetDatabase.GUIDToAssetPath(guid);
                var nom = Path.GetFileNameWithoutExtension(chemin);
                var cible = $"{Prefabs}/{nom}.prefab";
                if (File.Exists(cible))
                {
                    // une variante suit déjà son FBX : la refaire casserait les références ; on ne corrige que le
                    // matériau, et seulement s'il est l'un des nôtres (un choix fait à la main reste)
                    var contenu = PrefabUtility.LoadPrefabContents(cible);
                    var change = false;
                    foreach (var rendu in contenu.GetComponentsInChildren<MeshRenderer>())
                    {
                        var voulu = materiauDe(nom);
                        var actuel = rendu.sharedMaterial;
                        if (actuel == voulu || (actuel != null && actuel != couleurs && actuel != feuillage)) continue;
                        rendu.sharedMaterial = voulu;
                        change = true;
                    }
                    if (change) { PrefabUtility.SaveAsPrefabAsset(contenu, cible); repeints.Add(nom); }
                    PrefabUtility.UnloadPrefabContents(contenu);
                    continue;
                }
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(chemin));
                foreach (var rendu in instance.GetComponentsInChildren<MeshRenderer>())
                {
                    rendu.sharedMaterial = materiauDe(nom);
                    if (nom == "Vitrine_Eau" || nom == "Petales_Sol") rendu.shadowCastingMode = ShadowCastingMode.Off;
                    if (!SansCollision.Contains(nom)) rendu.gameObject.AddComponent<MeshCollider>();
                }
                PrefabUtility.SaveAsPrefabAsset(instance, cible);
                Object.DestroyImmediate(instance);
                faits.Add(nom);
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[Vitrine] {faits.Count} préfabriqués créés : {string.Join(", ", faits)} ; " +
                      $"matériau corrigé : {string.Join(", ", repeints)}");
        }

        [MenuItem("Froggy/Vitrine du jardin japonais/2. Construire la scène (si elle n'existe pas)")]
        public static void ConstruireMenu() => Construire(false);

        public static void Construire(bool remplacer)
        {
            if (File.Exists(Scene) && !remplacer)
            {
                Debug.LogWarning($"[Vitrine] {Scene} existe déjà : elle n'est pas reconstruite (elle se retouche à la main).");
                return;
            }
            var dispo = Lire();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var soleil = new GameObject("Soleil").AddComponent<Light>();
            soleil.type = LightType.Directional;
            soleil.color = new Color(1f, 0.95f, 0.86f);
            soleil.intensity = 1.1f;
            soleil.shadows = LightShadows.Soft;
            soleil.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
            RenderSettings.sun = soleil;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.62f, 0.70f, 0.80f);
            RenderSettings.ambientEquatorColor = new Color(0.55f, 0.60f, 0.58f);
            RenderSettings.ambientGroundColor = new Color(0.40f, 0.38f, 0.32f);
            RenderSettings.skybox = null;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = Ciel;
            RenderSettings.fogStartDistance = 30f;
            RenderSettings.fogEndDistance = 110f;

            var jardin = new GameObject("Jardin").transform;
            var manquants = new List<string>();
            foreach (var e in dispo.elements)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{Prefabs}/{e.nom}.prefab");
                if (prefab == null) { manquants.Add(e.nom); continue; }
                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, jardin);
                // la racine d'un FBX de Blender garde +90° sur x (même avec bakeAxisConversion) : le lacet s'y ajoute
                go.transform.SetPositionAndRotation(V(e.pos), Quaternion.Euler(0f, e.lacet, 0f) * prefab.transform.rotation);
                go.transform.localScale = prefab.transform.localScale * e.echelle;
            }
            foreach (var nom in new[] { "Vitrine_Sol", "Vitrine_Eau" })
                PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>($"{Prefabs}/{nom}.prefab"), jardin);

            // murs invisibles au bord du terrain
            var murs = new GameObject("Murs invisibles").transform;
            float x0 = dispo.bornes[0], x1 = dispo.bornes[1], z0 = dispo.bornes[2], z1 = dispo.bornes[3];
            Mur(murs, new Vector3(x0, 2f, (z0 + z1) / 2), new Vector3(0.5f, 6f, z1 - z0));
            Mur(murs, new Vector3(x1, 2f, (z0 + z1) / 2), new Vector3(0.5f, 6f, z1 - z0));
            Mur(murs, new Vector3((x0 + x1) / 2, 2f, z0), new Vector3(x1 - x0, 6f, 0.5f));
            Mur(murs, new Vector3((x0 + x1) / 2, 2f, z1), new Vector3(x1 - x0, 6f, 0.5f));

            var joueur = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Joueur));
            joueur.transform.position = V(dispo.depart) + Vector3.up * 0.6f;

            Dossier(Path.GetDirectoryName(Scene));
            EditorSceneManager.SaveScene(scene, Scene);
            Debug.Log($"[Vitrine] Scène construite : {dispo.elements.Length} éléments" +
                      (manquants.Count > 0 ? $", préfabriqués manquants : {string.Join(", ", manquants)}" : ""));
            Controler(dispo);
        }

        [MenuItem("Froggy/Vitrine du jardin japonais/3. Capturer les vues")]
        public static void Capturer()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[Vitrine] Une partie tourne : pas de capture.");
                return;
            }
            var dispo = Lire();
            EditorSceneManager.OpenScene(Scene, OpenSceneMode.Single);
            var asynchrone = EditorSettings.asyncShaderCompilation;
            EditorSettings.asyncShaderCompilation = false;   // sinon des objets sortent en cyan
            Directory.CreateDirectory(DossierCaptures);
            var cam = new GameObject("Camera de capture").AddComponent<Camera>();
            cam.fieldOfView = 50f;
            cam.farClipPlane = 300f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Ciel;
            var rt = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { antiAliasing = 4 };
            var image = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            try
            {
                foreach (var vue in dispo.vues)
                {
                    cam.transform.position = V(vue.pos);
                    cam.transform.LookAt(V(vue.cible));
                    var demande = new RenderPipeline.StandardRequest { destination = rt };
                    if (RenderPipeline.SupportsRenderRequest(cam, demande)) RenderPipeline.SubmitRenderRequest(cam, demande);
                    else { cam.targetTexture = rt; cam.Render(); cam.targetTexture = null; }
                    RenderTexture.active = rt;
                    image.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                    image.Apply();
                    RenderTexture.active = null;
                    File.WriteAllBytes($"{DossierCaptures}/{vue.nom}.jpg", image.EncodeToJPG(90));
                }
                Debug.Log($"[Vitrine] {dispo.vues.Length} captures dans {Path.GetFullPath(DossierCaptures)}");
            }
            finally
            {
                Object.DestroyImmediate(cam.gameObject);
                Object.DestroyImmediate(image);
                rt.Release();
                EditorSettings.asyncShaderCompilation = asynchrone;
            }
        }

        /// <summary>Les trois étapes, pour la ligne de commande (éditeur fermé).</summary>
        public static void ToutFaire()
        {
            Preparer();
            Construire(System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-reconstruire") >= 0);
            Capturer();
        }

        // Deux points où le sol doit être à une hauteur connue (fond de l'étang, sommet de la butte) : si les axes de
        // Blender étaient mal convertis, le rayon tomberait ailleurs.
        private static void Controler(Dispo dispo)
        {
            Physics.SyncTransforms();
            for (int i = 0; i + 2 < dispo.controle.Length; i += 3)
            {
                var p = new Vector3(dispo.controle[i], 20f, dispo.controle[i + 2]);
                var attendu = dispo.controle[i + 1];
                float? trouve = null;   // seul le sol compte (un nénuphar ou un arbre peut être au-dessus)
                foreach (var hit in Physics.RaycastAll(p, Vector3.down, 40f))
                    if (hit.collider.name.StartsWith("Vitrine_Sol")) trouve = hit.point.y;
                var ok = trouve.HasValue && Mathf.Abs(trouve.Value - attendu) < 0.15f;
                var message = $"[Vitrine] Contrôle en ({p.x:0.0}, {p.z:0.0}) : sol attendu à {attendu:0.00} m, " +
                              (trouve.HasValue ? $"trouvé à {trouve.Value:0.00} m" : "sol non touché");
                if (ok) Debug.Log(message); else Debug.LogError(message);
            }
        }

        private static Dispo Lire() => JsonUtility.FromJson<Dispo>(File.ReadAllText(Disposition));

        private static Vector3 V(float[] p) => new Vector3(p[0], p[1], p[2]);

        private static void Mur(Transform parent, Vector3 centre, Vector3 taille)
        {
            var mur = new GameObject("Mur");
            mur.transform.SetParent(parent);
            mur.transform.position = centre;
            mur.AddComponent<BoxCollider>().size = taille;
        }

        private static Material Materiau(string nom, string shader, System.Action<Material> regler)
        {
            var chemin = $"{Materiaux}/{nom}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(chemin);
            if (mat != null) return mat;   // un matériau existant garde ses réglages
            mat = new Material(Shader.Find(shader));
            regler?.Invoke(mat);
            AssetDatabase.CreateAsset(mat, chemin);
            return mat;
        }

        private static void Dossier(string chemin)
        {
            if (AssetDatabase.IsValidFolder(chemin)) return;
            Dossier(Path.GetDirectoryName(chemin).Replace('\\', '/'));
            AssetDatabase.CreateFolder(Path.GetDirectoryName(chemin).Replace('\\', '/'), Path.GetFileName(chemin));
        }
    }
}
