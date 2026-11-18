using UnityEngine;

namespace Froggy.Jardin
{
    /// <summary>
    /// Bascule entre le rendu d'avant (fond uni, eau simple, sans post-traitement) et le nouveau (ciel, vraie eau,
    /// post-traitement), pour que l'équipe compare avant de choisir (garde-fou n° 7). Décocher <see cref="nouveauRendu"/>
    /// rend l'ancien. Les réglages du soleil et des modèles ne changent pas.
    /// </summary>
    [ExecuteAlways]
    public class RenduJardin : MonoBehaviour
    {
        public bool nouveauRendu = true;

        [SerializeField] private Renderer eau;
        [SerializeField] private Material eauAncienne;
        [SerializeField] private Material eauNouvelle;
        [SerializeField] private Material ciel;
        [SerializeField] private GameObject postTraitement;
        [SerializeField] private Camera[] cameras = new Camera[0];
        [SerializeField] private Color fondAncien = new Color(0.70f, 0.82f, 0.90f);
        [SerializeField] private Color brouillardNouveau = new Color(0.80f, 0.88f, 0.93f);

        private bool? applique;

        public void Regler(Renderer eau, Material eauAncienne, Material eauNouvelle, Material ciel, GameObject postTraitement, Camera[] cameras)
        {
            this.eau = eau;
            this.eauAncienne = eauAncienne;
            this.eauNouvelle = eauNouvelle;
            this.ciel = ciel;
            this.postTraitement = postTraitement;
            this.cameras = cameras;
            applique = null;
        }

        private void OnEnable() { applique = null; Appliquer(); }

        private void Update() { if (applique != nouveauRendu) Appliquer(); }

        public void Appliquer()
        {
            applique = nouveauRendu;
            RenderSettings.skybox = nouveauRendu ? ciel : null;
            RenderSettings.fogColor = nouveauRendu ? brouillardNouveau : fondAncien;
            RenderSettings.fogStartDistance = nouveauRendu ? 55f : 30f;   // le ciel porte déjà la profondeur
            RenderSettings.fogEndDistance = nouveauRendu ? 170f : 110f;
            if (eau != null) eau.sharedMaterial = nouveauRendu ? eauNouvelle : eauAncienne;
            if (postTraitement != null) postTraitement.SetActive(nouveauRendu);
            foreach (var cam in cameras)
            {
                if (cam == null) continue;
                cam.clearFlags = nouveauRendu ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
                cam.backgroundColor = fondAncien;
            }
            DynamicGI.UpdateEnvironment();   // reflets et ambiante des matériaux Lit suivent le ciel
        }
    }
}
