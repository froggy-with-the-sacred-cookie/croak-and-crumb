using System.Collections;
using Froggy.Reseau;
using UnityEngine;
using UnityEngine.UI;

namespace Froggy.Interface
{
    /// <summary>
    /// La salle d'attente (P6-03), d'après la maquette de Julien : le code d'invitation à copier, les 4 places (couleur
    /// et numéro du joueur, prêt ou en attente), puis « Se mettre prêt » pour un invité et « Lancer la partie » pour
    /// l'hôte. Tout passe par Salon.Local (P2-03) ; les textes viennent de ContenuSalle.
    /// La couleur d'un joueur est toujours doublée de son numéro (skill accessibilite : jamais la couleur seule).
    /// </summary>
    public sealed class EcranSalleAttente : EcranMenu
    {
        private sealed class Ligne
        {
            public Image Bord;
            public Image Pastille;
            public Text Numero;
            public Text Nom;
            public Image FondEtat;
            public Text Etat;
        }

        private static readonly Color BordLigne = new Color(0.43f, 0.80f, 0.78f, 0.45f);

        private Text nomSalon, compte, code, aide;
        private Button copier, quitter, couleur, principal;
        private readonly Ligne[] lignes = new Ligne[EtatSalon.JoueursMax];
        private Salon abonne;
        private Coroutine copie;

        /// <summary>Le bouton principal, ou « Copier » tant que l'hôte ne peut pas lancer.</summary>
        public override Selectable PremierFocus => principal != null && principal.interactable ? principal : copier;

        protected override void Construire()
        {
            FabriqueMenu f = Fabrique;
            f.Titre("Salle d'attente", transform, new Vector2(0f, 440f));
            RectTransform panneau = FabriqueMenu.Panneau("Panneau", transform, new Vector2(0f, -55f),
                new Vector2(1240f, 830f), FabriqueMenu.Nuit, FabriqueMenu.Turquoise);

            RectTransform entete = FabriqueMenu.Panneau("Salon", panneau, new Vector2(0f, 300f), new Vector2(1160f, 84f),
                FabriqueMenu.NuitProfonde, FabriqueMenu.Turquoise, 1.5f);
            nomSalon = f.Texte("Salon", entete, 42, FabriqueMenu.Ivoire, TextAnchor.MiddleLeft,
                new Vector2(-250f, 0f), new Vector2(600f, 70f));
            compte = f.Texte("", entete, 38, FabriqueMenu.Ivoire, TextAnchor.MiddleRight,
                new Vector2(330f, 0f), new Vector2(400f, 70f));

            f.Texte("Code d'invitation", panneau, 36, FabriqueMenu.Ivoire, TextAnchor.MiddleLeft,
                new Vector2(-405f, 200f), new Vector2(340f, 70f));
            code = f.Puce("", panneau, new Vector2(30f, 200f), new Vector2(330f, 80f), FabriqueMenu.Ivoire,
                FabriqueMenu.Encre, 54);
            copier = f.Pilule("Copier", panneau, new Vector2(415f, 200f), new Vector2(250f, 76f), Copier);

            for (int i = 0; i < lignes.Length; i++)
                lignes[i] = ConstruireLigne(f, panneau, new Vector2(0f, 100f - i * 92f));

            aide = f.Texte("", panneau, 30, FabriqueMenu.Ivoire, TextAnchor.MiddleCenter,
                new Vector2(0f, -258f), new Vector2(1100f, 50f));
            quitter = f.Pilule("Quitter le salon", panneau, new Vector2(-420f, -350f), new Vector2(300f, 80f), Quitter);
            couleur = f.Pilule("Changer de couleur", panneau, new Vector2(-75f, -350f), new Vector2(350f, 80f),
                () => abonne?.CouleurSuivante());
            principal = f.Feuille("Se mettre prêt", panneau, true, new Vector2(330f, -350f), new Vector2(460f, 112f),
                Principal);
        }

