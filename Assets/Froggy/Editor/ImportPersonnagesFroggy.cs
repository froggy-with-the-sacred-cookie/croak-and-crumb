using System.Linq;
using UnityEditor;

namespace Froggy.Outils
{
    /// <summary>
    /// Import des personnages Meshy rangés en Assets/Froggy/Personnages/&lt;Modèle&gt;/Froggy_&lt;X&gt;_&lt;Clip&gt;.fbx : humanoïde,
    /// avatar créé depuis chaque FBX (le saut Prime a son propre repère d'os, le retargeting humanoïde l'absorbe), un clip
    /// par fichier nommé d'après le fichier (Repos, Marche…), joué sur place : le déplacement vient du joueur.
    /// Un réglage changé à la main dans l'inspecteur est réécrit à chaque import.
    /// </summary>
    public class ImportPersonnagesFroggy : AssetPostprocessor
    {
        public const string Dossier = "Assets/Froggy/Personnages/";
        private static readonly string[] Boucles = { "Repos", "Marche", "Course" };

        private bool Concerne => assetPath.StartsWith(Dossier) && assetPath.ToLowerInvariant().EndsWith(".fbx");

        private void OnPreprocessModel()
        {
            if (!Concerne) return;
            var importeur = (ModelImporter)assetImporter;
            importeur.animationType = ModelImporterAnimationType.Human;
            importeur.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importeur.importAnimation = true;
            importeur.importCameras = false;
            importeur.importLights = false;
            importeur.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
        }

        private void OnPreprocessAnimation()
        {
            if (!Concerne) return;
            var importeur = (ModelImporter)assetImporter;
            string fichier = System.IO.Path.GetFileNameWithoutExtension(assetPath);
            string clip = fichier.Substring(fichier.LastIndexOf('_') + 1);
            bool boucle = Boucles.Contains(clip);
            var clips = importeur.defaultClipAnimations;
            foreach (var c in clips)
            {
                c.name = clip;
                c.loopTime = boucle;
                c.loopPose = boucle;
                // Tout est « cuit dans la pose » : orientation, hauteur (d'après les pieds) et avancée restent dans le clip.
                c.lockRootRotation = true;
                c.keepOriginalOrientation = true;
                c.lockRootHeightY = true;
                c.heightFromFeet = true;
                c.lockRootPositionXZ = true;
                c.keepOriginalPositionXZ = true;
            }
            importeur.clipAnimations = clips;
        }
    }
}
