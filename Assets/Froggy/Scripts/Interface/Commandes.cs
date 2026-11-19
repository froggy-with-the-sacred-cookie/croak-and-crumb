using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Froggy.Interface
{
    /// <summary>
    /// Les commandes du jeu et leur réaffectation (P6-05). Toutes les actions de Froggy vivent dans la carte « Player »
    /// d'InputSystem_Actions (les actions du projet) : Move, Jump, Dash, Torgnole, UtiliserObjet, Plonger, PasDeCote,
    /// CourseAuto. Le joueur change une touche dans l'écran Commandes ; seul le chemin d'une liaison change, gardé par
    /// son identifiant dans ReglagesJoueur.touches.
    ///
    /// Pour un poste qui lit une touche :
    ///   - une action propre au poste : <c>Commandes.Creer("Torgnole")</c> (une copie à soi, qui suit la réaffectation),
    ///     puis <c>Commandes.Liberer(action)</c> quand on n'en veut plus ;
    ///   - les actions d'un PlayerInput : <c>Commandes.Suivre(playerInput.actions)</c>.
    /// Une touche affichée se lit sur l'action (GetBindingDisplayString), jamais en dur (skill accessibilite).
    /// Seuls les chemins changent : les surcharges de processeur posées par la caméra sur les composites de Move restent.
    /// </summary>
    public static class Commandes
    {
        public const string Carte = "Player";

        public enum Appareil { Clavier, Manette }

        /// <summary>Une ligne de l'écran Commandes : une action, et la partie d'un composite (null pour un bouton).</summary>
        public readonly struct Ligne
        {
            public readonly string Libelle, Action, Partie;
            public Ligne(string libelle, string action, string partie = null)
            {
                Libelle = libelle;
                Action = action;
                Partie = partie;
            }
        }

        /// <summary>Ce que le joueur peut réaffecter, dans l'ordre de l'écran.</summary>
        public static readonly Ligne[] Lignes =
        {
            new Ligne("Avancer", "Move", "up"),
            new Ligne("Reculer", "Move", "down"),
            new Ligne("Tourner à gauche", "Move", "left"),
            new Ligne("Tourner à droite", "Move", "right"),
            new Ligne("Pas de côté à gauche", "PasDeCote", "negative"),
            new Ligne("Pas de côté à droite", "PasDeCote", "positive"),
            new Ligne("Sauter", "Jump"),
            new Ligne("Dash", "Dash"),
            new Ligne("Torgnole", "Torgnole"),
            new Ligne("Utiliser l'objet", "UtiliserObjet"),
            new Ligne("Plonger", "Plonger"),
            new Ligne("Course automatique", "CourseAuto"),
        };

        /// <summary>Une touche a changé : les copies et les PlayerInput suivis l'ont déjà ; à écouter pour reconstruire.</summary>
        public static event Action Change;

        private static InputActionAsset reference;
        private static readonly List<InputActionAsset> suivis = new List<InputActionAsset>();
        private static readonly List<(InputAction copie, string nom)> copies = new List<(InputAction, string)>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reinitialiser()
        {
            reference = null;
            suivis.Clear();
            copies.Clear();
            Change = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Charger()
        {
            if (Reference != null) Appliquer(Reference, ReglagesJoueur.Actuels.touches);
        }

        /// <summary>Les actions que l'écran Commandes réaffecte : celles du projet, ou celles d'un test.</summary>
        public static InputActionAsset Reference => reference != null ? reference : InputSystem.actions;

        /// <summary>Pour un test : travailler sur ces actions plutôt que sur celles du projet (null : revenir au projet).</summary>
        public static void Utiliser(InputActionAsset actions)
        {
            reference = actions;
            suivis.Clear();
            copies.Clear();
        }

        // ---------------------------------------------------------------- Lire et écrire les liaisons (sans état)

        /// <summary>L'index de la liaison de cette ligne pour cet appareil dans l'action, ou −1.</summary>
        public static int Index(InputAction action, string partie, Appareil appareil)
        {
            if (action == null) return -1;
            string groupe = appareil == Appareil.Clavier ? "Keyboard&Mouse" : "Gamepad";
            var liaisons = action.bindings;
            for (int i = 0; i < liaisons.Count; i++)
            {
                InputBinding b = liaisons[i];
                if (b.isComposite) continue;
                if (partie != null && (!b.isPartOfComposite || !string.Equals(b.name, partie, StringComparison.OrdinalIgnoreCase)))
                    continue;
                if (partie == null && b.isPartOfComposite) continue;
                if (DansLeGroupe(b.groups, groupe)) return i;
            }
            return -1;
        }

        private static bool DansLeGroupe(string groupes, string groupe)
        {
            if (string.IsNullOrEmpty(groupes)) return false;
            foreach (string g in groupes.Split(InputBinding.Separator))
                if (g == groupe) return true;
            return false;
        }

        public static InputAction Action(InputActionAsset actions, Ligne ligne) =>
            actions != null ? actions.FindAction(Carte + "/" + ligne.Action) : null;

        /// <summary>Le nom de la touche d'une ligne (selon le clavier de la machine), ou « — » sans touche.</summary>
        public static string Nom(InputActionAsset actions, Ligne ligne, Appareil appareil)
        {
            InputAction action = Action(actions, ligne);
            int i = Index(action, ligne.Partie, appareil);
            if (i < 0 || string.IsNullOrEmpty(action.bindings[i].effectivePath)) return "—";
            string nom = action.GetBindingDisplayString(i);
            return string.IsNullOrEmpty(nom) ? "—" : nom;
        }

        /// <summary>La ligne qui utilise déjà ce chemin sur cet appareil (autre que la ligne donnée), ou null.</summary>
        public static string Conflit(InputActionAsset actions, Ligne ligne, Appareil appareil, string chemin)
        {
            if (string.IsNullOrEmpty(chemin)) return null;
            foreach (Ligne autre in Lignes)
            {
                if (autre.Action == ligne.Action && autre.Partie == ligne.Partie) continue;
                InputAction action = Action(actions, autre);
                int i = Index(action, autre.Partie, appareil);
                if (i >= 0 && string.Equals(action.bindings[i].effectivePath, chemin, StringComparison.OrdinalIgnoreCase))
                    return autre.Libelle;
            }
            return null;
        }

        /// <summary>
        /// Donne ce chemin à une ligne. Refusé (faux, et le nom de l'autre ligne) si une autre ligne l'utilise déjà sur
        /// le même appareil : deux actions sur une touche, l'une des deux ne marcherait plus.
        /// </summary>
        public static bool Affecter(InputActionAsset actions, Ligne ligne, Appareil appareil, string chemin, out string conflit)
        {
            conflit = Conflit(actions, ligne, appareil, chemin);
            if (conflit != null) return false;
            InputAction action = Action(actions, ligne);
            int i = Index(action, ligne.Partie, appareil);
            if (i < 0) return false;
            Chemin(action, i, chemin == action.bindings[i].path ? null : chemin);
            return true;
        }

        /// <summary>Remet toutes les touches du jeu (les surcharges de processeur restent).</summary>
        public static void ParDefaut(InputActionAsset actions) => Appliquer(actions, null);

        /// <summary>Les touches changées de ces actions, à enregistrer.</summary>
        public static List<ToucheJoueur> Lire(InputActionAsset actions)
        {
            var liste = new List<ToucheJoueur>();
            InputActionMap carte = actions != null ? actions.FindActionMap(Carte) : null;
            if (carte == null) return liste;
            foreach (InputBinding b in carte.bindings)
                if (!b.isComposite && b.overridePath != null && b.overridePath != b.path)
                    liste.Add(new ToucheJoueur { id = b.id.ToString(), chemin = b.overridePath });
            return liste;
        }

        /// <summary>Pose ces touches sur ces actions (null ou vide : les touches du jeu). Ne touche qu'aux chemins.</summary>
        public static void Appliquer(InputActionAsset actions, List<ToucheJoueur> touches)
        {
            InputActionMap carte = actions != null ? actions.FindActionMap(Carte) : null;
            if (carte == null) return;
            var parId = new Dictionary<string, string>();
            if (touches != null)
                foreach (ToucheJoueur t in touches)
                    if (t != null && !string.IsNullOrEmpty(t.id)) parId[t.id] = t.chemin;
            foreach (InputAction action in carte.actions)
            {
                var liaisons = action.bindings;
                for (int i = 0; i < liaisons.Count; i++)
                {
                    if (liaisons[i].isComposite) continue;
                    parId.TryGetValue(liaisons[i].id.ToString(), out string chemin);
                    Chemin(action, i, chemin);
                }
            }
        }

        /// <summary>Recopie les chemins de la source sur sa copie (mêmes liaisons, dans le même ordre).</summary>
        public static void Recopier(InputAction source, InputAction copie)
        {
            if (source == null || copie == null) return;
            int n = Mathf.Min(source.bindings.Count, copie.bindings.Count);
            for (int i = 0; i < n; i++)
                if (!source.bindings[i].isComposite) Chemin(copie, i, source.bindings[i].overridePath);
        }

        // Change seulement le chemin d'une liaison (null : celui du jeu), en gardant ses autres surcharges.
        private static void Chemin(InputAction action, int i, string chemin)
        {
            InputBinding b = action.bindings[i];
            if (chemin == b.path) chemin = null;
            if (b.overridePath == chemin) return;
            if (chemin == null && b.overrideProcessors == null && b.overrideInteractions == null)
            {
                action.RemoveBindingOverride(i);
                return;
            }
            action.ApplyBindingOverride(i, new InputBinding
            {
                overridePath = chemin,
                overrideProcessors = b.overrideProcessors,
                overrideInteractions = b.overrideInteractions
            });
        }

        // ---------------------------------------------------------------- En jeu

        /// <summary>
        /// Une copie à soi de l'action du jeu (à activer, puis à rendre par Liberer) ; elle suit la réaffectation.
        /// Sans les actions du projet (certains tests), les touches d'origine du jeu.
        /// </summary>
        public static InputAction Creer(string nom)
        {
            InputAction source = Reference != null ? Reference.FindAction(Carte + "/" + nom) : null;
            InputAction copie = source != null ? source.Clone() : Secours(nom);
            if (source != null) Recopier(source, copie);
            copies.Add((copie, nom));
            return copie;
        }

        /// <summary>Rend une copie faite par Creer.</summary>
        public static void Liberer(InputAction copie)
        {
            if (copie == null) return;
            copies.RemoveAll(c => c.copie == copie);
            copie.Disable();
            copie.Dispose();
        }

        /// <summary>Les actions d'un PlayerInput suivent la réaffectation (une copie des actions du projet, ou elles-mêmes).</summary>
        public static void Suivre(InputActionAsset actions)
        {
            if (actions == null || actions == Reference) return;
            suivis.RemoveAll(a => a == null);
            if (!suivis.Contains(actions)) suivis.Add(actions);
            Appliquer(actions, Lire(Reference));
        }

        /// <summary>
        /// L'écran Commandes a changé une touche des actions du projet : les copies et les PlayerInput suivis la prennent,
        /// et elle est notée dans les réglages du joueur (écrits sur le disque en quittant l'écran).
        /// </summary>
        public static void Propager()
        {
            List<ToucheJoueur> touches = Lire(Reference);
            ReglagesJoueur.Actuels.touches = touches;
            suivis.RemoveAll(a => a == null);
            foreach (InputActionAsset a in suivis) Appliquer(a, touches);
            foreach (var (copie, nom) in copies)
                Recopier(Reference != null ? Reference.FindAction(Carte + "/" + nom) : null, copie);
            Change?.Invoke();
        }

        // les touches d'origine, quand les actions du projet manquent (elles doivent rester celles d'InputSystem_Actions)
        private static InputAction Secours(string nom)
        {
            switch (nom)
            {
                case "PasDeCote":
                    var axe = new InputAction(nom, InputActionType.Value);
                    axe.AddCompositeBinding("1DAxis").With("Negative", "<Keyboard>/q").With("Positive", "<Keyboard>/e");
                    return axe;
                case "CourseAuto": return Bouton(nom, "<Keyboard>/numLock", null);
                case "Torgnole": return Bouton(nom, "<Keyboard>/v", "<Gamepad>/buttonWest");
                case "UtiliserObjet": return Bouton(nom, "<Keyboard>/f", "<Gamepad>/buttonNorth");
                case "Plonger": return Bouton(nom, "<Keyboard>/c", "<Gamepad>/buttonEast");
                default: return new InputAction(nom, InputActionType.Button);
            }
        }

        private static InputAction Bouton(string nom, string clavier, string manette)
        {
            var action = new InputAction(nom, InputActionType.Button);
            action.AddBinding(clavier);
            if (manette != null) action.AddBinding(manette);
            return action;
        }
    }
}
