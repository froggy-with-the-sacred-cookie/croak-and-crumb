using System.Collections.Generic;
using Froggy.Regles;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Froggy.Interface
{
    /// <summary>Une Froggy telle que le HUD la suit dans le monde (son score au-dessus de sa tête).</summary>
    public struct FroggyHud
    {
        public int Joueur;
        public int Cookies;
        public Color Couleur;
        /// <summary>Le haut de sa tête, dans le monde.</summary>
        public Vector3 Tete;
    }

    /// <summary>Ce que la vue lit en plus de l'état : les icônes des touches et de l'objet, les messages courts, les Froggy.</summary>
    public class DonneesVueHud
    {
        public Sprite ToucheDash, ToucheTorgnole, ToucheObjet, IconeObjet;
        public string MessageObjet = "", MessageTorgnole = "";
        public readonly List<FroggyHud> Froggy = new List<FroggyHud>();

        // le cookie sacré dans le monde (#152) : où il est, s'il est sur une Froggy, si c'est la Froggy de ce joueur
        public bool SacreVisible, SacrePorte, SacreChezMoi;
        public Vector3 SacreIci;
        public System.Action Rejouer;
    }

    /// <summary>
    /// Le HUD habillé (refonte du 19/11/2026, #63, jalon 2), sur un Canvas. Il ne lit que l'état photographié par
    /// HudPartie (EtatHud) et les textes de ContenuHud : il ne décide rien.
    ///
    /// Les choix viennent de la recherche (Docs/Recherches/hud-party-games-2026-11-19.md) et de la maquette validée :
    /// le HUD tient environ 5 % de l'écran, sur les bords ; un jeton de couleur ET un numéro par joueur, le score en gros
    /// (c'est l'information principale) ; des icônes plutôt que des phrases ; les annonces en petite étiquette penchée
    /// sous le minuteur ; le score des autres au-dessus de leur tête. Tailles écrites pour 1920 × 1080.
    /// </summary>
    [DisallowMultipleComponent]
    public class VueHud : MonoBehaviour
    {
        public const float Marge = 44f;
        public const string DossierIcones = "Hud/Icones/";
        private const string Police = "MenuEtang/Nunito-ExtraBold";

        // le projet mélange en espace linéaire : un fond sombre doit être plus opaque qu'on ne le croit
        public static readonly Color Encre = new Color32(31, 58, 42, 255);
        public static readonly Color Creme = new Color32(255, 246, 230, 255);
        public static readonly Color FondSombre = new Color(.07f, .15f, .1f, .8f);
        public static readonly Color Or = new Color32(245, 196, 66, 255);
        private static readonly Color Gris = new Color32(196, 202, 194, 255);
        private static readonly Color Ruban = new Color32(36, 92, 58, 255);

        private RectTransform racine;
        private Font police;
        private readonly Dictionary<string, Sprite> icones = new Dictionary<string, Sprite>();
        private readonly Dictionary<string, string> cles = new Dictionary<string, string>();

        private class Pastille
        {
            public RectTransform Rect, Toi;
            public Image Contour, Jeton, Cookie, Couronne;
            public Text Numero, Score;
            public int Vu = -1;
            public float Pop = -9f;
        }

        private class Etiquette
        {
            public RectTransform Rect;
            public EtiquettePenchee Fond, Bord;
            public Image Icone;
            public Text Texte;
            public string Vu = "";
            public float Depuis;
        }

        private class Bouton
        {
            public RectTransform Rect, Badge, ToucheFond;
            public Image Fond, Bord, Piste, Remplissage, Icone, Touche;
            public Text ToucheTexte, BadgeTexte;
            public bool Pret = true;
            public float Pop = -9f;
        }

        private class Repere
        {
            public RectTransform Rect;
            public Image Contour, Jeton;
            public Text Numero, Score;
        }

        private readonly List<Pastille> pastilles = new List<Pastille>();
        private readonly Etiquette[] etiquettes = new Etiquette[2];
        private readonly List<Repere> reperes = new List<Repere>();
        private RectTransform haut, bas, fin, barre, barrePleine, meteo, repere, repereAxe;
        private Text minuteur, sousMinuteur, decompte, decompteSous, meteoTexte, message, finTitre, finRaison;
        private Image meteoIcone;
        private Bouton dash, torgnole, objet;
        private RectTransform finPanneau, finBouton, finTrait;
        private readonly List<RectTransform> finTirets = new List<RectTransform>();
        private readonly List<Carte> podium = new List<Carte>();
        private Carte quatrieme, tonRang;
        private readonly List<JoueurHud> classement = new List<JoueurHud>();

        // une marche du podium, une ligne du classement ou la barre « Ton classement » (maquette de Julien)
        private class Carte
        {
            public RectTransform Rect;
            public Image Bord, Onglet, Jeton, Cookie;
            public Text Rang, Numero, Nom, Score;
        }
        private string decompteVu = "";
        private CanvasScaler echelle;
        private float tailleVue = -1f;

        /// <summary>La taille du HUD appliquée (réglage du joueur, 0,75 à 1,5).</summary>
        public float Taille => tailleVue;
        private float decomptePop = -9f;
        private DonneesVueHud donnees;

        // ---------------------------------------------------------------- Pour les tests

        /// <summary>Le nombre de pastilles de joueur affichées.</summary>
        public int PastillesVisibles { get; private set; }
        /// <summary>Le joueur qui porte la couronne, ou Personne.</summary>
        public int Couronne { get; private set; } = ReglesPartie.Personne;
        public string TexteMinuteur => minuteur != null && minuteur.gameObject.activeInHierarchy ? minuteur.text : "";
        public bool FinVisible => fin != null && fin.gameObject.activeSelf;
        /// <summary>Le nombre de marches du podium affichées (1 à 3).</summary>
        public int PodiumVisible { get; private set; }
        /// <summary>Le rang du joueur local au classement de fin (« 2e »), ou vide s'il n'y est pas.</summary>
        public string RangLocal { get; private set; } = "";
        /// <summary>La barre « Ton classement » est affichée (seulement si le joueur local n'est ni sur le podium ni 4e).</summary>
        public bool BarreTonRang => tonRang != null && tonRang.Rect.gameObject.activeSelf;
        public bool BarreVisible => barre != null && barre.gameObject.activeInHierarchy;
        public bool MeteoVisible => meteo != null && meteo.gameObject.activeInHierarchy;
        /// <summary>Les étiquettes d'annonce affichées, de haut en bas.</summary>
        public IEnumerable<string> Annonces
        {
            get
            {
                foreach (var e in etiquettes)
                    if (e != null && e.Rect.gameObject.activeSelf) yield return e.Texte.text;
            }
        }
        public bool DashPret => dash != null && dash.Pret;
        public bool TorgnolePrete => torgnole != null && torgnole.Pret;
        public bool ObjetTenu => objet != null && objet.Icone.enabled;
        /// <summary>Le repère du cookie sacré est affiché (#152).</summary>
        public bool RepereSacreVisible => repere != null && repere.gameObject.activeSelf;
        /// <summary>Le repère est au bord de l'écran (le cookie sacré est hors champ).</summary>
        public bool RepereSacreAuBord { get; private set; }

        // ---------------------------------------------------------------- Construction

        /// <summary>Crée le canvas du HUD habillé sous un parent (l'objet « HUD de partie »).</summary>
        public static VueHud Creer(Transform parent)
        {
            var go = new GameObject("HUD habillé", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(parent, false);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;   // sous le menu (100) et la pause (200)
            var echelle = go.GetComponent<CanvasScaler>();
            echelle.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            echelle.referenceResolution = new Vector2(1920f, 1080f);
            echelle.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            echelle.matchWidthOrHeight = 1f;
            var vue = go.AddComponent<VueHud>();
            vue.echelle = echelle;
            vue.Construire();
            return vue;
        }

        private void Construire()
        {
            racine = (RectTransform)transform;
            police = Resources.Load<Font>(Police);
            if (police == null) police = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            haut = Zone("Haut", racine);
            bas = Zone("Bas", racine);
            ConstruirePastilles();
            ConstruireMinuteur();
            ConstruireEtiquettes();
            ConstruireBoutons();
            ConstruireReperes();
            ConstruireFin();
        }

        private void ConstruirePastilles()
        {
            for (int i = 0; i < 4; i++)
            {
                var p = new Pastille();
                p.Rect = Rect("Joueur " + (i + 1), haut, new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(Marge + i * 156f, -Marge), new Vector2(146f, 58f));
                Pilule("Fond", p.Rect, FondSombre, true);
                p.Contour = Pilule("Toi", p.Rect, Color.white, false);
                p.Jeton = Jeton(p.Rect, new Vector2(29f, -29f), 22f, out p.Numero, 26);
                p.Cookie = Icone("Cookie", p.Rect, new Vector2(0f, .5f), new Vector2(72f, 0f), 30f);
                p.Score = Ecrire("Score", p.Rect, "0", 40, Creme, TextAnchor.MiddleLeft, new Vector2(0f, .5f), new Vector2(92f, 1f), new Vector2(60f, 58f), 4f);
                p.Couronne = Icone("Couronne", p.Rect, new Vector2(0f, 1f), new Vector2(25f, 6f), 44f);
                p.Couronne.rectTransform.localEulerAngles = new Vector3(0f, 0f, 14f);
                p.Couronne.sprite = IconeHud("couronne");
                p.Toi = Rect("Etiquette TOI", p.Rect, new Vector2(0f, 0f), new Vector2(.5f, .5f), new Vector2(29f, -4f), new Vector2(50f, 24f));
                Pilule("Fond", p.Toi, Color.white, true);
                Ecrire("TOI", p.Toi, "TOI", 18, Encre, TextAnchor.MiddleCenter, new Vector2(.5f, .5f), Vector2.zero, new Vector2(50f, 24f), 0f);
                pastilles.Add(p);
            }
        }

        private void ConstruireMinuteur()
        {
            minuteur = Ecrire("Minuteur", haut, "", 64, Creme, TextAnchor.UpperCenter, new Vector2(.5f, 1f), new Vector2(0f, -Marge + 8f), new Vector2(260f, 80f), 5f);
            barre = Rect("Barre de temps", haut, new Vector2(.5f, 1f), new Vector2(.5f, .5f), new Vector2(0f, -Marge - 78f), new Vector2(140f, 7f));
            Pilule("Fond", barre, FondSombre, true);
            barrePleine = (RectTransform)Pilule("Plein", barre, Creme, true).transform;
            barrePleine.anchorMin = barrePleine.anchorMax = new Vector2(0f, .5f);
            barrePleine.pivot = new Vector2(0f, .5f);
            sousMinuteur = Ecrire("Sous le minuteur", haut, "", 24, Or, TextAnchor.UpperCenter, new Vector2(.5f, 1f), new Vector2(0f, -Marge - 88f), new Vector2(600f, 32f), 3f);

            meteo = Rect("Météo", haut, new Vector2(.5f, 1f), new Vector2(0f, .5f), new Vector2(110f, -Marge - 30f), new Vector2(108f, 44f));
            Pilule("Fond", meteo, FondSombre, true);
            meteoIcone = Icone("Icône", meteo, new Vector2(0f, .5f), new Vector2(26f, 0f), 38f);
            meteoTexte = Ecrire("Secondes", meteo, "", 26, Creme, TextAnchor.MiddleLeft, new Vector2(0f, .5f), new Vector2(52f, 1f), new Vector2(60f, 44f), 3f);

            decompte = Ecrire("Décompte", racine, "", 180, Creme, TextAnchor.MiddleCenter, new Vector2(.5f, .5f), new Vector2(0f, 120f), new Vector2(400f, 220f), 7f);
            decompteSous = Ecrire("Prêts", racine, "", 34, Creme, TextAnchor.MiddleCenter, new Vector2(.5f, .5f), new Vector2(0f, 10f), new Vector2(600f, 50f), 4f);
        }

        private void ConstruireEtiquettes()
        {
            for (int i = 0; i < etiquettes.Length; i++)
            {
                var e = new Etiquette();
                e.Rect = Rect("Annonce " + (i + 1), haut, new Vector2(.5f, 1f), new Vector2(.5f, .5f),
                    new Vector2(0f, -Marge - 130f - i * 62f), new Vector2(500f, 52f));
                e.Bord = Penchee("Bord", e.Rect, Encre, 4f);
                e.Fond = Penchee("Fond", e.Rect, Ruban, 0f);
                e.Icone = Icone("Icône", e.Rect, new Vector2(0f, .5f), new Vector2(34f, 0f), 54f);
                e.Texte = Ecrire("Texte", e.Rect, "", 28, Creme, TextAnchor.MiddleLeft, new Vector2(0f, .5f), new Vector2(70f, 1f), new Vector2(900f, 52f), 3f);
                e.Rect.gameObject.SetActive(false);
                etiquettes[i] = e;
            }
        }

        private void ConstruireBoutons()
        {
            // l'objet à droite, en poche carrée ; torgnole et dash en ronds (recherche : Brawl Stars, anneau autour)
            objet = CreerBouton("Objet", new Vector2(-(Marge + 44f), 122f), 88f, true);
            torgnole = CreerBouton("Torgnole", new Vector2(-(Marge + 88f + 16f + 38f), 122f), 76f, false);
            dash = CreerBouton("Dash", new Vector2(-(Marge + 88f + 16f + 76f + 18f + 38f), 122f), 76f, false);
            torgnole.Icone.sprite = IconeHud("torgnole");
            dash.Icone.sprite = IconeHud("dash");
            message = Ecrire("Message", bas, "", 26, Creme, TextAnchor.MiddleRight, new Vector2(1f, 0f), new Vector2(-Marge, 222f), new Vector2(800f, 40f), 3f);
            message.rectTransform.pivot = new Vector2(1f, .5f);
        }

        private Bouton CreerBouton(string nom, Vector2 centre, float taille, bool carre)
        {
            var b = new Bouton();
            b.Rect = Rect(nom, bas, new Vector2(1f, 0f), new Vector2(.5f, .5f), centre, new Vector2(taille, taille));
            if (carre)
            {
                b.Fond = FabriqueMenu.Arrondi("Fond", b.Rect, true, Creme, 1.1f);
                FabriqueMenu.Etirer(b.Fond.rectTransform);
                b.Bord = FabriqueMenu.Arrondi("Bord", b.Rect, false, Encre, 1.1f);
                FabriqueMenu.Etirer(b.Bord.rectTransform);
            }
            else
            {
                b.Bord = Disque("Bord", b.Rect, Encre, taille);
                b.Fond = Disque("Fond", b.Rect, Creme, taille - 8f);
            }
            b.Piste = Anneau("Piste", b.Rect, FondSombre, taille + 12f);
            b.Remplissage = Anneau("Recharge", b.Rect, Creme, taille + 12f);
            b.Remplissage.type = UnityEngine.UI.Image.Type.Filled;
            b.Remplissage.fillMethod = UnityEngine.UI.Image.FillMethod.Radial360;
            b.Remplissage.fillOrigin = (int)UnityEngine.UI.Image.Origin360.Top;
            b.Remplissage.fillClockwise = true;
            b.Icone = Icone("Icône", b.Rect, new Vector2(.5f, .5f), Vector2.zero, taille * .76f);

            b.ToucheFond = Rect("Touche", b.Rect, new Vector2(.5f, 0f), new Vector2(.5f, .5f), new Vector2(0f, -6f), new Vector2(40f, 28f));
            Image capuchon = FabriqueMenu.Arrondi("Fond", b.ToucheFond, true, new Color32(248, 244, 236, 255), 2.5f);
            FabriqueMenu.Etirer(capuchon.rectTransform);
            Image bordTouche = FabriqueMenu.Arrondi("Bord", b.ToucheFond, false, Encre, 2.5f);
            FabriqueMenu.Etirer(bordTouche.rectTransform);
            b.ToucheTexte = Ecrire("Nom", b.ToucheFond, "", 16, Encre, TextAnchor.MiddleCenter, new Vector2(.5f, .5f), Vector2.zero, new Vector2(80f, 28f), 0f);
            b.Touche = Icone("Icône de touche", b.Rect, new Vector2(.5f, 0f), new Vector2(0f, -6f), 34f);

            b.Badge = Rect("Secondes", b.Rect, new Vector2(.5f, 1f), new Vector2(.5f, .5f), new Vector2(0f, 14f), new Vector2(54f, 28f));
            Pilule("Fond", b.Badge, new Color(.07f, .15f, .1f, .94f), true);
            b.BadgeTexte = Ecrire("Texte", b.Badge, "", 20, Creme, TextAnchor.MiddleCenter, new Vector2(.5f, .5f), Vector2.zero, new Vector2(120f, 28f), 0f);
            return b;
        }

        private void ConstruireReperes()
        {
            for (int i = 0; i < 4; i++)
            {
                var r = new Repere();
                r.Rect = Rect("Au-dessus de la tête " + (i + 1), racine, new Vector2(0f, 0f), new Vector2(.5f, 0f), Vector2.zero, new Vector2(104f, 44f));
                Pilule("Fond", r.Rect, FondSombre, true);
                r.Contour = Pilule("Contour", r.Rect, Color.white, false);
                r.Jeton = Jeton(r.Rect, new Vector2(22f, -22f), 13f, out r.Numero, 17);
                Image cookie = Icone("Cookie", r.Rect, new Vector2(0f, .5f), new Vector2(54f, 0f), 26f);
                cookie.sprite = IconeHud("cookie");
                r.Score = Ecrire("Score", r.Rect, "", 28, Creme, TextAnchor.MiddleLeft, new Vector2(0f, .5f), new Vector2(72f, 1f), new Vector2(40f, 44f), 3f);
                r.Rect.gameObject.SetActive(false);
                reperes.Add(r);
            }

            // le repère du cookie sacré (#152) : disque d'or (la couleur du cookie, pas le turquoise du kappa, trop proche
            // du joueur 3) et une pointe qui montre la direction ; au-dessus des étiquettes de score
            repere = Rect("Repère du cookie sacré", racine, Vector2.zero, new Vector2(.5f, .5f), Vector2.zero, new Vector2(60f, 60f));
            repereAxe = Rect("Axe", repere, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
            Pointe("Bord", repereAxe, Encre, new Vector2(30f, 0f), new Vector2(26f, 32f));
            Pointe("Pointe", repereAxe, Or, new Vector2(32f, 0f), new Vector2(18f, 22f));
            Disque("Bord", repere, Encre, 62f);
            Disque("Or", repere, Or, 56f);
            Disque("Fond", repere, new Color(.07f, .15f, .1f, 1f), 46f);
            Image icone = Icone("Cookie sacré", repere, new Vector2(.5f, .5f), Vector2.zero, 40f);
            icone.sprite = IconeHud("cookie_sacre");
            repere.gameObject.SetActive(false);
        }

        private static void Pointe(string nom, RectTransform parent, Color couleur, Vector2 position, Vector2 taille)
        {
            var go = new GameObject(nom, typeof(RectTransform), typeof(CanvasRenderer), typeof(TriangleHud));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<TriangleHud>();
            t.color = couleur;
            t.raycastTarget = false;
            var r = t.rectTransform;
            r.anchorMin = r.anchorMax = new Vector2(.5f, .5f);
            r.pivot = new Vector2(0f, .5f);
            r.anchoredPosition = position;
            r.sizeDelta = taille;
        }

        private void ConstruireFin()
        {
            // l'écran de victoire voilé, et par-dessus le classement de Julien (page Notion P6-03) : podium, 4e, ton rang
            fin = Rect("Fin de partie", racine, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
            FabriqueMenu.Etirer(fin);
            var voile = new GameObject("Voile", typeof(RectTransform), typeof(Image));
            voile.transform.SetParent(fin, false);
            FabriqueMenu.Etirer((RectTransform)voile.transform);
            Image iv = voile.GetComponent<Image>();
            iv.color = new Color(.02f, .07f, .11f, .55f);
            iv.raycastTarget = false;

            finPanneau = FabriqueMenu.Panneau("Classement", fin, new Vector2(0f, 6f), new Vector2(900f, 700f),
                FabriqueMenu.Nuit, FabriqueMenu.Turquoise, .7f);
            Deco("MenuEtang/Nenuphar_Jade", new Vector2(-470f, -320f), 210f, 16f);
            Deco("MenuEtang/Nenuphar_Anis", new Vector2(478f, 300f), 170f, -24f);
            Deco("MenuEtang/Fleur", new Vector2(-452f, 304f), 150f, 10f);
            Deco("MenuEtang/Fleur", new Vector2(462f, -316f), 128f, -8f);

            finTitre = Ecrire("Titre", finPanneau, "", 66, Creme, TextAnchor.MiddleCenter, new Vector2(.5f, 1f), new Vector2(0f, -72f), new Vector2(800f, 90f), 5f);
            finTitre.rectTransform.pivot = new Vector2(.5f, .5f);
            finTrait = (RectTransform)FabriqueMenu.Arrondi("Trait", finPanneau, true, FabriqueMenu.Anis, 3f).transform;
            Placer(finTrait, new Vector2(.5f, 1f), new Vector2(0f, -124f), new Vector2(360f, 10f));
            for (int i = 0; i < 6; i++)
                finTirets.Add((RectTransform)FabriqueMenu.Arrondi("Tiret", finPanneau, true, FabriqueMenu.Anis, 4f).transform);
            finRaison = Ecrire("Raison", finPanneau, "", 30, FabriqueMenu.Turquoise, TextAnchor.MiddleCenter, new Vector2(.5f, 1f), new Vector2(0f, -160f), new Vector2(800f, 40f), 0f);
            finRaison.rectTransform.pivot = new Vector2(.5f, .5f);

            podium.Add(CreerCarte("1er", new Vector2(240f, 190f), Or));
            podium.Add(CreerCarte("2e", new Vector2(220f, 160f), FabriqueMenu.Turquoise));
            podium.Add(CreerCarte("3e", new Vector2(220f, 160f), FabriqueMenu.Turquoise));
            quatrieme = CreerLigne("4e", FabriqueMenu.Turquoise);
            tonRang = CreerLigne("Ton classement", FabriqueMenu.Anis);
            // « Ton classement » d'abord, puis le rang, le jeton et le nom décalés d'autant
            Ecrire("Libellé", tonRang.Rect, "Ton classement", 24, Creme, TextAnchor.MiddleLeft, new Vector2(0f, .5f), new Vector2(24f, 1f), new Vector2(200f, 58f), 0f);
            ((RectTransform)tonRang.Rang.transform.parent).anchoredPosition = new Vector2(250f, 0f);
            ((RectTransform)tonRang.Jeton.transform.parent).anchoredPosition = new Vector2(318f, -29f);
            tonRang.Nom.rectTransform.anchoredPosition = new Vector2(346f, 1f);

            finBouton = Rect("Rejouer", finPanneau, new Vector2(.5f, 1f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(380f, 92f));
            var feuille = finBouton.gameObject.AddComponent<Image>();
            feuille.sprite = Resources.Load<Sprite>("MenuEtang/Feuille_Claire");
            feuille.preserveAspect = true;
            if (feuille.sprite == null) feuille.color = FabriqueMenu.Anis;
            Ecrire("Texte", finBouton, "Rejouer (R)", 34, FabriqueMenu.Encre, TextAnchor.MiddleCenter, new Vector2(.5f, .5f), new Vector2(-10f, 0f), new Vector2(380f, 92f), 0f);
            // dès l'appui : au clic gauche, la caméra capture la souris et le relâchement n'arrive jamais au bouton
            finBouton.gameObject.AddComponent<AppuiHud>().Action = () => donnees?.Rejouer?.Invoke();
            fin.gameObject.SetActive(false);
        }

        private void Deco(string sprite, Vector2 position, float taille, float angle)
        {
            Image i = Icone("Décor", finPanneau, new Vector2(.5f, .5f), position, taille);
            i.sprite = Resources.Load<Sprite>(sprite);
            i.enabled = i.sprite != null;
            i.rectTransform.localEulerAngles = new Vector3(0f, 0f, angle);
        }

        private static void Placer(RectTransform r, Vector2 ancre, Vector2 position, Vector2 taille)
        {
            r.anchorMin = r.anchorMax = ancre;
            r.pivot = new Vector2(.5f, .5f);
            r.anchoredPosition = position;
            r.sizeDelta = taille;
        }

        // une marche du podium : onglet du rang en haut, jeton et nom, puis le score en gros
        private Carte CreerCarte(string rang, Vector2 taille, Color bord)
        {
            var c = new Carte();
            c.Rect = Rect("Marche " + rang, finPanneau, new Vector2(.5f, 1f), new Vector2(.5f, .5f), Vector2.zero, taille);
            Image fond = FabriqueMenu.Arrondi("Fond", c.Rect, true, FabriqueMenu.NuitProfonde, 1.2f);
            FabriqueMenu.Etirer(fond.rectTransform);
            c.Bord = FabriqueMenu.Arrondi("Bord", c.Rect, false, bord, 1.2f);
            FabriqueMenu.Etirer(c.Bord.rectTransform);
            RectTransform onglet = Rect("Onglet", c.Rect, new Vector2(.5f, 1f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(96f, 40f));
            c.Onglet = Pilule("Fond", onglet, bord, true);
            c.Rang = Ecrire("Rang", onglet, rang, 26, FabriqueMenu.Encre, TextAnchor.MiddleCenter, new Vector2(.5f, .5f), new Vector2(0f, 1f), new Vector2(96f, 40f), 0f);
            c.Jeton = Jeton(c.Rect, new Vector2(taille.x * .5f, -46f), 18f, out c.Numero, 22);
            c.Jeton.transform.parent.GetComponent<RectTransform>().anchorMin = c.Jeton.transform.parent.GetComponent<RectTransform>().anchorMax = new Vector2(0f, 1f);
            c.Nom = Ecrire("Nom", c.Rect, "", 26, Creme, TextAnchor.MiddleCenter, new Vector2(.5f, 1f), new Vector2(0f, -84f), new Vector2(taille.x, 34f), 0f);
            c.Cookie = Icone("Cookie", c.Rect, new Vector2(.5f, 0f), new Vector2(-28f, 30f), 38f);
            c.Score = Ecrire("Score", c.Rect, "", 38, Creme, TextAnchor.MiddleLeft, new Vector2(.5f, 0f), new Vector2(-4f, 31f), new Vector2(100f, 46f), 3f);
            return c;
        }

        // une ligne : pastille du rang, jeton, nom, et le score à droite
        private Carte CreerLigne(string nom, Color bord)
        {
            var c = new Carte();
            c.Rect = Rect(nom, finPanneau, new Vector2(.5f, 1f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(680f, 58f));
            Pilule("Fond", c.Rect, FabriqueMenu.NuitProfonde, true);
            c.Bord = Pilule("Bord", c.Rect, bord, false);
            RectTransform onglet = Rect("Rang", c.Rect, new Vector2(0f, .5f), new Vector2(.5f, .5f), new Vector2(52f, 0f), new Vector2(76f, 38f));
            c.Onglet = Pilule("Fond", onglet, bord, true);
            c.Rang = Ecrire("Texte", onglet, "", 24, FabriqueMenu.Encre, TextAnchor.MiddleCenter, new Vector2(.5f, .5f), new Vector2(0f, 1f), new Vector2(76f, 38f), 0f);
            c.Jeton = Jeton(c.Rect, new Vector2(124f, -29f), 20f, out c.Numero, 24);
            c.Nom = Ecrire("Nom", c.Rect, "", 28, Creme, TextAnchor.MiddleLeft, new Vector2(0f, .5f), new Vector2(156f, 1f), new Vector2(330f, 58f), 0f);
            c.Cookie = Icone("Cookie", c.Rect, new Vector2(1f, .5f), new Vector2(-104f, 0f), 34f);
            c.Score = Ecrire("Score", c.Rect, "", 34, Creme, TextAnchor.MiddleLeft, new Vector2(1f, .5f), new Vector2(-80f, 1f), new Vector2(70f, 58f), 0f);
            return c;
        }

        // ---------------------------------------------------------------- Montrer

        /// <summary>Montre la partie telle que HudPartie l'a photographiée. Appelé à chaque image.</summary>
        public void Montrer(EtatHud e, DonneesVueHud d)
        {
            donnees = d;
            AppliquerTaille();
            bool rien = !e.PartieConnue && e.Joueurs.Count == 0 && !e.ObjetsConnus && !e.TorgnoleConnue
                        && string.IsNullOrEmpty(e.Meteo) && !e.DashConnu;
            bool visible = !rien && !(Application.isPlaying && MenuPrincipal.MenuVisible());
            racine.gameObject.SetActive(visible);
            if (!visible) return;

            bool finie = e.PartieConnue && e.Phase == Phase.Finie;
            haut.gameObject.SetActive(!finie);
            bas.gameObject.SetActive(!finie);
            fin.gameObject.SetActive(finie);
            if (finie)
            {
                MontrerFin(e);
                CacherReperes();
                repere.gameObject.SetActive(false);
                decompte.text = decompteSous.text = "";
                return;
            }
            MontrerPastilles(e);
            MontrerMinuteur(e);
            MontrerEtiquettes(e);
            MontrerBoutons(e, d);
            MontrerReperes(e, d);
            MontrerRepereSacre(e, d);
        }

        private void MontrerPastilles(EtatHud e)
        {
            bool enJeu = e.Phase == Phase.EnJeu || e.Phase == Phase.Prolongation;
            int meneur = enJeu ? ContenuHud.EnTete(e) : ReglesPartie.Personne;
            int porteur = e.Sacre == EtatCookieSacre.Porte ? e.PorteurSacre : ReglesPartie.Personne;
            Couronne = ReglesPartie.Personne;
            PastillesVisibles = Mathf.Min(e.Joueurs.Count, pastilles.Count);
            for (int i = 0; i < pastilles.Count; i++)
            {
                var p = pastilles[i];
                bool la = i < PastillesVisibles;
                p.Rect.gameObject.SetActive(la);
                if (!la) continue;
                JoueurHud j = e.Joueurs[i];
                p.Jeton.color = j.Couleur;
                Texte(p.Numero, j.Joueur.ToString());
                if (j.Cookies > p.Vu && p.Vu >= 0) p.Pop = Time.unscaledTime;
                p.Vu = j.Cookies;
                Texte(p.Score, j.Cookies.ToString());
                p.Score.rectTransform.localScale = Vector3.one * Rebond(p.Pop, .35f, .3f);
                p.Cookie.sprite = IconeHud(j.Joueur == porteur ? "cookie_sacre" : "cookie");
                bool local = j.Joueur == e.Local;
                p.Contour.enabled = local;
                p.Toi.gameObject.SetActive(local);
                p.Couronne.enabled = j.Joueur == meneur;
                if (p.Couronne.enabled) Couronne = j.Joueur;
            }
        }

        private void MontrerMinuteur(EtatHud e)
        {
            bool decompteEnCours = e.PartieConnue && e.Phase == Phase.Decompte;
            bool enJeu = e.PartieConnue && (e.Phase == Phase.EnJeu || e.Phase == Phase.Prolongation);

            string chiffre = decompteEnCours ? ContenuHud.Minuteur(e) : "";
            if (chiffre != decompteVu) { decompteVu = chiffre; decomptePop = Time.unscaledTime; }
            Texte(decompte, chiffre);
            Texte(decompteSous, decompteEnCours ? ContenuHud.SousMinuteur(e) : "");
            decompte.rectTransform.localScale = Vector3.one * Rebond(decomptePop, .3f, .35f);

            minuteur.gameObject.SetActive(enJeu);
            sousMinuteur.gameObject.SetActive(enJeu);
            bool presse = ContenuHud.MinuteurPresse(e);
            if (enJeu)
            {
                Texte(minuteur, ContenuHud.Minuteur(e));
                minuteur.color = presse ? Or : Creme;
                float pouls = presse ? 1f + .05f * Mathf.Max(0f, Mathf.Sin(Time.unscaledTime * Mathf.PI * 2f)) : 1f;
                minuteur.rectTransform.localScale = Vector3.one * pouls;
                Texte(sousMinuteur, ContenuHud.SousMinuteur(e));
            }
            bool barreLa = enJeu && e.Phase == Phase.EnJeu && e.Duree > 0f && string.IsNullOrEmpty(sousMinuteur.text);
            barre.gameObject.SetActive(barreLa);
            if (barreLa) barrePleine.sizeDelta = new Vector2(140f * Mathf.Clamp01(e.Reste / e.Duree), 7f);

            bool meteoLa = !string.IsNullOrEmpty(e.Meteo);
            meteo.gameObject.SetActive(meteoLa);
            if (meteoLa)
            {
                Sprite s = IconeHud(Cle(e.Meteo));
                meteoIcone.sprite = s;
                meteoIcone.enabled = s != null;
                Texte(meteoTexte, Mathf.CeilToInt(Mathf.Max(0f, e.MeteoReste)) + " s");
                // annoncée : la pastille bat doucement, l'étiquette dit ce qui vient
                float pouls = e.MeteoAnnoncee ? 1f + .06f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * Mathf.PI * 1.5f)) : 1f;
                meteo.localScale = Vector3.one * pouls;
            }
        }

        private void MontrerEtiquettes(EtatHud e)
        {
            int n = 0;
            // sous « Dernière minute » ou « Prolongation » quand ils sont là, sinon juste sous la barre de temps
            bool sous = sousMinuteur.gameObject.activeInHierarchy && !string.IsNullOrEmpty(sousMinuteur.text);
            float y = sous ? -Marge - 168f : -Marge - 130f;
            string sacre = ContenuHud.CookieSacre(e);
            if (sacre.Length > 0) { Etiqueter(etiquettes[n], sacre, "cookie_sacre", y - n * 62f); n++; }
            if (e.MeteoAnnoncee && !string.IsNullOrEmpty(e.Meteo))
            {
                Etiqueter(etiquettes[n], ContenuHud.Meteo(e), Cle(e.Meteo), y - n * 62f);
                n++;
            }
            for (int i = n; i < etiquettes.Length; i++)
            {
                etiquettes[i].Rect.gameObject.SetActive(false);
                etiquettes[i].Vu = "";
            }
        }

        private void Etiqueter(Etiquette et, string texte, string icone, float y)
        {
            if (!et.Rect.gameObject.activeSelf || et.Vu != texte)
            {
                // une annonce neuve glisse depuis la gauche ; le temps de garde qui défile ne la relance pas
                bool neuve = !et.Rect.gameObject.activeSelf || Debut(et.Vu) != Debut(texte);
                if (neuve) et.Depuis = Time.unscaledTime;
                et.Vu = texte;
                et.Texte.text = texte;
                et.Icone.sprite = IconeHud(icone);
                et.Icone.enabled = et.Icone.sprite != null;
                float largeur = et.Texte.preferredWidth + 110f;
                et.Rect.sizeDelta = new Vector2(largeur, 52f);
                et.Rect.gameObject.SetActive(true);
            }
            float t = Mathf.Clamp01((Time.unscaledTime - et.Depuis) / .2f);
            float lisse = 1f - (1f - t) * (1f - t);
            et.Rect.anchoredPosition = new Vector2(-40f * (1f - lisse), y);
        }

        private static string Debut(string s) => string.IsNullOrEmpty(s) ? "" : s.Substring(0, Mathf.Min(12, s.Length));

        private void MontrerBoutons(EtatHud e, DonneesVueHud d)
        {
            dash.Rect.gameObject.SetActive(e.DashConnu);
            if (e.DashConnu)
            {
                dash.Icone.sprite = IconeHud(e.Assomme ? "assomme" : "dash");
                Recharger(dash, e.Assomme ? 0f : e.RechargeDash, e.DureeRechargeDash, e.Assomme ? "Assommé !" : null, e.ToucheDash, d.ToucheDash);
            }
            torgnole.Rect.gameObject.SetActive(e.TorgnoleConnue);
            if (e.TorgnoleConnue)
                Recharger(torgnole, e.RechargeTorgnole, e.DureeRechargeTorgnole, null, e.ToucheTorgnole, d.ToucheTorgnole);

            objet.Rect.gameObject.SetActive(e.ObjetsConnus);
            if (e.ObjetsConnus)
            {
                bool tenu = !string.IsNullOrEmpty(e.Objet);
                Sprite icone = tenu ? (d.IconeObjet != null ? d.IconeObjet : IconeHud(Cle(e.Objet))) : null;
                objet.Icone.sprite = icone;
                objet.Icone.enabled = icone != null;
                // vide : la poche reste, en creux (fond pâle, bord clair)
                objet.Fond.color = tenu || e.RechargeObjet > 0f ? Creme : new Color(1f, 1f, 1f, .22f);
                objet.Bord.color = tenu || e.RechargeObjet > 0f ? Encre : new Color(1f, 1f, 1f, .7f);
                Recharger(objet, e.RechargeObjet, e.DureeRechargeObjet, null, tenu ? e.ToucheObjet : "", tenu ? d.ToucheObjet : null);
                if (!tenu && e.RechargeObjet <= 0f) objet.Fond.color = new Color(1f, 1f, 1f, .22f);
            }

            string m = !string.IsNullOrEmpty(d.MessageTorgnole) ? d.MessageTorgnole : d.MessageObjet ?? "";
            Texte(message, m);
        }

        private void Recharger(Bouton b, float reste, float duree, string etat, string nomTouche, Sprite iconeTouche)
        {
            bool pret = reste <= 0f && etat == null;
            if (pret && !b.Pret) b.Pop = Time.unscaledTime;   // redevenu prêt : il saute un peu
            b.Pret = pret;
            b.Rect.localScale = Vector3.one * Rebond(b.Pop, .18f, .25f);

            b.Fond.color = pret ? Creme : Gris;
            b.Icone.color = pret ? Color.white : new Color(.55f, .58f, .56f, .9f);
            bool charge = reste > 0f && duree > 0f;
            b.Piste.enabled = charge;
            b.Remplissage.enabled = charge;
            if (charge) b.Remplissage.fillAmount = ContenuHud.Jauge(reste, duree);

            b.Badge.gameObject.SetActive(!pret);
            if (!pret)
            {
                string t = etat ?? ContenuHud.Secondes(reste).Replace(" s", "");
                Texte(b.BadgeTexte, t);
                b.Badge.sizeDelta = new Vector2(Mathf.Max(54f, b.BadgeTexte.preferredWidth + 22f), 28f);
                b.BadgeTexte.color = etat != null ? Or : Creme;
            }

            // la touche : l'icône de l'Input System (réaffectée, elle suit), sinon son nom sur un petit capuchon
            b.Touche.sprite = iconeTouche;
            b.Touche.enabled = iconeTouche != null;
            if (iconeTouche != null)
            {
                float cote = 34f * IconesTouches.Echelle(iconeTouche);
                b.Touche.rectTransform.sizeDelta = new Vector2(cote, cote);
            }
            bool nom = iconeTouche == null && !string.IsNullOrEmpty(nomTouche);
            b.ToucheFond.gameObject.SetActive(nom);
            if (nom)
            {
                Texte(b.ToucheTexte, nomTouche);
                b.ToucheFond.sizeDelta = new Vector2(Mathf.Max(30f, b.ToucheTexte.preferredWidth + 16f), 28f);
            }
        }

        private void MontrerReperes(EtatHud e, DonneesVueHud d)
        {
            Camera cam = Camera.main;
            bool montrer = cam != null && (e.Phase == Phase.EnJeu || e.Phase == Phase.Prolongation || !e.PartieConnue);
            int n = 0;
            if (montrer)
            {
                Vector2 taille = racine.rect.size;
                foreach (var f in d.Froggy)
                {
                    if (f.Joueur == e.Local || n >= reperes.Count) continue;
                    Vector3 ecran = cam.WorldToScreenPoint(f.Tete);
                    if (ecran.z <= .5f || ecran.z > 30f) continue;
                    var vp = cam.ScreenToViewportPoint(ecran);
                    if (vp.x < .03f || vp.x > .97f || vp.y < .05f || vp.y > .82f) continue;   // pas sous le HUD du haut
                    var r = reperes[n++];
                    r.Rect.gameObject.SetActive(true);
                    r.Rect.anchoredPosition = SansChevauchement(new Vector2(vp.x * taille.x, vp.y * taille.y + 10f), n - 1);
                    r.Contour.color = f.Couleur;
                    r.Jeton.color = f.Couleur;
                    Texte(r.Numero, f.Joueur.ToString());
                    Texte(r.Score, f.Cookies.ToString());
                }
            }
            for (int i = n; i < reperes.Count; i++) reperes[i].Rect.gameObject.SetActive(false);
        }

        // deux Froggy proches : la seconde étiquette monte au-dessus de la première au lieu de la recouvrir
        private Vector2 SansChevauchement(Vector2 position, int rang)
        {
            const float largeur = 104f, hauteur = 44f, ecart = 6f;
            for (int essai = 0; essai < 4; essai++)
            {
                bool libre = true;
                for (int i = 0; i < rang; i++)
                {
                    Vector2 autre = reperes[i].Rect.anchoredPosition;
                    if (Mathf.Abs(autre.x - position.x) < largeur && Mathf.Abs(autre.y - position.y) < hauteur + ecart)
                    {
                        position.y = autre.y + hauteur + ecart;
                        libre = false;
                    }
                }
                if (libre) break;
            }
            return position;
        }

        // le cookie sacré (#152) : au-dessus de lui s'il est à l'écran, sinon au bord, pointé vers lui ; rien quand c'est
        // ce joueur qui le porte (son jeton et l'annonce le disent déjà)
        private void MontrerRepereSacre(EtatHud e, DonneesVueHud d)
        {
            bool enJeu = !e.PartieConnue || e.Phase == Phase.EnJeu || e.Phase == Phase.Prolongation;
            Camera cam = Camera.main;
            bool montrer = enJeu && d.SacreVisible && !d.SacreChezMoi && cam != null;
            repere.gameObject.SetActive(montrer);
            if (!montrer) { RepereSacreAuBord = false; return; }

            Vector3 vp = cam.WorldToViewportPoint(d.SacreIci);
            Vector2 taille = racine.rect.size;
            float balance = 5f * Mathf.Sin(Time.unscaledTime * 3f);
            RepereSacreAuBord = BordEcran.HorsChamp(vp, BordEcran.ZoneLibre);
            if (!RepereSacreAuBord)
            {
                // au-dessus de l'étiquette de score quand il est sur une Froggy
                float dessus = d.SacrePorte ? 96f : 44f;
                repere.anchoredPosition = new Vector2(vp.x * taille.x, vp.y * taille.y + dessus + balance);
                repereAxe.localEulerAngles = new Vector3(0f, 0f, -90f);
            }
            else
            {
                Vector2 p = BordEcran.AuBord(vp, BordEcran.ZoneLibre, taille.x / Mathf.Max(1f, taille.y), out float angle);
                repere.anchoredPosition = new Vector2(p.x * taille.x, p.y * taille.y);
                repereAxe.localEulerAngles = new Vector3(0f, 0f, angle);
                repere.localScale = Vector3.one * (1f + .06f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 3f)));
                return;
            }
            repere.localScale = Vector3.one;
        }

        private void CacherReperes()
        {
            foreach (var r in reperes) r.Rect.gameObject.SetActive(false);
        }

        private void MontrerFin(EtatHud e)
        {
            var (titre, raison) = ContenuHud.Fin(e);
            Texte(finTitre, titre);
            finTitre.color = e.Gagnant == e.Local ? Or : Creme;
            Texte(finRaison, raison);
            // les traits verts en éventail de part et d'autre du titre (comme le logo et les titres de Julien)
            float demi = finTitre.preferredWidth * .5f + 36f;
            for (int k = 0; k < finTirets.Count; k++)
            {
                int cote = k < 3 ? -1 : 1, i = k % 3 - 1;
                Placer(finTirets[k], new Vector2(.5f, 1f), new Vector2(cote * (demi + (i == 0 ? 6f : 0f)), -72f + i * 20f), new Vector2(30f, 9f));
                finTirets[k].localEulerAngles = new Vector3(0f, 0f, -cote * i * 28f);
            }

            ContenuHud.Classement(e, classement);
            bool parLeSacre = raison == "avec le cookie sacré";
            // le podium : le 1er au centre, plus haut ; le 2e à gauche, le 3e à droite
            Vector2[] places = { new Vector2(0f, -300f), new Vector2(-262f, -316f), new Vector2(262f, -316f) };
            PodiumVisible = Mathf.Min(3, classement.Count);
            for (int i = 0; i < podium.Count; i++)
            {
                bool la = i < classement.Count;
                podium[i].Rect.gameObject.SetActive(la);
                if (!la) continue;
                podium[i].Rect.anchoredPosition = places[i];
                Remplir(podium[i], classement[i], i + 1, e, parLeSacre);
            }

            float y = -446f;
            quatrieme.Rect.gameObject.SetActive(classement.Count >= 4);
            if (classement.Count >= 4)
            {
                quatrieme.Rect.anchoredPosition = new Vector2(0f, y);
                Remplir(quatrieme, classement[3], 4, e, parLeSacre);
                y -= 72f;
            }

            // « Ton classement » : pour un joueur qu'on ne voit ni sur le podium ni en 4e (la maquette de Julien prévoit des
            // classements longs) ; de 2 à 4 joueurs, son nom en or suffit
            int rangLocal = classement.FindIndex(j => j.Joueur == e.Local);
            RangLocal = rangLocal >= 0 ? ContenuHud.Rang(rangLocal + 1) : "";
            bool barre = rangLocal >= 4;
            tonRang.Rect.gameObject.SetActive(barre);
            if (barre)
            {
                tonRang.Rect.anchoredPosition = new Vector2(0f, y);
                Remplir(tonRang, classement[rangLocal], rangLocal + 1, e, parLeSacre);
                tonRang.Nom.color = Creme;
                y -= 72f;
            }

            finBouton.gameObject.SetActive(e.RejouerPermis);
            if (e.RejouerPermis)
            {
                finBouton.anchoredPosition = new Vector2(0f, y - 30f);
                y -= 92f;
                AssurerSaisie();
            }
            finPanneau.sizeDelta = new Vector2(900f, -y + 34f);
            finPanneau.anchoredPosition = new Vector2(0f, 6f);
        }

        private void Remplir(Carte c, JoueurHud j, int rang, EtatHud e, bool parLeSacre)
        {
            Texte(c.Rang, ContenuHud.Rang(rang));
            c.Jeton.color = j.Couleur;
            Texte(c.Numero, j.Joueur.ToString());
            Texte(c.Nom, ContenuHud.Nom(e, j.Joueur));
            c.Nom.color = j.Joueur == e.Local ? Or : Creme;
            Texte(c.Score, j.Cookies.ToString());
            c.Cookie.sprite = IconeHud(rang == 1 && parLeSacre ? "cookie_sacre" : "cookie");
        }

        // ---------------------------------------------------------------- Outils

        /// <summary>Une icône peinte du HUD (Resources/Hud/Icones/&lt;clé&gt;), ou rien si elle manque.</summary>
        public Sprite IconeHud(string cle)
        {
            if (string.IsNullOrEmpty(cle)) return null;
            if (icones.TryGetValue(cle, out Sprite s)) return s;
            s = Resources.Load<Sprite>(DossierIcones + cle);
            icones[cle] = s;
            return s;
        }

        // la taille du HUD (Paramètres ▸ Taille du HUD) : on réduit la résolution de référence, tout grandit d'autant
        private void AppliquerTaille()
        {
            float t = ReglagesJoueur.Actuels.tailleHud;
            if (Mathf.Approximately(t, tailleVue) || echelle == null) return;
            tailleVue = t;
            echelle.referenceResolution = new Vector2(1920f, 1080f) / Mathf.Max(.1f, t);
        }

        // la clé d'icône d'un nom, gardée : on ne la recalcule pas à chaque image
        private string Cle(string nom)
        {
            if (string.IsNullOrEmpty(nom)) return "";
            if (!cles.TryGetValue(nom, out string cle)) cles[nom] = cle = ContenuHud.CleIcone(nom);
            return cle;
        }

        // un petit rebond à l'échelle : de 1 + ampleur à 1 en « duree » secondes, depuis « depuis »
        private static float Rebond(float depuis, float ampleur, float duree)
        {
            float t = (Time.unscaledTime - depuis) / duree;
            if (t < 0f || t >= 1f) return 1f;
            return 1f + ampleur * (1f - t) * (1f - t);
        }

        private static void Texte(Text t, string s)
        {
            if (t.text != s) t.text = s;
        }

        private void AssurerSaisie()
        {
            if (EventSystem.current != null && EventSystem.current.isActiveAndEnabled) return;
            var go = new GameObject("Saisie du HUD", typeof(EventSystem), typeof(InputSystemUIInputModule));
            go.transform.SetParent(transform, false);
        }

        private static RectTransform Zone(string nom, Transform parent)
        {
            var go = new GameObject(nom, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var r = (RectTransform)go.transform;
            FabriqueMenu.Etirer(r);
            return r;
        }

        private static RectTransform Rect(string nom, Transform parent, Vector2 ancre, Vector2 pivot, Vector2 position, Vector2 taille)
        {
            var go = new GameObject(nom, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var r = (RectTransform)go.transform;
            r.anchorMin = r.anchorMax = ancre;
            r.pivot = pivot;
            r.anchoredPosition = position;
            r.sizeDelta = taille;
            return r;
        }

        // une pilule (bouts ronds) qui épouse son parent
        private static Image Pilule(string nom, RectTransform parent, Color couleur, bool plein)
        {
            float rayon = Mathf.Max(4f, parent.sizeDelta.y * .5f);
            Image i = FabriqueMenu.Arrondi(nom, parent, plein, couleur, 24f / rayon);
            FabriqueMenu.Etirer(i.rectTransform);
            return i;
        }

        private static Image Disque(string nom, RectTransform parent, Color couleur, float diametre)
        {
            var go = new GameObject(nom, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var i = go.GetComponent<Image>();
            i.sprite = FormesHud.Disque;
            i.color = couleur;
            i.raycastTarget = false;
            var r = i.rectTransform;
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(.5f, .5f);
            r.sizeDelta = new Vector2(diametre, diametre);
            return i;
        }

        private static Image Anneau(string nom, RectTransform parent, Color couleur, float diametre)
        {
            Image i = Disque(nom, parent, couleur, diametre);
            i.sprite = FormesHud.Anneau(9);
            return i;
        }

        // le jeton d'un joueur : un disque à sa couleur cerclé d'encre, et son numéro (la couleur ne suffit jamais seule)
        private Image Jeton(RectTransform parent, Vector2 centre, float rayon, out Text numero, int taille)
        {
            var r = Rect("Jeton", parent, new Vector2(0f, 1f), new Vector2(.5f, .5f), centre, new Vector2(rayon * 2f, rayon * 2f));
            Disque("Bord", r, Encre, rayon * 2f);
            Image jeton = Disque("Couleur", r, Color.white, rayon * 2f - 7f);
            numero = Ecrire("Numéro", r, "", taille, Encre, TextAnchor.MiddleCenter, new Vector2(.5f, .5f), new Vector2(0f, 1f), new Vector2(rayon * 2f, rayon * 2f), 0f);
            return jeton;
        }

        private Image Icone(string nom, RectTransform parent, Vector2 ancre, Vector2 position, float taille)
        {
            var go = new GameObject(nom, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var i = go.GetComponent<Image>();
            i.preserveAspect = true;
            i.raycastTarget = false;
            var r = i.rectTransform;
            r.anchorMin = r.anchorMax = ancre;
            r.pivot = new Vector2(.5f, .5f);
            r.anchoredPosition = position;
            r.sizeDelta = new Vector2(taille, taille);
            return i;
        }

        private static EtiquettePenchee Penchee(string nom, RectTransform parent, Color couleur, float debord, float pente = 14f)
        {
            var go = new GameObject(nom, typeof(RectTransform), typeof(CanvasRenderer), typeof(EtiquettePenchee));
            go.transform.SetParent(parent, false);
            var e = go.GetComponent<EtiquettePenchee>();
            e.color = couleur;
            e.raycastTarget = false;
            var r = e.rectTransform;
            FabriqueMenu.Etirer(r);
            r.offsetMin = new Vector2(-debord, -debord);
            r.offsetMax = new Vector2(debord, debord);
            e.Pente = pente + debord * .3f;
            return e;
        }

        // un texte de la police du menu (Nunito ExtraBold), cerné d'encre et ombré quand il est posé sur le jeu
        private Text Ecrire(string nom, RectTransform parent, string contenu, int taille, Color couleur, TextAnchor alignement,
            Vector2 ancre, Vector2 position, Vector2 dimensions, float trait)
        {
            var go = new GameObject(nom, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>();
            t.font = police;
            t.fontSize = taille;
            t.color = couleur;
            t.alignment = alignement;
            t.text = contenu;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            var r = t.rectTransform;
            r.anchorMin = r.anchorMax = ancre;
            r.pivot = new Vector2(alignement == TextAnchor.MiddleLeft || alignement == TextAnchor.UpperLeft ? 0f
                : alignement == TextAnchor.MiddleRight || alignement == TextAnchor.UpperRight ? 1f : .5f,
                alignement == TextAnchor.UpperCenter ? 1f : .5f);
            r.anchoredPosition = position;
            r.sizeDelta = dimensions;
            if (trait > 0f)
            {
                var ombre = go.AddComponent<Shadow>();
                ombre.effectColor = new Color(0f, 0f, 0f, .45f);
                ombre.effectDistance = new Vector2(2f, -4f);
                var contour = go.AddComponent<Outline>();
                contour.effectColor = Encre;
                contour.effectDistance = new Vector2(trait * .7f, -trait * .7f);
            }
            return t;
        }
    }
}
