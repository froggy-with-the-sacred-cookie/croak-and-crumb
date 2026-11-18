using UnityEngine;
using UnityEngine.SceneManagement;

namespace Froggy.Jardin
{
    /// <summary>
    /// Change le contour de l'arène depuis le menu de debug (éditeur et builds de développement) : la muraille artisanale
    /// du temple, la muraille simple, une bordure de bambous et de roseaux, ou la muraille de Chine. Les trois premières
    /// vivent dans la vitrine agrandie (on allume l'une, on éteint les autres) ; la muraille de Chine a sa propre scène,
    /// parce qu'elle a besoin de son sol en crête : la choisir charge l'autre scène, et le choix voyage avec le
    /// chargement. Dans un build, les deux scènes doivent être dans les Build Settings.
    /// </summary>
    public class ChoixMuraille : MonoBehaviour
    {
        public enum Contour { TempleArtisanal, TempleSimple, Bambous, Chine }

        public static readonly string[] Noms =
            { "Temple japonais (muraille artisanale)", "Temple japonais (muraille simple)", "Bambous et roseaux", "Muraille de Chine (autre scène)" };

        [SerializeField] private string sceneTemple = "Assets/Froggy/Scenes/Vitrine_JardinJaponais_Grande.unity";
        [SerializeField] private string sceneChine = "Assets/Froggy/Scenes/Vitrine_JardinJaponais_Chine.unity";
        [SerializeField] private bool estChine;
        [SerializeField] private GameObject artisanale, simple, bambous;

        // le choix demandé avant un changement de scène, appliqué par le ChoixMuraille de la scène suivante
        private static Contour? demande;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Oublier() => demande = null;   // sans rechargement de domaine, le statique survivrait d'une partie à l'autre

        /// <summary>Ce qui vient d'être chargé depuis un autre contour : le menu se rouvre pour qu'on voie le résultat.</summary>
        public static bool VientDeChanger { get; private set; }

        private void Awake()
        {
            if (!Application.isEditor && !Debug.isDebugBuild) enabled = false;
        }

        private void Start()
        {
            VientDeChanger = demande.HasValue;
            if (demande.HasValue && demande.Value != Contour.Chine) Appliquer(demande.Value);
            demande = null;
        }

        public Contour Actuel =>
            estChine ? Contour.Chine
            : bambous != null && bambous.activeSelf ? Contour.Bambous
            : simple != null && simple.activeSelf ? Contour.TempleSimple
            : Contour.TempleArtisanal;

        /// <summary>Passe à un contour : dans la scène si elle l'a, sinon en chargeant l'autre scène.</summary>
        public void Choisir(Contour contour)
        {
            if (contour == Actuel) return;
            if (contour == Contour.Chine || estChine)
            {
                demande = contour;
                Charger(contour == Contour.Chine ? sceneChine : sceneTemple);
                return;
            }
            Appliquer(contour);
        }

        /// <summary>Allume le contour voulu et éteint les autres (dans la scène du temple).</summary>
        public void Appliquer(Contour contour)
        {
            if (artisanale != null) artisanale.SetActive(contour == Contour.TempleArtisanal);
            if (simple != null) simple.SetActive(contour == Contour.TempleSimple);
            if (bambous != null) bambous.SetActive(contour == Contour.Bambous);
        }

        private static void Charger(string chemin)
        {
#if UNITY_EDITOR
            // dans l'éditeur, une scène hors des Build Settings se charge quand même
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(chemin, new LoadSceneParameters(LoadSceneMode.Single));
#else
            SceneManager.LoadScene(System.IO.Path.GetFileNameWithoutExtension(chemin));
#endif
        }
    }
}
