using UnityEngine;
using UnityEngine.SceneManagement;

namespace Froggy.Reseau
{
    /// <summary>
    /// Dans un test réseau : coupe le joueur local du décor (PlayerTemplate du jardin, avec sa caméra) dès que le décor
    /// est chargé, pour ne garder que les joueurs réseau. Ne modifie pas la scène du décor : seulement la partie en cours.
    /// </summary>
    public class CoupeJoueurLocal : MonoBehaviour
    {
        [Tooltip("Noms des objets racines à couper dans les autres scènes chargées.")]
        [SerializeField] private string[] objetsACouper = { "PlayerTemplate" };

        private void OnEnable() => SceneManager.sceneLoaded += QuandSceneChargee;
        private void OnDisable() => SceneManager.sceneLoaded -= QuandSceneChargee;

        private void Start()
        {
            // Le décor peut déjà être ouvert (dans l'éditeur, il s'ouvre à côté de la scène de test).
            for (int i = 0; i < SceneManager.sceneCount; i++)
                Couper(SceneManager.GetSceneAt(i));
        }

        private void QuandSceneChargee(Scene scene, LoadSceneMode mode) => Couper(scene);

        private void Couper(Scene scene)
        {
            if (!scene.isLoaded) return;
            foreach (GameObject racine in scene.GetRootGameObjects())
                if (System.Array.IndexOf(objetsACouper, racine.name) >= 0 && racine.activeSelf)
                    racine.SetActive(false);
        }
    }
}
