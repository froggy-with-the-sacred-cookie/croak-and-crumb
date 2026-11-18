using Froggy.Template;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Froggy.Cameras
{
    /// <summary>
    /// Menu de debug de la caméra (éditeur et builds de développement seulement) : la touche au-dessus de Tab (« ² » en
    /// AZERTY sur PC, « @ » en AZERTY sur Mac, « ` » en QWERTY ; pas de touche F, macOS en intercepte) l'ouvre et le ferme.
    /// Sur un Mac à clavier ISO, Key.Backquote tombe sur la touche « < » à côté de Maj (29 octobre 2026, clavier de
    /// Florent) : on écoute donc aussi le caractère tapé. On y change de style de caméra
    /// (WoW, WoW premier essai où tout arrête la caméra, ancienne caméra du Template) et on règle ce qui se juge en jouant.
    /// Les réglages changés ici ne vivent que le temps de la partie. Menu ouvert : le curseur reste libre et la caméra
    /// ignore la souris, pour qu'un clic sur un bouton ne la fasse pas tourner. Si la scène a un ChoixMuraille (vitrines
    /// du jardin), le menu propose aussi de changer le contour de l'arène ; si le joueur a un FroggyApparence, de changer
    /// de Froggy (B, Kawaii, P… ou la forme provisoire) et de lancer sa Victoire ; si elle a une VoltigeSakura, d'allumer
    /// et d'éteindre les deux versions des pétales de sakura.
    /// </summary>
    public class MenuCamera : MonoBehaviour
    {
        public enum Style { Wow, WowToutBloque, Template }

        private static readonly string[] Noms = { "WoW", "WoW, premier essai (tout arrête la caméra)", "Template (ancienne caméra)" };

        private static readonly string[] Aides =
        {
            "Clic droit : tourner avec Froggy · Clic gauche : regarder autour\nGauche / droite : tourner · Q / E (A / E en AZERTY) : pas de côté\n" +
            "Deux boutons : avancer · Verr. num : course auto · Molette : zoom",
            "Comme WoW, mais chaque pierre, rocher ou bambou rapproche la caméra.",
            "Souris : regarder (curseur verrouillé, Échap le libère)\nZQSD / WASD : courir dans le repère de la caméra",
        };

        /// <summary>Le menu est ouvert : la caméra et le déplacement ignorent la souris.</summary>
        public static bool Ouvert { get; private set; }

        [SerializeField] private Key touche = Key.Backquote;
        // les caractères de la touche au-dessus de Tab selon le clavier, en plus de la touche ci-dessus
        [SerializeField] private string caracteres = "²@";

        private CameraOrbitale wow;
        private TemplateCameraRig template;
        private Froggy.Jardin.ChoixMuraille contour;
        private Froggy.Personnages.FroggyApparence apparence;
        private string[] nomsFroggy;
        private Froggy.Jardin.VoltigeSakura voltige;
        private bool rouvert;
        private Keyboard ecoute;
        private bool tape;
        private float derniereBascule = -1f;

        private void Awake()
        {
            if (!Application.isEditor && !Debug.isDebugBuild) enabled = false;
        }

        private void OnDisable()
        {
            Ouvert = false;
            Ecouter(null);
        }

        private void Ecouter(Keyboard clavier)
        {
            if (ecoute == clavier) return;
            if (ecoute != null) ecoute.onTextInput -= Caractere;
            ecoute = clavier;
            if (ecoute != null) ecoute.onTextInput += Caractere;
        }

        private void Caractere(char c)
        {
            if (caracteres.IndexOf(c) >= 0) tape = true;
        }

        private void Update()
        {
            var clavier = Keyboard.current;
            Ecouter(clavier);
            bool appui = tape || clavier != null && clavier[touche].wasPressedThisFrame;
            tape = false;
            // sur PC, « ² » arrive à la fois comme touche et comme caractère : une seule bascule
            if (appui && Time.unscaledTime - derniereBascule > .2f)
            {
                Ouvert = !Ouvert;
                derniereBascule = Time.unscaledTime;
            }
            Trouver();
            // on arrive d'une autre scène par le menu : il se rouvre, pour qu'on voie le choix appliqué
            if (!rouvert && Froggy.Jardin.ChoixMuraille.VientDeChanger) { Ouvert = true; rouvert = true; }
        }

        private void LateUpdate()
        {
            // après les Update : l'ancienne caméra verrouille le curseur au clic
            if (!Ouvert) return;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public Style Actuel => wow == null || !wow.enabled ? Style.Template : wow.ToutBloque ? Style.WowToutBloque : Style.Wow;

        /// <summary>Passe à un style de caméra (aussi pour une sonde ou un test).</summary>
        public void Choisir(Style style)
        {
            Trouver();
            if (wow == null) return;   // sans CameraOrbitale, seul le Template existe
            wow.enabled = style != Style.Template;
            if (style != Style.Template) wow.ToutBloque = style == Style.WowToutBloque;
        }

        private void Trouver()
        {
            if (wow == null) wow = FindFirstObjectByType<CameraOrbitale>(FindObjectsInactive.Include);
            if (template == null) template = FindFirstObjectByType<TemplateCameraRig>(FindObjectsInactive.Include);
            if (contour == null) contour = FindFirstObjectByType<Froggy.Jardin.ChoixMuraille>(FindObjectsInactive.Include);
            if (voltige == null) voltige = FindFirstObjectByType<Froggy.Jardin.VoltigeSakura>(FindObjectsInactive.Include);
            if (apparence == null)
            {
                apparence = FindFirstObjectByType<Froggy.Personnages.FroggyApparence>();
                nomsFroggy = null;
            }
            if (apparence != null && nomsFroggy == null)
            {
                // la première case est la forme provisoire du template (index -1 de FroggyApparence)
                nomsFroggy = new string[apparence.Nombre + 1];
                nomsFroggy[0] = "Forme provisoire (template)";
                for (int i = 0; i < apparence.Nombre; i++) nomsFroggy[i + 1] = apparence.Nom(i);
            }
        }

        private void OnGUI()
        {
            float e = Mathf.Max(1f, Screen.height / 900f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(e, e, 1f));
            if (!Ouvert)
            {
                GUI.Label(new Rect(12f, 8f, 420f, 22f), "Touche au-dessus de Tab : menu de debug");
                return;
            }
            float hauteur = 560f + (contour != null ? 160f : 0f) + (voltige != null ? 120f : 0f) + (apparence != null ? 70f + 22f * nomsFroggy.Length : 0f);
            GUILayout.BeginArea(new Rect(16f, 16f, 400f, hauteur), GUI.skin.box);
            GUILayout.Label("Caméra (debug) · la même touche pour fermer");
            var actuel = Actuel;
            int choix = GUILayout.SelectionGrid((int)actuel, Noms, 1);
            if (choix != (int)actuel) Choisir((Style)choix);
            GUILayout.Space(6f);
            GUILayout.Label(Aides[(int)Actuel]);

            if (wow != null && wow.enabled)
            {
                GUILayout.Space(8f);
                wow.SensibiliteSouris = Curseur("Sensibilité de la souris (°/pixel)", wow.SensibiliteSouris, 0.03f, 0.5f, "0.00");
                wow.VitesseVirage = Curseur("Virage au clavier (°/s)", wow.VitesseVirage, 60f, 360f, "0");
                wow.ChampTroisiemePersonne = Curseur("Champ de vision (°)", wow.ChampTroisiemePersonne, 40f, 90f, "0");
                wow.DistanceMax = Curseur("Distance max (m)", wow.DistanceMax, 4f, 30f, "0.0");
                wow.InverserY = GUILayout.Toggle(wow.InverserY, " Inverser l'axe vertical");
                GUILayout.Label($"Distance actuelle : {wow.Distance:0.0} m{(wow.PremierePersonne ? " (première personne)" : "")}");
            }
            if (contour != null)
            {
                GUILayout.Space(12f);
                GUILayout.Label("Contour de l'arène");
                var c = contour.Actuel;
                int choixContour = GUILayout.SelectionGrid((int)c, Froggy.Jardin.ChoixMuraille.Noms, 1);
                if (choixContour != (int)c) contour.Choisir((Froggy.Jardin.ChoixMuraille.Contour)choixContour);
            }
            if (voltige != null)
            {
                GUILayout.Space(12f);
                GUILayout.Label("Pétales de sakura (kit de Codex)");
                bool auVent = GUILayout.Toggle(voltige.AuVent, " Voltige au vent (particules)");
                if (auVent != voltige.AuVent) voltige.AuVent = auVent;
                if (voltige.AuVent) voltige.Densite = Curseur("Densité (pétales par seconde et par m²)", voltige.Densite, 0f, 0.08f, "0.000");
                // une boucle rallumée repart de son départ : on ne la touche que si la case change
                bool boucle = GUILayout.Toggle(voltige.Boucle, " Boucle précalculée de 8 s (ne suit pas le vent)");
                if (boucle != voltige.Boucle) voltige.Boucle = boucle;
            }
            if (apparence != null)
            {
                GUILayout.Space(12f);
                GUILayout.Label("Froggy");
                int f = apparence.Courant + 1;
                int choixFroggy = GUILayout.SelectionGrid(f, nomsFroggy, 1);
                if (choixFroggy != f) apparence.Afficher(choixFroggy - 1);
                GUI.enabled = apparence.Courant >= 0;
                if (GUILayout.Button("Victoire")) apparence.Celebrer();
                GUI.enabled = true;
            }
            GUILayout.FlexibleSpace();
            GUILayout.Label("Les réglages changés ici s'oublient à la fin de la partie.");
            GUILayout.EndArea();
        }

        private static float Curseur(string nom, float valeur, float min, float max, string format)
        {
            GUILayout.Label($"{nom} : {valeur.ToString(format)}");
            return GUILayout.HorizontalSlider(valeur, min, max);
        }
    }
}
