using Froggy.Regles;
using Froggy.Reseau;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Froggy.Interface
{
    /// <summary>
    /// Le menu pause (#131) : Échap au clavier ou Start à la manette, pendant une partie. « Reprendre », « Paramètres »
    /// (les mêmes qu'au menu principal, P6-05), « Rejouer »
    /// (solo) et « Retour au menu ». En solo, la partie se fige (Time.timeScale à 0 : le temps de jeu, les règles et les
    /// bots avancent au pas du jeu) ; en ligne, elle continue (Notion : « pause (le jeu continue en multijoueur) ») et
    /// « Quitter la partie » quitte le salon. Échap, Start ou B referment. S'installe seul au lancement, dans le style
    /// des menus de Julien (FabriqueMenu).
    /// </summary>
    public sealed class MenuPause : MonoBehaviour
    {
        private static MenuPause instance;

        private Canvas canvas;
        private GameObject systemeSaisie;
        private Button reprendre, parametres, rejouer, quitter;
        private RectTransform panneauPause;
        private PanneauReglages reglages;
        private float echelleAvant = 1f;
        private bool fige;

        /// <summary>Vrai tant que le menu pause est à l'écran.</summary>
        public static bool Ouvert => instance != null && instance.canvas != null && instance.canvas.gameObject.activeSelf;

        /// <summary>Solo : aucun salon connecté. La pause fige alors la partie.</summary>
        public static bool EnSolo => Salon.Local == null || !Salon.Local.Connecte;

        /// <summary>Une partie tourne, et le menu principal n'est pas à l'écran.</summary>
        private static bool EnPartie => !MenuPrincipal.MenuVisible() && FindAnyObjectByType<DeroulementPartie>() != null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reinitialiser() => instance = null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Installer()
        {
            if (instance != null) return;
            var objet = new GameObject("Menu pause");
            DontDestroyOnLoad(objet);
            instance = objet.AddComponent<MenuPause>();
        }

        private void OnDestroy()
        {
            Degeler();
            if (instance == this) instance = null;
        }

        private void Update()
        {
            if (Ouvert && !EnPartie)
            {
                Fermer();   // la partie s'est arrêtée sous la pause (déconnexion, retour au salon)
                return;
            }

            if (PanneauReglages.EnEcoute) return;   // Échap annule l'écoute d'une touche, sans refermer
            Keyboard clavier = Keyboard.current;
            Gamepad manette = Gamepad.current;
            bool bascule = (clavier != null && clavier.escapeKey.wasPressedThisFrame)
                           || (manette != null && manette.startButton.wasPressedThisFrame);
            bool retour = Ouvert && manette != null && manette.buttonEast.wasPressedThisFrame;
            if (Ouvert && ReglagesOuverts && (bascule || retour))
            {
                reglages.Retour();   // Paramètres : des Commandes aux réglages, puis à la pause
                return;
            }
            if (bascule)
            {
                if (Ouvert) Fermer();
                else if (EnPartie) Ouvrir();
                return;
            }
            if (retour) Fermer();
        }

        /// <summary>Ouvre la pause : en solo, la partie se fige.</summary>
        public void Ouvrir()
        {
            if (canvas == null) Construire();
            bool solo = EnSolo;
            rejouer.gameObject.SetActive(solo);
            FabriqueMenu.Libeller(quitter, solo ? "Retour au menu" : "Quitter la partie");
            canvas.gameObject.SetActive(true);
            MontrerReglages(false);
            AssurerEventSystem();
            if (solo && !fige)
            {
                echelleAvant = Time.timeScale;
                Time.timeScale = 0f;
                fige = true;
            }
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (EventSystem.current != null) reprendre.Select();
        }

        /// <summary>Referme la pause et rend le temps à la partie.</summary>
        public void Fermer()
        {
            if (reglages != null) reglages.gameObject.SetActive(false);   // enregistre les réglages changés
            if (canvas != null) canvas.gameObject.SetActive(false);
            Degeler();
        }

        private bool ReglagesOuverts => reglages != null && reglages.gameObject.activeSelf;

        private void MontrerReglages(bool oui)
        {
            if (oui && reglages == null)
            {
                GameObject modele = Resources.Load<GameObject>("MenuEtang/Menu_Principal_Etang");
                var f = new FabriqueMenu(modele != null ? modele.GetComponent<VueMenuEtang>() : null);
                reglages = PanneauReglages.Creer(canvas.transform, f, () => MontrerReglages(false));
            }
            if (reglages != null) reglages.gameObject.SetActive(oui);
            panneauPause.gameObject.SetActive(!oui);
            if (EventSystem.current == null) return;
            Selectable cible = oui ? reglages.PremierFocus : parametres;
            if (cible != null) cible.Select();
        }

        private void Degeler()
        {
            if (!fige) return;
            Time.timeScale = echelleAvant > 0f ? echelleAvant : 1f;
            fige = false;
        }

        private void Rejouer()
        {
            Fermer();
            DeroulementPartie partie = FindAnyObjectByType<DeroulementPartie>();
            if (partie != null) partie.Rejouer();
        }

        /// <summary>
        /// Solo : on recharge la scène Menu (MenuPrincipal s'y réinstalle). En ligne : on quitte le salon ; la partie se
        /// décharge et le menu revient de lui-même (pour l'hôte, le salon se ferme pour tout le monde).
        /// </summary>
        private void Quitter()
        {
            Fermer();
            Salon salon = Salon.Local;
            if (salon != null && salon.Connecte)
            {
                salon.Quitter();
                return;
            }
            SceneManager.LoadScene(MenuPrincipal.SceneMenu);
        }

        private void Construire()
        {
            var objet = new GameObject("Pause", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            objet.transform.SetParent(transform, false);
            canvas = objet.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;
            var scaler = objet.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

            // un voile sur la partie, qui arrête aussi les clics
            var voile = new GameObject("Voile", typeof(RectTransform), typeof(Image));
            voile.transform.SetParent(objet.transform, false);
            FabriqueMenu.Etirer((RectTransform)voile.transform);
            voile.GetComponent<Image>().color = new Color(0.02f, 0.07f, 0.11f, 0.62f);

            GameObject modele = Resources.Load<GameObject>("MenuEtang/Menu_Principal_Etang");
            var f = new FabriqueMenu(modele != null ? modele.GetComponent<VueMenuEtang>() : null);
            RectTransform panneau = FabriqueMenu.Panneau("Panneau", objet.transform, new Vector2(0f, -10f),
                new Vector2(720f, 760f), FabriqueMenu.Nuit, FabriqueMenu.Turquoise);
            panneauPause = panneau;
            f.Titre("Pause", panneau, new Vector2(0f, 275f));
            reprendre = f.Feuille("Reprendre", panneau, true, new Vector2(0f, 135f), new Vector2(520f, 124f), Fermer);
            parametres = f.Feuille("Paramètres", panneau, false, new Vector2(0f, 5f), new Vector2(480f, 112f),
                () => MontrerReglages(true));
            rejouer = f.Feuille("Rejouer", panneau, false, new Vector2(0f, -120f), new Vector2(480f, 112f), Rejouer);
            quitter = f.Feuille("Retour au menu", panneau, false, new Vector2(0f, -245f), new Vector2(480f, 112f),
                Quitter);
            canvas.gameObject.SetActive(false);
        }

        /// <summary>En jeu, il n'y a pas toujours d'EventSystem (celui du menu est caché avec lui) : on en pose un.</summary>
        private void AssurerEventSystem()
        {
            if (EventSystem.current != null && EventSystem.current.isActiveAndEnabled) return;
            if (systemeSaisie == null)
            {
                systemeSaisie = new GameObject("Saisie de la pause", typeof(EventSystem),
                    typeof(InputSystemUIInputModule));
                systemeSaisie.transform.SetParent(canvas.transform, false);
            }
            systemeSaisie.SetActive(true);
        }
    }
}
