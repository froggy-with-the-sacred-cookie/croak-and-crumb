using UnityEngine;
using UnityEngine.UI;

namespace Froggy.Interface
{
    /// <summary>
    /// Une pointe de flèche du HUD (repère du cookie sacré, #152), dessinée par code : elle pointe vers la droite, la
    /// rotation de son parent l'oriente.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public class TriangleHud : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect r = GetPixelAdjustedRect();
            Color32 c = color;
            vh.AddVert(new Vector3(r.xMin, r.yMin), c, Vector2.zero);
            vh.AddVert(new Vector3(r.xMin, r.yMax), c, Vector2.zero);
            vh.AddVert(new Vector3(r.xMax, r.center.y), c, Vector2.zero);
            vh.AddTriangle(0, 1, 2);
        }
    }
}
