using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Froggy.Interface
{
    /// <summary>
    /// Les formes du HUD habillé, dessinées par code (pas d'image à importer) : disque, anneau, et l'étiquette penchée.
    /// Lissées sur 1 px, blanches : la couleur vient de l'Image. Les icônes, elles, sont peintes (Resources/Hud/Icones).
    /// </summary>
    public static class FormesHud
    {
        private const int Taille = 128;
        private static Sprite disque;
        private static readonly Dictionary<int, Sprite> anneaux = new Dictionary<int, Sprite>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Vider()
        {
            disque = null;
            anneaux.Clear();
        }

        /// <summary>Un disque plein, 128 px.</summary>
        public static Sprite Disque => disque != null ? disque : disque = Dessiner("HUD_Disque", 0f);

        /// <summary>Un anneau, épaisseur en pixels sur 128 (8 : un trait de 5 px pour un bouton de 80 px).</summary>
        public static Sprite Anneau(int epaisseur)
        {
            if (anneaux.TryGetValue(epaisseur, out Sprite s) && s != null) return s;
            s = Dessiner("HUD_Anneau_" + epaisseur, epaisseur);
            anneaux[epaisseur] = s;
            return s;
        }

        private static Sprite Dessiner(string nom, float epaisseur)
        {
            var texture = new Texture2D(Taille, Taille, TextureFormat.RGBA32, false)
            {
                name = nom, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear
            };
            var pixels = new Color32[Taille * Taille];
            float r = Taille * .5f, centre = Taille * .5f;
            for (int y = 0; y < Taille; y++)
            for (int x = 0; x < Taille; x++)
            {
                float d = Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(centre, centre));
                float a = Mathf.Clamp01(r - d);
                if (epaisseur > 0f) a *= Mathf.Clamp01(d - (r - epaisseur));
                pixels[y * Taille + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0, 0, Taille, Taille), new Vector2(.5f, .5f), 100f, 0, SpriteMeshType.FullRect);
        }
    }
}
