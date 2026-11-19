using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Froggy.Interface
{
    /// <summary>
    /// Les pièces des écrans du menu (P6-03), dans le style de l'étang de Julien : boutons en feuille de nénuphar (les
    /// sprites et leurs états viennent du préfabriqué, VueMenuEtang), panneaux bleu nuit bordés de turquoise, texte
    /// ivoire en Nunito. Les coins arrondis sont dessinés ici, en blanc, et teintés par la couleur de l'image.
    /// Positions en unités de l'écran de référence (1920 × 1080), depuis le centre du parent.
    /// </summary>
    public sealed class FabriqueMenu
    {
        public static readonly Color Ivoire = new Color32(250, 239, 207, 255);
        public static readonly Color Nuit = new Color32(21, 58, 83, 246);
        public static readonly Color NuitProfonde = new Color32(13, 40, 62, 240);
        public static readonly Color Turquoise = new Color32(110, 205, 200, 255);
        public static readonly Color Anis = new Color32(184, 232, 84, 255);
        public static readonly Color Encre = new Color32(25, 53, 40, 255);
        public static readonly Color Corail = new Color32(255, 150, 128, 255);
        public static readonly Color Brume = new Color32(250, 239, 207, 120);

        private static Sprite arrondiPlein, arrondiContour;

        private readonly Button modeleClair, modeleSarcelle;

        public Font Police { get; }

        public FabriqueMenu(VueMenuEtang etang)
        {
            modeleClair = etang != null ? etang.Rejoindre : null;
            modeleSarcelle = etang != null ? etang.Parametres : null;
            Police = etang != null && etang.Police != null ? etang.Police
                : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reinitialiser()
        {
            arrondiPlein = null;
            arrondiContour = null;
        }

        private const string CleHabille = "Froggy.Menus.habille";

        /// <summary>
        /// Le style des maquettes de Julien (page Notion P6-03, 19/11/2026) : double bordure turquoise des panneaux, titre
        /// encadré de traits verts, poignée des curseurs en nénuphar. Vrai par défaut ; faux : le style du 18/11, pour
        /// comparer (garde-fou 7). Gardé dans l'éditeur, toujours vrai dans un build. Lu à la construction des écrans.
        /// </summary>
        public static bool Habille
        {
            get
            {
#if UNITY_EDITOR
                return UnityEditor.EditorPrefs.GetBool(CleHabille, true);
#else
                return true;
#endif
            }
            set
            {
#if UNITY_EDITOR
                UnityEditor.EditorPrefs.SetBool(CleHabille, value);
#endif
            }
        }

        // ---------------------------------------------------------------- Placement

        /// <summary>Un objet d'interface vide, centré sur son parent.</summary>
        public static RectTransform Zone(string nom, Transform parent, Vector2 position, Vector2 taille)
        {
            var objet = new GameObject(nom, typeof(RectTransform));
            objet.transform.SetParent(parent, false);
            var rect = (RectTransform)objet.transform;
            Placer(rect, position, taille);
            return rect;
        }

        public static void Placer(RectTransform rect, Vector2 position, Vector2 taille)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = taille;
        }

        /// <summary>Étire un objet sur tout son parent.</summary>
        public static void Etirer(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        // ---------------------------------------------------------------- Formes

        /// <summary>Une image aux coins arrondis, pleine ou en contour. Arrondi : 24 unités à 1, 12 à 2.</summary>
        public static Image Arrondi(string nom, Transform parent, bool plein, Color couleur, float finesse = 1f)
        {
            var objet = new GameObject(nom, typeof(RectTransform), typeof(Image));
            objet.transform.SetParent(parent, false);
            var image = objet.GetComponent<Image>();
            image.sprite = plein ? SpritePlein : SpriteContour;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = finesse;
            image.color = couleur;
            image.raycastTarget = false;
            return image;
        }

        /// <summary>Un panneau : fond plein et bord, aux coins arrondis.</summary>
        public static RectTransform Panneau(string nom, Transform parent, Vector2 position, Vector2 taille,
            Color fond, Color bord, float finesse = 1f)
        {
            Image image = Arrondi(nom, parent, true, fond, finesse);
            Placer(image.rectTransform, position, taille);
            if (Habille)
            {
                // la maquette de Julien : un halo turquoise autour, un filet clair à l'intérieur
                Image halo = Arrondi("Halo", image.transform, false, new Color(bord.r, bord.g, bord.b, .3f), finesse * .8f);
                Etirer(halo.rectTransform);
                halo.rectTransform.offsetMin = new Vector2(-8f, -8f);
                halo.rectTransform.offsetMax = new Vector2(8f, 8f);
                Image filet = Arrondi("Filet", image.transform, false, new Color(1f, 1f, 1f, .28f), finesse * 1.6f);
                Etirer(filet.rectTransform);
                filet.rectTransform.offsetMin = new Vector2(9f, 9f);
                filet.rectTransform.offsetMax = new Vector2(-9f, -9f);
            }
            Image contour = Arrondi("Bord", image.transform, false, bord, finesse);
            Etirer(contour.rectTransform);
            return image.rectTransform;
        }

        private static Sprite SpritePlein => arrondiPlein != null ? arrondiPlein : arrondiPlein = DessinerArrondi(true);
        private static Sprite SpriteContour => arrondiContour != null ? arrondiContour : arrondiContour = DessinerArrondi(false);

        /// <summary>64 × 64 px, arrondi de 24 px, bord de 4 px, lissé ; découpé en 9 pour s'étirer sans déformer les coins.</summary>
        private static Sprite DessinerArrondi(bool plein)
        {
            const int taille = 64;
            const float rayon = 24f, epaisseur = 4f;
            var texture = new Texture2D(taille, taille, TextureFormat.RGBA32, false)
            {
                name = plein ? "Menu_Arrondi_Plein" : "Menu_Arrondi_Contour",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            var pixels = new Color32[taille * taille];
            for (int y = 0; y < taille; y++)
            for (int x = 0; x < taille; x++)
            {
                var p = new Vector2(x + 0.5f, y + 0.5f);
                var coin = new Vector2(Mathf.Clamp(p.x, rayon, taille - rayon), Mathf.Clamp(p.y, rayon, taille - rayon));
                float d = Vector2.Distance(p, coin);
                float alpha = Mathf.Clamp01(rayon - d + 0.5f);
                if (!plein) alpha *= Mathf.Clamp01(d - (rayon - epaisseur) + 0.5f);
                pixels[y * taille + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            const float bord = rayon + 4f;
            return Sprite.Create(texture, new Rect(0, 0, taille, taille), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(bord, bord, bord, bord));
        }

        // ---------------------------------------------------------------- Texte

        public Text Texte(string contenu, Transform parent, int taille, Color couleur, TextAnchor alignement,
            Vector2 position, Vector2 dimensions)
        {
            var objet = new GameObject("Texte - " + contenu, typeof(RectTransform), typeof(Text));
            objet.transform.SetParent(parent, false);
            var texte = objet.GetComponent<Text>();
            texte.text = contenu;
            texte.font = Police;
            texte.fontSize = taille;
            texte.color = couleur;
            texte.alignment = alignement;
            texte.horizontalOverflow = HorizontalWrapMode.Wrap;
            texte.verticalOverflow = VerticalWrapMode.Overflow;
            texte.raycastTarget = false;
            Placer(texte.rectTransform, position, dimensions);
            return texte;
        }

        /// <summary>Le titre d'un écran, ivoire avec une ombre, souligné d'un trait anis comme le logo.</summary>
        public Text Titre(string contenu, Transform parent, Vector2 position)
        {
            Text texte = Texte(contenu, parent, 84, Ivoire, TextAnchor.MiddleCenter, position, new Vector2(1100f, 110f));
            var ombre = texte.gameObject.AddComponent<Shadow>();
            ombre.effectColor = new Color(0.04f, 0.12f, 0.2f, 0.75f);
            ombre.effectDistance = new Vector2(3f, -4f);
            Image trait = Arrondi("Trait du titre", parent, true, Anis, 3f);
            Placer(trait.rectTransform, position + new Vector2(0f, -62f), new Vector2(420f, 12f));
            if (Habille) Tirets(parent, position, texte.preferredWidth);
            return texte;
        }

        /// <summary>
        /// Les trois traits verts de chaque côté d'un titre, en éventail (comme le logo FROGGY et les titres des maquettes
        /// de Julien).
        /// </summary>
        public static void Tirets(Transform parent, Vector2 centre, float largeurTexte)
        {
            for (int cote = -1; cote <= 1; cote += 2)
            for (int i = -1; i <= 1; i++)
            {
                Image trait = Arrondi("Tiret du titre", parent, true, Anis, 4f);
                float x = cote * (largeurTexte * .5f + 34f + (i == 0 ? 6f : 0f));
                Placer(trait.rectTransform, centre + new Vector2(x, i * 20f), new Vector2(30f, 9f));
                trait.rectTransform.localEulerAngles = new Vector3(0f, 0f, -cote * i * 28f);
            }
        }

        // ---------------------------------------------------------------- Boutons

        /// <summary>
        /// Un bouton en feuille de nénuphar, comme ceux de Julien : claire pour l'action principale (texte encre),
        /// sarcelle pour les autres (texte ivoire). La feuille s'éclaire quand le bouton a le focus (souris ou manette).
        /// </summary>
        public Button Feuille(string texte, Transform parent, bool claire, Vector2 position, Vector2 taille,
            UnityAction action)
        {
            var objet = new GameObject("Bouton - " + texte, typeof(RectTransform), typeof(Image), typeof(Button));
            objet.transform.SetParent(parent, false);
            Placer((RectTransform)objet.transform, position, taille);
            var image = objet.GetComponent<Image>();
            var bouton = objet.GetComponent<Button>();
            bouton.targetGraphic = image;

            Button modele = claire ? modeleClair : modeleSarcelle;
            if (modele != null && modele.image != null)
            {
                image.sprite = modele.image.sprite;
                bouton.transition = Selectable.Transition.SpriteSwap;
                bouton.spriteState = modele.spriteState;
            }
            else
            {
                image.sprite = SpritePlein;
                image.type = Image.Type.Sliced;
                image.color = claire ? Anis : Turquoise;
            }

            Text libelle = Texte(texte, objet.transform, Mathf.RoundToInt(taille.y * 0.34f), claire ? Encre : Ivoire,
                TextAnchor.MiddleCenter, new Vector2(-6f, -2f), new Vector2(taille.x * 0.8f, taille.y * 0.6f));
            UneLigne(libelle);
            if (action != null) bouton.onClick.AddListener(action);
            return bouton;
        }

        /// <summary>Un libellé trop long rapetisse au lieu de passer à la ligne (la zone ne tient pas deux lignes).</summary>
        private static void UneLigne(Text libelle)
        {
            libelle.name = "Libellé";
            libelle.resizeTextForBestFit = true;
            libelle.resizeTextMaxSize = libelle.fontSize;
            libelle.resizeTextMinSize = Mathf.Min(18, libelle.fontSize);
        }

        /// <summary>Un bouton secondaire en pilule bordée d'ivoire (Retour, Copier) ; le bord passe à l'anis au focus.</summary>
        public Button Pilule(string texte, Transform parent, Vector2 position, Vector2 taille, UnityAction action)
        {
            Image fond = Arrondi("Bouton - " + texte, parent, true, NuitProfonde, 1f);
            fond.raycastTarget = true;
            Placer(fond.rectTransform, position, taille);
            Image bord = Arrondi("Bord", fond.transform, false, Color.white, 1f);
            Etirer(bord.rectTransform);

            var bouton = fond.gameObject.AddComponent<Button>();
            bouton.targetGraphic = bord;
            bouton.transition = Selectable.Transition.ColorTint;
            bouton.colors = new ColorBlock
            {
                normalColor = Ivoire,
                highlightedColor = Anis,
                selectedColor = Anis,
                pressedColor = Turquoise,
                disabledColor = Brume,
                colorMultiplier = 1f,
                fadeDuration = 0.1f
            };

            int tailleTexte = Mathf.RoundToInt(Mathf.Min(taille.y * 0.42f, 34f));
            Text libelle = Texte(texte, fond.transform, tailleTexte, Ivoire, TextAnchor.MiddleCenter, Vector2.zero,
                new Vector2(taille.x - 36f, tailleTexte * 1.5f));
            UneLigne(libelle);
            if (action != null) bouton.onClick.AddListener(action);
            return bouton;
        }

        /// <summary>Change le texte d'un bouton fait par cette fabrique.</summary>
        public static void Libeller(Button bouton, string texte)
        {
            Text libelle = bouton != null ? bouton.GetComponentInChildren<Text>(true) : null;
            if (libelle != null) libelle.text = texte;
        }

        /// <summary>Active ou grise un bouton : une feuille grisée pâlit, son texte aussi.</summary>
        public static void Activer(Selectable s, bool actif)
        {
            if (s == null || s.interactable == actif) return;
            s.interactable = actif;
            float alpha = actif ? 1f : 0.45f;
            if (s.transition == Selectable.Transition.SpriteSwap && s.image != null)
                s.image.color = new Color(1f, 1f, 1f, alpha);
            foreach (Text t in s.GetComponentsInChildren<Text>(true))
            {
                Color c = t.color;
                c.a = alpha;
                t.color = c;
            }
        }

        // ---------------------------------------------------------------- Puces et curseurs

        /// <summary>Une petite étiquette arrondie (Prêt, En attente, le code).</summary>
        public Text Puce(string texte, Transform parent, Vector2 position, Vector2 taille, Color fond, Color encre,
            int tailleTexte = 30)
        {
            Image image = Arrondi("Puce - " + texte, parent, true, fond, 2f);
            Placer(image.rectTransform, position, taille);
            Text libelle = Texte(texte, image.transform, tailleTexte, encre, TextAnchor.MiddleCenter, Vector2.zero, taille);
            libelle.name = "Libellé";
            return libelle;
        }

        /// <summary>Un curseur de 0 à 1 : piste bleue, remplissage anis, poignée ivoire qui passe à l'anis au focus.</summary>
        public Slider Curseur(string nom, Transform parent, Vector2 position, Vector2 taille)
        {
            Image piste = Arrondi("Curseur - " + nom, parent, true, new Color32(78, 111, 120, 255), 3f);
            piste.raycastTarget = true;
            Placer(piste.rectTransform, position, taille);

            RectTransform zoneRemplissage = Zone("Zone de remplissage", piste.transform, Vector2.zero, Vector2.zero);
            Etirer(zoneRemplissage);
            Image remplissage = Arrondi("Remplissage", zoneRemplissage, true, Anis, 3f);
            Etirer(remplissage.rectTransform);

            RectTransform zonePoignee = Zone("Zone de la poignée", piste.transform, Vector2.zero, Vector2.zero);
            Etirer(zonePoignee);
            zonePoignee.offsetMin = new Vector2(taille.y * 0.5f, 0f);
            zonePoignee.offsetMax = new Vector2(-taille.y * 0.5f, 0f);
            Image poignee = Arrondi("Poignée", zonePoignee, true, Color.white, 1.2f);
            poignee.raycastTarget = true;
            // Le Slider étire la poignée sur la hauteur de la piste : sizeDelta.y s'y ajoute. Poignée ronde de 1,6 × la piste.
            poignee.rectTransform.sizeDelta = new Vector2(taille.y * 1.6f, taille.y * 0.6f);
            Sprite nenuphar = Habille ? Resources.Load<Sprite>("MenuEtang/Nenuphar_Anis") : null;
            if (nenuphar != null)
            {
                // la maquette de Julien : une piste nuit cerclée de turquoise, la poignée est un petit nénuphar
                piste.color = new Color32(13, 40, 62, 255);
                Image bordPiste = Arrondi("Bord", piste.transform, false, Turquoise, 3f);
                Etirer(bordPiste.rectTransform);
                bordPiste.transform.SetSiblingIndex(0);
                poignee.sprite = nenuphar;
                poignee.type = Image.Type.Simple;
                poignee.preserveAspect = true;
                poignee.rectTransform.sizeDelta = new Vector2(taille.y * 2.4f, taille.y * 1.4f);
            }

            var curseur = piste.gameObject.AddComponent<Slider>();
            curseur.fillRect = remplissage.rectTransform;
            curseur.handleRect = poignee.rectTransform;
            curseur.targetGraphic = poignee;
            curseur.direction = Slider.Direction.LeftToRight;
            curseur.minValue = 0f;
            curseur.maxValue = 1f;
            curseur.transition = Selectable.Transition.ColorTint;
            // La poignée passe au turquoise au focus (l'anis se perdrait sur le remplissage anis).
            curseur.colors = new ColorBlock
            {
                normalColor = Ivoire,
                highlightedColor = Turquoise,
                selectedColor = Turquoise,
                pressedColor = Turquoise,
                disabledColor = Brume,
                colorMultiplier = 1f,
                fadeDuration = 0.1f
            };
            return curseur;
        }
    }
}
