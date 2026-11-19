using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Froggy.Interface
{
    /// <summary>
    /// Les icônes des touches (P6-05), pour Paramètres ▸ Commandes et le HUD : le bouton de la manette branchée (Xbox,
    /// PlayStation ou Switch, par sa position : le bouton du bas, de droite…), les sticks, et au clavier toutes les touches
    /// du pack (lettres, chiffres, flèches, Espace, Maj, F1…, ponctuation ; écrites en anglais sur les touches nommées).
    /// Une touche sans icône (é, è, ù…) s'écrit en texte.
    /// PROVISOIRE, à faire valider par l'équipe : pack « Input Prompts » 1.5a de Kenney (CC0, domaine public), dans
    /// <c>Interface/Resources/Touches</c>, en attendant les icônes de la charte (#62). <see cref="Actives"/> à faux remet le
    /// texte d'avant, pour comparer.
    /// </summary>
    public static class IconesTouches
    {
        public const string Dossier = "Touches";

        /// <summary>Faux : plus d'icônes, les noms des touches en texte (comme avant).</summary>
        public static bool Actives = true;

        public enum Famille { Xbox, PlayStation, Switch }

        private static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reinitialiser() => cache.Clear();

        /// <summary>Vrai si le dernier appareil touché est une manette (sinon le clavier).</summary>
        public static bool ParManette()
        {
            Keyboard clavier = Keyboard.current;
            Gamepad manette = Gamepad.current;
            return manette != null && (clavier == null || manette.lastUpdateTime > clavier.lastUpdateTime);
        }

        /// <summary>La famille de la manette branchée (Xbox sans manette, ou pour une manette inconnue).</summary>
        public static Famille FamilleActuelle() => FamilleDe(Gamepad.current != null ? Gamepad.current.layout : null);

        public static Famille FamilleDe(string layout)
        {
            if (string.IsNullOrEmpty(layout)) return Famille.Xbox;
            if (layout.Contains("DualShock") || layout.Contains("DualSense")) return Famille.PlayStation;
            if (layout.Contains("Switch")) return Famille.Switch;
            return Famille.Xbox;
        }

        /// <summary>
        /// Le fichier d'un contrôle de manette (« buttonSouth », « dpad/up », « leftStick/up »…) pour une famille, ou
        /// null s'il n'a pas d'icône. Les boutons sont nommés par leur position, comme dans l'Input System.
        /// </summary>
        public static string FichierManette(string controle, Famille f)
        {
            if (string.IsNullOrEmpty(controle)) return null;
            string p = f == Famille.PlayStation ? "playstation" : f == Famille.Switch ? "switch" : "xbox";
            if (controle.StartsWith("dpad/")) return $"{p}_dpad_{controle.Substring(5)}";
            if (controle.StartsWith("leftStick/")) return $"{p}_stick_l_{controle.Substring(10)}";
            if (controle.StartsWith("rightStick/")) return $"{p}_stick_r_{controle.Substring(11)}";
            switch (f)
            {
                case Famille.PlayStation:
                    switch (controle)
                    {
                        case "buttonSouth": return "playstation_button_cross";
                        case "buttonEast": return "playstation_button_circle";
                        case "buttonWest": return "playstation_button_square";
                        case "buttonNorth": return "playstation_button_triangle";
                        case "leftShoulder": return "playstation_trigger_l1";
                        case "rightShoulder": return "playstation_trigger_r1";
                        case "leftTrigger": return "playstation_trigger_l2";
                        case "rightTrigger": return "playstation_trigger_r2";
                        case "leftStickPress": return "playstation_button_l3";
                        case "rightStickPress": return "playstation_button_r3";
                        case "start": return "playstation5_button_options";
                        case "select": return "playstation5_button_create";
                    }
                    return null;
                case Famille.Switch:
                    switch (controle)
                    {
                        case "buttonSouth": return "switch_button_b";
                        case "buttonEast": return "switch_button_a";
                        case "buttonWest": return "switch_button_y";
                        case "buttonNorth": return "switch_button_x";
                        case "leftShoulder": return "switch_button_l";
                        case "rightShoulder": return "switch_button_r";
                        case "leftTrigger": return "switch_button_zl";
                        case "rightTrigger": return "switch_button_zr";
                        case "leftStickPress": return "switch_stick_l_press";
                        case "rightStickPress": return "switch_stick_r_press";
                        case "start": return "switch_button_plus";
                        case "select": return "switch_button_minus";
                    }
                    return null;
                default:
                    switch (controle)
                    {
                        case "buttonSouth": return "xbox_button_a";
                        case "buttonEast": return "xbox_button_b";
                        case "buttonWest": return "xbox_button_x";
                        case "buttonNorth": return "xbox_button_y";
                        case "leftShoulder": return "xbox_lb";
                        case "rightShoulder": return "xbox_rb";
                        case "leftTrigger": return "xbox_lt";
                        case "rightTrigger": return "xbox_rt";
                        case "leftStickPress": return "xbox_ls";
                        case "rightStickPress": return "xbox_rs";
                        case "start": return "xbox_button_menu";
                        case "select": return "xbox_button_view";
                    }
                    return null;
            }
        }

        // les touches nommées, par leur place sur le clavier (le nom de la touche dans l'Input System)
        private static readonly Dictionary<string, string> nommees = new Dictionary<string, string>
        {
            { "upArrow", "arrow_up" }, { "downArrow", "arrow_down" }, { "leftArrow", "arrow_left" },
            { "rightArrow", "arrow_right" }, { "space", "space" }, { "leftShift", "shift" }, { "rightShift", "shift" },
            { "leftCtrl", "ctrl" }, { "rightCtrl", "ctrl" }, { "leftAlt", "alt" }, { "rightAlt", "alt" },
            { "enter", "enter" }, { "numpadEnter", "numpad_enter" }, { "tab", "tab" }, { "escape", "escape" },
            { "backspace", "backspace" }, { "capsLock", "capslock" }, { "delete", "delete" }, { "end", "end" },
            { "home", "home" }, { "insert", "insert" }, { "pageUp", "page_up" }, { "pageDown", "page_down" },
            { "pause", "pause" }, { "printScreen", "printscreen" }, { "scrollLock", "scroll_lock" },
            { "numLock", "numlock" }, { "leftWindows", "win" }, { "rightWindows", "win" }, { "leftMeta", "win" },
            { "rightMeta", "win" }, { "numpadPlus", "numpad_plus" },
        };

        // la ponctuation, par le caractère affiché (il dépend du clavier de la machine)
        private static readonly Dictionary<char, string> signes = new Dictionary<char, string>
        {
            { ',', "comma" }, { '.', "period" }, { ';', "semicolon" }, { ':', "colon" }, { '!', "exclamation" },
            { '?', "question" }, { '\'', "apostrophe" }, { '"', "quote" }, { '-', "minus" }, { '+', "plus" },
            { '=', "equals" }, { '*', "asterisk" }, { '/', "slash_forward" }, { '\\', "slash_back" },
            { '[', "bracket_open" }, { ']', "bracket_close" }, { '<', "bracket_less" }, { '>', "bracket_greater" },
            { '~', "tilde" }, { '^', "caret" }, { '_', "underscore" },
        };

        /// <summary>
        /// Le fichier d'une touche du clavier : une touche nommée par sa place (Espace, Maj, F1…), sinon par le caractère
        /// affiché (la lettre du clavier de la machine : Z sur un AZERTY) ; null si le pack n'a pas cette touche.
        /// </summary>
        public static string FichierClavier(string affiche, string chemin)
        {
            if (!string.IsNullOrEmpty(chemin))
            {
                string touche = chemin.Substring(chemin.LastIndexOf('/') + 1);
                if (nommees.TryGetValue(touche, out string nom)) return "keyboard_" + nom;
                if (touche.Length >= 2 && touche.Length <= 3 && touche[0] == 'f' && int.TryParse(touche.Substring(1), out int n)
                    && n >= 1 && n <= 12)
                    return "keyboard_f" + n;
            }
            if (affiche == null || affiche.Length != 1) return null;
            char c = char.ToLowerInvariant(affiche[0]);
            if ((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9')) return "keyboard_" + c;
            return signes.TryGetValue(c, out string signe) ? "keyboard_" + signe : null;
        }

        /// <summary>L'icône de cette liaison d'une action, ou null (pas d'icône : on écrit le nom).</summary>
        public static Sprite Pour(InputAction action, int index)
        {
            if (!Actives || action == null || index < 0 || index >= action.bindings.Count) return null;
            string chemin = action.bindings[index].effectivePath;
            if (string.IsNullOrEmpty(chemin)) return null;
            if (chemin.StartsWith("<Gamepad>/"))
                return Charger(FichierManette(chemin.Substring("<Gamepad>/".Length), FamilleActuelle()));
            if (chemin.StartsWith("<Keyboard>/"))
                return Charger(FichierClavier(action.GetBindingDisplayString(index), chemin));
            return null;
        }

        // toutes les touches du pack ont la même largeur ; un nom écrit dessus (SPACE, NUM LOCK) est donc petit
        private static readonly HashSet<string> larges =
            new HashSet<string> { "space", "shift", "ctrl", "alt", "tab", "backspace", "capslock" };

        /// <summary>
        /// L'agrandissement d'une icône pour que le nom écrit dessus se lise : ×1,7 pour une touche large (Espace, Maj,
        /// Ctrl…), ×1,25 pour une touche carrée nommée (Verr. num, Entrée, Échap…), ×1 sinon (lettres, boutons).
        /// </summary>
        public static float Echelle(Sprite icone)
        {
            if (icone == null || !icone.name.StartsWith("keyboard_")) return 1f;
            string nom = icone.name.Substring("keyboard_".Length);
            if (larges.Contains(nom)) return 1.7f;
            return nommees.ContainsValue(nom) && !nom.StartsWith("arrow") && nom != "numpad_plus" ? 1.25f : 1f;
        }

        /// <summary>L'icône d'un stick dans une direction (« up », « left »…), pour la manette branchée.</summary>
        public static Sprite Stick(bool gauche, string sens) =>
            Actives ? Charger(FichierManette((gauche ? "leftStick/" : "rightStick/") + sens, FamilleActuelle())) : null;

        /// <summary>Une icône du dossier Resources/Touches, ou null si elle manque.</summary>
        public static Sprite Charger(string fichier)
        {
            if (string.IsNullOrEmpty(fichier)) return null;
            if (!cache.TryGetValue(fichier, out Sprite s) || s == null)
            {
                s = Resources.Load<Sprite>(Dossier + "/" + fichier);
                cache[fichier] = s;
            }
            return s;
        }
    }
}
