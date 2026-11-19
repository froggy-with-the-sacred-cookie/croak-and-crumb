using UnityEngine;

namespace Froggy.Audio
{
    /// <summary>
    /// Les morceaux et le dosage de la musique (P6-08). Musique générée avec Sound Forge à partir du thème A « Jardin
    /// malicieux », choisi par Florent le 19 novembre 2026. La partition est Tools/musique/froggy_musique.py ; les
    /// masters WAV restent chez Florent (Sound Forge). Les durées et volumes sont des PROPOSITIONS, à régler à l'oreille
    /// en jeu (P6-09 pour le mixage).
    /// </summary>
    [CreateAssetMenu(menuName = "Froggy/Audio/Réglages de la musique", fileName = "ReglagesMusique")]
    public sealed class ReglagesMusique : ScriptableObject
    {
        /// <summary>Le nom à charger par Resources.</summary>
        public const string Chemin = "ReglagesMusique";

        [Header("Menu")]
        [Tooltip("Le thème du menu, en boucle.")]
        public AudioClip menu;

        [Header("Partie (jardin japonais) : quatre couches de même longueur, lues ensemble en boucle")]
        public AudioClip ambiance;
        public AudioClip melodie;
        public AudioClip rythme;
        [Tooltip("S'ajoute dans la dernière minute et en prolongation.")]
        public AudioClip tension;

        [Tooltip("Tempo des couches, en battements par minute : la tension attend la mesure suivante pour entrer.")]
        [Min(1f)] public float tempo = 112f;
        [Min(1)] public int tempsParMesure = 4;

        [Header("Fin de partie")]
        public AudioClip victoire;
        public AudioClip defaite;

        [Header("Dosage (PROPOSITIONS)")]
        [Tooltip("Volume de la musique sous les effets, avant le réglage « Musique » du joueur.")]
        [Range(0f, 1f)] public float volume = .6f;

        [Tooltip("Temps restant, en s, sous lequel la tension entre.")]
        [Min(0f)] public float seuilDerniereMinute = 60f;

        [Tooltip("La tension ne ressort qu'au-dessus du seuil plus cette marge, en s (hystérésis).")]
        [Min(0f)] public float margeSortie = 5f;

        [Tooltip("Durée d'un fondu qui monte (menu, départ, mélodie), en s.")]
        [Min(0f)] public float fonduEntree = 1.5f;

        [Tooltip("Durée du fondu de la tension, en s : elle entre sans coupure.")]
        [Min(0f)] public float fonduTension = 2f;

        [Tooltip("Durée d'un fondu qui descend, en s.")]
        [Min(0f)] public float fonduSortie = 1f;

        [Tooltip("Durée du fondu du jardin quand la partie finit, en s (le jingle part tout de suite).")]
        [Min(0f)] public float fonduFin = .4f;

        /// <summary>La durée d'une mesure des couches, en s.</summary>
        public double DureeMesure => tempsParMesure * 60d / tempo;

        /// <summary>Le morceau d'une couche du jardin, ou le thème du menu.</summary>
        public AudioClip Clip(Couche c) => c switch
        {
            Couche.Menu => menu,
            Couche.Ambiance => ambiance,
            Couche.Melodie => melodie,
            Couche.Rythme => rythme,
            _ => tension,
        };
    }
}
