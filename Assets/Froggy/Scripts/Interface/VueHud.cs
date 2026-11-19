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
        private RectTransform haut, bas, fin, barre, barrePleine, meteo;
        private Text minuteur, sousMinuteur, decompte, decompteSous, meteoTexte, message, finTitre, finRaison;
        private Image meteoIcone;
        private Bouton dash, torgnole, objet;
        private RectTransform finLignes, finBouton;
        private readonly List<Pastille> finRangs = new List<Pastille>();
        private readonly List<JoueurHud> classement = new List<JoueurHud>();
        private string decompteVu = "";
        private float decomptePop = -9f;
        private DonneesVueHud donnees;

        // ---------------------------------------------------------------- Pour les tests

        /// <summary>Le nombre de pastilles de joueur affichées.</summary>
        public int PastillesVisibles { get; private set; }
        /// <summary>Le joueur qui porte la couronne, ou Personne.</summary>
        public int Couronne { get; private set; } = ReglesPartie.Personne;
        public string TexteMinuteur => minuteur != null && minuteur.gameObject.activeInHierarchy ? minuteur.text : "";
        public bool FinVisible => fin != null && fin.gameObject.activeSelf;
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
        }

        private void ConstruireFin()
        {
            fin = Rect("Fin de partie", racine, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0f, 20f), new Vector2(640f, 520f));
            Image fond = FabriqueMenu.Arrondi("Fond", fin, true, Creme, .7f);
            FabriqueMenu.Etirer(fond.rectTransform);
            Image bord = FabriqueMenu.Arrondi("Bord", fin, false, Encre, .7f);
            FabriqueMenu.Etirer(bord.rectTransform);

            RectTransform ruban = Rect("Titre", fin, new Vector2(.5f, 1f), new Vector2(.5f, .5f), new Vector2(0f, 0f), new Vector2(520f, 86f));
            Penchee("Bord", ruban, Encre, 5f, 18f);
            Penchee("Fond", ruban, Ruban, 0f, 18f);
            finTitre = Ecrire("Texte", ruban, "", 54, Creme, TextAnchor.MiddleCenter, new Vector2(.5f, .5f), new Vector2(0f, 2f), new Vector2(700f, 86f), 5f);
            finRaison = Ecrire("Raison", fin, "", 30, Encre, TextAnchor.MiddleCenter, new Vector2(.5f, 1f), new Vector2(0f, -76f), new Vector2(600f, 40f), 0f);

            finLignes = Rect("Classement", fin, new Vector2(.5f, 1f), new Vector2(.5f, 1f), new Vector2(0f, -110f), new Vector2(520f, 280f));
            for (int i = 0; i < 4; i++)
            {
                var p = new Pastille();
                p.Rect = Rect("Rang " + (i + 1), finLignes, new Vector2(.5f, 1f), new Vector2(.5f, 1f), new Vector2(0f, -i * 66f), new Vector2(520f, 58f));
                Pilule("Fond", p.Rect, new Color(1f, 1f, 1f, .55f), true);
                p.Contour = Pilule("Toi", p.Rect, Encre, false);
                p.Jeton = Jeton(p.Rect, new Vector2(29f, -29f), 22f, out p.Numero, 26);
                p.Score = Ecrire("Nom", p.Rect, "", 30, Encre, TextAnchor.MiddleLeft, new Vector2(0f, .5f), new Vector2(64f, 1f), new Vector2(300f, 58f), 0f);
                p.Cookie = Icone("Cookie", p.Rect, new Vector2(1f, .5f), new Vector2(-90f, 0f), 34f);
                p.Couronne = null;
                var compte = Ecrire("Cookies", p.Rect, "", 36, Encre, TextAnchor.MiddleLeft, new Vector2(1f, .5f), new Vector2(-66f, 1f), new Vector2(60f, 58f), 0f);
                p.Toi = compte.rectTransform;   // rangé ici : le nombre de cookies du rang
                finRangs.Add(p);
            }

            finBouton = Rect("Rejouer", fin, new Vector2(.5f, 0f), new Vector2(.5f, .5f), new Vector2(0f, 56f), new Vector2(260f, 60f));
            Image fondBouton = FabriqueMenu.Arrondi("Fond", finBouton, true, Ruban, 1f);
            FabriqueMenu.Etirer(fondBouton.rectTransform);
            fondBouton.raycastTarget = true;
            Image bordBouton = FabriqueMenu.Arrondi("Bord", finBouton, false, Encre, 1f);
            FabriqueMenu.Etirer(bordBouton.rectTransform);
            Ecrire("Texte", finBouton, "Rejouer (R)", 30, Creme, TextAnchor.MiddleCenter, new Vector2(.5f, .5f), Vector2.zero, new Vector2(260f, 60f), 3f);
            // dès l'appui : au clic gauche, la caméra capture la souris et le relâchement n'arrive jamais au bouton
            finBouton.gameObject.AddComponent<AppuiHud>().Action = () => donnees?.Rejouer?.Invoke();
            fin.gameObject.SetActive(false);
        }

        // ---------------------------------------------------------------- Montrer

        /// <summary>Montre la partie telle que HudPartie l'a photographiée. Appelé à chaque image.</summary>
        public void Montrer(EtatHud e, DonneesVueHud d)
        {
            donnees = d;
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
                decompte.text = decompteSous.text = "";
                return;
            }
            MontrerPastilles(e);
            MontrerMinuteur(e);
            MontrerEtiquettes(e);
            MontrerBoutons(e, d);
            MontrerReperes(e, d);
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

        private void CacherReperes()
        {
            foreach (var r in reperes) r.Rect.gameObject.SetActive(false);
        }

        private void MontrerFin(EtatHud e)
        {
            var (titre, raison) = ContenuHud.Fin(e);
            Texte(finTitre, titre);
            Texte(finRaison, raison);
            classement.Clear();
            classement.AddRange(e.Joueurs);
            classement.Sort((a, b) =>
            {
                if ((a.Joueur == e.Gagnant) != (b.Joueur == e.Gagnant)) return a.Joueur == e.Gagnant ? -1 : 1;
                int c = b.Cookies.CompareTo(a.Cookies);
                return c != 0 ? c : a.Joueur.CompareTo(b.Joueur);
            });
            for (int i = 0; i < finRangs.Count; i++)
            {
                var p = finRangs[i];
                bool la = i < classement.Count;
                p.Rect.gameObject.SetActive(la);
                if (!la) continue;
                JoueurHud j = classement[i];
                p.Jeton.color = j.Couleur;
                Texte(p.Numero, j.Joueur.ToString());
                Texte(p.Score, ContenuHud.Nom(e, j.Joueur));
                p.Cookie.sprite = IconeHud(j.Joueur == e.Gagnant && raison == "avec le cookie sacré" ? "cookie_sacre" : "cookie");
                Texte(p.Toi.GetComponent<Text>(), j.Cookies.ToString());
                p.Contour.enabled = j.Joueur == e.Local;
            }
            float hauteur = 130f + classement.Count * 66f + (e.RejouerPermis ? 100f : 30f);
            fin.sizeDelta = new Vector2(640f, hauteur);
            finBouton.gameObject.SetActive(e.RejouerPermis);
            if (e.RejouerPermis) AssurerSaisie();
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
