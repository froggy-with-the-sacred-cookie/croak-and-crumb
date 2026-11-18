using System.Collections.Generic;
using System.IO;
using System.Linq;
using Froggy.Jardin;
using Froggy.Template;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Froggy.Outils
{
    /// <summary>
    /// Vitrine du jardin japonais : prépare les préfabriqués du kit (variantes des FBX, matériau à couleurs de sommets,
    /// collisions), les anime (shishi-odoshi, pétales, nénuphars qui flottent), construit une petite scène d'après
    /// Assets/Froggy/Vitrine/disposition_jardin.json (écrit par Tools/blender/vitrine_jardin.py), l'habille (ciel, soleil,
    /// vent, vraie eau, post-traitement, avec bascule vers l'ancien rendu) et en prend des captures. Ce n'est pas l'arène :
    /// une scène pour juger le kit, qui se remanie à la main. Construire ne remplace jamais une scène existante (sauf
    /// -reconstruire en ligne de commande) ; les autres étapes complètent sans rien défaire.
    /// La muraille d'enceinte, la caméra à la World of Warcraft et son menu de debug se posent ensuite (étapes 6 à 8),
    /// puis le choix du contour, les Froggy et la voltige de sakura de Codex dans ce menu (étapes 9 à 11).
    /// La vitrine agrandie (52 × 40 m, VITRINE=grande dans les scripts de Blender) a sa propre scène, sa disposition et sa
    /// muraille (fichiers en _grande) : la vitrine d'origine reste telle quelle, pour comparer. La vitrine « chine »
    /// (VITRINE=chine) est la vitrine agrandie posée sur un talus, bordée de la muraille de Chine (fichiers en _chine).
    /// En ligne de commande (éditeur fermé) : -executeMethod Froggy.Outils.VitrineJardin.ToutFaire [-grande | -chine]
    /// </summary>
    public static class VitrineJardin
    {
        private const string Modeles = "Assets/Froggy/Models/JardinJaponais";
        private const string Prefabs = "Assets/Froggy/Prefabs/JardinJaponais";
        private const string Materiaux = "Assets/Froggy/Materials";
        private const string Rendu = "Assets/Froggy/Rendu";
        private const string Sakura = "Assets/Froggy/Effets/Sakura";   // le kit de pétales de Codex
        // "" : la vitrine d'origine ; "_grande" : la vitrine agrandie ; "_chine" : agrandie, sur talus, muraille de Chine.
        // Les étapes sont les mêmes, les fichiers changent.
        private static string variante = "";
        private static string Scene =>
            $"Assets/Froggy/Scenes/Vitrine_JardinJaponais{(variante == "" ? "" : "_" + char.ToUpper(variante[1]) + variante.Substring(2))}.unity";
        private static string Disposition => $"Assets/Froggy/Vitrine/disposition_jardin{variante}.json";
        private static string Muraille => $"Assets/Froggy/Vitrine/muraille_jardin{variante}.json";
        private static string MurailleArtisanale => $"Assets/Froggy/Vitrine/muraille_jardin{variante}_artisan.json";
        private static string BordureBambous => $"Assets/Froggy/Vitrine/muraille_jardin{variante}_bambous.json";
        private const string Joueur = "Assets/Template/Prefabs/PlayerTemplate.prefab";
        private static string DossierCaptures => $"Captures/vitrine{variante}";
        private static readonly Color Ciel = new Color(0.70f, 0.82f, 0.90f);

        // ce qu'on traverse : fleurs, pétales, herbes, galets, l'eau elle-même, le bosquet de bambous (feuilles), les
        // plantes basses et la mousse
        private static readonly HashSet<string> SansCollision = new HashSet<string>
            { "Fleur_Nenuphar", "Petales_Sol", "Iris", "Galets", "Vitrine_Eau", "Bambous", "Hosta", "Fougere", "Mousse_Plaque",
              "Bordure_Bambous_1", "Bordure_Bambous_2", "Bordure_Bambous_Angle" };   // les murs invisibles tiennent le bord

        private static readonly HashSet<string> Feuillages = new HashSet<string>
            { "Cerisier_1", "Cerisier_2", "Erable_Momiji", "Pin_Niwaki", "Bambous",
              "Karikomi_Petit", "Karikomi_Moyen", "Karikomi_Fleuri", "O_Karikomi", "Hosta", "Fougere",
              "Bordure_Bambous_1", "Bordure_Bambous_2", "Bordure_Bambous_Angle" };

        private static readonly HashSet<string> Flottants = new HashSet<string>
            { "Nenuphar_Grand", "Nenuphar_Moyen", "Nenuphar_Petit", "Fleur_Nenuphar" };

        [System.Serializable]
        private class Element { public string nom; public float[] pos; public float lacet; public float echelle; }

        [System.Serializable]
        private class Vue { public string nom; public float[] pos; public float[] cible; }

        [System.Serializable]
        private class Dispo
        {
            public Element[] elements;
            public float[] depart;
            public string sol, eau;   // les préfabriqués du terrain (vides dans la disposition d'origine)
            public float[] bornes;
            public float[] controle;
            public Vue[] vues;
        }

        [System.Serializable]
        private class PieceMuraille { public string nom; public float[] pos; public float lacet; }

        [System.Serializable]
        private class PoseMuraille { public PieceMuraille[] pieces; }

        private class Mats
        {
            public Material couleurs, feuillage, flottant, eauVitrine, eauJardin, ciel, petale, goutte;
            public Material[] nos => new[] { couleurs, feuillage, flottant };
        }

        private static Mats Materiaux_()
        {
            Dossier(Materiaux);
            return new Mats
            {
                couleurs = Materiau("CouleurSommet", "Froggy/CouleurSommet", null),
                // un feuillage laisse passer la lumière : son dessous ne prend pas l'ambiante brune du sol (cerisiers mauves)
                feuillage = Materiau("CouleurSommet_Feuillage", "Froggy/CouleurSommet", m =>
                {
                    m.SetColor("_Sol", new Color(0.66f, 0.58f, 0.60f));
                    m.SetFloat("_Douceur", 0.85f);
                }),
                flottant = Materiau("CouleurSommet_Flottant", "Froggy/CouleurSommet", m => m.SetFloat("_Flotter", 1f)),
                eauVitrine = Materiau("Eau_Vitrine", "Froggy/EauSimple", null),
                eauJardin = Materiau("Eau_Jardin", "Froggy/Eau", null),
                ciel = Materiau("Ciel_Jardin", "Froggy/Ciel", null),
                petale = Materiau("Petale_Cerisier", "Froggy/Petale", null),
                goutte = Materiau("Goutte", "Froggy/Goutte", null),
            };
        }

        [MenuItem("Froggy/Vitrine du jardin japonais/1. Préparer matériaux et préfabriqués")]
        public static void Preparer()
        {
            Dossier(Prefabs);
            var m = Materiaux_();
            System.Func<string, Material> materiauDe = nom =>
                nom.StartsWith("Vitrine_Eau") ? m.eauVitrine : Feuillages.Contains(nom) ? m.feuillage : Flottants.Contains(nom) ? m.flottant : m.couleurs;
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
                    var change = Modifier(cible, contenu =>
                    {
                        var c = false;
                        foreach (var rendu in contenu.GetComponentsInChildren<MeshRenderer>())
                        {
                            var voulu = materiauDe(nom);
                            var actuel = rendu.sharedMaterial;
                            if (actuel == voulu || (actuel != null && !m.nos.Contains(actuel))) continue;
                            rendu.sharedMaterial = voulu;
                            c = true;
                        }
                        return c;
                    });
                    if (change) repeints.Add(nom);
                    continue;
                }
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(chemin));
                foreach (var rendu in instance.GetComponentsInChildren<MeshRenderer>())
                {
                    rendu.sharedMaterial = materiauDe(nom);
                    if (nom.StartsWith("Vitrine_Eau") || nom == "Petales_Sol") rendu.shadowCastingMode = ShadowCastingMode.Off;
                    if (!SansCollision.Contains(nom) && !nom.StartsWith("Vitrine_Eau")) rendu.gameObject.AddComponent<MeshCollider>();
                }
                PrefabUtility.SaveAsPrefabAsset(instance, cible);
                Object.DestroyImmediate(instance);
                faits.Add(nom);
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[Vitrine] {faits.Count} préfabriqués créés : {string.Join(", ", faits)} ; " +
                      $"matériau corrigé : {string.Join(", ", repeints)}");
        }

        [MenuItem("Froggy/Vitrine du jardin japonais/2. Animer les préfabriqués (shishi-odoshi, pétales)")]
        public static void Animer()
        {
            var m = Materiaux_();
            var petale = MaillagePetale();
            var faits = new List<string>();

            if (Modifier($"{Prefabs}/ShishiOdoshi.prefab", racine =>
            {
                if (racine.GetComponent<ShishiOdoshi>() != null) return false;
                var bascule = racine.GetComponentsInChildren<Transform>().First(t => t.name == "ShishiOdoshi_Bascule");
                // repères du modèle (Blender x, y, z → Unity x, z, y) : bout du bec d'eau, bouche du tube au repos
                var filet = Particules(racine.transform, "Filet", new Vector3(0f, 0.68f, 0.40f), Quaternion.LookRotation(Vector3.down));
                Gouttes(filet, m.goutte, 0.3f, 0.35f, new Vector2(0.018f, 0.03f));
                var em = filet.emission; em.rateOverTime = 40f;
                var forme = filet.shape; forme.shapeType = ParticleSystemShapeType.Cone; forme.angle = 3f; forme.radius = 0.008f;
                var verse = Particules(bascule, "Verse", new Vector3(0f, 0.536f, 0.32f), Quaternion.identity);
                Gouttes(verse, m.goutte, 0.7f, 0.5f, new Vector2(0.03f, 0.05f));
                var em2 = verse.emission; em2.rateOverTime = 0f;   // le script émet pendant le vidage
                var forme2 = verse.shape; forme2.shapeType = ParticleSystemShapeType.Sphere; forme2.radius = 0.03f;
                var so = new SerializedObject(racine.AddComponent<ShishiOdoshi>());
                so.FindProperty("bascule").objectReferenceValue = bascule;
                so.FindProperty("verse").objectReferenceValue = verse;
                so.ApplyModifiedPropertiesWithoutUndo();
                return true;
            })) faits.Add("ShishiOdoshi");

            foreach (var nom in new[] { "Cerisier_1", "Cerisier_2" })
            {
                if (Modifier($"{Prefabs}/{nom}.prefab", racine =>
                {
                    var existant = racine.transform.Find("Petales_Chute");
                    if (existant != null) { RéglerPetales(existant.GetComponent<ParticleSystem>(), m.petale, petale); return true; }
                    var ps = Particules(racine.transform, "Petales_Chute", new Vector3(0f, 2.7f, 0f), Quaternion.identity);
                    RéglerPetales(ps, m.petale, petale);
                    ps.gameObject.AddComponent<VentSurParticules>();
                    return true;
                })) faits.Add(nom);
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[Vitrine] Animations ajoutées : {(faits.Count > 0 ? string.Join(", ", faits) : "rien de nouveau")}");
        }

        [MenuItem("Froggy/Vitrine du jardin japonais/3. Construire la scène (si elle n'existe pas)")]
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
            soleil.shadows = LightShadows.Soft;
            RéglerSoleil(soleil);
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
            // l'objet garde le nom d'origine (les autres étapes le cherchent par ce nom), quel que soit son préfabriqué
            foreach (var (nom, prefab) in new[] { ("Vitrine_Sol", dispo.sol), ("Vitrine_Eau", dispo.eau) })
            {
                var go = PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(
                    $"{Prefabs}/{(string.IsNullOrEmpty(prefab) ? nom : prefab)}.prefab"), jardin);
                go.name = nom;
            }

            // murs invisibles au bord du terrain (jusqu'à 14 m : la vitrine chine monte à ~5 m au bord)
            var murs = new GameObject("Murs invisibles").transform;
            float x0 = dispo.bornes[0], x1 = dispo.bornes[1], z0 = dispo.bornes[2], z1 = dispo.bornes[3];
            Mur(murs, new Vector3(x0, 6f, (z0 + z1) / 2), new Vector3(0.5f, 16f, z1 - z0));
            Mur(murs, new Vector3(x1, 6f, (z0 + z1) / 2), new Vector3(0.5f, 16f, z1 - z0));
            Mur(murs, new Vector3((x0 + x1) / 2, 6f, z0), new Vector3(x1 - x0, 16f, 0.5f));
            Mur(murs, new Vector3((x0 + x1) / 2, 6f, z1), new Vector3(x1 - x0, 16f, 0.5f));

            var joueur = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Joueur));
            joueur.transform.position = V(dispo.depart) + Vector3.up * 0.6f;

            Dossier(Path.GetDirectoryName(Scene));
            EditorSceneManager.SaveScene(scene, Scene);
            Debug.Log($"[Vitrine] Scène construite : {dispo.elements.Length} éléments" +
                      (manquants.Count > 0 ? $", préfabriqués manquants : {string.Join(", ", manquants)}" : ""));
            Controler(dispo);
        }

        [MenuItem("Froggy/Vitrine du jardin japonais/4. Habiller la scène (ciel, soleil, eau, vent, post-traitement)")]
        public static void Habiller()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogWarning("[Vitrine] Une partie tourne : rien n'est touché."); return; }
            if (!OuvrirVitrine(out var precedente)) return;
            try { HabillerOuverte(); }
            finally { Revenir(precedente); }
        }

        private static void HabillerOuverte()
        {
            var m = Materiaux_();
            var scene = EditorSceneManager.GetActiveScene();

            var soleil = Object.FindObjectsByType<Light>(FindObjectsSortMode.None).FirstOrDefault(l => l.type == LightType.Directional);
            if (soleil != null) RéglerSoleil(soleil);

            var rendu = GameObject.Find("Rendu");
            if (rendu == null) rendu = new GameObject("Rendu");
            Composant<VentJardin>(rendu);
            var enfant = rendu.transform.Find("Post-traitement");
            var post = enfant != null ? enfant.gameObject : new GameObject("Post-traitement");
            post.transform.SetParent(rendu.transform, false);
            var volume = Composant<Volume>(post);
            volume.isGlobal = true;
            volume.sharedProfile = ProfilPostTraitement();

            var cameras = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
            foreach (var cam in cameras)
            {
                var donnees = cam.GetUniversalAdditionalCameraData();
                donnees.renderPostProcessing = true;
                donnees.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                donnees.antialiasingQuality = AntialiasingQuality.High;
            }
            var objetEau = GameObject.Find("Vitrine_Eau");
            var eau = objetEau != null ? objetEau.GetComponentInChildren<MeshRenderer>() : null;
            var bascule = Composant<RenduJardin>(rendu);
            bascule.Regler(eau, m.eauVitrine, m.eauJardin, m.ciel, post, cameras);
            bascule.nouveauRendu = true;
            bascule.Appliquer();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[Vitrine] Scène habillée : ciel, soleil, vent, eau ({(eau != null ? "trouvée" : "INTROUVABLE")}), " +
                      $"post-traitement, {cameras.Length} caméra(s) ; bascule « Rendu > RenduJardin > nouveauRendu ».");
        }

        [MenuItem("Froggy/Vitrine du jardin japonais/5. Capturer les vues (ancien et nouveau rendu)")]
        public static void Capturer()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogWarning("[Vitrine] Une partie tourne : pas de capture."); return; }
            if (!OuvrirVitrine(out var precedente)) return;
            try { CapturerOuverte(); }
            finally { Revenir(precedente); }
        }

        private static void CapturerOuverte()
        {
            var dispo = Lire();
            var asynchrone = EditorSettings.asyncShaderCompilation;
            EditorSettings.asyncShaderCompilation = false;   // sinon des objets sortent en cyan
            Directory.CreateDirectory(DossierCaptures);

            var vues = dispo.vues.Select(v => (v.nom, V(v.pos), V(v.cible))).ToList();
            var soleil = Object.FindObjectsByType<Light>(FindObjectsSortMode.None).FirstOrDefault(l => l.type == LightType.Directional);
            if (soleil != null)
            {
                // à contre-jour : le soleil dans le champ, le bloom et les éclats sur l'eau
                var versSoleil = -soleil.transform.forward;
                var oeil = new Vector3(6f, 1.8f, 5f);
                vues.Add(("contre_jour", oeil, oeil + new Vector3(versSoleil.x, 0f, versSoleil.z).normalized * 10f + Vector3.up * 3.2f));
            }
            var shishi = Object.FindAnyObjectByType<ShishiOdoshi>();
            if (shishi != null)
            {
                var p = shishi.transform.position;
                vues.Add(("shishi_odoshi", p + new Vector3(-1.6f, 1.0f, -1.4f), p + Vector3.up * 0.4f));
            }

            // les particules avancent de quelques secondes, sinon rien ne tombe sur une image prise hors du mode Play
            foreach (var ps in Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None)) ps.Simulate(8f, true, true, true);

            var cam = new GameObject("Camera de capture").AddComponent<Camera>();
            cam.fieldOfView = 50f;
            cam.farClipPlane = 300f;
            var donnees = cam.GetUniversalAdditionalCameraData();
            donnees.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            var bascule = Object.FindAnyObjectByType<RenduJardin>();
            var rt = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { antiAliasing = 4 };
            var image = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            var faites = 0;
            try
            {
                foreach (var nouveau in bascule != null ? new[] { false, true } : new[] { true })
                {
                    if (bascule != null) { bascule.nouveauRendu = nouveau; bascule.Appliquer(); }
                    cam.clearFlags = bascule != null && nouveau ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
                    cam.backgroundColor = Ciel;
                    donnees.renderPostProcessing = bascule != null && nouveau;
                    var prefixe = bascule == null ? "" : nouveau ? "nouveau_" : "ancien_";
                    foreach (var (nom, oeil, cible) in vues)
                    {
                        cam.transform.position = oeil;
                        cam.transform.LookAt(cible);
                        var demande = new RenderPipeline.StandardRequest { destination = rt };
                        if (RenderPipeline.SupportsRenderRequest(cam, demande)) RenderPipeline.SubmitRenderRequest(cam, demande);
                        else { cam.targetTexture = rt; cam.Render(); cam.targetTexture = null; }
                        RenderTexture.active = rt;
                        image.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                        image.Apply();
                        RenderTexture.active = null;
                        File.WriteAllBytes($"{DossierCaptures}/{prefixe}{nom}.jpg", image.EncodeToJPG(90));
                        faites++;
                    }
                }
                Debug.Log($"[Vitrine] {faites} captures dans {Path.GetFullPath(DossierCaptures)}");
            }
            finally
            {
                if (bascule != null) { bascule.nouveauRendu = true; bascule.Appliquer(); }
                Object.DestroyImmediate(cam.gameObject);
                Object.DestroyImmediate(image);
                rt.Release();
                EditorSettings.asyncShaderCompilation = asynchrone;
            }
        }

        [MenuItem("Froggy/Vitrine du jardin japonais/6. Poser la muraille (si elle n'y est pas)")]
        public static void PoserMurailleMenu() => PoserMuraille(false);

        /// <summary>
        /// La muraille d'enceinte (Tools/blender/muraille_jardin.py) autour du terrain, d'après muraille_jardin.json. Une
        /// muraille déjà posée reste (elle se retouche à la main), sauf remplacer.
        /// </summary>
        public static void PoserMuraille(bool remplacer) => Poser(Muraille, "Muraille", remplacer, null);

        [MenuItem("Froggy/Vitrine du jardin japonais/6 bis. Poser la muraille artisanale (l'ancienne reste, éteinte)")]
        public static void PoserMurailleArtisanaleMenu() => PoserMurailleArtisanale(false);

        /// <summary>
        /// La muraille artisanale (Tools/blender/muraille_artisanale.py) : mêmes places que la muraille d'origine. Les deux
        /// restent dans la scène ; l'ancienne est éteinte : rallumer « Muraille » et éteindre « Muraille artisanale » pour
        /// comparer.
        /// </summary>
        public static void PoserMurailleArtisanale(bool remplacer)
        {
            if (!File.Exists(MurailleArtisanale)) { Debug.Log($"[Vitrine] Pas de {MurailleArtisanale} : pas de muraille artisanale."); return; }
            Poser(MurailleArtisanale, "Muraille artisanale", remplacer, groupe =>
            {
                var ancienne = GameObject.Find("Muraille");
                if (ancienne != null) ancienne.SetActive(false);
            });
        }

        [MenuItem("Froggy/Vitrine du jardin japonais/6 ter. Poser la bordure de bambous (éteinte)")]
        public static void PoserBordureBambousMenu() => PoserBordureBambous(false);

        /// <summary>
        /// La bordure de bambous et de roseaux (Tools/blender/bordure_bambous.py), aux places de la muraille ; posée
        /// éteinte : on l'allume depuis le menu de debug (ChoixMuraille) ou à la main.
        /// </summary>
        public static void PoserBordureBambous(bool remplacer)
        {
            if (!File.Exists(BordureBambous)) { Debug.Log($"[Vitrine] Pas de {BordureBambous} : pas de bordure de bambous."); return; }
            Poser(BordureBambous, "Bordure de bambous", remplacer, groupe => groupe.SetActive(false));
        }

        [MenuItem("Froggy/Vitrine du jardin japonais/9. Choix du contour dans le menu de debug")]
        public static void PoserChoixMuraille()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogWarning("[Vitrine] Une partie tourne : rien n'est touché."); return; }
            if (variante == "") { Debug.Log("[Vitrine] La vitrine d'origine n'a qu'une muraille : pas de choix du contour."); return; }
            if (!OuvrirVitrine(out var precedente)) return;
            try
            {
                var scene = EditorSceneManager.GetActiveScene();
                GameObject Racine(string nom) => scene.GetRootGameObjects().FirstOrDefault(g => g.name == nom);   // les éteints aussi
                var menu = Racine("Menu debug caméra");
                if (menu == null) { Debug.LogError("[Vitrine] Pas de « Menu debug caméra » : poser d'abord le menu (étape 8)."); return; }
                var so = new SerializedObject(Composant<ChoixMuraille>(menu));
                so.FindProperty("estChine").boolValue = variante == "_chine";
                so.FindProperty("artisanale").objectReferenceValue = variante == "_chine" ? null : Racine("Muraille artisanale");
                so.FindProperty("simple").objectReferenceValue = variante == "_chine" ? null : Racine("Muraille");
                so.FindProperty("bambous").objectReferenceValue = variante == "_chine" ? null : Racine("Bordure de bambous");
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[Vitrine] Choix du contour posé ({(variante == "_chine" ? "scène de la muraille de Chine" : "scène du temple")}).");
            }
            finally { Revenir(precedente); }
        }

        private static void Poser(string fichier, string nomGroupe, bool remplacer, System.Action<GameObject> ensuite)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogWarning("[Vitrine] Une partie tourne : rien n'est touché."); return; }
            Preparer();   // les préfabriqués des nouveaux FBX (Muraille_*)
            if (!OuvrirVitrine(out var precedente)) return;
            try
            {
                // parmi les racines, éteintes comprises (GameObject.Find ne voit pas un groupe éteint : il serait reposé en double)
                var existante = EditorSceneManager.GetActiveScene().GetRootGameObjects().FirstOrDefault(g => g.name == nomGroupe);
                if (existante != null && !remplacer) { Debug.LogWarning($"[Vitrine] {nomGroupe} est déjà posée : elle n'est pas refaite."); return; }
                if (existante != null) Object.DestroyImmediate(existante);
                var pose = JsonUtility.FromJson<PoseMuraille>(File.ReadAllText(fichier));
                var groupe = new GameObject(nomGroupe).transform;
                groupe.gameObject.AddComponent<Froggy.Cameras.BloqueCamera>();   // la caméra bute contre la muraille
                var manquants = new HashSet<string>();
                foreach (var p in pose.pieces)
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{Prefabs}/{p.nom}.prefab");
                    if (prefab == null) { manquants.Add(p.nom); continue; }
                    var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, groupe);
                    go.transform.SetPositionAndRotation(V(p.pos), Quaternion.Euler(0f, p.lacet, 0f) * prefab.transform.rotation);
                }
                ensuite?.Invoke(groupe.gameObject);
                var scene = EditorSceneManager.GetActiveScene();
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Log(manquants.Count == 0, $"[Vitrine] {nomGroupe} posée : {groupe.childCount} pièces" +
                                          (manquants.Count > 0 ? $", préfabriqués manquants : {string.Join(", ", manquants)}" : ""));
            }
            finally { Revenir(precedente); }
        }

        [MenuItem("Froggy/Vitrine du jardin japonais/7. Caméra à la World of Warcraft")]
        public static void PoserCamera()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogWarning("[Vitrine] Une partie tourne : rien n'est touché."); return; }
            if (!OuvrirVitrine(out var precedente)) return;
            try
            {
                var rig = Object.FindObjectsByType<TemplateCameraRig>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault();
                if (rig == null) { Debug.LogError("[Vitrine] Pas de TemplateCameraRig dans la vitrine : pas de caméra posée."); return; }
                // l'ancien rig et l'ancien contrôleur restent : décocher CameraOrbitale les rend (bascule pour comparer)
                Composant<Froggy.Cameras.CameraOrbitale>(rig.gameObject);
                var joueur = rig.GetComponentInParent<TemplatePlayerController>();
                var deplacement = joueur.GetComponent<Froggy.Deplacement.DeplacementWow>();
                if (deplacement == null)
                {
                    deplacement = joueur.gameObject.AddComponent<Froggy.Deplacement.DeplacementWow>();
                    deplacement.enabled = false;   // c'est la caméra qui l'allume
                }
                var so = new SerializedObject(deplacement);
                if (so.FindProperty("reglages").objectReferenceValue == null)
                    so.FindProperty("reglages").objectReferenceValue = new SerializedObject(joueur).FindProperty("settings").objectReferenceValue;
                if (so.FindProperty("reglagesWow").objectReferenceValue == null)
                    so.FindProperty("reglagesWow").objectReferenceValue = ReglagesWow();
                so.ApplyModifiedPropertiesWithoutUndo();
                // la caméra ne bute que contre le sol et la muraille, elle traverse le petit décor (comme WoW)
                var bloquent = new List<string>();
                foreach (var nom in new[] { "Vitrine_Sol", "Muraille", "Muraille artisanale" })
                {
                    var go = GameObject.Find(nom);
                    if (go == null) continue;
                    Composant<Froggy.Cameras.BloqueCamera>(go);
                    bloquent.Add(nom);
                }
                var scene = EditorSceneManager.GetActiveScene();
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Log(bloquent.Count >= 2, $"[Vitrine] Caméra orbitale posée sur {rig.name}, DeplacementWow sur {joueur.name} ; " +
                    $"arrêtent la caméra : {string.Join(", ", bloquent)} ; décocher CameraOrbitale rend l'ancienne caméra et l'ancien contrôleur.");
            }
            finally { Revenir(precedente); }
        }

        [MenuItem("Froggy/Vitrine du jardin japonais/10. Les Froggy dans le menu de debug")]
        public static void PoserApparence()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogWarning("[Vitrine] Une partie tourne : rien n'est touché."); return; }
            if (!OuvrirVitrine(out var precedente)) return;
            try
            {
                var scene = EditorSceneManager.GetActiveScene();
                var joueur = Object.FindAnyObjectByType<TemplatePlayerController>();
                if (joueur == null) { Debug.LogError("[Vitrine] Pas de joueur dans la scène."); return; }
                var modeles = PersonnagesFroggy.Modeles();
                var so = new SerializedObject(PersonnagesFroggy.Brancher(joueur, modeles));
                so.FindProperty("raccourcis").boolValue = false;   // E est le pas de côté de la caméra WoW, 1 / 2 restent libres
                so.FindProperty("modeleDeDepart").intValue = -1;   // la forme provisoire, comme avant ; le menu change de Froggy
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[Vitrine] Froggy au choix dans le menu de debug : {string.Join(", ", modeles.Select(m => m.nom))}.");
            }
            finally { Revenir(precedente); }
        }

        [MenuItem("Froggy/Vitrine du jardin japonais/8. Menu de debug de la caméra")]
        public static void PoserMenuCamera()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogWarning("[Vitrine] Une partie tourne : rien n'est touché."); return; }
            if (!OuvrirVitrine(out var precedente)) return;
            try
            {
                var go = GameObject.Find("Menu debug caméra");
                if (go == null) go = new GameObject("Menu debug caméra");
                Composant<Froggy.Cameras.MenuCamera>(go);
                var scene = EditorSceneManager.GetActiveScene();
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log("[Vitrine] Menu de debug de la caméra posé (touche au-dessus de Tab en jeu).");
            }
            finally { Revenir(precedente); }
        }

        [MenuItem("Froggy/Vitrine du jardin japonais/11. Voltige de sakura (dans les trois vitrines)")]
        public static void PoserVoltigeSakuraPartout()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogWarning("[Vitrine] Une partie tourne : rien n'est touché."); return; }
            var avant = variante;
            try
            {
                foreach (var v in new[] { "", "_grande", "_chine" })
                {
                    variante = v;
                    if (File.Exists(Scene)) PoserVoltigeSakura(false);
                }
            }
            finally { variante = avant; }
        }

        /// <summary>
        /// Les pétales de sakura de Codex (Assets/Froggy/Effets/Sakura) dans la vitrine, objet « Voltige de sakura » :
        /// les pétales au vent (particules, allumées) et la boucle précalculée au-dessus de chaque cerisier (éteinte), à
        /// comparer depuis le menu de debug. Un objet déjà posé reste, sauf remplacer.
        /// </summary>
        public static void PoserVoltigeSakura(bool remplacer)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogWarning("[Vitrine] Une partie tourne : rien n'est touché."); return; }
            if (!File.Exists($"{Sakura}/Sakura_Voltige_Boucle_8s.fbx")) { Debug.Log($"[Vitrine] Pas de kit dans {Sakura} : pas de voltige."); return; }
            var m = Materiaux_();
            var boucle = PrefabBoucleSakura(m.petale);
            var maillages = new[] { "Ouvert", "Courbe", "Torsade" }
                .Select(n => AssetDatabase.LoadAllAssetsAtPath($"{Sakura}/Petale_Sakura_{n}.fbx").OfType<Mesh>().First()).ToArray();
            if (!OuvrirVitrine(out var precedente)) return;
            try
            {
                var scene = EditorSceneManager.GetActiveScene();
                var existante = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "Voltige de sakura");
                if (existante != null && !remplacer) { Debug.LogWarning("[Vitrine] La voltige de sakura est déjà posée : elle n'est pas refaite."); return; }
                if (existante != null) Object.DestroyImmediate(existante);

                var dispo = Lire();
                float x0 = dispo.bornes[0], x1 = dispo.bornes[1], z0 = dispo.bornes[2], z1 = dispo.bornes[3];
                // les pétales naissent de 2 à 5 m au-dessus du point le plus haut du sol (la vitrine chine monte à ~5 m)
                var sol = GameObject.Find("Vitrine_Sol");
                float haut = sol != null ? sol.GetComponentsInChildren<Renderer>().Max(r => r.bounds.max.y) : 1.5f;
                var racine = new GameObject("Voltige de sakura");
                var ps = Particules(racine.transform, "Pétales au vent", new Vector3((x0 + x1) / 2, haut + 3.5f, (z0 + z1) / 2), Quaternion.identity);
                RéglerVoltige(ps, m.petale, maillages, new Vector3(x1 - x0, 3f, z1 - z0), haut + 6f);
                var vent = new SerializedObject(ps.gameObject.AddComponent<VentSurParticules>());
                vent.FindProperty("poussee").floatValue = 2.5f;   // freinée par la traînée : ~0,6 m/s par vent moyen
                vent.ApplyModifiedPropertiesWithoutUndo();

                var groupe = new GameObject("Boucles précalculées");
                groupe.transform.SetParent(racine.transform, false);
                var cerisiers = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)
                    .Where(t => t.parent != null && t.parent.name == "Jardin" && t.name.StartsWith("Cerisier")).ToList();
                foreach (var c in cerisiers)
                {
                    // le clip anime aussi la racine du FBX (remise à l'origine) : on place un parent, pas la boucle
                    var place = new GameObject($"Boucle ({c.name})").transform;
                    place.SetParent(groupe.transform, false);
                    // la boucle descend de 4,6 m à 1 m sous son origine : posée 1 m au-dessus du pied de l'arbre
                    place.SetPositionAndRotation(c.position + Vector3.up, Quaternion.Euler(0f, 360f * VoltigeSakura.Phase(c.position), 0f));
                    PrefabUtility.InstantiatePrefab(boucle, place);
                }
                groupe.SetActive(false);

                var so = new SerializedObject(racine.AddComponent<VoltigeSakura>());
                so.FindProperty("auVent").objectReferenceValue = ps;
                so.FindProperty("boucles").objectReferenceValue = groupe;
                so.FindProperty("surface").floatValue = (x1 - x0) * (z1 - z0);
                so.ApplyModifiedPropertiesWithoutUndo();
                var em = ps.emission; em.rateOverTime = racine.GetComponent<VoltigeSakura>().Debit;   // celui que Start posera
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[Vitrine] Voltige de sakura posée : pétales au vent sur {(x1 - x0) * (z1 - z0):0} m² (allumés), " +
                          $"boucle précalculée sur {cerisiers.Count} cerisier(s) (éteinte) ; bascule dans le menu de debug.");
            }
            finally { Revenir(precedente); }
        }

        // La boucle de Codex lue en Generic, en boucle, avec le matériau des pétales (qui lit les couleurs de sommets du
        // kit) ; un préfabriqué variante du FBX avec son contrôleur.
        private static GameObject PrefabBoucleSakura(Material petale)
        {
            var fbx = $"{Sakura}/Sakura_Voltige_Boucle_8s.fbx";
            var importeur = (ModelImporter)AssetImporter.GetAtPath(fbx);
            var clips = importeur.clipAnimations.Length > 0 ? importeur.clipAnimations : importeur.defaultClipAnimations;
            if (clips.Any(c => !c.loopTime) || importeur.importCameras || importeur.importLights)
            {
                foreach (var c in clips) c.loopTime = true;
                importeur.clipAnimations = clips;
                importeur.importCameras = importeur.importLights = false;
                importeur.SearchAndRemapMaterials(ModelImporterMaterialName.BasedOnMaterialName, ModelImporterMaterialSearch.Local);
                importeur.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), "Sakura_RoseIvoire"), petale);
                importeur.SaveAndReimport();
            }
            var cible = $"{Prefabs}/Sakura_Voltige_Boucle.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(cible);
            if (prefab != null) return prefab;
            var clip = AssetDatabase.LoadAllAssetsAtPath(fbx).OfType<AnimationClip>().First(c => !c.name.StartsWith("__preview__"));
            var controleur = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath($"{Rendu}/Sakura_Voltige.controller");
            controleur.layers[0].stateMachine.AddState("Voltige").motion = clip;
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(fbx));
            var animator = Composant<Animator>(instance);
            animator.runtimeAnimatorController = controleur;
            animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
            foreach (var r in instance.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = ShadowCastingMode.Off;
            prefab = PrefabUtility.SaveAsPrefabAsset(instance, cible);
            Object.DestroyImmediate(instance);
            return prefab;
        }

        private static Froggy.Deplacement.ReglagesWow ReglagesWow()
        {
            const string chemin = "Assets/Froggy/Vitrine/ReglagesWow.asset";
            var r = AssetDatabase.LoadAssetAtPath<Froggy.Deplacement.ReglagesWow>(chemin);
            if (r != null) return r;   // des réglages existants restent
            r = ScriptableObject.CreateInstance<Froggy.Deplacement.ReglagesWow>();
            AssetDatabase.CreateAsset(r, chemin);
            return r;
        }

        /// <summary>Toutes les étapes, pour la ligne de commande (éditeur fermé) ; -grande : la vitrine agrandie.</summary>
        public static void ToutFaire()
        {
            var args = System.Environment.GetCommandLineArgs();
            variante = System.Array.IndexOf(args, "-chine") >= 0 ? "_chine" : System.Array.IndexOf(args, "-grande") >= 0 ? "_grande" : "";
            try { Etapes(System.Array.IndexOf(args, "-reconstruire") >= 0); }
            finally { variante = ""; }
        }

        [MenuItem("Froggy/Vitrine du jardin japonais/Vitrine agrandie (52 × 40 m) : tout construire")]
        public static void ToutFaireGrande() => ToutFaireVariante("_grande");

        [MenuItem("Froggy/Vitrine du jardin japonais/Vitrine sur talus, muraille de Chine : tout construire")]
        public static void ToutFaireChine() => ToutFaireVariante("_chine");

        private static void ToutFaireVariante(string v)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogWarning("[Vitrine] Une partie tourne : rien n'est touché."); return; }
            var active = EditorSceneManager.GetActiveScene();
            // construire ouvre une scène neuve : des modifications non enregistrées de la scène ouverte seraient perdues
            if (active.isDirty) { Debug.LogWarning($"[Vitrine] {active.path} a des modifications non enregistrées : rien n'est construit."); return; }
            var precedente = active.path;   // à garder tout de suite : une fois la scène fermée, active.path est vide
            variante = v;
            try { Etapes(false); }
            finally { Revenir(precedente); variante = ""; }   // Revenir compare à la scène de la variante : avant de la remettre
        }

        private static void Etapes(bool reconstruire)
        {
            Preparer();
            Animer();
            Construire(reconstruire);
            Habiller();
            PoserMuraille(false);
            PoserMurailleArtisanale(false);
            PoserBordureBambous(false);
            PoserCamera();
            PoserMenuCamera();
            PoserChoixMuraille();
            PoserApparence();
            PoserVoltigeSakura(false);
            ControlerAnimations();
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

        /// <summary>
        /// Sonde des animations (skill rendu-vivant) : la bouche du shishi-odoshi descend quand il bascule et le pivot ne
        /// bouge pas ; les durées de remplissage restent dans leurs bornes et ne se répètent pas ; les pétales tombent,
        /// lentement, et ne remontent pas.
        /// </summary>
        [MenuItem("Froggy/Vitrine du jardin japonais/Contrôler les animations")]
        public static void ControlerAnimations()
        {
            // une scène d'aperçu, isolée : la scène ouverte (peut-être celle d'un autre) n'est pas touchée
            var bac = EditorSceneManager.NewPreviewScene();
            try { ControlerDans(bac); }
            finally { EditorSceneManager.ClosePreviewScene(bac); }
        }

        private static void ControlerDans(UnityEngine.SceneManagement.Scene bac)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{Prefabs}/ShishiOdoshi.prefab");
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, bac);
            try
            {
                var s = go.GetComponent<ShishiOdoshi>();
                s.Preparer();
                var bouche = go.GetComponentsInChildren<Transform>().First(t => t.name == "Verse");
                var pivot = go.GetComponentsInChildren<Transform>().First(t => t.name == "ShishiOdoshi_Bascule");
                s.PoserAngle(0f);
                float yRepos = bouche.position.y; var pRepos = pivot.position;
                s.PoserAngle(52f);
                float yBascule = bouche.position.y;
                var ok = yBascule < yRepos - 0.2f && (pivot.position - pRepos).magnitude < 1e-4f;
                Log(ok, $"[Vitrine] Shishi-odoshi : bouche à {yRepos:0.00} m au repos, {yBascule:0.00} m basculé, pivot immobile");

                var alea = new System.Random(ShishiOdoshi.HachagePosition(new Vector3(7.3f, 0f, -1.8f)));
                var tirages = new List<float>(); float prec = 0f;
                for (int i = 0; i < 8; i++) { prec = ShishiOdoshi.TirerRemplissage(alea, new Vector2(12f, 18f), prec); tirages.Add(prec); }
                ok = tirages.All(v => v >= 12f && v <= 18f) && tirages.Zip(tirages.Skip(1), (a, b) => Mathf.Abs(a - b) >= 0.6f).All(x => x);
                Log(ok, $"[Vitrine] Remplissages tirés : {string.Join(" ; ", tirages.Select(v => v.ToString("0.0")))} s");
            }
            finally { Object.DestroyImmediate(go); }

            var cerisier = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>($"{Prefabs}/Cerisier_1.prefab"), bac);
            try
            {
                var ps = cerisier.GetComponentsInChildren<ParticleSystem>().First();
                ps.Simulate(6f, true, true, true);
                var parts = new ParticleSystem.Particle[ps.main.maxParticles];
                int n = ps.GetParticles(parts);
                var vitesses = parts.Take(n).Select(p => p.totalVelocity.y).ToList();
                var hauts = parts.Take(n).Select(p => p.position.y).ToList();
                // la voltige les fait parfois remonter un instant : on juge la chute moyenne et la hauteur atteinte
                var moyenne = n > 0 ? vitesses.Average() : 0f;
                var ok = n > 10 && hauts.Max() < 3.2f + 0.6f && moyenne < -0.3f && moyenne > -1.0f;
                Log(ok, $"[Vitrine] Pétales après 6 s : {n} en l'air ou posés, hauteur {(n > 0 ? hauts.Min() : 0):0.0} à {(n > 0 ? hauts.Max() : 0):0.0} m, " +
                        $"chute moyenne {moyenne:0.00} m/s (de {(n > 0 ? vitesses.Min() : 0):0.00} à {(n > 0 ? vitesses.Max() : 0):0.00})");
            }
            finally { Object.DestroyImmediate(cerisier); }
        }

        private static void Log(bool ok, string message) { if (ok) Debug.Log(message); else Debug.LogError(message); }

        // ---------- réglages ----------

        // fin d'après-midi : soleil bas (22°) au sud-ouest, chaud ; l'ombre portée part vers le nord-est
        private static void RéglerSoleil(Light soleil)
        {
            soleil.color = new Color(1f, 0.90f, 0.78f);
            soleil.intensity = 1.35f;
            soleil.transform.rotation = Quaternion.Euler(22f, 60f, 0f);
            RenderSettings.sun = soleil;
        }

        private static VolumeProfile ProfilPostTraitement()
        {
            var chemin = $"{Rendu}/PostTraitement_Jardin.asset";
            var profil = AssetDatabase.LoadAssetAtPath<VolumeProfile>(chemin);
            if (profil != null) return profil;   // un profil existant garde ses réglages
            Dossier(Rendu);
            profil = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profil, chemin);
            profil.Add<Tonemapping>().mode.Override(TonemappingMode.Neutral);
            var bloom = profil.Add<Bloom>();
            bloom.threshold.Override(1.0f);
            bloom.intensity.Override(0.6f);
            bloom.scatter.Override(0.65f);
            var couleurs = profil.Add<ColorAdjustments>();
            couleurs.postExposure.Override(0.1f);
            couleurs.contrast.Override(8f);
            couleurs.saturation.Override(12f);
            profil.Add<WhiteBalance>().temperature.Override(3f);
            // lumière chaude, ombre froide (skill direction-artistique)
            profil.Add<ShadowsMidtonesHighlights>().shadows.Override(new Vector4(0.97f, 0.99f, 1.05f, 0f));
            var vignette = profil.Add<Vignette>();
            vignette.intensity.Override(0.18f);
            vignette.smoothness.Override(0.5f);
            foreach (var c in profil.components)
            {
                c.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
                AssetDatabase.AddObjectToAsset(c, profil);
            }
            EditorUtility.SetDirty(profil);
            AssetDatabase.SaveAssets();
            return profil;
        }

        // Pétales : chute de 0,5 à 0,8 m/s (des pétales de rose mesurés tombent à 1,3-1,6 m/s ; ralenti pour qu'on les
        // voie danser), voltige par le bruit, culbute sur les trois axes, poussée du vent, se posent puis s'effacent.
        // Taille exagérée (6 à 9 cm au lieu de 1,5) pour se lire depuis la caméra.
        private static void RéglerPetales(ParticleSystem ps, Material materiau, Mesh petale)
        {
            var main = ps.main;
            main.duration = 10f;
            main.loop = true;
            main.prewarm = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(8f, 12f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.09f);
            main.startRotation3D = true;
            main.startRotationX = main.startRotationY = main.startRotationZ = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.86f, 0.90f), new Color(0.96f, 0.70f, 0.80f));
            main.gravityModifier = 0f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 160;
            var em = ps.emission; em.rateOverTime = 8f;
            var forme = ps.shape; forme.shapeType = ParticleSystemShapeType.Box; forme.scale = new Vector3(4.2f, 1.0f, 4.2f);
            var vitesse = ps.velocityOverLifetime;
            vitesse.enabled = true;
            vitesse.space = ParticleSystemSimulationSpace.World;
            vitesse.x = new ParticleSystem.MinMaxCurve(0f, 0f);
            vitesse.y = new ParticleSystem.MinMaxCurve(-0.8f, -0.5f);
            vitesse.z = new ParticleSystem.MinMaxCurve(0f, 0f);
            var bruit = ps.noise;
            bruit.enabled = true;
            bruit.strength = 0.25f;
            bruit.frequency = 0.6f;
            bruit.scrollSpeed = 0.3f;
            bruit.octaveCount = 2;
            var tourne = ps.rotationOverLifetime;
            tourne.enabled = true;
            tourne.separateAxes = true;
            tourne.x = tourne.y = tourne.z = new ParticleSystem.MinMaxCurve(-3f, 3f);
            var fondu = ps.colorOverLifetime;
            fondu.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.05f), new GradientAlphaKey(1f, 0.85f), new GradientAlphaKey(0f, 1f) });
            fondu.color = g;
            var collision = ps.collision;
            collision.enabled = true;
            collision.type = ParticleSystemCollisionType.World;
            collision.mode = ParticleSystemCollisionMode.Collision3D;
            collision.dampen = 1f;
            collision.bounce = 0f;
            collision.lifetimeLoss = 0f;
            collision.quality = ParticleSystemCollisionQuality.Medium;
            collision.radiusScale = 0.4f;
            var rendu = ps.GetComponent<ParticleSystemRenderer>();
            rendu.renderMode = ParticleSystemRenderMode.Mesh;
            rendu.mesh = petale;
            rendu.alignment = ParticleSystemRenderSpace.World;
            rendu.sharedMaterial = materiau;
            rendu.shadowCastingMode = ShadowCastingMode.Off;
        }

        // Voltige de sakura : les réglages des pétales des cerisiers (RéglerPetales), sur toute la vitrine, avec les trois
        // formes du kit de Codex (tirées au hasard), plus de bruit pour qu'ils tourbillonnent, et une traînée qui borne la
        // poussée du vent (sinon elle accélère sans fin sur une vie de 15 à 25 s). La traînée freine aussi le module de
        // vitesse (chute mesurée à -0,02 m/s) : la chute vient donc d'une gravité faible, dont la traînée fait une vitesse
        // limite (9,81 × 0,075..0,12 / 1,5 ≈ 0,5..0,8 m/s, comme les pétales des cerisiers). La couleur vient des sommets du kit :
        // la particule reste presque blanche, pour ne pas teinter deux fois. chute : la plus grande hauteur à descendre.
        private static void RéglerVoltige(ParticleSystem ps, Material materiau, Mesh[] petales, Vector3 boite, float chute)
        {
            RéglerPetales(ps, materiau, petales[0]);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(chute / 0.8f, chute / 0.5f + 4f);   // + quelques secondes posés au sol
            main.startSize = new ParticleSystem.MinMaxCurve(0.075f, 0.115f);
            main.startColor = new ParticleSystem.MinMaxGradient(Color.white, new Color(1f, 0.93f, 0.96f));
            main.maxParticles = 5000;
            main.gravityModifier = new ParticleSystem.MinMaxCurve(0.075f, 0.12f);
            var vitesse = ps.velocityOverLifetime; vitesse.enabled = false;
            var forme = ps.shape; forme.scale = boite;
            var bruit = ps.noise;
            bruit.strength = 0.4f;
            bruit.frequency = 0.45f;
            var frein = ps.limitVelocityOverLifetime;
            frein.enabled = true;
            frein.limit = 50f;   // pas de plafond : seule la traînée compte
            frein.drag = 1.5f;
            // sinon Unity multiplie la traînée par la taille (0,1) : 2,3 m/s de dérive mesurés au lieu de ~0,6
            frein.multiplyDragByParticleSize = false;
            frein.multiplyDragByParticleVelocity = false;
            ps.GetComponent<ParticleSystemRenderer>().SetMeshes(petales);
        }

        private static void Gouttes(ParticleSystem ps, Material materiau, float vie, float vitesse, Vector2 taille)
        {
            var main = ps.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(vie * 0.8f, vie);
            main.startSpeed = new ParticleSystem.MinMaxCurve(vitesse * 0.6f, vitesse);
            main.startSize = new ParticleSystem.MinMaxCurve(taille.x, taille.y);
            main.startColor = new Color(0.86f, 0.95f, 1f, 0.85f);
            main.gravityModifier = 1f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 200;
            var rendu = ps.GetComponent<ParticleSystemRenderer>();
            rendu.sharedMaterial = materiau;
            rendu.shadowCastingMode = ShadowCastingMode.Off;
        }

        private static ParticleSystem Particules(Transform parent, string nom, Vector3 position, Quaternion rotation)
        {
            var go = new GameObject(nom);
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(position, rotation);   // dans l'espace du préfabriqué, racine à l'origine
            return go.AddComponent<ParticleSystem>();
        }

        // Pétale de Somei-Yoshino : ovale, avec l'encoche au bout ; 1 m de long (la taille vient de la particule).
        private static Mesh MaillagePetale()
        {
            var chemin = $"{Rendu}/Petale.asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(chemin);
            if (mesh != null) return mesh;
            Dossier(Rendu);
            var tour = new[] { new Vector3(0f, -0.5f), new Vector3(0.32f, -0.05f), new Vector3(0.24f, 0.4f), new Vector3(0f, 0.3f),
                               new Vector3(-0.24f, 0.4f), new Vector3(-0.32f, -0.05f) };
            var sommets = new List<Vector3> { new Vector3(0f, 0f, -0.06f) };   // creusé au centre
            sommets.AddRange(tour);
            var triangles = new List<int>();
            for (int i = 0; i < tour.Length; i++) triangles.AddRange(new[] { 0, 1 + (i + 1) % tour.Length, 1 + i });
            mesh = new Mesh { name = "Petale", vertices = sommets.ToArray(), triangles = triangles.ToArray(),
                              colors = Enumerable.Repeat(Color.white, sommets.Count).ToArray() };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, chemin);
            return mesh;
        }

        // ---------- outils ----------

        /// <summary>Ouvre un préfabriqué, le modifie, ne l'enregistre que si la modification a changé quelque chose.</summary>
        private static bool Modifier(string chemin, System.Func<GameObject, bool> modifier)
        {
            var contenu = PrefabUtility.LoadPrefabContents(chemin);
            try
            {
                var change = modifier(contenu);
                if (change) PrefabUtility.SaveAsPrefabAsset(contenu, chemin);
                return change;
            }
            finally { PrefabUtility.UnloadPrefabContents(contenu); }
        }

        /// <summary>
        /// Ouvre la vitrine en se souvenant de la scène d'avant ; refuse si celle-ci a des modifications non enregistrées
        /// (quelqu'un d'autre y travaille peut-être).
        /// </summary>
        private static bool OuvrirVitrine(out string precedente)
        {
            var active = EditorSceneManager.GetActiveScene();
            precedente = active.path;
            if (active.path == Scene) return true;
            if (active.isDirty)
            {
                Debug.LogWarning($"[Vitrine] {active.path} a des modifications non enregistrées : la vitrine n'est pas ouverte.");
                return false;
            }
            EditorSceneManager.OpenScene(Scene, OpenSceneMode.Single);
            return true;
        }

        private static void Revenir(string precedente)
        {
            if (!string.IsNullOrEmpty(precedente) && precedente != Scene) EditorSceneManager.OpenScene(precedente, OpenSceneMode.Single);
        }

        // GetComponent rend un « faux null » dans l'éditeur quand le composant manque : pas de ?? avec un objet Unity
        private static T Composant<T>(GameObject go) where T : Component
        {
            var c = go.GetComponent<T>();
            return c != null ? c : go.AddComponent<T>();
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
