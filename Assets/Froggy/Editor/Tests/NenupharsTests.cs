using System.Collections.Generic;
using Froggy.Deplacement;
using Froggy.Template;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Froggy.Outils.Tests
{
    /// <summary>
    /// Nénuphars (#16) : « on saute de nénuphar en nénuphar sur 5 nénuphars d'affilée », à 30, 60 et 144 pas par seconde,
    /// sur le vrai préfabriqué Gris_Nenuphar. Puis chaque règle avec sa mesure : le dessin s'enfonce sous le poids et
    /// remonte, plonge d'autant plus que la chute est haute, penche vers la grenouille, ne coule pas sous le nombre, oscille
    /// à la fréquence tirée de sa profondeur ; le dessin de Froggy le suit ; le collider ne bouge jamais.
    /// Toujours sur des copies des vrais réglages, jamais sur les assets.
    /// </summary>
    public class NenupharsTests
    {
        private const string CheminCourse = "Assets/Froggy/Joueur/Settings/TemplateMovementSettings.asset";
        private const string CheminWow = "Assets/Froggy/Vitrine/ReglagesWow.asset";
        private const string CheminMatiere = "Assets/Froggy/Joueur/Settings/TemplateNoFriction.physicMaterial";
        private const string CheminPrefab = "Assets/Froggy/Prefabs/Gris/Gris_Nenuphar.prefab";
        private const string CheminJardin = "Assets/Froggy/Prefabs/JardinJaponais/Nenuphar_Moyen.prefab";

        /// <summary>Les écarts de bord à bord entre deux nénuphars de 2 m, en m : de quoi viser, sans être injuste.</summary>
        private static readonly float[] Ecarts = { 2.0f, 2.6f, 3.0f, 2.2f, 2.8f };

        private TemplateMovementSettings course;
        private ReglagesWow wow;
        private ReglagesNenuphar reglages;
        private Scene scene;
        private PhysicsScene physique;
        private readonly List<Nenuphar> nenuphars = new List<Nenuphar>();

        [SetUp]
        public void Preparer()
        {
            course = Object.Instantiate(AssetDatabase.LoadAssetAtPath<TemplateMovementSettings>(CheminCourse));
            wow = Object.Instantiate(AssetDatabase.LoadAssetAtPath<ReglagesWow>(CheminWow));
            reglages = Object.Instantiate(AssetDatabase.LoadAssetAtPath<ReglagesNenuphar>(Nenuphar.CheminReglages));
        }

        [TearDown]
        public void Ranger()
        {
            if (scene.IsValid()) EditorSceneManager.ClosePreviewScene(scene);
            nenuphars.Clear();
            Object.DestroyImmediate(course);
            Object.DestroyImmediate(wow);
            Object.DestroyImmediate(reglages);
        }

        [Test]
        public void LePrefabEstPretPourLeBatisseur()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CheminPrefab);
            Assert.That(prefab.GetComponent<Nenuphar>(), Is.Not.Null, "Gris_Nenuphar n'a pas le composant Nenuphar");
            foreach (var nom in new[] { "Nenuphar_Petit", "Nenuphar_Moyen", "Nenuphar_Grand" })
            {
                var p = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Froggy/Prefabs/JardinJaponais/{nom}.prefab");
                Assert.That(p.GetComponent<Nenuphar>(), Is.Not.Null, $"{nom} n'a pas le composant Nenuphar");
            }
        }

        [Test]
        public void CinqNenupharsDAffilee([Values(30, 60, 144)] int pasParSeconde)
        {
            Scene();
            Bloc("Berge", new Vector3(0f, 0.08f, 0f), new Vector3(3f, 0.4f, 3f));
            float z = 1.5f;
            foreach (float ecart in Ecarts)
            {
                z += ecart + 1f;   // bord à bord, puis le rayon
                Nouveau(new Vector3(0f, 0.08f, z));
                z += 1f;
            }
            var reposCollider = new List<Vector3>();
            foreach (var n in nenuphars) reposCollider.Add(n.transform.position);
            var plusBas = new float[nenuphars.Count];

            var joueur = Joueur(new Vector3(0f, 0.08f, 0f));
            var corps = joueur.GetComponent<Rigidbody>();
            float dt = 1f / pasParSeconde;
            for (int i = 0; i < 10; i++) Avancer(joueur, dt);

            int cible = 0, pas = 0;
            bool saute = false;
            while (cible < nenuphars.Count)
            {
                Assert.That(pas++, Is.LessThan(pasParSeconde * 8 * nenuphars.Count), $"bloqué avant le nénuphar {cible + 1}");
                Vector3 vers = nenuphars[cible].transform.position - corps.position;
                vers.y = 0f;
                corps.rotation = Quaternion.Euler(0f, Mathf.Atan2(vers.x, vers.z) * Mathf.Rad2Deg, 0f);   // il vise le suivant
                bool appui = !saute && !joueur.AuSol;   // au premier pas hors du bord : le coyote time le rattrape
                joueur.Imposer(new DeplacementWow.Entrees { deplacement = new Vector2(0f, 1f), sautAppuye = appui, sautTenu = true });
                Avancer(joueur, dt);
                if (appui)
                {
                    saute = true;
                    Assert.That(corps.linearVelocity.y, Is.GreaterThan(1f), $"le saut vers le nénuphar {cible + 1} n'est pas parti");
                }
                Assert.That(corps.position.y, Is.GreaterThan(-1.5f), $"tombé à l'eau avant le nénuphar {cible + 1} ({pasParSeconde} pas/s)");
                for (int i = 0; i < nenuphars.Count; i++) plusBas[i] = Mathf.Max(plusBas[i], nenuphars[i].Enfoncement);
                if (saute && joueur.AuSol && joueur.SolTouche == nenuphars[cible].GetComponent<Collider>())
                {
                    cible++;
                    saute = false;
                }
            }
            for (int i = 0; i < 20; i++) Avancer(joueur, dt);   // le dernier aussi a le temps de s'enfoncer
            plusBas[nenuphars.Count - 1] = Mathf.Max(plusBas[nenuphars.Count - 1], nenuphars[nenuphars.Count - 1].Enfoncement);

            for (int i = 0; i < nenuphars.Count; i++)
            {
                Assert.That(nenuphars[i].transform.position, Is.EqualTo(reposCollider[i]), $"le collider du nénuphar {i + 1} a bougé");
                Assert.That(plusBas[i], Is.GreaterThan(reglages.enfoncementParGrenouille * 0.5f), $"le nénuphar {i + 1} ne s'est pas enfoncé");
            }
        }

        [Test]
        public void SEnfonceSousLePoidsPuisRemonte()
        {
            Scene();
            var n = Nouveau(new Vector3(0f, 0.08f, 0f));
            var col = n.GetComponent<Collider>();
            Vector3 reposCentre = col.bounds.center;
            var joueur = Joueur(new Vector3(0f, 0.38f, 0f));
            float dt = 1f / 60f;
            for (int i = 0; i < 120; i++) Avancer(joueur, dt);

            Assert.That(n.NombreDePorteurs, Is.EqualTo(1));
            Assert.That(n.Enfoncement, Is.EqualTo(reglages.enfoncementParGrenouille).Within(0.005f), "pas à l'enfoncement d'une grenouille");
            Assert.That(n.transform.position.y - n.Dessin.position.y, Is.EqualTo(n.Enfoncement).Within(1e-4f), "le dessin ne descend pas");
            Assert.That(n.GetComponent<MeshRenderer>().enabled, Is.False, "le dessin d'origine est encore affiché");
            Assert.That(n.Dessin.GetComponent<MeshRenderer>().enabled, Is.True, "le dessin séparé est caché");
            Assert.That(col.bounds.center, Is.EqualTo(reposCentre), "le collider a bougé");

            joueur.GetComponent<Rigidbody>().position = new Vector3(0f, 50f, 0f);   // parti
            for (int i = 0; i < 120; i++) Avancer(joueur, dt);
            Assert.That(n.NombreDePorteurs, Is.EqualTo(0));
            Assert.That(Mathf.Abs(n.Enfoncement), Is.LessThan(0.002f), "le nénuphar ne remonte pas");
        }

        [Test]
        public void UnNenupharDuJardinTourneSEnfonceToutDroit()
        {
            // les modèles du jardin sont tournés de 90° autour de x (Blender a z vers le haut) : leur y local est à plat.
            // Avant ce test, leur dessin glissait de côté de 5 cm et penchait de 3° avec Froggy au milieu (mesuré en jeu).
            Scene();
            var n = Nouveau(new Vector3(0f, 0.08f, 0f), CheminJardin, 30f);
            Assert.That(Vector3.Angle(n.transform.up, Vector3.up), Is.GreaterThan(45f),
                "le modèle du jardin n'est plus tourné : ce test ne prouve plus rien");
            var top = n.GetComponent<Collider>().bounds.max.y;
            var joueur = Joueur(new Vector3(n.transform.position.x, top + 0.3f, n.transform.position.z));
            var visuel = joueur.transform.Find("Visual");
            float dt = 1f / 60f;
            for (int i = 0; i < 120; i++) Avancer(joueur, dt);

            Vector3 ecart = n.Dessin.position - n.transform.position;
            Assert.That(n.Enfoncement, Is.EqualTo(reglages.enfoncementParGrenouille).Within(0.005f));
            Assert.That(new Vector2(ecart.x, ecart.z).magnitude, Is.LessThan(0.001f), "le dessin glisse de côté au lieu de s'enfoncer");
            Assert.That(ecart.y, Is.EqualTo(-n.Enfoncement).Within(0.001f), "le dessin ne descend pas tout droit");
            Assert.That(n.Pente.magnitude, Is.LessThan(0.5f), "il penche alors que Froggy est au milieu");
            Assert.That(joueur.transform.position.y - visuel.position.y, Is.EqualTo(n.Enfoncement).Within(0.003f),
                "le dessin de Froggy ne descend pas avec le nénuphar");
        }

        [Test]
        public void PlusLaChuteEstHautePlusIlPlonge()
        {
            float bas = Plongee(0.3f), haut = Plongee(2.5f);
            Assert.That(haut, Is.GreaterThan(bas + 0.005f), $"tombé de 2,5 m : {haut:0.000} m ; de 0,3 m : {bas:0.000} m");
            // borné : une chute de très haut ne l'envoie pas au fond
            float pulsation = Mathf.Sqrt(-Physics.gravity.y / reglages.enfoncementParGrenouille);
            Assert.That(haut, Is.LessThan(reglages.enfoncementParGrenouille + reglages.impactMax / pulsation + 0.01f));
        }

        [Test]
        public void PencheDuCoteDeLaGrenouille()
        {
            Scene();
            var n = Nouveau(new Vector3(0f, 0.08f, 0f));
            var joueur = Joueur(new Vector3(0.7f, 0.38f, 0f));   // vers le bord +x
            float dt = 1f / 60f;
            for (int i = 0; i < 150; i++) Avancer(joueur, dt);

            Vector3 droite = n.transform.position + new Vector3(0.9f, 0f, 0f), gauche = n.transform.position - new Vector3(0.9f, 0f, 0f);
            Assert.That(n.DecalageEn(droite), Is.GreaterThan(n.DecalageEn(gauche) + 0.02f), "le côté de la grenouille ne descend pas");
            Assert.That(n.Dessin.TransformPoint(new Vector3(0.9f, 0f, 0f)).y, Is.LessThan(n.Dessin.TransformPoint(new Vector3(-0.9f, 0f, 0f)).y),
                "le dessin penche du mauvais côté");
            Assert.That(n.Pente.magnitude, Is.LessThanOrEqualTo(reglages.penteAuBord + 0.2f), "il penche plus qu'au bord");
        }

        [Test]
        public void PlusieursGrenouillesSansCoulerAuFond()
        {
            Scene();
            var n = Nouveau(new Vector3(0f, 0.08f, 0f));
            var grenouilles = new[] { new GameObject("A"), new GameObject("B"), new GameObject("C") };
            try
            {
                for (int i = 0; i < 240; i++)
                {
                    foreach (var g in grenouilles) n.Porter(g, n.transform.position, 0f);
                    n.Pas(1f / 60f);
                }
                Assert.That(n.NombreDePorteurs, Is.EqualTo(3));
                Assert.That(n.Enfoncement, Is.EqualTo(reglages.enfoncementMax).Within(0.005f), "trois grenouilles : au plus enfoncementMax");
            }
            finally
            {
                foreach (var g in grenouilles) Object.DestroyImmediate(g);
            }
        }

        [Test]
        public void LaFrequenceSeDeriveDeLaProfondeur([Values(0.03f, 0.05f, 0.08f)] float profondeur)
        {
            reglages.enfoncementParGrenouille = profondeur;
            reglages.amortissement = 0.05f;   // presque libre, pour lire la période
            Scene();
            var n = Nouveau(new Vector3(0f, 0.08f, 0f));
            n.Pousser(0.3f);
            float dt = 1f / 240f, t = 0f, avant = 0f;
            var passages = new List<float>();
            for (int i = 0; i < 2400 && passages.Count < 3; i++)
            {
                n.Pas(dt);
                t += dt;
                float e = n.Enfoncement;
                if (i > 0 && (avant > 0f) != (e > 0f)) passages.Add(t - dt * e / (e - avant));   // passage par le repos
                avant = e;
            }
            Assert.That(passages.Count, Is.EqualTo(3), "il n'oscille pas");
            float w = Mathf.Sqrt(-Physics.gravity.y / profondeur);
            float periode = 2f * Mathf.PI / (w * Mathf.Sqrt(1f - 0.05f * 0.05f));
            Assert.That(passages[2] - passages[0], Is.EqualTo(periode).Within(periode * 0.03f), $"période mesurée à {profondeur * 100:0} cm");
        }

        [Test]
        public void LeDessinDeFroggySuitLeNenuphar()
        {
            Scene();
            var n = Nouveau(new Vector3(0f, 0.08f, 0f));
            var joueur = Joueur(new Vector3(0.4f, 0.38f, 0.2f));
            var visuel = joueur.transform.Find("Visual");
            var corps = joueur.GetComponent<Rigidbody>();
            float dt = 1f / 60f;
            for (int i = 0; i < 90; i++) Avancer(joueur, dt);
            float descendu = joueur.transform.position.y - visuel.position.y;
            Assert.That(descendu, Is.EqualTo(n.DecalageEn(joueur.transform.position)).Within(0.003f), "le dessin de Froggy flotte au-dessus du nénuphar");
            Assert.That(descendu, Is.GreaterThan(0.03f));

            joueur.Imposer(new DeplacementWow.Entrees { sautAppuye = true, sautTenu = true });
            for (int i = 0; i < 12; i++) Avancer(joueur, dt);   // 0,2 s en l'air
            Assert.That(joueur.AuSol, Is.False);
            Assert.That(joueur.transform.position.y - visuel.position.y, Is.EqualTo(0f).Within(0.005f), "en l'air, le dessin ne revient pas au corps");
        }

        // ---- le décor ----

        private void Scene()
        {
            if (scene.IsValid()) EditorSceneManager.ClosePreviewScene(scene);
            nenuphars.Clear();
            scene = EditorSceneManager.NewPreviewScene();
            physique = scene.GetPhysicsScene();
        }

        /// <summary>Un vrai préfabriqué (tourné de lacet degrés), le dessus à la hauteur voulue, avec une copie du réglage.</summary>
        private Nenuphar Nouveau(Vector3 dessus, string chemin = CheminPrefab, float lacet = 0f)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(chemin), scene);
            go.transform.rotation = Quaternion.Euler(0f, lacet, 0f) * go.transform.rotation;
            Physics.SyncTransforms();
            var b = go.GetComponent<Collider>().bounds;
            go.transform.position += new Vector3(dessus.x - b.center.x, dessus.y - b.max.y, dessus.z - b.center.z);
            Physics.SyncTransforms();
            var n = go.GetComponent<Nenuphar>();
            n.Regler(reglages);
            nenuphars.Add(n);
            return n;
        }

        private void Bloc(string nom, Vector3 dessus, Vector3 taille)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = nom;
            SceneManager.MoveGameObjectToScene(go, scene);
            go.transform.position = dessus + Vector3.down * (taille.y * 0.5f);
            go.transform.localScale = taille;
            Physics.SyncTransforms();
        }

        private DeplacementWow Joueur(Vector3 pieds)
        {
            var go = new GameObject("Froggy");
            SceneManager.MoveGameObjectToScene(go, scene);
            go.transform.position = pieds + Vector3.up * 0.52f;
            var boite = go.AddComponent<BoxCollider>();
            boite.sharedMaterial = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(CheminMatiere);
            var corps = go.AddComponent<Rigidbody>();
            corps.freezeRotation = true;
            new GameObject("Visual").transform.SetParent(go.transform, false);
            var joueur = go.AddComponent<DeplacementWow>();
            joueur.Regler(course, wow);
            joueur.Imposer(new DeplacementWow.Entrees());
            return joueur;
        }

        /// <summary>Un pas physique, puis une image : les nénuphars et le dessin de Froggy, comme en jeu.</summary>
        private void Avancer(DeplacementWow joueur, float dt)
        {
            joueur.Pas(dt);
            physique.Simulate(dt);
            foreach (var n in nenuphars) n.Pas(dt);
            joueur.Dessiner(dt);
        }

        /// <summary>Tombe d'une hauteur au centre d'un nénuphar : de combien il plonge, au plus, dans la première demi-seconde.</summary>
        private float Plongee(float hauteur)
        {
            Scene();
            var n = Nouveau(new Vector3(0f, 0.08f, 0f));
            var joueur = Joueur(new Vector3(0f, 0.08f + hauteur, 0f));
            float dt = 1f / 120f, plusBas = 0f, chute = Mathf.Sqrt(2f * hauteur / -Physics.gravity.y);
            for (float t = 0f; t < chute + 0.5f; t += dt)
            {
                Avancer(joueur, dt);
                plusBas = Mathf.Max(plusBas, n.Enfoncement);
            }
            return plusBas;
        }
    }
}
