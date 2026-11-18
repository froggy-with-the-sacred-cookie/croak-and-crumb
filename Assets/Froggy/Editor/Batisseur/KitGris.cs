using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Froggy.Outils
{
    /// <summary>
    /// Le kit de blocs gris de l'étape ① Terrain (Assets/Froggy/Prefabs/Gris) : des formes simples pour essayer une
    /// disposition avant le vrai décor (l'arène grise de P8-02), comme dans les studios (« greybox »). Un damier de 1 m sur
    /// chaque face fait lire les distances d'un coup d'œil ; les hauteurs suivent le gabarit de Froggy : une marche se
    /// monte en marchant, une plate-forme demande un saut, un mur ne se franchit pas. Couleurs : clair où l'on marche, foncé
    /// pour ce qui bloque, vert pour les nénuphars, bleu pour l'eau (sans collision, reconnue à son nom par l'analyse).
    /// Les pièces se remplacent ensuite par le vrai décor (Poser ▸ Retoucher la sélection ▸ Remplacer par).
    /// Créer le kit ne remplace jamais une pièce existante.
    /// </summary>
    public static class KitGris
    {
        public const string Dossier = "Assets/Froggy/Prefabs/Gris";
        private const string Maillages = Dossier + "/Maillages";

        private enum Forme { Boite, Rampe, Cylindre, Plan }

        private class Piece
        {
            public string nom; public Vector3 taille; public Forme forme; public Color couleur; public bool collision = true, eau;
        }

        private static readonly Color Sol = new Color(.62f, .62f, .6f), Bloc = new Color(.78f, .78f, .76f), Bloque = new Color(.42f, .42f, .44f),
            Saut = new Color(.95f, .62f, .25f), Nenuphar = new Color(.45f, .72f, .4f), Eau = new Color(.25f, .5f, .85f, .55f);

        private static readonly Piece[] Pieces =
        {
            new Piece { nom = "Gris_Dalle_4x4", taille = new Vector3(4, .5f, 4), forme = Forme.Boite, couleur = Sol },
            new Piece { nom = "Gris_Dalle_8x8", taille = new Vector3(8, .5f, 8), forme = Forme.Boite, couleur = Sol },
            new Piece { nom = "Gris_Bloc_1x1", taille = new Vector3(1, 1, 1), forme = Forme.Boite, couleur = Bloc },
            new Piece { nom = "Gris_Bloc_2x2", taille = new Vector3(2, 1, 2), forme = Forme.Boite, couleur = Bloc },
            new Piece { nom = "Gris_Marche", taille = new Vector3(2, .3f, 1), forme = Forme.Boite, couleur = Bloc },
            new Piece { nom = "Gris_Plateforme_Saut", taille = new Vector3(2, 1.2f, 2), forme = Forme.Boite, couleur = Saut },
            new Piece { nom = "Gris_Rampe", taille = new Vector3(2, 1, 4), forme = Forme.Rampe, couleur = Bloc },
            new Piece { nom = "Gris_Mur", taille = new Vector3(4, 2.5f, .5f), forme = Forme.Boite, couleur = Bloque },
            new Piece { nom = "Gris_Pilier", taille = new Vector3(.6f, 3, .6f), forme = Forme.Boite, couleur = Bloque },
            new Piece { nom = "Gris_Nenuphar", taille = new Vector3(2, .08f, 2), forme = Forme.Cylindre, couleur = Nenuphar },
            new Piece { nom = "Gris_Eau_8x8", taille = new Vector3(8, 0, 8), forme = Forme.Plan, couleur = Eau, collision = false, eau = true },
        };

        public static bool Existe => AssetDatabase.LoadAssetAtPath<GameObject>($"{Dossier}/{Pieces[0].nom}.prefab") != null;

        [MenuItem("Froggy/Bâtisseur/Créer le kit de blocs gris", priority = 20)]
        public static void Creer()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Froggy/Prefabs")) AssetDatabase.CreateFolder("Assets/Froggy", "Prefabs");
            if (!AssetDatabase.IsValidFolder(Dossier)) AssetDatabase.CreateFolder("Assets/Froggy/Prefabs", "Gris");
            if (!AssetDatabase.IsValidFolder(Maillages)) AssetDatabase.CreateFolder(Dossier, "Maillages");
            var damier = Damier();
            var materiaux = new Dictionary<Color, Material>();
            int crees = 0;
            foreach (var p in Pieces)
            {
                var chemin = $"{Dossier}/{p.nom}.prefab";
                if (AssetDatabase.LoadAssetAtPath<GameObject>(chemin) != null) continue;
                var maillage = Maillage(p);
                maillage.name = p.nom;
                AssetDatabase.CreateAsset(maillage, $"{Maillages}/{p.nom}.asset");
                if (!materiaux.TryGetValue(p.couleur, out var mat)) materiaux[p.couleur] = mat = Materiau(p, damier);
                var go = new GameObject(p.nom);
                go.AddComponent<MeshFilter>().sharedMesh = maillage;
                go.AddComponent<MeshRenderer>().sharedMaterial = mat;
                if (p.collision)
                {
                    if (p.forme == Forme.Boite) { var b = go.AddComponent<BoxCollider>(); b.center = new Vector3(0, p.taille.y / 2, 0); b.size = p.taille; }
                    else go.AddComponent<MeshCollider>().sharedMesh = maillage;
                }
                PrefabUtility.SaveAsPrefabAsset(go, chemin);
                Object.DestroyImmediate(go);
                crees++;
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"Kit de blocs gris : {crees} pièce(s) créée(s) dans {Dossier}.");
        }

        /// <summary>Le damier de 1 m (deux tons de gris clair), en texture du projet.</summary>
        private static Texture2D Damier()
        {
            var chemin = Maillages + "/Damier_1m.png";
            var t = AssetDatabase.LoadAssetAtPath<Texture2D>(chemin);
            if (t != null) return t;
            var tex = new Texture2D(64, 64, TextureFormat.RGB24, false);
            for (int y = 0; y < 64; y++)
                for (int x = 0; x < 64; x++)
                {
                    bool clair = ((x / 32) + (y / 32)) % 2 == 0;
                    bool trait = x % 32 == 0 || y % 32 == 0;
                    float v = trait ? .72f : clair ? 1f : .88f;
                    tex.SetPixel(x, y, new Color(v, v, v));
                }
            System.IO.File.WriteAllBytes(chemin, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(chemin);
            var imp = (TextureImporter)AssetImporter.GetAtPath(chemin);
            imp.wrapMode = TextureWrapMode.Repeat; imp.filterMode = FilterMode.Bilinear; imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(chemin);
        }

        private static Material Materiau(Piece p, Texture2D damier)
        {
            var nom = p.eau ? "Gris_Eau" : $"Gris_{ColorUtility.ToHtmlStringRGB(p.couleur)}";
            var chemin = $"{Maillages}/{nom}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(chemin);
            if (m != null) return m;
            m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = nom };
            m.SetColor("_BaseColor", p.couleur);
            if (p.eau)
            {
                // transparent, à la façon d'URP
                m.SetFloat("_Surface", 1); m.SetFloat("_Blend", 0);
                m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                m.SetFloat("_ZWrite", 0);
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                m.SetFloat("_Smoothness", .85f);
            }
            else
            {
                // une case du damier = 1 m : les UV des maillages sont en mètres, la texture en couvre deux
                m.SetTexture("_BaseMap", damier);
                m.SetTextureScale("_BaseMap", new Vector2(.5f, .5f));
                m.SetFloat("_Smoothness", .1f);
            }
            AssetDatabase.CreateAsset(m, chemin);
            return m;
        }

        // ---------- Les maillages (pivot au milieu du dessous, UV en mètres) ----------

        private static Mesh Maillage(Piece p)
        {
            switch (p.forme)
            {
                case Forme.Rampe: return Rampe(p.taille);
                case Forme.Cylindre: return Cylindre(p.taille.x / 2, p.taille.y, 32);
                case Forme.Plan: return Plan(p.taille);
                default: return Boite(p.taille, 0);
            }
        }

        /// <summary>Une boîte de cette taille, posée sur y = bas.</summary>
        public static Mesh Boite(Vector3 t, float bas)
        {
            var v = new List<Vector3>(); var uv = new List<Vector2>(); var tri = new List<int>();
            float x = t.x / 2, z = t.z / 2, y0 = bas, y1 = bas + t.y;
            void Face(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float u, float w)
            {
                int k = v.Count; v.Add(a); v.Add(b); v.Add(c); v.Add(d);
                uv.Add(new Vector2(0, 0)); uv.Add(new Vector2(0, w)); uv.Add(new Vector2(u, w)); uv.Add(new Vector2(u, 0));
                tri.Add(k); tri.Add(k + 1); tri.Add(k + 2); tri.Add(k); tri.Add(k + 2); tri.Add(k + 3);
            }
            Face(new Vector3(-x, y1, -z), new Vector3(-x, y1, z), new Vector3(x, y1, z), new Vector3(x, y1, -z), t.x, t.z);   // dessus
            Face(new Vector3(-x, y0, z), new Vector3(-x, y0, -z), new Vector3(x, y0, -z), new Vector3(x, y0, z), t.x, t.z);   // dessous
            Face(new Vector3(-x, y0, -z), new Vector3(-x, y1, -z), new Vector3(x, y1, -z), new Vector3(x, y0, -z), t.x, t.y); // avant (-z)
            Face(new Vector3(x, y0, z), new Vector3(x, y1, z), new Vector3(-x, y1, z), new Vector3(-x, y0, z), t.x, t.y);     // arrière
            Face(new Vector3(-x, y0, z), new Vector3(-x, y1, z), new Vector3(-x, y1, -z), new Vector3(-x, y0, -z), t.z, t.y); // gauche
            Face(new Vector3(x, y0, -z), new Vector3(x, y1, -z), new Vector3(x, y1, z), new Vector3(x, y0, z), t.z, t.y);     // droite
            return Finir(v, uv, tri);
        }

        /// <summary>Une rampe qui monte de 0 (côté -z) à t.y (côté +z).</summary>
        private static Mesh Rampe(Vector3 t)
        {
            var v = new List<Vector3>(); var uv = new List<Vector2>(); var tri = new List<int>();
            float x = t.x / 2, z = t.z / 2, h = t.y, pente = Mathf.Sqrt(t.z * t.z + h * h);
            void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float u, float w)
            {
                int k = v.Count; v.Add(a); v.Add(b); v.Add(c); v.Add(d);
                uv.Add(new Vector2(0, 0)); uv.Add(new Vector2(0, w)); uv.Add(new Vector2(u, w)); uv.Add(new Vector2(u, 0));
                tri.Add(k); tri.Add(k + 1); tri.Add(k + 2); tri.Add(k); tri.Add(k + 2); tri.Add(k + 3);
            }
            void Tri(Vector3 a, Vector3 b, Vector3 c, Vector2 ua, Vector2 ub, Vector2 uc)
            {
                int k = v.Count; v.Add(a); v.Add(b); v.Add(c); uv.Add(ua); uv.Add(ub); uv.Add(uc);
                tri.Add(k); tri.Add(k + 1); tri.Add(k + 2);
            }
            Quad(new Vector3(-x, 0, -z), new Vector3(-x, h, z), new Vector3(x, h, z), new Vector3(x, 0, -z), t.x, pente);    // la pente
            Quad(new Vector3(-x, 0, z), new Vector3(-x, 0, -z), new Vector3(x, 0, -z), new Vector3(x, 0, z), t.x, t.z);      // dessous
            Quad(new Vector3(x, 0, z), new Vector3(x, h, z), new Vector3(-x, h, z), new Vector3(-x, 0, z), t.x, h);          // le haut (+z)
            Tri(new Vector3(-x, 0, z), new Vector3(-x, h, z), new Vector3(-x, 0, -z), new Vector2(t.z, 0), new Vector2(t.z, h), Vector2.zero);
            Tri(new Vector3(x, 0, -z), new Vector3(x, h, z), new Vector3(x, 0, z), Vector2.zero, new Vector2(t.z, h), new Vector2(t.z, 0));
            return Finir(v, uv, tri);
        }

        private static Mesh Cylindre(float r, float h, int n)
        {
            var v = new List<Vector3>(); var uv = new List<Vector2>(); var tri = new List<int>();
            // le dessus et le dessous
            foreach (var (y, haut) in new[] { (h, true), (0f, false) })
            {
                int c = v.Count; v.Add(new Vector3(0, y, 0)); uv.Add(Vector2.zero);
                for (int i = 0; i <= n; i++)
                {
                    float a = i * Mathf.PI * 2 / n; var p = new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r);
                    v.Add(p); uv.Add(new Vector2(p.x, p.z));
                }
                for (int i = 0; i < n; i++)
                    if (haut) { tri.Add(c); tri.Add(c + i + 2); tri.Add(c + i + 1); }
                    else { tri.Add(c); tri.Add(c + i + 1); tri.Add(c + i + 2); }
            }
            // le bord
            int s = v.Count;
            for (int i = 0; i <= n; i++)
            {
                float a = i * Mathf.PI * 2 / n; var d = new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r);
                v.Add(d); v.Add(d + Vector3.up * h); uv.Add(new Vector2(a * r, 0)); uv.Add(new Vector2(a * r, h));
            }
            for (int i = 0; i < n; i++)
            {
                int k = s + i * 2;
                tri.Add(k); tri.Add(k + 1); tri.Add(k + 3); tri.Add(k); tri.Add(k + 3); tri.Add(k + 2);
            }
            return Finir(v, uv, tri);
        }

        private static Mesh Plan(Vector3 t)
        {
            float x = t.x / 2, z = t.z / 2;
            var v = new List<Vector3> { new Vector3(-x, 0, -z), new Vector3(-x, 0, z), new Vector3(x, 0, z), new Vector3(x, 0, -z) };
            var uv = new List<Vector2> { Vector2.zero, new Vector2(0, t.z), new Vector2(t.x, t.z), new Vector2(t.x, 0) };
            return Finir(v, uv, new List<int> { 0, 1, 2, 0, 2, 3 });
        }

        private static Mesh Finir(List<Vector3> v, List<Vector2> uv, List<int> tri)
        {
            var m = new Mesh();
            m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(tri, 0);
            m.RecalculateNormals(); m.RecalculateBounds(); m.RecalculateTangents();
            return m;
        }

        /// <summary>Le matériau du sol gris (pour le bac à sable), créé avec le kit.</summary>
        public static Material MateriauSol() => AssetDatabase.LoadAssetAtPath<Material>($"{Maillages}/Gris_{ColorUtility.ToHtmlStringRGB(Sol)}.mat");
        public static Material MateriauEau() => AssetDatabase.LoadAssetAtPath<Material>($"{Maillages}/Gris_Eau.mat");
        public static Material MateriauBloque() => AssetDatabase.LoadAssetAtPath<Material>($"{Maillages}/Gris_{ColorUtility.ToHtmlStringRGB(Bloque)}.mat");
    }
}
