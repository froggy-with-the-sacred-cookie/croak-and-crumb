using UnityEditor;
using UnityEngine;

namespace Froggy.Outils
{
    /// <summary>
    /// Les icônes des touches (Interface/Resources/Touches, IconesTouches) et celles du HUD habillé (Interface/Resources/
    /// Hud, VueHud) s'importent en sprites d'interface : sans mipmaps, transparence nette, sans compression (de petites
    /// images qui doivent rester piquées).
    /// </summary>
    public class ImportIconesTouches : AssetPostprocessor
    {
        public const string Dossier = "Assets/Froggy/Interface/Resources/Touches/";
        public const string DossierHud = "Assets/Froggy/Interface/Resources/Hud/";

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Dossier) && !assetPath.StartsWith(DossierHud)) return;
            var importeur = (TextureImporter)assetImporter;
            importeur.textureType = TextureImporterType.Sprite;
            importeur.spriteImportMode = SpriteImportMode.Single;
            importeur.mipmapEnabled = false;
            importeur.alphaIsTransparency = true;
            importeur.filterMode = FilterMode.Bilinear;
            importeur.textureCompression = TextureImporterCompression.Uncompressed;
            importeur.wrapMode = TextureWrapMode.Clamp;
        }
    }
}
