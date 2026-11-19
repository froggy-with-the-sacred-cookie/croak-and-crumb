using UnityEngine;
using UnityEngine.SceneManagement;

namespace Froggy.Socle
{
    /// <summary>
    /// Dans une scène de test (socle commun, T-01) : charge un décor par-dessus, avec sa Froggy et sa caméra, au lieu
    /// d'en garder une copie. Il n'y a qu'un jardin : quand il change, toutes les scènes de test le voient, et personne
    /// ne modifie son fichier en travaillant dans la sienne. Pour tester sur une autre arène, changer « decor ».
    /// Le décor devient la scène active en jeu : la lumière, le ciel et le brouillard viennent de la scène active.
    /// Dans l'éditeur, ScenesDeTest l'ouvre aussi à côté de la scène de test, pour le voir sans lancer le jeu.
    /// </summary>
    public class DecorDeTest : MonoBehaviour
    {
        public const string Jardin = "Assets/Froggy/Scenes/Vitrine_JardinJaponais.unity";

        [Tooltip("Chemin de la scène qui sert de décor (elle apporte Froggy, la caméra, la lumière).")]
        [SerializeField] private string decor = Jardin;

        public string Decor { get => decor; set => decor = value; }

        private void Start()
        {
            Scene scene = SceneManager.GetSceneByPath(decor);
            if (scene.IsValid() && scene.isLoaded) { Activer(); return; }   // déjà ouvert à côté dans l'éditeur
            // déjà en cours de chargement (en ligne, le salon charge le décor juste avant la partie) : on l'attend
            if (scene.IsValid()) { StartCoroutine(AttendreLeDecor()); return; }

            AsyncOperation chargement;
#if UNITY_EDITOR
            // dans l'éditeur, pas besoin que le décor soit dans la liste des scènes du build
            chargement = UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(
                decor, new LoadSceneParameters(LoadSceneMode.Additive));
#else
            chargement = SceneManager.LoadSceneAsync(decor, LoadSceneMode.Additive);
#endif
            if (chargement == null) { Debug.LogError($"Décor de test introuvable : {decor}", this); return; }
            chargement.completed += _ => Activer();
        }

        private System.Collections.IEnumerator AttendreLeDecor()
        {
            while (!SceneManager.GetSceneByPath(decor).isLoaded) yield return null;
            Activer();
        }

        private void Activer()
        {
            Scene scene = SceneManager.GetSceneByPath(decor);
            if (scene.IsValid() && scene.isLoaded) SceneManager.SetActiveScene(scene);
        }
    }
}
