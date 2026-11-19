using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Froggy.Interface
{
    /// <summary>
    /// Le contenu de Paramètres (P6-05), le même au menu principal (EcranParametres) et dans la pause (MenuPause) :
    /// volumes, confort, champ de vision, puis la vue Commandes (réaffecter clavier et manette, « Par défaut »).
    /// Chaque réglage s'applique tout de suite (ReglagesJoueur.Change, Commandes.Propager) et s'écrit sur le disque en
    /// quittant le panneau. Une touche déjà prise par une autre commande est refusée, avec le nom de cette commande.
    /// Pendant qu'on attend une touche (<see cref="EnEcoute"/>), Échap annule l'écoute et ne referme rien.
    /// </summary>
    public sealed class PanneauReglages : MonoBehaviour
    {
        private FabriqueMenu f;
        private Action auRetour;
        private RectTransform vueReglages, vueCommandes;
        private Slider premier;
        private Button premiereTouche, boutonCommandes;
        private Text message;
        private readonly Button[,] boutons = new Button[Commandes.Lignes.Length, 2];
        private InputActionRebindingExtensions.RebindingOperation ecoute;
        private bool modifie;

        private static int finEcoute = int.MinValue / 2;

        /// <summary>Vrai pendant qu'on attend une touche, et l'image où l'écoute finit : Échap ne doit rien refermer.</summary>
        public static bool EnEcoute => instanceEnEcoute != null || Time.frameCount <= finEcoute + 1;
        private static PanneauReglages instanceEnEcoute;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reinitialiser()
        {
            instanceEnEcoute = null;
            finEcoute = int.MinValue / 2;
        }

        /// <summary>Ce qui reçoit le focus quand le panneau s'ouvre.</summary>
        public Selectable PremierFocus => vueCommandes != null && vueCommandes.gameObject.activeSelf
            ? premiereTouche : (Selectable)premier;

        /// <summary>Construit le panneau dans ce parent (étiré dessus). auRetour : quitter Paramètres.</summary>
        public static PanneauReglages Creer(Transform parent, FabriqueMenu fabrique, Action auRetour)
        {
            var objet = new GameObject("Réglages du joueur", typeof(RectTransform));
            objet.transform.SetParent(parent, false);
            FabriqueMenu.Etirer((RectTransform)objet.transform);
            var p = objet.AddComponent<PanneauReglages>();
            p.f = fabrique;
            p.auRetour = auRetour;
            p.Construire();
            return p;
        }

        /// <summary>Échap ou B : annule l'écoute, revient des Commandes aux réglages, ou quitte Paramètres.</summary>
        public bool Retour()
        {
            if (ecoute != null)
            {
                ecoute.Cancel();
                return true;
            }
            if (vueCommandes.gameObject.activeSelf)
            {
                Montrer(false, boutonCommandes);
                return true;
            }
            auRetour?.Invoke();
            return true;
        }

        private void OnEnable() => Montrer(false, premier);

        /// <summary>En quittant le panneau : on n'écrit le fichier du joueur que si quelque chose a changé.</summary>
        private void OnDisable()
        {
            if (ecoute != null) ecoute.Cancel();
            if (!modifie) return;
            modifie = false;
            ReglagesJoueur.Enregistrer();
        }

        private void Montrer(bool commandes, Selectable cible)
        {
            if (vueReglages == null) return;
            vueReglages.gameObject.SetActive(!commandes);
            vueCommandes.gameObject.SetActive(commandes);
            if (commandes) Rafraichir();
            Dire("");
            if (EventSystem.current != null && cible != null && isActiveAndEnabled) cible.Select();
        }

        // ---------------------------------------------------------------- Construction

        private void Construire()
        {
            f.Titre("Paramètres", transform, new Vector2(0f, 430f));

            vueReglages = FabriqueMenu.Panneau("Panneau", transform, new Vector2(0f, -20f), new Vector2(1100f, 720f),
                FabriqueMenu.Nuit, FabriqueMenu.Turquoise);
            ReglagesJoueur r = ReglagesJoueur.Actuels;
            Section(vueReglages, "Son", 300f);
            premier = Ligne(vueReglages, "Général", 240f, () => r.volumeGeneral, v => r.volumeGeneral = v);
            Ligne(vueReglages, "Musique", 182f, () => r.volumeMusique, v => r.volumeMusique = v);
            Ligne(vueReglages, "Effets", 124f, () => r.volumeEffets, v => r.volumeEffets = v);
            Ligne(vueReglages, "Ambiance", 66f, () => r.volumeAmbiance, v => r.volumeAmbiance = v);
            Section(vueReglages, "Confort", -4f);
            Ligne(vueReglages, "Secousses", -64f, () => r.secousses, v => r.secousses = v);
            Ligne(vueReglages, "Flashs", -122f, () => r.flashs, v => r.flashs = v);
            Ligne(vueReglages, "Vibrations", -180f, () => r.vibrations, v => r.vibrations = v);
            Ligne(vueReglages, "Champ de vision", -238f, () => r.champDeVision, v => r.champDeVision = v,
                ReglagesJoueur.ChampMin, ReglagesJoueur.ChampMax, v => Mathf.RoundToInt(v) + "°");
            f.Texte("0 % coupe l'effet. Réglages gardés sur cet ordinateur.", vueReglages, 28, FabriqueMenu.Brume,
                TextAnchor.MiddleCenter, new Vector2(0f, -310f), new Vector2(1000f, 44f));
            boutonCommandes = f.Feuille("Commandes", vueReglages, true, new Vector2(-220f, -435f),
                new Vector2(380f, 100f), () => Montrer(true, premiereTouche));
            f.Feuille("Retour", vueReglages, false, new Vector2(220f, -435f), new Vector2(380f, 100f),
                () => auRetour?.Invoke());

            vueCommandes = FabriqueMenu.Panneau("Commandes", transform, new Vector2(0f, -20f), new Vector2(1100f, 720f),
                FabriqueMenu.Nuit, FabriqueMenu.Turquoise);
            f.Texte("Clavier", vueCommandes, 30, FabriqueMenu.Anis, TextAnchor.MiddleCenter, new Vector2(ColonneClavier, 318f),
                new Vector2(240f, 44f));
            f.Texte("Manette", vueCommandes, 30, FabriqueMenu.Anis, TextAnchor.MiddleCenter, new Vector2(ColonneManette, 318f),
                new Vector2(240f, 44f));
            for (int i = 0; i < Commandes.Lignes.Length; i++)
            {
                float y = 268f - i * 50f;
                Commandes.Ligne ligne = Commandes.Lignes[i];
                Text libelle = f.Texte(ligne.Libelle, vueCommandes, 28, FabriqueMenu.Ivoire, TextAnchor.MiddleLeft,
                    new Vector2(-300f, y), new Vector2(420f, 46f));
                for (int a = 0; a < 2; a++)
                {
                    int li = i, ap = a;
                    Button b = f.Pilule("—", vueCommandes, new Vector2(a == 0 ? ColonneClavier : ColonneManette, y),
                        new Vector2(240f, 44f), () => Ecouter(li, (Commandes.Appareil)ap));
                    Text nom = b.GetComponentInChildren<Text>(true);   // lisible : la pilule seule donnerait 18 points
                    nom.fontSize = nom.resizeTextMaxSize = 26;
                    b.gameObject.AddComponent<LibelleDuFocus>().Libelle = libelle;
                    boutons[i, a] = b;
                }
            }
            premiereTouche = boutons[0, 0];
            message = f.Texte("", vueCommandes, 26, FabriqueMenu.Corail, TextAnchor.MiddleCenter,
                new Vector2(0f, -330f), new Vector2(1040f, 44f));
            f.Feuille("Par défaut", vueCommandes, false, new Vector2(-220f, -435f), new Vector2(380f, 100f), ParDefaut);
            f.Feuille("Retour", vueCommandes, false, new Vector2(220f, -435f), new Vector2(380f, 100f),
                () => Montrer(false, boutonCommandes));
            vueCommandes.gameObject.SetActive(false);
        }

        private const float ColonneClavier = 110f, ColonneManette = 380f;

        private void Section(Transform parent, string titre, float y) =>
            f.Texte(titre, parent, 36, FabriqueMenu.Anis, TextAnchor.MiddleLeft, new Vector2(-150f, y),
                new Vector2(700f, 52f));

        private Slider Ligne(Transform parent, string nom, float y, Func<float> lire, Action<float> ecrire,
            float min = 0f, float max = 1f, Func<float, string> format = null)
        {
            format ??= v => Mathf.RoundToInt(v * 100f) + " %";
            Text libelle = f.Texte(nom, parent, 32, FabriqueMenu.Ivoire, TextAnchor.MiddleLeft, new Vector2(-300f, y),
                new Vector2(340f, 52f));
            Slider curseur = f.Curseur(nom, parent, new Vector2(90f, y), new Vector2(460f, 22f));
            curseur.minValue = min;
            curseur.maxValue = max;
            curseur.wholeNumbers = max > 1f;
            curseur.gameObject.AddComponent<LibelleDuFocus>().Libelle = libelle;
            Text valeur = f.Texte("", parent, 30, FabriqueMenu.Ivoire, TextAnchor.MiddleRight, new Vector2(415f, y),
                new Vector2(130f, 52f));
            curseur.SetValueWithoutNotify(lire());
            valeur.text = format(lire());
            curseur.onValueChanged.AddListener(v =>
            {
                ecrire(v);
                modifie = true;
                valeur.text = format(v);
                ReglagesJoueur.Prevenir();
            });
            return curseur;
        }

        // ---------------------------------------------------------------- Commandes

        private void Rafraichir()
        {
            InputActionAsset actions = Commandes.Reference;
            for (int i = 0; i < Commandes.Lignes.Length; i++)
            {
                Commandes.Ligne ligne = Commandes.Lignes[i];
                for (int a = 0; a < 2; a++)
                {
                    var appareil = (Commandes.Appareil)a;
                    int index = Commandes.Index(Commandes.Action(actions, ligne), ligne.Partie, appareil);
                    bool possible = index >= 0;
                    // à la manette, le déplacement reste aux sticks : leur icône, grisée (ils ne se réaffectent pas)
                    Sprite icone = possible ? IconesTouches.Pour(Commandes.Action(actions, ligne), index)
                        : appareil == Commandes.Appareil.Manette ? Stick(ligne) : null;
                    Afficher(boutons[i, a], icone, possible ? Commandes.Nom(actions, ligne, appareil) : "—");
                    FabriqueMenu.Activer(boutons[i, a], possible);
                }
            }
        }

        private void Dire(string texte) => message.text = texte ?? "";

        // le stick d'une ligne de déplacement : le gauche avance et fait le pas de côté, le droit tourne
        private static Sprite Stick(Commandes.Ligne ligne)
        {
            if (ligne.Partie == null) return null;
            switch (ligne.Action + "/" + ligne.Partie)
            {
                case "Move/up": return IconesTouches.Stick(true, "up");
                case "Move/down": return IconesTouches.Stick(true, "down");
                case "Move/left": return IconesTouches.Stick(false, "left");
                case "Move/right": return IconesTouches.Stick(false, "right");
                case "PasDeCote/negative": return IconesTouches.Stick(true, "left");
                case "PasDeCote/positive": return IconesTouches.Stick(true, "right");
                default: return null;
            }
        }

        /// <summary>
        /// Une case de touche : l'icône seule, ou à défaut le nom écrit sur une touche blanche du même air (Espace, Maj,
        /// en français). Le focus la passe à l'anis ; grisée, elle ne se réaffecte pas (les sticks).
        /// </summary>
        private static void Afficher(Button bouton, Sprite icone, string texte)
        {
            Transform t = bouton.transform.Find("Icône");
            Image image;
            if (t == null)
            {
                var objet = new GameObject("Icône", typeof(RectTransform), typeof(Image));
                objet.transform.SetParent(bouton.transform, false);
                var rect = (RectTransform)objet.transform;
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(46f, 46f);
                image = objet.GetComponent<Image>();
                image.preserveAspect = true;
                image.raycastTarget = false;
            }
            else image = t.GetComponent<Image>();

            bool avecIcone = icone != null;
            float cote = 46f * IconesTouches.Echelle(icone);   // Espace, Verr. num… : leur nom doit se lire
            image.rectTransform.sizeDelta = new Vector2(cote, cote);
            image.sprite = icone;
            image.enabled = avecIcone;
            Image fond = bouton.GetComponent<Image>();
            Transform bord = bouton.transform.Find("Bord");
            if (bord != null) bord.GetComponent<Image>().enabled = false;
            Text libelle = bouton.GetComponentInChildren<Text>(true);
            bool vide = !avecIcone && texte == "—";   // pas de touche : un tiret, sans touche dessinée
            if (libelle != null)
            {
                libelle.enabled = !avecIcone;
                libelle.text = texte;
                libelle.color = vide ? Color.white : FabriqueMenu.NuitProfonde;
            }
            // sans icône : une touche blanche à la largeur du nom, de la hauteur d'une icône
            fond.color = avecIcone || vide ? Color.clear : Color.white;   // transparente, elle reste cliquable
            var cadre = (RectTransform)bouton.transform;
            float largeur = avecIcone || libelle == null ? cote : Mathf.Clamp(libelle.preferredWidth + 28f, 44f, 230f);
            cadre.sizeDelta = new Vector2(largeur, avecIcone ? 44f : 38f);
            if (libelle != null) libelle.rectTransform.sizeDelta = new Vector2(largeur - 12f, 34f);
            bouton.targetGraphic = avecIcone ? image : vide && libelle != null ? (Graphic)libelle : fond;
        }

        private void Ecouter(int i, Commandes.Appareil appareil)
        {
            if (ecoute != null) return;
            InputActionAsset actions = Commandes.Reference;
            Commandes.Ligne ligne = Commandes.Lignes[i];
            InputAction action = Commandes.Action(actions, ligne);
            int index = Commandes.Index(action, ligne.Partie, appareil);
            if (index < 0) return;

            string avant = action.bindings[index].overridePath;
            bool etaitActive = action.enabled;
            action.Disable();   // une action se réaffecte éteinte
            Button bouton = boutons[i, (int)appareil];
            FabriqueMenu.Libeller(bouton, "Appuie…");
            Dire(appareil == Commandes.Appareil.Clavier
                ? "Appuie sur une touche du clavier (Échap pour annuler)."
                : "Appuie sur un bouton de la manette (Échap pour annuler).");
            instanceEnEcoute = this;

            ecoute = action.PerformInteractiveRebinding(index)
                .WithControlsHavingToMatchPath(appareil == Commandes.Appareil.Clavier ? "<Keyboard>" : "<Gamepad>")
                .WithControlsExcluding("<Gamepad>/start")
                .WithControlsExcluding("<Keyboard>/escape")
                .WithCancelingThrough("<Keyboard>/escape")
                .OnMatchWaitForAnother(0.1f)
                .OnComplete(op =>
                {
                    string chemin = action.bindings[index].effectivePath;
                    // remettre l'ancienne touche le temps de vérifier : Affecter refuse une touche déjà prise
                    if (avant == null) action.RemoveBindingOverride(index);
                    else action.ApplyBindingOverride(index, avant);
                    if (Commandes.Affecter(actions, ligne, appareil, chemin, out string conflit))
                    {
                        modifie = true;
                        Commandes.Propager();
                        Dire("");
                    }
                    else Dire(conflit != null ? $"Déjà prise par « {conflit} » : choisis une autre touche." : "");
                    Finir(action, etaitActive, bouton, conflit != null);
                })
                .OnCancel(op =>
                {
                    Dire("");
                    Finir(action, etaitActive, bouton, false);
                })
                .Start();
        }

        private void Finir(InputAction action, bool etaitActive, Button bouton, bool garderMessage)
        {
            ecoute?.Dispose();
            ecoute = null;
            instanceEnEcoute = null;
            finEcoute = Time.frameCount;
            if (etaitActive) action.Enable();
            Rafraichir();
            if (!garderMessage) Dire("");
            if (bouton != null && EventSystem.current != null) bouton.Select();
        }

        private void ParDefaut()
        {
            Commandes.ParDefaut(Commandes.Reference);
            Commandes.Propager();
            modifie = true;
            Dire("");
            Rafraichir();
        }
    }
}
