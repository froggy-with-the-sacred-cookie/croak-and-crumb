using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Froggy.Outils
{
    /// <summary>
    /// Le fantôme d'un préfabriqué dans la vue Scène : ses maillages dessinés là où il serait posé, sans rien créer dans la
    /// scène (rien à annuler, rien qui gêne la visée). Les niveaux de détail au-delà du premier ne sont pas dessinés.
    /// </summary>
    internal sealed class Fantome
    {
        private struct Morceau { public Mesh maillage; public Matrix4x4 local; public Material[] materiaux; }

        private readonly List<Morceau> morceaux = new List<Morceau>();
        /// <summary>La boîte du préfabriqué posé sans rotation ni changement de taille (la rotation et l'échelle de sa racine comprises).</summary>
        public Bounds Bornes { get; private set; }

        private static readonly Dictionary<GameObject, Fantome> cache = new Dictionary<GameObject, Fantome>();

        public static void Oublier() => cache.Clear();

        public static Fantome De(GameObject modele)
        {
            if (modele == null) return null;
            if (cache.TryGetValue(modele, out var f) && f != null) return f;
            f = new Fantome(modele);
            cache[modele] = f;
            return f;
        }

        private Fantome(GameObject modele)
        {
            // le repère de pose : la racine garde sa rotation et son échelle (les FBX de Blender arrivent tournés de 90° sur X)
            var versRacine = Matrix4x4.TRS(Vector3.zero, modele.transform.localRotation, modele.transform.localScale) * modele.transform.worldToLocalMatrix;
            var loin = new HashSet<Renderer>();
            foreach (var lod in modele.GetComponentsInChildren<LODGroup>(true))
            {
                var niveaux = lod.GetLODs();
                for (int i = 1; i < niveaux.Length; i++)
                    foreach (var r in niveaux[i].renderers) if (r != null) loin.Add(r);
            }
            bool vide = true; var b = new Bounds();
            foreach (var r in modele.GetComponentsInChildren<Renderer>(true))
            {
                if (loin.Contains(r) || !r.enabled) continue;
                Mesh m = null;
                if (r is MeshRenderer && r.TryGetComponent<MeshFilter>(out var mf)) m = mf.sharedMesh;
                else if (r is SkinnedMeshRenderer smr) m = smr.sharedMesh;
                if (m == null) continue;
                var local = versRacine * r.transform.localToWorldMatrix;
                morceaux.Add(new Morceau { maillage = m, local = local, materiaux = r.sharedMaterials });
                var mb = m.bounds;
                for (int c = 0; c < 8; c++)
                {
                    var coin = local.MultiplyPoint3x4(mb.center + Vector3.Scale(mb.extents,
                        new Vector3((c & 1) == 0 ? -1 : 1, (c & 2) == 0 ? -1 : 1, (c & 4) == 0 ? -1 : 1)));
                    if (vide) { b = new Bounds(coin, Vector3.zero); vide = false; } else b.Encapsulate(coin);
                }
            }
            Bornes = b;
        }

        /// <summary>Le placement d'un préfabriqué posé en pos, tourné de rot, à taille fois sa taille (ce qu'Instancier fait).</summary>
        public static Matrix4x4 Place(Vector3 pos, Quaternion rot, Vector3 taille) => Matrix4x4.TRS(pos, rot, taille);

        /// <summary>Dessine le fantôme (appeler au Repaint de la vue Scène).</summary>
        public void Dessiner(Matrix4x4 place, Camera camera)
        {
            foreach (var m in morceaux)
            {
                var mat = place * m.local;
                for (int s = 0; s < m.maillage.subMeshCount; s++)
                {
                    var materiau = m.materiaux.Length == 0 ? null : m.materiaux[Mathf.Min(s, m.materiaux.Length - 1)];
                    if (materiau != null) Graphics.DrawMesh(m.maillage, mat, materiau, 0, camera, s);
                }
            }
        }

        /// <summary>Le cadre du fantôme, dans la couleur donnée.</summary>
        public void Cadre(Matrix4x4 place, Color couleur)
        {
            using (new Handles.DrawingScope(couleur, place))
                Handles.DrawWireCube(Bornes.center, Bornes.size);
        }

        /// <summary>La longueur du préfabriqué posé, le long de l'axe X ou Z.</summary>
        public static float Longueur(GameObject modele, TracesArene.Axe axe)
        {
            var f = De(modele); if (f == null) return 1;
            return axe == TracesArene.Axe.X ? f.Bornes.size.x : f.Bornes.size.z;
        }
    }
}
