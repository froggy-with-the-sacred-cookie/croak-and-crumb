using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Froggy.Interface
{
    /// <summary>
    /// Point d’entrée du menu, de ses écrans et de ses transitions de scène. S'installe seul quand la scène Menu
    /// s'ouvre. Écrans : l'accueil de Julien (l'étang), Paramètres, Rejoindre un salon, Salle d'attente ; un seul à la
    /// fois, avec le focus posé pour la manette (Échap ou B : retour).
    ///
    /// La scène Menu porte le salon (P2-03) : la partie s'y charge par-dessus, en additif. Le menu se cache donc tant
    /// qu'une autre scène est ouverte, et revient sur la salle d'attente au retour de la partie.
    /// </summary>
    public sealed class MenuPrincipal : MonoBehaviour
    {
        /// <summary>La scène du menu et du salon, première du build.</summary>
        public const string SceneMenu = "Assets/Froggy/Scenes/Menu.unity";
        /// <summary>
        /// La partie complète (règles, objets, météo, cookie sacré, bots, HUD) sur le jardin : « Solo » la lance, avec
        /// des bots. Le salon charge encore la vitrine tant que ces règles ne sont pas en réseau.
        /// </summary>
        public const string SceneSolo = "Assets/Froggy/Scenes/Partie_JardinJaponais.unity";
        private const string PrefabEtang = "MenuEtang/Menu_Principal_Etang";
        /// <summary>Volume de l'ambiance du menu quand le réglage « Ambiance » est à 100 % (choix de Julien).</summary>
        private const float VolumeAmbiance = 0.12f;
#if UNITY_EDITOR
        private static readonly Color EauCroquis = new Color32(43, 80, 121, 255);
#endif
        private static bool instanceCreee;

        private AudioSource ambiance;
        private Canvas canvas;
        private GameObject systemeSaisie;
        private RectTransform menuPrincipal;
        private VueMenuEtang etang;
        private EcranParametres ecranParametres;
        private EcranSalons ecranSalons;
        private EcranSalleAttente ecranSalle;
        private Ecran ecran = Ecran.Accueil;

        /// <summary>L'écran affiché.</summary>
        public Ecran EcranActuel => ecran;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reinitialiser()
        {
            instanceCreee = false;
            SceneManager.sceneLoaded -= QuandUneSceneArrive;
        }

        /// <summary>
        /// Au lancement si le jeu part du menu, et chaque fois que la scène Menu revient (retour d'une partie solo, y
        /// compris d'une partie lancée directement dans l'éditeur).
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreerAuDemarrage()
        {
            SceneManager.sceneLoaded -= QuandUneSceneArrive;
            SceneManager.sceneLoaded += QuandUneSceneArrive;
            Creer(SceneManager.GetActiveScene());
        }

        private static void QuandUneSceneArrive(Scene scene, LoadSceneMode mode) => Creer(scene);

        private static void Creer(Scene scene)
        {
            if (scene.path != SceneMenu || instanceCreee) return;
            instanceCreee = true;
            GameObject objet = new GameObject("Menu principal Froggy");
            DontDestroyOnLoad(objet);
            objet.AddComponent<MenuPrincipal>();
        }

        private void Awake()
        {
            ambiance = gameObject.AddComponent<AudioSource>();
            ambiance.clip = Resources.Load<AudioClip>("Ambiance_Stream_Calm_Loop_Stereo");
            ambiance.loop = true;
            ambiance.spatialBlend = 0f;
            AppliquerVolumes();
            ReglagesJoueur.Change += AppliquerVolumes;
            if (ambiance.clip != null) ambiance.Play();
            ConstruireInterface();
            CacherPanneauProvisoire();
        }

        private void OnDestroy() => ReglagesJoueur.Change -= AppliquerVolumes;

        private void Start() => Afficher(Ecran.Accueil);

        /// <summary>
        /// Le menu se montre seul dans sa scène : caché pendant une partie (chargée par-dessus par le salon) ou en solo
        /// (scène remplacée). Dans un salon, on est dans la salle d'attente ; sorti du salon, on revient aux salons.
        /// </summary>
        private void Update()
        {
            bool visible = MenuVisible();
            if (canvas.gameObject.activeSelf != visible)
            {
                canvas.gameObject.SetActive(visible);
                if (visible)
                {
                    AssurerEventSystem();
                    CacherPanneauProvisoire();   // au retour d'une partie solo, le salon de la scène Menu est neuf
                    Afficher(ecran);
                    if (ambiance.clip != null && !ambiance.isPlaying) ambiance.Play();
                }
                else ambiance.Stop();
            }
            if (!visible) return;

            Reseau.Salon salon = Reseau.Salon.Local;
            bool dansUnSalon = salon != null && salon.Connecte && salon.Moi.HasValue;
            if (dansUnSalon && ecran != Ecran.SalleAttente) Afficher(Ecran.SalleAttente);
            else if (!dansUnSalon && ecran == Ecran.SalleAttente) Afficher(Ecran.Salons);

            if (RetourDemande()) Retour();
        }

        /// <summary>Vrai quand seule la scène Menu est ouverte.</summary>
        public static bool MenuVisible() =>
            SceneManager.sceneCount == 1 && SceneManager.GetActiveScene().path == SceneMenu;

        /// <summary>Échap au clavier, B à la manette : l'action « Cancel » du module d'interface (réaffectable).</summary>
        private static bool RetourDemande()
        {
            if (EventSystem.current == null) return false;
            var module = EventSystem.current.currentInputModule as InputSystemUIInputModule;
            var action = module != null && module.cancel != null ? module.cancel.action : null;
            return action != null && action.WasPressedThisFrame();
        }

        private void Retour()
        {
            EcranMenu actuel = Actuel();
            if (actuel != null) actuel.Retour();
        }

        /// <summary>Montre un écran (un seul à la fois) et donne le focus à son premier élément.</summary>
        public void Afficher(Ecran cible)
        {
            if (cible == Ecran.Parametres && ecran != Ecran.Parametres && ecranParametres != null)
                ecranParametres.Depuis(ecran);
            ecran = cible;
            if (menuPrincipal != null) menuPrincipal.gameObject.SetActive(cible == Ecran.Accueil);
            if (ecranParametres != null) ecranParametres.gameObject.SetActive(cible == Ecran.Parametres);
            if (ecranSalons != null) ecranSalons.gameObject.SetActive(cible == Ecran.Salons);
            if (ecranSalle != null) ecranSalle.gameObject.SetActive(cible == Ecran.SalleAttente);

            EcranMenu actuel = Actuel();
            Selectionner(actuel != null ? actuel.PremierFocus : etang != null ? etang.Rejoindre : null);
        }

        private EcranMenu Actuel() => ecran switch
        {
            Ecran.Parametres => ecranParametres,
            Ecran.Salons => ecranSalons,
            Ecran.SalleAttente => ecranSalle,
            _ => null
        };

        /// <summary>Le volume général règle tout le son ; « Ambiance » règle l'ambiance du menu.</summary>
        private void AppliquerVolumes()
        {
            ReglagesJoueur r = ReglagesJoueur.Actuels;
            AudioListener.volume = r.volumeGeneral;
            if (ambiance != null) ambiance.volume = VolumeAmbiance * r.volumeAmbiance;
        }

        /// <summary>Le panneau de test du salon (P2-03, IMGUI) laisse la place aux vrais écrans.</summary>
        private static void CacherPanneauProvisoire()
        {
            Reseau.Salon salon = Reseau.Salon.Local;
            var panneau = salon != null ? salon.GetComponent<Reseau.PanneauSalon>() : null;
            if (panneau != null) panneau.enabled = false;
        }

        /// <summary>
        /// « Solo » : la partie complète, sans réseau. Le salon de la scène Menu survit aux changements de scène, et il
        /// coupe le joueur local du décor (CoupeJoueurLocal, utile seulement en réseau) : hors de tout salon, on le
        /// ferme d'abord, pour que la partie soit la même que lancée seule (sinon, ni joueur ni bots).
        /// </summary>
        private static void LancerSolo()
        {
            Reseau.Salon salon = Reseau.Salon.Local;
            if (salon != null && !salon.Connecte && !salon.Occupe) Destroy(salon.gameObject);
            SceneManager.LoadScene(SceneSolo);
        }

        /// <summary>Les écrans posés sur l'étang (ou sur l'eau du croquis), cachés jusqu'à leur tour.</summary>
        private void ConstruireEcrans(Transform parent)
        {
            var fabrique = new FabriqueMenu(etang);
            ecranParametres = EcranMenu.Creer<EcranParametres>("Écran - Paramètres", parent, this, fabrique);
            ecranSalons = EcranMenu.Creer<EcranSalons>("Écran - Salons", parent, this, fabrique);
            ecranSalle = EcranMenu.Creer<EcranSalleAttente>("Écran - Salle d'attente", parent, this, fabrique);
        }

        private void ConstruireInterface()
        {
            GameObject canevas = new GameObject("Interface du menu", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster));
            canevas.transform.SetParent(transform, false);
            canvas = canevas.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            CanvasScaler scaler = canevas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            AssurerEventSystem();

#if UNITY_EDITOR
            if (UnityEditor.SessionState.GetBool("Froggy.Menu.VersionCroquis", false)
                && Resources.Load<Texture2D>("MenuPeint/Eau_Croquis") != null)
            {
                ConstruireVersionCroquis(canevas.transform);
                return;
            }
#endif

            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            PoserEtang(canevas.transform);
        }

        private void PoserEtang(Transform parent)
        {
            GameObject prefab = Resources.Load<GameObject>(PrefabEtang);
            if (prefab == null || !prefab.TryGetComponent(out VueMenuEtang modele))
            {
                Debug.LogError("Préfabriqué du menu introuvable : " + PrefabEtang);
                return;
            }

            etang = Instantiate(modele, parent, false);
            menuPrincipal = etang.Centre;
            etang.Rejoindre.onClick.AddListener(() => Afficher(Ecran.Salons));
            etang.Parametres.onClick.AddListener(() => Afficher(Ecran.Parametres));
            etang.Solo.onClick.AddListener(LancerSolo);
            ConstruireEcrans(etang.transform);
        }

#if UNITY_EDITOR
        private void ConstruireVersionCroquis(Transform parent)
        {
            Sprite eauPeinte = ChargerSprite("Eau_Croquis");
            RectTransform fond = Zone("Fond peint", parent, new Vector2(0.5f, 0.5f), Vector2.one);
            Etirer(fond);
            Image couleurEau = fond.gameObject.AddComponent<Image>();
            couleurEau.color = EauCroquis;
            couleurEau.raycastTarget = false;

            Image textureEau = ImageSprite("Texture d'eau du croquis", fond, eauPeinte);
            Etirer(textureEau.rectTransform);
            textureEau.color = Color.white;

            PoserNenu("Groupe gauche haut", fond, "Nenu_Gauche_Haut", new Vector2(0.045f, 0.84f),
                new Vector2(330f, 217f), -3f);
            PoserNenu("Groupe gauche milieu", fond, "Nenu_Gauche_Milieu", new Vector2(0.045f, 0.50f),
                new Vector2(330f, 217f), 2f);
            PoserNenu("Groupe gauche bas", fond, "Nenu_Gauche_Bas", new Vector2(0.045f, 0.16f),
                new Vector2(330f, 229f), -2f);
            PoserNenu("Groupe droit haut", fond, "Nenu_Droite_Haut", new Vector2(0.955f, 0.84f),
                new Vector2(330f, 217f), 2f);
            PoserNenu("Groupe droit milieu", fond, "Nenu_Droite_Milieu", new Vector2(0.955f, 0.50f),
                new Vector2(330f, 217f), -2f);
            PoserNenu("Groupe droit bas", fond, "Nenu_Droite_Bas", new Vector2(0.955f, 0.16f),
                new Vector2(330f, 229f), 3f);

            menuPrincipal = Zone("Menu central", fond, new Vector2(0.5f, 0.5f), new Vector2(760f, 880f));
            Image logo = ImageSprite("Titre Froggy peint", menuPrincipal, ChargerSprite("Titre_Froggy"));
            Placer(logo.rectTransform, new Vector2(0.5f, 0.855f), Vector2.zero, new Vector2(720f, 232f));
            logo.preserveAspect = true;

            BoutonPeint("Rejoindre un salon", menuPrincipal, "Bouton_Rejoindre",
                new Vector2(0.5f, 0.625f), new Vector2(800f, 170f), () => Afficher(Ecran.Salons));
            BoutonPeint("Paramètres", menuPrincipal, "Bouton_Parametres",
                new Vector2(0.5f, 0.415f), new Vector2(740f, 170f), () => Afficher(Ecran.Parametres));
            BoutonPeint("Solo", menuPrincipal, "Bouton_Solo",
                new Vector2(0.5f, 0.205f), new Vector2(710f, 179f),
                LancerSolo);

            ConstruireEcrans(fond);
        }

        private void PoserNenu(string nom, Transform parent, string sprite, Vector2 ancre,
            Vector2 dimensions, float rotation)
        {
            Image feuille = ImageSprite(nom, parent, ChargerSprite(sprite));
            Placer(feuille.rectTransform, ancre, Vector2.zero, dimensions);
            feuille.rectTransform.localEulerAngles = new Vector3(0f, 0f, rotation);
            feuille.preserveAspect = true;
        }

        private void BoutonPeint(string nom, Transform parent, string sprite, Vector2 ancre,
            Vector2 dimensions, UnityEngine.Events.UnityAction action)
        {
            GameObject objet = new GameObject("Bouton peint - " + nom,
                typeof(RectTransform), typeof(Image), typeof(Button));
            objet.transform.SetParent(parent, false);
            RectTransform rect = objet.GetComponent<RectTransform>();
            Placer(rect, ancre, Vector2.zero, dimensions);
            Image image = objet.GetComponent<Image>();
            image.sprite = ChargerSprite(sprite);
            image.preserveAspect = true;
            image.raycastTarget = true;
            Button bouton = objet.GetComponent<Button>();
            bouton.targetGraphic = image;
            bouton.transition = Selectable.Transition.ColorTint;
            bouton.colors = new ColorBlock
            {
                normalColor = Color.white,
                highlightedColor = new Color(1f, 1f, 1f, 0.96f),
                pressedColor = new Color(0.84f, 0.92f, 0.74f, 1f),
                selectedColor = Color.white,
                disabledColor = new Color(1f, 1f, 1f, 0.45f),
                colorMultiplier = 1f,
                fadeDuration = 0.12f
            };
            bouton.onClick.AddListener(action);
        }
#endif

        private static void Selectionner(Selectable cible)
        {
            if (cible != null && EventSystem.current != null) cible.Select();
        }

        private void AssurerEventSystem()
        {
            EventSystem systeme = FindAnyObjectByType<EventSystem>();
            if (systemeSaisie != null)
            {
                systemeSaisie.SetActive(systeme == null || systeme.gameObject == systemeSaisie);
                return;
            }

            if (systeme != null) return;
            systemeSaisie = new GameObject("Saisie du menu", typeof(EventSystem), typeof(InputSystemUIInputModule));
            systemeSaisie.transform.SetParent(canvas.transform, false);
        }

#if UNITY_EDITOR
        private static Sprite ChargerSprite(string nom)
        {
            Texture2D texture = Resources.Load<Texture2D>("MenuPeint/" + nom);
            if (texture == null)
            {
                Debug.LogError("Sprite de menu introuvable : MenuPeint/" + nom);
                return null;
            }
            return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f), 100f);
        }

        private static RectTransform Zone(string nom, Transform parent, Vector2 ancre, Vector2 dimensions)
        {
            GameObject objet = new GameObject(nom, typeof(RectTransform));
            objet.transform.SetParent(parent, false);
            RectTransform rect = objet.GetComponent<RectTransform>();
            rect.anchorMin = ancre;
            rect.anchorMax = ancre;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = dimensions;
            return rect;
        }

        private static void Etirer(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void Placer(RectTransform rect, Vector2 ancre, Vector2 position, Vector2 dimensions)
        {
            rect.anchorMin = ancre;
            rect.anchorMax = ancre;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = dimensions;
        }

        private static Image ImageSprite(string nom, Transform parent, Sprite sprite)
        {
            GameObject objet = new GameObject(nom, typeof(RectTransform), typeof(Image));
            objet.transform.SetParent(parent, false);
            Image image = objet.GetComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            return image;
        }
#endif
    }
}
