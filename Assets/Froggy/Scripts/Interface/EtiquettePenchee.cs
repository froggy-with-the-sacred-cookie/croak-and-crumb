using UnityEngine;
using UnityEngine.UI;

namespace Froggy.Interface
{
    /// <summary>
    /// L'étiquette penchée des annonces (recherche HUD, 19/11/2026 : Fall Guys penche ses étiquettes au lieu de les
    /// encadrer) : un parallélogramme, sa pente en unités du canvas.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public class EtiquettePenchee : MaskableGraphic
    {
        [SerializeField] private float pente = 14f;

        public float Pente
        {
            get => pente;
            set { pente = value; SetVerticesDirty(); }
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect r = GetPixelAdjustedRect();
            Color32 c = color;
            vh.AddVert(new Vector3(r.xMin - pente, r.yMin), c, Vector2.zero);
            vh.AddVert(new Vector3(r.xMin + pente, r.yMax), c, Vector2.zero);
            vh.AddVert(new Vector3(r.xMax + pente, r.yMax), c, Vector2.zero);
            vh.AddVert(new Vector3(r.xMax - pente, r.yMin), c, Vector2.zero);
            vh.AddTriangle(0, 1, 2);
            vh.AddTriangle(2, 3, 0);
        }
    }
}
