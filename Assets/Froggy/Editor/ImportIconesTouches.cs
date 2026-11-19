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
        public const string DossierCartes = "Assets/Froggy/Interface/Resources/Cartes/";

        private void OnPreprocessTexture()
        {
            // les peintures des cartes (écran de chargement, accueil) : grandes, donc compressées, sans mipmaps
            if (assetPath.StartsWith(DossierCartes))
            {
                var peinture = (TextureImporter)assetImporter;
                peinture.textureType = TextureImporterType.Sprite;
                peinture.spriteImportMode = SpriteImportMode.Single;
                peinture.mipmapEnabled = false;
                peinture.wrapMode = TextureWrapMode.Clamp;
                peinture.maxTextureSize = 2048;
                peinture.textureCompression = TextureImporterCompression.CompressedHQ;
                return;
            }
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
