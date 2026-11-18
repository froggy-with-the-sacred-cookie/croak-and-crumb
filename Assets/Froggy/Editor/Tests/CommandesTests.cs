using System.Collections.Generic;
using System.IO;
using System.Linq;
using Froggy.Interface;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Froggy.Interface.Tests
{
    /// <summary>
    /// La réaffectation des touches et le champ de vision (P6-05). Chaque test travaille sur une copie des actions du
    /// projet, lue dans InputSystem_Actions ; les réglages passent par un fichier de test, jamais par le vrai.
    /// </summary>
    public class CommandesTests
    {
        private const string Fichier = "Assets/InputSystem_Actions.inputactions";
        private readonly List<InputActionAsset> crees = new List<InputActionAsset>();
        private string fichierTest;

        private InputActionAsset Actions()
        {
            var a = InputActionAsset.FromJson(File.ReadAllText(Fichier));
            crees.Add(a);
            return a;
        }

        private static Commandes.Ligne L(string libelle) => Commandes.Lignes.First(l => l.Libelle == libelle);

        private static string Chemin(InputActionAsset a, string libelle, Commandes.Appareil appareil)
        {
            Commandes.Ligne l = L(libelle);
            InputAction action = Commandes.Action(a, l);
            int i = Commandes.Index(action, l.Partie, appareil);
            return i < 0 ? null : action.bindings[i].effectivePath;
        }

        [SetUp]
        public void Preparer() =>
            fichierTest = Path.Combine(Path.GetTempPath(), "froggy_reglages_test_" + System.Guid.NewGuid() + ".json");

        [TearDown]
        public void Nettoyer()
        {
            Commandes.Utiliser(null);
            foreach (var a in crees) Object.DestroyImmediate(a);
            crees.Clear();
            if (File.Exists(fichierTest)) File.Delete(fichierTest);
        }

        [Test]
        public void ChaqueCommandeAUneToucheClavier()
        {
            var a = Actions();
            foreach (Commandes.Ligne l in Commandes.Lignes)
                Assert.That(Commandes.Index(Commandes.Action(a, l), l.Partie, Commandes.Appareil.Clavier),
                    Is.GreaterThanOrEqualTo(0), l.Libelle);
        }

        [Test]
        public void LesBoutonsSeReaffectentALaManette_LeDeplacementResteAuStick()
        {
            var a = Actions();
            foreach (string nom in new[] { "Sauter", "Dash", "Torgnole", "Utiliser l'objet", "Plonger", "Course automatique" })
                Assert.That(Commandes.Index(Commandes.Action(a, L(nom)), null, Commandes.Appareil.Manette),
                    Is.GreaterThanOrEqualTo(0), nom);
            foreach (string nom in new[] { "Avancer", "Pas de côté à gauche" })
            {
                Commandes.Ligne l = L(nom);
                Assert.That(Commandes.Index(Commandes.Action(a, l), l.Partie, Commandes.Appareil.Manette), Is.EqualTo(-1), nom);
            }
        }

        [Test]
        public void LesTouchesDuJeuRestentCellesDAvant()
        {
            var a = Actions();
            var clavier = Commandes.Appareil.Clavier;
            var manette = Commandes.Appareil.Manette;
            Assert.That(Chemin(a, "Avancer", clavier), Is.EqualTo("<Keyboard>/w"));
            Assert.That(Chemin(a, "Torgnole", clavier), Is.EqualTo("<Keyboard>/v"));
            Assert.That(Chemin(a, "Torgnole", manette), Is.EqualTo("<Gamepad>/buttonWest"));
            Assert.That(Chemin(a, "Utiliser l'objet", clavier), Is.EqualTo("<Keyboard>/f"));
            Assert.That(Chemin(a, "Utiliser l'objet", manette), Is.EqualTo("<Gamepad>/buttonNorth"));
            Assert.That(Chemin(a, "Plonger", clavier), Is.EqualTo("<Keyboard>/c"));
            Assert.That(Chemin(a, "Plonger", manette), Is.EqualTo("<Gamepad>/buttonEast"));
            Assert.That(Chemin(a, "Pas de côté à gauche", clavier), Is.EqualTo("<Keyboard>/q"));
            Assert.That(Chemin(a, "Pas de côté à droite", clavier), Is.EqualTo("<Keyboard>/e"));
            Assert.That(Chemin(a, "Course automatique", clavier), Is.EqualTo("<Keyboard>/numLock"));
            Assert.That(Chemin(a, "Course automatique", manette), Is.Empty);
            Assert.That(Chemin(a, "Dash", clavier), Is.EqualTo("<Keyboard>/leftShift"));
        }

        [Test]
        public void UneToucheChangeeSEnregistreEtSeRelit()
        {
            var a = Actions();
            Assert.That(Commandes.Affecter(a, L("Torgnole"), Commandes.Appareil.Clavier, "<Keyboard>/g", out _), Is.True);
            List<ToucheJoueur> touches = Commandes.Lire(a);
            Assert.That(touches, Has.Count.EqualTo(1));

            var r = new ReglagesJoueur { touches = touches, champDeVision = 75f };
            r.Ecrire(fichierTest);
            ReglagesJoueur relu = ReglagesJoueur.Lire(fichierTest);
            Assert.That(relu.champDeVision, Is.EqualTo(75f));

            var b = Actions();
            Commandes.Appliquer(b, relu.touches);
            Assert.That(Chemin(b, "Torgnole", Commandes.Appareil.Clavier), Is.EqualTo("<Keyboard>/g"));
            Assert.That(Chemin(b, "Sauter", Commandes.Appareil.Clavier), Is.EqualTo("<Keyboard>/space"));
        }

        [Test]
        public void UneToucheDejaPriseEstRefusee()
        {
            var a = Actions();
            bool ok = Commandes.Affecter(a, L("Torgnole"), Commandes.Appareil.Clavier, "<Keyboard>/space", out string conflit);
            Assert.That(ok, Is.False);
            Assert.That(conflit, Is.EqualTo("Sauter"));
            Assert.That(Chemin(a, "Torgnole", Commandes.Appareil.Clavier), Is.EqualTo("<Keyboard>/v"));
        }

        [Test]
        public void ParDefautRemetLesTouches_SansToucherALaSurchargeDeLaCamera()
        {
            var a = Actions();
            InputAction move = a.FindAction("Player/Move");
            int composite = Enumerable.Range(0, move.bindings.Count).First(i => move.bindings[i].isComposite);
            move.ApplyBindingOverride(composite, new InputBinding { overrideProcessors = "ScaleVector2(x=0,y=1)" });

            Assert.That(Commandes.Affecter(a, L("Avancer"), Commandes.Appareil.Clavier, "<Keyboard>/i", out _), Is.True);
            Assert.That(move.bindings[composite].overrideProcessors, Is.EqualTo("ScaleVector2(x=0,y=1)"));

            Commandes.ParDefaut(a);
            Assert.That(Chemin(a, "Avancer", Commandes.Appareil.Clavier), Is.EqualTo("<Keyboard>/w"));
            Assert.That(move.bindings[composite].overrideProcessors, Is.EqualTo("ScaleVector2(x=0,y=1)"));
            Assert.That(Commandes.Lire(a), Is.Empty);
        }

        [Test]
        public void UneCopieDUnPosteSuitLaReaffectation()
        {
            var a = Actions();
            Commandes.Utiliser(a);
            InputAction copie = Commandes.Creer("Torgnole");
            List<ToucheJoueur> avant = ReglagesJoueur.Actuels.touches;
            try
            {
                Assert.That(copie.bindings[0].effectivePath, Is.EqualTo("<Keyboard>/v"));
                Commandes.Affecter(a, L("Torgnole"), Commandes.Appareil.Clavier, "<Keyboard>/g", out _);
                Commandes.Propager();
                Assert.That(copie.bindings[0].effectivePath, Is.EqualTo("<Keyboard>/g"));
            }
            finally
            {
                ReglagesJoueur.Actuels.touches = avant;   // en mémoire seulement : rien n'est écrit
                Commandes.Liberer(copie);
            }
        }

        [Test]
        public void SansLesActionsDuProjet_LesTouchesDOrigine()
        {
            var vide = ScriptableObject.CreateInstance<InputActionAsset>();
            crees.Add(vide);
            Commandes.Utiliser(vide);
            InputAction plonger = Commandes.Creer("Plonger");
            try
            {
                Assert.That(plonger.bindings.Select(b => b.path), Is.EqualTo(new[] { "<Keyboard>/c", "<Gamepad>/buttonEast" }));
            }
            finally { Commandes.Liberer(plonger); }
        }

        private static readonly string[] ControlesManette =
        {
            "buttonSouth", "buttonEast", "buttonWest", "buttonNorth", "leftShoulder", "rightShoulder", "leftTrigger",
            "rightTrigger", "leftStickPress", "rightStickPress", "start", "select", "dpad/up", "dpad/down", "dpad/left",
            "dpad/right", "leftStick/up", "leftStick/down", "leftStick/left", "leftStick/right", "rightStick/left",
            "rightStick/right",
        };

        [Test]
        public void ChaqueBoutonDeChaqueManetteASonIcone()
        {
            foreach (IconesTouches.Famille f in System.Enum.GetValues(typeof(IconesTouches.Famille)))
                foreach (string c in ControlesManette)
                {
                    string fichier = IconesTouches.FichierManette(c, f);
                    Assert.That(fichier, Is.Not.Null, $"{f} {c}");
                    Assert.That(IconesTouches.Charger(fichier), Is.Not.Null, $"{f} {c} : {fichier}");
                }
        }

        [Test]
        public void LesBoutonsSontNommesParLeurPosition()
        {
            Assert.That(IconesTouches.FichierManette("buttonSouth", IconesTouches.Famille.Xbox), Is.EqualTo("xbox_button_a"));
            Assert.That(IconesTouches.FichierManette("buttonSouth", IconesTouches.Famille.PlayStation),
                Is.EqualTo("playstation_button_cross"));
            Assert.That(IconesTouches.FichierManette("buttonSouth", IconesTouches.Famille.Switch), Is.EqualTo("switch_button_b"));
            Assert.That(IconesTouches.FamilleDe("DualSenseGamepadHID"), Is.EqualTo(IconesTouches.Famille.PlayStation));
            Assert.That(IconesTouches.FamilleDe("SwitchProControllerHID"), Is.EqualTo(IconesTouches.Famille.Switch));
            Assert.That(IconesTouches.FamilleDe("XInputControllerWindows"), Is.EqualTo(IconesTouches.Famille.Xbox));
            Assert.That(IconesTouches.FamilleDe(null), Is.EqualTo(IconesTouches.Famille.Xbox));
        }

        [Test]
        public void AuClavier_ToutesLesTouchesDuPack_LesAutresEnTexte()
        {
            foreach (char c in "abcdefghijklmnopqrstuvwxyz0123456789")
                Assert.That(IconesTouches.Charger(IconesTouches.FichierClavier(c.ToString().ToUpper(), "<Keyboard>/x")),
                    Is.Not.Null, c.ToString());
            Assert.That(IconesTouches.FichierClavier("Z", "<Keyboard>/w"), Is.EqualTo("keyboard_z"));   // AZERTY
            Assert.That(IconesTouches.Charger(IconesTouches.FichierClavier("Haut", "<Keyboard>/upArrow")), Is.Not.Null);
            foreach (string chemin in new[] { "space", "leftShift", "numLock", "enter", "tab", "f1", "f12", "leftCtrl" })
                Assert.That(IconesTouches.Charger(IconesTouches.FichierClavier("?", "<Keyboard>/" + chemin)), Is.Not.Null, chemin);
            foreach (string signe in new[] { ",", ";", ":", "!", "*", "<" })
                Assert.That(IconesTouches.Charger(IconesTouches.FichierClavier(signe, "<Keyboard>/x")), Is.Not.Null, signe);
            Assert.That(IconesTouches.FichierClavier("é", "<Keyboard>/2"), Is.Null);
            Assert.That(IconesTouches.FichierClavier("ù", "<Keyboard>/quote"), Is.Null);
        }

        [Test]
        public void SansIcones_LeTexteDAvant()
        {
            var a = Actions();
            InputAction torgnole = a.FindAction("Player/Torgnole");
            Assert.That(IconesTouches.Pour(torgnole, 1), Is.Not.Null);
            IconesTouches.Actives = false;
            try { Assert.That(IconesTouches.Pour(torgnole, 1), Is.Null); }
            finally { IconesTouches.Actives = true; }
        }

        [Test]
        public void LeChampDeVisionEstBorne()
        {
            Assert.That(new ReglagesJoueur().champDeVision, Is.EqualTo(60f));
            var r = new ReglagesJoueur { champDeVision = 120f };
            r.Borner();
            Assert.That(r.champDeVision, Is.EqualTo(90f));
            r.champDeVision = 10f;
            r.Borner();
            Assert.That(r.champDeVision, Is.EqualTo(50f));
            r.champDeVision = float.NaN;
            r.Borner();
            Assert.That(r.champDeVision, Is.EqualTo(60f));
        }

        [Test]
        public void UnAncienFichierGardeLesNouveauxReglagesParDefaut()
        {
            File.WriteAllText(fichierTest, "{\"volumeGeneral\":0.5}");
            ReglagesJoueur r = ReglagesJoueur.Lire(fichierTest);
            Assert.That(r.volumeGeneral, Is.EqualTo(0.5f));
            Assert.That(r.champDeVision, Is.EqualTo(60f));
            Assert.That(r.touches, Is.Empty);
        }
    }
}
