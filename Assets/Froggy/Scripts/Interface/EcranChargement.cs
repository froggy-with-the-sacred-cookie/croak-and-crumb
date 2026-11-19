using System.Collections;
using Froggy.Regles;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Froggy.Interface
{
    /// <summary>
    /// L'écran de chargement, d'après la maquette de Julien (page Notion P6-03) : la peinture de la carte en plein écran,
    /// son nom, une astuce, une barre de progression anis et un lotus qui se balance sur une onde.
    ///
    /// Il couvre le passage du menu à la partie : en solo, il charge la scène lui-même (vraie progression) ; en ligne,
    /// MenuPrincipal le montre quand le salon fait arriver les scènes de la partie (la barre avance seule). Il s'efface
    /// quand la partie commence (compte à rebours), au plus tôt après 1,2 s pour ne pas clignoter, au plus tard après 20 s.
    /// </summary>
    public sealed class EcranChargement : MonoBehaviour
    {
        private const float DureeMin = 1.2f, Fondu = .35f, Limite = 20f;
        public const string CarteJardin = "Cartes/JardinJaponais";
        public const string NomJardin = "Jardin japonais";

        public static EcranChargement Actuel { get; private set; }

        /// <summary>Vrai tant que l'écran couvre le jeu (fondu compris).</summary>
        public static bool Visible => Actuel != null && Actuel.groupe != null && Actuel.groupe.alpha > 0f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Vider() => Actuel = null;

        /// <summary>Montre l'écran pour une carte (sans rien charger : le salon s'en charge en ligne).</summary>
        public static EcranChargement Montrer(string nomCarte = NomJardin, string image = CarteJardin)
        {
            if (Actuel == null)
            {
                var go = new GameObject("Écran de chargement");
                DontDestroyOnLoad(go);
                Actuel = go.AddComponent<EcranChargement>();
            }
            Actuel.Commencer(nomCarte, image);
            return Actuel;
        }

        /// <summary>Charge une scène (solo) derrière l'écran de chargement.</summary>
        public static void Charger(string scene, string nomCarte = NomJardin, string image = CarteJardin)
        {
            EcranChargement e = Montrer(nomCarte, image);
            e.StartCoroutine(e.ChargerScene(scene));
        }

        /// <summary>Le chargement est fini ailleurs (retour au menu…) : l'écran part tout de suite.</summary>
        public static void Cacher()
        {
            if (Actuel == null) return;
            Actuel.enCours = false;
            Actuel.groupe.alpha = 0f;
            Actuel.groupe.blocksRaycasts = false;
        }

        private CanvasGroup groupe;
        private Image peinture;
        private RectTransform plein, lotus, onde;
        private Text titre, astuce;
        private AsyncOperation operation;
        private float debut, progression;
        private bool enCours;

        /// <summary>Où en est la barre, de 0 à 1 (pour un test).</summary>
        public float Progression => progression;

        private void Awake() => Construire();

        private void Commencer(string nomCarte, string image)
        {
            titre.text = nomCarte;
            astuce.text = ContenuChargement.Astuce(Random.Range(0, 1000));
            Sprite s = Resources.Load<Sprite>(image);
            peinture.sprite = s;
            peinture.enabled = s != null;
            if (!enCours) { progression = 0f; debut = Time.unscaledTime; }
            operation = null;
            enCours = true;
            groupe.alpha = 1f;
            groupe.blocksRaycasts = true;
        }

        private IEnumerator ChargerScene(string scene)
        {
            yield return null;   // une image d'abord : l'écran est dessiné avant que le chargement ne prenne la main
            operation = SceneManager.LoadSceneAsync(scene);
        }

        private void Update()
        {
            if (!enCours)
            {
                if (groupe.alpha > 0f)
                {
                    groupe.alpha = Mathf.Max(0f, groupe.alpha - Time.unscaledDeltaTime / Fondu);
                    if (groupe.alpha <= 0f) groupe.blocksRaycasts = false;
                }
                return;
            }

            float t = Time.unscaledTime - debut;
            // la scène compte pour 80 % ; le reste, c'est le décor et la partie qui s'assemblent
            float cible = operation != null ? .8f * Mathf.Clamp01(operation.progress / .9f) : Mathf.Min(.85f, t / 6f);
            bool prete = PartiePrete();
            if (prete) cible = 1f;
            progression = Mathf.MoveTowards(progression, Mathf.Max(progression, cible), Time.unscaledDeltaTime * 1.5f);
            plein.anchorMax = new Vector2(Mathf.Max(.02f, progression), 1f);

            // le lotus se balance et l'onde s'élargit sous lui (skill rendu-vivant : rythmes décalés, rien de mécanique)
            lotus.localEulerAngles = new Vector3(0f, 0f, 6f * Mathf.Sin(t * 1.7f));
            lotus.anchoredPosition = new Vector2(-120f, 112f + 4f * Mathf.Sin(t * 2.3f + 1f));
            float o = Mathf.Repeat(t * .6f, 1f);
            onde.localScale = Vector3.one * (.7f + .6f * o);
            onde.GetComponent<Image>().color = new Color(1f, 1f, 1f, .8f * (1f - o));

            if ((prete && progression >= .999f && t >= DureeMin) || t > Limite) enCours = false;
        }

        // la partie a commencé : son déroulement a passé l'attente (compte à rebours, ou en jeu)
        private static bool PartiePrete()
        {
            if (MenuPrincipal.MenuVisible()) return false;
            var d = FindAnyObjectByType<DeroulementPartie>();
            return d != null && d.Regles != null && d.Regles.Phase != Phase.Attente;
        }

        // ---------------------------------------------------------------- Construction

        private void Construire()
        {
            var canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler),
                typeof(CanvasGroup));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 300;   // au-dessus du HUD (50), du menu (100) et de la pause (200)
            var echelle = canvasGo.GetComponent<CanvasScaler>();
            echelle.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            echelle.referenceResolution = new Vector2(1920f, 1080f);
            echelle.matchWidthOrHeight = .5f;
            groupe = canvasGo.GetComponent<CanvasGroup>();
            groupe.alpha = 0f;
            groupe.blocksRaycasts = false;
            Transform racine = canvasGo.transform;
            Font police = Resources.Load<Font>("MenuEtang/Nunito-ExtraBold");

            Image fond = NouvelleImage("Fond", racine, null, new Color32(13, 40, 62, 255));
            FabriqueMenu.Etirer(fond.rectTransform);
            peinture = NouvelleImage("Peinture de la carte", racine, null, Color.white);
            FabriqueMenu.Etirer(peinture.rectTransform);
            var ajuste = peinture.gameObject.AddComponent<AspectRatioFitter>();
            ajuste.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            ajuste.aspectRatio = 16f / 9f;

            Image voile = NouvelleImage("Dégradé", racine, FormesHud.Degrade, new Color(.03f, .1f, .16f, .85f));
            RectTransform rv = voile.rectTransform;
            rv.anchorMin = Vector2.zero; rv.anchorMax = new Vector2(1f, .42f);
            rv.offsetMin = rv.offsetMax = Vector2.zero;

            titre = Ecrire("Nom de la carte", racine, police, 64, TextAnchor.LowerLeft, new Vector2(0f, 0f),
                new Vector2(96f, 140f), new Vector2(1100f, 90f), 4f);
            astuce = Ecrire("Astuce", racine, police, 28, TextAnchor.LowerLeft, new Vector2(0f, 0f),
                new Vector2(100f, 92f), new Vector2(1000f, 44f), 2f);
            astuce.color = new Color(1f, .96f, .88f, .92f);

            Ecrire("Chargement", racine, police, 28, TextAnchor.LowerRight, new Vector2(1f, 0f),
                new Vector2(-250f, 92f), new Vector2(600f, 44f), 2f).text = ContenuChargement.Titre;

            // la barre : piste nuit cerclée de turquoise, remplissage anis (comme la maquette)
            Image piste = FabriqueMenu.Arrondi("Barre", racine, true, new Color32(13, 40, 62, 230), 4f);
            RectTransform rp = piste.rectTransform;
            rp.anchorMin = rp.anchorMax = new Vector2(1f, 0f);
            rp.pivot = new Vector2(1f, .5f);
            rp.anchoredPosition = new Vector2(-250f, 72f);
            rp.sizeDelta = new Vector2(560f, 16f);
            Image bord = FabriqueMenu.Arrondi("Bord", piste.transform, false, FabriqueMenu.Turquoise, 4f);
            FabriqueMenu.Etirer(bord.rectTransform);
            RectTransform zone = FabriqueMenu.Zone("Zone", piste.transform, Vector2.zero, Vector2.zero);
            FabriqueMenu.Etirer(zone);
            zone.offsetMin = new Vector2(3f, 3f);
            zone.offsetMax = new Vector2(-3f, -3f);
            Image remplissage = FabriqueMenu.Arrondi("Remplissage", zone, true, FabriqueMenu.Anis, 6f);
            plein = remplissage.rectTransform;
            plein.anchorMin = Vector2.zero;
            plein.anchorMax = new Vector2(.02f, 1f);
            plein.offsetMin = plein.offsetMax = Vector2.zero;

            Image imageOnde = NouvelleImage("Onde", racine, Resources.Load<Sprite>("MenuEtang/Ondes_1"), Color.white);
            onde = imageOnde.rectTransform;
            onde.anchorMin = onde.anchorMax = new Vector2(1f, 0f);
            onde.anchoredPosition = new Vector2(-120f, 84f);
            onde.sizeDelta = new Vector2(250f, 60f);
            Image imageLotus = NouvelleImage("Lotus", racine, Resources.Load<Sprite>("MenuEtang/Fleur"), Color.white);
            imageLotus.preserveAspect = true;
            lotus = imageLotus.rectTransform;
            lotus.anchorMin = lotus.anchorMax = new Vector2(1f, 0f);
            lotus.sizeDelta = new Vector2(170f, 112f);
        }

        private static Image NouvelleImage(string nom, Transform parent, Sprite sprite, Color couleur)
        {
            var go = new GameObject(nom, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var i = go.GetComponent<Image>();
            i.sprite = sprite;
            i.color = couleur;
            i.raycastTarget = false;
            return i;
        }

        private static Text Ecrire(string nom, Transform parent, Font police, int taille, TextAnchor alignement,
            Vector2 ancre, Vector2 position, Vector2 dimensions, float trait)
        {
            var go = new GameObject(nom, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>();
            t.font = police != null ? police : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = taille;
            t.color = FabriqueMenu.Ivoire;
            t.alignment = alignement;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            var r = t.rectTransform;
            r.anchorMin = r.anchorMax = ancre;
            r.pivot = new Vector2(alignement == TextAnchor.LowerRight ? 1f : 0f, 0f);
            r.anchoredPosition = position;
            r.sizeDelta = dimensions;
            if (trait > 0f)
            {
                var ombre = go.AddComponent<Shadow>();
                ombre.effectColor = new Color(0f, 0f, 0f, .5f);
                ombre.effectDistance = new Vector2(2f, -4f);
                var contour = go.AddComponent<Outline>();
                contour.effectColor = FabriqueMenu.Encre;
                contour.effectDistance = new Vector2(trait * .7f, -trait * .7f);
            }
            return t;
        }
    }
}