        private Ligne ConstruireLigne(FabriqueMenu f, Transform parent, Vector2 position)
        {
            var l = new Ligne();
            Image fond = FabriqueMenu.Arrondi("Place", parent, true, FabriqueMenu.NuitProfonde, 1.5f);
            FabriqueMenu.Placer(fond.rectTransform, position, new Vector2(1160f, 80f));
            l.Bord = FabriqueMenu.Arrondi("Bord", fond.transform, false, BordLigne, 1.5f);
            FabriqueMenu.Etirer(l.Bord.rectTransform);

            l.Pastille = FabriqueMenu.Arrondi("Couleur", fond.transform, true, Color.white, 2f);
            FabriqueMenu.Placer(l.Pastille.rectTransform, new Vector2(-530f, 0f), new Vector2(58f, 58f));
            l.Numero = f.Texte("", l.Pastille.transform, 34, FabriqueMenu.Encre, TextAnchor.MiddleCenter,
                new Vector2(0f, -1f), new Vector2(58f, 58f));
            l.Nom = f.Texte("", fond.transform, 40, FabriqueMenu.Ivoire, TextAnchor.MiddleLeft,
                new Vector2(-120f, 0f), new Vector2(680f, 70f));
            l.Etat = f.Puce("", fond.transform, new Vector2(430f, 0f), new Vector2(270f, 60f), FabriqueMenu.Ivoire,
                FabriqueMenu.Encre, 32);
            l.FondEtat = l.Etat.transform.parent.GetComponent<Image>();
            return l;
        }

        private void OnEnable()
        {
            abonne = Salon.Local;
            if (abonne != null) abonne.Change += Rafraichir;
            Libeller(copier, "Copier");
            Rafraichir();
        }

        private void OnDisable()
        {
            if (abonne != null) abonne.Change -= Rafraichir;
            abonne = null;
            if (copie != null) StopCoroutine(copie);
            copie = null;
        }

        private void Principal()
        {
            if (abonne == null || !abonne.Moi.HasValue) return;
            if (abonne.EstHote) abonne.Lancer();
            else abonne.SeDirePret(!abonne.Moi.Value.Pret);
        }

        private void Quitter()
        {
            abonne?.Quitter();
            Menu.Afficher(Ecran.Salons);
        }

        private void Copier()
        {
            if (abonne == null || string.IsNullOrEmpty(abonne.Code)) return;
            GUIUtility.systemCopyBuffer = abonne.Code;
            if (copie != null) StopCoroutine(copie);
            copie = StartCoroutine(Copie());
        }

        private IEnumerator Copie()
        {
            Libeller(copier, "Copié !");
            yield return new WaitForSecondsRealtime(1.5f);
            Libeller(copier, "Copier");
            copie = null;
        }

        private static void Libeller(Button b, string texte) => FabriqueMenu.Libeller(b, texte);

        private void Rafraichir()
        {
            if (abonne == null || !abonne.Connecte) return;
            MembreSalon? moi = abonne.Moi;
            PhaseSalon phase = abonne.Phase;
            bool pret = moi.HasValue && moi.Value.Pret;

            nomSalon.text = $"Salon {abonne.Code}";
            compte.text = ContenuSalle.Compte(abonne.Membres.Count);
            code.text = abonne.Code;

            PlaceSalle[] places = ContenuSalle.Places(abonne.Membres, moi.HasValue ? moi.Value.ClientId : (int?)null);
            for (int i = 0; i < lignes.Length; i++) Dessiner(lignes[i], places[i]);

            aide.text = ContenuSalle.Aide(abonne.Membres, abonne.EstHote, pret, phase);
            FabriqueMenu.Libeller(principal, ContenuSalle.BoutonPrincipal(abonne.EstHote, pret, phase));
            bool enSalon = phase == PhaseSalon.Salon;
            FabriqueMenu.Activer(principal, enSalon && moi.HasValue && (!abonne.EstHote || abonne.PeutLancer));
            FabriqueMenu.Activer(couleur, enSalon && moi.HasValue);
            FabriqueMenu.Activer(quitter, enSalon);
            Libeller(quitter, abonne.EstHote ? "Fermer le salon" : "Quitter le salon");
        }

        private void Dessiner(Ligne l, PlaceSalle p)
        {
            bool libre = p.Etat == EtatPlace.Libre;
            l.Nom.text = p.Nom;
            l.Nom.color = libre ? FabriqueMenu.Brume : FabriqueMenu.Ivoire;
            l.Bord.color = p.Vous ? FabriqueMenu.Turquoise : libre ? new Color(1f, 1f, 1f, 0.22f) : BordLigne;

            l.Pastille.gameObject.SetActive(!libre);
            l.FondEtat.gameObject.SetActive(!libre);
            if (libre) return;

            Color teinte = abonne.Couleur(p.Couleur);
            l.Pastille.color = teinte;
            l.Numero.text = p.Numero.ToString();
            l.Numero.color = Luminance(teinte) > 0.45f ? FabriqueMenu.Encre : FabriqueMenu.Ivoire;
            l.Etat.text = p.TexteEtat;
            l.FondEtat.color = p.Etat switch
            {
                EtatPlace.Pret => FabriqueMenu.Anis,
                EtatPlace.Hote => FabriqueMenu.Turquoise,
                _ => FabriqueMenu.Ivoire
            };
        }

        private static float Luminance(Color c) => 0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b;
    }
}
