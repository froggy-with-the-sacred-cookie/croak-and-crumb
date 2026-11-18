using UnityEditor;

namespace Froggy.Outils
{
    /// <summary>
    /// Réglages d'import des modèles faits par script Blender (Tools/blender) : axes convertis dans le maillage (pas de
    /// rotation de -90° sur la racine), aucun matériau importé (le préfabriqué pose Froggy/CouleurSommet), pas d'animation.
    /// S'applique à chaque import sous Assets/Froggy/Models : un réglage changé à la main dans l'inspecteur est réécrit.
    /// </summary>
    public class ImportModelesFroggy : AssetPostprocessor
    {
        private void OnPreprocessModel()
        {
            if (!assetPath.StartsWith("Assets/Froggy/Models/")) return;
            var importeur = (ModelImporter)assetImporter;
            importeur.bakeAxisConversion = true;
            importeur.materialImportMode = ModelImporterMaterialImportMode.None;
            importeur.importNormals = ModelImporterNormals.Import;
            importeur.animationType = ModelImporterAnimationType.None;
            importeur.importAnimation = false;
            importeur.importCameras = false;
            importeur.importLights = false;
        }
    }
}
