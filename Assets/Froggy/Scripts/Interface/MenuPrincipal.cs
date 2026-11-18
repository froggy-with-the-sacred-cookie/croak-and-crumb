using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Froggy.Interface
{
    /// <summary>Point d’entrée du menu, de ses réglages et de ses transitions de scène.</summary>
    public sealed class MenuPrincipal : MonoBehaviour
    {
        private const string SceneMenu = "Assets/Scenes/SampleScene.unity";
        private const string SceneSalon = "Reseau_JardinJaponais";
        private const string SceneSolo = "Vitrine_JardinJaponais";
        private const string PrefabEtang = "MenuEtang/Menu_Principal_Etang";
#if UNITY_EDITOR
        private static readonly Color EauCroquis = new Color32(43, 80, 121, 255);
#endif
        private static readonly Color Ivoire = new Color32(250, 239, 207, 255);
        private static bool instanceCreee;

        private AudioSource ambiance;
        private Canvas canvas;
        private GameObject systemeSaisie;
        private RectTransform menuPrincipal;
        private RectTransform panneauReglages;
        private VueMenuEtang etang;
        private Slider curseurVolume;
        private Button retourReglages;
        private Text valeurVolume;
        private float volumeAmbiance = 0.12f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reinitialiser() => instanceCreee = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreerAuDemarrage()
        {
            Scene scene = SceneManager.GetActiveScene();
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
            ambiance.volume = volumeAmbiance;
            ambiance.spatialBlend = 0f;
            if (ambiance.clip != null) ambiance.Play();
            ConstruireInterface();
            SceneManager.sceneLoaded += QuandSceneChargee;
        }

        private void OnDestroy() => SceneManager.sceneLoaded -= QuandSceneChargee;

        private void Start() => Selectionner(etang != null ? etang.Rejoindre : null);

        private void QuandSceneChargee(Scene scene, LoadSceneMode mode)
        {
            bool menuActif = scene.path == SceneMenu;
            if (menuActif) AssurerEventSystem();
            canvas.gameObject.SetActive(menuActif);
            if (menuActif && etang != null)
                Selectionner(panneauReglages.gameObject.activeSelf ? FocusReglages : etang.Rejoindre);
            if (menuActif && ambiance.clip != null && !ambiance.isPlaying) ambiance.Play();
            else if (!menuActif) ambiance.Stop();
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
            etang.Rejoindre.onClick.AddListener(() => SceneManager.LoadScene(SceneSalon));
            etang.Parametres.onClick.AddListener(AfficherReglages);
            etang.Solo.onClick.AddListener(() => SceneManager.LoadScene(SceneSolo));
            ConstruirePanneauReglages(etang.transform);
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
                new Vector2(0.5f, 0.625f), new Vector2(800f, 170f),
                () => SceneManager.LoadScene(SceneSalon));
            BoutonPeint("Paramètres", menuPrincipal, "Bouton_Parametres",
                new Vector2(0.5f, 0.415f), new Vector2(740f, 170f), AfficherReglages);
            BoutonPeint("Solo", menuPrincipal, "Bouton_Solo",
                new Vector2(0.5f, 0.205f), new Vector2(710f, 179f),
                () => SceneManager.LoadScene(SceneSolo));

            ConstruirePanneauReglages(fond);
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

        private void ConstruirePanneauReglages(Transform parent)
        {
            panneauReglages = Zone("Panneau des paramètres", parent, new Vector2(0.5f, 0.5f),
                new Vector2(650f, 350f));
            Image fond = panneauReglages.gameObject.AddComponent<Image>();
            fond.color = new Color32(21, 58, 83, 246);
            Texte("Paramètres", panneauReglages, 34, Ivoire, FontStyle.Bold,
                new Vector2(0.5f, 0.80f), new Vector2(560f, 60f));
            if (ambiance.clip != null) ConstruireReglageVolume();
            else Texte("Ambiance indisponible", panneauReglages, 23, Ivoire, FontStyle.Normal,
                new Vector2(0.50f, 0.48f), new Vector2(560f, 46f));

            retourReglages = BoutonRetour(panneauReglages);
            panneauReglages.gameObject.SetActive(false);
        }

        private void ConstruireReglageVolume()
        {
            Texte("Ambiance", panneauReglages, 23, Ivoire, FontStyle.Normal,
                new Vector2(0.30f, 0.51f), new Vector2(170f, 46f));
            valeurVolume = Texte("12 %", panneauReglages, 21, Ivoire, FontStyle.Bold,
                new Vector2(0.81f, 0.51f), new Vector2(90f, 46f));
            curseurVolume = CreerSlider(panneauReglages);
            curseurVolume.value = volumeAmbiance / 0.25f;
            curseurVolume.onValueChanged.AddListener(v =>
            {
                volumeAmbiance = Mathf.Lerp(0f, 0.25f, v);
                ambiance.volume = volumeAmbiance;
                valeurVolume.text = Mathf.RoundToInt(v * 25f) + " %";
            });
        }

        private Button BoutonRetour(Transform parent)
        {
            GameObject objet = new GameObject("Bouton - Retour",
                typeof(RectTransform), typeof(Image), typeof(Button));
            objet.transform.SetParent(parent, false);
            Placer(objet.GetComponent<RectTransform>(), new Vector2(0.5f, 0.17f),
                Vector2.zero, new Vector2(260f, 60f));
            Image image = objet.GetComponent<Image>();
            Button bouton = objet.GetComponent<Button>();
            bouton.targetGraphic = image;
            if (etang != null)
            {
                image.sprite = etang.Parametres.image.sprite;
                bouton.transition = Selectable.Transition.SpriteSwap;
                bouton.spriteState = etang.Parametres.spriteState;
            }
            else image.color = new Color32(67, 151, 137, 255);
            bouton.onClick.AddListener(CacherReglages);
            Texte("Retour", objet.transform, 22, Ivoire, FontStyle.Bold,
                new Vector2(0.5f, 0.5f), new Vector2(240f, 52f));
            return bouton;
        }

        private static Slider CreerSlider(Transform parent)
        {
            RectTransform piste = Zone("Curseur du volume", parent,
                new Vector2(0.56f, 0.37f), new Vector2(360f, 22f));
            Image pisteImage = piste.gameObject.AddComponent<Image>();
            pisteImage.color = new Color32(78, 111, 120, 255);
            GameObject remplissage = new GameObject("Remplissage", typeof(RectTransform), typeof(Image));
            remplissage.transform.SetParent(piste, false);
            Image remplissageImage = remplissage.GetComponent<Image>();
            remplissageImage.color = new Color32(173, 207, 108, 255);
            RectTransform remplissageRect = remplissageImage.rectTransform;
            remplissageRect.anchorMin = new Vector2(0f, 0.1f);
            remplissageRect.anchorMax = new Vector2(1f, 0.9f);
            remplissageRect.offsetMin = new Vector2(2f, 0f);
            remplissageRect.offsetMax = new Vector2(-2f, 0f);
            GameObject poignee = new GameObject("Poignée", typeof(RectTransform), typeof(Image));
            poignee.transform.SetParent(piste, false);
            Image poigneeImage = poignee.GetComponent<Image>();
            poigneeImage.color = Ivoire;
            Placer(poigneeImage.rectTransform, Vector2.zero, Vector2.zero, new Vector2(28f, 28f));
            Slider slider = piste.gameObject.AddComponent<Slider>();
            slider.fillRect = remplissageRect;
            slider.handleRect = poigneeImage.rectTransform;
            slider.targetGraphic = poigneeImage;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            return slider;
        }

        private void AfficherReglages()
        {
            menuPrincipal.gameObject.SetActive(false);
            panneauReglages.gameObject.SetActive(true);
            Selectionner(FocusReglages);
        }

        private Selectable FocusReglages => curseurVolume != null ? curseurVolume : retourReglages;

        private void CacherReglages()
        {
            panneauReglages.gameObject.SetActive(false);
            menuPrincipal.gameObject.SetActive(true);
            Selectionner(etang != null ? etang.Parametres : null);
        }

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
#endif

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

#if UNITY_EDITOR
        private static void Etirer(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
#endif

        private static void Placer(RectTransform rect, Vector2 ancre, Vector2 position, Vector2 dimensions)
        {
            rect.anchorMin = ancre;
            rect.anchorMax = ancre;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = dimensions;
        }

#if UNITY_EDITOR
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

        private Text Texte(string contenu, Transform parent, int taille, Color couleur,
            FontStyle style, Vector2 ancre, Vector2 dimensions)
        {
            GameObject objet = new GameObject("Texte - " + contenu, typeof(RectTransform), typeof(Text));
            objet.transform.SetParent(parent, false);
            Text texte = objet.GetComponent<Text>();
            texte.text = contenu;
            texte.font = etang != null ? etang.Police : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            texte.fontSize = taille;
            texte.fontStyle = etang != null ? FontStyle.Normal : style;
            texte.color = couleur;
            texte.alignment = TextAnchor.MiddleCenter;
            texte.horizontalOverflow = HorizontalWrapMode.Wrap;
            texte.verticalOverflow = VerticalWrapMode.Overflow;
            texte.raycastTarget = false;
            Placer(texte.rectTransform, ancre, Vector2.zero, dimensions);
            return texte;
        }
    }
}
