using UnityEngine;
using UnityEngine.SceneManagement;

namespace Froggy.Reseau
{
    /// <summary>
    /// Scène de test réseau (P2-01) : charge le jardin japonais par-dessus, sans le modifier, et y coupe
    /// le joueur local (PlayerTemplate) pour ne garder que les joueurs réseau.
    /// Chaque instance (hôte et client) charge le jardin chez elle : c'est du décor, rien n'est envoyé.
    /// </summary>
    public class ChargeurJardinReseau : MonoBehaviour
    {
        [Tooltip("Scène de décor à charger en additif (doit être dans les Build Settings pour un build).")]
        [SerializeField] private string sceneJardin = "Vitrine_JardinJaponais";

        [Tooltip("Objets du jardin à désactiver une fois chargé (joueur et caméra locaux).")]
        [SerializeField] private string[] objetsADesactiver = { "PlayerTemplate" };

        private void Awake()
        {
            if (SceneManager.GetSceneByName(sceneJardin).isLoaded)
            {
                Couper(SceneManager.GetSceneByName(sceneJardin));
                return;
            }

            SceneManager.sceneLoaded += QuandSceneChargee;
            SceneManager.LoadScene(sceneJardin, LoadSceneMode.Additive);
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= QuandSceneChargee;
        }

        private void QuandSceneChargee(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != sceneJardin)
                return;

            SceneManager.sceneLoaded -= QuandSceneChargee;
            Couper(scene);
        }

        private void Couper(Scene scene)
        {
            foreach (var racine in scene.GetRootGameObjects())
            {
                if (System.Array.IndexOf(objetsADesactiver, racine.name) >= 0)
                    racine.SetActive(false);
            }
        }
    }
}
