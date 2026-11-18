using Froggy.Reseau;
using UnityEngine;
using UnityEngine.UI;

namespace Froggy.Interface
{
    /// <summary>
    /// « Rejoindre un salon » (P6-03) : taper le code de 4 lettres donné par l'hôte, ou créer son salon. Branché sur
    /// Salon.Local (P2-03) ; dès qu'on est dans un salon, MenuPrincipal passe à la salle d'attente.
    /// La liste des salons des maquettes de Julien attend le relais (P2-05) : aujourd'hui un salon se trouve par son code.
    /// </summary>
    public sealed class EcranSalons : EcranMenu
    {
        private SaisieCode code;
        private Button creer, rejoindre, retour;
        private Text etat;
        private Salon abonne;

        public override Selectable PremierFocus => code;

        protected override void Construire()
        {
            FabriqueMenu f = Fabrique;
            f.Titre("Rejoindre un salon", transform, new Vector2(0f, 400f));
            RectTransform panneau = FabriqueMenu.Panneau("Panneau", transform, new Vector2(0f, -10f),
                new Vector2(1100f, 600f), FabriqueMenu.Nuit, FabriqueMenu.Turquoise);

            f.Texte("Code du salon", panneau, 40, FabriqueMenu.Ivoire, TextAnchor.MiddleCenter,
                new Vector2(0f, 220f), new Vector2(900f, 60f));
            code = SaisieCode.Construire(f, panneau, new Vector2(0f, 105f));
            code.Change += Rafraichir;
            code.Valide += Rejoindre;
            f.Texte("Les 4 lettres données par l'hôte.", panneau, 28, FabriqueMenu.Brume, TextAnchor.MiddleCenter,
                new Vector2(0f, 10f), new Vector2(900f, 44f));
            etat = f.Texte("", panneau, 30, FabriqueMenu.Ivoire, TextAnchor.MiddleCenter,
                new Vector2(0f, -60f), new Vector2(980f, 80f));

            creer = f.Feuille("Créer un salon", panneau, false, new Vector2(-255f, -195f), new Vector2(470f, 108f),
                Creer);
            rejoindre = f.Feuille("Rejoindre", panneau, true, new Vector2(255f, -195f), new Vector2(470f, 112f),
                Rejoindre);
            retour = f.Pilule("Retour", transform, new Vector2(0f, -400f), new Vector2(260f, 74f),
                () => Menu.Afficher(Ecran.Accueil));
        }

        private void OnEnable()
        {
            abonne = Salon.Local;
            if (abonne != null) abonne.Change += Rafraichir;
            Rafraichir();
        }

        private void OnDisable()
        {
            if (abonne != null) abonne.Change -= Rafraichir;
            abonne = null;
        }

        public override bool Retour()
        {
            if (abonne != null && abonne.Occupe) return true; // on attend la fin de la recherche
            Menu.Afficher(Ecran.Accueil);
            return true;
        }

        private void Creer()
        {
            if (abonne != null && !abonne.Occupe && !abonne.Connecte) abonne.Creer();
        }

        private void Rejoindre()
        {
            if (abonne != null && !abonne.Occupe && !abonne.Connecte && code.Saisie.Complet)
                abonne.Rejoindre(code.Saisie.Code);
        }

        private void Rafraichir()
        {
            if (etat == null) return;
            if (abonne == null)
            {
                etat.text = "Pas de salon dans cette scène : ouvrir la scène Menu.";
                etat.color = FabriqueMenu.Corail;
                FabriqueMenu.Activer(creer, false);
                FabriqueMenu.Activer(rejoindre, false);
                return;
            }

            bool libre = !abonne.Occupe && !abonne.Connecte;
            FabriqueMenu.Activer(creer, libre);
            FabriqueMenu.Activer(rejoindre, libre && code.Saisie.Complet);
            FabriqueMenu.Activer(code, libre);
            FabriqueMenu.Activer(retour, !abonne.Occupe);

            bool erreur = !abonne.Occupe && !string.IsNullOrEmpty(abonne.DerniereErreur);
            etat.text = abonne.Occupe || abonne.Connecte ? abonne.Etat : erreur ? abonne.DerniereErreur : "";
            etat.color = erreur ? FabriqueMenu.Corail : FabriqueMenu.Ivoire;
        }
    }
}
