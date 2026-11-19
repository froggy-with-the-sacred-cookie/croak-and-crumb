using System;
using Froggy.Regles;

namespace Froggy.Audio
{
    /// <summary>Ce que la musique joue (P6-08).</summary>
    public enum EtatMusique
    {
        /// <summary>Ni menu ni partie (une scène d'essai sans déroulement) : rien.</summary>
        Silence,
        /// <summary>La scène Menu seule : le thème du menu.</summary>
        Menu,
        /// <summary>Partie en attente ou compte à rebours : le jardin, ambiance seule.</summary>
        Depart,
        /// <summary>En jeu : ambiance, mélodie et rythme.</summary>
        Partie,
        /// <summary>La dernière minute ou la prolongation : la couche de tension s'ajoute.</summary>
        DerniereMinute,
        /// <summary>Partie finie, gagnée par le joueur de cette machine : le jingle de victoire.</summary>
        Victoire,
        /// <summary>Partie finie, gagnée par un autre : le jingle de défaite.</summary>
        Defaite,
    }

    /// <summary>Les voix de la musique : le thème du menu et les quatre couches du jardin, lues ensemble.</summary>
    public enum Couche { Menu, Ambiance, Melodie, Rythme, Tension }

    /// <summary>Le volume visé pour chaque voix, de 0 à 1.</summary>
    public readonly struct Dosage
    {
        public readonly float Menu, Ambiance, Melodie, Rythme, Tension;

        public Dosage(float menu, float ambiance, float melodie, float rythme, float tension)
        {
            Menu = menu; Ambiance = ambiance; Melodie = melodie; Rythme = rythme; Tension = tension;
        }

        public float this[Couche c] => c switch
        {
            Couche.Menu => Menu,
            Couche.Ambiance => Ambiance,
            Couche.Melodie => Melodie,
            Couche.Rythme => Rythme,
            _ => Tension,
        };

        /// <summary>Vrai si une couche du jardin doit sonner.</summary>
        public bool Jardin => Ambiance > 0f || Melodie > 0f || Rythme > 0f || Tension > 0f;
    }

    /// <summary>Ce que la musique lit de la partie à chaque image.</summary>
    public struct PhotoMusique
    {
        public bool MenuVisible;
        public bool PartieConnue;
        public Phase Phase;
        /// <summary>Le temps qui reste dans la phase en cours, en s (ReglesPartie.Reste).</summary>
        public float Reste;
        public int Gagnant;
        /// <summary>Le joueur de cette machine, ou ReglesPartie.Personne.</summary>
        public int Local;
    }

    /// <summary>
    /// Ce que joue la musique selon la partie (P6-08), sans scène ni son : du C# pur, testé seul. La musique suit la
    /// partie de la machine qui l'écoute (en ligne, celle que l'hôte envoie) : elle ne décide rien du jeu.
    /// La dernière minute a une hystérésis : on y entre sous le seuil, on n'en ressort qu'au-dessus du seuil plus une
    /// marge (un temps reçu du réseau qui remonte d'un poil ne fait pas clignoter la tension).
    /// </summary>
    public sealed class ReglesMusique
    {
        private readonly float seuil, marge;

        public ReglesMusique(float seuilDerniereMinute, float margeSortie)
        {
            seuil = Math.Max(0f, seuilDerniereMinute);
            marge = Math.Max(0f, margeSortie);
        }

        public EtatMusique Etat { get; private set; } = EtatMusique.Silence;

        /// <summary>Lit la partie et rend l'état de la musique (gardé dans Etat).</summary>
        public EtatMusique Decider(in PhotoMusique p)
        {
            Etat = Suivant(p);
            return Etat;
        }

        private EtatMusique Suivant(in PhotoMusique p)
        {
            if (p.MenuVisible) return EtatMusique.Menu;
            if (!p.PartieConnue) return EtatMusique.Silence;
            switch (p.Phase)
            {
                case Phase.Attente:
                case Phase.Decompte:
                    return EtatMusique.Depart;
                case Phase.EnJeu:
                    bool dedans = Etat == EtatMusique.DerniereMinute ? p.Reste <= seuil + marge : p.Reste <= seuil;
                    return dedans ? EtatMusique.DerniereMinute : EtatMusique.Partie;
                case Phase.Prolongation:
                    return EtatMusique.DerniereMinute;
                default:
                    // la fin se décide une fois : le jingle ne change pas en route
                    if (Etat == EtatMusique.Victoire || Etat == EtatMusique.Defaite) return Etat;
                    // sans joueur sur cette machine (spectateur), on fête le gagnant
                    bool gagne = p.Local == ReglesPartie.Personne || p.Gagnant == p.Local;
                    return gagne ? EtatMusique.Victoire : EtatMusique.Defaite;
            }
        }

