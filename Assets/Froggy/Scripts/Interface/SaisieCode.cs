using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Froggy.Interface
{
    /// <summary>
    /// Le champ du code de salon (P6-03) : 4 cases ivoire. Au clavier, on tape le code n'importe quand pendant que
    /// l'écran est ouvert (Retour arrière efface, Cmd/Ctrl + V colle). À la manette, quand le champ a le focus : haut et
    /// bas font défiler la lettre, gauche et droite changent de case, et la navigation sort du champ par les bords.
    /// La logique est dans CodeEnSaisie.
    /// </summary>
    public sealed class SaisieCode : Selectable, ISubmitHandler, IPointerClickHandler
    {
        private CodeEnSaisie saisie = new CodeEnSaisie();
        private Image[] cases;
        private Image[] bords;
        private Text[] lettres;
        private bool aLeFocus;

        /// <summary>Le code a changé (lettre tapée, effacée, collée).</summary>
        public event Action Change;

        /// <summary>Entrée ou bouton de validation sur un code complet.</summary>
        public event Action Valide;

        public CodeEnSaisie Saisie => saisie;

        /// <summary>
        /// Fabrique le champ : une case par caractère du code (4 en réseau local, 6 en ligne, P2-05), de 104 × 120,
        /// espacées de 18.
        /// </summary>
        public static SaisieCode Construire(FabriqueMenu f, Transform parent, Vector2 position, bool enLigne = true)
        {
            const float largeurCase = 104f, hauteur = 120f, ecart = 18f;
            var saisieDuMode = new CodeEnSaisie(enLigne);
            int n = saisieDuMode.Longueur;
            float largeur = n * largeurCase + (n - 1) * ecart;

            Image zone = FabriqueMenu.Arrondi("Code du salon", parent, true, new Color(1f, 1f, 1f, 0f));
            zone.raycastTarget = true;
            FabriqueMenu.Placer(zone.rectTransform, position, new Vector2(largeur + 24f, hauteur + 24f));

            var champ = zone.gameObject.AddComponent<SaisieCode>();
            champ.saisie = saisieDuMode;
            champ.targetGraphic = zone;
            champ.transition = Transition.None;
            champ.cases = new Image[n];
            champ.bords = new Image[n];
            champ.lettres = new Text[n];
            for (int i = 0; i < n; i++)
            {
                float x = -largeur * 0.5f + largeurCase * 0.5f + i * (largeurCase + ecart);
                Image fond = FabriqueMenu.Arrondi($"Case {i + 1}", zone.transform, true, FabriqueMenu.Ivoire, 1.5f);
                FabriqueMenu.Placer(fond.rectTransform, new Vector2(x, 0f), new Vector2(largeurCase, hauteur));
                Image bord = FabriqueMenu.Arrondi("Bord", fond.transform, false, FabriqueMenu.Anis, 0.7f);
                FabriqueMenu.Etirer(bord.rectTransform);
                champ.cases[i] = fond;
                champ.bords[i] = bord;
                champ.lettres[i] = f.Texte("", fond.transform, 70, FabriqueMenu.Encre, TextAnchor.MiddleCenter,
                    new Vector2(0f, -2f), new Vector2(largeurCase, hauteur));
            }
            champ.Redessiner();
            return champ;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (Keyboard.current != null) Keyboard.current.onTextInput += QuandTexte;
            Redessiner();
        }

        protected override void OnDisable()
        {
            if (Keyboard.current != null) Keyboard.current.onTextInput -= QuandTexte;
            aLeFocus = false;
            base.OnDisable();
        }

        public void Vider()
        {
            saisie.Vider();
            Modifie();
        }

        private void Update()
        {
            Keyboard clavier = Keyboard.current;
            if (clavier == null) return;
            if (clavier.backspaceKey.wasPressedThisFrame)
            {
                saisie.Effacer();
                Modifie();
            }
            bool commande = clavier.leftCommandKey.isPressed || clavier.rightCommandKey.isPressed
                            || clavier.leftCtrlKey.isPressed || clavier.rightCtrlKey.isPressed;
            if (commande && clavier.vKey.wasPressedThisFrame)
            {
                saisie.Coller(GUIUtility.systemCopyBuffer);
                Modifie();
                Prendre();
            }
        }

        /// <summary>Qui tape un code prend le champ : Entrée valide alors le code, pas le bouton qui avait le focus.</summary>
        private void Prendre()
        {
            if (!aLeFocus && EventSystem.current != null) Select();
        }

        private void QuandTexte(char c)
        {
            if (!isActiveAndEnabled || !IsInteractable()) return;
            if (char.IsControl(c)) return;
            Keyboard clavier = Keyboard.current;
            if (clavier != null && (clavier.leftCommandKey.isPressed || clavier.rightCommandKey.isPressed
                                    || clavier.leftCtrlKey.isPressed || clavier.rightCtrlKey.isPressed)) return;
            if (!saisie.Taper(c)) return;
            Modifie();
            Prendre();
        }

        // ---------------------------------------------------------------- Manette et navigation

        public override void OnMove(AxisEventData e)
        {
            switch (e.moveDir)
            {
                case MoveDirection.Up:
                    saisie.Defiler(+1);
                    Modifie();
                    e.Use();
                    return;
                case MoveDirection.Down:
                    saisie.Defiler(-1);
                    Modifie();
                    e.Use();
                    return;
                case MoveDirection.Left when saisie.Deplacer(-1):
                case MoveDirection.Right when saisie.Deplacer(+1):
                    Redessiner();
                    e.Use();
                    return;
            }
            base.OnMove(e);
        }

        public void OnSubmit(BaseEventData e)
        {
            if (saisie.Complet) Valide?.Invoke();
            else if (saisie.Deplacer(+1)) Redessiner();
        }

        public void OnPointerClick(PointerEventData e)
        {
            // La case cliquée devient la case active.
            for (int i = 0; i < cases.Length; i++)
            {
                if (!RectTransformUtility.RectangleContainsScreenPoint(cases[i].rectTransform, e.position,
                        e.pressEventCamera)) continue;
                saisie.Aller(i);
                break;
            }
            Select();
            Redessiner();
        }

        public override void OnSelect(BaseEventData e)
        {
            base.OnSelect(e);
            aLeFocus = true;
            Redessiner();
        }

        public override void OnDeselect(BaseEventData e)
        {
            base.OnDeselect(e);
            aLeFocus = false;
            Redessiner();
        }

        private void Modifie()
        {
            Redessiner();
            Change?.Invoke();
        }

        /// <summary>Les lettres dans les cases ; la case active a un bord anis quand le champ a le focus.</summary>
        private void Redessiner()
        {
            if (cases == null) return;
            for (int i = 0; i < cases.Length; i++)
            {
                char c = saisie[i];
                lettres[i].text = c == CodeEnSaisie.Vide ? "" : c.ToString();
                bords[i].enabled = aLeFocus && i == saisie.Curseur;
                cases[i].color = c == CodeEnSaisie.Vide ? new Color(0.98f, 0.94f, 0.81f, 0.78f) : FabriqueMenu.Ivoire;
            }
        }
    }
}
