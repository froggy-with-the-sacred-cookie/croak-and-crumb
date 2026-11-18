using UnityEngine;

namespace Froggy.Deplacement
{
    /// <summary>
    /// Un volume d'eau où Froggy nage (P1-02) : une boîte invisible (déclencheur) posée sur un lac ou un bassin, dont le
    /// dessus est la surface. Quand le centre de Froggy est dedans, DeplacementWow passe la main à NageFroggy.
    /// À poser dans chaque arène sur ses plans d'eau, assez profond pour nager (Froggy fait 1 m) : là où l'eau est moins
    /// profonde, Froggy a pied et marche au fond.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class ZoneEau : MonoBehaviour
    {
        private BoxCollider boite;

        /// <summary>La hauteur de la surface, dans le monde.</summary>
        public float Surface
        {
            get
            {
                if (boite == null) boite = GetComponent<BoxCollider>();
                return transform.TransformPoint(boite.center + Vector3.up * boite.size.y * 0.5f).y;
            }
        }

        private void Reset() => GetComponent<BoxCollider>().isTrigger = true;

        private void OnValidate()
        {
            var b = GetComponent<BoxCollider>();
            if (b != null && !b.isTrigger) b.isTrigger = true;   // jamais un mur : on entre dans l'eau
        }

        /// <summary>La zone d'eau qui contient ce point, dans cette scène physique, ou null.</summary>
        public static ZoneEau Trouver(PhysicsScene physique, Vector3 point, Collider[] tampon)
        {
            int n = physique.OverlapSphere(point, 0.01f, tampon, ~0, QueryTriggerInteraction.Collide);
            for (int i = 0; i < n; i++)
            {
                if (!tampon[i].isTrigger) continue;
                var zone = tampon[i].GetComponent<ZoneEau>();
                if (zone != null) return zone;
            }
            return null;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            var b = GetComponent<BoxCollider>();
            if (b == null) return;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(0.2f, 0.55f, 1f, 0.15f);
            Gizmos.DrawCube(b.center, b.size);
            Gizmos.color = new Color(0.2f, 0.55f, 1f, 0.8f);
            Gizmos.DrawWireCube(b.center, b.size);
        }
#endif
    }
}