        /// <summary>Le volume visé de chaque voix dans un état.</summary>
        public static Dosage DosageDe(EtatMusique etat) => etat switch
        {
            EtatMusique.Menu => new Dosage(1f, 0f, 0f, 0f, 0f),
            EtatMusique.Depart => new Dosage(0f, 1f, 0f, 0f, 0f),
            EtatMusique.Partie => new Dosage(0f, 1f, 1f, 1f, 0f),
            EtatMusique.DerniereMinute => new Dosage(0f, 1f, 1f, 1f, 1f),
            _ => new Dosage(0f, 0f, 0f, 0f, 0f),
        };

        /// <summary>Vrai pour les états qui lancent un jingle (une fois, en entrant).</summary>
        public static bool JoueUnJingle(EtatMusique etat) => etat == EtatMusique.Victoire || etat == EtatMusique.Defaite;

        /// <summary>
        /// Vrai si le changement attend la mesure suivante : entre deux états qui gardent le jardin, pour que la tension
        /// entre en rythme. La fin de partie, elle, n'attend jamais (skill son-integration).
        /// </summary>
        public static bool AttendLaMesure(EtatMusique avant, EtatMusique apres)
        {
            return DosageDe(avant).Jardin && DosageDe(apres).Jardin;
        }

        /// <summary>L'instant (en temps audio) de la prochaine barre de mesure des couches lancées à « debut ».</summary>
        public static double ProchaineMesure(double debut, double maintenant, double dureeMesure)
        {
            if (dureeMesure <= 0d || maintenant <= debut) return Math.Max(debut, maintenant);
            double mesures = Math.Ceiling((maintenant - debut) / dureeMesure - 1e-9);
            return debut + mesures * dureeMesure;
        }
    }

    /// <summary>
    /// Les fondus des voix de la musique, sans son : un volume courant par voix qui rejoint sa cible à vitesse fixe. Un
    /// dosage peut attendre un instant (la mesure suivante) avant de commencer.
    /// </summary>
    public sealed class MelangeurMusique
    {
        private static readonly int Nombre = Enum.GetValues(typeof(Couche)).Length;

        private readonly float[] courant = new float[Nombre];
        private readonly float[] cible = new float[Nombre];
        private readonly float[] vitesse = new float[Nombre];
        private readonly float[] cibleEnAttente = new float[Nombre];
        private readonly float[] vitesseEnAttente = new float[Nombre];
        private double debutEnAttente = double.NaN;

        public float Volume(Couche c) => courant[(int)c];
        public float Cible(Couche c) => cible[(int)c];

        /// <summary>Vrai tant qu'un dosage attend son instant.</summary>
        public bool EnAttente => !double.IsNaN(debutEnAttente);

        /// <summary>
        /// Vise un dosage. Les fondus commencent à « aPartirDe » (temps audio) ; une voix qui monte met « montee »
        /// secondes pour passer de 0 à 1, une voix qui descend « descente » secondes de 1 à 0.
        /// </summary>
        public void Viser(Dosage dosage, double aPartirDe, double maintenant, float montee, float descente)
        {
            for (int i = 0; i < Nombre; i++)
            {
                float v = Clamp01(dosage[(Couche)i]);
                cibleEnAttente[i] = v;
                float duree = v >= courant[i] ? montee : descente;
                vitesseEnAttente[i] = duree <= 0f ? float.PositiveInfinity : 1f / duree;
            }
            debutEnAttente = aPartirDe;
            if (aPartirDe <= maintenant) Commencer();
        }

        /// <summary>Avance les fondus de dt secondes ; « maintenant » (temps audio) déclenche un dosage en attente.</summary>
        public void Avancer(double maintenant, float dt)
        {
            if (EnAttente && maintenant >= debutEnAttente) Commencer();
            for (int i = 0; i < Nombre; i++)
            {
                float pas = float.IsPositiveInfinity(vitesse[i]) ? 1f : vitesse[i] * Math.Max(0f, dt);
                if (courant[i] < cible[i]) courant[i] = Math.Min(cible[i], courant[i] + pas);
                else if (courant[i] > cible[i]) courant[i] = Math.Max(cible[i], courant[i] - pas);
            }
        }

        /// <summary>Vrai si les quatre couches du jardin sont muettes et doivent le rester.</summary>
        public bool JardinMuet
        {
            get
            {
                for (int i = (int)Couche.Ambiance; i < Nombre; i++)
                    if (courant[i] > 0f || cible[i] > 0f || (EnAttente && cibleEnAttente[i] > 0f)) return false;
                return true;
            }
        }

        /// <summary>Remet les couches du jardin à zéro, sans fondu (elles repartent du début).</summary>
        public void CouperJardin()
        {
            for (int i = (int)Couche.Ambiance; i < Nombre; i++) courant[i] = 0f;
        }

        private void Commencer()
        {
            Array.Copy(cibleEnAttente, cible, Nombre);
            Array.Copy(vitesseEnAttente, vitesse, Nombre);
            debutEnAttente = double.NaN;
        }

        private static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;
    }
}
