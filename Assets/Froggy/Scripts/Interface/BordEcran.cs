using UnityEngine;

namespace Froggy.Interface
{
    /// <summary>
    /// Où poser un repère qui suit quelque chose du monde (#152, le cookie sacré), en C# pur : au-dessus de la chose
    /// quand elle est à l'écran, sinon au bord de l'écran, dans sa direction (derrière la caméra : du côté opposé, là où
    /// il faut se tourner). Les positions sont en coordonnées d'écran normalisées (0 à 1, comme un viewport).
    /// </summary>
    public static class BordEcran
    {
        /// <summary>
        /// La zone où un repère peut se poser, en coordonnées normalisées : on laisse le haut au HUD (joueurs, minuteur,
        /// annonces) et le coin bas droit aux capacités.
        /// </summary>
        public static readonly Rect ZoneLibre = Rect.MinMaxRect(.05f, .1f, .95f, .74f);

        /// <summary>Vrai si le point (viewport, z = profondeur) n'est pas dans la zone libre, ou derrière la caméra.</summary>
        public static bool HorsChamp(Vector3 viewport, Rect zone) =>
            viewport.z <= 0f || !zone.Contains(new Vector2(viewport.x, viewport.y));

        /// <summary>
        /// Le point du bord de la zone dans la direction de « viewport » vue du centre de l'écran, et l'angle de la
        /// flèche (degrés, 0 = vers la droite, 90 = vers le haut), corrigé du format de l'écran (largeur / hauteur).
        /// </summary>
        public static Vector2 AuBord(Vector3 viewport, Rect zone, float format, out float angle)
        {
            var centre = new Vector2(.5f, .5f);
            Vector2 d = new Vector2(viewport.x, viewport.y) - centre;
            if (viewport.z < 0f) d = -d;                   // derrière la caméra : la projection est retournée
            if (d.sqrMagnitude < 1e-8f) d = Vector2.down;  // pile derrière : vers le bas, « retourne-toi »
            angle = Mathf.Atan2(d.y, d.x * format) * Mathf.Rad2Deg;

            // le long du rayon, jusqu'au premier bord de la zone
            float t = float.PositiveInfinity;
            if (d.x > 0f) t = Mathf.Min(t, (zone.xMax - centre.x) / d.x);
            if (d.x < 0f) t = Mathf.Min(t, (zone.xMin - centre.x) / d.x);
            if (d.y > 0f) t = Mathf.Min(t, (zone.yMax - centre.y) / d.y);
            if (d.y < 0f) t = Mathf.Min(t, (zone.yMin - centre.y) / d.y);
            Vector2 p = centre + d * t;
            return new Vector2(Mathf.Clamp(p.x, zone.xMin, zone.xMax), Mathf.Clamp(p.y, zone.yMin, zone.yMax));
        }
    }
}
