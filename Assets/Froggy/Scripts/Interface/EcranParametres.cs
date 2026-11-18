using System;
using UnityEngine;
using UnityEngine.UI;

namespace Froggy.Interface
{
    /// <summary>
    /// Paramètres (P6-03, premier pas de P6-05) : volumes par famille et intensité des effets forts, de 0 à 100 %.
    /// Chaque curseur s'applique tout de suite (ReglagesJoueur.Change) et s'enregistre en quittant l'écran.
    /// Le volume général règle tout le son ; l'ambiance règle celle du menu. Musique, effets, secousses, flashs et
    /// vibrations sont lus par les postes qui les jouent (ReglagesJoueur.Actuels).
    /// Pas encore : réaffectation des touches, champ de vision (P6-05).
    /// </summary>
    public sealed class EcranParametres : EcranMenu
    {
        private Slider premier;
        private Ecran retourVers = Ecran.Accueil;
        private bool modifie;

        public override Selectable PremierFocus => premier;

        /// <summary>L'écran d'où l'on vient, pour y revenir.</summary>
        public void Depuis(Ecran ecran) => retourVers = ecran;

        protected override void Construire()
        {
            FabriqueMenu f = Fabrique;
            f.Titre("Paramètres", transform, new Vector2(0f, 430f));
            RectTransform panneau = FabriqueMenu.Panneau("Panneau", transform, new Vector2(0f, -20f),
                new Vector2(1100f, 720f), FabriqueMenu.Nuit, FabriqueMenu.Turquoise);

            ReglagesJoueur r = ReglagesJoueur.Actuels;
            Section(f, panneau, "Son", 300f);
            premier = Ligne(f, panneau, "Général", 235f, () => r.volumeGeneral, v => Noter(r.volumeGeneral = v));
            Ligne(f, panneau, "Musique", 172f, () => r.volumeMusique, v => Noter(r.volumeMusique = v));
            Ligne(f, panneau, "Effets", 109f, () => r.volumeEffets, v => Noter(r.volumeEffets = v));
            Ligne(f, panneau, "Ambiance", 46f, () => r.volumeAmbiance, v => Noter(r.volumeAmbiance = v));

            Section(f, panneau, "Confort", -35f);
            Ligne(f, panneau, "Secousses", -100f, () => r.secousses, v => Noter(r.secousses = v));
            Ligne(f, panneau, "Flashs", -163f, () => r.flashs, v => Noter(r.flashs = v));
            Ligne(f, panneau, "Vibrations", -226f, () => r.vibrations, v => Noter(r.vibrations = v));

            f.Texte("0 % coupe l'effet. Réglages gardés sur cet ordinateur.", panneau, 28, FabriqueMenu.Brume,
                TextAnchor.MiddleCenter, new Vector2(0f, -300f), new Vector2(1000f, 44f));
            f.Feuille("Retour", transform, false, new Vector2(0f, -455f), new Vector2(380f, 100f), () => Retour());
        }

        private void Noter(float _) => modifie = true;

        private static void Section(FabriqueMenu f, Transform parent, string titre, float y)
        {
            f.Texte(titre, parent, 38, FabriqueMenu.Anis, TextAnchor.MiddleLeft, new Vector2(-150f, y),
                new Vector2(700f, 56f));
        }

        private static Slider Ligne(FabriqueMenu f, Transform parent, string nom, float y, Func<float> lire,
            Action<float> ecrire)
        {
            Text libelle = f.Texte(nom, parent, 34, FabriqueMenu.Ivoire, TextAnchor.MiddleLeft, new Vector2(-300f, y),
                new Vector2(340f, 56f));
            Slider curseur = f.Curseur(nom, parent, new Vector2(90f, y), new Vector2(460f, 22f));
            curseur.gameObject.AddComponent<LibelleDuFocus>().Libelle = libelle;
            Text valeur = f.Texte("", parent, 32, FabriqueMenu.Ivoire, TextAnchor.MiddleRight, new Vector2(415f, y),
                new Vector2(130f, 56f));

            curseur.SetValueWithoutNotify(lire());
            valeur.text = Pourcent(lire());
            curseur.onValueChanged.AddListener(v =>
            {
                ecrire(v);
                valeur.text = Pourcent(v);
                ReglagesJoueur.Prevenir();
            });
            return curseur;
        }

        private static string Pourcent(float v) => Mathf.RoundToInt(v * 100f) + " %";

        public override bool Retour()
        {
            Menu.Afficher(retourVers);
            return true;
        }

        /// <summary>En quittant l'écran : on n'écrit le fichier du joueur que si un curseur a bougé.</summary>
        private void OnDisable()
        {
            if (!modifie) return;
            modifie = false;
            ReglagesJoueur.Enregistrer();
        }
    }
}
